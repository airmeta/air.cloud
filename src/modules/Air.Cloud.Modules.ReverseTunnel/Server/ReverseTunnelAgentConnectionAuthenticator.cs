using System.Security.Cryptography.X509Certificates;

namespace Air.Cloud.Modules.ReverseTunnel.Server;

/// <summary>
/// <para>zh-cn:默认 Agent 连接认证器。它强制连接携带客户端证书，并确保 Hello 帧声明不会覆盖或冒充证书身份。</para>
/// <para>en-us:Default Agent connection authenticator. It requires a client certificate on the connection and ensures the Hello-frame declaration cannot override or impersonate certificate identity.</para>
/// </summary>
public sealed class ReverseTunnelAgentConnectionAuthenticator : IReverseTunnelAgentConnectionAuthenticator
{
    private readonly IReverseTunnelAgentCertificateValidator certificateValidator;

    /// <summary>
    /// <para>zh-cn:创建 Agent 连接认证器。</para>
    /// <para>en-us:Creates an Agent connection authenticator.</para>
    /// </summary>
    /// <param name="certificateValidator">
    /// <para>zh-cn:用于验证客户端证书链、有效期、用途和吊销状态的证书验证器。</para>
    /// <para>en-us:Certificate validator used to validate the client certificate chain, validity period, usage, and revocation status.</para>
    /// </param>
    public ReverseTunnelAgentConnectionAuthenticator(IReverseTunnelAgentCertificateValidator certificateValidator)
    {
        this.certificateValidator = certificateValidator;
    }

    /// <inheritdoc />
    public ReverseTunnelCertificateValidationResult Authenticate(X509Certificate2? clientCertificate, string? declaredAgentId, DateTimeOffset utcNow)
    {
        if (clientCertificate is null)
        {
            return ReverseTunnelCertificateValidationResult.Failure("Agent client certificate is required.");
        }

        var result = certificateValidator.Validate(clientCertificate, null, utcNow);
        if (!result.Succeeded)
        {
            return result;
        }

        if (!string.IsNullOrWhiteSpace(declaredAgentId)
            && !string.Equals(result.AgentId, declaredAgentId.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return ReverseTunnelCertificateValidationResult.Failure("Hello AgentId does not match the mTLS certificate identity.");
        }

        return result;
    }
}
