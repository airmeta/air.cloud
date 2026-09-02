using System.Security.Cryptography;
using System.Text;

namespace Air.Cloud.Capabilities.ControlPlaneAuth.Security;

/// <summary>
/// <para>zh-cn:实现 RFC 6238 兼容的 SHA-1 TOTP，使用 6 位验证码和 30 秒时间片以兼容 Microsoft Authenticator 与 Google Authenticator。</para>
/// <para>en-us:Implements RFC 6238-compatible SHA-1 TOTP with six-digit codes and thirty-second time steps for Microsoft Authenticator and Google Authenticator compatibility.</para>
/// </summary>
internal static class ControlPlaneTotp
{
    private const int SecretByteLength = 20;
    private const int Digits = 6;
    private const int PeriodSeconds = 30;

    public static string CreateSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(SecretByteLength);
        return Base32Encode(bytes);
    }

    public static string CreateProvisioningUri(string secret, string issuer, string account)
    {
        var label = Uri.EscapeDataString($"{issuer}:{account}");
        return $"otpauth://totp/{label}?secret={Uri.EscapeDataString(secret)}&issuer={Uri.EscapeDataString(issuer)}&algorithm=SHA1&digits={Digits}&period={PeriodSeconds}";
    }

    public static bool VerifyCode(string secret, string? code, DateTimeOffset now, long? lastUsedTimeStep, out long acceptedTimeStep)
    {
        acceptedTimeStep = 0;
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var normalizedCode = new string(code.Where(char.IsDigit).ToArray());
        if (normalizedCode.Length != Digits)
        {
            return false;
        }

        byte[] secretBytes;
        try
        {
            secretBytes = Base32Decode(secret);
        }
        catch (FormatException)
        {
            return false;
        }

        var currentTimeStep = now.ToUnixTimeSeconds() / PeriodSeconds;
        for (var offset = -1; offset <= 1; offset++)
        {
            var timeStep = currentTimeStep + offset;
            if (timeStep <= 0 || (lastUsedTimeStep is not null && timeStep <= lastUsedTimeStep.Value))
            {
                continue;
            }

            var expected = CreateCode(secretBytes, timeStep);
            if (CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(expected), Encoding.ASCII.GetBytes(normalizedCode)))
            {
                acceptedTimeStep = timeStep;
                return true;
            }
        }

        return false;
    }

    private static string CreateCode(byte[] secret, long timeStep)
    {
        Span<byte> counter = stackalloc byte[8];
        for (var index = counter.Length - 1; index >= 0; index--)
        {
            counter[index] = (byte)(timeStep & 0xff);
            timeStep >>= 8;
        }

        using var hmac = new HMACSHA1(secret);
        var hash = hmac.ComputeHash(counter.ToArray());
        var offset = hash[^1] & 0x0f;
        var binaryCode = ((hash[offset] & 0x7f) << 24)
            | (hash[offset + 1] << 16)
            | (hash[offset + 2] << 8)
            | hash[offset + 3];
        return (binaryCode % 1_000_000).ToString("D6");
    }

    private static string Base32Encode(byte[] bytes)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var builder = new StringBuilder((bytes.Length + 4) / 5 * 8);
        var buffer = 0;
        var bits = 0;
        foreach (var value in bytes)
        {
            buffer = (buffer << 8) | value;
            bits += 8;
            while (bits >= 5)
            {
                bits -= 5;
                builder.Append(alphabet[(buffer >> bits) & 31]);
            }

            buffer = bits == 0 ? 0 : buffer & ((1 << bits) - 1);
        }

        if (bits > 0)
        {
            builder.Append(alphabet[(buffer << (5 - bits)) & 31]);
        }

        return builder.ToString();
    }

    private static byte[] Base32Decode(string value)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var normalized = value.Trim().TrimEnd('=').Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
        var output = new List<byte>(normalized.Length * 5 / 8);
        var buffer = 0;
        var bits = 0;
        foreach (var character in normalized)
        {
            var index = alphabet.IndexOf(character);
            if (index < 0)
            {
                throw new FormatException("The TOTP secret is not valid Base32.");
            }

            buffer = (buffer << 5) | index;
            bits += 5;
            if (bits >= 8)
            {
                bits -= 8;
                output.Add((byte)(buffer >> bits));
                buffer = bits == 0 ? 0 : buffer & ((1 << bits) - 1);
            }
        }

        return output.ToArray();
    }
}
