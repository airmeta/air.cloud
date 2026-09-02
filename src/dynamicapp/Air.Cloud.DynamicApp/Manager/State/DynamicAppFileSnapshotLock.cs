namespace Air.Cloud.DynamicApp.Manager;

/// <summary>
/// <para>zh-cn:为共享控制面快照提供跨进程排他锁；调用方应在重载、修改和原子写回期间持有该锁。</para>
/// <para>en-us:Provides an inter-process exclusive lock for shared control-plane snapshots; callers should hold it while reloading, mutating, and atomically replacing a snapshot.</para>
/// </summary>
internal sealed class DynamicAppFileSnapshotLock : IDisposable
{
    private static readonly TimeSpan RetryInterval = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan AcquisitionTimeout = TimeSpan.FromSeconds(30);
    private readonly FileStream _stream;

    private DynamicAppFileSnapshotLock(FileStream stream)
    {
        _stream = stream;
    }

    /// <summary>
    /// <para>zh-cn:获取与指定快照路径关联的跨进程文件锁。</para>
    /// <para>en-us:Acquires the inter-process file lock associated with a snapshot path.</para>
    /// </summary>
    /// <param name="snapshotPath">
    /// <para>zh-cn:共享快照路径。</para>
    /// <para>en-us:The shared snapshot path.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:已获取的锁句柄。</para>
    /// <para>en-us:The acquired lock handle.</para>
    /// </returns>
    public static DynamicAppFileSnapshotLock Acquire(string snapshotPath)
    {
        if (string.IsNullOrWhiteSpace(snapshotPath))
        {
            throw new ArgumentException("Snapshot path is required.", nameof(snapshotPath));
        }

        var fullPath = Path.GetFullPath(snapshotPath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var lockPath = fullPath + ".lock";
        var startedAt = DateTime.UtcNow;
        while (true)
        {
            FileStream? stream = null;
            try
            {
                stream = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite,
                    FileShare.ReadWrite | FileShare.Delete, 1, FileOptions.WriteThrough);
                if (!OperatingSystem.IsMacOS())
                {
                    stream.Lock(0, 1);
                }
                return new DynamicAppFileSnapshotLock(stream);
            }
            catch (IOException) when (stream != null)
            {
                stream.Dispose();
                if (DateTime.UtcNow - startedAt >= AcquisitionTimeout)
                {
                    throw new IOException($"Snapshot lock `{lockPath}` could not be acquired within {AcquisitionTimeout.TotalSeconds:0} seconds.");
                }

                Thread.Sleep(RetryInterval);
            }
        }
    }

    /// <summary>
    /// <para>zh-cn:释放快照锁。</para>
    /// <para>en-us:Releases the snapshot lock.</para>
    /// </summary>
    public void Dispose()
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
    }
}
