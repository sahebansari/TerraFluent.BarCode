using System.Globalization;
using System.Text;
using TerraFluent.Barcode.Compatibility;
using TerraFluent.Barcode.Encoding;

namespace TerraFluent.Barcode;

/// <summary>
/// Builds the text that phone cameras and scanner apps act on: a contact to
/// save, a Wi-Fi network to join, a link, an email, a text message, a call or
/// a GS1 Digital Link. Each helper validates its input and escapes it the way
/// the format requires, then returns a string to pass to
/// <see cref="Barcodes.QrCode"/> (or any other 2D symbology):
/// <code>
/// byte[] png = Barcodes.QrCode(QrPayload.Wifi("Home", "correct horse battery")).ToPng();
/// string svg = Barcodes.QrCode(QrPayload.Email("hello@example.com", subject: "Hi")).ToSvg();
/// string url = QrPayload.Gs1DigitalLink(("01", "09501101530003"), ("10", "ABC123"));
/// </code>
/// </summary>
public static class QrPayload
{
    /// <summary>The resolver GS1 runs, used by <see cref="Gs1DigitalLink(ValueTuple{string, string}[])"/> when no domain is given.</summary>
    public const string Gs1ResolverDomain = "https://id.gs1.org";

    private const string Hex = "0123456789ABCDEF";

    /// <summary>
    /// A link to a web page. A value without a scheme, such as
    /// <c>terrafluent.dev/barcode</c>, gets <c>https://</c> in front; otherwise
    /// the scheme must be <c>http</c> or <c>https</c>.
    /// </summary>
    /// <exception cref="ArgumentException">The value is not an absolute web address or contains spaces.</exception>
    public static string Url(string url)
    {
        Guard.NotNullOrEmpty(url, nameof(url));
        var value = url.Trim();
        RejectWhiteSpace(value, "A URL", nameof(url));
        if (!HasScheme(value)) value = "https://" + value;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || uri.Host.Length == 0)
        {
            throw new ArgumentException($"'{url}' is not an http or https URL.", nameof(url));
        }

