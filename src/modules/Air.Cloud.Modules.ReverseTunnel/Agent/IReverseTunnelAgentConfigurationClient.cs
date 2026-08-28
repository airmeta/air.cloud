using Air.Cloud.Modules.ReverseTunnel.Common.Runtime;

namespace Air.Cloud.Modules.ReverseTunnel.Agent;

/// <summary>
/// <para>zh-cn:Agent 主动从 Server 查询目标配置的客户端契约。</para>
/// <para>en-us:Client contract for an Agent to actively query desired configuration from the Server.</para>
/// </summary>
public interface IReverseTunnelAgentConfigurationClient
{
    /// <summary>
    /// <para>zh-cn:使用当前 mTLS 证书查询高于本地版本的目标配置。</para>
    /// <para>en-us:Uses the current mTLS certificate to query a desired configuration newer than the local version.</para>
    /// </summary>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消 gRPC 查询的令牌。</para>
    /// <para>en-us:Token used to cancel the gRPC query.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:配置查询结果；没有更新时 Snapshot 为空。</para>
    /// <para>en-us:Configuration query result; Snapshot is null when no update is available.</para>
    /// </returns>
    Task<ReverseTunnelAgentConfigurationFetchResult> FetchAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// <para>zh-cn:Agent 主动配置查询结果。</para>
/// <para>en-us:Agent active configuration-fetch result.</para>
/// </summary>
public sealed record ReverseTunnelAgentConfigurationFetchResult(
    ReverseTunnelAgentConfigurationSnapshot? Snapshot,
    long ServerVersion,
    string? FailureReason = null)
{
    /// <summary><para>zh-cn:查询是否成功。</para><para>en-us:Whether the query succeeded.</para></summary>
    public bool Succeeded => string.IsNullOrWhiteSpace(FailureReason);
}
