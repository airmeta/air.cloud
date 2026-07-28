using System.Diagnostics;

using Air.Cloud.DynamicApp.Contracts.Lifecycle;
using Air.Cloud.DynamicApp.Contracts.Manifest;
using Air.Cloud.DynamicApp.Contracts.Runtime;

namespace Air.Cloud.DynamicApp.Orchestrator;

/// <summary>
/// <para>zh-cn:命令式 DynamicApp 编排器，通过显式配置且通过校验的受控命令部署 Container 模式 Mod。</para>
/// <para>en-us:Command-based DynamicApp orchestrator that deploys Container-mode Mods through an explicitly configured and validated trusted command.</para>
/// </summary>
public sealed class CommandDynamicAppOrchestrator : IDynamicAppOrchestrator
{
    private static readonly HashSet<string> ShellFileNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "cmd",
        "cmd.exe",
        "powershell",
        "powershell.exe",
        "pwsh",
        "pwsh.exe",
        "sh",
        "bash"
    };

    private readonly CommandDynamicAppOrchestratorOptions _options;
    private readonly IDynamicAppStateStore _stateStore;

    /// <summary>
    /// <para>zh-cn:创建命令式编排器。</para>
    /// <para>en-us:Creates the command-based orchestrator.</para>
    /// </summary>
    /// <param name="stateStore">
    /// <para>zh-cn:状态存储，用于在命令成功后发布目标。</para>
    /// <para>en-us:The state store used to publish the target after the command succeeds.</para>
    /// </param>
    /// <param name="options">
    /// <para>zh-cn:命令式编排器选项。</para>
    /// <para>en-us:The command-based orchestrator options.</para>
    /// </param>
    public CommandDynamicAppOrchestrator(IDynamicAppStateStore stateStore, CommandDynamicAppOrchestratorOptions options)
    {
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// <para>zh-cn:执行受控部署命令；未启用、未配置、未通过命令安全校验或 Mod 非 Container 模式时返回失败。</para>
    /// <para>en-us:Executes the trusted deployment command; returns failure when disabled, not configured, rejected by command safety checks, or the Mod is not in Container mode.</para>
    /// </summary>
    /// <param name="manifest">
    /// <para>zh-cn:所属 DynamicApp Manifest。</para>
    /// <para>en-us:The owning DynamicApp manifest.</para>
    /// </param>
    /// <param name="mod">
    /// <para>zh-cn:待部署 Mod Manifest。</para>
    /// <para>en-us:The Mod manifest to deploy.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌。</para>
    /// <para>en-us:Cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:部署结果。</para>
    /// <para>en-us:The deployment result.</para>
    /// </returns>
    public async Task<DynamicAppOperationResult> DeployAsync(DynamicAppManifest manifest, DynamicModManifest mod, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(mod);

        if (mod.ExecutionMode != DynamicModExecutionMode.Container)
        {
            return DynamicAppOperationResult.Failure($"Mod `{manifest.Id}/{mod.Id}` is not configured for Container mode.");
        }

        var validation = ValidateCommandOptions();
        if (!validation.Succeeded)
        {
            return validation;
        }

        if (string.IsNullOrWhiteSpace(mod.Artifact.Image))
        {
            return DynamicAppOperationResult.Failure($"Container Mod `{manifest.Id}/{mod.Id}` does not declare Artifact.Image.");
        }

        var key = new DynamicModVersionKey(manifest.Id, mod.Id, mod.Version);
        using var process = CreateProcess(manifest, mod);
        if (!process.Start())
        {
            return DynamicAppOperationResult.Failure($"Command orchestrator failed to start for `{key}`.");
        }

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        using var timeout = new CancellationTokenSource(NormalizeTimeout(_options.Timeout));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, cancellationToken);
        try
        {
            await process.WaitForExitAsync(linked.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            TryKill(process);
            return DynamicAppOperationResult.Failure($"Command orchestrator timed out for `{key}`.");
        }

        var output = await outputTask.ConfigureAwait(false);
        var error = await errorTask.ConfigureAwait(false);
        if (process.ExitCode != 0)
        {
            return DynamicAppOperationResult.Failure($"Command orchestrator failed for `{key}` with exit code `{process.ExitCode}`. {error}".Trim());
        }

        _stateStore.UpsertTarget(new DynamicAppRouteTarget
        {
            Key = key,
            BaseAddress = ResolveTargetBaseAddress(manifest, mod),
            RoutePrefix = mod.Route.Prefix,
            HealthPath = mod.Health.Path,
            HealthProbeTimeoutSeconds = mod.Health.StartupTimeoutSeconds,
            State = DynamicModLifecycleState.Starting,
            HealthStatus = DynamicModHealthStatus.Starting,
            LastStatusMessage = string.IsNullOrWhiteSpace(output)
                ? $"Container target `{key}` deployed by command orchestrator."
                : output.Trim()
        });

        return DynamicAppOperationResult.Success($"Container target `{key}` deployed by command orchestrator.");
    }

    /// <summary>
    /// <para>zh-cn:执行受控停止命令来停止容器模式 Mod；只有启用编排器、配置停止模板并通过命令安全校验后才会执行。</para>
    /// <para>en-us:Executes the trusted stop command for a Container-mode Mod; it runs only when the orchestrator is enabled, a stop template is configured, and command safety validation passes.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:要停止的目标版本键。</para>
    /// <para>en-us:The target version key to stop.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌。</para>
    /// <para>en-us:Cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:停止命令执行结果。</para>
    /// <para>en-us:The stop command execution result.</para>
    /// </returns>
    public async Task<DynamicAppOperationResult> StopAsync(DynamicModVersionKey key, CancellationToken cancellationToken = default)
    {
        var validation = ValidateCommandOptions();
        if (!validation.Succeeded)
        {
            return validation;
        }

        if (string.IsNullOrWhiteSpace(_options.StopArgumentsTemplate))
        {
            return DynamicAppOperationResult.Failure("Command DynamicApp orchestrator StopArgumentsTemplate is not configured.");
        }

        using var process = CreateProcess(ApplyTemplate(_options.StopArgumentsTemplate, key));
        if (!process.Start())
        {
            return DynamicAppOperationResult.Failure($"Command orchestrator failed to start stop command for `{key}`.");
        }

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        using var timeout = new CancellationTokenSource(NormalizeTimeout(_options.Timeout));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, cancellationToken);
        try
        {
            await process.WaitForExitAsync(linked.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            TryKill(process);
            return DynamicAppOperationResult.Failure($"Command orchestrator stop timed out for `{key}`.");
        }

        var output = await outputTask.ConfigureAwait(false);
        var error = await errorTask.ConfigureAwait(false);
        if (process.ExitCode != 0)
        {
            return DynamicAppOperationResult.Failure($"Command orchestrator stop failed for `{key}` with exit code `{process.ExitCode}`. {error}".Trim());
        }

        return DynamicAppOperationResult.Success(string.IsNullOrWhiteSpace(output)
            ? $"Container target `{key}` stopped by command orchestrator."
            : output.Trim());
    }

    private DynamicAppOperationResult ValidateCommandOptions()
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.FileName))
        {
            return DynamicAppOperationResult.Failure("Command DynamicApp orchestrator is not enabled or FileName is not configured.");
        }

        if (_options.RequireAbsoluteFileName && !Path.IsPathFullyQualified(_options.FileName))
        {
            return DynamicAppOperationResult.Failure("Command DynamicApp orchestrator requires FileName to be an absolute path.");
        }

        if (_options.RequireConfiguredWorkingDirectory && string.IsNullOrWhiteSpace(_options.WorkingDirectory))
        {
            return DynamicAppOperationResult.Failure("Command DynamicApp orchestrator requires WorkingDirectory to be configured.");
        }

        if (!string.IsNullOrWhiteSpace(_options.WorkingDirectory) && !Directory.Exists(_options.WorkingDirectory))
        {
            return DynamicAppOperationResult.Failure($"Command DynamicApp orchestrator WorkingDirectory `{_options.WorkingDirectory}` does not exist.");
        }

        var fileName = Path.GetFileName(_options.FileName);
        if (_options.BlockShellCommands && ShellFileNames.Contains(fileName))
        {
            return DynamicAppOperationResult.Failure($"Command DynamicApp orchestrator does not allow shell entrypoint `{fileName}`.");
        }

        if (_options.AllowedFileNames.Length > 0
            && !_options.AllowedFileNames.Any(allowed => string.Equals(allowed, fileName, StringComparison.OrdinalIgnoreCase)))
        {
            return DynamicAppOperationResult.Failure($"Command DynamicApp orchestrator FileName `{fileName}` is not in the allowed list.");
        }

        return DynamicAppOperationResult.Success("Command DynamicApp orchestrator options are valid.");
    }

    private Process CreateProcess(DynamicAppManifest manifest, DynamicModManifest mod) => CreateProcess(ApplyTemplate(_options.ArgumentsTemplate, manifest, mod));

    private Process CreateProcess(string arguments) => new()
    {
        StartInfo = new ProcessStartInfo
        {
            FileName = _options.FileName,
            Arguments = arguments,
            WorkingDirectory = string.IsNullOrWhiteSpace(_options.WorkingDirectory) ? Environment.CurrentDirectory : _options.WorkingDirectory,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        },
        EnableRaisingEvents = true
    };

    private Uri ResolveTargetBaseAddress(DynamicAppManifest manifest, DynamicModManifest mod)
    {
        var value = string.IsNullOrWhiteSpace(_options.TargetBaseAddressTemplate)
            ? mod.Artifact.Endpoint
            : ApplyTemplate(_options.TargetBaseAddressTemplate, manifest, mod);

        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            ? uri
            : new Uri("http://localhost");
    }

    private static string ApplyTemplate(string? template, DynamicAppManifest manifest, DynamicModManifest mod)
    {
        return (template ?? string.Empty)
            .Replace("{dynamicAppId}", manifest.Id.Value, StringComparison.OrdinalIgnoreCase)
            .Replace("{modId}", mod.Id.Value, StringComparison.OrdinalIgnoreCase)
            .Replace("{version}", mod.Version.Value, StringComparison.OrdinalIgnoreCase)
            .Replace("{image}", mod.Artifact.Image ?? string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("{routePrefix}", mod.Route.Prefix, StringComparison.OrdinalIgnoreCase)
            .Replace("{healthPath}", mod.Health.Path, StringComparison.OrdinalIgnoreCase);
    }

    private static string ApplyTemplate(string? template, DynamicModVersionKey key)
    {
        return (template ?? string.Empty)
            .Replace("{dynamicAppId}", key.DynamicAppId.Value, StringComparison.OrdinalIgnoreCase)
            .Replace("{modId}", key.ModId.Value, StringComparison.OrdinalIgnoreCase)
            .Replace("{version}", key.Version.Value, StringComparison.OrdinalIgnoreCase);
    }

    private static TimeSpan NormalizeTimeout(TimeSpan timeout) => timeout <= TimeSpan.Zero ? TimeSpan.FromSeconds(120) : timeout;

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
            // The command may exit between the timeout and the kill attempt.
        }
    }
}
