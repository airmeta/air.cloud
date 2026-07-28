namespace Air.Cloud.DynamicApp.Contracts.Runtime;

/// <summary>
/// <para>zh-cn:Process 模式 Mod 的日志条目，用于采集 stdout、stderr 和进程退出事件。</para>
/// <para>en-us:Log entry for Process-mode Mods, capturing stdout, stderr, and process exit events.</para>
/// </summary>
public sealed class DynamicAppProcessLogEntry
{
    /// <summary>
    /// <para>zh-cn:日志所属 Mod 版本键。</para>
    /// <para>en-us:Mod version key that owns the log entry.</para>
    /// </summary>
    public DynamicModVersionKey Key { get; set; }

    /// <summary>
    /// <para>zh-cn:日志流名称，例如 stdout、stderr 或 exit。</para>
    /// <para>en-us:Log stream name such as stdout, stderr, or exit.</para>
    /// </summary>
    public string Stream { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:日志消息内容；换行由写入方裁剪。</para>
    /// <para>en-us:Log message content; trailing new lines are trimmed by writers.</para>
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:日志写入 UTC 时间。</para>
    /// <para>en-us:UTC timestamp when the log entry was written.</para>
    /// </summary>
    public DateTimeOffset TimestampUtc { get; set; } = DateTimeOffset.UtcNow;
}
