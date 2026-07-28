using Air.Cloud.DynamicApp.Contracts.Manifest;

namespace Air.Cloud.DynamicApp.Contracts.Runtime;

/// <summary>
/// <para>zh-cn:DynamicApp Mod 运行器接口。</para>
/// <para>en-us:DynamicApp Mod runner interface.</para>
/// </summary>
public interface IDynamicAppModRunner
{
    /// <summary>
    /// <para>zh-cn:启动指定 Mod。</para>
    /// <para>en-us:Starts the specified Mod.</para>
    /// </summary>
    /// <param name="manifest">
    /// <para>zh-cn:所属 DynamicApp 清单。</para>
    /// <para>en-us:The owning DynamicApp manifest.</para>
    /// </param>
    /// <param name="mod">
    /// <para>zh-cn:待启动 Mod 清单。</para>
    /// <para>en-us:The Mod manifest to start.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌。</para>
    /// <para>en-us:Cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:启动结果。</para>
    /// <para>en-us:The start result.</para>
    /// </returns>
    Task<DynamicAppOperationResult> StartAsync(DynamicAppManifest manifest, DynamicModManifest mod, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:检查指定 Process 模式 Mod 版本是否仍由当前运行器持有并处于运行中；该检查用于修正持久化状态中残留的 Active/Healthy 目标。</para>
    /// <para>en-us:Checks whether the specified Process-mode Mod version is still owned by the current runner and running; this is used to correct stale Active/Healthy targets left in persisted state.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:目标版本键。</para>
    /// <para>en-us:Target version key.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌；取消时返回失败结果而不是抛出异常。</para>
    /// <para>en-us:Cancellation token; when cancelled the method returns a failed result instead of throwing.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:运行检查结果；成功表示当前运行器持有活进程，失败表示目标不存在、已退出或检查被取消。</para>
    /// <para>en-us:The runtime inspection result; success means the current runner owns a live process, while failure means the target is missing, exited, or inspection was cancelled.</para>
    /// </returns>
    Task<DynamicAppOperationResult> InspectAsync(DynamicModVersionKey key, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:停止指定 Mod 版本。</para>
    /// <para>en-us:Stops the specified Mod version.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:目标版本键。</para>
    /// <para>en-us:Target version key.</para>
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
