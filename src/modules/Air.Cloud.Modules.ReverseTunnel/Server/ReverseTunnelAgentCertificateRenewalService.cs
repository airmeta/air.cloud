using Air.Cloud.Modules.ReverseTunnel.Common.Options;

using Microsoft.Extensions.Options;

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Air.Cloud.Modules.ReverseTunnel.Server;

/// <summary>
/// <para>zh-cn:默认 Agent 证书续签服务。它要求当前证书仍有效且进入续签窗口，然后使用 Server CA 签发下一张不含私钥的客户端证书。</para>
/// <para>en-us:Default Agent certificate-renewal service. It requires the current certificate to remain valid and to be inside the renewal window, then uses the Server CA to issue the next client certificate without a private key.</para>
/// </summary>
public sealed class ReverseTunnelAgentCertificateRenewalService : IReverseTunnelAgentCertificateRenewalService
{
    private readonly IReverseTunnelAgentConnectionAuthenticator connectionAuthenticator;
    private readonly IReverseTunnelCertificateAuthority certificateAuthority;
    private readonly IOptions<ReverseTunnelOptions> options;

    /// <summary>
    /// <para>zh-cn:创建 Agent 证书续签服务。</para>
    /// <para>en-us:Creates an Agent certificate-renewal service.</para>
    /// </summary>
    /// <param name="connectionAuthenticator">
    /// <para>zh-cn:用于认证当前 mTLS 客户端证书并校验请求 AgentId 一致性的连接认证器。</para>
    /// <para>en-us:Connection authenticator used to authenticate the current mTLS client certificate and verify request AgentId consistency.</para>
    /// </param>
    /// <param name="certificateAuthority">
    /// <para>zh-cn:用于签发下一张 Agent 客户端证书的 Server CA。</para>
    /// <para>en-us:Server CA used to issue the next Agent client certificate.</para>
    /// </param>
    /// <param name="options">
    /// <para>zh-cn:包含续签窗口和证书有效期策略的反向隧道选项。</para>
    /// <para>en-us:Reverse tunnel options containing renewal-window and certificate-lifetime policy.</para>
    /// </param>
    public ReverseTunnelAgentCertificateRenewalService(
        IReverseTunnelAgentConnectionAuthenticator connectionAuthenticator,
        IReverseTunnelCertificateAuthority certificateAuthority,
        IOptions<ReverseTunnelOptions> options)
    {
        this.connectionAuthenticator = connectionAuthenticator;
        this.certificateAuthority = certificateAuthority;
        this.options = options;
    }

    /// <inheritdoc />
    public ReverseTunnelAgentCertificateRenewalResult Renew(X509Certificate2? currentCertificate, string? declaredAgentId, byte[] certificateSigningRequest, DateTimeOffset utcNow)
    {
        var authentication = connectionAuthenticator.Authenticate(currentCertificate, declaredAgentId, utcNow);
        if (!authentication.Succeeded || string.IsNullOrWhiteSpace(authentication.AgentId))
        {
            return ReverseTunnelAgentCertificateRenewalResult.Failure(authentication.FailureReason ?? "Agent certificate authentication failed.");
        }

        if (certificateSigningRequest is null || certificateSigningRequest.Length == 0)
        {
            return ReverseTunnelAgentCertificateRenewalResult.Failure("Certificate signing request is required.");
        }

        var renewalWindow = TimeSpan.FromDays(options.Value.CertificateAuthority.RenewalWindowDays);
        if (utcNow.UtcDateTime < currentCertificate!.NotAfter.ToUniversalTime().Subtract(renewalWindow))
        {
            return ReverseTunnelAgentCertificateRenewalResult.Failure("Agent certificate is not inside the renewal window.");
        }

        try
        {
            using var issuer = certificateAuthority.LoadIssuerCertificate();
            using var renewedCertificate = certificateAuthority.SignAgentCertificate(authentication.AgentId, certificateSigningRequest, utcNow.AddMinutes(-1));
            return ReverseTunnelAgentCertificateRenewalResult.Success(
                renewedCertificate.Export(X509ContentType.Cert),
                issuer.Export(X509ContentType.Cert),
                renewedCertificate.NotAfter.ToUniversalTime());
        }
        catch (Exception exception) when (exception is ArgumentException or CryptographicException or InvalidOperationException)
        {
            return ReverseTunnelAgentCertificateRenewalResult.Failure(exception.Message);
        }
    }
}
