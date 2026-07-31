using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Durably.Execution;

/// <summary>
/// Executed-prefix hash chain over step keys: <c>h0 = SHA256("durably-v1")</c>,
/// <c>h(i+1) = SHA256(h(i) || UTF8(key))</c>, stored as lowercase hex.
/// </summary>
internal static class StepPathHasher
{
    private const string SeedLiteral = "durably-v1";

    public static string Seed() => ToHex(Sha256(Encoding.UTF8.GetBytes(SeedLiteral)));

    public static string Append(string hashHex, string key)
    {
        if (string.IsNullOrEmpty(hashHex))
        {
            throw new ArgumentException("Hash hex is required.", nameof(hashHex));
        }

        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        var previous = FromHex(hashHex);
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var combined = new byte[previous.Length + keyBytes.Length];
        Buffer.BlockCopy(previous, 0, combined, 0, previous.Length);
        Buffer.BlockCopy(keyBytes, 0, combined, previous.Length, keyBytes.Length);
        return ToHex(Sha256(combined));
    }

    public static string ComputePrefix(IReadOnlyList<string> keys, int count)
    {
        if (keys is null)
        {
            throw new ArgumentNullException(nameof(keys));
        }

        if (count < 0 || count > keys.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        var hash = Seed();
        for (var i = 0; i < count; i++)
        {
            hash = Append(hash, keys[i]);
        }

        return hash;
    }

    private static byte[] Sha256(byte[] data)
    {
        using var sha = SHA256.Create();
        return sha.ComputeHash(data);
    }

    private static byte[] FromHex(string hex)
    {
        if (hex.Length % 2 != 0)
        {
            throw new FormatException("Hex string must have an even length.");
        }

        var bytes = new byte[hex.Length / 2];
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = byte.Parse(hex.Substring(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        return bytes;
    }

    private static string ToHex(byte[] bytes)
    {
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
        {
            sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
        }

        return sb.ToString();
    }
}
