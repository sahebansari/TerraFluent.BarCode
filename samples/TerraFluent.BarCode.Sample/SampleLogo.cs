using System.IO.Compression;

namespace TerraFluent.BarCode.Sample;

/// <summary>
/// Draws a small round "TF" badge as a PNG so the sample needs no image file.
/// In your own code, pass your logo's bytes or path to <c>Style(s =&gt; s.Logo(...))</c>.
/// </summary>
internal static class SampleLogo
{
    public static byte[] Create(int size = 128)
    {
        var rgba = new byte[size * size * 4];
        double c = (size - 1) / 2.0, r = size / 2.0 - 2;
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var d = Math.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                if (d > r) continue;
                var i = (y * size + x) * 4;
                double u = x / (double)size, v = y / (double)size;
                // White "T" and "F" on a pink disc.
                var t = (v is > 0.28 and < 0.38 && u is > 0.18 and < 0.50) || (u is > 0.29 and < 0.39 && v is > 0.28 and < 0.74);
                var f = (u is > 0.56 and < 0.66 && v is > 0.28 and < 0.74) || (v is > 0.28 and < 0.38 && u is > 0.56 and < 0.84) || (v is > 0.46 and < 0.55 && u is > 0.56 and < 0.78);
                var white = t || f;
                rgba[i] = white ? (byte)255 : (byte)233;
                rgba[i + 1] = white ? (byte)255 : (byte)30;
                rgba[i + 2] = white ? (byte)255 : (byte)99;
                rgba[i + 3] = (byte)Math.Round(255 * Math.Min(1, r - d + 0.5));
            }
        }

        return EncodePng(size, rgba);
    }

    private static byte[] EncodePng(int size, byte[] rgba)
    {
        var raw = new byte[size * (size * 4 + 1)];
        for (var y = 0; y < size; y++) Buffer.BlockCopy(rgba, y * size * 4, raw, y * (size * 4 + 1) + 1, size * 4);
        using var zlib = new MemoryStream();
        using (var z = new ZLibStream(zlib, CompressionLevel.Optimal, leaveOpen: true)) z.Write(raw);

        using var png = new MemoryStream();
        png.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        var ihdr = new byte[13];
        WriteInt(ihdr, 0, size);
        WriteInt(ihdr, 4, size);
        ihdr[8] = 8;
        ihdr[9] = 6;
        Chunk(png, "IHDR", ihdr);
        Chunk(png, "IDAT", zlib.ToArray());
        Chunk(png, "IEND", Array.Empty<byte>());
        return png.ToArray();
    }

    private static void Chunk(Stream s, string type, byte[] data)
    {
        var head = new byte[8];
        WriteInt(head, 0, data.Length);
        System.Text.Encoding.ASCII.GetBytes(type, 0, 4, head, 4);
        s.Write(head);
        s.Write(data);
        var crc = Crc32(head.AsSpan(4), data);
        var tail = new byte[4];
        WriteInt(tail, 0, (int)crc);
        s.Write(tail);
    }

    private static uint Crc32(ReadOnlySpan<byte> type, byte[] data)
    {
        var crc = 0xFFFFFFFFu;
        void Add(byte b)
        {
            crc ^= b;
            for (var k = 0; k < 8; k++) crc = (crc & 1) != 0 ? 0xEDB88320u ^ (crc >> 1) : crc >> 1;
        }

        foreach (var b in type) Add(b);
        foreach (var b in data) Add(b);
        return crc ^ 0xFFFFFFFFu;
    }

    private static void WriteInt(byte[] b, int o, int v)
    {
        b[o] = (byte)(v >> 24);
        b[o + 1] = (byte)(v >> 16);
        b[o + 2] = (byte)(v >> 8);
        b[o + 3] = (byte)v;
    }
}
