using System.Text;

namespace TerraFluent.Barcode.Encoding;

/// <summary>
/// Aztec Code (ISO/IEC 24778): compact (1-4 layers) and full-range (4-32
/// layers) symbols around a central bullseye. The text is split across the
/// Upper, Lower, Mixed, Punctuation, Digit and Binary Shift modes by a
/// shortest-path search; text outside ISO-8859-1 is sent as UTF-8 behind an
/// ECI 26 flag.
/// </summary>
internal static class AztecEncoder
{
    /// <summary>Default error correction: ISO/IEC 24778 recommends 23% of the symbol's codewords plus 3.</summary>
    public const int DefaultErrorCorrectionPercent = 23;

    public const int MinErrorCorrectionPercent = 5;
    public const int MaxErrorCorrectionPercent = 90;

    /// <summary>Aztec needs no quiet zone; a small margin helps scanners find the edge.</summary>
    public const int QuietZone = 2;

    public static BarcodeMatrix Encode(string value, int errorCorrectionPercent = DefaultErrorCorrectionPercent)
    {
        var bits = ModeEncoder.Encode(value);
        List<int>? words = null;
        var wordsSize = 0;

        // Candidates from small to large: compact 1-4 layers, then full-range 4-32.
        for (var candidate = 0; candidate < 4 + 29; candidate++)
        {
            var compact = candidate < 4;
            var layers = compact ? candidate + 1 : candidate;
            var wordSize = WordSize(layers);
            if (words is null || wordsSize != wordSize)
            {
                words = Stuff(bits, wordSize);
                wordsSize = wordSize;
            }

            var capacityBits = ((compact ? 88 : 112) + 16 * layers) * layers;
            var capacityWords = capacityBits / wordSize;
            var minCheckWords = (capacityWords * errorCorrectionPercent + 99) / 100 + 3;
            if (words.Count > (compact ? 64 : 2048) || words.Count + minCheckWords > capacityWords) continue;

            return Build(compact, layers, words, wordSize, capacityBits, capacityWords);
        }

        throw new ArgumentException(
            $"The value is too long for an Aztec code ({bits.Count} data bits with {errorCorrectionPercent}% error correction). " +
            "Shorten it or lower the error correction.",
            nameof(value));
    }

    private static int WordSize(int layers) => layers switch
    {
        <= 2 => 6,
        <= 8 => 8,
        <= 22 => 10,
        _ => 12,
    };

    private static GaloisField Field(int wordSize) => wordSize switch
    {
        4 => GaloisField.Aztec4,
        6 => GaloisField.Aztec6,
        8 => GaloisField.Aztec8,
        10 => GaloisField.Aztec10,
        _ => GaloisField.Aztec12,
    };

    /// <summary>
    /// Cuts the bit stream into codewords. A word whose first <c>b - 1</c> bits are
    /// all equal gets the opposite bit appended instead of the next data bit, because
    /// all-zero and all-one words are reserved. The last word is padded with ones.
    /// </summary>
    internal static List<int> Stuff(List<bool> bits, int wordSize)
    {
        var words = new List<int>(bits.Count / (wordSize - 1) + 1);
        var allOnes = (1 << (wordSize - 1)) - 1;
        var i = 0;
        while (i < bits.Count)
        {
            var word = 0;
            for (var j = 0; j < wordSize - 1; j++, i++) word = (word << 1) | (i >= bits.Count || bits[i] ? 1 : 0);
            if (word == 0) word = 1;
            else if (word == allOnes) word <<= 1;
            else
            {
                word = (word << 1) | (i >= bits.Count || bits[i] ? 1 : 0);
                i++;
            }

            words.Add(word);
        }

        return words;
    }

