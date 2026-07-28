using System.Collections.Concurrent;
using System.Diagnostics;

using Air.Cloud.DynamicApp.Contracts.Lifecycle;
using Air.Cloud.DynamicApp.Contracts.Manifest;
using Air.Cloud.DynamicApp.Contracts.Runtime;

namespace Air.Cloud.DynamicApp.Host;

/// <summary>
/// <para>zh-cn:Process 模式 Mod 运行器，负责在 Compact Host 内以子进程方式启动、停止和监控 Mod。</para>
/// <para>en-us:Process-mode Mod runner responsible for starting, stopping, and monitoring Mods as child processes inside Compact Host.</para>
/// </summary>
public sealed class ProcessDynamicAppModRunner : IDynamicAppModRunner, IDisposable
{
    private readonly ConcurrentDictionary<string, byte> _expectedStops = new(StringComparer.OrdinalIgnoreCase);
    private readonly IDynamicAppProcessLogSink _logSink;
    private readonly DynamicAppProcessOptions _options;
    private readonly ConcurrentDictionary<string, Process> _processes = new(StringComparer.OrdinalIgnoreCase);
    private readonly IDynamicAppStateStore _stateStore;
    private bool _disposed;

    /// <summary>
    /// <para>zh-cn:创建 Process 模式运行器。</para>
    /// <para>en-us:Creates a Process-mode runner.</para>
    /// </summary>
    /// <param name="stateStore">
    /// <para>zh-cn:状态存储，用于发布启动、停止和退出结果。</para>
    /// <para>en-us:The state store used to publish start, stop, and exit results.</para>
    /// </param>
    /// <param name="logSink">
    /// <para>zh-cn:进程日志 Sink，用于写入 stdout、stderr 和退出事件。</para>
    /// <para>en-us:Process log sink used to write stdout, stderr, and exit events.</para>
    /// </param>
    /// <param name="options">
    /// <para>zh-cn:进程启动选项；为 null 时使用安全默认值。</para>
    /// <para>en-us:Process startup options; safe defaults are used when null.</para>
    /// </param>
    public ProcessDynamicAppModRunner(IDynamicAppStateStore stateStore, IDynamicAppProcessLogSink logSink, DynamicAppProcessOptions? options = null)
    {
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
        _logSink = logSink ?? throw new ArgumentNullException(nameof(logSink));
        _options = options ?? new DynamicAppProcessOptions();
    }

    /// <summary>
    /// <para>zh-cn:启动 Process 模式 Mod，将目标状态发布为 Starting，并采集输出和退出事件；.NET DLL 工件会在 Host 容器内通过 dotnet 启动。</para>
    /// <para>en-us:Starts a Process-mode Mod, publishes its target state as Starting, and begins collecting output and exit events; .NET DLL artifacts are launched through dotnet inside the Host container.</para>
    /// </summary>
    /// <param name="manifest">
    /// <para>zh-cn:所属 DynamicApp Manifest。</para>
    /// <para>en-us:The owning DynamicApp manifest.</para>
    /// </param>
    /// <param name="mod">
    /// <para>zh-cn:要启动的 Mod Manifest。</para>
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
    public async Task<DynamicAppOperationResult> StartAsync(DynamicAppManifest manifest, DynamicModManifest mod, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(mod);

        if (cancellationToken.IsCancellationRequested)
        {
            return DynamicAppOperationResult.Failure("Start was cancelled.");
        }

        if (mod.ExecutionMode != DynamicModExecutionMode.Process)
        {
            return DynamicAppOperationResult.Failure($"Mod `{mod.Id}` is not configured for Process mode.");
        }

        if (string.IsNullOrWhiteSpace(mod.Artifact.File))
        {
            return DynamicAppOperationResult.Failure($"Mod `{mod.Id}` does not declare Artifact.File.");
        }

        var key = new DynamicModVersionKey(manifest.Id, mod.Id, mod.Version);
        var runtime = InspectTrackedProcess(key);
        if (runtime.Succeeded)
        {
            return DynamicAppOperationResult.Failure($"Process target `{key}` is already running.");
        }

        var fullPath = ResolveArtifactPath(mod.Artifact.File);
        if (!File.Exists(fullPath))
        {
            return DynamicAppOperationResult.Failure($"Process target `{key}` artifact `{fullPath}` does not exist.");
        }

        var startInfo = CreateStartInfo(fullPath, mod.Artifact.Arguments);
        var process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true
        };

