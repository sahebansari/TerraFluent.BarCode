using System.Globalization;

namespace TerraFluent.Barcode.Encoding;

/// <summary>
/// The GS1 application identifiers and the format of their data, after the
/// GS1 General Specifications (section 3, "GS1 Application Identifiers"), so
/// that unknown AIs, impossible dates and over-long fields are caught before
/// a label is printed.
/// </summary>
/// <remarks>
/// A format is one or more space-separated parts, matched in order: <c>N</c>
/// (digits) or <c>X</c> (GS1 character set 82), then a fixed length (<c>N6</c>),
/// a maximum (<c>X..20</c>) or a range (<c>N6..12</c>). <c>:check</c> marks a
/// part ending in a GS1 modulo 10 check digit, <c>:date</c> a part starting
/// with a YYMMDD date (DD may be 00), and <c>:datetime</c> YYMMDDHHMM.
/// </remarks>
internal static class Gs1Ai
{
    private static readonly Dictionary<string, Part[]> Formats = Build();

    public sealed class Part
    {
        public Part(bool numeric, int min, int max, string? check)
        {
            Numeric = numeric;
            Min = min;
            Max = max;
            Check = check;
        }

        public bool Numeric { get; }
        public int Min { get; }
        public int Max { get; }
        public string? Check { get; }
    }

    /// <summary>The data format of <paramref name="ai"/>, or <c>null</c> if GS1 defines no such AI.</summary>
    public static Part[]? Find(string ai) => Formats.TryGetValue(ai, out var parts) ? parts : null;

    /// <summary>Checks <paramref name="data"/> against the format of <paramref name="ai"/>; returns an error, or <c>null</c> if it is valid.</summary>
    public static string? Validate(string ai, Part[] parts, string data)
    {
        var pos = 0;
        for (var i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            var remaining = data.Length - pos;
            var length = part.Min == part.Max ? part.Max : Math.Min(remaining, part.Max);
            if (remaining < part.Min || (part.Min == part.Max && remaining < length))
            {
                return Describe(ai, parts, data.Length);
            }

            var field = data.Substring(pos, length);
            if (part.Numeric && field.Any(c => c < '0' || c > '9'))
            {
                return string.Format(CultureInfo.InvariantCulture, "AI ({0}) data must be digits only{1}", ai, parts.Length > 1 ? " in its first " + (pos + length) + " characters" : string.Empty);
            }

            switch (part.Check)
            {
                case "check":
                    var expected = EanUpcEncoder.CheckDigit(field, field.Length - 1);
                    if (field[field.Length - 1] != expected)
                    {
                        return $"AI ({ai}) check digit '{field[field.Length - 1]}' should be '{expected}'";
                    }

                    break;
                case "date":
                case "datetime":
                    if (!IsDate(field, part.Check == "datetime"))
                    {
                        return $"AI ({ai}) date '{field.Substring(0, Math.Min(field.Length, part.Check == "datetime" ? 10 : 6))}' is not a valid {(part.Check == "datetime" ? "YYMMDDHHMM date and time" : "YYMMDD date")}";
                    }

                    break;
            }

            pos += length;
        }

        return pos == data.Length ? null : Describe(ai, parts, data.Length);
    }

    private static string Describe(string ai, Part[] parts, int got)
    {
        int min = parts.Sum(p => p.Min), max = parts.Sum(p => p.Max);
        return min == max
            ? string.Format(CultureInfo.InvariantCulture, "AI ({0}) needs exactly {1} characters of data; got {2}", ai, max, got)
            : string.Format(CultureInfo.InvariantCulture, "AI ({0}) needs {1} to {2} characters of data; got {3}", ai, Math.Max(1, min), max, got);
    }

    private static bool IsDate(string s, bool withTime)
    {
        int Two(int i) => (s[i] - '0') * 10 + (s[i + 1] - '0');
        int month = Two(2), day = Two(4);
        if (month < 1 || month > 12) return false;
        int[] days = { 31, 29, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 }; // the century is not encoded, so 29 February is allowed
        if (day > days[month - 1]) return false;
        return !withTime || (Two(6) <= 23 && Two(8) <= 59);
    }

