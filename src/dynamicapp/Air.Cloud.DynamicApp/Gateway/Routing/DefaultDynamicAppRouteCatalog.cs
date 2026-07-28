using Air.Cloud.DynamicApp.Contracts.Runtime;

namespace Air.Cloud.DynamicApp.Gateway;

/// <summary>
/// <para>zh-cn:默认 DynamicApp 路由目录，通过状态存储解析当前活动 Mod 目标。</para>
/// <para>en-us:Default DynamicApp route catalog that resolves the current active Mod target through the state store.</para>
/// </summary>
public sealed class DefaultDynamicAppRouteCatalog : IDynamicAppRouteCatalog
{
    private readonly IDynamicAppStateStore _stateStore;

    /// <summary>
    /// <para>zh-cn:创建默认路由目录。</para>
    /// <para>en-us:Creates the default route catalog.</para>
    /// </summary>
    /// <param name="stateStore">
    /// <para>zh-cn:Manager 发布的状态存储。</para>
    /// <para>en-us:The state store published by Manager.</para>
    /// </param>
    public DefaultDynamicAppRouteCatalog(IDynamicAppStateStore stateStore) => _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));

    /// <summary>
    /// <para>zh-cn:解析请求对应的活动目标；没有 Active/Healthy 目标时返回 null。</para>
    /// <para>en-us:Resolves the active target for a request; returns null when no Active/Healthy target exists.</para>
    /// </summary>
    /// <param name="request">
    /// <para>zh-cn:路由解析请求。</para>
    /// <para>en-us:The route resolution request.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌。</para>
    /// <para>en-us:Cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:活动目标或 null。</para>
    /// <para>en-us:The active target or null.</para>
    /// </returns>
    public ValueTask<DynamicAppRouteTarget?> ResolveAsync(DynamicAppRouteRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromResult<DynamicAppRouteTarget?>(null);
        }

        return _stateStore.TryGetActiveTarget(request.DynamicAppId, request.ModId, out var target)
            ? ValueTask.FromResult<DynamicAppRouteTarget?>(target)
            : ValueTask.FromResult<DynamicAppRouteTarget?>(null);
    }
}