        AttachLogHandlers(process, key);
        process.Exited += (_, _) => OnProcessExited(key, process);
        try
        {
            if (!process.Start())
            {
                process.Dispose();
                return DynamicAppOperationResult.Failure($"Process target `{key}` failed to start.");
            }
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception or ObjectDisposedException)
        {
            process.Dispose();
            return DynamicAppOperationResult.Failure($"Process target `{key}` failed to start. {exception.Message}");
        }

        _processes[key.ToString()] = process;
        BeginOutputRead(process);
        _stateStore.UpsertTarget(new DynamicAppRouteTarget
        {
            Key = key,
            BaseAddress = ResolveBaseAddress(mod),
            RoutePrefix = mod.Route.Prefix,
            HealthPath = mod.Health.Path,
            HealthProbeTimeoutSeconds = mod.Health.StartupTimeoutSeconds,
            State = DynamicModLifecycleState.Starting,
            HealthStatus = DynamicModHealthStatus.Starting,
            InstanceId = process.Id.ToString(),
            LastStatusMessage = $"Process target `{key}` started with pid `{process.Id}`."
        });

        var earlyExit = await WatchEarlyExitAsync(key, cancellationToken).ConfigureAwait(false);
        return earlyExit.Succeeded
            ? DynamicAppOperationResult.Success($"Process target `{key}` started with pid `{process.Id}`.")
            : earlyExit;
    }

    /// <summary>
    /// <para>zh-cn:检查当前运行器是否仍持有指定目标的活进程；已退出的进程会从运行器缓存中移除并释放。</para>
    /// <para>en-us:Checks whether the current runner still owns a live process for the target; exited processes are removed from the runner cache and disposed.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:目标版本键。</para>
    /// <para>en-us:Target version key.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌；取消时返回失败结果。</para>
    /// <para>en-us:Cancellation token; cancellation returns a failed result.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:进程运行检查结果；成功表示进程仍存活，失败会说明未跟踪或已退出。</para>
    /// <para>en-us:The process runtime inspection result; success means the process is alive, and failure explains whether it is untracked or exited.</para>
    /// </returns>
    public Task<DynamicAppOperationResult> InspectAsync(DynamicModVersionKey key, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return Task.FromResult(cancellationToken.IsCancellationRequested
            ? DynamicAppOperationResult.Failure("Runtime inspection was cancelled.")
            : InspectTrackedProcess(key));
    }

    private DynamicAppOperationResult InspectTrackedProcess(DynamicModVersionKey key)
    {
        if (!_processes.TryGetValue(key.ToString(), out var process))
        {
            return DynamicAppOperationResult.Failure($"Process target `{key}` is not tracked by the current runner.");
        }

        try
        {
            if (!process.HasExited)
            {
                return DynamicAppOperationResult.Success($"Process target `{key}` is running with pid `{process.Id}`.");
            }

            _processes.TryRemove(key.ToString(), out _);
            var message = $"Process target `{key}` already exited with exit code `{SafeExitCode(process)}`.";
            process.Dispose();
            return DynamicAppOperationResult.Failure(message);
        }
        catch (InvalidOperationException exception)
        {
            _processes.TryRemove(key.ToString(), out _);
            process.Dispose();
            return DynamicAppOperationResult.Failure($"Process target `{key}` cannot be inspected. {exception.Message}");
        }
    }

    /// <summary>
    /// <para>zh-cn:停止 Process 模式 Mod；先等待优雅退出，超时后结束进程并发布 Stopped 状态。</para>
    /// <para>en-us:Stops a Process-mode Mod; waits for graceful exit first, then kills after timeout and publishes Stopped state.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:要停止的目标键。</para>
    /// <para>en-us:The target key to stop.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌。</para>
    /// <para>en-us:Cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:停止结果。</para>
    /// <para>en-us:The stop result.</para>
    /// </returns>
    public async Task<DynamicAppOperationResult> StopAsync(DynamicModVersionKey key, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_processes.TryGetValue(key.ToString(), out var process))
        {
            return DynamicAppOperationResult.Failure($"Process target `{key}` is not running.");
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return DynamicAppOperationResult.Failure("Stop was cancelled.");
        }

        _expectedStops[key.ToString()] = 0;
        try
        {
            if (!process.HasExited)
            {
                process.CloseMainWindow();
                using var timeout = new CancellationTokenSource(_options.GracefulStopTimeout);
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, cancellationToken);
                try
                {
                    await process.WaitForExitAsync(linked.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    process.Kill(entireProcessTree: true);
                    await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
                }
            }

            _processes.TryRemove(key.ToString(), out _);
            await WriteLogAsync(key, "exit", $"Process target `{key}` stopped with exit code `{SafeExitCode(process)}`.", cancellationToken).ConfigureAwait(false);
            _stateStore.UpdateState(key, DynamicModLifecycleState.Stopped, DynamicModHealthStatus.Stopped, $"Process target `{key}` stopped.");
            return DynamicAppOperationResult.Success($"Process target `{key}` stopped.");
        }
        finally
        {
            _expectedStops.TryRemove(key.ToString(), out _);
            process.Dispose();
        }
    }

    /// <summary>
    /// <para>zh-cn:释放运行器，结束仍由当前运行器持有的子进程。</para>
    /// <para>en-us:Disposes the runner and terminates child processes still owned by this runner.</para>
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var pair in _processes.ToArray())
        {
            _expectedStops[pair.Key] = 0;
            try
            {
                if (!pair.Value.HasExited)
                {
                    pair.Value.Kill(entireProcessTree: true);
                }
            }
            catch (InvalidOperationException)
            {
                // The process may exit between the state check and the kill call.
            }
            finally
            {
                pair.Value.Dispose();
            }
        }

        _processes.Clear();
        _expectedStops.Clear();
        _disposed = true;
    }

    private void AttachLogHandlers(Process process, DynamicModVersionKey key)
    {
        if (process.StartInfo.RedirectStandardOutput)
        {
            process.OutputDataReceived += (_, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.Data))
                {
                    _ = WriteLogAsync(key, "stdout", args.Data, CancellationToken.None);
                }
            };
        }

        if (process.StartInfo.RedirectStandardError)
        {
            process.ErrorDataReceived += (_, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.Data))
                {
                    _ = WriteLogAsync(key, "stderr", args.Data, CancellationToken.None);
                }
            };
        }
    }

    private ProcessStartInfo CreateStartInfo(string fullPath, string? arguments)
    {
        var isDotNetAssembly = string.Equals(Path.GetExtension(fullPath), ".dll", StringComparison.OrdinalIgnoreCase);
        var fileName = isDotNetAssembly ? "dotnet" : fullPath;
        var processArguments = isDotNetAssembly
            ? $"{QuoteArgument(fullPath)} {arguments ?? string.Empty}".Trim()
            : arguments ?? string.Empty;

        return new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = processArguments,
            WorkingDirectory = Path.GetDirectoryName(fullPath) ?? AppContext.BaseDirectory,
            UseShellExecute = _options.UseShellExecute,
            RedirectStandardOutput = !_options.UseShellExecute && _options.RedirectStandardOutput,
            RedirectStandardError = !_options.UseShellExecute && _options.RedirectStandardError
        };
    }

    private async Task<DynamicAppOperationResult> WatchEarlyExitAsync(DynamicModVersionKey key, CancellationToken cancellationToken)
    {
        var timeout = _options.EarlyExitWatchTimeout <= TimeSpan.Zero
            ? TimeSpan.Zero
            : _options.EarlyExitWatchTimeout;
        if (timeout == TimeSpan.Zero)
        {
            return DynamicAppOperationResult.Success($"Process target `{key}` early-exit watch is disabled.");
        }

        try
        {
            await Task.Delay(timeout, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return DynamicAppOperationResult.Failure("Start was cancelled.");
        }

        if (!_processes.TryGetValue(key.ToString(), out var process))
        {
            return DynamicAppOperationResult.Failure($"Process target `{key}` exited during startup and is no longer tracked by the runner.");
        }

        try
        {
            if (!process.HasExited)
            {
                return DynamicAppOperationResult.Success($"Process target `{key}` survived the startup observation window.");
            }

            _processes.TryRemove(key.ToString(), out _);
            var message = $"Process target `{key}` exited during startup with exit code `{SafeExitCode(process)}`.";
            await WriteLogAsync(key, "exit", message, CancellationToken.None).ConfigureAwait(false);
            _stateStore.UpdateState(key, DynamicModLifecycleState.Failed, DynamicModHealthStatus.Unhealthy, message);
            process.Dispose();
            return DynamicAppOperationResult.Failure(message);
        }
        catch (InvalidOperationException exception)
        {
            _processes.TryRemove(key.ToString(), out _);
            process.Dispose();
            return DynamicAppOperationResult.Failure($"Process target `{key}` cannot be inspected during startup. {exception.Message}");
        }
    }

    private static string ResolveArtifactPath(string file)
    {
        return Path.IsPathFullyQualified(file)
            ? Path.GetFullPath(file)
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, file));
    }

    private static string QuoteArgument(string value)
    {
        return value.Contains(' ') || value.Contains('\t')
            ? $"\"{value.Replace("\"", "\\\"", StringComparison.Ordinal)}\""
            : value;
    }

    private static void BeginOutputRead(Process process)
    {
        if (process.StartInfo.RedirectStandardOutput)
        {
            process.BeginOutputReadLine();
        }

        if (process.StartInfo.RedirectStandardError)
        {
            process.BeginErrorReadLine();
        }
    }

    private void OnProcessExited(DynamicModVersionKey key, Process process)
    {
        if (!_options.MonitorUnexpectedExit || _expectedStops.ContainsKey(key.ToString()))
        {
            return;
        }

        if (!_processes.TryRemove(key.ToString(), out _))
        {
            return;
        }

        var message = $"Process target `{key}` exited unexpectedly with exit code `{SafeExitCode(process)}`.";
        _ = WriteLogAsync(key, "exit", message, CancellationToken.None);
        _stateStore.UpdateState(key, DynamicModLifecycleState.Failed, DynamicModHealthStatus.Unhealthy, message);
        process.Dispose();
    }

    private Task WriteLogAsync(DynamicModVersionKey key, string stream, string message, CancellationToken cancellationToken)
    {
        return _logSink.WriteAsync(new DynamicAppProcessLogEntry
        {
            Key = key,
            Stream = stream,
            Message = message.Trim(),
            TimestampUtc = DateTimeOffset.UtcNow
        }, cancellationToken);
    }

    private static int? SafeExitCode(Process process)
    {
        try
        {
            return process.HasExited ? process.ExitCode : null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static Uri ResolveBaseAddress(DynamicModManifest mod)
    {
        if (Uri.TryCreate(mod.Artifact.Endpoint, UriKind.Absolute, out var configured))
        {
            return configured;
        }

        var parsed = TryResolveFromArguments(mod.Artifact.Arguments);
        return parsed ?? new Uri("http://127.0.0.1");
    }

    private static Uri? TryResolveFromArguments(string? arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments))
        {
            return null;
        }

        var tokens = arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (var index = 0; index < tokens.Length; index++)
        {
            var token = tokens[index];
            var value = token.StartsWith("--urls=", StringComparison.OrdinalIgnoreCase)
                ? token["--urls=".Length..]
                : token.Equals("--urls", StringComparison.OrdinalIgnoreCase) && index + 1 < tokens.Length
                    ? tokens[index + 1]
                    : null;

            var uri = TryCreatePreferredUri(value);
            if (uri != null)
            {
                return uri;
            }
        }

        return null;
    }

    private static Uri? TryCreatePreferredUri(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        foreach (var candidate in value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri))
            {
                continue;
            }

            if (uri.Host is "*" or "+" or "0.0.0.0")
            {
                var builder = new UriBuilder(uri)
                {
                    Host = "127.0.0.1"
                };
                return builder.Uri;
            }

            return uri;
        }

        return null;
    }
}
