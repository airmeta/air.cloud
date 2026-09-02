using System.Security.Cryptography;

namespace Air.Cloud.Capabilities.ControlPlaneAuth.Security;

/// <summary>
/// <para>zh-cn:控制面密码和 Ticket hash 工具。输出格式包含算法、迭代次数、盐和 hash，便于后续升级算法。</para>
/// <para>en-us:Hashing utility for control-plane passwords and tickets. The output contains the algorithm, iteration count, salt, and hash so the algorithm can be upgraded later.</para>
/// </summary>
public static class ControlPlanePasswordHasher
{
    private const int Iterations = 100_000;
    private const int SaltBytes = 16;
    private const int HashBytes = 32;
    private const string Prefix = "pbkdf2-sha256";

    /// <summary>
    /// <para>zh-cn:对明文值进行 PBKDF2-SHA256 hash。</para>
    /// <para>en-us:Hashes a clear-text value with PBKDF2-SHA256.</para>
    /// </summary>
    /// <param name="value">
    /// <para>zh-cn:需要 hash 的明文值。</para>
    /// <para>en-us:The clear-text value to hash.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:包含算法参数的 hash 字符串。</para>
    /// <para>en-us:A hash string containing algorithm parameters.</para>
    /// </returns>
    public static string Hash(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var salt = RandomNumberGenerator.GetBytes(SaltBytes);
        var hash = Rfc2898DeriveBytes.Pbkdf2(value, salt, Iterations, HashAlgorithmName.SHA256, HashBytes);
        return $"{Prefix}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    /// <summary>
    /// <para>zh-cn:验证明文值是否匹配已保存的 hash；格式不合法或 hash 不匹配时返回 false。</para>
    /// <para>en-us:Verifies whether a clear-text value matches a stored hash; invalid formats or mismatches return false.</para>
    /// </summary>
    /// <param name="value">
    /// <para>zh-cn:待验证的明文值。</para>
    /// <para>en-us:The clear-text value to verify.</para>
    /// </param>
    /// <param name="storedHash">
    /// <para>zh-cn:已保存的 hash 字符串。</para>
    /// <para>en-us:The stored hash string.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:匹配时返回 true。</para>
    /// <para>en-us:True when the value matches.</para>
    /// </returns>
    public static bool Verify(string value, string storedHash)
    {
        if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(storedHash))
        {
            return false;
        }

        var parts = storedHash.Split('$');
        if (parts.Length != 4 || !string.Equals(parts[0], Prefix, StringComparison.Ordinal) || !int.TryParse(parts[1], out var iterations))
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(value, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