    private static BarcodeMatrix Build(bool compact, int layers, List<int> dataWords, int wordSize, int capacityBits, int capacityWords)
    {
        // Data words then check words, preceded by zero bits to fill the layers exactly.
        var codewords = new int[capacityWords];
        dataWords.CopyTo(codewords);
        Field(wordSize).Encode(codewords, 0, dataWords.Count, capacityWords - dataWords.Count);
        var stream = new bool[capacityBits];
        var position = capacityBits % wordSize;
        foreach (var word in codewords)
        {
            for (var b = wordSize - 1; b >= 0; b--) stream[position++] = ((word >> b) & 1) == 1;
        }

        var baseSize = (compact ? 11 : 14) + 4 * layers;
        var size = compact ? baseSize : baseSize + 1 + 2 * ((baseSize / 2 - 1) / 15);
        var centre = size / 2;
        var modules = new bool[size * size];

        // Data coordinates skip the reference grid lines of full-range symbols (every 16th row and column from the centre).
        var map = new int[baseSize];
        for (int p = 0, k = 0; p < size; p++)
        {
            if (compact || Math.Abs(p - centre) % 16 != 0) map[k++] = p;
        }

        // Each layer is a ring two modules thick, filled with dominoes (outer module first) anticlockwise from the
        // top-left corner: down the left side, along the bottom, up the right side, back along the top.
        var bit = 0;
        for (var layer = 0; layer < layers; layer++)
        {
            var side = (layers - layer) * 4 + (compact ? 9 : 12);
            int near = 2 * layer, far = baseSize - 1 - 2 * layer;
            for (var s = 0; s < 4; s++)
            {
                for (var j = 0; j < side; j++)
                {
                    for (var depth = 0; depth < 2; depth++)
                    {
                        var (x, y) = s switch
                        {
                            0 => (near + depth, near + j),
                            1 => (near + j, far - depth),
                            2 => (far - depth, far - j),
                            _ => (far - j, near + depth),
                        };

                        if (stream[bit++]) modules[map[y] * size + map[x]] = true;
                    }
                }
            }
        }

        DrawCore(modules, size, centre, compact, ModeMessage(compact, layers, dataWords.Count));

        if (!compact)
        {
            // Reference grid: dark on every other module along each grid line, in step with the centre.
            for (var offset = 0; centre - offset >= 0; offset += 16)
            {
                for (var p = 0; p < size; p++)
                {
                    if (((p - centre) & 1) != 0) continue;
                    modules[(centre - offset) * size + p] = true;
                    modules[(centre + offset) * size + p] = true;
                    modules[p * size + centre - offset] = true;
                    modules[p * size + centre + offset] = true;
                }
            }
        }

        return new BarcodeMatrix(size, size, modules, isLinear: false, QuietZone);
    }

    /// <summary>The mode message: layer and data word counts with Reed-Solomon check words over GF(16).</summary>
    private static bool[] ModeMessage(bool compact, int layers, int dataWords)
    {
        int[] nibbles;
        if (compact)
        {
            var value = ((layers - 1) << 6) | (dataWords - 1); // 2 + 6 bits
            nibbles = new int[7];
            nibbles[0] = value >> 4;
            nibbles[1] = value & 0xF;
            GaloisField.Aztec4.Encode(nibbles, 0, 2, 5);
        }
        else
        {
            var value = ((layers - 1) << 11) | (dataWords - 1); // 5 + 11 bits
            nibbles = new int[10];
            for (var i = 0; i < 4; i++) nibbles[i] = (value >> (12 - 4 * i)) & 0xF;
            GaloisField.Aztec4.Encode(nibbles, 0, 4, 6);
        }

        var bits = new bool[nibbles.Length * 4];
        for (var i = 0; i < bits.Length; i++) bits[i] = ((nibbles[i / 4] >> (3 - i % 4)) & 1) == 1;
        return bits;
    }

