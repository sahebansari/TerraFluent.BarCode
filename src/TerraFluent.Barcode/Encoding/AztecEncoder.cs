// Portions adapted from ZXing (https://github.com/zxing/zxing), Copyright 2013 ZXing authors,
// licensed under the Apache License, Version 2.0; modified (translated to C# and restructured).
// See THIRD-PARTY-NOTICES.md.

using System.Text;

namespace TerraFluent.Barcode.Encoding;

/// <summary>
/// Aztec Code (ISO/IEC 24778): compact (1-4 layers) and full-range (4-32
/// layers) symbols around a central bullseye finder. Text is split across the
/// Upper, Lower, Mixed, Punctuation, Digit and Byte modes for the shortest bit
/// stream; text outside ISO-8859-1 is sent as UTF-8 behind an ECI 26 flag.
/// </summary>
/// <remarks>
/// The mode-selection search and the layer layout follow the approach of the
/// ZXing Aztec encoder (Apache License 2.0); see THIRD-PARTY-NOTICES.md.
/// </remarks>
internal static class AztecEncoder
{
    /// <summary>Default minimum error correction, as a percentage of the data bits.</summary>
    public const int DefaultErrorCorrectionPercent = 33;

    public const int MinErrorCorrectionPercent = 5;
    public const int MaxErrorCorrectionPercent = 95;

    /// <summary>Aztec needs no quiet zone; a small margin helps scanners find the edge.</summary>
    public const int QuietZone = 2;

    private const int MaxLayers = 32;

    private static readonly int[] WordSize =
    {
        4, 6, 6, 8, 8, 8, 8, 8, 8, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10,
        12, 12, 12, 12, 12, 12, 12, 12, 12, 12,
    };

    public static BarcodeMatrix Encode(string value, int errorCorrectionPercent = DefaultErrorCorrectionPercent)
    {
        var bits = HighLevel.Encode(value);
        var eccBits = bits.Count * errorCorrectionPercent / 100 + 11;
        var totalSizeBits = bits.Count + eccBits;

        bool compact = false;
        int layers = 0, totalBitsInLayer = 0, wordSize = 0;
        List<bool>? stuffed = null;
        for (var i = 0; ; i++)
        {
            if (i > MaxLayers)
            {
                throw new ArgumentException(
                    $"The value is too long for an Aztec code ({bits.Count} data bits with {errorCorrectionPercent}% error correction). " +
                    "Shorten it or lower the error correction.",
                    nameof(value));
            }

            compact = i <= 3;
            layers = compact ? i + 1 : i;
            totalBitsInLayer = TotalBitsInLayers(layers, compact);
            if (totalSizeBits > totalBitsInLayer) continue;
            if (stuffed is null || wordSize != WordSize[layers])
            {
                wordSize = WordSize[layers];
                stuffed = StuffBits(bits, wordSize);
            }

            var usable = totalBitsInLayer - totalBitsInLayer % wordSize;
            if (compact && stuffed.Count > wordSize * 64) continue;
            if (stuffed.Count + eccBits <= usable) break;
        }

        var messageBits = CheckWords(stuffed, totalBitsInLayer, wordSize);
        var messageWords = stuffed.Count / wordSize;
        var modeMessage = ModeMessage(compact, layers, messageWords);
        return Layout(compact, layers, messageBits, modeMessage);
    }

    private static int TotalBitsInLayers(int layers, bool compact) => ((compact ? 88 : 112) + 16 * layers) * layers;

    /// <summary>
    /// Splits the bits into words and stuffs a bit into any word that would be
    /// all zeros or all ones (both are reserved), padding the last word with ones.
    /// </summary>
    internal static List<bool> StuffBits(List<bool> bits, int wordSize)
    {
        var result = new List<bool>(bits.Count + bits.Count / wordSize + wordSize);
        var n = bits.Count;
        var mask = (1 << wordSize) - 2;
        for (var i = 0; i < n; i += wordSize)
        {
            var word = 0;
            for (var j = 0; j < wordSize; j++)
            {
                if (i + j >= n || bits[i + j]) word |= 1 << (wordSize - 1 - j);
            }

            if ((word & mask) == mask)
            {
                Append(result, word & mask, wordSize);
                i--;
            }
            else if ((word & mask) == 0)
            {
                Append(result, word | 1, wordSize);
                i--;
            }
            else
            {
                Append(result, word, wordSize);
            }
        }

        return result;
    }

