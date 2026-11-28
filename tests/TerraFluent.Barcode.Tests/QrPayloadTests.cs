using ZXing;
using ZXing.Client.Result;

namespace TerraFluent.Barcode.Tests;

/// <summary>
/// QR payload helpers. Each payload is put in a QR code, read back with ZXing.Net
/// and handed to ZXing's result parser, which is how scanner apps decide what a
/// code means, so escaping mistakes show up as a wrong field or the wrong type.
/// </summary>
public class QrPayloadTests
{
    private static T Scan<T>(string payload)
        where T : ParsedResult
    {
        var png = Barcodes.QrCode(payload).ToPng();
        var result = DecoderHelper.Decode(png, BarcodeFormat.QR_CODE);
        Assert.NotNull(result);
        Assert.Equal(payload, result!.Text);
        return Assert.IsType<T>(ResultParser.parseResult(result));
    }

    // ---- Wi-Fi ----

    [Fact]
    public void Wifi_wpa_scans_as_a_network()
    {
        var payload = QrPayload.Wifi("Home", "correct horse");
        Assert.Equal("WIFI:T:WPA;S:Home;P:correct horse;;", payload);
        var wifi = Scan<WifiParsedResult>(payload);
        Assert.Equal("Home", wifi.Ssid);
        Assert.Equal("correct horse", wifi.Password);
        Assert.Equal("WPA", wifi.NetworkEncryption);
        Assert.False(wifi.Hidden);
    }

    [Fact]
    public void Wifi_escapes_special_characters()
    {
        var payload = QrPayload.Wifi("Café;Net:5G", "p\\a;s,s:\"word\"", WifiSecurity.Wpa, hidden: true);
        Assert.Equal("WIFI:T:WPA;S:Café\\;Net\\:5G;P:p\\\\a\\;s\\,s\\:\\\"word\\\";H:true;;", payload);
        var wifi = Scan<WifiParsedResult>(payload);
        Assert.Equal("Café;Net:5G", wifi.Ssid);
        Assert.Equal("p\\a;s,s:\"word\"", wifi.Password);
        Assert.True(wifi.Hidden);
    }

    [Fact]
    public void Wifi_open_and_wep_networks()
    {
        Assert.Equal("WIFI:T:nopass;S:Guest;;", QrPayload.Wifi("Guest", null, WifiSecurity.None));
        Assert.Equal("nopass", Scan<WifiParsedResult>(QrPayload.Wifi("Guest", null, WifiSecurity.None)).NetworkEncryption);
        var wep = Scan<WifiParsedResult>(QrPayload.Wifi("Old", "0123456789", WifiSecurity.Wep));
        Assert.Equal("WEP", wep.NetworkEncryption);
        Assert.Equal("0123456789", wep.Password);
    }

    [Theory]
    [InlineData("Net", null, WifiSecurity.Wpa)]
    [InlineData("Net", "short", WifiSecurity.Wpa)]
    [InlineData("Net", "1234567", WifiSecurity.Wpa)]
    [InlineData("Net", "123456", WifiSecurity.Wep)]
    [InlineData("Net", "012345678z", WifiSecurity.Wep)]
    [InlineData("Net", "password", WifiSecurity.None)]
    [InlineData("This network name is far too long", "password", WifiSecurity.Wpa)]
    [InlineData("Line\nbreak", "password", WifiSecurity.Wpa)]
    public void Wifi_rejects_invalid_input(string ssid, string? password, WifiSecurity security)
    {
        Assert.Throws<ArgumentException>(() => QrPayload.Wifi(ssid, password, security));
    }

    [Fact]
    public void Wifi_accepts_a_64_hex_digit_key_but_not_64_characters()
    {
        QrPayload.Wifi("Net", new string('a', 64));
        Assert.Throws<ArgumentException>(() => QrPayload.Wifi("Net", new string('z', 64)));
    }

    // ---- URL ----

