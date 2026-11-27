using System.Globalization;
using System.Text;

namespace TerraFluent.BarCode.Encoding;

/// <summary>
/// Parses GS1 element strings written with application identifiers in
/// parentheses, for example <c>(01)09501101530003(17)250101(10)ABC123</c>.
/// </summary>
internal static class Gs1
{
    /// <summary>GS1-128 allows at most 48 data characters, not counting FNC1.</summary>
    public const int MaxDataLength = 48;

    // Total length (AI digits plus data) of AIs whose first two digits fix the length (GS1 General Specifications, figure 7.8.5-2).
    private static readonly Dictionary<string, int> PredefinedLength = new()
    {
        ["00"] = 20, ["01"] = 16, ["02"] = 16, ["03"] = 16, ["04"] = 18,
        ["11"] = 8, ["12"] = 8, ["13"] = 8, ["14"] = 8, ["15"] = 8, ["16"] = 8, ["17"] = 8, ["18"] = 8, ["19"] = 8,
        ["20"] = 4, ["31"] = 10, ["32"] = 10, ["33"] = 10, ["34"] = 10, ["35"] = 10, ["36"] = 10, ["41"] = 16,
    };

    // GS1 AI encodable character set 82.
    private const string Charset82 = "!\"%&'()*+,-./0123456789:;<=>?ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz";

    /// <summary>The parsed element string.</summary>
    public sealed class Result
    {
        public Result(string encoded, string humanReadable)
        {
            Encoded = encoded;
            HumanReadable = humanReadable;
        }

        /// <summary>AIs and data with <see cref="Code128Encoder.Fnc1"/> at the start and after each variable-length field.</summary>
        public string Encoded { get; }

        /// <summary>The value with AIs in parentheses, as printed under the barcode.</summary>
        public string HumanReadable { get; }
    }

    public static Result Parse(string value, string paramName)
    {
        var elements = new List<(string Ai, string Data)>();
        var pos = 0;
        while (pos < value.Length)
        {
            if (value[pos] != '(') throw Error($"expected '(' at index {pos}", paramName);
            var close = value.IndexOf(')', pos + 1);
            if (close < 0) throw Error($"the application identifier starting at index {pos} has no closing ')'", paramName);
            var ai = value.Substring(pos + 1, close - pos - 1);
            if (ai.Length < 2 || ai.Length > 4 || ai.Any(c => c < '0' || c > '9'))
            {
                throw Error($"'{ai}' is not an application identifier (2 to 4 digits)", paramName);
            }

            var next = value.IndexOf('(', close + 1);
            if (next < 0) next = value.Length;
            var data = value.Substring(close + 1, next - close - 1);
            if (data.Length == 0) throw Error($"AI ({ai}) has no data", paramName);
            foreach (var c in data)
            {
                if (Charset82.IndexOf(c) < 0) throw Error($"'{c}' in AI ({ai}) is not in the GS1 character set", paramName);
            }

            if (PredefinedLength.TryGetValue(ai.Substring(0, 2), out var total))
            {
                if (ai.Length + data.Length != total)
                {
                    throw Error($"AI ({ai}) needs exactly {total - ai.Length} characters of data; got {data.Length}", paramName);
                }

                if (data.Any(c => c < '0' || c > '9')) throw Error($"AI ({ai}) data must be digits only", paramName);
                if (HasCheckDigit(ai))
                {
                    var expected = EanUpcEncoder.CheckDigit(data.Substring(0, data.Length - 1));
                    if (data[data.Length - 1] != expected)
                    {
                        throw Error($"AI ({ai}) check digit '{data[data.Length - 1]}' should be '{expected}'", paramName);
                    }
                }
            }

            elements.Add((ai, data));
            pos = next;
        }

        if (elements.Count == 0) throw Error("no application identifiers found", paramName);

        var encoded = new StringBuilder().Append(Code128Encoder.Fnc1);
        var readable = new StringBuilder();
        var dataLength = 0;
        for (var i = 0; i < elements.Count; i++)
        {
            var (ai, data) = elements[i];
            encoded.Append(ai).Append(data);
            readable.Append('(').Append(ai).Append(')').Append(data);
            dataLength += ai.Length + data.Length;
            var fixedLength = PredefinedLength.ContainsKey(ai.Substring(0, 2));
            if (!fixedLength && i < elements.Count - 1)
            {
                encoded.Append(Code128Encoder.Fnc1);
                dataLength++; // the separator counts towards the symbol's data characters
            }
        }

        if (dataLength > MaxDataLength)
        {
            throw new ArgumentException(
                string.Format(CultureInfo.InvariantCulture, "GS1-128 holds at most {0} data characters; this value needs {1}. Split it over two symbols.", MaxDataLength, dataLength),
                paramName);
        }

        return new Result(encoded.ToString(), readable.ToString());
    }

    // SSCC, GTIN and GLN fields end in a GS1 modulo 10 check digit.
    private static bool HasCheckDigit(string ai) => ai is "00" or "01" or "02" || (ai.Length == 3 && ai.StartsWith("41", StringComparison.Ordinal));

    private static ArgumentException Error(string detail, string paramName) =>
        new($"Invalid GS1 element string: {detail}. Write it as (AI)data(AI)data, for example (01)09501101530003(17)250101.", paramName);
}
