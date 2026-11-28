using TerraFluent.Barcode.Encoding;
using TerraFluent.Barcode.Rendering;

namespace TerraFluent.Barcode.Tests;

/// <summary>
/// Checks the table-driven and vectorised check-word code against plain,
/// textbook versions: Adler-32, Reed-Solomon over each field, PDF417's GF(929)
/// and the GS1 modulo 10 check digit.
/// </summary>
public class ChecksumTests
{
    [Fact]
    public void Adler32_matches_the_known_value()
    {
        var data = System.Text.Encoding.ASCII.GetBytes("Wikipedia");
        Assert.Equal(0x11E60398u, PngEncoder.Adler32(1, data, 0, data.Length));
    }

    [Fact]
    public void Adler32_matches_the_bytewise_sum_for_every_length_and_offset()
    {
        var data = new byte[200];
        new Random(5).NextBytes(data);
        for (var offset = 0; offset < 17; offset++)
        {
            for (var count = 0; offset + count <= data.Length; count++)
            {
                Assert.Equal(ReferenceAdler32(1, data, offset, count), PngEncoder.Adler32(1, data, offset, count));
            }
        }
    }

    [Theory]
    [InlineData(0x00)]
    [InlineData(0xFF)] // largest sums: checks that nothing overflows between reductions
    [InlineData(-1)]   // random bytes
    public void Adler32_matches_the_bytewise_sum_over_long_input_in_uneven_pieces(int fill)
    {
        var data = new byte[300_000];
        if (fill < 0) new Random(6).NextBytes(data);
        else
        {
            for (var i = 0; i < data.Length; i++) data[i] = (byte)fill;
        }

        Assert.Equal(ReferenceAdler32(1, data, 0, data.Length), PngEncoder.Adler32(1, data, 0, data.Length));

        // As the PNG writer calls it: one row at a time, carrying the running value.
        var random = new Random(7);
        uint expected = 1, actual = 1;
        for (var offset = 0; offset < data.Length;)
        {
            var count = Math.Min(data.Length - offset, random.Next(1, 20_000));
            expected = ReferenceAdler32(expected, data, offset, count);
            actual = PngEncoder.Adler32(actual, data, offset, count);
            Assert.Equal(expected, actual);
            offset += count;
        }
    }

    public static TheoryData<string> Fields => new() { "QrCode", "Aztec4", "Aztec6", "Aztec8", "Aztec10", "Aztec12" };

    [Theory]
    [MemberData(nameof(Fields))]
    public void Reed_solomon_matches_polynomial_long_division(string name)
    {
        var (field, primitive, generatorBase) = FieldByName(name);
        var random = new Random(name.Length * 31 + name[name.Length - 1]);
        foreach (var eccCount in new[] { 1, 2, 5, 7, 10, 30, 68 })
        {
            if (eccCount >= field.Size - 1) continue;
            for (var trial = 0; trial < 5; trial++)
            {
                var dataCount = Math.Min(field.Size - 1 - eccCount, random.Next(1, 120));
                var data = new int[dataCount];
                for (var i = 0; i < dataCount; i++) data[i] = trial == 0 ? 0 : random.Next(field.Size);

                var expected = ReferenceRemainder(data, eccCount, field.Size, primitive, generatorBase);
                var actual = new int[eccCount];
                field.Remainder(data, 0, dataCount, 1, actual);
                Assert.Equal(expected, actual);
            }
        }
    }

    [Fact]
    public void Reed_solomon_reads_strided_words_and_reuses_the_buffer()
    {
        var field = GaloisField.DataMatrix;
        var random = new Random(11);
        var words = new int[3 * 40];
        for (var i = 0; i < words.Length; i++) words[i] = random.Next(256);

        var ecc = new int[12];
        for (var block = 0; block < 3; block++)
        {
            var data = new int[40];
            for (var i = 0; i < 40; i++) data[i] = words[block + 3 * i];
            field.Remainder(words, block, 40, 3, ecc);
            Assert.Equal(ReferenceRemainder(data, 12, 256, 0x12D, 1), ecc);
        }
    }

    [Fact]
    public void Qr_error_correction_matches_the_published_example()
    {
        // "HELLO WORLD", version 1-M: data codewords and their 10 check bytes.
        var data = new[] { 32, 91, 11, 120, 209, 114, 220, 77, 67, 64, 236, 17, 236, 17, 236, 17 };
        var ecc = new int[10];
        GaloisField.QrCode.Remainder(data, 0, data.Length, 1, ecc);
        Assert.Equal(new[] { 196, 35, 39, 119, 235, 215, 231, 226, 93, 23 }, ecc);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(8)]
    public void Pdf417_error_correction_matches_the_modular_arithmetic(int level)
    {
        var random = new Random(level + 20);
        var count = 1 << (level + 1);
        var data = new List<int>();
        for (var i = 0; i < 928 - count; i++) data.Add(random.Next(929));

        Assert.Equal(ReferencePdf417(data, level), Pdf417Encoder.ErrorCorrection(data, level));
        Assert.Same(Pdf417Encoder.GeneratorFactors(level), Pdf417Encoder.GeneratorFactors(level));
    }