    [Theory]
    [InlineData("https://terrafluent.dev/barcode/", "https://terrafluent.dev/barcode/")]
    [InlineData("terrafluent.dev/barcode", "https://terrafluent.dev/barcode")]
    [InlineData("  http://example.com/a?b=c&d=e#f  ", "http://example.com/a?b=c&d=e#f")]
    [InlineData("localhost:8080/x", "https://localhost:8080/x")]
    public void Url_scans_as_a_link(string input, string expected)
    {
        var payload = QrPayload.Url(input);
        Assert.Equal(expected, payload);
        Assert.Equal(expected, Scan<URIParsedResult>(payload).URI);
    }

    [Theory]
    [InlineData("javascript://alert(1)")]
    [InlineData("ftp://example.com/file")]
    [InlineData("https://exa mple.com")]
    [InlineData("https://")]
    public void Url_rejects_non_web_addresses(string url)
    {
        Assert.Throws<ArgumentException>(() => QrPayload.Url(url));
    }

    // ---- Email ----

    [Fact]
    public void Email_scans_with_subject_and_body()
    {
        var payload = QrPayload.Email("ada+qr@example.com", "Hello & welcome?", "Line 1\nLine 2 – ünïcode");
        Assert.Equal("mailto:ada%2Bqr@example.com?subject=Hello%20%26%20welcome%3F&body=Line%201%0D%0ALine%202%20%E2%80%93%20%C3%BCn%C3%AFcode", payload);
        var email = Scan<EmailAddressParsedResult>(payload);
        Assert.Equal(new[] { "ada+qr@example.com" }, email.Tos);
        Assert.Equal("Hello & welcome?", email.Subject);
        // RFC 6068 percent-encodes UTF-8; ZXing.Net decodes it as Latin-1, so check the body with a UTF-8 decoder instead.
        Assert.StartsWith("Line 1\r\nLine 2 ", email.Body);
        Assert.Equal("Line 1\r\nLine 2 – ünïcode", Uri.UnescapeDataString(payload.Substring(payload.IndexOf("body=", StringComparison.Ordinal) + 5)));
    }

    [Fact]
    public void Email_without_extras_is_a_plain_mailto()
    {
        Assert.Equal("mailto:hello@example.com", QrPayload.Email("hello@example.com"));
        Assert.Equal("mailto:hello@example.com?body=Hi", QrPayload.Email("hello@example.com", body: "Hi"));
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("@example.com")]
    [InlineData("a@")]
    [InlineData("a@b@c")]
    [InlineData("a b@example.com")]
    [InlineData("a@example.com,b@example.com")]
    public void Email_rejects_invalid_addresses(string address)
    {
        Assert.Throws<ArgumentException>(() => QrPayload.Email(address));
    }

    // ---- Phone and SMS ----

    [Fact]
    public void Phone_scans_as_a_number()
    {
        var payload = QrPayload.Phone("+1 (555) 123-4567");
        Assert.Equal("tel:+15551234567", payload);
        Assert.Equal("+15551234567", Scan<TelParsedResult>(payload).Number);
    }

    [Fact]
    public void Sms_scans_with_a_message()
    {
        var payload = QrPayload.Sms("+44 20 7946 0958", "Running late: 10 min");
        Assert.Equal("SMSTO:+442079460958:Running late: 10 min", payload);
        var sms = Scan<SMSParsedResult>(payload);
        Assert.Equal(new[] { "+442079460958" }, sms.Numbers);
        Assert.Equal("Running late: 10 min", sms.Body);
        Assert.Equal("SMSTO:12345", QrPayload.Sms("12345"));
    }

    [Theory]
    [InlineData("12")]
    [InlineData("555-CALL-NOW")]
    [InlineData("1+2")]
    [InlineData("123456789012345678901")]
    public void Phone_rejects_invalid_numbers(string number)
    {
        Assert.Throws<ArgumentException>(() => QrPayload.Phone(number));
        Assert.Throws<ArgumentException>(() => QrPayload.Sms(number));
    }

    // ---- Contact (vCard) ----