    /// <summary>Bullseye, orientation marks and the mode message around it.</summary>
    private static void DrawCore(bool[] modules, int size, int c, bool compact, bool[] modeMessage)
    {
        var ring = compact ? 5 : 7;
        void Set(int x, int y) => modules[y * size + x] = true;

        for (var y = c - ring + 1; y < c + ring; y++)
        {
            for (var x = c - ring + 1; x < c + ring; x++)
            {
                if (Math.Max(Math.Abs(x - c), Math.Abs(y - c)) % 2 == 0) Set(x, y);
            }
        }

        // Orientation: three dark modules at the top-left corner of the mode message ring, two at top-right, one at bottom-right.
        Set(c - ring, c - ring);
        Set(c - ring + 1, c - ring);
        Set(c - ring, c - ring + 1);
        Set(c + ring, c - ring);
        Set(c + ring, c - ring + 1);
        Set(c + ring, c + ring - 1);

        // Mode message bits run clockwise from the top-left, skipping the corners (and the centre grid line when full).
        var perSide = modeMessage.Length / 4;
        var positions = new List<int>(perSide);
        for (var d = -ring + 2; d <= ring - 2; d++)
        {
            if (!compact && d == 0) continue;
            positions.Add(d);
        }

        for (var i = 0; i < perSide; i++)
        {
            var d = positions[i];
            if (modeMessage[i]) Set(c + d, c - ring);                    // top, left to right
            if (modeMessage[perSide + i]) Set(c + ring, c + d);          // right, top to bottom
            if (modeMessage[2 * perSide + i]) Set(c - d, c + ring);      // bottom, right to left
            if (modeMessage[3 * perSide + i]) Set(c - ring, c - d);      // left, bottom to top
        }
    }

    /// <summary>
    /// Turns text into the Aztec bit stream with the fewest bits: a dynamic
    /// programme over positions and modes, where each step encodes one character
    /// (latched or shifted), a two-character punctuation pair, or a run of bytes
    /// in Binary Shift.
    /// </summary>
    internal static class ModeEncoder
    {
        private const int Upper = 0, Lower = 1, Mixed = 2, Punct = 3, Digit = 4, Modes = 5;
        private const int BinaryShift = 31;
        private const int Infinity = int.MaxValue / 4;

        // Character codes per mode (0 = not available). Index = byte value.
        private static readonly byte[][] Codes = BuildCodes();

        // Shortest latch sequence between modes: total bits and the (code, bits) steps.
        private static readonly (int Bits, (int Code, int Bits)[] Steps)[,] Latches = BuildLatches();

        private static byte[][] BuildCodes()
        {
            var codes = new byte[Modes][];
            for (var m = 0; m < Modes; m++) codes[m] = new byte[256];
            codes[Upper][' '] = 1;
            codes[Lower][' '] = 1;
            codes[Mixed][' '] = 1;
            codes[Digit][' '] = 1;
            for (var i = 0; i < 26; i++)
            {
                codes[Upper]['A' + i] = (byte)(2 + i);
                codes[Lower]['a' + i] = (byte)(2 + i);
            }

            for (var i = 0; i < 10; i++) codes[Digit]['0' + i] = (byte)(2 + i);
            codes[Digit][','] = 12;
            codes[Digit]['.'] = 13;

            // Mixed: control characters 1-13 and 27-31, then @ \ ^ _ ` | ~ DEL.
            for (var ch = 1; ch <= 13; ch++) codes[Mixed][ch] = (byte)(1 + ch);
            for (var ch = 27; ch <= 31; ch++) codes[Mixed][ch] = (byte)(ch - 12);
            const string mixed = "@\\^_`|~\u007f";
            for (var i = 0; i < mixed.Length; i++) codes[Mixed][mixed[i]] = (byte)(20 + i);

            // Punctuation: CR, then (codes 2-5 are pairs) ! " # $ % & ' ( ) * + , - . / : ; < = > ? [ ] { }.
            codes[Punct]['\r'] = 1;
            const string punct = "!\"#$%&'()*+,-./:;<=>?[]{}";
            for (var i = 0; i < punct.Length; i++) codes[Punct][punct[i]] = (byte)(6 + i);
            return codes;
        }

        private static int Bits(int mode) => mode == Digit ? 4 : 5;

