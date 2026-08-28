using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Server;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Air.Cloud.UnitTest.Modules.ReverseTunnel;

/// <summary>
/// <para>zh-cn:反向隧道 mTLS 证书控制面测试集合。</para>
/// <para>en-us:Test suite for the reverse tunnel mTLS certificate control plane.</para>
/// </summary>
public sealed class ReverseTunnelCertificateTests
{
    /// <summary>
    /// <para>zh-cn:验证模块默认禁用时不会要求 Server CA 挂载文件。</para>
    /// <para>en-us:Verifies that the disabled module does not require Server CA mounted files.</para>
    /// </summary>
    [Fact]
    public void Options_validation_should_ignore_ca_mounts_when_module_is_disabled()
    {
        var options = new ReverseTunnelOptions
        {
            Enabled = false,
            TunnelMode = ReverseTunnelMode.Server
        };
        options.PostConfigure(options, new ConfigurationBuilder().Build());

        var result = new ReverseTunnelOptionsValidation().Validate(null, options);

        Assert.False(result.Failed);
    }

    /// <summary>
    /// <para>zh-cn:验证启用 Server 角色后必须配置存在的 CA PFX 文件和包含密码的环境变量。</para>
    /// <para>en-us:Verifies that enabling the Server role requires an existing CA PFX file and a populated password environment variable.</para>
    /// </summary>
    [Fact]
    public void Options_validation_should_require_mounted_ca_when_server_is_enabled()
    {
        var options = new ReverseTunnelOptions
        {
            Enabled = true,
            TunnelMode = ReverseTunnelMode.Server,
            CertificateAuthority = new ReverseTunnelCertificateAuthorityOptions
            {
                CertificatePath = "X:\\missing-reverse-tunnel-ca.pfx",
                CertificatePasswordEnvironmentVariable = "AIR_CLOUD_REVERSE_TUNNEL_TEST_MISSING_PASSWORD"
            }
        };
        options.PostConfigure(options, new ConfigurationBuilder().Build());

        var result = new ReverseTunnelOptionsValidation().Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("CertificatePath", string.Join(";", result.Failures));
        Assert.Contains("AIR_CLOUD_REVERSE_TUNNEL_TEST_MISSING_PASSWORD", string.Join(";", result.Failures));
    }

    /// <summary>
    /// <para>zh-cn:验证启用 Enrollment 时不再要求挂载 token 凭据目录。</para>
    /// <para>en-us:Verifies that enabling enrollment no longer requires a mounted token credential directory.</para>
    /// </summary>
    [Fact]
    public void Options_validation_should_not_require_enrollment_credential_directory_when_enrollment_is_enabled()
    {
        using var material = TestCertificateMaterial.Create();
        var options = new ReverseTunnelOptions
        {
            Enabled = true,
            TunnelMode = ReverseTunnelMode.Server,
            CertificateAuthority = new ReverseTunnelCertificateAuthorityOptions
            {
                CertificatePath = material.CaPfxPath,
                CertificatePasswordEnvironmentVariable = material.PasswordEnvironmentVariable
            },
            Enrollment = new ReverseTunnelEnrollmentOptions
            {
                Enabled = true,
                CredentialDirectory = Path.Combine(Path.GetTempPath(), "missing-reverse-tunnel-enrollment")
            }
        };
        options.PostConfigure(options, new ConfigurationBuilder().Build());

        var result = new ReverseTunnelOptionsValidation().Validate(null, options);

        Assert.False(result.Failed);
    }

