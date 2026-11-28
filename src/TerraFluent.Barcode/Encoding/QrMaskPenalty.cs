namespace TerraFluent.Barcode.Encoding;

/// <summary>
/// Scores QR mask candidates (ISO/IEC 18004 section 7.8.3, rules N1-N4) with
/// bit-parallel operations: each row and column is packed into three 64-bit
/// words (a version 40 symbol is 177 modules wide), so a whole line is
/// checked for runs and finder-like patterns with a few shifts and pop counts
/// instead of a branch per module.
/// </summary>
/// <remarks>
/// Bit <c>p</c> of a line is module <c>p</c> (column <c>p</c> of a row, or row
/// <c>p</c> of a column). <c>Shl</c> by <c>k</c> lines module
/// <c>p - k</c> up with module <c>p</c>.
/// </remarks>
internal sealed class QrMaskPenalty
{
    private const int Words = 3;
    private const int MaxSize = Words * 64;
    private const int Period = 12; // every mask pattern repeats every 12 modules in x and in y

    // Finder-like 1:1:3:1:1 patterns with four light modules after (A) or before (B); bit j is module p - j of a window ending at p.
    private const int FinderPatternA = 0b10111010000;
    private const int FinderPatternB = 0b00001011101;

    // [mask, phase, word]: mask bits of a row y with y % 12 == phase (over x), and of a column x with x % 12 == phase (over y).
    private static readonly ulong[] RowPatterns = BuildPatterns(columns: false);
    private static readonly ulong[] ColumnPatterns = BuildPatterns(columns: true);

    private readonly int _size;
    private readonly ulong[] _baseRows;
    private readonly ulong[] _baseColumns;
    private readonly ulong[] _dataRows;
    private readonly ulong[] _dataColumns;
    private readonly ulong[] _rows;
    private readonly ulong[] _columns;
    private readonly Line _all;
    private readonly Line _from1;
    private readonly Line _from4;
    private readonly Line _from10;

