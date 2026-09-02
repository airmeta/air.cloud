using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

using Air.Cloud.DynamicApp.Contracts.Identifiers;
using Air.Cloud.DynamicApp.Contracts.Runtime;
using Air.Cloud.DynamicApp.Manager;

namespace Air.Cloud.DynamicApp.Host;

/// <summary>
/// <para>zh-cn:进程内 DynamicApp 运行操作协调器，按运行实例和包版本维护异步互斥锁。</para>
/// <para>en-us:In-process DynamicApp runtime operation coordinator maintaining asynchronous mutexes by runtime instance and package version.</para>
/// </summary>
public sealed class DynamicAppRuntimeOperationCoordinator : IDynamicAppRuntimeOperationCoordinator
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.OrdinalIgnoreCase);
    private readonly DynamicAppPackageStoreOptions _packageOptions;

    /// <summary>
    /// <para>zh-cn:创建生命周期操作协调器；除进程内互斥外，还在共享包目录上建立跨进程文件锁。</para>
    /// <para>en-us:Creates the lifecycle operation coordinator with both in-process mutexes and cross-process locks under the shared package directory.</para>
    /// </summary>
    public DynamicAppRuntimeOperationCoordinator(DynamicAppPackageStoreOptions? packageOptions = null)
    {
        _packageOptions = packageOptions ?? new DynamicAppPackageStoreOptions();
    }

    /// <summary>
    /// <para>zh-cn:获取指定运行实例的互斥锁。</para>
    /// <para>en-us:Acquires the mutex for the specified runtime instance.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:运行实例键。</para>
    /// <para>en-us:The runtime-instance key.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:等待锁期间使用的取消令牌。</para>
    /// <para>en-us:The cancellation token used while waiting.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:异步释放句柄。</para>
    /// <para>en-us:An asynchronously disposable lock handle.</para>
    /// </returns>
    public Task<IAsyncDisposable> AcquireAsync(DynamicAppRuntimeKey key, CancellationToken cancellationToken = default)
        => AcquireCoreAsync($"runtime:{key}", cancellationToken);

    /// <summary>
    /// <para>zh-cn:获取指定 DynamicApp/Mod 家族的互斥锁；激活、归档和版本删除都会影响该家族。</para>
    /// <para>en-us:Acquires the mutex for a DynamicApp/Mod family; activation, archiving, and version deletion can affect the whole family.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:家族成员键；仅使用其中的 DynamicApp/Mod 标识生成锁。</para>
    /// <para>en-us:A family-member key; only its DynamicApp/Mod identifiers are used to generate the lock.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:等待锁期间使用的取消令牌。</para>
    /// <para>en-us:The cancellation token used while waiting.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:异步释放句柄。</para>
    /// <para>en-us:An asynchronously disposable lock handle.</para>
    /// </returns>
    public Task<IAsyncDisposable> AcquirePackageAsync(DynamicModVersionKey key, CancellationToken cancellationToken = default)
        => AcquireCoreAsync($"package-family:{key.DynamicAppId}/{key.ModId}", cancellationToken);

    private async Task<IAsyncDisposable> AcquireCoreAsync(string lockKey, CancellationToken cancellationToken)
    {
        var semaphore = _locks.GetOrAdd(lockKey, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        DynamicAppDeploymentFileLock? fileLock = null;
        try
        {
            var lockName = $"runtime-{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(lockKey))).ToLowerInvariant()}.lock";
            fileLock = await DynamicAppDeploymentFileLock
                .AcquireAsync(_packageOptions.RootPath, lockName, cancellationToken)
                .ConfigureAwait(false);
            return new Releaser(semaphore, fileLock);
        }
        catch
        {
            fileLock?.DisposeAsync().AsTask().GetAwaiter().GetResult();
            semaphore.Release();
            throw;
        }
    }

    private sealed class Releaser : IAsyncDisposable
    {
        private SemaphoreSlim? _semaphore;
        private DynamicAppDeploymentFileLock? _fileLock;

        public Releaser(SemaphoreSlim semaphore, DynamicAppDeploymentFileLock fileLock)
        {
            _semaphore = semaphore;
            _fileLock = fileLock;
        }

        public ValueTask DisposeAsync()
        {
            Interlocked.Exchange(ref _fileLock, null)?.DisposeAsync().AsTask().GetAwaiter().GetResult();
            Interlocked.Exchange(ref _semaphore, null)?.Release();
            return ValueTask.CompletedTask;
        }
    }
}
