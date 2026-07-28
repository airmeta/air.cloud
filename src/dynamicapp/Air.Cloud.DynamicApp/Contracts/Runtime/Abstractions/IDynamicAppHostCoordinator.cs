using Air.Cloud.DynamicApp.Contracts.Manifest;

namespace Air.Cloud.DynamicApp.Contracts.Runtime;

/// <summary>
/// <para>zh-cn:DynamicApp Host 发布、启动、健康探测和激活协调接口。</para>
/// <para>en-us:DynamicApp Host coordination interface for publishing, starting, health probing, and activation.</para>
/// </summary>
public interface IDynamicAppHostCoordinator
{
    /// <summary>
    /// <para>zh-cn:部署清单中的所有 Mod，并在健康探测通过后激活对应版本。</para>
    /// <para>en-us:Deploys all Mods in a manifest and activates each version after its health probe succeeds.</para>
    /// </summary>
    /// <param name="manifest">
    /// <para>zh-cn:要部署的 DynamicApp 清单。</para>
    /// <para>en-us:The DynamicApp manifest to deploy.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌。</para>
    /// <para>en-us:Cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:部署协调结果。</para>
    /// <para>en-us:The deployment coordination result.</para>
    /// </returns>
    Task<DynamicAppOperationResult> DeployAsync(DynamicAppManifest manifest, CancellationToken cancellationToken = default);
}
