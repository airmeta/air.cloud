namespace Air.Cloud.DynamicApp.Manager;

/// <summary>
/// <para>zh-cn:基于包存储根目录文件锁的 DynamicApp 部署创建互斥器，用于协调共享挂载目录上的多个 Host 进程。</para>
/// <para>en-us:File-lock based DynamicApp deployment-creation mutex that coordinates multiple Host processes sharing one package-store directory.</para>
/// </summary>
internal sealed class DynamicAppDeploymentFileLock : IAsyncDisposable
{
    private static readonly TimeSpan RetryInterval = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan AcquisitionTimeout = TimeSpan.FromSeconds(30);
    private readonly FileStream _stream;

    private DynamicAppDeploymentFileLock(FileStream stream)
    {
        _stream = stream;
    }

    /// <summary>
    /// <para>zh-cn:在包存储根目录创建并获取部署创建锁；锁文件会保留，锁区域由持有进程独占。</para>
    /// <para>en-us:Creates and acquires the deployment-creation lock under the package-store root; the lock file remains while its region is exclusively held by the owning process.</para>
    /// </summary>
    /// <param name="rootPath">
    /// <para>zh-cn:与其他 Host 共享的包存储根目录。</para>
    /// <para>en-us:The package-store root shared with other Host processes.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:等待锁期间使用的取消令牌。</para>
    /// <para>en-us:The cancellation token used while waiting for the lock.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:已获取锁的异步释放对象。</para>
    /// <para>en-us:An asynchronously disposable object holding the acquired lock.</para>
    /// </returns>
    /// <exception cref="IOException">
    /// <para>zh-cn:超过 30 秒仍无法获取锁时抛出。</para>
    /// <para>en-us:Thrown when the lock cannot be acquired within 30 seconds.</para>
    /// </exception>
    public static Task<DynamicAppDeploymentFileLock> AcquireAsync(string rootPath, CancellationToken cancellationToken = default)
        => AcquireAsync(rootPath, "deployment-create.lock", cancellationToken);

    /// <summary>
    /// <para>zh-cn:获取指定名称的跨进程文件锁；调用方必须在生命周期操作完成后释放锁。</para>
    /// <para>en-us:Acquires a named cross-process file lock; callers must release it after the lifecycle operation completes.</para>
    /// </summary>
    public static async Task<DynamicAppDeploymentFileLock> AcquireAsync(string rootPath, string lockFileName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new ArgumentException("Package-store root path is required.", nameof(rootPath));
        }

        if (OperatingSystem.IsMacOS())
        {
            throw new PlatformNotSupportedException("DynamicApp deployment file locking is supported on Windows and Linux only.");
        }

        var lockDirectory = Path.Combine(Path.GetFullPath(rootPath), ".locks");
        Directory.CreateDirectory(lockDirectory);
        if (string.IsNullOrWhiteSpace(lockFileName) || lockFileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException("Lock file name is invalid.", nameof(lockFileName));
        }

        var lockPath = Path.Combine(lockDirectory, lockFileName);
        var startedAt = DateTime.UtcNow;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            FileStream? stream = null;
            try
            {
                stream = new FileStream(
                    lockPath,
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.ReadWrite | FileShare.Delete,
                    bufferSize: 1,
                    options: FileOptions.Asynchronous);
                stream.Lock(0, 1);
                return new DynamicAppDeploymentFileLock(stream);
            }
            catch (IOException) when (stream != null)
            {
                stream.Dispose();
                if (DateTime.UtcNow - startedAt >= AcquisitionTimeout)
                {
                    throw new IOException($"DynamicApp deployment creation lock `{lockPath}` could not be acquired within {AcquisitionTimeout.TotalSeconds:0} seconds.");
                }

                await Task.Delay(RetryInterval, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// <para>zh-cn:释放文件锁并关闭锁文件句柄。</para>
    /// <para>en-us:Releases the file lock and closes the lock-file handle.</para>
    /// </summary>
    public ValueTask DisposeAsync()
    {
        try
        {
            if (!OperatingSystem.IsMacOS())
            {
                _stream.Unlock(0, 1);
            }
        }
        finally
        {
            _stream.Dispose();
        }

        return ValueTask.CompletedTask;
    }
}
