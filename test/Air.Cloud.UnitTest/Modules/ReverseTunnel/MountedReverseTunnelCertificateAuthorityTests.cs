using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Server;

using Microsoft.Extensions.Options;

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Air.Cloud.UnitTest.Modules.ReverseTunnel;

/// <summary>
/// <para>zh-cn:验证挂载式 AirTunnel CA 证书加载行为。</para>
/// <para>en-us:Verifies mounted AirTunnel certificate-authority loading behavior.</para>
/// </summary>
public sealed class MountedReverseTunnelCertificateAuthorityTests
{
    /// <summary>
    /// <para>zh-cn:验证环境变量末尾包含换行时，加载器会清理换行并成功打开 CA PFX。</para>
    /// <para>en-us:Verifies that a trailing line break in the environment variable is trimmed before opening the CA PFX.</para>
    /// </summary>
    [Fact]
    public void LoadIssuerCertificate_should_trim_environment_value()
    {
        var directory = Path.Combine(Path.GetTempPath(), "air-cloud-reverse-tunnel-ca-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var passwordVariable = $"AIR_TUNNEL_CA_PASSWORD_TEST_{Guid.NewGuid():N}";
        var password = "test-password-123";
        var pfxPath = Path.Combine(directory, "ca.pfx");

        try
        {
            using var key = RSA.Create(2048);
            var request = new CertificateRequest("CN=AirTunnel Test CA", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
            using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(1));
            File.WriteAllBytes(pfxPath, certificate.Export(X509ContentType.Pfx, password));
            Environment.SetEnvironmentVariable(passwordVariable, password + "\r\n");

            var options = new ReverseTunnelOptions
            {
                CertificateAuthority = new ReverseTunnelCertificateAuthorityOptions
                {
                    CertificatePath = pfxPath,
                    CertificatePasswordEnvironmentVariable = passwordVariable
                }
            };

            using var loaded = new MountedReverseTunnelCertificateAuthority(Options.Create(options)).LoadIssuerCertificate();

            Assert.True(loaded.HasPrivateKey);
            Assert.Equal("CN=AirTunnel Test CA", loaded.Subject);
        }
        finally
        {
            Environment.SetEnvironmentVariable(passwordVariable, null);
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}


