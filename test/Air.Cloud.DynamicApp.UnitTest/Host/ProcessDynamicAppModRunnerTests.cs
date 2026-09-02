using System.Diagnostics;

using Air.Cloud.DynamicApp.Contracts.Identifiers;
using Air.Cloud.DynamicApp.Contracts.Lifecycle;
using Air.Cloud.DynamicApp.Contracts.Manifest;
using Air.Cloud.DynamicApp.Contracts.Runtime;
using Air.Cloud.DynamicApp.Host;
using Air.Cloud.DynamicApp.Manager;

namespace Air.Cloud.DynamicApp.UnitTest.Host;

/// <summary>
/// <para>zh-cn:Process 模式运行器生命周期回归测试。</para>
/// <para>en-us:Regression tests for Process-mode runner lifecycle behavior.</para>
/// </summary>
public sealed class ProcessDynamicAppModRunnerTests
{
    /// <summary>
    /// <para>zh-cn:验证启动观察期间取消会终止已经创建的子进程，而不是留下继续占用端口的孤儿进程。</para>
    /// <para>en-us:Verifies that cancellation during startup observation terminates an already-created child process instead of leaving an orphan process holding its endpoint.</para>
    /// </summary>
    [Fact]
    public async Task StartAsync_should_kill_child_process_when_startup_is_cancelled()
    {
        var script = CreateLongRunningScript();
        try
        {
            var key = CreateKey();
            var stateStore = new InMemoryDynamicAppStateStore();
            using var runner = new ProcessDynamicAppModRunner(
                stateStore,
                new InMemoryDynamicAppProcessLogSink(),
                new DynamicAppProcessOptions
                {
                    EarlyExitWatchTimeout = TimeSpan.FromSeconds(5),
                    GracefulStopTimeout = TimeSpan.FromMilliseconds(100)
                });
            using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
            var result = await runner.StartAsync(
                new DynamicAppManifest { Id = key.DynamicAppId },
                new DynamicModManifest
                {
                    Id = key.ModId,
                    Version = key.Version,
                    ExecutionMode = DynamicModExecutionMode.Process,
                    Artifact = new DynamicModArtifactManifest { File = script, Endpoint = "http://127.0.0.1:5111" }
                },
                cancellation.Token);

            Assert.False(result.Succeeded);
            var target = Assert.Single(stateStore.ListTargets());
            Assert.Equal(DynamicModLifecycleState.Failed, target.State);
            Assert.False(IsProcessAlive(target.InstanceId));
        }
        finally
        {
            TryDelete(script);
        }
    }

    /// <summary>
    /// <para>zh-cn:验证新 Runner 可以从状态存储接管旧 Runner 遗留的进程，并在停止时彻底结束该进程。</para>
    /// <para>en-us:Verifies that a new Runner can adopt a process left by an old Runner from persisted state and fully terminate it during stop.</para>
    /// </summary>
    [Fact]
    public async Task StopAsync_should_attach_persisted_process_and_kill_it()
    {
        var script = CreateLongRunningScript();
        Process? child = null;
        try
        {
            child = StartLongRunningScript(script);
            var key = CreateKey();
            var stateStore = new InMemoryDynamicAppStateStore();
            stateStore.UpsertTarget(new DynamicAppRouteTarget
            {
                Key = key,
                DeploymentEntryFilePath = script,
                DeploymentDirectory = Path.GetDirectoryName(script) ?? string.Empty,
                InstanceId = child.Id.ToString(),
                State = DynamicModLifecycleState.Active,
                HealthStatus = DynamicModHealthStatus.Healthy
            });

            using var runner = new ProcessDynamicAppModRunner(
                stateStore,
                new InMemoryDynamicAppProcessLogSink(),
                new DynamicAppProcessOptions { GracefulStopTimeout = TimeSpan.FromMilliseconds(100) });

            var result = await runner.StopAsync(new DynamicAppRuntimeKey(key, "default"));

            Assert.True(result.Succeeded, result.Message);
            await child.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
            Assert.True(child.HasExited);
            var target = Assert.Single(stateStore.ListTargets());
            Assert.Equal(DynamicModLifecycleState.Stopped, target.State);
            Assert.Equal(DynamicModHealthStatus.Stopped, target.HealthStatus);
        }
        finally
        {
            if (child is { HasExited: false })
            {
                child.Kill(entireProcessTree: true);
                await child.WaitForExitAsync();
            }

            child?.Dispose();
            TryDelete(script);
        }
    }

    /// <summary>
    /// <para>zh-cn:验证持久化 PID 已不存在时停止操作仍幂等地发布 Stopped，而不是返回误导性的运行中失败。</para>
    /// <para>en-us:Verifies that stop remains idempotent and publishes Stopped when the persisted PID no longer exists instead of reporting a misleading running-process failure.</para>
    /// </summary>
    [Fact]
    public async Task StopAsync_should_be_idempotent_when_persisted_process_is_gone()
    {
        var key = CreateKey();
        var stateStore = new InMemoryDynamicAppStateStore();
        stateStore.UpsertTarget(new DynamicAppRouteTarget
        {
            Key = key,
            DeploymentEntryFilePath = Path.Combine(Path.GetTempPath(), "missing-process.dll"),
            InstanceId = int.MaxValue.ToString(),
            State = DynamicModLifecycleState.Active,
            HealthStatus = DynamicModHealthStatus.Healthy
        });

        using var runner = new ProcessDynamicAppModRunner(stateStore, new InMemoryDynamicAppProcessLogSink());
        var result = await runner.StopAsync(new DynamicAppRuntimeKey(key, "default"));

        Assert.True(result.Succeeded, result.Message);
        var target = Assert.Single(stateStore.ListTargets());
        Assert.Equal(DynamicModLifecycleState.Stopped, target.State);
        Assert.Equal(DynamicModHealthStatus.Stopped, target.HealthStatus);
    }

    private static DynamicModVersionKey CreateKey()
        => new(new DynamicAppId("runner-test"), new DynamicModId("workflow"), new DynamicAppVersion("1.0.0"));

    private static string CreateLongRunningScript()
    {
        var extension = OperatingSystem.IsWindows() ? ".cmd" : ".sh";
        var path = Path.Combine(Path.GetTempPath(), $"air-cloud-runner-{Guid.NewGuid():N}{extension}");
        var contents = OperatingSystem.IsWindows()
            ? "@echo off\r\nping 127.0.0.1 -n 120 >nul\r\n"
            : "#!/bin/sh\nsleep 120\n";
        File.WriteAllText(path, contents);
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }

        return path;
    }

    private static Process StartLongRunningScript(string path)
    {
        var process = new Process
        {
            StartInfo = OperatingSystem.IsWindows()
                ? new ProcessStartInfo("cmd.exe", $"/c \"{path}\"") { UseShellExecute = false }
                : new ProcessStartInfo(path) { UseShellExecute = false },
            EnableRaisingEvents = true
        };
        Assert.True(process.Start());
        return process;
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static bool IsProcessAlive(string? processId)
    {
        if (!int.TryParse(processId, out var id)) return false;
        try
        {
            using var process = Process.GetProcessById(id);
            return !process.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
