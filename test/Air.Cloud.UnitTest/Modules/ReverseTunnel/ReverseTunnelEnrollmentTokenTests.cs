using Air.Cloud.Modules.ReverseTunnel.Common.Enrollment;
using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Server;

using Microsoft.Extensions.Options;

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Air.Cloud.UnitTest.Modules.ReverseTunnel;

/// <summary>
/// <para>zh-cn:AirTunnel Enrollment Token 引导材料测试。</para>
/// <para>en-us:Tests for AirTunnel enrollment-token bootstrap material.</para>
/// </summary>
public sealed class ReverseTunnelEnrollmentTokenTests
{
    /// <summary>
    /// <para>zh-cn:验证 Token 可以携带 AgentId、CA 和有效期，并在解析后恢复可信 CA。</para>
    /// <para>en-us:Verifies that a token carries AgentId, CA, and expiry information and restores the trusted CA after parsing.</para>
    /// </summary>
    [Fact]
    public void Token_should_round_trip_bootstrap_material()
    {
        using var ca = CreateCa();
        var expiresAtUtc = DateTimeOffset.UtcNow.AddHours(2);
        var token = ReverseTunnelEnrollmentToken.Create(
            "agent-alpha",
            "one-time-credential",
            ca,
            "https://airtunnel.example.com:18443",
            expiresAtUtc);

        var parsed = ReverseTunnelEnrollmentToken.Parse(token.Serialize());
        using var trustedIssuer = parsed.LoadTrustedIssuerCertificate();

        Assert.Equal(1, parsed.Version);
        Assert.Equal("agent-alpha", parsed.AgentId);
        Assert.Equal("one-time-credential", parsed.Credential);
        Assert.Equal("https://airtunnel.example.com:18443", parsed.ServerAddress);
        Assert.Equal(expiresAtUtc, parsed.ExpiresAtUtc);
        Assert.NotNull(trustedIssuer);
        Assert.Equal(ca.Thumbprint, trustedIssuer!.Thumbprint);
        Assert.False(trustedIssuer.HasPrivateKey);
    }

    /// <summary>
    /// <para>zh-cn:验证旧版本纯文本 Token 仍然可以作为兼容凭据解析。</para>
    /// <para>en-us:Verifies that a legacy plain-text token remains parseable as a compatible credential.</para>
    /// </summary>
    [Fact]
    public void Legacy_plain_text_token_should_remain_compatible()
    {
        var parsed = ReverseTunnelEnrollmentToken.Parse("legacy-token");

        Assert.Equal("legacy-token", parsed.Credential);
        Assert.Empty(parsed.AgentId);
        Assert.Null(parsed.LoadTrustedIssuerCertificate());
    }

    /// <summary>
    /// <para>zh-cn:验证 Token 创建过程不会接受包含私钥的 CA 证书。</para>
    /// <para>en-us:Verifies that token creation rejects a CA certificate that contains a private key.</para>
    /// </summary>
    [Fact]
    public void Token_should_reject_private_key_material()
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=AirCloudReverseTunnelPrivateCa", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        using var ca = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));

        var exception = Assert.Throws<ArgumentException>(() =>
            ReverseTunnelEnrollmentToken.Create("agent-private-ca", "credential", ca));

