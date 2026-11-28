using System.Text;

namespace TerraFluent.Barcode.Encoding;

/// <summary>
/// Data Matrix ECC 200 (ISO/IEC 16022), square and rectangular symbols, ASCII
/// encodation (digit pairs packed into one codeword, upper shift for bytes
/// above 127). Text outside ISO-8859-1 is sent as UTF-8 behind an ECI 26 header.
/// </summary>
internal static class DataMatrixEncoder
{
    public const int QuietZone = 2;

    private const int UpperShift = 235;
    private const int Pad = 129;
    private const int Eci = 241;

    internal sealed class SymbolInfo
    {
        public SymbolInfo(int rows, int cols, int regionRows, int regionCols, int dataCodewords, int eccCodewords, int blocks)
        {
            Rows = rows;
            Cols = cols;
            RegionRows = regionRows;
            RegionCols = regionCols;
            DataCodewords = dataCodewords;
            EccCodewords = eccCodewords;
            Blocks = blocks;
        }

        public int Rows { get; }
        public int Cols { get; }
        public int RegionRows { get; }
        public int RegionCols { get; }
        public int DataCodewords { get; }
        public int EccCodewords { get; }
        public int Blocks { get; }

        public bool IsSquare => Rows == Cols;

        /// <summary>Data region height and width, in modules (without the finder and timing edges).</summary>
        public int RegionHeight => Rows / RegionRows - 2;
        public int RegionWidth => Cols / RegionCols - 2;
    }

    // ISO/IEC 16022 Table 7: rows, cols, regions vertically, regions horizontally, data codewords, ECC codewords, interleaved blocks.
    internal static readonly SymbolInfo[] Symbols =
    {
        new(10, 10, 1, 1, 3, 5, 1), new(12, 12, 1, 1, 5, 7, 1), new(8, 18, 1, 1, 5, 7, 1),
        new(14, 14, 1, 1, 8, 10, 1), new(8, 32, 1, 2, 10, 11, 1), new(16, 16, 1, 1, 12, 12, 1),
        new(12, 26, 1, 1, 16, 14, 1), new(18, 18, 1, 1, 18, 14, 1), new(20, 20, 1, 1, 22, 18, 1),
        new(12, 36, 1, 2, 22, 18, 1), new(22, 22, 1, 1, 30, 20, 1), new(16, 36, 1, 2, 32, 24, 1),
        new(24, 24, 1, 1, 36, 24, 1), new(26, 26, 1, 1, 44, 28, 1), new(16, 48, 1, 2, 49, 28, 1),
        new(32, 32, 2, 2, 62, 36, 1), new(36, 36, 2, 2, 86, 42, 1), new(40, 40, 2, 2, 114, 48, 1),
        new(44, 44, 2, 2, 144, 56, 1), new(48, 48, 2, 2, 174, 68, 1), new(52, 52, 2, 2, 204, 84, 2),
        new(64, 64, 4, 4, 280, 112, 2), new(72, 72, 4, 4, 368, 144, 4), new(80, 80, 4, 4, 456, 192, 4),
        new(88, 88, 4, 4, 576, 224, 4), new(96, 96, 4, 4, 696, 272, 4), new(104, 104, 4, 4, 816, 336, 6),
        new(120, 120, 6, 6, 1050, 408, 6), new(132, 132, 6, 6, 1304, 496, 8), new(144, 144, 6, 6, 1558, 620, 10),
    };

    public static BarcodeMatrix Encode(string value, DataMatrixShape shape)
    {
        var data = EncodeAscii(value);
        var symbol = Symbols.FirstOrDefault(s => s.DataCodewords >= data.Count && Allowed(s, shape))
            ?? throw new ArgumentException(
                $"The value is too long for a Data Matrix symbol ({data.Count} codewords; the largest {ShapeName(shape)} symbol holds {Symbols.Where(s => Allowed(s, shape)).Max(s => s.DataCodewords)}).",
                nameof(value));

        AddPadding(data, symbol.DataCodewords);
        var codewords = AddErrorCorrection(data.ToArray(), symbol);
        return Place(codewords, symbol);
    }

    private static bool Allowed(SymbolInfo s, DataMatrixShape shape) => shape switch
    {
        DataMatrixShape.Square => s.IsSquare,
        DataMatrixShape.Rectangle => !s.IsSquare,
        _ => true,
    };

    private static string ShapeName(DataMatrixShape shape) => shape switch
    {
        DataMatrixShape.Square => "square",
        DataMatrixShape.Rectangle => "rectangular",
        _ => "",
    };

