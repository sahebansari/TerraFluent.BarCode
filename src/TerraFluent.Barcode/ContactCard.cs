namespace TerraFluent.Barcode;

/// <summary>
/// The details of a person or business, written as a vCard by
/// <see cref="QrPayload.Contact"/> so that a phone's camera offers to save the
/// contact. Set at least <see cref="FirstName"/>, <see cref="LastName"/> or
/// <see cref="Organization"/>; every other property is optional.
/// </summary>
/// <example>
/// <code>
/// var card = new ContactCard
/// {
///     FirstName = "Ada",
///     LastName = "Lovelace",
///     Organization = "Analytical Engines Ltd",
///     MobilePhone = "+44 20 7946 0958",
///     Email = "ada@example.com",
/// };
/// byte[] png = Barcodes.QrCode(QrPayload.Contact(card)).ToPng();
/// </code>
/// </example>
public sealed class ContactCard
{
    /// <summary>Given name.</summary>
    public string? FirstName { get; set; }

    /// <summary>Family name.</summary>
    public string? LastName { get; set; }

    /// <summary>Company or organization.</summary>
    public string? Organization { get; set; }

    /// <summary>Job title, for example <c>Head of Engineering</c>.</summary>
    public string? JobTitle { get; set; }

    /// <summary>Main (voice) phone number.</summary>
    public string? Phone { get; set; }

    /// <summary>Mobile phone number.</summary>
    public string? MobilePhone { get; set; }

    /// <summary>Work phone number.</summary>
    public string? WorkPhone { get; set; }

    /// <summary>Email address.</summary>
    public string? Email { get; set; }

    /// <summary>Website, for example <c>https://terrafluent.dev</c>.</summary>
    public string? Website { get; set; }

    /// <summary>Street address. May span several lines.</summary>
    public string? Street { get; set; }

    /// <summary>City or locality.</summary>
    public string? City { get; set; }

    /// <summary>State, province or region.</summary>
    public string? Region { get; set; }

    /// <summary>Postal or ZIP code.</summary>
    public string? PostalCode { get; set; }

    /// <summary>Country name.</summary>
    public string? Country { get; set; }

    /// <summary>Date of birth. Only the date part is written.</summary>
    public DateTime? Birthday { get; set; }

    /// <summary>Free-text note. May span several lines.</summary>
    public string? Note { get; set; }
}
