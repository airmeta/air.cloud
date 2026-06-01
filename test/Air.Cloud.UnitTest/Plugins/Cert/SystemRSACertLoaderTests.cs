using Air.Cloud.Core.Plugins.Cert;

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Air.Cloud.UnitTest.Plugins.Cert
{
    public class SystemRSACertLoaderTests
    {
        [Fact]
        public void LoadRsaCert_should_export_public_and_private_keys_from_pfx()
        {
            const string password = "unit-test-password";
            var certificatePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.pfx");

            try
            {
                using var rsa = RSA.Create(2048);
                var request = new CertificateRequest(
                    "CN=air-cloud-unit-test",
                    rsa,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                using var certificate = request.CreateSelfSigned(
                    DateTimeOffset.UtcNow.AddMinutes(-1),
                    DateTimeOffset.UtcNow.AddDays(1));

                File.WriteAllBytes(
                    certificatePath,
                    certificate.Export(X509ContentType.Pfx, password));

                var keys = SystemRSACertLoader.LoadRsaCert(certificatePath, password);

                Assert.Contains("BEGIN PUBLIC KEY", keys.Item1);
                Assert.Contains("BEGIN RSA PRIVATE KEY", keys.Item2);
            }
            finally
            {
                if (File.Exists(certificatePath))
                {
                    File.Delete(certificatePath);
                }
            }
        }
    }
}