    /// <summary>ASCII encodation of the value's bytes (ISO-8859-1, or UTF-8 behind ECI 26).</summary>
    internal static List<int> EncodeAscii(string value)
    {
        byte[] bytes;
        var result = new List<int>(value.Length + 4);
        if (value.All(c => c <= 0xFF))
        {
            bytes = value.Select(c => (byte)c).ToArray();
        }
        else
        {
            try
            {
                bytes = new UTF8Encoding(false, true).GetBytes(value);
            }
            catch (EncoderFallbackException ex)
            {
                throw new ArgumentException("The value contains an unpaired surrogate character and is not valid Unicode text.", nameof(value), ex);
            }

            result.Add(Eci);
            result.Add(26 + 1); // ECI designators 0-126 are sent as value + 1
        }

        for (var i = 0; i < bytes.Length; i++)
        {
            var b = bytes[i];
            if (IsDigit(b) && i + 1 < bytes.Length && IsDigit(bytes[i + 1]))
            {
                result.Add(130 + (b - '0') * 10 + (bytes[i + 1] - '0'));
                i++;
            }
            else if (b < 128)
            {
                result.Add(b + 1);
            }
            else
            {
                result.Add(UpperShift);
                result.Add(b - 128 + 1);
            }
        }

        return result;
    }

    private static bool IsDigit(byte b) => b >= '0' && b <= '9';

    private static void AddPadding(List<int> data, int capacity)
    {
        if (data.Count < capacity) data.Add(Pad);
        while (data.Count < capacity)
        {
            var position = data.Count + 1; // 1-based position of this pad codeword
            var pad = Pad + 149 * position % 253 + 1;
            data.Add(pad <= 254 ? pad : pad - 254);
        }
    }

    private static byte[] AddErrorCorrection(int[] data, SymbolInfo symbol)
    {
        var blocks = symbol.Blocks;
        var result = new byte[symbol.DataCodewords + symbol.EccCodewords];
        for (var i = 0; i < data.Length; i++) result[i] = (byte)data[i];

        // Block b holds every blocks-th codeword from b, data and check words alike.
        var ecc = new int[symbol.EccCodewords / blocks];
        for (var b = 0; b < blocks; b++)
        {
            var count = (data.Length - b + blocks - 1) / blocks;
            GaloisField.DataMatrix.Remainder(data, b, count, blocks, ecc);
            for (var j = 0; j < ecc.Length; j++) result[data.Length + b + j * blocks] = (byte)ecc[j];
        }

        return result;
    }

    private static BarcodeMatrix Place(byte[] codewords, SymbolInfo symbol)
    {
        int regionH = symbol.RegionHeight, regionW = symbol.RegionWidth;
        int numRows = regionH * symbol.RegionRows, numCols = regionW * symbol.RegionCols;
        var placement = new Placement(codewords, numRows, numCols);
        placement.Run();

        var width = symbol.Cols;
        var height = symbol.Rows;
        var modules = new bool[width * height];
        void Set(int x, int y, bool dark) => modules[y * width + x] = dark;

        var matrixY = 0;
        for (var y = 0; y < numRows; y++)
        {
            int matrixX;
            if (y % regionH == 0)
            {
                // Timing pattern along the top of each region.
                for (matrixX = 0; matrixX < width; matrixX++) Set(matrixX, matrixY, matrixX % 2 == 0);
                matrixY++;
            }

            matrixX = 0;
            for (var x = 0; x < numCols; x++)
            {
                if (x % regionW == 0) Set(matrixX++, matrixY, true); // solid left edge
                Set(matrixX++, matrixY, placement.Get(x, y));
                if (x % regionW == regionW - 1) Set(matrixX++, matrixY, y % 2 == 0); // timing on the right edge
            }

            matrixY++;
            if (y % regionH == regionH - 1)
            {
                // Solid bottom edge.
                for (matrixX = 0; matrixX < width; matrixX++) Set(matrixX, matrixY, true);
                matrixY++;
            }
        }

        return new BarcodeMatrix(width, height, modules, isLinear: false, QuietZone);
    }

    /// <summary>ISO/IEC 16022 Annex F: places codeword bits diagonally in the mapping matrix.</summary>
    private sealed class Placement
    {
        private readonly byte[] _codewords;
        private readonly int _rows;
        private readonly int _cols;
        private readonly sbyte[] _bits;

