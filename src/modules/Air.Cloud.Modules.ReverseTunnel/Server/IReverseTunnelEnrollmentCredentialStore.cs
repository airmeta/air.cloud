namespace Air.Cloud.Modules.ReverseTunnel.Server;

/// <summary>
/// <para>zh-cn:Agent Enrollment 一次性凭据存储。实现必须从挂载磁盘或 Secret 读取凭据，不得从数据库或普通配置读取凭据内容。</para>
/// <para>en-us:One-time credential store for Agent enrollment. Implementations must read credentials from mounted disk or Secret material, never from a database or ordinary configuration.</para>
/// </summary>
public interface IReverseTunnelEnrollmentCredentialStore
{
    /// <summary>
    /// <para>zh-cn:校验并按策略消费 Agent 的一次性 Enrollment 凭据。成功消费后同一个凭据不能再次用于签发证书。</para>
    /// <para>en-us:Validates and consumes an Agent one-time enrollment credential according to policy. After successful consumption, the same credential cannot be used again to issue a certificate.</para>
    /// </summary>
    /// <param name="agentId">
    /// <para>zh-cn:请求签发证书的 Agent 稳定身份。</para>
    /// <para>en-us:Stable Agent identity requesting a certificate.</para>
    /// </param>
    /// <param name="credential">
    /// <para>zh-cn:Agent 提交的一次性 Enrollment 凭据。</para>
    /// <para>en-us:One-time enrollment credential submitted by the Agent.</para>
    /// </param>
    /// <param name="failureReason">
    /// <para>zh-cn:验证失败时的稳定失败原因。</para>
    /// <para>en-us:Stable failure reason when validation fails.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:凭据有效并已按策略消费时返回 true，否则返回 false。</para>
    /// <para>en-us:Returns true when the credential is valid and has been consumed according to policy; otherwise false.</para>
    /// </returns>
    bool TryConsume(string agentId, string credential, out string? failureReason);
}