    private static List<bool> ModeMessage(bool compact, int layers, int messageWords)
    {
        var bits = new List<bool>(40);
        if (compact)
        {
            Append(bits, layers - 1, 2);
            Append(bits, messageWords - 1, 6);
            return CheckWords(bits, 28, 4);
        }

        Append(bits, layers - 1, 5);
        Append(bits, messageWords - 1, 11);
        return CheckWords(bits, 40, 4);
    }

    /// <summary>Appends Reed-Solomon check words to fill <paramref name="totalBits"/>, with leading zero padding.</summary>
    private static List<bool> CheckWords(List<bool> bits, int totalBits, int wordSize)
    {
        var dataWords = bits.Count / wordSize;
        var totalWords = totalBits / wordSize;
        var words = new int[totalWords];
        for (var i = 0; i < dataWords; i++)
        {
            var value = 0;
            for (var j = 0; j < wordSize; j++)
            {
                if (bits[i * wordSize + j]) value |= 1 << (wordSize - 1 - j);
            }

            words[i] = value;
        }

        Field(wordSize).Encode(words, 0, dataWords, totalWords - dataWords);
        var result = new List<bool>(totalBits);
        Append(result, 0, totalBits % wordSize);
        foreach (var word in words) Append(result, word, wordSize);
        return result;
    }

    private static GaloisField Field(int wordSize) => wordSize switch
    {
        4 => GaloisField.Aztec4,
        6 => GaloisField.Aztec6,
        8 => GaloisField.Aztec8,
        10 => GaloisField.Aztec10,
        _ => GaloisField.Aztec12,
    };

    private static BarcodeMatrix Layout(bool compact, int layers, List<bool> message, List<bool> modeMessage)
    {
        var baseSize = (compact ? 11 : 14) + layers * 4;
        var map = new int[baseSize];
        int size;
        if (compact)
        {
            size = baseSize;
            for (var i = 0; i < baseSize; i++) map[i] = i;
        }
        else
        {
            // Full-range symbols have a reference grid line every 16 modules from the centre.
            size = baseSize + 1 + 2 * ((baseSize / 2 - 1) / 15);
            var origCentre = baseSize / 2;
            var centre = size / 2;
            for (var i = 0; i < origCentre; i++)
            {
                var offset = i + i / 15;
                map[origCentre - i - 1] = centre - offset - 1;
                map[origCentre + i] = centre + offset + 1;
            }
        }

        var modules = new bool[size * size];
        void Set(int x, int y) => modules[y * size + x] = true;

        // Data spirals inwards: each layer is two modules thick, written in four sides.
        for (int i = 0, rowOffset = 0; i < layers; i++)
        {
            var rowSize = (layers - i) * 4 + (compact ? 9 : 12);
            for (var j = 0; j < rowSize; j++)
            {
                var columnOffset = j * 2;
                for (var k = 0; k < 2; k++)
                {
                    if (message[rowOffset + columnOffset + k]) Set(map[i * 2 + k], map[i * 2 + j]);
                    if (message[rowOffset + rowSize * 2 + columnOffset + k]) Set(map[i * 2 + j], map[baseSize - 1 - i * 2 - k]);
                    if (message[rowOffset + rowSize * 4 + columnOffset + k]) Set(map[baseSize - 1 - i * 2 - k], map[baseSize - 1 - i * 2 - j]);
                    if (message[rowOffset + rowSize * 6 + columnOffset + k]) Set(map[baseSize - 1 - i * 2 - j], map[i * 2 + k]);
                }
            }

            rowOffset += rowSize * 8;
        }

        var c = size / 2;
        if (compact)
        {
            for (var i = 0; i < 7; i++)
            {
                var offset = c - 3 + i;
                if (modeMessage[i]) Set(offset, c - 5);
                if (modeMessage[i + 7]) Set(c + 5, offset);
                if (modeMessage[20 - i]) Set(offset, c + 5);
                if (modeMessage[27 - i]) Set(c - 5, offset);
            }
        }
        else
        {
            for (var i = 0; i < 10; i++)
            {
                var offset = c - 5 + i + i / 5;
                if (modeMessage[i]) Set(offset, c - 7);
                if (modeMessage[i + 10]) Set(c + 7, offset);
                if (modeMessage[29 - i]) Set(offset, c + 7);
                if (modeMessage[39 - i]) Set(c - 7, offset);
            }
        }

        // Bullseye rings and the orientation marks at its corners.
        var eye = compact ? 5 : 7;
        for (var i = 0; i < eye; i += 2)
        {
            for (var j = c - i; j <= c + i; j++)
            {
                Set(j, c - i);
                Set(j, c + i);
                Set(c - i, j);
                Set(c + i, j);
            }
        }

        Set(c - eye, c - eye);
        Set(c - eye + 1, c - eye);
        Set(c - eye, c - eye + 1);
        Set(c + eye, c - eye);
        Set(c + eye, c - eye + 1);
        Set(c + eye, c + eye - 1);

        if (!compact)
        {
            for (int i = 0, j = 0; i < baseSize / 2 - 1; i += 15, j += 16)
            {
                for (var k = c & 1; k < size; k += 2)
                {
                    Set(c - j, k);
                    Set(c + j, k);
                    Set(k, c - j);
                    Set(k, c + j);
                }
            }
        }

        return new BarcodeMatrix(size, size, modules, isLinear: false, QuietZone);
    }

