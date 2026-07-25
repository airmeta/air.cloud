using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Air.Cloud.Modules.ReverseTunnel.Server;

/// <summary>
/// <para>zh-cn:默认 Agent Enrollment 服务。它先消费一次性挂载凭据，再通过 Server CA 签发不含私钥的 Agent 客户端证书。</para>
/// <para>en-us:Default Agent enrollment service. It first consumes the one-time mounted credential, then uses the Server CA to issue an Agent client certificate without a private key.</para>
/// </summary>
public sealed class ReverseTunnelAgentEnrollmentService : IReverseTunnelAgentEnrollmentService
{
    private readonly IReverseTunnelCertificateAuthority certificateAuthority;
    private readonly IReverseTunnelEnrollmentCredentialStore credentialStore;

    /// <summary>
    /// <para>zh-cn:创建 Agent Enrollment 服务。</para>
    /// <para>en-us:Creates an Agent enrollment service.</para>
    /// </summary>
    /// <param name="certificateAuthority">
    /// <para>zh-cn:用于签发 Agent 客户端证书的 Server CA。</para>
    /// <para>en-us:Server CA used to issue Agent client certificates.</para>
    /// </param>
    /// <param name="credentialStore">
    /// <para>zh-cn:用于校验和消费一次性 Enrollment 凭据的挂载凭据存储。</para>
    /// <para>en-us:Mounted credential store used to validate and consume one-time enrollment credentials.</para>
    /// </param>
    public ReverseTunnelAgentEnrollmentService(
        IReverseTunnelCertificateAuthority certificateAuthority,
        IReverseTunnelEnrollmentCredentialStore credentialStore)
    {
        this.certificateAuthority = certificateAuthority;
        this.credentialStore = credentialStore;
    }

    /// <inheritdoc />
    public ReverseTunnelAgentEnrollmentResult Enroll(string agentId, string credential, byte[] certificateSigningRequest, DateTimeOffset notBeforeUtc)
    {
        if (!credentialStore.TryConsume(agentId, credential, out var failureReason))
        {
            return ReverseTunnelAgentEnrollmentResult.Failure(failureReason ?? "Enrollment credential validation failed.");
        }

        try
        {
            using var issuer = certificateAuthority.LoadIssuerCertificate();
            using var certificate = certificateAuthority.SignAgentCertificate(agentId, certificateSigningRequest, notBeforeUtc);
            return ReverseTunnelAgentEnrollmentResult.Success(
                certificate.Export(X509ContentType.Cert),
                issuer.Export(X509ContentType.Cert),
                certificate.NotAfter.ToUniversalTime());
        }
        catch (Exception exception) when (exception is ArgumentException or CryptographicException or InvalidOperationException)
        {
            return ReverseTunnelAgentEnrollmentResult.Failure(exception.Message);
        }
    }
}
