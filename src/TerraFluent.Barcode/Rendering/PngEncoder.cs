using System.IO.Compression;

namespace TerraFluent.Barcode.Rendering;

/// <summary>
/// Writes a two-colour image as a 1-bit indexed PNG: the smallest lossless
/// form for barcodes. Uses only <see cref="DeflateStream"/> from the BCL.
/// </summary>
internal static class PngEncoder
{
    private static readonly byte[] Signature = { 137, 80, 78, 71, 13, 10, 26, 10 };
    private static readonly uint[] CrcTable = BuildCrcTable();

    public static byte[] Encode(BarcodeMatrix matrix, RenderOptions options, bool transparent = false, CancellationToken cancellationToken = default)
    {
        using var output = new MemoryStream();
        Write(matrix, options, transparent, output, cancellationToken);
        return output.ToArray();
    }

    /// <summary>Writes a plain barcode as a 1-bit indexed PNG to <paramref name="output"/>, one row at a time.</summary>
    public static void Write(BarcodeMatrix matrix, RenderOptions options, bool transparent, Stream output, CancellationToken cancellationToken = default)
    {
        var (width, height) = options.ImageSize(matrix);
        var stride = (width + 7) / 8;

        output.Write(Signature, 0, Signature.Length);
        WriteChunk(output, "IHDR", Header(width, height, bitDepth: 1, colourType: 3));
        var bg = options.Background;
        var fg = options.Foreground;
        WriteChunk(output, "PLTE", new[] { bg.R, bg.G, bg.B, fg.R, fg.G, fg.B });
        if (transparent) WriteChunk(output, "tRNS", new byte[] { 0 }); // palette entry 0 (background) fully transparent

        // Every row of a linear barcode is identical, and 2D rows repeat per module; build each distinct row once.
        var row = new byte[stride + 1]; // filter byte (0: none) + pixels
        var lastModuleRow = -1;
        using (var zlib = new ZlibWriter(output))
        {
            for (var y = 0; y < height; y++)
            {
                var moduleRow = matrix.IsLinear ? 0 : y / options.ModuleSize;
                if (moduleRow != lastModuleRow)
                {
                    if ((y & 255) == 0) cancellationToken.ThrowIfCancellationRequested();
                    Array.Clear(row, 0, row.Length);
                    for (var x = 0; x < width; x++)
                    {
                        // Palette index 1 = foreground.
                        if (options.IsDark(matrix, x, y)) row[1 + (x >> 3)] |= (byte)(0x80 >> (x & 7));
                    }

                    lastModuleRow = moduleRow;
                }

                zlib.Write(row, row.Length);
            }
        }

        WriteChunk(output, "IEND", Array.Empty<byte>());
    }

    /// <summary>Writes a full-colour image (styled barcodes) as an 8-bit RGB or RGBA PNG, using the Sub filter.</summary>
    public static byte[] Encode(RgbImage image)
    {
        using var output = new MemoryStream();
        Write(image.Width, image.Height, image.Alpha is not null, output, (y0, rows) => (image, y0), image.Height, default);
        return output.ToArray();
    }

    /// <summary>Rasterises a styled scene band by band and writes it as an 8-bit RGB or RGBA PNG.</summary>
    public static byte[] Encode(Scene scene, CancellationToken cancellationToken = default)
    {
        using var output = new MemoryStream();
        Write(scene, output, cancellationToken);
        return output.ToArray();
    }

    /// <inheritdoc cref="Encode(Scene, CancellationToken)"/>
    public static void Write(Scene scene, Stream output, CancellationToken cancellationToken = default)
    {
        var rasterizer = new Rasterizer(scene);
        var band = rasterizer.CreateBand();
        Write(scene.Width, scene.Height, scene.Transparent, output, (y0, rows) =>
        {
            rasterizer.RenderBand(y0, band, rows);
            return (band, 0);
        }, rasterizer.BandRows, cancellationToken);
    }

    // nextBand(y0, rows) renders rows y0.. into an image and says which of its rows holds y0.
    private static void Write(int width, int height, bool alpha, Stream output, Func<int, int, (RgbImage Image, int FirstRow)> nextBand, int bandRows, CancellationToken cancellationToken)
    {
        var channels = alpha ? 4 : 3;
        var stride = width * channels;
        output.Write(Signature, 0, Signature.Length);
        WriteChunk(output, "IHDR", Header(width, height, bitDepth: 8, colourType: (byte)(alpha ? 6 : 2)));

        var row = new byte[stride];
        var filtered = new byte[stride + 1];
        filtered[0] = 1; // filter: sub
        using (var zlib = new ZlibWriter(output))
        {
            for (var y0 = 0; y0 < height; y0 += bandRows)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var rows = Math.Min(bandRows, height - y0);
                var (image, firstRow) = nextBand(y0, rows);
                var px = image.Pixels;
                var alphaChannel = image.Alpha;
                for (var r = 0; r < rows; r++)
                {
                    var source = (firstRow + r) * width;
                    for (var x = 0; x < width; x++)
                    {
                        var s = (source + x) * 3;
                        var d = x * channels;
                        row[d] = px[s];
                        row[d + 1] = px[s + 1];
                        row[d + 2] = px[s + 2];
                        if (alphaChannel is not null) row[d + 3] = alphaChannel[source + x];
                    }

                    for (var i = 0; i < stride; i++)
                    {
                        var left = i >= channels ? row[i - channels] : 0;
                        filtered[1 + i] = (byte)(row[i] - left);
                    }

                    zlib.Write(filtered, filtered.Length);
                }
            }
        }

