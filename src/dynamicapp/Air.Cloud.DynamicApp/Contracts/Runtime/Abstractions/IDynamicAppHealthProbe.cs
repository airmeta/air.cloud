using Air.Cloud.DynamicApp.Contracts.Manifest;

namespace Air.Cloud.DynamicApp.Contracts.Runtime;

/// <summary>
/// <para>zh-cn:DynamicApp Mod 健康探测接口。</para>
/// <para>en-us:DynamicApp Mod health probe interface.</para>
/// </summary>
public interface IDynamicAppHealthProbe
{
    /// <summary>
    /// <para>zh-cn:对指定运行目标执行健康探测。</para>
    /// <para>en-us:Executes a health probe for the specified runtime target.</para>
    /// </summary>
    /// <param name="target">
    /// <para>zh-cn:运行目标。</para>
    /// <para>en-us:The runtime target.</para>
    /// </param>
    /// <param name="health">
    /// <para>zh-cn:健康检查清单配置。</para>
    /// <para>en-us:The health check manifest configuration.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌。</para>
    /// <para>en-us:Cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:健康探测结果。</para>
    /// <para>en-us:The health probe result.</para>
    /// </returns>
    Task<DynamicAppHealthProbeResult> ProbeAsync(DynamicAppRouteTarget target, DynamicModHealthManifest health, CancellationToken cancellationToken = default);
}
