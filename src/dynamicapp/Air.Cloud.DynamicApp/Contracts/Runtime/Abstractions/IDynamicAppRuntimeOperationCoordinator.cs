using Air.Cloud.DynamicApp.Contracts.Identifiers;

namespace Air.Cloud.DynamicApp.Contracts.Runtime;

/// <summary>
/// <para>zh-cn:DynamicApp 运行实例操作协调器，确保同一个运行实例的启动、停止、重启、自愈、恢复和健康检查不会并发交叉执行。</para>
/// <para>en-us:Coordinates DynamicApp runtime-instance operations so start, stop, restart, self-healing, restore, and health checks for one instance cannot interleave.</para>
/// </summary>
public interface IDynamicAppRuntimeOperationCoordinator
{
    /// <summary>
    /// <para>zh-cn:获取指定运行实例的异步操作锁；调用方必须释放返回的句柄以允许后续操作继续。</para>
    /// <para>en-us:Acquires the asynchronous operation lock for one runtime instance; callers must dispose the returned handle to release subsequent operations.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:需要互斥保护的运行实例键。</para>
    /// <para>en-us:The runtime-instance key to protect.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:等待锁期间使用的取消令牌。</para>
    /// <para>en-us:The cancellation token used while waiting for the lock.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:已获取锁的异步释放句柄。</para>
    /// <para>en-us:An asynchronously disposable handle holding the acquired lock.</para>
    /// </returns>
    Task<IAsyncDisposable> AcquireAsync(DynamicAppRuntimeKey key, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:获取指定 DynamicApp/Mod 家族范围的异步操作锁，供激活、归档和版本删除等会影响多个版本的操作使用。</para>
    /// <para>en-us:Acquires an asynchronous DynamicApp/Mod-family operation lock for activation, archiving, version deletion, and other operations that can affect multiple versions.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:需要互斥保护的家族成员键；锁范围由其 DynamicApp/Mod 部分决定。</para>
    /// <para>en-us:A family-member key to protect; the lock scope is determined by its DynamicApp/Mod parts.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:等待锁期间使用的取消令牌。</para>
    /// <para>en-us:The cancellation token used while waiting for the lock.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:已获取锁的异步释放句柄。</para>
    /// <para>en-us:An asynchronously disposable handle holding the acquired lock.</para>
    /// </returns>
    Task<IAsyncDisposable> AcquirePackageAsync(DynamicModVersionKey key, CancellationToken cancellationToken = default);
}
