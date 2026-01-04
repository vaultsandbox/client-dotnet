namespace VaultSandbox.Client.Crypto;

/// <summary>
/// URL-safe Base64 encoding/decoding (RFC 4648 Section 5).
/// Per VaultSandbox spec: MUST reject input containing +, /, or = characters.
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
    /// <param name="encoded">Base64URL encoded string (must not contain +, /, or =).</param>
    /// <returns>Decoded bytes.</returns>
    /// <exception cref="FormatException">Thrown if input contains invalid characters (+, /, or =).</exception>
    public static byte[] Decode(string encoded)
    {
        // Per spec: Implementations MUST reject input containing +, /, or =
        if (encoded.Contains('+') || encoded.Contains('/') || encoded.Contains('='))
        {
            throw new FormatException(
                "Invalid Base64URL: input must not contain '+', '/', or '=' characters. " +
                "Use '-' instead of '+', '_' instead of '/', and no padding.");
        }

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
