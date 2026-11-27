using System.IO.Compression;

namespace TerraFluent.BarCode.Rendering;

/// <summary>
/// Reads a PNG file (any colour type and bit depth, non-interlaced) into an
/// RGBA buffer. Used for logos, so the library stays free of image dependencies.
/// </summary>
internal static class PngDecoder
{
    private const int MaxPixels = 4096 * 4096;
    private static readonly byte[] Signature = { 137, 80, 78, 71, 13, 10, 26, 10 };

    public static RgbaImage Decode(byte[] data, string paramName)
    {
        if (data is null) throw new ArgumentNullException(paramName);
        if (data.Length < 8 || !data.Take(8).SequenceEqual(Signature)) throw new ArgumentException("The image is not a PNG file.", paramName);

        int width = 0, height = 0, bitDepth = 0, colourType = -1;
        byte[]? palette = null, transparency = null;
        using var idat = new MemoryStream();
        var pos = 8;
        var sawEnd = false;
        while ((long)pos + 8 <= data.Length)
        {
            var length = ReadInt32(data, pos);
            var type = System.Text.Encoding.ASCII.GetString(data, pos + 4, 4);
            pos += 8;
            if (length < 0 || (long)pos + length + 4 > data.Length) throw new ArgumentException("The PNG file is truncated.", paramName);

            switch (type)
            {
                case "IHDR":
                    if (length < 13) throw new ArgumentException("The PNG header is invalid.", paramName);
                    width = ReadInt32(data, pos);
                    height = ReadInt32(data, pos + 4);
                    bitDepth = data[pos + 8];
                    colourType = data[pos + 9];
                    if (data[pos + 12] != 0) throw new ArgumentException("Interlaced PNG images are not supported; save the image without interlacing.", paramName);
                    break;
                case "PLTE":
                    palette = new byte[length];
                    Buffer.BlockCopy(data, pos, palette, 0, length);
                    break;
                case "tRNS":
                    transparency = new byte[length];
                    Buffer.BlockCopy(data, pos, transparency, 0, length);
                    break;
                case "IDAT":
                    idat.Write(data, pos, length);
                    break;
                case "IEND":
                    sawEnd = true;
                    break;
            }

            pos += length + 4; // data + CRC
            if (sawEnd) break;
        }

        var channels = colourType switch { 0 => 1, 2 => 3, 3 => 1, 4 => 2, 6 => 4, _ => 0 };
        var validDepth = colourType switch
        {
            0 => bitDepth is 1 or 2 or 4 or 8 or 16,
            3 => bitDepth is 1 or 2 or 4 or 8,
            2 or 4 or 6 => bitDepth is 8 or 16,
            _ => false,
        };
        if (width <= 0 || height <= 0 || channels == 0 || !validDepth) throw new ArgumentException("The PNG header is invalid or uses an unsupported format.", paramName);
        if ((long)width * height > MaxPixels) throw new ArgumentException("The PNG image is too large (over 4096x4096 pixels).", paramName);
        if (colourType == 3 && palette is null) throw new ArgumentException("The PNG image has no palette.", paramName);
        if (idat.Length < 2) throw new ArgumentException("The PNG image has no pixel data.", paramName);

        var bitsPerPixel = channels * bitDepth;
        var bytesPerPixel = Math.Max(1, bitsPerPixel / 8);
        var stride = (int)(((long)width * bitsPerPixel + 7) / 8);
        var raw = Inflate(idat.ToArray(), (stride + 1) * height, paramName);

        var rgba = new byte[width * height * 4];
        var previous = new byte[stride];
        var current = new byte[stride];
        for (var y = 0; y < height; y++)
        {
            var offset = y * (stride + 1);
            var filter = raw[offset];
            Buffer.BlockCopy(raw, offset + 1, current, 0, stride);
            Unfilter(filter, current, previous, bytesPerPixel, paramName);
            for (var x = 0; x < width; x++) WritePixel(current, x, colourType, bitDepth, palette, transparency, rgba, (y * width + x) * 4);
            (previous, current) = (current, previous);
        }

        return new RgbaImage(width, height, rgba);
    }

