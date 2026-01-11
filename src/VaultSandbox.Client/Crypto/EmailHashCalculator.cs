using System.Security.Cryptography;
using System.Text;

namespace VaultSandbox.Client.Crypto;

/// <summary>
/// Computes email hashes using the algorithm: BASE64URL(SHA256(SORT(emailIds).join(",")))
/// </summary>
internal static class EmailHashCalculator
{
    /// <summary>
    /// Computes a hash from a collection of email IDs.
    /// </summary>
    /// <param name="emailIds">The email IDs to hash.</param>
    /// <returns>Base64URL-encoded SHA256 hash of the sorted, comma-joined email IDs.</returns>
    public static string ComputeHash(IEnumerable<string> emailIds)
    {
        var sortedIds = emailIds.OrderBy(id => id, StringComparer.Ordinal);
        var joined = string.Join(",", sortedIds);
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(joined));
        return Base64Url.Encode(hashBytes);
    }
}