        Assert.Contains("without a private key", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// <para>zh-cn:验证服务端凭据存储可以校验引导 Token 中的凭据并按 AgentId 绑定。</para>
    /// <para>en-us:Verifies that the Server credential store validates the embedded credential and enforces the AgentId binding.</para>
    /// </summary>
    [Fact]
    public void Credential_store_should_validate_bootstrap_token_binding()
    {
        using var ca = CreateCa();
        var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "air-cloud-reverse-tunnel-token-tests", Guid.NewGuid().ToString("N")));
        try
        {
            var token = ReverseTunnelEnrollmentToken.Create("agent-alpha", "one-time-credential", ca);
            var path = Path.Combine(directory.FullName, ReverseTunnelEnrollmentCredentialNames.CreateFileName("agent-alpha", ".token"));
            File.WriteAllText(path, token.Serialize(), Encoding.UTF8);
            var mismatchedAgentPath = Path.Combine(directory.FullName, ReverseTunnelEnrollmentCredentialNames.CreateFileName("agent-beta", ".token"));
            File.WriteAllText(mismatchedAgentPath, token.Serialize(), Encoding.UTF8);
            var options = new ReverseTunnelOptions
            {
                Enabled = true,
                TunnelMode = ReverseTunnelMode.Server,
                Enrollment = new ReverseTunnelEnrollmentOptions
                {
                    Enabled = true,
                    CredentialDirectory = directory.FullName,
                    ConsumeOnUse = false
                }
            };
            var store = new MountedReverseTunnelEnrollmentCredentialStore(Options.Create(options));

            Assert.True(store.TryConsume("agent-alpha", token.Serialize(), out var successFailure), successFailure);
            Assert.False(store.TryConsume("agent-beta", token.Serialize(), out var mismatchFailure));
            Assert.Equal("Enrollment credential is bound to a different AgentId.", mismatchFailure);
        }
        finally
        {
            if (Directory.Exists(directory.FullName))
            {
                Directory.Delete(directory.FullName, recursive: true);
            }
        }
    }

    /// <summary>
    /// <para>zh-cn:验证已过期的 JSON Enrollment Token 会被服务端拒绝，且不会被消费。</para>
    /// <para>en-us:Verifies that an expired JSON enrollment token is rejected by the Server and is not consumed.</para>
    /// </summary>
    [Fact]
    public void Credential_store_should_reject_expired_bootstrap_token()
    {
        using var ca = CreateCa();
        var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "air-cloud-reverse-tunnel-token-tests", Guid.NewGuid().ToString("N")));
        try
        {
            var token = ReverseTunnelEnrollmentToken.Create(
                "agent-expired",
                "expired-credential",
                ca,
                expiresAtUtc: DateTimeOffset.UtcNow.AddMinutes(-1));
            var path = Path.Combine(directory.FullName, ReverseTunnelEnrollmentCredentialNames.CreateFileName("agent-expired", ".token"));
            File.WriteAllText(path, token.Serialize(), Encoding.UTF8);
            var options = new ReverseTunnelOptions
            {
                Enabled = true,
                TunnelMode = ReverseTunnelMode.Server,
                Enrollment = new ReverseTunnelEnrollmentOptions
                {
                    Enabled = true,
                    CredentialDirectory = directory.FullName,
                    ConsumeOnUse = true
                }
            };
            var store = new MountedReverseTunnelEnrollmentCredentialStore(Options.Create(options));

            Assert.False(store.TryConsume("agent-expired", token.Serialize(), out var failureReason));
            Assert.Equal("Enrollment credential has expired.", failureReason);
            Assert.True(File.Exists(path));
        }
        finally
        {
            if (Directory.Exists(directory.FullName))
            {
                Directory.Delete(directory.FullName, recursive: true);
            }
        }
    }

    /// <summary>
    /// <para>zh-cn:验证损坏的 JSON 不会被当作旧版纯文本凭据接受。</para>
    /// <para>en-us:Verifies that malformed JSON is not accepted as a legacy plain-text credential.</para>
    /// </summary>
    [Fact]
    public void Credential_store_should_reject_malformed_json_token()
    {
        var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "air-cloud-reverse-tunnel-token-tests", Guid.NewGuid().ToString("N")));
        try
        {
            const string malformedToken = "{\"version\":1,\"credential\":";
            var path = Path.Combine(directory.FullName, ReverseTunnelEnrollmentCredentialNames.CreateFileName("agent-malformed", ".token"));
            File.WriteAllText(path, malformedToken, Encoding.UTF8);
            var options = new ReverseTunnelOptions
            {
                Enabled = true,
                TunnelMode = ReverseTunnelMode.Server,
                Enrollment = new ReverseTunnelEnrollmentOptions
                {
                    Enabled = true,
                    CredentialDirectory = directory.FullName,
                    ConsumeOnUse = false
                }
            };
            var store = new MountedReverseTunnelEnrollmentCredentialStore(Options.Create(options));

            Assert.False(store.TryConsume("agent-malformed", malformedToken, out var failureReason));
            Assert.Equal("Enrollment credential is invalid.", failureReason);
        }
        finally
        {
            if (Directory.Exists(directory.FullName))
            {
                Directory.Delete(directory.FullName, recursive: true);
            }
        }
    }

    private static X509Certificate2 CreateCa()
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=AirCloudReverseTunnelTestCa", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, true));
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
        return X509CertificateLoader.LoadCertificate(certificate.Export(X509ContentType.Cert));
    }
}