    private static byte[] Inflate(byte[] zlib, int expected, string paramName)
    {
        try
        {
            using var input = new MemoryStream(zlib, 2, zlib.Length - 2); // skip the zlib header
            using var deflate = new DeflateStream(input, CompressionMode.Decompress);
            var result = new byte[expected];
            var read = 0;
            while (read < expected)
            {
                var n = deflate.Read(result, read, expected - read);
                if (n == 0) break;
                read += n;
            }

            if (read < expected) throw new ArgumentException("The PNG pixel data is truncated.", paramName);
            return result;
        }
        catch (InvalidDataException ex)
        {
            throw new ArgumentException("The PNG pixel data is corrupt.", paramName, ex);
        }
    }

    private static void Unfilter(byte filter, byte[] row, byte[] above, int bpp, string paramName)
    {
        switch (filter)
        {
            case 0:
                return;
            case 1:
                for (var i = bpp; i < row.Length; i++) row[i] += row[i - bpp];
                return;
            case 2:
                for (var i = 0; i < row.Length; i++) row[i] += above[i];
                return;
            case 3:
                for (var i = 0; i < row.Length; i++) row[i] += (byte)(((i >= bpp ? row[i - bpp] : 0) + above[i]) >> 1);
                return;
            case 4:
                for (var i = 0; i < row.Length; i++)
                {
                    int a = i >= bpp ? row[i - bpp] : 0, b = above[i], c = i >= bpp ? above[i - bpp] : 0;
                    int p = a + b - c, pa = Math.Abs(p - a), pb = Math.Abs(p - b), pc = Math.Abs(p - c);
                    row[i] += (byte)(pa <= pb && pa <= pc ? a : pb <= pc ? b : c);
                }

                return;
            default:
                throw new ArgumentException("The PNG pixel data uses an unknown filter.", paramName);
        }
    }

    private static void WritePixel(byte[] row, int x, int colourType, int bitDepth, byte[]? palette, byte[]? trns, byte[] rgba, int o)
    {
        int Sample(int index)
        {
            if (bitDepth == 8) return row[index];
            if (bitDepth == 16) return (row[index * 2] << 8) | row[index * 2 + 1];
            var bit = index * bitDepth;
            return (row[bit >> 3] >> (8 - bitDepth - (bit & 7))) & ((1 << bitDepth) - 1);
        }

        var max = (1 << bitDepth) - 1;
        byte To8(int value) => (byte)(bitDepth == 16 ? value >> 8 : value * 255 / max);

        switch (colourType)
        {
            case 0:
            {
                var v = Sample(x);
                rgba[o] = rgba[o + 1] = rgba[o + 2] = To8(v);
                rgba[o + 3] = trns is { Length: >= 2 } && v == ((trns[0] << 8) | trns[1]) ? (byte)0 : (byte)255;
                break;
            }

            case 2:
            {
                int r = Sample(x * 3), g = Sample(x * 3 + 1), b = Sample(x * 3 + 2);
                rgba[o] = To8(r);
                rgba[o + 1] = To8(g);
                rgba[o + 2] = To8(b);
                var keyed = trns is { Length: >= 6 } && r == ((trns[0] << 8) | trns[1]) && g == ((trns[2] << 8) | trns[3]) && b == ((trns[4] << 8) | trns[5]);
                rgba[o + 3] = keyed ? (byte)0 : (byte)255;
                break;
            }

            case 3:
            {
                var index = Sample(x);
                if (index * 3 + 2 < palette!.Length)
                {
                    rgba[o] = palette[index * 3];
                    rgba[o + 1] = palette[index * 3 + 1];
                    rgba[o + 2] = palette[index * 3 + 2];
                }

                rgba[o + 3] = trns is not null && index < trns.Length ? trns[index] : (byte)255;
                break;
            }

            case 4:
                rgba[o] = rgba[o + 1] = rgba[o + 2] = To8(Sample(x * 2));
                rgba[o + 3] = To8(Sample(x * 2 + 1));
                break;

            default:
                rgba[o] = To8(Sample(x * 4));
                rgba[o + 1] = To8(Sample(x * 4 + 1));
                rgba[o + 2] = To8(Sample(x * 4 + 2));
                rgba[o + 3] = To8(Sample(x * 4 + 3));
                break;
        }
    }

    private static int ReadInt32(byte[] b, int offset) => (b[offset] << 24) | (b[offset + 1] << 16) | (b[offset + 2] << 8) | b[offset + 3];
}
