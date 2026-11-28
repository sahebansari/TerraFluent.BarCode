namespace TerraFluent.Barcode.Rendering;

/// <summary>
/// Minimal baseline JPEG (ITU-T T.81) encoder: 8-bit, no chroma subsampling,
/// standard Annex K quantisation and Huffman tables. Writes a single-component
/// greyscale image when both colours are grey, otherwise YCbCr.
/// </summary>
internal static class JpegEncoder
{
    private static readonly int[] ZigZag =
    {
        0, 1, 8, 16, 9, 2, 3, 10, 17, 24, 32, 25, 18, 11, 4, 5,
        12, 19, 26, 33, 40, 48, 41, 34, 27, 20, 13, 6, 7, 14, 21, 28,
        35, 42, 49, 56, 57, 50, 43, 36, 29, 22, 15, 23, 30, 37, 44, 51,
        58, 59, 52, 45, 38, 31, 39, 46, 53, 60, 61, 54, 47, 55, 62, 63,
    };

    private static readonly int[] LuminanceQuant =
    {
        16, 11, 10, 16, 24, 40, 51, 61,
        12, 12, 14, 19, 26, 58, 60, 55,
        14, 13, 16, 24, 40, 57, 69, 56,
        14, 17, 22, 29, 51, 87, 80, 62,
        18, 22, 37, 56, 68, 109, 103, 77,
        24, 35, 55, 64, 81, 104, 113, 92,
        49, 64, 78, 87, 103, 121, 120, 101,
        72, 92, 95, 98, 112, 100, 103, 99,
    };

    private static readonly int[] ChrominanceQuant =
    {
        17, 18, 24, 47, 99, 99, 99, 99,
        18, 21, 26, 66, 99, 99, 99, 99,
        24, 26, 56, 99, 99, 99, 99, 99,
        47, 66, 99, 99, 99, 99, 99, 99,
        99, 99, 99, 99, 99, 99, 99, 99,
        99, 99, 99, 99, 99, 99, 99, 99,
        99, 99, 99, 99, 99, 99, 99, 99,
        99, 99, 99, 99, 99, 99, 99, 99,
    };

