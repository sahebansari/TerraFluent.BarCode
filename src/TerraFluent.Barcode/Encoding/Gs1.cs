using System.Globalization;
using System.Text;

namespace TerraFluent.Barcode.Encoding;

/// <summary>
/// Parses GS1 element strings written with application identifiers in
/// parentheses, for example <c>(01)09501101530003(17)250101(10)ABC123</c>.
/// </summary>
internal static class Gs1
{
    /// <summary>GS1-128 allows at most 48 data characters, not counting FNC1.</summary>
    public const int MaxDataLength = 48;

    // AIs whose first two digits fix the length need no FNC1 after them (GS1 General Specifications, figure 7.8.5-2).
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

    /// <summary>Which symbology the element string is for; it sets the length limit.</summary>
    public enum Symbology { Gs1Code128, DataBarExpanded }

    /// <summary>Parses an element string written as <c>(AI)data(AI)data</c>.</summary>
    public static Result Parse(string value, string paramName, Symbology symbology = Symbology.Gs1Code128) =>
        Build(Split(value, paramName), paramName, symbology);

    /// <summary>Splits an element string written as <c>(AI)data(AI)data</c> into AI and data pairs, without validating them.</summary>
    public static List<(string Ai, string Data)> Split(string value, string paramName)
    {
        var elements = new List<(string Ai, string Data)>();
        var pos = 0;
        while (pos < value.Length)
        {
            if (value[pos] != '(') throw Error($"expected '(' at index {pos}", paramName);
            var close = value.IndexOf(')', pos + 1);
            if (close < 0) throw Error($"the application identifier starting at index {pos} has no closing ')'", paramName);
            var ai = value.Substring(pos + 1, close - pos - 1);
            var next = value.IndexOf('(', close + 1);
            if (next < 0) next = value.Length;
            elements.Add((ai, value.Substring(close + 1, next - close - 1)));
            pos = next;
        }

        return elements;
    }

    /// <summary>
    /// Validates AI and data pairs and builds the encoded and printed forms. Data
    /// may contain parentheses here, which the element-string syntax cannot express.
    /// </summary>
    public static Result Build(IReadOnlyList<(string Ai, string Data)> elements, string paramName, Symbology symbology = Symbology.Gs1Code128)
    {
        foreach (var (ai, data) in elements)
        {
            Validate(ai, data, paramName);
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

        // DataBar Expanded's limit depends on the data's mix of digits and letters; its encoder checks it.
        if (symbology == Symbology.Gs1Code128 && dataLength > MaxDataLength)
        {
            throw new ArgumentException(
                string.Format(CultureInfo.InvariantCulture, "GS1-128 holds at most {0} data characters; this value needs {1}. Split it over two symbols.", MaxDataLength, dataLength),
                paramName);
        }

        return new Result(encoded.ToString(), readable.ToString());
    }

    /// <summary>Checks that <paramref name="ai"/> is a GS1 application identifier and <paramref name="data"/> matches its format.</summary>
    public static void Validate(string ai, string data, string paramName)
    {
        if (ai is null || ai.Length < 2 || ai.Length > 4 || ai.Any(c => c < '0' || c > '9'))
        {
            throw Error($"'{ai}' is not an application identifier (2 to 4 digits)", paramName);
        }

        var format = Gs1Ai.Find(ai) ?? throw Error($"({ai}) is not a GS1 application identifier", paramName);
        if (string.IsNullOrEmpty(data)) throw Error($"AI ({ai}) has no data", paramName);
        foreach (var c in data)
        {
            if (Charset82.IndexOf(c) < 0) throw Error($"'{c}' in AI ({ai}) is not in the GS1 character set", paramName);
        }

        if (Gs1Ai.Validate(ai, format, data) is { } problem) throw Error(problem, paramName);
    }

    private static ArgumentException Error(string detail, string paramName) =>
        new($"Invalid GS1 element string: {detail}. Write it as (AI)data(AI)data, for example (01)09501101530003(17)250101.", paramName);
}
