using Air.Cloud.Modules.ReverseTunnel.Agent;
using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Server;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace Air.Cloud.UnitTest.Modules.ReverseTunnel;

/// <summary>
/// <para>zh-cn:反向隧道 Agent 侧证书材料管理测试集合。</para>
/// <para>en-us:Test suite for reverse tunnel Agent-side certificate material management.</para>
/// </summary>
public sealed class ReverseTunnelAgentCertificateMaterialTests
{
    /// <summary>
    /// <para>zh-cn:验证 Agent 本地生成私钥和 CSR 后，Server 只签 CSR 公钥，私钥内容不会进入配置对象。</para>
    /// <para>en-us:Verifies that the Agent generates a local private key and CSR, the Server signs only the CSR public key, and private-key content does not enter the options object.</para>
    /// </summary>
    [Fact]
    public void Agent_material_store_should_generate_local_key_and_signed_certificate_without_config_secret()
    {
        using var material = TestAgentMaterial.Create();
        var serverOptions = CreateServerOptions(material);
        var agentOptions = CreateAgentOptions(material);
        var certificateAuthority = new MountedReverseTunnelCertificateAuthority(Options.Create(serverOptions));
        var store = new MountedReverseTunnelAgentCertificateMaterialStore(Options.Create(agentOptions));

        var csr = store.CreateCertificateSigningRequest("agent-alpha");
        using var certificate = certificateAuthority.SignAgentCertificate("agent-alpha", csr, DateTimeOffset.UtcNow.AddMinutes(-1));
        using var issuer = certificateAuthority.LoadIssuerCertificate();
        store.SaveIssuedCertificate(certificate.Export(X509ContentType.Cert), issuer.Export(X509ContentType.Cert));
        using var clientCertificate = store.LoadClientCertificate();

        Assert.True(File.Exists(agentOptions.AgentCertificate.PrivateKeyPath));
        Assert.True(File.Exists(agentOptions.AgentCertificate.CertificatePath));
        Assert.True(File.Exists(agentOptions.AgentCertificate.TrustedIssuerPath));
        Assert.True(clientCertificate.HasPrivateKey);
        Assert.Contains("PRIVATE KEY", File.ReadAllText(agentOptions.AgentCertificate.PrivateKeyPath), StringComparison.Ordinal);
        Assert.DoesNotContain("PRIVATE KEY", JsonSerializer.Serialize(agentOptions), StringComparison.Ordinal);
    }

    /// <summary>
    /// <para>zh-cn:验证保存 Server 签发证书时使用原子替换，不留下临时文件，且新证书覆盖旧证书。</para>
    /// <para>en-us:Verifies that saving a Server-issued certificate uses atomic replacement, leaves no temporary files, and replaces the previous certificate.</para>
    /// </summary>
    [Fact]
    public void Agent_material_store_should_atomically_replace_certificate_files()
    {
        using var material = TestAgentMaterial.Create();
        var serverOptions = CreateServerOptions(material);
        var agentOptions = CreateAgentOptions(material);
        var certificateAuthority = new MountedReverseTunnelCertificateAuthority(Options.Create(serverOptions));
        var store = new MountedReverseTunnelAgentCertificateMaterialStore(Options.Create(agentOptions));
        var csr = store.CreateCertificateSigningRequest("agent-alpha");
        using var firstCertificate = certificateAuthority.SignAgentCertificate("agent-alpha", csr, DateTimeOffset.UtcNow.AddDays(-2));
        using var secondCertificate = certificateAuthority.SignAgentCertificate("agent-alpha", csr, DateTimeOffset.UtcNow.AddMinutes(-1));
        using var issuer = certificateAuthority.LoadIssuerCertificate();

        store.SaveIssuedCertificate(firstCertificate.Export(X509ContentType.Cert), issuer.Export(X509ContentType.Cert));
        var firstSerialNumber = store.LoadCertificate().SerialNumber;
        store.SaveIssuedCertificate(secondCertificate.Export(X509ContentType.Cert), issuer.Export(X509ContentType.Cert));
        using var loadedCertificate = store.LoadCertificate();

        Assert.NotEqual(firstSerialNumber, loadedCertificate.SerialNumber);
        Assert.Equal(secondCertificate.SerialNumber, loadedCertificate.SerialNumber);
        Assert.Empty(Directory.GetFiles(material.Directory, "*.tmp"));
    }

