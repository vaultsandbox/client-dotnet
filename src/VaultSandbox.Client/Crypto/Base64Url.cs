namespace VaultSandbox.Client.Crypto;

/// <summary>
/// URL-safe Base64 encoding/decoding (RFC 4648 Section 5).
/// </summary>
internal static class Base64Url
{
    /// <summary>
    /// Encodes bytes to URL-safe Base64 without padding.
    /// </summary>
    public static string Encode(ReadOnlySpan<byte> data)
    {
        var base64 = Convert.ToBase64String(data);
        return base64
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    /// <summary>
    /// Decodes URL-safe Base64 string to bytes.
    /// </summary>
    public static byte[] Decode(string encoded)
    {
        var base64 = encoded
            .Replace('-', '+')
            .Replace('_', '/');

        // Add padding if needed
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }

        return Convert.FromBase64String(base64);
    }
}
