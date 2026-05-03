using System.Security.Cryptography;
using System.Text;

namespace RajFinancial.Api.Services.RateLimit.Storage;

/// <summary>
///     Stable, opaque hash of a user identifier suitable for use as an Azure Table partition key.
/// </summary>
/// <remarks>
///     <para>
///         Hashing serves two purposes:
///         <list type="bullet">
///             <item>Avoids persisting raw user identity (e.g., Entra ObjectId) in storage.</item>
///             <item>Distributes user partitions evenly across the table service.</item>
///         </list>
///     </para>
///     <para>
///         Output is the lowercase hexadecimal SHA-256 of the UTF-8 user id, truncated to
///         32 characters. 32 hex chars = 128 bits — far more than needed to avoid collisions
///         in our user space, while staying well under the 1024-character Azure Table partition
///         key limit and keeping logs/traces compact.
///     </para>
/// </remarks>
internal static class UserIdHashing
{
    /// <summary>
    ///     Returns a stable, lowercase 32-char hex SHA-256 hash of <paramref name="userId" />.
    /// </summary>
    /// <exception cref="ArgumentException">If <paramref name="userId" /> is null or whitespace.</exception>
    public static string Hash(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User id must not be null or empty.", nameof(userId));

        Span<byte> hash = stackalloc byte[32];
        var bytes = Encoding.UTF8.GetBytes(userId);
        SHA256.HashData(bytes, hash);

        // 32 bytes -> 64 hex chars; we want first 32 (16 bytes).
        return Convert.ToHexString(hash[..16]).ToLowerInvariant();
    }
}
