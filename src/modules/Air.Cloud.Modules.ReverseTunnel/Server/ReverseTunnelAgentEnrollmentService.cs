using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using Air.Cloud.Modules.ReverseTunnel.Common.Enrollment;
using Air.Cloud.Modules.ReverseTunnel.Common.Options;

using Microsoft.Extensions.Options;

namespace Air.Cloud.Modules.ReverseTunnel.Server;

/// <summary>
/// <para>zh-cn:默认 Agent Enrollment 服务。它先用 Server CA 私钥解密客户端随机 GUID 并执行 replay 防护，再通过 Server CA 签发不含私钥的 Agent 客户端证书。</para>
/// <para>en-us:Default Agent enrollment service. It first decrypts the client random GUID with the Server CA private key and applies replay protection, then uses the Server CA to issue an Agent client certificate without a private key.</para>
/// </summary>
public sealed class ReverseTunnelAgentEnrollmentService : IReverseTunnelAgentEnrollmentService
{
    private readonly IReverseTunnelCertificateAuthority certificateAuthority;
    private readonly IReverseTunnelEnrollmentReplayStore replayStore;
    private readonly IReverseTunnelEnrollmentCredentialStore? legacyCredentialStore;
    private readonly int replayWindowSeconds;

    /// <summary>
    /// <para>zh-cn:创建使用加密 GUID 和 replay store 的 Enrollment 服务。</para>
    /// <para>en-us:Creates an enrollment service that uses encrypted GUIDs and a replay store.</para>
    /// </summary>
    public ReverseTunnelAgentEnrollmentService(
        IReverseTunnelCertificateAuthority certificateAuthority,
        IReverseTunnelEnrollmentReplayStore replayStore,
        IOptions<ReverseTunnelOptions>? options = null)
    {
        this.certificateAuthority = certificateAuthority;
        this.replayStore = replayStore;
        replayWindowSeconds = Math.Max(1, options?.Value.Enrollment.ReplayWindowSeconds ?? 300);
    }

    /// <summary>
    /// <para>zh-cn:创建仅用于旧版 token 测试或迁移的兼容服务。</para>
    /// <para>en-us:Creates a compatibility service for legacy token tests or migrations only.</para>
    /// </summary>
    /// <param name="certificateAuthority">
    /// <para>zh-cn:用于签发 Agent 客户端证书的 Server CA。</para>
    /// <para>en-us:Server CA used to issue Agent client certificates.</para>
    /// </param>
    /// <param name="credentialStore">
    /// <para>zh-cn:用于校验和消费一次性 Enrollment 凭据的挂载凭据存储。</para>
    /// <para>en-us:Mounted credential store used to validate and consume one-time enrollment credentials.</para>
    /// </param>
    [Obsolete("Use the encrypted-GUID enrollment constructor.")]
    public ReverseTunnelAgentEnrollmentService(
        IReverseTunnelCertificateAuthority certificateAuthority,
        IReverseTunnelEnrollmentCredentialStore credentialStore)
    {
        this.certificateAuthority = certificateAuthority;
        legacyCredentialStore = credentialStore;
        replayStore = new InMemoryReverseTunnelEnrollmentReplayStore();
        replayWindowSeconds = 300;
    }

    /// <inheritdoc />
    public ReverseTunnelAgentEnrollmentResult Enroll(string agentId, byte[] encryptedGuid, byte[] certificateSigningRequest, DateTimeOffset notBeforeUtc)
    {
        if (string.IsNullOrWhiteSpace(agentId))
        {
            return ReverseTunnelAgentEnrollmentResult.Failure("AgentId is required for enrollment.");
        }

        using var issuer = certificateAuthority.LoadIssuerCertificate();
        if (!ReverseTunnelEnrollmentCipher.TryDecryptGuid(issuer, encryptedGuid, out var guid))
        {
            return ReverseTunnelAgentEnrollmentResult.Failure("Encrypted enrollment GUID could not be decrypted.");
        }

        if (!replayStore.TryClaim(agentId, guid, DateTimeOffset.UtcNow, TimeSpan.FromSeconds(replayWindowSeconds)))
        {
            return ReverseTunnelAgentEnrollmentResult.Failure("Encrypted enrollment GUID was already used or is invalid.");
        }

        try
        {
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

    /// <summary>
    /// <para>zh-cn:兼容旧版 token 调用方；新客户端和 gRPC 协议不会调用此方法。</para>
    /// <para>en-us:Preserves legacy token callers; new clients and the gRPC protocol never invoke this method.</para>
    /// </summary>
    [Obsolete("Use the encrypted-GUID enrollment overload.")]
    public ReverseTunnelAgentEnrollmentResult Enroll(string agentId, string credential, byte[] certificateSigningRequest, DateTimeOffset notBeforeUtc)
    {
        string? failureReason = null;
        if (legacyCredentialStore is null || !legacyCredentialStore.TryConsume(agentId, credential, out failureReason))
        {
            return ReverseTunnelAgentEnrollmentResult.Failure(failureReason ?? "Legacy enrollment token support is unavailable.");
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
