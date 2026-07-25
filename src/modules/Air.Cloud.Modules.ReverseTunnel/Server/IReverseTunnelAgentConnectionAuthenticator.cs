using System.Security.Cryptography.X509Certificates;

namespace Air.Cloud.Modules.ReverseTunnel.Server;

/// <summary>
/// <para>zh-cn:Agent 连接认证器，用于把 gRPC 连接上的客户端证书和 Hello 帧声明合并为可信 Agent 身份。</para>
/// <para>en-us:Agent connection authenticator that combines the client certificate on a gRPC connection with the Hello-frame declaration into a trusted Agent identity.</para>
/// </summary>
public interface IReverseTunnelAgentConnectionAuthenticator
{
    /// <summary>
    /// <para>zh-cn:认证 Agent 连接。客户端证书必须存在并通过证书验证器；Hello 帧中的 AgentId 如果提供，则必须与证书身份一致。</para>
    /// <para>en-us:Authenticates an Agent connection. The client certificate must be present and pass the certificate validator; when the Hello frame supplies AgentId, it must match the certificate identity.</para>
    /// </summary>
    /// <param name="clientCertificate">
    /// <para>zh-cn:gRPC mTLS 握手得到的客户端证书；为空时认证失败。</para>
    /// <para>en-us:Client certificate obtained from the gRPC mTLS handshake; authentication fails when it is null.</para>
    /// </param>
    /// <param name="declaredAgentId">
    /// <para>zh-cn:Hello 帧声明的 AgentId。该值只用于一致性校验，不能作为可信身份来源。</para>
    /// <para>en-us:AgentId declared by the Hello frame. This value is used only for consistency checks and must not be treated as the trusted identity source.</para>
    /// </param>
    /// <param name="utcNow">
    /// <para>zh-cn:用于证书有效期判断的 UTC 当前时间。</para>
    /// <para>en-us:Current UTC time used for certificate validity checks.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:认证成功时返回证书中的 AgentId，失败时返回稳定失败原因。</para>
    /// <para>en-us:Returns the AgentId from the certificate when authentication succeeds, or a stable failure reason when it fails.</para>
    /// </returns>
    ReverseTunnelCertificateValidationResult Authenticate(X509Certificate2? clientCertificate, string? declaredAgentId, DateTimeOffset utcNow);
}