        return value;
    }

    /// <summary>
    /// A <c>mailto:</c> link (RFC 6068) that opens a new email to
    /// <paramref name="address"/>, with an optional subject and body. Both are
    /// percent-encoded as UTF-8, and line breaks in the body become CRLF.
    /// </summary>
    /// <exception cref="ArgumentException">The address is not a single email address.</exception>
    public static string Email(string address, string? subject = null, string? body = null)
    {
        var to = ValidateEmail(address, nameof(address));
        var sb = new StringBuilder("mailto:");
        AppendPercentEncoded(sb, to, "@!$'*=");
        var separator = '?';
        if (!string.IsNullOrEmpty(subject))
        {
            sb.Append(separator).Append("subject=");
            AppendPercentEncoded(sb, subject!, null);
            separator = '&';
        }

        if (!string.IsNullOrEmpty(body))
        {
            sb.Append(separator).Append("body=");
            AppendPercentEncoded(sb, NormalizeNewLines(body!, "\r\n"), null);
        }

        return sb.ToString();
    }

    /// <summary>
    /// A <c>tel:</c> link that offers to call <paramref name="number"/>. Spaces,
    /// dashes, dots and parentheses are removed; a leading <c>+</c> for the
    /// international form is kept.
    /// </summary>
    /// <exception cref="ArgumentException">The number has characters other than digits and those separators.</exception>
    public static string Phone(string number) => "tel:" + NormalizePhone(number, nameof(number));

    /// <summary>
    /// An <c>SMSTO:</c> payload that opens a text message to
    /// <paramref name="number"/>, with <paramref name="message"/> filled in if
    /// given. The number is normalized as in <see cref="Phone"/>.
    /// </summary>
    /// <exception cref="ArgumentException">The number has characters other than digits and separators.</exception>
    public static string Sms(string number, string? message = null)
    {
        var to = NormalizePhone(number, nameof(number));
        return string.IsNullOrEmpty(message) ? "SMSTO:" + to : "SMSTO:" + to + ":" + message;
    }

    /// <summary>
    /// A Wi-Fi network that phones offer to join, in the <c>WIFI:</c> format
    /// that Android and iOS cameras read. Backslash, semicolon, comma, colon and
    /// double quote in the network name and password are escaped.
    /// </summary>
    /// <param name="ssid">The network name, up to 32 bytes as UTF-8.</param>
    /// <param name="password">
    /// The passphrase: 8 to 63 characters (or 64 hex digits) for
    /// <see cref="WifiSecurity.Wpa"/>; 5 or 13 characters (or 10 or 26 hex
    /// digits) for <see cref="WifiSecurity.Wep"/>; <c>null</c> or empty for
    /// <see cref="WifiSecurity.None"/>.
    /// </param>
    /// <param name="security">How the network is secured.</param>
    /// <param name="hidden"><c>true</c> if the network does not broadcast its name.</param>
    /// <exception cref="ArgumentException">The name or password does not suit the security type.</exception>
    public static string Wifi(string ssid, string? password, WifiSecurity security = WifiSecurity.Wpa, bool hidden = false)
    {
        Guard.NotNullOrEmpty(ssid, nameof(ssid));
        if (System.Text.Encoding.UTF8.GetByteCount(ssid) > 32)
        {
            throw new ArgumentException("A Wi-Fi network name is at most 32 bytes as UTF-8.", nameof(ssid));
        }

        RejectControl(ssid, "A Wi-Fi network name", nameof(ssid));
        string type;
        switch (security)
        {
            case WifiSecurity.Wpa:
                type = "WPA";
                if (string.IsNullOrEmpty(password)) throw new ArgumentException("A WPA network needs a password.", nameof(password));
                if (!(password!.Length is >= 8 and <= 63 || (password.Length == 64 && IsHex(password))))
                {
                    throw new ArgumentException($"A WPA password is 8 to 63 characters or 64 hex digits (got {password.Length} characters).", nameof(password));
                }

                break;
            case WifiSecurity.Wep:
                type = "WEP";
                if (string.IsNullOrEmpty(password)) throw new ArgumentException("A WEP network needs a password.", nameof(password));
                if (!(password!.Length is 5 or 13 || (password.Length is 10 or 26 && IsHex(password))))
                {
                    throw new ArgumentException($"A WEP key is 5 or 13 characters, or 10 or 26 hex digits (got {password.Length} characters).", nameof(password));
                }

                break;
            case WifiSecurity.None:
                type = "nopass";
                if (!string.IsNullOrEmpty(password)) throw new ArgumentException("An open network has no password; pass null or choose WifiSecurity.Wpa.", nameof(password));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(security), security, "Unknown Wi-Fi security type.");
        }

        if (password is not null) RejectControl(password, "A Wi-Fi password", nameof(password));

        var sb = new StringBuilder("WIFI:T:").Append(type).Append(";S:");
        AppendWifiEscaped(sb, ssid);
        sb.Append(';');
        if (security != WifiSecurity.None)
        {
            sb.Append("P:");
            AppendWifiEscaped(sb, password!);
            sb.Append(';');
        }

        if (hidden) sb.Append("H:true;");
        return sb.Append(';').ToString();
    }

    /// <summary>
    /// A vCard 3.0 contact (RFC 2426) that phones offer to save. Text is
    /// escaped as vCard requires (backslash, comma, semicolon and line breaks),
    /// lines end in CRLF, and properties left <c>null</c> or empty are omitted.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="contact"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">The contact has no name or organization, or an email, website or phone number is not valid.</exception>
    public static string Contact(ContactCard contact)
    {
        if (contact is null) throw new ArgumentNullException(nameof(contact));
        var first = Trimmed(contact.FirstName);
        var last = Trimmed(contact.LastName);
        var org = Trimmed(contact.Organization);
        if (first is null && last is null && org is null)
        {
            throw new ArgumentException("A contact needs a first name, last name or organization.", nameof(contact));
        }

        var sb = new StringBuilder("BEGIN:VCARD\r\nVERSION:3.0\r\n");
        sb.Append("N:");
        AppendVCardEscaped(sb, last);
        sb.Append(';');
        AppendVCardEscaped(sb, first);
        sb.Append(";;;\r\n");

        sb.Append("FN:");
        AppendVCardEscaped(sb, first is null || last is null ? first ?? last ?? org : first + " " + last);
        sb.Append("\r\n");

        AppendProperty(sb, "ORG", org);
        AppendProperty(sb, "TITLE", Trimmed(contact.JobTitle));
        AppendPhone(sb, "TEL;TYPE=VOICE", contact.Phone, nameof(ContactCard.Phone));
        AppendPhone(sb, "TEL;TYPE=CELL", contact.MobilePhone, nameof(ContactCard.MobilePhone));
        AppendPhone(sb, "TEL;TYPE=WORK,VOICE", contact.WorkPhone, nameof(ContactCard.WorkPhone));
        if (Trimmed(contact.Email) is { } email)
        {
            sb.Append("EMAIL;TYPE=INTERNET:").Append(ValidateEmail(email, nameof(ContactCard.Email))).Append("\r\n");
        }

        if (Trimmed(contact.Website) is { } website)
        {
            sb.Append("URL:").Append(Url(website)).Append("\r\n");
        }

        var street = Trimmed(contact.Street);
        var city = Trimmed(contact.City);
        var region = Trimmed(contact.Region);
        var postal = Trimmed(contact.PostalCode);
        var country = Trimmed(contact.Country);
        if (street is not null || city is not null || region is not null || postal is not null || country is not null)
        {
            // ADR: post office box; extended address; street; locality; region; postal code; country.
            sb.Append("ADR:;;");
            AppendVCardEscaped(sb, street);
            sb.Append(';');
            AppendVCardEscaped(sb, city);
            sb.Append(';');
            AppendVCardEscaped(sb, region);
            sb.Append(';');
            AppendVCardEscaped(sb, postal);
            sb.Append(';');
            AppendVCardEscaped(sb, country);
            sb.Append("\r\n");
        }

        if (contact.Birthday is { } birthday)
        {
            sb.Append("BDAY:").Append(birthday.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append("\r\n");
        }

        AppendProperty(sb, "NOTE", Trimmed(contact.Note));
        return sb.Append("END:VCARD").ToString();
    }

    /// <summary>
    /// A GS1 Digital Link URI on GS1's resolver (<see cref="Gs1ResolverDomain"/>)
    /// from an element string with AIs in parentheses, for example
    /// <c>(01)09501101530003(10)ABC123(17)251231</c>. See
    /// <see cref="Gs1DigitalLink(Uri, ValueTuple{string, string}[])"/> for the rules.
    /// </summary>
    public static string Gs1DigitalLink(string elementString) =>
        Gs1DigitalLink(new Uri(Gs1ResolverDomain), Gs1.Split(Guard.NotNullOrEmpty(elementString, nameof(elementString)), nameof(elementString)).ToArray());

    /// <summary>
    /// A GS1 Digital Link URI on GS1's resolver (<see cref="Gs1ResolverDomain"/>)
    /// from application identifier and data pairs. See
    /// <see cref="Gs1DigitalLink(Uri, ValueTuple{string, string}[])"/> for the rules.
    /// </summary>
    public static string Gs1DigitalLink(params (string Ai, string Data)[] elements) =>
        Gs1DigitalLink(new Uri(Gs1ResolverDomain), elements);

    /// <summary>
    /// A GS1 Digital Link URI on your own <paramref name="domain"/>, from an
    /// element string with AIs in parentheses. See
    /// <see cref="Gs1DigitalLink(Uri, ValueTuple{string, string}[])"/> for the rules.
    /// </summary>
    public static string Gs1DigitalLink(Uri domain, string elementString) =>
        Gs1DigitalLink(domain, Gs1.Split(Guard.NotNullOrEmpty(elementString, nameof(elementString)), nameof(elementString)).ToArray());

    /// <summary>
    /// A GS1 Digital Link URI (GS1 Digital Link standard, uncompressed form) on
    /// <paramref name="domain"/>, for example
    /// <c>https://example.com/01/09501101530003/10/ABC123?17=251231</c>.
    /// </summary>
    /// <remarks>
    /// Exactly one element must be a primary key: GTIN (01), SSCC (00), GLN (414)
    /// and the other GS1 keys. Its key qualifiers, such as CPV (22), batch (10)
    /// and serial (21) for a GTIN, go in the path in the order the standard
    /// sets, whatever order they are given in; every other AI becomes a query
    /// parameter. A GTIN of 8, 12 or 13 digits is padded to 14. Each AI's data is
    /// checked as for GS1-128, and characters outside the URI unreserved set
    /// are percent-encoded.
    /// </remarks>
    /// <exception cref="ArgumentException">An AI is unknown or its data is invalid, there is not exactly one primary key, an AI repeats, or the domain is not an http or https address.</exception>
    public static string Gs1DigitalLink(Uri domain, params (string Ai, string Data)[] elements)
    {
        if (domain is null) throw new ArgumentNullException(nameof(domain));
        if (elements is null) throw new ArgumentNullException(nameof(elements));
        if (!domain.IsAbsoluteUri || (domain.Scheme != Uri.UriSchemeHttp && domain.Scheme != Uri.UriSchemeHttps)
            || domain.Query.Length > 0 || domain.Fragment.Length > 0)
        {
            throw new ArgumentException("The domain must be an http or https address with no query or fragment, for example https://example.com.", nameof(domain));
        }

        if (elements.Length == 0) throw new ArgumentException("At least one application identifier is needed.", nameof(elements));

        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        var order = new List<string>(elements.Length);
        string? key = null;
        foreach (var (ai, rawData) in elements)
        {
            var data = rawData;
            if (ai == "01" && data is { Length: 8 or 12 or 13 } && data.All(c => c is >= '0' and <= '9'))
            {
                data = data.PadLeft(14, '0');
            }

            Gs1.Validate(ai, data, nameof(elements));
            if (values.ContainsKey(ai)) throw new ArgumentException($"AI ({ai}) appears more than once.", nameof(elements));
            values.Add(ai, data);
            order.Add(ai);
            if (DigitalLinkQualifiers.ContainsKey(ai))
            {
                if (key is not null) throw new ArgumentException($"A Digital Link has one primary key; ({key}) and ({ai}) are both keys.", nameof(elements));
                key = ai;
            }
        }

        if (key is null)
        {
            throw new ArgumentException("A Digital Link needs a primary key such as GTIN (01), SSCC (00) or GLN (414).", nameof(elements));
        }

        // Pick the qualifier path that holds every qualifier given; the rest are data attributes.
        var sequences = DigitalLinkQualifiers[key];
        string[] path = [];
        foreach (var sequence in sequences)
        {
            if (sequence.Count(values.ContainsKey) > path.Count(values.ContainsKey)) path = sequence;
        }

        foreach (var sequence in sequences)
        {
            foreach (var qualifier in sequence)
            {
                if (values.ContainsKey(qualifier) && Array.IndexOf(path, qualifier) < 0)
                {
                    throw new ArgumentException($"AI ({qualifier}) cannot qualify ({key}) together with ({string.Join(")(", path.Where(values.ContainsKey))}).", nameof(elements));
                }
            }
        }

        var sb = new StringBuilder(domain.AbsoluteUri.TrimEnd('/'));
        AppendPathSegment(sb, key, values[key]);
        foreach (var qualifier in path)
        {
            if (values.TryGetValue(qualifier, out var data)) AppendPathSegment(sb, qualifier, data);
        }

        var separator = '?';
        foreach (var ai in order)
        {
            if (ai == key || Array.IndexOf(path, ai) >= 0) continue;
            sb.Append(separator).Append(ai).Append('=');
            AppendPercentEncoded(sb, values[ai], null);
            separator = '&';
        }

        return sb.ToString();
    }

    // GS1 Digital Link primary keys and the key qualifiers each may carry, in path order.
    // A key with several sequences takes qualifiers from one of them only.
    private static readonly Dictionary<string, string[][]> DigitalLinkQualifiers = new(StringComparer.Ordinal)
    {
        ["00"] = [], // SSCC
        ["01"] = [["22", "10", "21"]], // GTIN: CPV, batch/lot, serial
        ["253"] = [], // GDTI
        ["255"] = [], // GCN
        ["401"] = [], // GINC
        ["402"] = [], // GSIN
        ["414"] = [["254"], ["7040"]], // GLN: extension or UIC with extension
        ["417"] = [["7040"]], // party GLN
        ["8003"] = [], // GRAI
        ["8004"] = [], // GIAI
        ["8006"] = [["22", "10", "21"]], // ITIP
        ["8010"] = [["8011"]], // CPID: serial
        ["8013"] = [], // GMN
        ["8017"] = [["8019"]], // GSRN provider: service relation instance
        ["8018"] = [["8019"]], // GSRN recipient: service relation instance
    };

    private static void AppendPathSegment(StringBuilder sb, string ai, string data)
    {
        sb.Append('/').Append(ai).Append('/');
        AppendPercentEncoded(sb, data, null);
    }

    private static bool HasScheme(string value)
    {
        // scheme "://" per RFC 3986: a letter, then letters, digits, '+', '-' or '.'.
        var colon = value.IndexOf("://", StringComparison.Ordinal);
        if (colon <= 0 || !IsAsciiLetter(value[0])) return false;
        for (var i = 1; i < colon; i++)
        {
            var c = value[i];
            if (!IsAsciiLetter(c) && c is not (>= '0' and <= '9') and not '+' and not '-' and not '.') return false;
        }

        return true;
    }

    private static bool IsAsciiLetter(char c) => c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z');

    private static bool IsHex(string value) => value.All(c => c is (>= '0' and <= '9') or (>= 'a' and <= 'f') or (>= 'A' and <= 'F'));

    private static string? Trimmed(string? value)
    {
        if (value is null) return null;
        var trimmed = value.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }

    private static void RejectWhiteSpace(string value, string what, string paramName)
    {
        if (value.Any(c => char.IsWhiteSpace(c) || char.IsControl(c)))
        {
            throw new ArgumentException($"{what} cannot contain spaces or control characters.", paramName);
        }
    }

    private static void RejectControl(string value, string what, string paramName)
    {
        if (value.Any(char.IsControl))
        {
            throw new ArgumentException($"{what} cannot contain control characters or line breaks.", paramName);
        }
    }

    private static string ValidateEmail(string address, string paramName)
    {
        Guard.NotNullOrEmpty(address, paramName);
        var value = address.Trim();
        RejectWhiteSpace(value, "An email address", paramName);
        var at = value.LastIndexOf('@');
        if (at <= 0 || at == value.Length - 1 || value.IndexOf('@') != at || value.Length > 254 || value.IndexOf(',') >= 0)
        {
            throw new ArgumentException($"'{address}' is not an email address.", paramName);
        }

        return value;
    }

    private static string NormalizePhone(string number, string paramName)
    {
        Guard.NotNullOrEmpty(number, paramName);
        var sb = new StringBuilder(number.Length);
        var digits = 0;
        foreach (var c in number.Trim())
        {
            if (c is >= '0' and <= '9')
            {
                sb.Append(c);
                digits++;
            }
            else if (c == '+' && sb.Length == 0)
            {
                sb.Append(c);
            }
            else if (c is not (' ' or '-' or '.' or '(' or ')' or ' '))
            {
                throw new ArgumentException($"'{number}' is not a phone number: use digits, an optional leading '+', and spaces, dashes, dots or parentheses.", paramName);
            }
        }

        if (digits is < 3 or > 20)
        {
            throw new ArgumentException($"'{number}' is not a phone number: it has {digits} digits.", paramName);
        }

        return sb.ToString();
    }

    private static void AppendPhone(StringBuilder sb, string property, string? number, string paramName)
    {
        if (Trimmed(number) is { } value)
        {
            sb.Append(property).Append(':').Append(NormalizePhone(value, paramName)).Append("\r\n");
        }
    }

    private static void AppendProperty(StringBuilder sb, string name, string? value)
    {
        if (value is null) return;
        sb.Append(name).Append(':');
        AppendVCardEscaped(sb, value);
        sb.Append("\r\n");
    }

    private static string NormalizeNewLines(string value, string newLine) =>
        value.Replace("\r\n", "\n").Replace('\r', '\n').Replace("\n", newLine);

    // vCard 3.0 text values (RFC 2426 section 4): escape backslash, comma and semicolon, and write line breaks as \n.
    private static void AppendVCardEscaped(StringBuilder sb, string? value)
    {
        if (value is null) return;
        foreach (var c in NormalizeNewLines(value, "\n"))
        {
            switch (c)
            {
                case '\\': sb.Append(@"\\"); break;
                case ',': sb.Append(@"\,"); break;
                case ';': sb.Append(@"\;"); break;
                case '\n': sb.Append(@"\n"); break;
                default:
                    if (!char.IsControl(c)) sb.Append(c);
                    break;
            }
        }
    }

    // The WIFI: format escapes \ ; , : and " with a backslash.
    private static void AppendWifiEscaped(StringBuilder sb, string value)
    {
        foreach (var c in value)
        {
            if (c is '\\' or ';' or ',' or ':' or '"') sb.Append('\\');
            sb.Append(c);
        }
    }

    // Percent-encodes UTF-8 bytes outside the RFC 3986 unreserved set (and outside keep, if given).
    private static void AppendPercentEncoded(StringBuilder sb, string value, string? keep)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(value);
        foreach (var b in bytes)
        {
            var c = (char)b;
            if (c is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or (>= '0' and <= '9') or '-' or '.' or '_' or '~'
                || (keep is not null && b < 0x80 && keep.IndexOf(c) >= 0))
            {
                sb.Append(c);
            }
            else
            {
                sb.Append('%').Append(Hex[b >> 4]).Append(Hex[b & 0xF]);
            }
        }
    }
}