        WriteChunk(output, "IEND", Array.Empty<byte>());
    }

    private static byte[] Header(int width, int height, byte bitDepth, byte colourType)
    {
        var ihdr = new byte[13];
        WriteUInt32(ihdr, 0, (uint)width);
        WriteUInt32(ihdr, 4, (uint)height);
        ihdr[8] = bitDepth;
        ihdr[9] = colourType; // 2: RGB, 3: indexed, 6: RGBA
        // compression, filter and interlace stay 0
        return ihdr;
    }

    /// <summary>
    /// Streams a zlib stream (header, deflate data, Adler-32) as a series of
    /// IDAT chunks of at most <see cref="ChunkSize"/> bytes, so neither the raw
    /// pixels nor the compressed data are held in memory as a whole.
    /// </summary>
    private sealed class ZlibWriter : IDisposable
    {
        private const int ChunkSize = 64 * 1024;

        private readonly IdatStream _idat;
        private readonly DeflateStream _deflate;
        private uint _a = 1;
        private uint _b;

        public ZlibWriter(Stream output)
        {
            _idat = new IdatStream(output, ChunkSize);
            _idat.WriteByte(0x78); // CMF: deflate, 32K window
            _idat.WriteByte(0xDA); // FLG: best compression, valid check bits
            _deflate = new DeflateStream(_idat, CompressionLevel.Optimal, leaveOpen: true);
        }

        public void Write(byte[] data, int count)
        {
            _deflate.Write(data, 0, count);

            // Adler-32, reducing modulo 65521 once per 5,552 bytes (the most that cannot overflow 32 bits).
            const int Nmax = 5552;
            uint a = _a, b = _b;
            for (var offset = 0; offset < count;)
            {
                var end = Math.Min(count, offset + Nmax);
                for (; offset < end; offset++)
                {
                    a += data[offset];
                    b += a;
                }

                a %= 65521;
                b %= 65521;
            }

            _a = a;
            _b = b;
        }

        public void Dispose()
        {
            _deflate.Dispose();
            var tail = new byte[4];
            WriteUInt32(tail, 0, (_b << 16) | _a);
            _idat.Write(tail, 0, 4);
            _idat.Dispose();
        }
    }

    /// <summary>Buffers bytes and writes them out as IDAT chunks.</summary>
    private sealed class IdatStream : Stream
    {
        private readonly Stream _output;
        private readonly int _chunkSize;
        private byte[] _buffer;
        private int _count;

        public IdatStream(Stream output, int chunkSize)
        {
            _output = output;
            _chunkSize = chunkSize;
            _buffer = new byte[1024]; // grows to chunkSize; most barcodes compress to a few KB
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override void Write(byte[] buffer, int offset, int count)
        {
            while (count > 0)
            {
                Reserve(count);
                var n = Math.Min(count, _buffer.Length - _count);
                Buffer.BlockCopy(buffer, offset, _buffer, _count, n);
                _count += n;
                offset += n;
                count -= n;
                if (_count == _chunkSize) EmitChunk();
            }
        }

        public override void WriteByte(byte value)
        {
            Reserve(1);
            _buffer[_count++] = value;
            if (_count == _chunkSize) EmitChunk();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing && _count > 0) EmitChunk();
            base.Dispose(disposing);
        }

        /// <summary>Grows the buffer (doubling, up to one chunk) to make room for <paramref name="count"/> more bytes where possible.</summary>
        private void Reserve(int count)
        {
            if (_count + count <= _buffer.Length || _buffer.Length == _chunkSize) return;
            var size = _buffer.Length;
            while (size < _count + count && size < _chunkSize) size *= 2;
            Array.Resize(ref _buffer, Math.Min(size, _chunkSize));
        }

        private void EmitChunk()
        {
            WriteChunk(_output, "IDAT", _buffer, _count);
            _count = 0;
        }
    }

    private static void WriteChunk(Stream output, string type, byte[] data) => WriteChunk(output, type, data, data.Length);

    private static void WriteChunk(Stream output, string type, byte[] data, int count)
    {
        var header = new byte[8];
        WriteUInt32(header, 0, (uint)count);
        for (var i = 0; i < 4; i++) header[4 + i] = (byte)type[i];
        output.Write(header, 0, 8);
        output.Write(data, 0, count);

        var crc = 0xFFFFFFFFu;
        for (var i = 4; i < 8; i++) crc = CrcTable[(crc ^ header[i]) & 0xFF] ^ (crc >> 8);
        for (var i = 0; i < count; i++) crc = CrcTable[(crc ^ data[i]) & 0xFF] ^ (crc >> 8);
        var tail = new byte[4];
        WriteUInt32(tail, 0, crc ^ 0xFFFFFFFFu);
        output.Write(tail, 0, 4);
    }

    private static uint[] BuildCrcTable()
    {
        var table = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            var c = n;
            for (var k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            table[n] = c;
        }

        return table;
    }

    private static void WriteUInt32(byte[] buffer, int offset, uint value)
    {
        buffer[offset] = (byte)(value >> 24);
        buffer[offset + 1] = (byte)(value >> 16);
        buffer[offset + 2] = (byte)(value >> 8);
        buffer[offset + 3] = (byte)value;
    }
}
