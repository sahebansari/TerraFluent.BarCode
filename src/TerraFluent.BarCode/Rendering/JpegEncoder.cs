namespace TerraFluent.BarCode.Rendering;

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

    private static readonly double[,] Cosine = BuildCosine();
    private static readonly (int Code, int Length)[] DcTable = BuildHuffman(DcBits, DcValues);
    private static readonly (int Code, int Length)[] AcTable = BuildHuffman(AcBits, AcValues);

    public static byte[] Encode(BarcodeMatrix matrix, RenderOptions options, int quality)
    {
        var (width, height) = options.ImageSize(matrix);
        var fg = options.Foreground;
        var bg = options.Background;
        var greyscale = fg.R == fg.G && fg.G == fg.B && bg.R == bg.G && bg.G == bg.B;

        // Per-component sample value for a dark and a light pixel.
        var darkSample = ToComponents(fg, greyscale);
        var lightSample = ToComponents(bg, greyscale);
        return Encode(width, height, greyscale, quality, (x, y, c) => options.IsDark(matrix, x, y) ? darkSample[c] : lightSample[c]);
    }

    /// <summary>Encodes a full-colour image (styled barcodes).</summary>
    public static byte[] Encode(RgbImage image, int quality)
    {
        var greyscale = image.IsGreyscale();
        var px = image.Pixels;
        return Encode(image.Width, image.Height, greyscale, quality, (x, y, c) =>
        {
            var i = (y * image.Width + x) * 3;
            double r = px[i], g = px[i + 1], b = px[i + 2];
            if (greyscale) return r;
            return c switch
            {
                0 => 0.299 * r + 0.587 * g + 0.114 * b,
                1 => -0.168736 * r - 0.331264 * g + 0.5 * b + 128,
                _ => 0.5 * r - 0.418688 * g - 0.081312 * b + 128,
            };
        });
    }

    // sample(x, y, c): component c (Y, Cb, Cr; or grey) of pixel (x, y), 0-255.
    private static byte[] Encode(int width, int height, bool greyscale, int quality, Func<int, int, int, double> sample)
    {
        // The frame header stores 16-bit dimensions.
        if (width > ushort.MaxValue || height > ushort.MaxValue)
        {
            throw new ArgumentException($"JPEG images are limited to {ushort.MaxValue} pixels per side; this one would be {width} x {height}.");
        }

        var componentCount = greyscale ? 1 : 3;
        var quant = new[] { Scale(LuminanceQuant, quality), Scale(ChrominanceQuant, quality) };

        using var output = new MemoryStream();
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
        var coefficients = new int[64];
        for (var by = 0; by < height; by += 8)
        {
            for (var bx = 0; bx < width; bx += 8)
            {
                for (var c = 0; c < componentCount; c++)
                {
                    for (var y = 0; y < 8; y++)
                    {
                        var py = Math.Min(by + y, height - 1);
                        for (var x = 0; x < 8; x++)
                        {
                            var px = Math.Min(bx + x, width - 1);
                            block[y * 8 + x] = sample(px, py, c) - 128.0;
                        }
                    }

                    ForwardDct(block, quant[c == 0 ? 0 : 1], coefficients);
                    EncodeBlock(bits, coefficients, ref previousDc[c]);
                }
            }
        }

        bits.Flush();
        WriteMarker(output, 0xD9); // EOI
        return output.ToArray();
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

    private static void ForwardDct(double[] block, int[] quant, int[] output)
    {
        for (var v = 0; v < 8; v++)
        {
            for (var u = 0; u < 8; u++)
            {
                var sum = 0.0;
                for (var y = 0; y < 8; y++)
                {
                    var cy = Cosine[v, y];
                    for (var x = 0; x < 8; x++) sum += block[y * 8 + x] * Cosine[u, x] * cy;
                }

                var cu = u == 0 ? 1 / Math.Sqrt(2) : 1.0;
                var cv = v == 0 ? 1 / Math.Sqrt(2) : 1.0;
                var index = v * 8 + u;
                output[index] = (int)Math.Round(0.25 * cu * cv * sum / quant[index]);
            }
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

    private static double[,] BuildCosine()
    {
        var table = new double[8, 8];
        for (var u = 0; u < 8; u++)
        {
            for (var x = 0; x < 8; x++) table[u, x] = Math.Cos((2 * x + 1) * u * Math.PI / 16);
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

    private sealed class BitWriter
    {
        private readonly Stream _output;
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
            for (var i = length - 1; i >= 0; i--)
            {
                _buffer = (_buffer << 1) | ((value >> i) & 1);
                if (++_count == 8) EmitByte();
            }
        }

        public void Flush()
        {
            while (_count != 0)
            {
                _buffer = (_buffer << 1) | 1; // pad with 1-bits
                if (++_count == 8) EmitByte();
            }
        }

        private void EmitByte()
        {
            var b = (byte)_buffer;
            _output.WriteByte(b);
            if (b == 0xFF) _output.WriteByte(0x00); // byte stuffing
            _buffer = 0;
            _count = 0;
        }
    }
}