        public Placement(byte[] codewords, int rows, int cols)
        {
            _codewords = codewords;
            _rows = rows;
            _cols = cols;
            _bits = Enumerable.Repeat((sbyte)-1, rows * cols).ToArray();
        }

        public bool Get(int col, int row) => _bits[row * _cols + col] == 1;

        private bool Has(int col, int row) => _bits[row * _cols + col] >= 0;

        private void Put(int col, int row, bool bit) => _bits[row * _cols + col] = (sbyte)(bit ? 1 : 0);

        public void Run()
        {
            var pos = 0;
            int row = 4, col = 0;
            do
            {
                if (row == _rows && col == 0) Corner1(pos++);
                if (row == _rows - 2 && col == 0 && _cols % 4 != 0) Corner2(pos++);
                if (row == _rows - 2 && col == 0 && _cols % 8 == 4) Corner3(pos++);
                if (row == _rows + 4 && col == 2 && _cols % 8 == 0) Corner4(pos++);

                do
                {
                    if (row < _rows && col >= 0 && !Has(col, row)) Utah(row, col, pos++);
                    row -= 2;
                    col += 2;
                }
                while (row >= 0 && col < _cols);
                row++;
                col += 3;

                do
                {
                    if (row >= 0 && col < _cols && !Has(col, row)) Utah(row, col, pos++);
                    row += 2;
                    col -= 2;
                }
                while (row < _rows && col >= 0);
                row += 3;
                col++;
            }
            while (row < _rows || col < _cols);

            // Fixed pattern in the bottom-right corner when it is left unfilled.
            if (!Has(_cols - 1, _rows - 1))
            {
                Put(_cols - 1, _rows - 1, true);
                Put(_cols - 2, _rows - 2, true);
                Put(_cols - 1, _rows - 2, false);
                Put(_cols - 2, _rows - 1, false);
            }
        }

        private void Module(int row, int col, int pos, int bit)
        {
            if (row < 0)
            {
                row += _rows;
                col += 4 - (_rows + 4) % 8;
            }

            if (col < 0)
            {
                col += _cols;
                row += 4 - (_cols + 4) % 8;
            }

            var value = pos < _codewords.Length ? _codewords[pos] : 0;
            Put(col, row, (value & (1 << (8 - bit))) != 0);
        }

        private void Utah(int row, int col, int pos)
        {
            Module(row - 2, col - 2, pos, 1);
            Module(row - 2, col - 1, pos, 2);
            Module(row - 1, col - 2, pos, 3);
            Module(row - 1, col - 1, pos, 4);
            Module(row - 1, col, pos, 5);
            Module(row, col - 2, pos, 6);
            Module(row, col - 1, pos, 7);
            Module(row, col, pos, 8);
        }

        private void Corner1(int pos)
        {
            Module(_rows - 1, 0, pos, 1);
            Module(_rows - 1, 1, pos, 2);
            Module(_rows - 1, 2, pos, 3);
            Module(0, _cols - 2, pos, 4);
            Module(0, _cols - 1, pos, 5);
            Module(1, _cols - 1, pos, 6);
            Module(2, _cols - 1, pos, 7);
            Module(3, _cols - 1, pos, 8);
        }

        private void Corner2(int pos)
        {
            Module(_rows - 3, 0, pos, 1);
            Module(_rows - 2, 0, pos, 2);
            Module(_rows - 1, 0, pos, 3);
            Module(0, _cols - 4, pos, 4);
            Module(0, _cols - 3, pos, 5);
            Module(0, _cols - 2, pos, 6);
            Module(0, _cols - 1, pos, 7);
            Module(1, _cols - 1, pos, 8);
        }

        private void Corner3(int pos)
        {
            Module(_rows - 3, 0, pos, 1);
            Module(_rows - 2, 0, pos, 2);
            Module(_rows - 1, 0, pos, 3);
            Module(0, _cols - 2, pos, 4);
            Module(0, _cols - 1, pos, 5);
            Module(1, _cols - 1, pos, 6);
            Module(2, _cols - 1, pos, 7);
            Module(3, _cols - 1, pos, 8);
        }

        private void Corner4(int pos)
        {
            Module(_rows - 1, 0, pos, 1);
            Module(_rows - 1, _cols - 1, pos, 2);
            Module(0, _cols - 3, pos, 3);
            Module(0, _cols - 2, pos, 4);
            Module(0, _cols - 1, pos, 5);
            Module(1, _cols - 3, pos, 6);
            Module(1, _cols - 2, pos, 7);
            Module(1, _cols - 1, pos, 8);
        }
    }
}