    private static void Append(List<bool> bits, int value, int count)
    {
        for (var i = count - 1; i >= 0; i--) bits.Add(((value >> i) & 1) != 0);
    }

    /// <summary>
    /// Finds the shortest encoding by tracking, for each character, the best
    /// state ending in each mode (and in an open binary shift), dropping states
    /// that another state beats whatever comes next.
    /// </summary>
    internal static class HighLevel
    {
        private const int Upper = 0, Lower = 1, Digit = 2, Mixed = 3, Punct = 4;

        // Latch costs and codes: (bit count << 16) | code bits.
        private static readonly int[][] Latch =
        {
            new[] { 0, (5 << 16) + 28, (5 << 16) + 30, (5 << 16) + 29, (10 << 16) + (29 << 5) + 30 },
            new[] { (9 << 16) + (30 << 4) + 14, 0, (5 << 16) + 30, (5 << 16) + 29, (10 << 16) + (29 << 5) + 30 },
            new[] { (4 << 16) + 14, (9 << 16) + (14 << 5) + 28, 0, (9 << 16) + (14 << 5) + 29, (14 << 16) + (14 << 10) + (29 << 5) + 30 },
            new[] { (5 << 16) + 29, (5 << 16) + 28, (10 << 16) + (29 << 5) + 30, 0, (5 << 16) + 30 },
            new[] { (5 << 16) + 31, (10 << 16) + (31 << 5) + 28, (10 << 16) + (31 << 5) + 30, (10 << 16) + (31 << 5) + 29, 0 },
        };

        // Shift codes from one mode to another, or -1.
        private static readonly int[][] Shift = BuildShiftTable();

        // Code of each byte value in each mode, or 0 when the mode cannot encode it.
        private static readonly int[][] CharMap = BuildCharMap();

        private static int[][] BuildShiftTable()
        {
            var table = new int[5][];
            for (var i = 0; i < 5; i++) table[i] = new[] { -1, -1, -1, -1, -1 };
            table[Upper][Punct] = 0;
            table[Lower][Punct] = 0;
            table[Lower][Upper] = 28;
            table[Mixed][Punct] = 0;
            table[Digit][Punct] = 0;
            table[Digit][Upper] = 15;
            return table;
        }

        private static int[][] BuildCharMap()
        {
            var map = new int[5][];
            for (var i = 0; i < 5; i++) map[i] = new int[256];
            map[Upper][' '] = 1;
            for (var c = 'A'; c <= 'Z'; c++) map[Upper][c] = c - 'A' + 2;
            map[Lower][' '] = 1;
            for (var c = 'a'; c <= 'z'; c++) map[Lower][c] = c - 'a' + 2;
            map[Digit][' '] = 1;
            for (var c = '0'; c <= '9'; c++) map[Digit][c] = c - '0' + 2;
            map[Digit][','] = 12;
            map[Digit]['.'] = 13;

            int[] mixed = { 0, ' ', 1, 2, 3, 4, 5, 6, 7, '\b', '\t', '\n', 11, '\f', '\r', 27, 28, 29, 30, 31, '@', '\\', '^', '_', '`', '|', '~', 127 };
            for (var i = 0; i < mixed.Length; i++) map[Mixed][mixed[i]] = i;

            const string punct = "\0\r\0\0\0\0!\"#$%&'()*+,-./:;<=>?[]{}";
            for (var i = 0; i < punct.Length; i++)
            {
                if (punct[i] > 0) map[Punct][punct[i]] = i;
            }

            return map;
        }

