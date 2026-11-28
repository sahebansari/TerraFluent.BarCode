namespace TerraFluent.Barcode.Encoding;

/// <summary>
/// Reed-Solomon error correction over GF(2^m) with generator roots a^1..a^n,
/// as Aztec (4- to 12-bit words) and MaxiCode (6-bit words) use.
/// </summary>
internal sealed class GaloisField
{
    private readonly int[] _exp;
    private readonly int[] _log;
    private readonly Dictionary<int, int[]> _generators = new();

    public GaloisField(int primitive, int size)
    {
        Size = size;
        _exp = new int[2 * size];
        _log = new int[size];
        var x = 1;
        for (var i = 0; i < size - 1; i++)
        {
            _exp[i] = x;
            _log[x] = i;
            x <<= 1;
            if (x >= size) x ^= primitive;
        }

        for (var i = size - 1; i < _exp.Length; i++) _exp[i] = _exp[i - (size - 1)];
    }

    public static readonly GaloisField Aztec4 = new(0x13, 16);
    public static readonly GaloisField Aztec6 = new(0x43, 64);
    public static readonly GaloisField Aztec8 = new(0x12D, 256);
    public static readonly GaloisField Aztec10 = new(0x409, 1024);
    public static readonly GaloisField Aztec12 = new(0x1069, 4096);

    /// <summary>MaxiCode uses the same 6-bit field as Aztec.</summary>
    public static GaloisField MaxiCode => Aztec6;

    public int Size { get; }

    private int Multiply(int a, int b) => a == 0 || b == 0 ? 0 : _exp[_log[a] + _log[b]];

    /// <summary>Coefficients of prod (x + a^i), i = 1..degree, highest power first, leading 1 omitted.</summary>
    private int[] Generator(int degree)
    {
        lock (_generators)
        {
            if (_generators.TryGetValue(degree, out var cached)) return cached;
            var g = new int[degree + 1];
            g[0] = 1;
            for (var i = 1; i <= degree; i++)
            {
                var root = _exp[i];
                for (var j = i; j >= 1; j--) g[j] ^= Multiply(g[j - 1], root);
            }

            var result = new int[degree];
            Array.Copy(g, 1, result, 0, degree);
            _generators[degree] = result;
            return result;
        }
    }

    /// <summary>
    /// Writes <paramref name="eccCount"/> check words for <paramref name="dataCount"/>
    /// words of <paramref name="words"/>, read and written <paramref name="stride"/>
    /// apart from <paramref name="start"/>, into the positions after the data.
    /// </summary>
    public void Encode(int[] words, int start, int dataCount, int eccCount, int stride = 1)
    {
        if (eccCount == 0) return;
        var generator = Generator(eccCount);
        var ecc = new int[eccCount];
        for (var i = 0; i < dataCount; i++)
        {
            var factor = words[start + i * stride] ^ ecc[0];
            for (var j = 0; j < eccCount - 1; j++) ecc[j] = ecc[j + 1] ^ Multiply(factor, generator[j]);
            ecc[eccCount - 1] = Multiply(factor, generator[eccCount - 1]);
        }

        for (var i = 0; i < eccCount; i++) words[start + (dataCount + i) * stride] = ecc[i];
    }
}