    [Fact]
    public void Gs1_check_digit_matches_the_weighted_sum_for_every_length()
    {
        Assert.Equal('1', EanUpcEncoder.CheckDigit("400638133393"));
        Assert.Equal('2', EanUpcEncoder.CheckDigit("036000291452", 11));
        var random = new Random(9);
        for (var length = 0; length <= 18; length++)
        {
            var digits = new char[length + 2];
            for (var i = 0; i < digits.Length; i++) digits[i] = (char)('0' + random.Next(10));
            var value = new string(digits);
            var sum = 0;
            for (var i = 0; i < length; i++) sum += (value[length - 1 - i] - '0') * (i % 2 == 0 ? 3 : 1);
            var expected = (char)('0' + (10 - sum % 10) % 10);
            Assert.Equal(expected, EanUpcEncoder.CheckDigit(value, length));
            Assert.Equal(expected, EanUpcEncoder.CheckDigit(value.Substring(0, length)));
        }
    }

    private static uint ReferenceAdler32(uint adler, byte[] data, int offset, int count)
    {
        uint a = adler & 0xFFFF, b = adler >> 16;
        for (var i = offset; i < offset + count; i++)
        {
            a = (a + data[i]) % 65521;
            b = (b + a) % 65521;
        }

        return (b << 16) | a;
    }

    private static (GaloisField Field, int Primitive, int GeneratorBase) FieldByName(string name) => name switch
    {
        "QrCode" => (GaloisField.QrCode, 0x11D, 0),
        "Aztec4" => (GaloisField.Aztec4, 0x13, 1),
        "Aztec6" => (GaloisField.Aztec6, 0x43, 1),
        "Aztec8" => (GaloisField.Aztec8, 0x12D, 1),
        "Aztec10" => (GaloisField.Aztec10, 0x409, 1),
        _ => (GaloisField.Aztec12, 0x1069, 1),
    };

    /// <summary>Carry-less multiplication reduced by the field polynomial: no tables.</summary>
    private static int Multiply(int a, int b, int size, int primitive)
    {
        var result = 0;
        while (b != 0)
        {
            if ((b & 1) != 0) result ^= a;
            b >>= 1;
            a <<= 1;
            if (a >= size) a ^= primitive;
        }

        return result;
    }

    /// <summary>data(x) * x^n mod g(x), with g(x) = prod (x - a^i) over n roots from a^generatorBase.</summary>
    private static int[] ReferenceRemainder(int[] data, int n, int size, int primitive, int generatorBase)
    {
        var generator = new int[n + 1]; // generator[i] is the coefficient of x^(n - i)
        generator[0] = 1;
        var root = 1;
        for (var i = 0; i < generatorBase; i++) root = Multiply(root, 2, size, primitive);
        for (var i = 1; i <= n; i++)
        {
            for (var j = i; j >= 1; j--) generator[j] ^= Multiply(generator[j - 1], root, size, primitive);
            root = Multiply(root, 2, size, primitive);
        }

        var dividend = new int[data.Length + n];
        Array.Copy(data, dividend, data.Length);
        for (var i = 0; i < data.Length; i++)
        {
            var factor = dividend[i];
            if (factor == 0) continue;
            for (var j = 0; j <= n; j++) dividend[i + j] ^= Multiply(generator[j], factor, size, primitive);
        }

        var remainder = new int[n];
        Array.Copy(dividend, data.Length, remainder, 0, n);
        return remainder;
    }

    /// <summary>The ISO/IEC 15438 procedure written with long arithmetic and two modulo steps, as before.</summary>
    private static int[] ReferencePdf417(List<int> data, int level)
    {
        var k = 1 << (level + 1);
        var g = new long[k + 1];
        g[0] = 1;
        long root = 1;
        for (var i = 1; i <= k; i++)
        {
            root = root * 3 % 929;
            for (var j = i; j >= 0; j--)
            {
                var shifted = j > 0 ? g[j - 1] : 0;
                g[j] = ((shifted - root * g[j]) % 929 + 929) % 929;
            }
        }

        var ecc = new long[k];
        foreach (var word in data)
        {
            var t = (word + ecc[k - 1]) % 929;
            for (var x = k - 1; x >= 0; x--)
            {
                var previous = x > 0 ? ecc[x - 1] : 0;
                ecc[x] = (previous + 929 - t * g[x] % 929) % 929;
            }
        }

        var result = new int[k];
        for (var i = 0; i < k; i++)
        {
            var v = ecc[k - 1 - i];
            result[i] = v > 0 ? (int)(929 - v) : 0;
        }

        return result;
    }
}