        public static List<bool> Encode(string value)
        {
            var latin1 = true;
            foreach (var ch in value)
            {
                if (ch > 0xFF)
                {
                    latin1 = false;
                    break;
                }
            }

            byte[] text;
            var initial = State.Initial;
            if (latin1)
            {
                text = new byte[value.Length];
                for (var i = 0; i < value.Length; i++) text[i] = (byte)value[i];
            }
            else
            {
                try
                {
                    text = new UTF8Encoding(false, true).GetBytes(value);
                }
                catch (EncoderFallbackException ex)
                {
                    throw new ArgumentException("The value contains an unpaired surrogate and cannot be encoded as UTF-8.", nameof(value), ex);
                }

                initial = initial.AppendEci(26);
            }

            var states = new List<State> { initial };
            for (var index = 0; index < text.Length; index++)
            {
                var next = index + 1 < text.Length ? text[index + 1] : 0;
                var pairCode = text[index] switch
                {
                    (byte)'\r' => next == '\n' ? 2 : 0,
                    (byte)'.' => next == ' ' ? 3 : 0,
                    (byte)',' => next == ' ' ? 4 : 0,
                    (byte)':' => next == ' ' ? 5 : 0,
                    _ => 0,
                };

                if (pairCode > 0)
                {
                    states = UpdateForPair(states, index, pairCode);
                    index++;
                }
                else
                {
                    states = UpdateForChar(states, index, text);
                }
            }

            var best = states[0];
            foreach (var s in states)
            {
                if (s.BitCount < best.BitCount) best = s;
            }

            return best.ToBits(text);
        }

        private static List<State> UpdateForChar(List<State> states, int index, byte[] text)
        {
            var result = new List<State>();
            foreach (var state in states) UpdateForChar(state, index, text[index], result);
            return Simplify(result);
        }

        private static void UpdateForChar(State state, int index, byte ch, List<State> result)
        {
            var inCurrent = CharMap[state.Mode][ch] > 0;
            State? noBinary = null;
            for (var mode = 0; mode <= Punct; mode++)
            {
                var code = CharMap[mode][ch];
                if (code <= 0) continue;
                noBinary ??= state.EndBinaryShift(index);
                // Latch only when the current mode cannot take the character, or to Digit (cheap per character).
                if (!inCurrent || mode == state.Mode || mode == Digit) result.Add(noBinary.LatchAndAppend(mode, code));
                // A shift never helps when the current mode already has the character.
                if (!inCurrent && Shift[state.Mode][mode] >= 0) result.Add(noBinary.ShiftAndAppend(mode, code));
            }

            if (state.BinaryShiftByteCount > 0 || CharMap[state.Mode][ch] == 0) result.Add(state.AddBinaryShiftChar(index));
        }

        private static List<State> UpdateForPair(List<State> states, int index, int pairCode)
        {
            var result = new List<State>();
            foreach (var state in states)
            {
                var noBinary = state.EndBinaryShift(index);
                result.Add(noBinary.LatchAndAppend(Punct, pairCode));
                if (state.Mode != Punct) result.Add(noBinary.ShiftAndAppend(Punct, pairCode));
                if (pairCode is 3 or 4)
                {
                    // ". " and ", " also exist in Digit mode as two characters.
                    result.Add(noBinary.LatchAndAppend(Digit, 16 - pairCode).LatchAndAppend(Digit, 1));
                }

                if (state.BinaryShiftByteCount > 0) result.Add(state.AddBinaryShiftChar(index).AddBinaryShiftChar(index + 1));
            }

            return Simplify(result);
        }

        private static List<State> Simplify(List<State> states)
        {
            var result = new List<State>(states.Count);
            foreach (var candidate in states)
            {
                var add = true;
                for (var i = result.Count - 1; i >= 0; i--)
                {
                    var old = result[i];
                    if (old.IsBetterThanOrEqualTo(candidate))
                    {
                        add = false;
                        break;
                    }

                    if (candidate.IsBetterThanOrEqualTo(old)) result.RemoveAt(i);
                }

                if (add) result.Add(candidate);
            }

            return result;
        }

        /// <summary>A bit sequence to emit: a fixed-width code, or a run of bytes in Binary Shift mode.</summary>
        private sealed class Token
        {
            public static readonly Token Empty = new(null, 0, 0, binary: false);

            private Token(Token? previous, int value, int count, bool binary)
            {
                Previous = previous;
                Value = value;
                Count = count;
                IsBinary = binary;
            }

            public Token? Previous { get; }

            /// <summary>The code, or the first byte index of a binary shift.</summary>
            public int Value { get; }

            /// <summary>The code's bit count, or the byte count of a binary shift.</summary>
            public int Count { get; }

            public bool IsBinary { get; }

            public Token Add(int value, int bitCount) => new(this, value, bitCount, binary: false);