    private static Dictionary<string, Part[]> Build()
    {
        var table = new Dictionary<string, Part[]>(StringComparer.Ordinal);
        void Add(string format, params string[] ais)
        {
            var parts = format.Split(' ').Select(ParsePart).ToArray();
            foreach (var ai in ais) table.Add(ai, parts);
        }

        // AIs written as "310n" stand for 3100 to 3105 (decimal point position), "390n" for 3900 to 3909.
        void AddN(string format, int lastN, params string[] prefixes)
        {
            foreach (var prefix in prefixes)
            {
                for (var n = 0; n <= lastN; n++) Add(format, prefix + n.ToString(CultureInfo.InvariantCulture));
            }
        }

        static string[] Range(int from, int to) =>
            Enumerable.Range(from, to - from + 1).Select(i => i.ToString(CultureInfo.InvariantCulture)).ToArray();

        // Identification keys and product data.
        Add("N18:check", "00");
        Add("N14:check", "01", "02");
        Add("X..20", "10", "21", "22");
        Add("N6:date", "11", "12", "13", "15", "16", "17");
        Add("N2", "20");
        Add("X..28", "235");
        Add("X..30", "240", "241", "250", "251");
        Add("N..6", "242");
        Add("X..20", "243", "254");
        Add("N13:check X..17", "253");
        Add("N13:check N..12", "255");
        Add("N..8", "30", "37");

        // Trade and logistic measures: six digits with the decimal point position in the fourth AI digit.
        AddN("N6", 5, Range(310, 316).Concat(Range(320, 337)).Concat(Range(340, 357)).Concat(Range(360, 369)).ToArray());

        // Amounts and prices.
        AddN("N..15", 9, "390", "392");
        AddN("N3 N..15", 9, "391", "393");
        AddN("N4", 9, "394");
        AddN("N6", 9, "395");

        // Shipments, locations and parties.
        Add("X..30", "400", "401", "403");
        Add("N17:check", "402");
        Add("N13:check", Range(410, 417));
        Add("X..20", "420");
        Add("N3 X..9", "421");
        Add("N3", "422", "424", "426");
        Add("N3 N..12", "423", "425");
        Add("X..3", "427");

        // Ship-to and return-to addresses.
        Add("X..35", "4300", "4301", "4310", "4311", "4320");
        Add("X..70", "4302", "4303", "4304", "4305", "4306", "4312", "4313", "4314", "4315", "4316");
        Add("X2", "4307", "4317");
        Add("X..30", "4308", "4319");
        Add("X..20", "4318");
        Add("N10", "4309");
        Add("N1", "4321", "4322", "4323");
        Add("N10:datetime", "4324", "4325");
        Add("N6:date", "4326");
        Add("N6 X..1", "4330", "4331", "4332", "4333");

        // Additional product and logistics data.
        Add("N13", "7001");
        Add("X..30", "7002", "7023");
        Add("N10:datetime", "7003");
        Add("N..4", "7004");
        Add("X..12", "7005");
        Add("N6:date", "7006");
        Add("N6..12:date", "7007");
        Add("X..3", "7008");
        Add("X..10", "7009", "7255");
        Add("X..2", "7010");
        Add("N6:date N..4", "7011");
        Add("X..20", "7020", "7021", "7022", "7240");
        Add("N3 X..27", Range(7030, 7039));
        Add("X4", "7040");
        Add("X..20", Range(710, 716));
        Add("X2 X..28", Range(7230, 7239));
        Add("N2", "7241");
        Add("X..25", "7242");
        Add("N8", "7250");
        Add("N12", "7251");
        Add("N1", "7252");
        Add("X..40", "7253", "7254", "7259");
        Add("X..90", "7256");
        Add("X..70", "7257");
        Add("X3", "7258");

        // Assets, services, coupons and the like.
        Add("N14", "8001");
        Add("X..20", "8002", "8012");
        Add("N14:check X..16", "8003");
        Add("X..30", "8004", "8010");
        Add("N6", "8005");
        Add("N14:check N2 N2", "8006", "8026");
        Add("X..34", "8007");
        Add("N8 N..4", "8008");
        Add("X..50", "8009");
        Add("N..12", "8011");
        Add("X..25", "8013", "8020");
        Add("N18:check", "8017", "8018");
        Add("N..10", "8019");
        Add("X..90", "8030");
        Add("X..70", "8110", "8112", "8200");
        Add("N4", "8111");

        // Company internal information.
        Add("X..30", "90");
        Add("X..90", Range(91, 99));
        return table;
    }

    private static Part ParsePart(string text)
    {
        var colon = text.IndexOf(':');
        var check = colon < 0 ? null : text.Substring(colon + 1);
        var spec = colon < 0 ? text.Substring(1) : text.Substring(1, colon - 1);
        int min, max;
        var dots = spec.IndexOf("..", StringComparison.Ordinal);
        if (dots < 0)
        {
            min = max = int.Parse(spec, CultureInfo.InvariantCulture);
        }
        else
        {
            min = dots == 0 ? 0 : int.Parse(spec.Substring(0, dots), CultureInfo.InvariantCulture);
            max = int.Parse(spec.Substring(dots + 2), CultureInfo.InvariantCulture);
        }

        return new Part(text[0] == 'N', min, max, check);
    }
}