    /// <summary>
    /// <para>zh-cn:验证 Server 可使用挂载 CA 为 Agent CSR 签发证书，并通过 CA 链和 URI SAN 身份校验。</para>
    /// <para>en-us:Verifies that the Server can sign an Agent CSR with the mounted CA and validate it through the CA chain and URI SAN identity.</para>
    /// </summary>
    [Fact]
    public void Certificate_authority_should_sign_and_validate_agent_certificate()
    {
        using var material = TestCertificateMaterial.Create();
        var services = CreateCertificateServices(material);
        using var agentKey = RSA.Create(2048);
        var csr = new CertificateRequest("CN=ignored-by-server", agentKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
            .CreateSigningRequest();

        using var certificate = services.CertificateAuthority.SignAgentCertificate("agent-alpha", csr, DateTimeOffset.UtcNow.AddMinutes(-1));
        var result = services.Validator.Validate(certificate, "agent-alpha", DateTimeOffset.UtcNow);

        Assert.True(result.Succeeded, result.FailureReason);
        Assert.Equal("agent-alpha", result.AgentId);
        Assert.False(certificate.HasPrivateKey);
    }

    /// <summary>
    /// <para>zh-cn:验证证书中的 AgentId 与预期身份不一致时会被拒绝。</para>
    /// <para>en-us:Verifies that a certificate is rejected when the AgentId in the certificate does not match the expected identity.</para>
    /// </summary>
    [Fact]
    public void Certificate_validator_should_reject_agent_id_mismatch()
    {
        using var material = TestCertificateMaterial.Create();
        var services = CreateCertificateServices(material);
        using var agentKey = RSA.Create(2048);
        var csr = new CertificateRequest("CN=ignored-by-server", agentKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
            .CreateSigningRequest();

        using var certificate = services.CertificateAuthority.SignAgentCertificate("agent-alpha", csr, DateTimeOffset.UtcNow.AddMinutes(-1));
        var result = services.Validator.Validate(certificate, "agent-beta", DateTimeOffset.UtcNow);

        Assert.False(result.Succeeded);
        Assert.Equal("Agent certificate identity does not match the expected AgentId.", result.FailureReason);
    }

    /// <summary>
    /// <para>zh-cn:验证已被 Server 标记吊销的证书不会通过校验。</para>
    /// <para>en-us:Verifies that a certificate marked as revoked by the Server does not pass validation.</para>
    /// </summary>
    [Fact]
    public void Certificate_validator_should_reject_revoked_certificate()
    {
        using var material = TestCertificateMaterial.Create();
        var services = CreateCertificateServices(material);
        using var agentKey = RSA.Create(2048);
        var csr = new CertificateRequest("CN=ignored-by-server", agentKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
            .CreateSigningRequest();

        using var issuer = services.CertificateAuthority.LoadIssuerCertificate();
        using var certificate = services.CertificateAuthority.SignAgentCertificate("agent-alpha", csr, DateTimeOffset.UtcNow.AddMinutes(-1));
        services.StatusRegistry.Revoke(issuer.Thumbprint, certificate.SerialNumber);

        var result = services.Validator.Validate(certificate, "agent-alpha", DateTimeOffset.UtcNow);

        Assert.False(result.Succeeded);
        Assert.Equal("Agent certificate has been revoked.", result.FailureReason);
    }

    /// <summary>
    /// <para>zh-cn:验证 Agent 连接认证器会拒绝未携带客户端证书的连接。</para>
    /// <para>en-us:Verifies that the Agent connection authenticator rejects a connection without a client certificate.</para>
    /// </summary>
    [Fact]
    public void Connection_authenticator_should_reject_missing_client_certificate()
    {
        var authenticator = new ReverseTunnelAgentConnectionAuthenticator(new RejectingCertificateValidator("not used"));

        var result = authenticator.Authenticate(null, "agent-alpha", DateTimeOffset.UtcNow);

        Assert.False(result.Succeeded);
        Assert.Equal("Agent client certificate is required.", result.FailureReason);
    }

    /// <summary>
    /// <para>zh-cn:验证 Hello 帧声明的 AgentId 与 mTLS 证书身份不一致时会被拒绝。</para>
    /// <para>en-us:Verifies that the connection is rejected when the AgentId declared by the Hello frame does not match the mTLS certificate identity.</para>
    /// </summary>
    [Fact]
    public void Connection_authenticator_should_reject_hello_agent_id_mismatch()
    {
        using var material = TestCertificateMaterial.Create();
        var services = CreateCertificateServices(material);
        using var agentKey = RSA.Create(2048);
        var csr = new CertificateRequest("CN=ignored-by-server", agentKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
            .CreateSigningRequest();
        using var certificate = services.CertificateAuthority.SignAgentCertificate("agent-alpha", csr, DateTimeOffset.UtcNow.AddMinutes(-1));
        var authenticator = new ReverseTunnelAgentConnectionAuthenticator(services.Validator);

        var result = authenticator.Authenticate(certificate, "agent-beta", DateTimeOffset.UtcNow);

        Assert.False(result.Succeeded);
        Assert.Equal("Hello AgentId does not match the mTLS certificate identity.", result.FailureReason);
    }

    /// <summary>
    /// <para>zh-cn:验证挂载凭据存储会使用 AgentId 摘要文件名校验 token，并在成功后消费凭据文件。</para>
    /// <para>en-us:Verifies that the mounted credential store validates a token by AgentId digest file name and consumes the credential file after success.</para>
    /// </summary>
    [Fact]
    public void Enrollment_credential_store_should_consume_valid_token_once()
    {
        using var material = TestCertificateMaterial.Create();
        var enrollmentDirectory = Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(material.CaPfxPath)!, "enrollment"));
        const string agentId = "agent-alpha";
        const string token = "one-time-token";
        var credentialPath = Path.Combine(enrollmentDirectory.FullName, ReverseTunnelEnrollmentCredentialNames.CreateFileName(agentId, ".token"));
        File.WriteAllText(credentialPath, token);
        var options = CreateServerOptions(material, enrollmentDirectory.FullName);
        var store = new MountedReverseTunnelEnrollmentCredentialStore(Options.Create(options));

        var firstResult = store.TryConsume(agentId, token, out var firstFailure);
        var secondResult = store.TryConsume(agentId, token, out var secondFailure);

        Assert.True(firstResult, firstFailure);
        Assert.False(File.Exists(credentialPath));
        Assert.False(secondResult);
        Assert.Equal("Enrollment credential was not found or has already been consumed.", secondFailure);
    }

    /// <summary>
    /// <para>zh-cn:验证 Enrollment 服务会用一次性 token 为 Agent CSR 签发证书，并拒绝同一 token 的第二次签发。</para>
    /// <para>en-us:Verifies that the enrollment service issues a certificate for an Agent CSR with a one-time token and rejects a second issuance with the same token.</para>
    /// </summary>
    [Fact]
    public void Enrollment_service_should_issue_certificate_and_reject_reused_token()
    {
        using var material = TestCertificateMaterial.Create();
        var enrollmentDirectory = Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(material.CaPfxPath)!, "enrollment"));
        const string agentId = "agent-alpha";
        const string token = "one-time-token";
        File.WriteAllText(Path.Combine(enrollmentDirectory.FullName, ReverseTunnelEnrollmentCredentialNames.CreateFileName(agentId, ".token")), token);
        var options = CreateServerOptions(material, enrollmentDirectory.FullName);
        var certificateAuthority = new MountedReverseTunnelCertificateAuthority(Options.Create(options));
        var store = new MountedReverseTunnelEnrollmentCredentialStore(Options.Create(options));
        var enrollmentService = new ReverseTunnelAgentEnrollmentService(certificateAuthority, store);
        var validator = new ReverseTunnelAgentCertificateValidator(certificateAuthority, new InMemoryReverseTunnelCertificateStatusRegistry());
        using var agentKey = RSA.Create(2048);
        var csr = new CertificateRequest("CN=ignored-by-server", agentKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
            .CreateSigningRequest();

        var firstResult = enrollmentService.Enroll(agentId, token, csr, DateTimeOffset.UtcNow.AddMinutes(-1));
        var secondResult = enrollmentService.Enroll(agentId, token, csr, DateTimeOffset.UtcNow.AddMinutes(-1));

        Assert.True(firstResult.Succeeded, firstResult.FailureReason);
        using var certificate = X509CertificateLoader.LoadCertificate(firstResult.Certificate);
        var validation = validator.Validate(certificate, agentId, DateTimeOffset.UtcNow);
        Assert.True(validation.Succeeded, validation.FailureReason);
        Assert.NotEmpty(firstResult.IssuerCertificate);
        Assert.False(secondResult.Succeeded);
        Assert.Equal("Enrollment credential was not found or has already been consumed.", secondResult.FailureReason);
    }

    /// <summary>
    /// <para>zh-cn:验证未进入续签窗口的有效 Agent 证书不会被提前续签。</para>
    /// <para>en-us:Verifies that a valid Agent certificate outside the renewal window is not renewed early.</para>
    /// </summary>
    [Fact]
    public void Renewal_service_should_reject_certificate_before_renewal_window()
    {
        using var material = TestCertificateMaterial.Create();
        var options = CreateServerOptions(material);
        var services = CreateCertificateServices(material);
        var renewalService = new ReverseTunnelAgentCertificateRenewalService(
            new ReverseTunnelAgentConnectionAuthenticator(services.Validator),
            services.CertificateAuthority,
            Options.Create(options));
        using var currentKey = RSA.Create(2048);
        using var newKey = RSA.Create(2048);
        var currentCsr = new CertificateRequest("CN=ignored-by-server", currentKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
            .CreateSigningRequest();
        var newCsr = new CertificateRequest("CN=ignored-by-server", newKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
            .CreateSigningRequest();
        using var currentCertificate = services.CertificateAuthority.SignAgentCertificate("agent-alpha", currentCsr, DateTimeOffset.UtcNow.AddDays(-1));

        var result = renewalService.Renew(currentCertificate, "agent-alpha", newCsr, DateTimeOffset.UtcNow);

        Assert.False(result.Succeeded);
        Assert.Equal("Agent certificate is not inside the renewal window.", result.FailureReason);
    }

    /// <summary>
    /// <para>zh-cn:验证进入续签窗口的有效 Agent 证书可以用新的 CSR 换取下一张证书。</para>
    /// <para>en-us:Verifies that a valid Agent certificate inside the renewal window can exchange a new CSR for the next certificate.</para>
    /// </summary>
    [Fact]
    public void Renewal_service_should_issue_next_certificate_inside_renewal_window()
    {
        using var material = TestCertificateMaterial.Create();
        var options = CreateServerOptions(material);
        var services = CreateCertificateServices(material);
        var renewalService = new ReverseTunnelAgentCertificateRenewalService(
            new ReverseTunnelAgentConnectionAuthenticator(services.Validator),
            services.CertificateAuthority,
            Options.Create(options));
        using var currentKey = RSA.Create(2048);
        using var newKey = RSA.Create(2048);
        var currentCsr = new CertificateRequest("CN=ignored-by-server", currentKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
            .CreateSigningRequest();
        var newCsr = new CertificateRequest("CN=ignored-by-server", newKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
            .CreateSigningRequest();
        using var currentCertificate = services.CertificateAuthority.SignAgentCertificate("agent-alpha", currentCsr, DateTimeOffset.UtcNow.AddDays(-24));

        var result = renewalService.Renew(currentCertificate, "agent-alpha", newCsr, DateTimeOffset.UtcNow);

        Assert.True(result.Succeeded, result.FailureReason);
        using var renewedCertificate = X509CertificateLoader.LoadCertificate(result.Certificate);
        var validation = services.Validator.Validate(renewedCertificate, "agent-alpha", DateTimeOffset.UtcNow);
        Assert.True(validation.Succeeded, validation.FailureReason);
        Assert.False(renewedCertificate.HasPrivateKey);
        Assert.NotEqual(currentCertificate.SerialNumber, renewedCertificate.SerialNumber);
        Assert.NotEmpty(result.IssuerCertificate);
    }

    private static CertificateServices CreateCertificateServices(TestCertificateMaterial material)
    {
        var options = CreateServerOptions(material);

        var certificateAuthority = new MountedReverseTunnelCertificateAuthority(Options.Create(options));
        var statusRegistry = new InMemoryReverseTunnelCertificateStatusRegistry();
        var validator = new ReverseTunnelAgentCertificateValidator(certificateAuthority, statusRegistry);
        return new CertificateServices(certificateAuthority, statusRegistry, validator);
    }

    private static ReverseTunnelOptions CreateServerOptions(TestCertificateMaterial material, string? enrollmentDirectory = null)
    {
        var options = new ReverseTunnelOptions
        {
            Enabled = true,
            TunnelMode = ReverseTunnelMode.Server,
            CertificateAuthority = new ReverseTunnelCertificateAuthorityOptions
            {
                CertificatePath = material.CaPfxPath,
                CertificatePasswordEnvironmentVariable = material.PasswordEnvironmentVariable
            },
            Enrollment = new ReverseTunnelEnrollmentOptions
            {
                Enabled = !string.IsNullOrWhiteSpace(enrollmentDirectory),
                CredentialDirectory = enrollmentDirectory ?? string.Empty
            }
        };
        options.PostConfigure(options, new ConfigurationBuilder().Build());
        return options;
    }

    private sealed record CertificateServices(
        IReverseTunnelCertificateAuthority CertificateAuthority,
        IReverseTunnelCertificateStatusRegistry StatusRegistry,
        IReverseTunnelAgentCertificateValidator Validator);

    private sealed class RejectingCertificateValidator : IReverseTunnelAgentCertificateValidator
    {
        private readonly string failureReason;

        public RejectingCertificateValidator(string failureReason)
        {
            this.failureReason = failureReason;
        }

        public ReverseTunnelCertificateValidationResult Validate(X509Certificate2 certificate, string? expectedAgentId, DateTimeOffset utcNow)
        {
            return ReverseTunnelCertificateValidationResult.Failure(failureReason);
        }
    }

    private sealed class TestCertificateMaterial : IDisposable
    {
        private TestCertificateMaterial(string caPfxPath, string passwordEnvironmentVariable)
        {
            CaPfxPath = caPfxPath;
            PasswordEnvironmentVariable = passwordEnvironmentVariable;
        }

        public string CaPfxPath { get; }

        public string PasswordEnvironmentVariable { get; }

        public static TestCertificateMaterial Create()
        {
            var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "air-cloud-reverse-tunnel-tests", Guid.NewGuid().ToString("N")));
            var passwordEnvironmentVariable = $"AIR_CLOUD_REVERSE_TUNNEL_TEST_CA_PASSWORD_{Guid.NewGuid():N}";
            const string password = "test-ca-password";
            var caPfxPath = Path.Combine(directory.FullName, "ca.pfx");

            using var caKey = RSA.Create(2048);
            var request = new CertificateRequest("CN=AirCloudReverseTunnelTestCa", caKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
            request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, true));
            using var caCertificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
            File.WriteAllBytes(caPfxPath, caCertificate.Export(X509ContentType.Pkcs12, password));
            Environment.SetEnvironmentVariable(passwordEnvironmentVariable, password);

            return new TestCertificateMaterial(caPfxPath, passwordEnvironmentVariable);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable(PasswordEnvironmentVariable, null);
            var directory = Path.GetDirectoryName(CaPfxPath);
            if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}


