using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Air.Cloud.Modules.ReverseTunnel.Server;

/// <summary>
/// <para>zh-cn:默认 Agent 客户端证书验证器。它使用挂载 CA 作为自定义信任根，验证证书链、有效期、吊销状态和证书身份。</para>
/// <para>en-us:Default Agent client-certificate validator. It uses the mounted CA as a custom trust root and validates the certificate chain, validity period, revocation status, and certificate identity.</para>
/// </summary>
public sealed class ReverseTunnelAgentCertificateValidator : IReverseTunnelAgentCertificateValidator
{
    private const string ClientAuthenticationOid = "1.3.6.1.5.5.7.3.2";
    private readonly IReverseTunnelCertificateAuthority certificateAuthority;
    private readonly IReverseTunnelCertificateStatusRegistry certificateStatusRegistry;

    /// <summary>
    /// <para>zh-cn:创建 Agent 客户端证书验证器。</para>
    /// <para>en-us:Creates an Agent client-certificate validator.</para>
    /// </summary>
    /// <param name="certificateAuthority">
    /// <para>zh-cn:用于加载可信 CA 的证书颁发机构。</para>
    /// <para>en-us:Certificate authority used to load the trusted CA.</para>
    /// </param>
    /// <param name="certificateStatusRegistry">
    /// <para>zh-cn:用于判断证书是否已被 Server 吊销的状态注册表。</para>
    /// <para>en-us:Status registry used to determine whether a certificate has been revoked by the Server.</para>
    /// </param>
    public ReverseTunnelAgentCertificateValidator(
        IReverseTunnelCertificateAuthority certificateAuthority,
        IReverseTunnelCertificateStatusRegistry certificateStatusRegistry)
    {
        this.certificateAuthority = certificateAuthority;
        this.certificateStatusRegistry = certificateStatusRegistry;
    }

    /// <inheritdoc />
    public ReverseTunnelCertificateValidationResult Validate(X509Certificate2 certificate, string? expectedAgentId, DateTimeOffset utcNow)
    {
        if (certificate is null)
        {
            return ReverseTunnelCertificateValidationResult.Failure("Agent certificate is required.");
        }

        if (utcNow < certificate.NotBefore.ToUniversalTime() || utcNow > certificate.NotAfter.ToUniversalTime())
        {
            return ReverseTunnelCertificateValidationResult.Failure("Agent certificate is outside its validity period.");
        }

        var agentId = ReverseTunnelAgentCertificateIdentity.GetAgentId(certificate);
        if (string.IsNullOrWhiteSpace(agentId))
        {
            return ReverseTunnelCertificateValidationResult.Failure("Agent certificate does not contain an AgentId.");
        }

        if (!string.IsNullOrWhiteSpace(expectedAgentId) && !string.Equals(agentId, expectedAgentId.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return ReverseTunnelCertificateValidationResult.Failure("Agent certificate identity does not match the expected AgentId.");
        }

        if (!HasClientAuthenticationUsage(certificate))
        {
            return ReverseTunnelCertificateValidationResult.Failure("Agent certificate is not valid for client authentication.");
        }

        using var issuer = certificateAuthority.LoadIssuerCertificate();
        if (certificateStatusRegistry.IsRevoked(issuer.Thumbprint, certificate.SerialNumber))
        {
            return ReverseTunnelCertificateValidationResult.Failure("Agent certificate has been revoked.");
        }

        using var chain = new X509Chain();
        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        chain.ChainPolicy.CustomTrustStore.Add(issuer);
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        chain.ChainPolicy.VerificationTime = utcNow.UtcDateTime;
        chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;

        if (!chain.Build(certificate))
        {
            return ReverseTunnelCertificateValidationResult.Failure("Agent certificate chain is not trusted by the mounted CA.");
        }

        return ReverseTunnelCertificateValidationResult.Success(agentId, issuer.Thumbprint, certificate.SerialNumber, certificate.Subject);
    }

    private static bool HasClientAuthenticationUsage(X509Certificate2 certificate)
    {
        var enhancedKeyUsage = certificate.Extensions.OfType<X509EnhancedKeyUsageExtension>().FirstOrDefault();
        return enhancedKeyUsage?.EnhancedKeyUsages.Cast<Oid>().Any(oid => oid.Value == ClientAuthenticationOid) == true;
    }
}
