namespace TerraFluent.Barcode.Encoding;

/// <summary>
/// Reed-Solomon error correction over GF(2^m), shared by QR Code (roots a^0..a^(n-1)),
/// Data Matrix and Aztec (4- to 12-bit words) and MaxiCode (6-bit words), all with
/// roots a^1..a^n.
/// </summary>
/// <remarks>
/// Each generator polynomial is built once per degree and kept as logarithms, so a
/// check-word step is one table lookup per coefficient. Zero has a sentinel
/// logarithm that lands in a zero-filled part of the exponent table, so no step
/// needs a branch for a zero coefficient.
/// </remarks>
internal sealed class GaloisField
{
    private readonly int[] _exp;
    private readonly int[] _log;
    private readonly int _generatorBase;
    private readonly int[]?[] _generators;

    public GaloisField(int primitive, int size, int generatorBase = 1)
    {
        Size = size;
        _generatorBase = generatorBase;
        ZeroLog = 2 * (size - 1);

        // Two periods of a^i cover the sum of any two logarithms (at most 2 * (size - 2));
        // past them, the table is zero for every sum that includes ZeroLog.
        _exp = new int[2 * ZeroLog + 1];
        _log = new int[size];
        var x = 1;
        for (var i = 0; i < size - 1; i++)
        {
            _exp[i] = x;
            _log[x] = i;
            x <<= 1;
            if (x >= size) x ^= primitive;
        }

        for (var i = size - 1; i < ZeroLog; i++) _exp[i] = _exp[i - (size - 1)];
        _log[0] = ZeroLog;
        _generators = new int[]?[size];
    }

    public static readonly GaloisField Aztec4 = new(0x13, 16);
    public static readonly GaloisField Aztec6 = new(0x43, 64);
    public static readonly GaloisField Aztec8 = new(0x12D, 256);
    public static readonly GaloisField Aztec10 = new(0x409, 1024);
    public static readonly GaloisField Aztec12 = new(0x1069, 4096);

    /// <summary>QR Code: polynomial x^8 + x^4 + x^3 + x^2 + 1, generator roots a^0..a^(n-1).</summary>
    public static readonly GaloisField QrCode = new(0x11D, 256, generatorBase: 0);

    /// <summary>Data Matrix uses the same field and roots as 8-bit Aztec words.</summary>
    public static GaloisField DataMatrix => Aztec8;

    /// <summary>MaxiCode uses the same 6-bit field as Aztec.</summary>
    public static GaloisField MaxiCode => Aztec6;

    public int Size { get; }

    /// <summary>The logarithm the tables give zero: adding any logarithm to it reads a zero.</summary>
    private int ZeroLog { get; }

    private int Multiply(int a, int b) => _exp[_log[a] + _log[b]];

    /// <summary>
    /// Logarithms of the coefficients of prod (x + a^i) over the field's
    /// <paramref name="degree"/> roots, highest power first, leading 1 omitted.
    /// Built once per degree; a race only builds an identical array twice.
    /// </summary>
    internal int[] GeneratorLogs(int degree)
    {
        var cached = Volatile.Read(ref _generators[degree]);
        if (cached is not null) return cached;

        var g = new int[degree + 1];
        g[0] = 1;
        for (var i = 1; i <= degree; i++)
        {
            var root = _exp[_generatorBase + i - 1];
            for (var j = i; j >= 1; j--) g[j] ^= Multiply(g[j - 1], root);
        }

        var logs = new int[degree];
        for (var i = 0; i < degree; i++) logs[i] = _log[g[i + 1]];
        Volatile.Write(ref _generators[degree], logs);
        return logs;
    }

    /// <summary>
    /// Computes the <c>ecc.Length</c> check words for <paramref name="dataCount"/> words of
    /// <paramref name="words"/>, read <paramref name="stride"/> apart from <paramref name="start"/>,
    /// into <paramref name="ecc"/> (highest power first, as they follow the data).
    /// </summary>
    public void Remainder(int[] words, int start, int dataCount, int stride, int[] ecc)
    {
        var n = ecc.Length;
        if (n == 0) return;
        var generator = GeneratorLogs(n);
        var exp = _exp;
        var log = _log;
        Array.Clear(ecc, 0, n);
        for (int i = 0, at = start; i < dataCount; i++, at += stride)
        {
            // Shift the remainder up by one word and add factor * generator; a zero factor only shifts.
            var factor = log[words[at] ^ ecc[0]];
            for (var j = 0; j < n - 1; j++) ecc[j] = ecc[j + 1] ^ exp[factor + generator[j]];
            ecc[n - 1] = exp[factor + generator[n - 1]];
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
        var ecc = new int[eccCount];
        Remainder(words, start, dataCount, stride, ecc);
        for (var i = 0; i < eccCount; i++) words[start + (dataCount + i) * stride] = ecc[i];
    }
}