        private static (int, (int, int)[])[,] BuildLatches()
        {
            // Direct latches: (from, to, code).
            var direct = new (int From, int To, int Code)[]
            {
                (Upper, Lower, 28), (Upper, Mixed, 29), (Upper, Digit, 30),
                (Lower, Mixed, 29), (Lower, Digit, 30),
                (Mixed, Lower, 28), (Mixed, Upper, 29), (Mixed, Punct, 30),
                (Punct, Upper, 31),
                (Digit, Upper, 14),
            };

            var result = new (int, (int, int)[])[Modes, Modes];
            for (var from = 0; from < Modes; from++)
            {
                // Breadth-first over latch steps, keeping the fewest bits to each mode.
                var cost = new int[Modes];
                var path = new List<(int, int)>[Modes];
                for (var m = 0; m < Modes; m++) cost[m] = Infinity;
                cost[from] = 0;
                path[from] = new List<(int, int)>();
                for (var round = 0; round < Modes; round++)
                {
                    foreach (var (f, t, code) in direct)
                    {
                        if (cost[f] == Infinity || cost[f] + Bits(f) >= cost[t]) continue;
                        cost[t] = cost[f] + Bits(f);
                        path[t] = new List<(int, int)>(path[f]) { (code, Bits(f)) };
                    }
                }

                for (var to = 0; to < Modes; to++) result[from, to] = (cost[to], path[to].ToArray());
            }

            return result;
        }

        private static int PairCode(byte a, byte b) => (a, b) switch
        {
            ((byte)'\r', (byte)'\n') => 2,
            ((byte)'.', (byte)' ') => 3,
            ((byte)',', (byte)' ') => 4,
            ((byte)':', (byte)' ') => 5,
            _ => 0,
        };

        // How a state was reached.
        private enum Step : byte { None, Latch, Char, ShiftPunct, ShiftUpper, Pair, ShiftPair, ShortRun, LongRunOpen, LongRunExtend, LongRunClose }

        public static List<bool> Encode(string value)
        {
            byte[] text;
            var eci = false;
            if (value.All(ch => ch <= 0xFF))
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

                eci = true;
            }

            var n = text.Length;
            var stride = Modes;
            // arrived[i, m]: best cost having encoded i bytes, in mode m, before latching.
            // ready[i, m]: the same after any latch, ready to encode byte i in mode m.
            // longRun[i, m]: inside a Binary Shift run of 32+ bytes opened from mode m.
            var arrived = Fill(n + 1);
            var ready = Fill(n + 1);
            var longRun = Fill(n + 1);
            var arrivedStep = new Step[(n + 1) * stride];
            var arrivedArg = new int[(n + 1) * stride];
            var readyFrom = new byte[(n + 1) * stride];
            var longStep = new Step[(n + 1) * stride];

            arrived[Upper] = 0;
            for (var i = 0; i <= n; i++)
            {
                for (var m = 0; m < Modes; m++)
                {
                    if (longRun[i * stride + m] < arrived[i * stride + m])
                    {
                        arrived[i * stride + m] = longRun[i * stride + m];
                        arrivedStep[i * stride + m] = Step.LongRunClose;
                    }
                }

                for (var to = 0; to < Modes; to++)
                {
                    for (var from = 0; from < Modes; from++)
                    {
                        var c = arrived[i * stride + from] + Latches[from, to].Bits;
                        if (c < ready[i * stride + to])
                        {
                            ready[i * stride + to] = c;
                            readyFrom[i * stride + to] = (byte)from;
                        }
                    }
                }

                if (i == n) break;
                var ch = text[i];
                var pair = i + 1 < n ? PairCode(ch, text[i + 1]) : 0;
                for (var m = 0; m < Modes; m++)
                {
                    var v = ready[i * stride + m];
                    if (v >= Infinity) continue;
                    void Relax(int at, int cost, Step step, int arg = 0)
                    {
                        if (cost < arrived[at * stride + m])
                        {
                            arrived[at * stride + m] = cost;
                            arrivedStep[at * stride + m] = step;
                            arrivedArg[at * stride + m] = arg;
                        }
                    }

                    if (Codes[m][ch] != 0) Relax(i + 1, v + Bits(m), Step.Char);
                    if (m != Punct && Codes[Punct][ch] != 0) Relax(i + 1, v + Bits(m) + 5, Step.ShiftPunct);
                    if (m is Lower or Digit && Codes[Upper][ch] != 0) Relax(i + 1, v + Bits(m) + 5, Step.ShiftUpper);
                    if (pair != 0) Relax(i + 2, v + Bits(m) + (m == Punct ? 0 : 5), m == Punct ? Step.Pair : Step.ShiftPair);
                    if (m is Upper or Lower or Mixed)
                    {
                        for (var len = 1; len <= 31 && i + len <= n; len++) Relax(i + len, v + 10 + 8 * len, Step.ShortRun, len);
                        if (i + 32 <= n && v + 21 + 8 * 32 < longRun[(i + 32) * stride + m])
                        {
                            longRun[(i + 32) * stride + m] = v + 21 + 8 * 32;
                            longStep[(i + 32) * stride + m] = Step.LongRunOpen;
                        }
                    }

                    if (longRun[i * stride + m] < Infinity && longRun[i * stride + m] + 8 < longRun[(i + 1) * stride + m])
                    {
                        longRun[(i + 1) * stride + m] = longRun[i * stride + m] + 8;
                        longStep[(i + 1) * stride + m] = Step.LongRunExtend;
                    }
                }
            }

