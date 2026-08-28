using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Air.Cloud.Modules.ReverseTunnel.Common.Enrollment;

/// <summary>
/// <para>zh-cn:使用 Server CA 证书公钥保护首次 Enrollment 的随机 GUID。Agent 只持有公钥，Server 使用同一证书材料中的私钥解密。</para>
/// <para>en-us:Protects the random GUID used by initial enrollment with the Server CA public key. The Agent holds only the public key, while the Server decrypts with the private key from the same certificate material.</para>
/// </summary>
public static class ReverseTunnelEnrollmentCipher
{
    private const int GuidLength = 16;

    /// <summary>
    /// <para>zh-cn:生成随机 GUID 并使用证书 RSA 公钥加密。证书必须包含 RSA 公钥，返回值可直接写入 gRPC bytes 字段。</para>
    /// <para>en-us:Generates a random GUID and encrypts it with the certificate RSA public key. The certificate must expose an RSA public key, and the returned bytes can be sent directly in the gRPC bytes field.</para>
    /// </summary>
    /// <param name="certificate"><para>zh-cn:Agent 已信任的 Server CA 公钥证书。</para><para>en-us:Trusted Server CA public certificate held by the Agent.</para></param>
    /// <param name="guid"><para>zh-cn:本次 Enrollment 的随机 GUID。</para><para>en-us:Random GUID for this enrollment attempt.</para></param>
    /// <returns><para>zh-cn:RSA-OAEP-SHA256 密文。</para><para>en-us:RSA-OAEP-SHA256 ciphertext.</para></returns>
    public static byte[] EncryptGuid(X509Certificate2 certificate, out Guid guid)
    {
        ArgumentNullException.ThrowIfNull(certificate);
        guid = Guid.NewGuid();
        using var rsa = certificate.GetRSAPublicKey()
            ?? throw new InvalidOperationException("The enrollment certificate must contain an RSA public key.");
        return rsa.Encrypt(guid.ToByteArray(), RSAEncryptionPadding.OaepSHA256);
    }

    /// <summary>
    /// <para>zh-cn:使用 Server CA 私钥解密并解析 Enrollment GUID。任意解密、长度或 GUID 错误都会返回 false，不向调用方泄露密码学细节。</para>
    /// <para>en-us:Decrypts and parses an enrollment GUID with the Server CA private key. Any cryptographic, length, or GUID error returns false without exposing cryptographic details to the caller.</para>
    /// </summary>
    /// <param name="certificate"><para>zh-cn:包含 RSA 私钥的 Server CA 证书。</para><para>en-us:Server CA certificate containing the RSA private key.</para></param>
    /// <param name="ciphertext"><para>zh-cn:Agent 提交的 GUID 密文。</para><para>en-us:GUID ciphertext submitted by the Agent.</para></param>
    /// <param name="guid"><para>zh-cn:解密后的 GUID。</para><para>en-us:Decrypted GUID.</para></param>
    /// <returns><para>zh-cn:密文有效且成功解密时返回 true。</para><para>en-us:True when the ciphertext is valid and successfully decrypted.</para></returns>
    public static bool TryDecryptGuid(X509Certificate2 certificate, ReadOnlySpan<byte> ciphertext, out Guid guid)
    {
        guid = default;
        if (certificate is null || ciphertext.IsEmpty)
        {
            return false;
        }

        try
        {
            using var rsa = certificate.GetRSAPrivateKey();
            if (rsa is null)
            {
                return false;
            }

            var plaintext = rsa.Decrypt(ciphertext.ToArray(), RSAEncryptionPadding.OaepSHA256);
            return plaintext.Length == GuidLength && (guid = new Guid(plaintext)) != Guid.Empty;
        }
        catch (CryptographicException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