            public Token AddBinaryShift(int start, int byteCount) => new(this, start, byteCount, binary: true);

            public void AppendTo(List<bool> bits, byte[] text)
            {
                if (!IsBinary)
                {
                    Append(bits, Value, Count);
                    return;
                }

                for (var i = 0; i < Count; i++)
                {
                    if (i == 0 || (i == 31 && Count <= 62))
                    {
                        Append(bits, 31, 5); // Binary Shift
                        if (Count > 62) Append(bits, Count - 31, 16);
                        else if (i == 0) Append(bits, Math.Min(Count, 31), 5);
                        else Append(bits, Count - 31, 5);
                    }

                    Append(bits, text[Value + i], 8);
                }
            }
        }

        private sealed class State
        {
            public static readonly State Initial = new(Token.Empty, Upper, 0, 0);

            private State(Token token, int mode, int binaryShiftByteCount, int bitCount)
            {
                Token = token;
                Mode = mode;
                BinaryShiftByteCount = binaryShiftByteCount;
                BitCount = bitCount;
            }

            public Token Token { get; }
            public int Mode { get; }
            public int BinaryShiftByteCount { get; }
            public int BitCount { get; }

            private int ModeBits => Mode == Digit ? 4 : 5;

            /// <summary>FLG(n) with an ECI designator, sent from Punctuation mode by a shift.</summary>
            public State AppendEci(int eci)
            {
                var shifted = ShiftAndAppend(Punct, 0);
                var digits = eci.ToString(System.Globalization.CultureInfo.InvariantCulture);
                var token = shifted.Token.Add(digits.Length, 3);
                foreach (var d in digits) token = token.Add(d - '0' + 2, 4);
                return new State(token, Mode, 0, shifted.BitCount + 3 + digits.Length * 4);
            }

            public State LatchAndAppend(int mode, int code)
            {
                var bitCount = BitCount;
                var token = Token;
                if (mode != Mode)
                {
                    var latch = Latch[Mode][mode];
                    token = token.Add(latch & 0xFFFF, latch >> 16);
                    bitCount += latch >> 16;
                }

                var modeBits = mode == Digit ? 4 : 5;
                return new State(token.Add(code, modeBits), mode, 0, bitCount + modeBits);
            }

            public State ShiftAndAppend(int mode, int code)
            {
                var token = Token.Add(Shift[Mode][mode], ModeBits).Add(code, 5);
                return new State(token, Mode, 0, BitCount + ModeBits + 5);
            }

            public State AddBinaryShiftChar(int index)
            {
                var token = Token;
                var mode = Mode;
                var bitCount = BitCount;
                if (Mode is Punct or Digit)
                {
                    var latch = Latch[Mode][Upper];
                    token = token.Add(latch & 0xFFFF, latch >> 16);
                    bitCount += latch >> 16;
                    mode = Upper;
                }

                var delta = BinaryShiftByteCount is 0 or 31 ? 18 : BinaryShiftByteCount == 62 ? 9 : 8;
                var result = new State(token, mode, BinaryShiftByteCount + 1, bitCount + delta);
                return result.BinaryShiftByteCount == 2047 + 31 ? result.EndBinaryShift(index + 1) : result;
            }

            public State EndBinaryShift(int index)
            {
                if (BinaryShiftByteCount == 0) return this;
                return new State(Token.AddBinaryShift(index - BinaryShiftByteCount, BinaryShiftByteCount), Mode, 0, BitCount);
            }

            public bool IsBetterThanOrEqualTo(State other)
            {
                var bits = BitCount + (Latch[Mode][other.Mode] >> 16);
                if (BinaryShiftByteCount < other.BinaryShiftByteCount) bits += BinaryShiftCost(other) - BinaryShiftCost(this);
                else if (BinaryShiftByteCount > other.BinaryShiftByteCount && other.BinaryShiftByteCount > 0) bits += 10;
                return bits <= other.BitCount;
            }

            private static int BinaryShiftCost(State state) => state.BinaryShiftByteCount switch
            {
                > 62 => 21,
                > 31 => 20,
                > 0 => 10,
                _ => 0,
            };

            public List<bool> ToBits(byte[] text)
            {
                var end = EndBinaryShift(text.Length);
                var tokens = new List<Token>();
                for (var t = end.Token; t.Previous is not null; t = t.Previous) tokens.Add(t);
                var bits = new List<bool>(BitCount + 16);
                for (var i = tokens.Count - 1; i >= 0; i--) tokens[i].AppendTo(bits, text);
                return bits;
            }
        }
    }
}
