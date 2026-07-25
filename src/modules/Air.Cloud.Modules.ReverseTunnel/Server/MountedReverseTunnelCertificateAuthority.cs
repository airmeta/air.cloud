using Air.Cloud.Modules.ReverseTunnel.Common.Options;

using Microsoft.Extensions.Options;

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Air.Cloud.Modules.ReverseTunnel.Server;

/// <summary>
/// <para>zh-cn:从挂载磁盘或 Secret 加载 CA PFX 的 Server 证书颁发机构。PFX 密码只从环境变量读取，证书材料不会写入数据库或普通配置。</para>
/// <para>en-us:Server certificate authority that loads the CA PFX from mounted disk or Secret material. The PFX password is read only from an environment variable, and certificate material is not written to a database or ordinary configuration.</para>
/// </summary>
public sealed class MountedReverseTunnelCertificateAuthority : IReverseTunnelCertificateAuthority
{
    private readonly IOptions<ReverseTunnelOptions> options;

    /// <summary>
    /// <para>zh-cn:创建基于挂载文件的证书颁发机构。</para>
    /// <para>en-us:Creates a certificate authority backed by mounted files.</para>
    /// </summary>
    /// <param name="options">
    /// <para>zh-cn:包含 CA 路径和密码环境变量名的反向隧道选项。</para>
    /// <para>en-us:Reverse tunnel options containing the CA path and password environment-variable name.</para>
    /// </param>
    public MountedReverseTunnelCertificateAuthority(IOptions<ReverseTunnelOptions> options)
    {
        this.options = options;
    }

    /// <inheritdoc />
    public X509Certificate2 LoadIssuerCertificate()
    {
        var certificateAuthority = options.Value.CertificateAuthority;
        var password = Environment.GetEnvironmentVariable(certificateAuthority.CertificatePasswordEnvironmentVariable);
        if (string.IsNullOrEmpty(password))
        {
            throw new InvalidOperationException($"Environment variable '{certificateAuthority.CertificatePasswordEnvironmentVariable}' must contain the mounted CA PFX password.");
        }

        var certificate = X509CertificateLoader.LoadPkcs12FromFile(
            certificateAuthority.CertificatePath,
            password,
            X509KeyStorageFlags.EphemeralKeySet,
            Pkcs12LoaderLimits.Defaults);

        if (!certificate.HasPrivateKey)
        {
            certificate.Dispose();
            throw new InvalidOperationException("Reverse tunnel CA certificate must include a private key.");
        }

        return certificate;
    }

    /// <inheritdoc />
    public X509Certificate2 SignAgentCertificate(string agentId, byte[] certificateSigningRequest, DateTimeOffset notBeforeUtc)
    {
        if (string.IsNullOrWhiteSpace(agentId))
        {
            throw new ArgumentException("AgentId is required.", nameof(agentId));
        }

        if (certificateSigningRequest is null || certificateSigningRequest.Length == 0)
        {
            throw new ArgumentException("Certificate signing request is required.", nameof(certificateSigningRequest));
        }

        using var issuer = LoadIssuerCertificate();
        var loadedRequest = CertificateRequest.LoadSigningRequest(
            certificateSigningRequest,
            HashAlgorithmName.SHA256,
            CertificateRequestLoadOptions.Default,
            RSASignaturePadding.Pkcs1);

        var subject = new X500DistinguishedName($"CN={EscapeDistinguishedNameValue(agentId.Trim())}");
        var request = new CertificateRequest(subject, loadedRequest.PublicKey, HashAlgorithmName.SHA256);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
        request.CertificateExtensions.Add(CreateClientAuthenticationExtension());
        request.CertificateExtensions.Add(CreateSubjectAlternativeNameExtension(agentId.Trim()));

        var serialNumber = RandomNumberGenerator.GetBytes(16);
        serialNumber[0] &= 0x7F;
        var notAfterUtc = notBeforeUtc.AddDays(options.Value.CertificateAuthority.AgentCertificateLifetimeDays);
        using var rsaIssuerKey = issuer.GetRSAPrivateKey();
        if (rsaIssuerKey is not null)
        {
            return request.Create(issuer.SubjectName, X509SignatureGenerator.CreateForRSA(rsaIssuerKey, RSASignaturePadding.Pkcs1), notBeforeUtc, notAfterUtc, serialNumber);
        }

        using var ecdsaIssuerKey = issuer.GetECDsaPrivateKey();
        if (ecdsaIssuerKey is not null)
        {
            return request.Create(issuer.SubjectName, X509SignatureGenerator.CreateForECDsa(ecdsaIssuerKey), notBeforeUtc, notAfterUtc, serialNumber);
        }

        throw new InvalidOperationException("Reverse tunnel CA certificate must use an RSA or ECDSA private key.");
    }

    private static X509EnhancedKeyUsageExtension CreateClientAuthenticationExtension()
    {
        var usages = new OidCollection
        {
            new Oid("1.3.6.1.5.5.7.3.2", "Client Authentication")
        };
        return new X509EnhancedKeyUsageExtension(usages, true);
    }

    private static X509Extension CreateSubjectAlternativeNameExtension(string agentId)
    {
        var subjectAlternativeName = new SubjectAlternativeNameBuilder();
        subjectAlternativeName.AddUri(new Uri($"air-cloud-reverse-tunnel://agent/{Uri.EscapeDataString(agentId)}"));
        return subjectAlternativeName.Build(critical: false);
    }

    private static string EscapeDistinguishedNameValue(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace(",", "\\,", StringComparison.Ordinal)
            .Replace("+", "\\+", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("<", "\\<", StringComparison.Ordinal)
            .Replace(">", "\\>", StringComparison.Ordinal)
            .Replace(";", "\\;", StringComparison.Ordinal);
    }
}
