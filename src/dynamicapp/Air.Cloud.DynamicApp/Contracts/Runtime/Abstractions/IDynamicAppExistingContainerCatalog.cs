namespace Air.Cloud.DynamicApp.Contracts.Runtime;

/// <summary>
/// <para>zh-cn:已有容器目录接口，用于从受控编排边界读取可被 DynamicApp 登记的容器列表。</para>
/// <para>en-us:Existing-container catalog interface used to read containers that can be registered by DynamicApp from the controlled orchestration boundary.</para>
/// </summary>
public interface IDynamicAppExistingContainerCatalog
{
    /// <summary>
    /// <para>zh-cn:列出现有容器及其即时状态；实现方必须限制在自身受控边界内，不能越权枚举无关平台资源。</para>
    /// <para>en-us:Lists existing containers with their live status; implementations must stay within their controlled boundary and must not enumerate unrelated platform resources.</para>
    /// </summary>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌。</para>
    /// <para>en-us:Cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:已有容器视图集合。</para>
    /// <para>en-us:A collection of existing-container views.</para>
    /// </returns>
    Task<IReadOnlyCollection<DynamicAppExistingContainerView>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:按 DynamicApp 目标键读取已登记容器或托管实例的即时状态；该方法只查询容器引擎标准，不发起 HTTP 健康检查或路由测试。</para>
    /// <para>en-us:Reads the live status of a registered container or hosted instance for a DynamicApp target key; this method only queries the container engine standard and does not issue HTTP health checks or route tests.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:已登记的 DynamicApp/Mod/Version 目标键，用于解析绑定的容器名称。</para>
    /// <para>en-us:The registered DynamicApp/Mod/Version target key used to resolve the bound container name.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌。</para>
    /// <para>en-us:Cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:容器或托管实例的即时状态视图；目标不存在或容器引擎不可用时由实现抛出异常。</para>
    /// <para>en-us:The live status view for the container or hosted instance; implementations throw when the target is missing or the container engine is unavailable.</para>
    /// </returns>
    Task<DynamicAppExistingContainerView> InspectAsync(DynamicModVersionKey key, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:按运行实例读取已有容器状态；默认实现兼容尚未支持实例级目录的旧实现。</para>
    /// <para>en-us:Reads an existing-container status by runtime instance; the default implementation keeps compatibility with catalogs that only support package-level lookup.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:运行实例键。</para>
    /// <para>en-us:The runtime-instance key.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌。</para>
    /// <para>en-us:Cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:容器即时状态。</para>
    /// <para>en-us:The live container status.</para>
    /// </returns>
    Task<DynamicAppExistingContainerView> InspectAsync(DynamicAppRuntimeKey key, CancellationToken cancellationToken = default)
        => InspectAsync(key.PackageKey, cancellationToken);
}