            var best = 0;
            for (var m = 1; m < Modes; m++)
            {
                if (arrived[n * stride + m] < arrived[n * stride + best]) best = m;
            }

            // Walk back to collect the steps, then write them forwards.
            var steps = new List<(int Position, int Mode, Step Step, int Arg)>();
            int pos = n, mode = best;
            var inReady = false;
            while (pos > 0 || mode != Upper || inReady)
            {
                if (inReady)
                {
                    var from = readyFrom[pos * stride + mode];
                    if (from != mode) steps.Add((pos, from, Step.Latch, mode));
                    mode = from;
                    inReady = false;
                    continue;
                }

                var step = arrivedStep[pos * stride + mode];
                if (step == Step.LongRunClose)
                {
                    var end = pos;
                    while (longStep[pos * stride + mode] == Step.LongRunExtend) pos--;
                    pos -= 32;
                    steps.Add((pos, mode, Step.LongRunOpen, end - pos));
                }
                else
                {
                    var length = step switch
                    {
                        Step.Pair or Step.ShiftPair => 2,
                        Step.ShortRun => arrivedArg[pos * stride + mode],
                        _ => 1,
                    };
                    pos -= length;
                    steps.Add((pos, mode, step, length));
                }

                inReady = true;
            }

            var bits = new List<bool>(arrived[n * stride + best] + 32);
            if (eci)
            {
                // P/S, FLG(n) with two digits, then "26" in Digit-mode codes.
                Append(bits, 0, 5);
                Append(bits, 0, 5);
                Append(bits, 2, 3);
                Append(bits, 2 + 2, 4);
                Append(bits, 6 + 2, 4);
            }

            for (var s = steps.Count - 1; s >= 0; s--)
            {
                var (p, m, step, arg) = steps[s];
                switch (step)
                {
                    case Step.Latch:
                        foreach (var (code, count) in Latches[m, arg].Steps) Append(bits, code, count);
                        break;
                    case Step.Char:
                        Append(bits, Codes[m][text[p]], Bits(m));
                        break;
                    case Step.ShiftPunct:
                        Append(bits, 0, Bits(m));
                        Append(bits, Codes[Punct][text[p]], 5);
                        break;
                    case Step.ShiftUpper:
                        Append(bits, m == Digit ? 15 : 28, Bits(m));
                        Append(bits, Codes[Upper][text[p]], 5);
                        break;
                    case Step.Pair:
                        Append(bits, PairCode(text[p], text[p + 1]), 5);
                        break;
                    case Step.ShiftPair:
                        Append(bits, 0, Bits(m));
                        Append(bits, PairCode(text[p], text[p + 1]), 5);
                        break;
                    case Step.ShortRun:
                        Append(bits, BinaryShift, 5);
                        Append(bits, arg, 5);
                        for (var k = 0; k < arg; k++) Append(bits, text[p + k], 8);
                        break;
                    case Step.LongRunOpen:
                        Append(bits, BinaryShift, 5);
                        Append(bits, 0, 5);
                        Append(bits, arg - 31, 11);
                        for (var k = 0; k < arg; k++) Append(bits, text[p + k], 8);
                        break;
                }
            }

            return bits;
        }

        private static int[] Fill(int positions)
        {
            var array = new int[positions * Modes];
            for (var i = 0; i < array.Length; i++) array[i] = Infinity;
            return array;
        }
    }

    private static void Append(List<bool> bits, int value, int count)
    {
        for (var i = count - 1; i >= 0; i--) bits.Add(((value >> i) & 1) != 0);
    }
}
