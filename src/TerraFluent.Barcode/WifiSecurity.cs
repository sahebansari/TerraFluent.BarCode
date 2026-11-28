namespace TerraFluent.Barcode;

/// <summary>How a Wi-Fi network is secured, for <see cref="QrPayload.Wifi"/>.</summary>
public enum WifiSecurity
{
    /// <summary>WPA, WPA2 or WPA3 Personal (a shared passphrase). The default.</summary>
    Wpa,

    /// <summary>WEP, the obsolete scheme some old access points still use.</summary>
    Wep,

    /// <summary>An open network with no password.</summary>
    None,
}