    /// <summary>
    /// <para>zh-cn:验证 Agent 续签窗口判断仅基于证书到期时间，未进入窗口时返回 false，进入窗口后返回 true。</para>
    /// <para>en-us:Verifies that the Agent renewal-window decision is based only on certificate expiry and returns false before the window and true inside it.</para>
    /// </summary>
    [Fact]
    public void Agent_material_store_should_detect_renewal_window_from_certificate_expiry()
    {
        using var material = TestAgentMaterial.Create();
        var serverOptions = CreateServerOptions(material);
        var agentOptions = CreateAgentOptions(material);
        var certificateAuthority = new MountedReverseTunnelCertificateAuthority(Options.Create(serverOptions));
        var store = new MountedReverseTunnelAgentCertificateMaterialStore(Options.Create(agentOptions));
        var csr = store.CreateCertificateSigningRequest("agent-alpha");
        using var certificate = certificateAuthority.SignAgentCertificate("agent-alpha", csr, DateTimeOffset.UtcNow.AddMinutes(-1));

        var outsideWindow = new DateTimeOffset(certificate.NotAfter.ToUniversalTime(), TimeSpan.Zero).AddDays(-8);
        var insideWindow = new DateTimeOffset(certificate.NotAfter.ToUniversalTime(), TimeSpan.Zero).AddDays(-6);

        Assert.False(store.ShouldRenew(certificate, renewalWindowDays: 7, outsideWindow));
        Assert.True(store.ShouldRenew(certificate, renewalWindowDays: 7, insideWindow));
    }

    /// <summary>
    /// <para>zh-cn:验证 Agent 角色启用时，配置验证只要求挂载路径和目录，不要求证书文件在初次 Enrollment 前已经存在。</para>
    /// <para>en-us:Verifies that Agent-role validation requires mounted paths and directories but does not require certificate files to exist before initial enrollment.</para>
    /// </summary>
    [Fact]
    public void Options_validation_should_require_agent_paths_without_requiring_existing_certificate_files()
    {
        using var material = TestAgentMaterial.Create();
        var options = CreateAgentOptions(material);

        var result = new ReverseTunnelOptionsValidation().Validate(null, options);

        Assert.False(result.Failed);
        Assert.False(File.Exists(options.AgentCertificate.CertificatePath));
        Assert.False(File.Exists(options.AgentCertificate.PrivateKeyPath));
    }

    private static ReverseTunnelOptions CreateServerOptions(TestAgentMaterial material)
    {
        var options = new ReverseTunnelOptions
        {
            Enabled = true,
            TunnelMode = ReverseTunnelMode.Server,
            CertificateAuthority = new ReverseTunnelCertificateAuthorityOptions
            {
                CertificatePath = material.CaPfxPath,
                CertificatePasswordEnvironmentVariable = material.PasswordEnvironmentVariable
            }
        };
        options.PostConfigure(options, new ConfigurationBuilder().Build());
        return options;
    }

    private static ReverseTunnelOptions CreateAgentOptions(TestAgentMaterial material)
    {
        var options = new ReverseTunnelOptions
        {
            Enabled = true,
            TunnelMode = ReverseTunnelMode.Agent,
            AgentId = " agent-alpha ",
            ServerAddress = " https://reverse-tunnel.example.test/ ",
            AgentCertificate = new ReverseTunnelAgentCertificateOptions
            {
                CertificatePath = Path.Combine(material.Directory, "agent.cer"),
                PrivateKeyPath = Path.Combine(material.Directory, "agent.key"),
                TrustedIssuerPath = Path.Combine(material.Directory, "issuer.cer"),
                PrivateKeySizeBits = 1024
            }
        };
        options.PostConfigure(options, new ConfigurationBuilder().Build());
        return options;
    }

    private sealed class TestAgentMaterial : IDisposable
    {
        private TestAgentMaterial(string directory, string caPfxPath, string passwordEnvironmentVariable)
        {
            Directory = directory;
            CaPfxPath = caPfxPath;
            PasswordEnvironmentVariable = passwordEnvironmentVariable;
        }

        public string Directory { get; }

        public string CaPfxPath { get; }

        public string PasswordEnvironmentVariable { get; }

        public static TestAgentMaterial Create()
        {
            var directory = System.IO.Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "air-cloud-reverse-tunnel-agent-tests", Guid.NewGuid().ToString("N"))).FullName;
            var passwordEnvironmentVariable = $"AIR_CLOUD_REVERSE_TUNNEL_AGENT_TEST_CA_PASSWORD_{Guid.NewGuid():N}";
            const string password = "test-ca-password";
            var caPfxPath = Path.Combine(directory, "ca.pfx");

            using var caKey = RSA.Create(2048);
            var request = new CertificateRequest("CN=AirCloudReverseTunnelAgentTestCa", caKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
            request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, true));
            using var caCertificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
            File.WriteAllBytes(caPfxPath, caCertificate.Export(X509ContentType.Pkcs12, password));
            Environment.SetEnvironmentVariable(passwordEnvironmentVariable, password);

            return new TestAgentMaterial(directory, caPfxPath, passwordEnvironmentVariable);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable(PasswordEnvironmentVariable, null);
            if (System.IO.Directory.Exists(Directory))
            {
                System.IO.Directory.Delete(Directory, recursive: true);
            }
        }
    }
}