    [Fact]
    public void Contact_scans_as_an_address_book_entry()
    {
        var card = new ContactCard
        {
            FirstName = "Ada",
            LastName = "Lovelace",
            Organization = "Engines, Ltd; London",
            JobTitle = "Analyst",
            Phone = "+44 20 7946 0000",
            MobilePhone = "+44 7700 900123",
            WorkPhone = "+44 20 7946 0958",
            Email = "ada@example.com",
            Website = "example.com/ada",
            Street = "12 St James's Square\nFlat 3",
            City = "London",
            PostalCode = "SW1Y 4JH",
            Country = "United Kingdom",
            Birthday = new DateTime(1815, 12, 10),
            Note = "Back\\slash, comma; semicolon",
        };

        var payload = QrPayload.Contact(card);
        Assert.Equal(
            "BEGIN:VCARD\r\nVERSION:3.0\r\nN:Lovelace;Ada;;;\r\nFN:Ada Lovelace\r\nORG:Engines\\, Ltd\\; London\r\nTITLE:Analyst\r\n"
            + "TEL;TYPE=VOICE:+442079460000\r\nTEL;TYPE=CELL:+447700900123\r\nTEL;TYPE=WORK,VOICE:+442079460958\r\n"
            + "EMAIL;TYPE=INTERNET:ada@example.com\r\nURL:https://example.com/ada\r\n"
            + "ADR:;;12 St James's Square\\nFlat 3;London;;SW1Y 4JH;United Kingdom\r\nBDAY:1815-12-10\r\n"
            + "NOTE:Back\\\\slash\\, comma\\; semicolon\r\nEND:VCARD",
            payload);

        var contact = Scan<AddressBookParsedResult>(payload);
        Assert.Equal("Ada Lovelace", Assert.Single(contact.Names));
        Assert.Equal("Engines, Ltd; London", contact.Org);
        Assert.Equal("Analyst", contact.Title);
        Assert.Equal(new[] { "+442079460000", "+447700900123", "+442079460958" }, contact.PhoneNumbers);
        Assert.Equal("ada@example.com", Assert.Single(contact.Emails));
        Assert.Equal("https://example.com/ada", Assert.Single(contact.URLs));
        Assert.Contains("London", Assert.Single(contact.Addresses));
        Assert.Equal("Back\\slash, comma; semicolon", contact.Note);
    }

    [Fact]
    public void Contact_with_only_an_organization_uses_it_as_the_display_name()
    {
        var payload = QrPayload.Contact(new ContactCard { Organization = "TerraFluent" });
        Assert.Equal("BEGIN:VCARD\r\nVERSION:3.0\r\nN:;;;;\r\nFN:TerraFluent\r\nORG:TerraFluent\r\nEND:VCARD", payload);
        Assert.Equal("TerraFluent", Scan<AddressBookParsedResult>(payload).Org);
    }

    [Fact]
    public void Contact_rejects_cards_without_a_name_or_with_bad_fields()
    {
        Assert.Throws<ArgumentNullException>(() => QrPayload.Contact(null!));
        Assert.Throws<ArgumentException>(() => QrPayload.Contact(new ContactCard { JobTitle = "Nobody" }));
        Assert.Throws<ArgumentException>(() => QrPayload.Contact(new ContactCard { FirstName = "A", Email = "nope" }));
        Assert.Throws<ArgumentException>(() => QrPayload.Contact(new ContactCard { FirstName = "A", MobilePhone = "call me" }));
        Assert.Throws<ArgumentException>(() => QrPayload.Contact(new ContactCard { FirstName = "A", Website = "ftp://x.example" }));
    }

    // ---- GS1 Digital Link ----

    [Fact]
    public void Gs1DigitalLink_puts_key_and_qualifiers_in_the_path_in_standard_order()
    {
        var payload = QrPayload.Gs1DigitalLink(("17", "251231"), ("21", "SN/1-2"), ("01", "09501101530003"), ("10", "ABC123"), ("3103", "000189"));
        Assert.Equal("https://id.gs1.org/01/09501101530003/10/ABC123/21/SN%2F1-2?17=251231&3103=000189", payload);
        Assert.Equal(payload, Scan<URIParsedResult>(payload).URI);
    }

    [Fact]
    public void Gs1DigitalLink_accepts_an_element_string_and_a_custom_domain()
    {
        Assert.Equal(
            "https://example.com/dl/01/09501101530003/22/2A?15=260101",
            QrPayload.Gs1DigitalLink(new Uri("https://example.com/dl/"), "(01)09501101530003(15)260101(22)2A"));
        Assert.Equal("https://id.gs1.org/00/106141412345678908", QrPayload.Gs1DigitalLink("(00)106141412345678908"));
    }

