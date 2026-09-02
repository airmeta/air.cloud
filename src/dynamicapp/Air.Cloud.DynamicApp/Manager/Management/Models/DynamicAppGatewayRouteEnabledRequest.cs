namespace Air.Cloud.DynamicApp.Manager;

/// <summary>
/// <para>zh-cn:动态路由启用状态更新请求。</para>
/// <para>en-us:Dynamic route enabled-state update request.</para>
/// </summary>
public sealed class DynamicAppGatewayRouteEnabledRequest
{
    /// <summary>
    /// <para>zh-cn:是否启用路由。</para>
    /// <para>en-us:Whether the route is enabled.</para>
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// <para>zh-cn:可选的乐观并发时间戳。</para>
    /// <para>en-us:Optional optimistic-concurrency timestamp.</para>
    /// </summary>
    public DateTimeOffset? ExpectedUpdatedAtUtc { get; set; }
}