    /// <summary>Packs the unmasked <paramref name="modules"/> and the data-module layout of a symbol.</summary>
    public QrMaskPenalty(int size, bool[] modules, bool[] isFunction)
    {
        System.Diagnostics.Debug.Assert(size <= MaxSize, "QR symbols are at most 177 modules wide.");
        _size = size;
        _baseRows = new ulong[size * Words];
        _baseColumns = new ulong[size * Words];
        _dataRows = new ulong[size * Words];
        _dataColumns = new ulong[size * Words];
        _rows = new ulong[size * Words];
        _columns = new ulong[size * Words];
        _all = Line.Range(0, size);
        _from1 = Line.Range(1, size);
        _from4 = Line.Range(4, size);
        _from10 = Line.Range(10, size);

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var i = y * size + x;
                if (modules[i]) SetBit(x, y, _baseRows, _baseColumns);
                if (!isFunction[i]) SetBit(x, y, _dataRows, _dataColumns);
            }
        }
    }

    /// <summary>
    /// Re-reads row 8 and column 8, which hold every format-information
    /// module, after the format bits for a new candidate were drawn.
    /// </summary>
    public void RefreshFormatLines(bool[] modules)
    {
        const int Line8 = 8;
        for (var i = 0; i < _size; i++)
        {
            SetBit(i, Line8, _baseRows, _baseColumns, modules[Line8 * _size + i]);
            SetBit(Line8, i, _baseRows, _baseColumns, modules[i * _size + Line8]);
        }
    }

    /// <summary>The total penalty of the symbol with mask <paramref name="mask"/> applied.</summary>
    public int Score(int mask)
    {
        var size = _size;
        for (var y = 0; y < size; y++)
        {
            var o = y * Words;
            var p = (mask * Period + y % Period) * Words;
            _rows[o] = _baseRows[o] ^ (RowPatterns[p] & _dataRows[o]);
            _rows[o + 1] = _baseRows[o + 1] ^ (RowPatterns[p + 1] & _dataRows[o + 1]);
            _rows[o + 2] = _baseRows[o + 2] ^ (RowPatterns[p + 2] & _dataRows[o + 2]);
            _columns[o] = _baseColumns[o] ^ (ColumnPatterns[p] & _dataColumns[o]);
            _columns[o + 1] = _baseColumns[o + 1] ^ (ColumnPatterns[p + 1] & _dataColumns[o + 1]);
            _columns[o + 2] = _baseColumns[o + 2] ^ (ColumnPatterns[p + 2] & _dataColumns[o + 2]);
        }

        var penalty = 0;
        var dark = 0;
        var previous = default(Line);
        for (var i = 0; i < size; i++)
        {
            var row = Line.Load(_rows, i * Words);
            penalty += LinePenalty(row) + LinePenalty(Line.Load(_columns, i * Words));
            dark += (row & _all).PopCount();

            // N2: 2x2 blocks of one colour, found between this row and the one above.
            if (i > 0)
            {
                var vertical = ~(row ^ previous);
                var horizontal = ~(previous ^ previous.Shl(1));
                penalty += 3 * (vertical & vertical.Shl(1) & horizontal & _from1).PopCount();
            }

            previous = row;
        }

        // N4: 10 points per full 5% that the dark share deviates from 50%.
        var total = size * size;
        var k = (Math.Abs(dark * 20 - total * 10) + total - 1) / total - 1;
        return penalty + k * 10;
    }

    /// <summary>N1 (runs of five or more) and N3 (finder-like patterns) along one line.</summary>
    private int LinePenalty(Line line)
    {
        // Modules equal to each of the four before them: one per module beyond the fourth of a run of five or more.
        var same = ~(line ^ line.Shl(1)) & ~(line ^ line.Shl(2)) & ~(line ^ line.Shl(3)) & ~(line ^ line.Shl(4)) & _from4;
        var runStarts = same & ~same.Shl(1);

        // A run of length n >= 5 scores 3 + (n - 5) = (n - 4) + 2.
        var penalty = same.PopCount() + 2 * runStarts.PopCount();

        var matchA = _from10;
        var matchB = _from10;
        for (var j = 0; j <= 10; j++)
        {
            var shifted = j == 0 ? line : line.Shl(j);
            matchA &= ((FinderPatternA >> j) & 1) != 0 ? shifted : ~shifted;
            matchB &= ((FinderPatternB >> j) & 1) != 0 ? shifted : ~shifted;
        }

        return penalty + 40 * (matchA.PopCount() + matchB.PopCount());
    }

    private static void SetBit(int x, int y, ulong[] rows, ulong[] columns, bool value = true)
    {
        var rowBit = 1UL << (x & 63);
        var columnBit = 1UL << (y & 63);
        var r = y * Words + (x >> 6);
        var c = x * Words + (y >> 6);
        if (value)
        {
            rows[r] |= rowBit;
            columns[c] |= columnBit;
        }
        else
        {
            rows[r] &= ~rowBit;
            columns[c] &= ~columnBit;
        }
    }

    private static ulong[] BuildPatterns(bool columns)
    {
        var table = new ulong[8 * Period * Words];
        for (var mask = 0; mask < 8; mask++)
        {
            for (var phase = 0; phase < Period; phase++)
            {
                for (var p = 0; p < MaxSize; p++)
                {
                    var inverts = columns ? QrCodeEncoder.MaskInverts(mask, phase, p) : QrCodeEncoder.MaskInverts(mask, p, phase);
                    if (inverts) table[(mask * Period + phase) * Words + (p >> 6)] |= 1UL << (p & 63);
                }
            }
        }

        return table;
    }

    /// <summary>A line of up to 192 modules.</summary>
    private readonly struct Line
    {
        private readonly ulong _w0;
        private readonly ulong _w1;
        private readonly ulong _w2;

        private Line(ulong w0, ulong w1, ulong w2)
        {
            _w0 = w0;
            _w1 = w1;
            _w2 = w2;
        }

        public static Line Load(ulong[] words, int offset) => new(words[offset], words[offset + 1], words[offset + 2]);

        /// <summary>Bits <paramref name="from"/> (inclusive) to <paramref name="to"/> (exclusive).</summary>
        public static Line Range(int from, int to)
        {
            ulong w0 = 0, w1 = 0, w2 = 0;
            for (var p = from; p < to; p++)
            {
                var bit = 1UL << (p & 63);
                if (p < 64) w0 |= bit;
                else if (p < 128) w1 |= bit;
                else w2 |= bit;
            }

            return new Line(w0, w1, w2);
        }

        /// <summary>Moves every module <paramref name="k"/> places up (1 to 63), so bit p holds module p - k.</summary>
        public Line Shl(int k) => new(_w0 << k, (_w1 << k) | (_w0 >> (64 - k)), (_w2 << k) | (_w1 >> (64 - k)));

        public int PopCount() => PopCount(_w0) + PopCount(_w1) + PopCount(_w2);

        public static Line operator &(Line a, Line b) => new(a._w0 & b._w0, a._w1 & b._w1, a._w2 & b._w2);

        public static Line operator ^(Line a, Line b) => new(a._w0 ^ b._w0, a._w1 ^ b._w1, a._w2 ^ b._w2);

        public static Line operator ~(Line a) => new(~a._w0, ~a._w1, ~a._w2);

        private static int PopCount(ulong value)
        {
#if NETSTANDARD2_0
            value -= (value >> 1) & 0x5555555555555555UL;
            value = (value & 0x3333333333333333UL) + ((value >> 2) & 0x3333333333333333UL);
            value = (value + (value >> 4)) & 0x0F0F0F0F0F0F0F0FUL;
            return (int)((value * 0x0101010101010101UL) >> 56);
#else
            return System.Numerics.BitOperations.PopCount(value);
#endif
        }
    }
}
