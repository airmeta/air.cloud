using Air.Cloud.Modules.ReverseTunnel.Common.Enrollment;
using Air.Cloud.Modules.ReverseTunnel.Server;

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Air.Cloud.UnitTest.Modules.ReverseTunnel;

/// <summary>
/// <para>zh-cn:加密 GUID Enrollment 和 replay 防护测试。</para>
/// <para>en-us:Tests for encrypted-GUID enrollment and replay protection.</para>
/// </summary>
public sealed class ReverseTunnelEncryptedEnrollmentTests
{
    /// <summary>
    /// <para>zh-cn:验证 Agent 使用 CA 公钥加密的 GUID 可以由同一 CA 私钥解密，错误密文会被拒绝。</para>
    /// <para>en-us:Verifies that a GUID encrypted with the CA public key can be decrypted by the matching CA private key and malformed ciphertext is rejected.</para>
    /// </summary>
    [Fact]
    public void Enrollment_cipher_should_round_trip_and_reject_invalid_ciphertext()
    {
        using var ca = CreateCertificate(includePrivateKey: true);
        using var publicCertificate = X509CertificateLoader.LoadCertificate(ca.Export(X509ContentType.Cert));

        var encrypted = ReverseTunnelEnrollmentCipher.EncryptGuid(publicCertificate, out var expectedGuid);

        Assert.True(ReverseTunnelEnrollmentCipher.TryDecryptGuid(ca, encrypted, out var actualGuid));
        Assert.Equal(expectedGuid, actualGuid);
        Assert.False(ReverseTunnelEnrollmentCipher.TryDecryptGuid(ca, [1, 2, 3], out _));
    }

    /// <summary>
    /// <para>zh-cn:验证同一个 Agent/GUID 只在 replay 窗口内成功一次，过期记录可以重新占用。</para>
    /// <para>en-us:Verifies that an Agent/GUID pair succeeds only once inside the replay window and can be claimed again after the old entry expires.</para>
    /// </summary>
    [Fact]
    public void Replay_store_should_claim_once_and_expire_entries()
    {
        var store = new InMemoryReverseTunnelEnrollmentReplayStore();
        var guid = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        Assert.True(store.TryClaim("agent-a", guid, now, TimeSpan.FromMinutes(1)));
        Assert.False(store.TryClaim("agent-a", guid, now.AddSeconds(1), TimeSpan.FromMinutes(1)));
        Assert.True(store.TryClaim("agent-a", guid, now.AddMinutes(2), TimeSpan.FromMinutes(1)));
    }

    /// <summary>
    /// <para>zh-cn:验证服务端只能为同一个解密 GUID 签发一次证书，第二次请求在 CA 签发前被 replay store 拒绝。</para>
    /// <para>en-us:Verifies that the Server issues only once for a decrypted GUID and rejects the second request before CA issuance.</para>
    /// </summary>
    [Fact]
    public void Enrollment_service_should_reject_replayed_guid()
    {
        using var ca = CreateCertificate(includePrivateKey: true);
        using var publicCertificate = X509CertificateLoader.LoadCertificate(ca.Export(X509ContentType.Cert));
        var encrypted = ReverseTunnelEnrollmentCipher.EncryptGuid(publicCertificate, out _);
        var service = new ReverseTunnelAgentEnrollmentService(new TestCertificateAuthority(ca), new InMemoryReverseTunnelEnrollmentReplayStore());

        var first = service.Enroll("agent-a", encrypted, [1], DateTimeOffset.UtcNow);
        var second = service.Enroll("agent-a", encrypted, [1], DateTimeOffset.UtcNow);

        Assert.True(first.Succeeded, first.FailureReason);
        Assert.False(second.Succeeded);
        Assert.Equal("Encrypted enrollment GUID was already used or is invalid.", second.FailureReason);
    }

    private static X509Certificate2 CreateCertificate(bool includePrivateKey)
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=AirCloudReverseTunnelEnrollmentTest", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        return includePrivateKey
            ? X509CertificateLoader.LoadPkcs12(certificate.Export(X509ContentType.Pkcs12), null, X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.Exportable)
            : X509CertificateLoader.LoadCertificate(certificate.Export(X509ContentType.Cert));
    }

    private sealed class TestCertificateAuthority : IReverseTunnelCertificateAuthority
    {
        private readonly byte[] pfx;

        public TestCertificateAuthority(X509Certificate2 certificate)
        {
            pfx = certificate.Export(X509ContentType.Pkcs12);
        }

        public X509Certificate2 LoadIssuerCertificate() => X509CertificateLoader.LoadPkcs12(pfx, null, X509KeyStorageFlags.EphemeralKeySet);

        public X509Certificate2 SignAgentCertificate(string agentId, byte[] certificateSigningRequest, DateTimeOffset notBeforeUtc)
            => X509CertificateLoader.LoadPkcs12(pfx, null, X509KeyStorageFlags.EphemeralKeySet);
    }
}
