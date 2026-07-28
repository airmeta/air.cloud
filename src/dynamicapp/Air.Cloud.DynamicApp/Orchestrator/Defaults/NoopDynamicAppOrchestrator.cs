using Air.Cloud.DynamicApp.Contracts.Lifecycle;
using Air.Cloud.DynamicApp.Contracts.Manifest;
using Air.Cloud.DynamicApp.Contracts.Runtime;

namespace Air.Cloud.DynamicApp.Orchestrator;

/// <summary>
/// <para>zh-cn:安全的空编排器实现，默认不执行任何容器操作，避免未配置受控适配器时暴露宿主机权限。</para>
/// <para>en-us:Safe no-op orchestrator implementation that performs no container operations by default, avoiding host privilege exposure without a trusted adapter.</para>
/// </summary>
public sealed class NoopDynamicAppOrchestrator : IDynamicAppOrchestrator
{
    /// <summary>
    /// <para>zh-cn:拒绝容器部署请求，并返回需要配置受控编排器的失败结果。</para>
    /// <para>en-us:Rejects orchestrated deployment requests and returns a failure indicating that a trusted orchestrator must be configured.</para>
    /// </summary>
    /// <param name="manifest">
    /// <para>zh-cn:所属 DynamicApp 清单。</para>
    /// <para>en-us:The owning DynamicApp manifest.</para>
    /// </param>
    /// <param name="mod">
    /// <para>zh-cn:请求部署的 Mod 清单。</para>
    /// <para>en-us:The Mod manifest requested for deployment.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌。</para>
    /// <para>en-us:Cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:始终为失败的操作结果。</para>
    /// <para>en-us:An operation result that always fails.</para>
    /// </returns>
    public Task<DynamicAppOperationResult> DeployAsync(DynamicAppManifest manifest, DynamicModManifest mod, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(mod);

        if (mod.ExecutionMode is not DynamicModExecutionMode.Container and not DynamicModExecutionMode.ExistingContainer)
        {
            return Task.FromResult(DynamicAppOperationResult.Failure($"Mod `{manifest.Id}/{mod.Id}` is not configured for an orchestrated mode."));
        }

        return Task.FromResult(DynamicAppOperationResult.Failure($"No trusted DynamicApp orchestrator is configured for `{manifest.Id}/{mod.Id}:{mod.Version}`."));
    }

    /// <summary>
    /// <para>zh-cn:拒绝容器停止请求；未配置受信编排器时不会对宿主机或容器平台产生副作用。</para>
    /// <para>en-us:Rejects orchestrated stop requests; without a trusted orchestrator, it produces no side effects on the host or container platform.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:请求停止的目标版本键。</para>
    /// <para>en-us:The target version key requested for stop.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌。</para>
    /// <para>en-us:Cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:始终为失败的操作结果。</para>
    /// <para>en-us:An operation result that always fails.</para>
    /// </returns>
    public Task<DynamicAppOperationResult> StopAsync(DynamicModVersionKey key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(DynamicAppOperationResult.Failure($"No trusted DynamicApp orchestrator is configured for `{key}`."));
    }
}
