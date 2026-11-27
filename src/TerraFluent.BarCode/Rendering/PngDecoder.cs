using System.IO.Compression;

namespace TerraFluent.BarCode.Rendering;

/// <summary>Decodes PNG logos to RGBA. Supports every colour type at bit depths 1-16; not interlaced images.</summary>
internal static class PngDecoder
{
    internal sealed class Image
    {
        public Image(int width, int height, byte[] pixels)
        {
            Width = width;
            Height = height;
            Pixels = pixels;
        }

        public int Width { get; }
        public int Height { get; }
        public byte[] Pixels { get; }
    }

    public static Image Decode(byte[] data)
    {
        if (data.Length < 8 || data[0] != 137 || data[1] != 80 || data[2] != 78 || data[3] != 71)
        {
            throw new NotSupportedException("PNG and JPEG output need a PNG logo. SVG output also accepts JPEG, GIF, WebP and SVG logos.");
        }

        int width = 0, height = 0, bitDepth = 0, colorType = 0;
        byte[]? palette = null;
        byte[]? transparency = null;
        using var idat = new MemoryStream();
        var pos = 8;
        while (pos + 8 <= data.Length)
        {
            var length = ReadInt(data, pos);
            var type = System.Text.Encoding.ASCII.GetString(data, pos + 4, 4);
            var body = pos + 8;
            if (length < 0 || body + length > data.Length) throw new InvalidDataException("Truncated PNG logo.");
            switch (type)
            {
                case "IHDR":
                    width = ReadInt(data, body);
                    height = ReadInt(data, body + 4);
                    bitDepth = data[body + 8];
                    colorType = data[body + 9];
                    if (data[body + 12] != 0) throw new NotSupportedException("Interlaced PNG logos are not supported; save the logo without interlacing.");
                    break;
                case "PLTE":
                    palette = new byte[length];
                    Array.Copy(data, body, palette, 0, length);
                    break;
                case "tRNS":
                    transparency = new byte[length];
                    Array.Copy(data, body, transparency, 0, length);
                    break;
                case "IDAT":
                    idat.Write(data, body, length);
                    break;
            }

            if (type == "IEND") break;
            pos = body + length + 4;
        }

        if (width <= 0 || height <= 0) throw new InvalidDataException("PNG logo has no valid header.");
        var channels = colorType switch { 0 => 1, 2 => 3, 3 => 1, 4 => 2, 6 => 4, _ => throw new InvalidDataException("Unknown PNG colour type.") };
        var bitsPerPixel = channels * bitDepth;
        var stride = (width * bitsPerPixel + 7) / 8;
        var bpp = Math.Max(1, bitsPerPixel / 8);

        var raw = Inflate(idat.ToArray(), checked((stride + 1) * height));
        var current = new byte[stride];
        var previous = new byte[stride];
        var output = new byte[checked(width * height * 4)];
        for (var y = 0; y < height; y++)
        {
            var filter = raw[y * (stride + 1)];
            Array.Copy(raw, y * (stride + 1) + 1, current, 0, stride);
            Unfilter(filter, current, previous, bpp);
            for (var x = 0; x < width; x++) WritePixel(current, x, colorType, bitDepth, palette, transparency, output, (y * width + x) * 4);
            (previous, current) = (current, previous);
        }

        return new Image(width, height, output);
    }

    private static byte[] Inflate(byte[] zlib, int expected)
    {
        using var input = new MemoryStream(zlib, 2, zlib.Length - 2); // skip the zlib header
        using var deflate = new DeflateStream(input, CompressionMode.Decompress);
        var result = new byte[expected];
        var read = 0;
        while (read < expected)
        {
            var n = deflate.Read(result, read, expected - read);
            if (n == 0) throw new InvalidDataException("PNG logo data is truncated.");
            read += n;
        }

        return result;
    }

    private static void Unfilter(byte filter, byte[] cur, byte[] prev, int bpp)
    {
        for (var i = 0; i < cur.Length; i++)
        {
            int a = i >= bpp ? cur[i - bpp] : 0, b = prev[i], c = i >= bpp ? prev[i - bpp] : 0;
            cur[i] = filter switch
            {
                0 => cur[i],
                1 => (byte)(cur[i] + a),
                2 => (byte)(cur[i] + b),
                3 => (byte)(cur[i] + (a + b) / 2),
                4 => (byte)(cur[i] + Paeth(a, b, c)),
                _ => throw new InvalidDataException("Unknown PNG filter."),
            };
        }
    }

    private static int Paeth(int a, int b, int c)
    {
        int p = a + b - c, pa = Math.Abs(p - a), pb = Math.Abs(p - b), pc = Math.Abs(p - c);
        return pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
    }

    private static int Sample(byte[] row, int index, int bitDepth)
    {
        switch (bitDepth)
        {
            case 16:
                return row[index * 2];
            case 8:
                return row[index];
            default:
                var bitPos = index * bitDepth;
                return (row[bitPos >> 3] >> (8 - bitDepth - (bitPos & 7))) & ((1 << bitDepth) - 1);
        }
    }

    private static void WritePixel(byte[] row, int x, int colorType, int bitDepth, byte[]? palette, byte[]? trns, byte[] output, int o)
    {
        int ScaleToByte(int v) => bitDepth >= 8 ? v : v * 255 / ((1 << bitDepth) - 1);
        switch (colorType)
        {
            case 0:
            {
                var raw = Sample(row, x, bitDepth);
                var v = (byte)ScaleToByte(raw);
                output[o] = output[o + 1] = output[o + 2] = v;
                var transparent = trns is { Length: >= 2 } && (bitDepth == 16 ? row[x * 2] == trns[0] && row[x * 2 + 1] == trns[1] : raw == trns[1]);
                output[o + 3] = transparent ? (byte)0 : (byte)255;
                break;
            }
            case 2:
                output[o] = (byte)Sample(row, x * 3, bitDepth);
                output[o + 1] = (byte)Sample(row, x * 3 + 1, bitDepth);
                output[o + 2] = (byte)Sample(row, x * 3 + 2, bitDepth);
                output[o + 3] = 255;
                break;
            case 3:
            {
                var index = Sample(row, x, bitDepth);
                if (palette is null || index * 3 + 2 >= palette.Length) throw new InvalidDataException("PNG logo palette is missing or too short.");
                output[o] = palette[index * 3];
                output[o + 1] = palette[index * 3 + 1];
                output[o + 2] = palette[index * 3 + 2];
                output[o + 3] = trns is not null && index < trns.Length ? trns[index] : (byte)255;
                break;
            }
            case 4:
                output[o] = output[o + 1] = output[o + 2] = (byte)Sample(row, x * 2, bitDepth);
                output[o + 3] = (byte)Sample(row, x * 2 + 1, bitDepth);
                break;
            default:
                output[o] = (byte)Sample(row, x * 4, bitDepth);
                output[o + 1] = (byte)Sample(row, x * 4 + 1, bitDepth);
                output[o + 2] = (byte)Sample(row, x * 4 + 2, bitDepth);
                output[o + 3] = (byte)Sample(row, x * 4 + 3, bitDepth);
                break;
        }
    }

    private static int ReadInt(byte[] b, int i) => b[i] << 24 | b[i + 1] << 16 | b[i + 2] << 8 | b[i + 3];
}