    [Theory]
    [InlineData("9501101530003", "https://id.gs1.org/01/09501101530003")]
    [InlineData("614141123452", "https://id.gs1.org/01/00614141123452")]
    [InlineData("96385074", "https://id.gs1.org/01/00000096385074")]
    public void Gs1DigitalLink_pads_short_gtins_to_14_digits(string gtin, string expected)
    {
        Assert.Equal(expected, QrPayload.Gs1DigitalLink(("01", gtin)));
    }

    [Fact]
    public void Gs1DigitalLink_gln_takes_one_kind_of_qualifier()
    {
        Assert.Equal("https://id.gs1.org/414/9520123456788/254/32a%2Fb", QrPayload.Gs1DigitalLink(("414", "9520123456788"), ("254", "32a/b")));
        Assert.Throws<ArgumentException>(() => QrPayload.Gs1DigitalLink(("414", "9520123456788"), ("254", "1"), ("7040", "1A2B")));
    }

    [Theory]
    [InlineData("(10)ABC")]                                     // no primary key
    [InlineData("(01)09501101530003(00)106141412345678908")]    // two primary keys
    [InlineData("(01)09501101530004")]                          // bad check digit
    [InlineData("(01)09501101530003(10)A(10)B")]                // repeated AI
    [InlineData("(01)09501101530003(17)251332")]                // impossible date
    [InlineData("(01)09501101530003(999)X")]                    // unknown AI
    public void Gs1DigitalLink_rejects_invalid_element_strings(string elements)
    {
        Assert.Throws<ArgumentException>(() => QrPayload.Gs1DigitalLink(elements));
    }

    [Theory]
    [InlineData("ftp://example.com")]
    [InlineData("https://example.com/?a=b")]
    public void Gs1DigitalLink_rejects_unsuitable_domains(string domain)
    {
        Assert.Throws<ArgumentException>(() => QrPayload.Gs1DigitalLink(new Uri(domain), ("01", "09501101530003")));
    }

    // ---- the examples in docs/qr-payloads.md ----

    [Fact]
    public void Documented_examples_produce_the_documented_output()
    {
        Assert.Equal(@"WIFI:T:WPA;S:Office\;5G;P:p@ss\;word;H:true;;", QrPayload.Wifi("Office;5G", "p@ss;word", WifiSecurity.Wpa, hidden: true));
        Assert.Equal("WIFI:T:nopass;S:Lobby;;", QrPayload.Wifi("Lobby", null, WifiSecurity.None));
        Assert.Equal("mailto:hello@example.com?subject=Order%201042&body=Hi%2C%0D%0AI%20have%20a%20question.", QrPayload.Email("hello@example.com", subject: "Order 1042", body: "Hi,\nI have a question."));
        Assert.Equal("tel:+15551234567", QrPayload.Phone("+1 (555) 123-4567"));
        Assert.Equal("SMSTO:+15551234567:Running late", QrPayload.Sms("+1 555 123 4567", "Running late"));
        Assert.Equal("https://id.gs1.org/01/09501101530003/10/ABC123?17=271231", QrPayload.Gs1DigitalLink("(01)09501101530003(10)ABC123(17)271231"));
        Assert.Equal(
            "https://brand.example.com/01/09501101530003/10/ABC123/21/SN-42",
            QrPayload.Gs1DigitalLink(new Uri("https://brand.example.com"), ("01", "09501101530003"), ("21", "SN-42"), ("10", "ABC123")));
        Assert.Throws<ArgumentException>(() => QrPayload.Phone("1-800-FLOWERS"));
    }

    // ---- fits the fluent API ----

    [Fact]
    public void Payloads_work_with_other_2d_symbologies()
    {
        var payload = QrPayload.Wifi("Home", "correct horse");
        Assert.Equal(payload, DecoderHelper.DecodeText(Barcodes.DataMatrix(payload).ToPng(), BarcodeFormat.DATA_MATRIX));
        Assert.Equal(payload, DecoderHelper.DecodeText(Barcodes.Aztec(payload).ToPng(), BarcodeFormat.AZTEC));
    }
}
