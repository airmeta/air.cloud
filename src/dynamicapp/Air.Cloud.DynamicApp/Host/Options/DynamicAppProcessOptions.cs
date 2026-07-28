namespace Air.Cloud.DynamicApp.Host;

/// <summary>
/// <para>zh-cn:Process 模式 Mod 启动选项。</para>
/// <para>en-us:Startup options for Process-mode Mods.</para>
/// </summary>
public sealed class DynamicAppProcessOptions
{
    /// <summary>
    /// <para>zh-cn:是否使用 Shell 执行入口文件，默认 false。</para>
    /// <para>en-us:Whether to execute the entry file through the shell; false by default.</para>
    /// </summary>
    public bool UseShellExecute { get; set; }

    /// <summary>
    /// <para>zh-cn:是否重定向标准输出，默认 true；使用 Shell 执行时会被运行器关闭。</para>
    /// <para>en-us:Whether to redirect standard output, true by default; the runner disables it when shell execution is used.</para>
    /// </summary>
    public bool RedirectStandardOutput { get; set; } = true;

    /// <summary>
    /// <para>zh-cn:是否重定向标准错误，默认 true；使用 Shell 执行时会被运行器关闭。</para>
    /// <para>en-us:Whether to redirect standard error, true by default; the runner disables it when shell execution is used.</para>
    /// </summary>
    public bool RedirectStandardError { get; set; } = true;

    /// <summary>
    /// <para>zh-cn:停止时等待进程自行退出的时间，默认 20 秒。</para>
    /// <para>en-us:The time to wait for graceful process exit during stop, 20 seconds by default.</para>
    /// </summary>
    public TimeSpan GracefulStopTimeout { get; set; } = TimeSpan.FromSeconds(20);

    /// <summary>
    /// <para>zh-cn:启动后观察进程是否快速退出的时间窗口，默认 1 秒；用于把端口占用、运行时缺失和启动配置错误直接作为启动失败返回。</para>
    /// <para>en-us:The post-start observation window for quick process exits, one second by default; this returns port conflicts, missing runtimes, and startup configuration errors as startup failures.</para>
    /// </summary>
    public TimeSpan EarlyExitWatchTimeout { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// <para>zh-cn:是否监控非预期进程退出并同步写入 Failed/Unhealthy 状态，默认 true。</para>
    /// <para>en-us:Whether unexpected process exits are monitored and written as Failed/Unhealthy state, true by default.</para>
    /// </summary>
    public bool MonitorUnexpectedExit { get; set; } = true;
}
