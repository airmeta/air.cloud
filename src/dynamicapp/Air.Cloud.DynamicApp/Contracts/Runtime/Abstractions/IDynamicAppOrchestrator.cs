using Air.Cloud.DynamicApp.Contracts.Manifest;

namespace Air.Cloud.DynamicApp.Contracts.Runtime;

/// <summary>
/// <para>zh-cn:DynamicApp 容器编排接口。</para>
/// <para>en-us:DynamicApp orchestration interface for container and existing-container modes.</para>
/// </summary>
public interface IDynamicAppOrchestrator
{
    /// <summary>
    /// <para>zh-cn:部署容器模式 Mod。</para>
    /// <para>en-us:Deploys a Container-mode or ExistingContainer-mode Mod through a trusted orchestration boundary.</para>
    /// </summary>
    /// <param name="manifest">
    /// <para>zh-cn:所属 DynamicApp 清单。</para>
    /// <para>en-us:The owning DynamicApp manifest.</para>
    /// </param>
    /// <param name="mod">
    /// <para>zh-cn:待部署 Mod 清单。</para>
    /// <para>en-us:The Mod manifest to deploy.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌。</para>
    /// <para>en-us:Cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:部署结果。</para>
    /// <para>en-us:The deployment result.</para>
    /// </returns>
    Task<DynamicAppOperationResult> DeployAsync(DynamicAppManifest manifest, DynamicModManifest mod, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:停止指定容器模式 Mod 版本；实现方应只停止自己受控边界内的目标，不能越权操作任意宿主机资源。</para>
    /// <para>en-us:Stops the specified orchestrated Mod version; implementations should stop only targets inside their controlled boundary and must not operate on arbitrary host resources.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:目标版本键，用于解析要停止的容器、Pod 或平台实例。</para>
    /// <para>en-us:Target version key used to resolve the container, Pod, or platform instance to stop.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌。</para>
    /// <para>en-us:Cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:停止结果。</para>
    /// <para>en-us:The stop result.</para>
    /// </returns>
    Task<DynamicAppOperationResult> StopAsync(DynamicModVersionKey key, CancellationToken cancellationToken = default);
}
