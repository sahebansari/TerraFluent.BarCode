using System.IO.Compression;

namespace TerraFluent.BarCode.Rendering;

/// <summary>
/// Writes a two-colour image as a 1-bit indexed PNG: the smallest lossless
/// form for barcodes. Uses only <see cref="DeflateStream"/> from the BCL.
/// </summary>
internal static class PngEncoder
{
    private static readonly byte[] Signature = { 137, 80, 78, 71, 13, 10, 26, 10 };
    private static readonly uint[] CrcTable = BuildCrcTable();

    public static byte[] Encode(BarcodeMatrix matrix, RenderOptions options)
    {
        var (width, height) = options.ImageSize(matrix);
        var stride = (width + 7) / 8;
        var raw = new byte[height * (stride + 1)];

        // Every row of a linear barcode is identical, and QR rows repeat per module; build each distinct row once.
        var row = new byte[stride];
        var lastModuleRow = -1;
        for (var y = 0; y < height; y++)
        {
            var moduleRow = matrix.IsLinear ? 0 : y / options.ModuleSize;
            if (moduleRow != lastModuleRow)
            {
                Array.Clear(row, 0, stride);
                for (var x = 0; x < width; x++)
                {
                    // Palette index 1 = foreground.
                    if (options.IsDark(matrix, x, y)) row[x >> 3] |= (byte)(0x80 >> (x & 7));
                }

                lastModuleRow = moduleRow;
            }

            var offset = y * (stride + 1);
            raw[offset] = 0; // filter: none
            Buffer.BlockCopy(row, 0, raw, offset + 1, stride);
        }

        using var output = new MemoryStream();
        output.Write(Signature, 0, Signature.Length);

        var ihdr = new byte[13];
        WriteUInt32(ihdr, 0, (uint)width);
        WriteUInt32(ihdr, 4, (uint)height);
        ihdr[8] = 1;  // bit depth
        ihdr[9] = 3;  // colour type: indexed
        ihdr[10] = 0; // compression
        ihdr[11] = 0; // filter
        ihdr[12] = 0; // interlace
        WriteChunk(output, "IHDR", ihdr);

        var bg = options.Background;
        var fg = options.Foreground;
        WriteChunk(output, "PLTE", new[] { bg.R, bg.G, bg.B, fg.R, fg.G, fg.B });
        WriteChunk(output, "IDAT", ZlibCompress(raw));
        WriteChunk(output, "IEND", Array.Empty<byte>());
        return output.ToArray();
    }

    private static byte[] ZlibCompress(byte[] data)
    {
        using var ms = new MemoryStream();
        ms.WriteByte(0x78); // CMF: deflate, 32K window
        ms.WriteByte(0xDA); // FLG: best compression, valid check bits
        using (var deflate = new DeflateStream(ms, CompressionLevel.Optimal, leaveOpen: true))
        {
            deflate.Write(data, 0, data.Length);
        }

        var adler = Adler32(data);
        var tail = new byte[4];
        WriteUInt32(tail, 0, adler);
        ms.Write(tail, 0, 4);
        return ms.ToArray();
    }

    private static void WriteChunk(Stream output, string type, byte[] data)
    {
        var header = new byte[8];
        WriteUInt32(header, 0, (uint)data.Length);
        for (var i = 0; i < 4; i++) header[4 + i] = (byte)type[i];
        output.Write(header, 0, 8);
        output.Write(data, 0, data.Length);

        var crc = 0xFFFFFFFFu;
        for (var i = 4; i < 8; i++) crc = CrcTable[(crc ^ header[i]) & 0xFF] ^ (crc >> 8);
        foreach (var b in data) crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
        var tail = new byte[4];
        WriteUInt32(tail, 0, crc ^ 0xFFFFFFFFu);
        output.Write(tail, 0, 4);
    }

    private static uint Adler32(byte[] data)
    {
        uint a = 1, b = 0;
        foreach (var d in data)
        {
            a = (a + d) % 65521;
            b = (b + a) % 65521;
        }

        return (b << 16) | a;
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
