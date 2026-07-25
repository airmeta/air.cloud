using System.Security.Cryptography.X509Certificates;

namespace Air.Cloud.Modules.ReverseTunnel.Server;

/// <summary>
/// <para>zh-cn:Agent 客户端证书验证器，用于在 gRPC mTLS 握手后把传输层证书映射为 Server 可信的 Agent 身份。</para>
/// <para>en-us:Agent client-certificate validator used after the gRPC mTLS handshake to map the transport certificate to a Server-trusted Agent identity.</para>
/// </summary>
public interface IReverseTunnelAgentCertificateValidator
{
    /// <summary>
    /// <para>zh-cn:验证 Agent 证书链、有效期、吊销状态和证书身份。expectedAgentId 不为空时，证书中的 AgentId 必须与它一致。</para>
    /// <para>en-us:Validates the Agent certificate chain, validity period, revocation status, and certificate identity. When expectedAgentId is not empty, the AgentId in the certificate must match it.</para>
    /// </summary>
    /// <param name="certificate">
    /// <para>zh-cn:gRPC mTLS 握手得到的 Agent 客户端证书。</para>
    /// <para>en-us:Agent client certificate obtained from the gRPC mTLS handshake.</para>
    /// </param>
    /// <param name="expectedAgentId">
    /// <para>zh-cn:可选的预期 Agent 身份，通常来自已认证连接上下文或授权策略。</para>
    /// <para>en-us:Optional expected Agent identity, normally coming from an authenticated connection context or authorization policy.</para>
    /// </param>
    /// <param name="utcNow">
    /// <para>zh-cn:用于有效期判断的 UTC 时间。</para>
    /// <para>en-us:UTC time used for validity-period checks.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:包含 AgentId 或失败原因的校验结果。</para>
    /// <para>en-us:Validation result containing the AgentId or a failure reason.</para>
    /// </returns>
    ReverseTunnelCertificateValidationResult Validate(X509Certificate2 certificate, string? expectedAgentId, DateTimeOffset utcNow);
}