    // Annex K.3 luminance tables; all components share them, which is valid baseline JPEG.
    internal static readonly byte[] DcBits = { 0, 1, 5, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0 };
    internal static readonly byte[] DcValues = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };
    internal static readonly byte[] AcBits = { 0, 2, 1, 3, 3, 2, 4, 3, 5, 5, 4, 4, 0, 0, 1, 0x7D };
    internal static readonly byte[] AcValues =
    {
        0x01, 0x02, 0x03, 0x00, 0x04, 0x11, 0x05, 0x12, 0x21, 0x31, 0x41, 0x06, 0x13, 0x51, 0x61, 0x07,
        0x22, 0x71, 0x14, 0x32, 0x81, 0x91, 0xA1, 0x08, 0x23, 0x42, 0xB1, 0xC1, 0x15, 0x52, 0xD1, 0xF0,
        0x24, 0x33, 0x62, 0x72, 0x82, 0x09, 0x0A, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x25, 0x26, 0x27, 0x28,
        0x29, 0x2A, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3A, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49,
        0x4A, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59, 0x5A, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69,
        0x6A, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79, 0x7A, 0x83, 0x84, 0x85, 0x86, 0x87, 0x88, 0x89,
        0x8A, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97, 0x98, 0x99, 0x9A, 0xA2, 0xA3, 0xA4, 0xA5, 0xA6, 0xA7,
        0xA8, 0xA9, 0xAA, 0xB2, 0xB3, 0xB4, 0xB5, 0xB6, 0xB7, 0xB8, 0xB9, 0xBA, 0xC2, 0xC3, 0xC4, 0xC5,
        0xC6, 0xC7, 0xC8, 0xC9, 0xCA, 0xD2, 0xD3, 0xD4, 0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA, 0xE1, 0xE2,
        0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9, 0xEA, 0xF1, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8,
        0xF9, 0xFA,
    };

    // Basis[u * 8 + x] = c(u) / 2 * cos((2x + 1) u pi / 16), with c(0) = 1 / sqrt(2): the orthonormal 1D DCT-II.
    private static readonly double[] Basis = BuildBasis();
    private static readonly (int Code, int Length)[] DcTable = BuildHuffman(DcBits, DcValues);
    private static readonly (int Code, int Length)[] AcTable = BuildHuffman(AcBits, AcValues);

    public static byte[] Encode(BarcodeMatrix matrix, RenderOptions options, int quality, CancellationToken cancellationToken = default)
    {
        using var output = new MemoryStream();
        Write(matrix, options, quality, output, cancellationToken);
        return output.ToArray();
    }

    public static void Write(BarcodeMatrix matrix, RenderOptions options, int quality, Stream output, CancellationToken cancellationToken = default)
    {
        var (width, height) = options.ImageSize(matrix);
        var fg = options.Foreground;
        var bg = options.Background;
        var greyscale = fg.R == fg.G && fg.G == fg.B && bg.R == bg.G && bg.G == bg.B;

        // Per-component sample value for a dark and a light pixel.
        var darkSample = ToComponents(fg, greyscale);
        var lightSample = ToComponents(bg, greyscale);
        var dark = new bool[width];
        var lastModuleRow = -1;
        Write(output, width, height, greyscale, quality, (y, planes) =>
        {
            // Rows repeat within a module row (and everywhere for linear codes), so compute each distinct row once.
            var moduleRow = matrix.IsLinear ? 0 : y / options.ModuleSize;
            if (moduleRow != lastModuleRow)
            {
                for (var x = 0; x < width; x++) dark[x] = options.IsDark(matrix, x, y);
                lastModuleRow = moduleRow;
            }

            for (var c = 0; c < planes.Length; c++)
            {
                var plane = planes[c];
                double d = darkSample[c] - 128.0, l = lightSample[c] - 128.0;
                for (var x = 0; x < width; x++) plane[x] = dark[x] ? d : l;
            }
        }, cancellationToken);
    }

    /// <summary>Encodes a full-colour image (styled barcodes).</summary>
    public static byte[] Encode(RgbImage image, int quality)
    {
        using var output = new MemoryStream();
        var width = image.Width;
        Write(output, width, image.Height, image.IsGreyscale(), quality, (y, planes) => FillRow(image, y, planes), default);
        return output.ToArray();
    }

    /// <summary>Rasterises a styled scene band by band and encodes it.</summary>
    public static byte[] Encode(Scene scene, int quality, CancellationToken cancellationToken = default)
    {
        using var output = new MemoryStream();
        Write(scene, quality, output, cancellationToken);
        return output.ToArray();
    }

    /// <inheritdoc cref="Encode(Scene, int, CancellationToken)"/>
    public static void Write(Scene scene, int quality, Stream output, CancellationToken cancellationToken = default)
    {
        var rasterizer = new Rasterizer(scene);
        var band = rasterizer.CreateBand();
        var bandStart = -1;
        Write(output, scene.Width, scene.Height, scene.IsGreyscale, quality, (y, planes) =>
        {
            if (bandStart < 0 || y < bandStart || y >= bandStart + rasterizer.BandRows)
            {
                bandStart = y - y % rasterizer.BandRows;
                rasterizer.RenderBand(bandStart, band);
            }

            FillRow(band, y - bandStart, planes);
        }, cancellationToken);
    }

    private static void FillRow(RgbImage image, int y, double[][] planes)
    {
        var px = image.Pixels;
        var width = image.Width;
        var greyscale = planes.Length == 1;
        var i = y * width * 3;
        for (var x = 0; x < width; x++, i += 3)
        {
            double r = px[i], g = px[i + 1], b = px[i + 2];
            if (greyscale)
            {
                planes[0][x] = r - 128.0;
                continue;
            }

            planes[0][x] = 0.299 * r + 0.587 * g + 0.114 * b - 128.0;
            planes[1][x] = -0.168736 * r - 0.331264 * g + 0.5 * b + 128 - 128.0;
            planes[2][x] = 0.5 * r - 0.418688 * g - 0.081312 * b + 128 - 128.0;
        }
    }

    // fillRow(y, planes): writes row y of each component (Y, Cb, Cr; or grey), level-shifted to -128..127, into planes[c][0..width).
    private static void Write(Stream output, int width, int height, bool greyscale, int quality, Action<int, double[][]> fillRow, CancellationToken cancellationToken)
    {
        // The frame header stores 16-bit dimensions.
        if (width > ushort.MaxValue || height > ushort.MaxValue)
        {
            throw new ArgumentException($"JPEG images are limited to {ushort.MaxValue} pixels per side; this one would be {width} x {height}.");
        }

        var componentCount = greyscale ? 1 : 3;
        var quant = new[] { Scale(LuminanceQuant, quality), Scale(ChrominanceQuant, quality) };

        WriteMarker(output, 0xD8); // SOI
        WriteSegment(output, 0xE0, new byte[] { (byte)'J', (byte)'F', (byte)'I', (byte)'F', 0, 1, 1, 0, 0, 1, 0, 1, 0, 0 });

        var dqt = new List<byte>();
        for (var t = 0; t < (greyscale ? 1 : 2); t++)
        {
            dqt.Add((byte)t);
            for (var k = 0; k < 64; k++) dqt.Add((byte)quant[t][ZigZag[k]]);
        }

        WriteSegment(output, 0xDB, dqt.ToArray());

        var sof = new List<byte> { 8, (byte)(height >> 8), (byte)height, (byte)(width >> 8), (byte)width, (byte)componentCount };
        for (var c = 0; c < componentCount; c++) sof.AddRange(new[] { (byte)(c + 1), (byte)0x11, (byte)(c == 0 ? 0 : 1) });
        WriteSegment(output, 0xC0, sof.ToArray());

        var dht = new List<byte> { 0x00 };
        dht.AddRange(DcBits);
        dht.AddRange(DcValues);
        dht.Add(0x10);
        dht.AddRange(AcBits);
        dht.AddRange(AcValues);
        WriteSegment(output, 0xC4, dht.ToArray());

        var sos = new List<byte> { (byte)componentCount };
        for (var c = 0; c < componentCount; c++) sos.AddRange(new[] { (byte)(c + 1), (byte)0x00 });
        sos.AddRange(new byte[] { 0, 63, 0 });
        WriteSegment(output, 0xDA, sos.ToArray());

        var bits = new BitWriter(output);
        var previousDc = new int[componentCount];
        var block = new double[64];
        var temp = new double[64];
        var coefficients = new int[64];

        // One band of eight rows per component: memory grows with the width, not the image area.
        var paddedWidth = (width + 7) & ~7;
        var bands = new double[componentCount][][];
        for (var c = 0; c < componentCount; c++)
        {
            bands[c] = new double[8][];
            for (var y = 0; y < 8; y++) bands[c][y] = new double[paddedWidth];
        }

        var rowPlanes = new double[componentCount][];
        for (var by = 0; by < height; by += 8)
        {
            if ((by & 255) == 0) cancellationToken.ThrowIfCancellationRequested();
            for (var y = 0; y < 8; y++)
            {
                // Edge rows and columns repeat the last pixel, as before.
                var py = Math.Min(by + y, height - 1);
                for (var c = 0; c < componentCount; c++) rowPlanes[c] = bands[c][y];
                if (by + y < height || y == 0)
                {
                    fillRow(py, rowPlanes);
                }
                else
                {
                    for (var c = 0; c < componentCount; c++) Array.Copy(bands[c][y - 1], bands[c][y], paddedWidth);
                }

                for (var c = 0; c < componentCount; c++)
                {
                    var row = bands[c][y];
                    for (var x = width; x < paddedWidth; x++) row[x] = row[width - 1];
                }
            }

            for (var bx = 0; bx < width; bx += 8)
            {
                for (var c = 0; c < componentCount; c++)
                {
                    var band = bands[c];
                    var top = band[0];
                    var first = top[bx];
                    var flat = true;     // every sample equal
                    var columns = true;  // every row equal to the first (vertical bars)
                    for (var y = 0; y < 8; y++)
                    {
                        var row = band[y];
                        for (var x = 0; x < 8; x++)
                        {
                            var v = row[bx + x];
                            block[y * 8 + x] = v;
                            flat &= v == first;
                            columns &= v == top[bx + x];
                        }
                    }

                    var q = quant[c == 0 ? 0 : 1];
                    if (flat)
                    {
                        // A flat block has only a DC term (8 x the level-shifted sample); most 2D-code blocks are flat.
                        Array.Clear(coefficients, 0, 64);
                        coefficients[0] = (int)Math.Round(8.0 * first / q[0]);
                    }
                    else if (columns)
                    {
                        // Identical rows (linear barcodes): only the first row of coefficients is non-zero.
                        RowDct(block, q, coefficients);
                    }
                    else
                    {
                        ForwardDct(block, temp, q, coefficients);
                    }

                    EncodeBlock(bits, coefficients, ref previousDc[c]);
                }
            }
        }

        bits.Flush();
        WriteMarker(output, 0xD9); // EOI
    }

    private static double[] ToComponents(BarcodeColor color, bool greyscale)
    {
        if (greyscale) return new double[] { color.R };
        double r = color.R, g = color.G, b = color.B;
        return new[]
        {
            0.299 * r + 0.587 * g + 0.114 * b,
            -0.168736 * r - 0.331264 * g + 0.5 * b + 128,
            0.5 * r - 0.418688 * g - 0.081312 * b + 128,
        };
    }

    private static int[] Scale(int[] table, int quality)
    {
        var scale = quality < 50 ? 5000 / quality : 200 - quality * 2;
        var result = new int[64];
        for (var i = 0; i < 64; i++) result[i] = Math.Min(255, Math.Max(1, (table[i] * scale + 50) / 100));
        return result;
    }

    /// <summary>
    /// Separable 8x8 forward DCT (rows, then columns) with quantisation:
    /// 1,024 multiply-adds per block instead of 4,096 for the direct 2D sum.
    /// </summary>
    private static void ForwardDct(double[] block, double[] temp, int[] quant, int[] output)
    {
        var basis = Basis;
        for (var y = 0; y < 8; y++)
        {
            var row = y * 8;
            for (var u = 0; u < 8; u++)
            {
                var b = u * 8;
                temp[row + u] =
                    block[row] * basis[b] + block[row + 1] * basis[b + 1] + block[row + 2] * basis[b + 2] + block[row + 3] * basis[b + 3] +
                    block[row + 4] * basis[b + 4] + block[row + 5] * basis[b + 5] + block[row + 6] * basis[b + 6] + block[row + 7] * basis[b + 7];
            }
        }

        for (var u = 0; u < 8; u++)
        {
            for (var v = 0; v < 8; v++)
            {
                var b = v * 8;
                var sum =
                    temp[u] * basis[b] + temp[8 + u] * basis[b + 1] + temp[16 + u] * basis[b + 2] + temp[24 + u] * basis[b + 3] +
                    temp[32 + u] * basis[b + 4] + temp[40 + u] * basis[b + 5] + temp[48 + u] * basis[b + 6] + temp[56 + u] * basis[b + 7];
                var index = v * 8 + u;
                output[index] = (int)Math.Round(sum / quant[index]);
            }
        }
    }

    /// <summary>DCT of a block whose eight rows are equal: a 1D DCT of the first row, scaled by the v = 0 column basis (8 x c(0) / 2).</summary>
    private static void RowDct(double[] block, int[] quant, int[] output)
    {
        var basis = Basis;
        var scale = 8 * basis[0];
        Array.Clear(output, 0, 64);
        for (var u = 0; u < 8; u++)
        {
            var b = u * 8;
            var sum =
                block[0] * basis[b] + block[1] * basis[b + 1] + block[2] * basis[b + 2] + block[3] * basis[b + 3] +
                block[4] * basis[b + 4] + block[5] * basis[b + 5] + block[6] * basis[b + 6] + block[7] * basis[b + 7];
            output[u] = (int)Math.Round(sum * scale / quant[u]);
        }
    }

    private static void EncodeBlock(BitWriter bits, int[] coefficients, ref int previousDc)
    {
        var dc = coefficients[0];
        var diff = dc - previousDc;
        previousDc = dc;
        var dcSize = BitSize(diff);
        bits.Write(DcTable[dcSize]);
        if (dcSize > 0) bits.Write(Amplitude(diff, dcSize), dcSize);

        var run = 0;
        for (var k = 1; k < 64; k++)
        {
            var value = coefficients[ZigZag[k]];
            if (value == 0)
            {
                run++;
                continue;
            }

            while (run > 15)
            {
                bits.Write(AcTable[0xF0]); // ZRL: sixteen zeros
                run -= 16;
            }

            var size = BitSize(value);
            bits.Write(AcTable[(run << 4) | size]);
            bits.Write(Amplitude(value, size), size);
            run = 0;
        }

        if (run > 0) bits.Write(AcTable[0x00]); // EOB
    }

    private static int BitSize(int value)
    {
        value = Math.Abs(value);
        var size = 0;
        while (value > 0)
        {
            size++;
            value >>= 1;
        }

        return size;
    }

    private static int Amplitude(int value, int size) => value >= 0 ? value : value + (1 << size) - 1;

    internal static (int Code, int Length)[] BuildHuffman(byte[] bits, byte[] values)
    {
        var table = new (int Code, int Length)[256];
        var code = 0;
        var k = 0;
        for (var length = 1; length <= 16; length++)
        {
            for (var i = 0; i < bits[length - 1]; i++) table[values[k++]] = (code++, length);
            code <<= 1;
        }

        return table;
    }

    private static double[] BuildBasis()
    {
        var table = new double[64];
        for (var u = 0; u < 8; u++)
        {
            var scale = (u == 0 ? 1 / Math.Sqrt(2) : 1.0) / 2;
            for (var x = 0; x < 8; x++) table[u * 8 + x] = scale * Math.Cos((2 * x + 1) * u * Math.PI / 16);
        }

        return table;
    }

    private static void WriteMarker(Stream output, byte marker)
    {
        output.WriteByte(0xFF);
        output.WriteByte(marker);
    }

    private static void WriteSegment(Stream output, byte marker, byte[] payload)
    {
        WriteMarker(output, marker);
        var length = payload.Length + 2;
        output.WriteByte((byte)(length >> 8));
        output.WriteByte((byte)length);
        output.Write(payload, 0, payload.Length);
    }

    /// <summary>Huffman bit packer with byte stuffing; collects output bytes and writes them in blocks.</summary>
    private sealed class BitWriter
    {
        private readonly Stream _output;
        private readonly byte[] _bytes = new byte[4096];
        private int _byteCount;
        private int _buffer;
        private int _count;

        public BitWriter(Stream output) => _output = output;

        public void Write((int Code, int Length) entry)
        {
            if (entry.Length == 0) throw new InvalidOperationException("Missing Huffman code.");
            Write(entry.Code, entry.Length);
        }

        public void Write(int value, int length)
        {
            // Up to 16 bits per call, so with at most 7 pending bits the buffer never exceeds 23 bits.
            _buffer = (_buffer << length) | (value & ((1 << length) - 1));
            _count += length;
            while (_count >= 8)
            {
                _count -= 8;
                var b = (byte)(_buffer >> _count);
                Emit(b);
                if (b == 0xFF) Emit(0x00); // byte stuffing
            }

            _buffer &= (1 << _count) - 1;
        }

        /// <summary>Pads the last byte with 1-bits and writes out everything collected.</summary>
        public void Flush()
        {
            if (_count > 0) Write((1 << (8 - _count)) - 1, 8 - _count);
            _output.Write(_bytes, 0, _byteCount);
            _byteCount = 0;
        }

        private void Emit(byte b)
        {
            if (_byteCount == _bytes.Length)
            {
                _output.Write(_bytes, 0, _byteCount);
                _byteCount = 0;
            }

            _bytes[_byteCount++] = b;
        }
    }
}
