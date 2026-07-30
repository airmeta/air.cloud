using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Air.Cloud.DynamicApp.Contracts.Lifecycle;
using Air.Cloud.DynamicApp.Contracts.Manifest;
using Air.Cloud.DynamicApp.Contracts.Runtime;
using Air.Cloud.DynamicApp.Manager;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.DynamicApp.Panel;

/// <summary>
/// <para>zh-cn:DynamicApp 内置控制面板中间件，提供一个无外部依赖的单页运维界面。</para>
/// <para>en-us:Built-in DynamicApp control panel middleware that serves a dependency-free single-page operations UI.</para>
/// </summary>
public sealed class DynamicAppPanelMiddleware
{
    private static readonly JsonSerializerOptions RuntimeStatsJsonOptions = new(JsonSerializerDefaults.Web);
    private readonly DynamicAppManagerApiOptions _managerOptions;
    private readonly RequestDelegate _next;
    private readonly DynamicAppPanelOptions _options;
    private readonly Dictionary<string, ResourceCounter> _resourceCounters = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _resourceStatsLock = new();
    private readonly TimeSpan _resourceStatsSampleInterval;
    private RuntimeStatsSnapshot? _cachedRuntimeStats;

    /// <summary>
    /// <para>zh-cn:创建 DynamicApp 控制面板中间件；页面会根据 Manager API 配置生成默认请求前缀和 Ticket 请求头名称。</para>
    /// <para>en-us:Creates the DynamicApp control panel middleware; the page uses Manager API options to generate the default request prefix and Ticket header name.</para>
    /// </summary>
    /// <param name="next">
    /// <para>zh-cn:下一个中间件。</para>
    /// <para>en-us:The next middleware.</para>
    /// </param>
    /// <param name="options">
    /// <para>zh-cn:控制面板选项。</para>
    /// <para>en-us:The control panel options.</para>
    /// </param>
    /// <param name="managerOptions">
    /// <para>zh-cn:Manager API 选项，用于读取 API 前缀和 Ticket 请求头名称。</para>
    /// <para>en-us:Manager API options used to read the API prefix and Ticket request header name.</para>
    /// </param>
    public DynamicAppPanelMiddleware(RequestDelegate next, DynamicAppPanelOptions options, DynamicAppManagerApiOptions managerOptions)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _managerOptions = managerOptions ?? throw new ArgumentNullException(nameof(managerOptions));
        _resourceStatsSampleInterval = _options.ResourceStatsSampleInterval > TimeSpan.Zero
            ? _options.ResourceStatsSampleInterval
            : TimeSpan.FromSeconds(10);
    }

    /// <summary>
    /// <para>zh-cn:处理控制面板页面请求；非控制面板路径会继续传递给后续 DynamicApp Manager 或 Gateway 中间件。</para>
    /// <para>en-us:Handles control panel page requests; non-panel paths continue to downstream DynamicApp Manager or Gateway middleware.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:当前 HTTP 上下文。</para>
    /// <para>en-us:The current HTTP context.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:异步处理任务。</para>
    /// <para>en-us:The asynchronous handling task.</para>
    /// </returns>
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!_options.Enabled)
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        if (IsRuntimeStatsPath(context.Request.Path))
        {
            if (!IsAuthorized(context))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            await WriteRuntimeStatsAsync(context).ConfigureAwait(false);
            return;
        }

        if (!IsPanelPath(context.Request.Path))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "text/html; charset=utf-8";
        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers.XFrameOptions = "DENY";
        context.Response.Headers.XContentTypeOptions = "nosniff";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
        context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        context.Response.Headers.ContentSecurityPolicy = "default-src 'none'; style-src 'unsafe-inline'; script-src 'unsafe-inline'; connect-src 'self'; img-src 'self' data:; font-src 'self'; base-uri 'none'; frame-ancestors 'none'; form-action 'none'";
        await context.Response.WriteAsync(BuildHtml(), Encoding.UTF8, context.RequestAborted).ConfigureAwait(false);
    }

    private bool IsPanelPath(PathString path)
    {
        var prefix = NormalizePrefix(_options.RoutePrefix);
        var value = path.Value ?? string.Empty;
        return value.Equals(prefix, StringComparison.OrdinalIgnoreCase)
            || value.Equals(prefix + "/", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsRuntimeStatsPath(PathString path)
    {
        var value = path.Value ?? string.Empty;
        return value.Equals(GetRuntimeStatsPath(), StringComparison.OrdinalIgnoreCase);
    }

    private async Task WriteRuntimeStatsAsync(HttpContext context)
    {
        var payload = await GetRuntimeStatsSnapshotAsync(context).ConfigureAwait(false);
        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, RuntimeStatsJsonOptions), Encoding.UTF8, context.RequestAborted).ConfigureAwait(false);
    }

    private async Task<RuntimeStatsSnapshot> GetRuntimeStatsSnapshotAsync(HttpContext context)
    {
        var now = DateTimeOffset.UtcNow;
        lock (_resourceStatsLock)
        {
            if (_cachedRuntimeStats != null && now - _cachedRuntimeStats.TimestampUtc < _resourceStatsSampleInterval)
            {
                return _cachedRuntimeStats;
            }
        }

        var stateStore = context.RequestServices.GetService<IDynamicAppStateStore>();
        var targets = stateStore?.ListTargets() ?? Array.Empty<DynamicAppRouteTarget>();
        var manifestStore = context.RequestServices.GetService<IDynamicAppManifestStore>();
        var manifestRecords = manifestStore == null
            ? Array.Empty<DynamicAppManifestRecord>()
            : await manifestStore.ListAllAsync(context.RequestAborted).ConfigureAwait(false);
        var existingContainerCatalog = context.RequestServices.GetService<IDynamicAppExistingContainerCatalog>();
        var hostProcess = CreateProcessSnapshot(Process.GetCurrentProcess(), "host", null, null, now);
        var container = CreateContainerSnapshot(now);
        var mods = await CreateRuntimeResourceSnapshotsAsync(targets, manifestRecords, existingContainerCatalog, now, context.RequestAborted).ConfigureAwait(false);
        var activeResourceKeys = new HashSet<string>(mods.Where(item => item.ProcessId > 0).Select(item => "process:" + item.ProcessId), StringComparer.OrdinalIgnoreCase)
        {
            "process:" + Environment.ProcessId,
            "container:self"
        };

        var snapshot = new RuntimeStatsSnapshot
        {
            TimestampUtc = now,
            CacheExpiresAtUtc = now.Add(_resourceStatsSampleInterval),
            SampleIntervalSeconds = Math.Round(_resourceStatsSampleInterval.TotalSeconds, 2),
            ProcessorCount = Environment.ProcessorCount,
            ProcessId = hostProcess.ProcessId,
            CpuPercent = hostProcess.CpuPercent,
            WorkingSetBytes = hostProcess.WorkingSetBytes,
            PrivateMemoryBytes = hostProcess.PrivateMemoryBytes,
            ManagedMemoryBytes = hostProcess.ManagedMemoryBytes,
            TotalAvailableMemoryBytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes,
            ThreadCount = hostProcess.ThreadCount,
            HandleCount = hostProcess.HandleCount,
            UptimeSeconds = hostProcess.UptimeSeconds,
            HostProcess = hostProcess,
            Container = container,
            Mods = mods
        };

        lock (_resourceStatsLock)
        {
            if (_cachedRuntimeStats != null && now - _cachedRuntimeStats.TimestampUtc < _resourceStatsSampleInterval)
            {
                return _cachedRuntimeStats;
            }

            foreach (var key in _resourceCounters.Keys.Where(key => !activeResourceKeys.Contains(key)).ToArray())
            {
                _resourceCounters.Remove(key);
            }

            _cachedRuntimeStats = snapshot;
            return _cachedRuntimeStats;
        }
    }

    private async Task<IReadOnlyCollection<RuntimeResourceSnapshot>> CreateRuntimeResourceSnapshotsAsync(
        IReadOnlyCollection<DynamicAppRouteTarget> targets,
        IReadOnlyCollection<DynamicAppManifestRecord> manifestRecords,
        IDynamicAppExistingContainerCatalog? existingContainerCatalog,
        DateTimeOffset sampledAtUtc,
        CancellationToken cancellationToken)
    {
        var snapshots = new List<RuntimeResourceSnapshot>();
        var activeKeys = targets.Select(item => item.Key).ToHashSet();
        var mergedTargets = targets.Concat(CreateManifestOnlyExistingContainerTargets(manifestRecords, activeKeys));
        foreach (var target in mergedTargets
            .OrderBy(item => item.Key.DynamicAppId.Value, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Key.ModId.Value, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Key.Version.Value, StringComparer.OrdinalIgnoreCase))
        {
            var mod = FindManifestMod(manifestRecords, target.Key);
            var executionMode = mod?.ExecutionMode ?? ResolveExecutionMode(target);
            if (executionMode == DynamicModExecutionMode.ExistingContainer)
            {
                snapshots.Add(await CreateExistingContainerSnapshotAsync(target, existingContainerCatalog, mod, cancellationToken).ConfigureAwait(false));
                continue;
            }

            snapshots.Add(CreateModProcessSnapshot(target, executionMode, sampledAtUtc));
        }

        return SelectCurrentRuntimeSnapshots(snapshots);
    }

    private static IReadOnlyCollection<RuntimeResourceSnapshot> SelectCurrentRuntimeSnapshots(IReadOnlyCollection<RuntimeResourceSnapshot> snapshots)
    {
        return snapshots
            .GroupBy(item => (item.DynamicAppId, item.ModId))
            .Select(group => group
                .OrderByDescending(GetRuntimeSnapshotRank)
                .ThenByDescending(item => item.UpdatedAtUtc)
                .ThenByDescending(item => item.CreatedAtUtc)
                .ThenByDescending(item => item.Version, StringComparer.OrdinalIgnoreCase)
                .First())
            .OrderBy(item => item.DynamicAppId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.ModId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static int GetRuntimeSnapshotRank(RuntimeResourceSnapshot snapshot)
    {
        return snapshot.State.Equals(DynamicModLifecycleState.Active.ToString(), StringComparison.OrdinalIgnoreCase) && snapshot.HealthStatus.Equals(DynamicModHealthStatus.Healthy.ToString(), StringComparison.OrdinalIgnoreCase)
            ? 1000
            : snapshot.State.Equals(DynamicModLifecycleState.Starting.ToString(), StringComparison.OrdinalIgnoreCase)
                ? 700
                : snapshot.State.Equals(DynamicModLifecycleState.Failed.ToString(), StringComparison.OrdinalIgnoreCase)
                    ? 300
                    : snapshot.State.Equals(DynamicModLifecycleState.Stopped.ToString(), StringComparison.OrdinalIgnoreCase)
                        ? 200
                        : snapshot.State.Equals(DynamicModLifecycleState.Draining.ToString(), StringComparison.OrdinalIgnoreCase)
                            ? 50
                            : 100;
    }

    private static IReadOnlyCollection<DynamicAppRouteTarget> CreateManifestOnlyExistingContainerTargets(
        IReadOnlyCollection<DynamicAppManifestRecord> manifestRecords,
        IEnumerable<DynamicModVersionKey> activeKeys)
    {
        var activeKeySet = new HashSet<DynamicModVersionKey>(activeKeys);
        return manifestRecords
            .SelectMany(record => record.Manifest.Mods.Select(mod => new { record.Manifest, Mod = mod }))
            .Where(item => item.Mod.ExecutionMode == DynamicModExecutionMode.ExistingContainer)
            .GroupBy(item => (item.Manifest.Id, item.Mod.Id))
            .Select(group => group.First())
            .Select(item => new DynamicAppRouteTarget
            {
                Key = new DynamicModVersionKey(item.Manifest.Id, item.Mod.Id, item.Mod.Version),
                BaseAddress = Uri.TryCreate(item.Mod.Artifact.Endpoint, UriKind.Absolute, out var endpoint) ? endpoint : new Uri("http://localhost"),
                RoutePrefix = item.Mod.Route.Prefix,
                HealthPath = item.Mod.Health.Path,
                HealthProbeTimeoutSeconds = item.Mod.Health.StartupTimeoutSeconds,
                State = DynamicModLifecycleState.Pending,
                HealthStatus = DynamicModHealthStatus.Unknown,
                InstanceId = item.Mod.Artifact.ContainerName ?? string.Empty,
                LastStatusMessage = "Registered ExistingContainer manifest has not been written into runtime state."
            })
            .Where(target => !activeKeySet.Contains(target.Key))
            .ToArray();
    }

    private RuntimeResourceSnapshot CreateModProcessSnapshot(DynamicAppRouteTarget target, DynamicModExecutionMode executionMode, DateTimeOffset sampledAtUtc)
    {
        var key = target.Key.ToString();
        if (!int.TryParse(target.InstanceId, out var processId) || processId <= 0)
        {
            return RuntimeResourceSnapshot.Unavailable("mod", key, target, executionMode, $"Runtime instance `{target.InstanceId}` is not a process id.");
        }

        try
        {
            using var process = Process.GetProcessById(processId);
            var snapshot = CreateProcessSnapshot(process, "mod", key, target, sampledAtUtc);
            snapshot.ExecutionMode = executionMode.ToString();
            snapshot.RuntimeType = "Mod";
            return snapshot;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return RuntimeResourceSnapshot.Unavailable("mod", key, target, executionMode, $"Process `{processId}` cannot be inspected. {exception.Message}");
        }
    }

    private async Task<RuntimeResourceSnapshot> CreateExistingContainerSnapshotAsync(
        DynamicAppRouteTarget target,
        IDynamicAppExistingContainerCatalog? catalog,
        DynamicModManifest? mod,
        CancellationToken cancellationToken)
    {
        var key = target.Key.ToString();
        if (catalog == null)
        {
            return RuntimeResourceSnapshot.Unavailable("container", key, target, DynamicModExecutionMode.ExistingContainer, "IDynamicAppExistingContainerCatalog is not registered.");
        }

        try
        {
            var container = await catalog.InspectAsync(target.Key, cancellationToken).ConfigureAwait(false);
            var state = container.IsRunning ? DynamicModLifecycleState.Active : DynamicModLifecycleState.Stopped;
            var health = container.IsAvailable
                ? DynamicModHealthStatus.Healthy
                : container.IsRunning ? DynamicModHealthStatus.Unhealthy : DynamicModHealthStatus.Stopped;
            return new RuntimeResourceSnapshot
            {
                Scope = "container",
                Key = key,
                DynamicAppId = target.Key.DynamicAppId.Value,
                ApplicationName = target.Key.DynamicAppId.Value,
                ModId = target.Key.ModId.Value,
                Version = target.Key.Version.Value,
                ExecutionMode = DynamicModExecutionMode.ExistingContainer.ToString(),
                RuntimeType = "Container",
                ContainerName = string.IsNullOrWhiteSpace(container.Name) ? mod?.Artifact.ContainerName ?? target.InstanceId : container.Name,
                State = state.ToString(),
                HealthStatus = health.ToString(),
                IsAvailable = container.IsAvailable,
                Message = string.IsNullOrWhiteSpace(container.Status) ? target.LastStatusMessage : container.Status,
                CreatedAtUtc = target.CreatedAtUtc,
                UpdatedAtUtc = target.UpdatedAtUtc,
                ProcessId = 0,
                CpuPercent = 0,
                WorkingSetBytes = 0,
                PrivateMemoryBytes = 0,
                ManagedMemoryBytes = 0,
                ThreadCount = -1,
                HandleCount = -1,
                UptimeSeconds = 0
            };
        }
        catch (Exception exception) when (exception is InvalidOperationException or HttpRequestException or OperationCanceledException)
        {
            return RuntimeResourceSnapshot.Unavailable("container", key, target, DynamicModExecutionMode.ExistingContainer, exception.Message);
        }
    }

    private static DynamicModManifest? FindManifestMod(IReadOnlyCollection<DynamicAppManifestRecord> records, DynamicModVersionKey key)
    {
        return records
            .Where(record => record.Manifest.Id == key.DynamicAppId)
            .SelectMany(record => record.Manifest.Mods)
            .FirstOrDefault(mod => mod.Id == key.ModId && mod.Version == key.Version);
    }

    private static DynamicModExecutionMode ResolveExecutionMode(DynamicAppRouteTarget target)
    {
        if (int.TryParse(target.InstanceId, out var processId) && processId > 0)
        {
            return DynamicModExecutionMode.Process;
        }

        return string.IsNullOrWhiteSpace(target.InstanceId)
            ? DynamicModExecutionMode.Process
            : DynamicModExecutionMode.ExistingContainer;
    }

    private RuntimeResourceSnapshot CreateProcessSnapshot(Process process, string scope, string? key, DynamicAppRouteTarget? target, DateTimeOffset sampledAtUtc)
    {
        var processId = process.Id;
        var totalCpuMs = SafeTotalProcessorTime(process);
        var cpuPercent = CalculateCpuPercent("process:" + processId, totalCpuMs, sampledAtUtc);
        process.Refresh();
        return new RuntimeResourceSnapshot
        {
            Scope = scope,
            Key = key ?? string.Empty,
            DynamicAppId = target?.Key.DynamicAppId.Value ?? string.Empty,
            ApplicationName = target?.Key.DynamicAppId.Value ?? string.Empty,
            ModId = target?.Key.ModId.Value ?? string.Empty,
            Version = target?.Key.Version.Value ?? string.Empty,
            ExecutionMode = scope.Equals("host", StringComparison.OrdinalIgnoreCase) ? string.Empty : DynamicModExecutionMode.Process.ToString(),
            RuntimeType = scope.Equals("host", StringComparison.OrdinalIgnoreCase) ? string.Empty : "Mod",
            State = target?.State.ToString() ?? string.Empty,
            HealthStatus = target?.HealthStatus.ToString() ?? string.Empty,
            IsAvailable = true,
            Message = string.Empty,
            CreatedAtUtc = target?.CreatedAtUtc ?? DateTimeOffset.MinValue,
            UpdatedAtUtc = target?.UpdatedAtUtc ?? DateTimeOffset.MinValue,
            ProcessId = processId,
            CpuPercent = cpuPercent,
            WorkingSetBytes = SafeWorkingSet(process),
            PrivateMemoryBytes = SafePrivateMemory(process),
            ManagedMemoryBytes = scope.Equals("host", StringComparison.OrdinalIgnoreCase) ? GC.GetTotalMemory(false) : 0,
            ThreadCount = SafeThreadCount(process),
            HandleCount = GetHandleCount(process),
            UptimeSeconds = SafeUptimeSeconds(process)
        };
    }

    private ContainerResourceSnapshot CreateContainerSnapshot(DateTimeOffset sampledAtUtc)
    {
        var cpuUsageMs = TryReadCgroupCpuUsageMilliseconds(out var cpuMsMessage);
        var memoryBytes = TryReadCgroupMemoryValue("memory.current", "/sys/fs/cgroup/memory/memory.usage_in_bytes");
        var memoryLimitBytes = TryReadCgroupMemoryLimit();
        if (cpuUsageMs == null && memoryBytes == null)
        {
            return new ContainerResourceSnapshot
            {
                IsAvailable = false,
                Message = string.IsNullOrWhiteSpace(cpuMsMessage)
                    ? "Container cgroup resource files are not available."
                    : cpuMsMessage
            };
        }

        return new ContainerResourceSnapshot
        {
            IsAvailable = true,
            Message = string.Empty,
            CpuPercent = cpuUsageMs == null ? 0 : CalculateCpuPercent("container:self", cpuUsageMs.Value, sampledAtUtc),
            MemoryBytes = memoryBytes ?? 0,
            MemoryLimitBytes = memoryLimitBytes,
            ProcessorCount = Environment.ProcessorCount
        };
    }

    private double CalculateCpuPercent(string key, double totalCpuMs, DateTimeOffset sampledAtUtc)
    {
        lock (_resourceStatsLock)
        {
            if (!_resourceCounters.TryGetValue(key, out var previous))
            {
                _resourceCounters[key] = new ResourceCounter(totalCpuMs, sampledAtUtc);
                return 0d;
            }

            _resourceCounters[key] = new ResourceCounter(totalCpuMs, sampledAtUtc);
            var elapsedMs = (sampledAtUtc - previous.SampledAtUtc).TotalMilliseconds;
            if (elapsedMs <= 0 || totalCpuMs < previous.TotalCpuMilliseconds)
            {
                return 0d;
            }

            var percent = (totalCpuMs - previous.TotalCpuMilliseconds) / (elapsedMs * Math.Max(1, Environment.ProcessorCount)) * 100d;
            return Math.Round(Math.Max(0d, percent), 2);
        }
    }

    private bool IsAuthorized(HttpContext context)
    {
        if (!_managerOptions.RequireTicket)
        {
            return true;
        }

        if (_managerOptions.Tickets.Length == 0 || string.IsNullOrWhiteSpace(_managerOptions.HeaderName))
        {
            return false;
        }

        var requestTickets = context.Request.Headers[_managerOptions.HeaderName].Where(ticket => !string.IsNullOrWhiteSpace(ticket)).ToArray();
        return requestTickets.Any(requestTicket => _managerOptions.Tickets.Any(allowedTicket => FixedTimeEquals(requestTicket!, allowedTicket)));
    }

    private static int GetHandleCount(Process process)
    {
        try
        {
            return process.HandleCount;
        }
        catch (InvalidOperationException)
        {
            return -1;
        }
    }

    private static double SafeTotalProcessorTime(Process process)
    {
        try
        {
            return process.TotalProcessorTime.TotalMilliseconds;
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return 0d;
        }
    }

    private static long SafeWorkingSet(Process process)
    {
        try
        {
            return process.WorkingSet64;
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return 0;
        }
    }

    private static long SafePrivateMemory(Process process)
    {
        try
        {
            return process.PrivateMemorySize64;
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return 0;
        }
    }

    private static int SafeThreadCount(Process process)
    {
        try
        {
            return process.Threads.Count;
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return -1;
        }
    }

    private static double SafeUptimeSeconds(Process process)
    {
        try
        {
            return Math.Round((DateTimeOffset.UtcNow - process.StartTime.ToUniversalTime()).TotalSeconds);
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return 0d;
        }
    }

    private static double? TryReadCgroupCpuUsageMilliseconds(out string message)
    {
        message = string.Empty;
        const string cpuStatPath = "/sys/fs/cgroup/cpu.stat";
        if (File.Exists(cpuStatPath))
        {
            foreach (var line in File.ReadLines(cpuStatPath))
            {
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length == 2 && parts[0].Equals("usage_usec", StringComparison.OrdinalIgnoreCase) && double.TryParse(parts[1], out var usageUsec))
                {
                    return usageUsec / 1000d;
                }
            }
        }

        const string cpuAcctPath = "/sys/fs/cgroup/cpuacct/cpuacct.usage";
        if (File.Exists(cpuAcctPath) && double.TryParse(File.ReadAllText(cpuAcctPath).Trim(), out var usageNsec))
        {
            return usageNsec / 1_000_000d;
        }

        message = "Container CPU cgroup resource files are not available.";
        return null;
    }

    private static long? TryReadCgroupMemoryLimit()
    {
        var raw = TryReadCgroupMemoryValue("memory.max", "/sys/fs/cgroup/memory/memory.limit_in_bytes");
        if (raw == null || raw <= 0 || raw >= long.MaxValue / 4)
        {
            return null;
        }

        return raw;
    }

    private static long? TryReadCgroupMemoryValue(string v2FileName, string v1Path)
    {
        var v2Path = "/sys/fs/cgroup/" + v2FileName;
        if (File.Exists(v2Path))
        {
            var raw = File.ReadAllText(v2Path).Trim();
            if (raw.Equals("max", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (long.TryParse(raw, out var value))
            {
                return value;
            }
        }

        if (File.Exists(v1Path) && long.TryParse(File.ReadAllText(v1Path).Trim(), out var legacyValue))
        {
            return legacyValue;
        }

        return null;
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return leftBytes.Length == rightBytes.Length && CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    private string GetRuntimeStatsPath()
    {
        return NormalizePrefix(_options.RoutePrefix) + "/runtime-stats";
    }

    private string BuildHtml()
    {
        var title = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(_options.Title) ? "DynamicApp Manager" : _options.Title);
        var managerPrefix = ToJavaScriptString(NormalizePrefix(_managerOptions.RoutePrefix));
        var runtimeStatsPath = ToJavaScriptString(GetRuntimeStatsPath());
        var ticketHeader = ToJavaScriptString(string.IsNullOrWhiteSpace(_managerOptions.HeaderName) ? "X-DynamicApp-Manager-Ticket" : _managerOptions.HeaderName);
        return $$"""
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>{{title}}</title>
  <style>
    :root {
      color-scheme: light;
      --bg: #f6f8fb;
      --panel: #ffffff;
      --line: #d9e0ea;
      --text: #17202a;
      --muted: #687385;
      --primary: #0f766e;
      --primary-dark: #115e59;
      --danger: #b42318;
      --warn: #a15c07;
      --ok: #067647;
      --code: #101828;
    }
    * { box-sizing: border-box; }
    body {
      margin: 0;
      background: var(--bg);
      color: var(--text);
      font-family: "Segoe UI", Arial, sans-serif;
      font-size: 14px;
      letter-spacing: 0;
    }
    header {
      position: sticky;
      top: 0;
      z-index: 3;
      display: flex;
      gap: 16px;
      align-items: center;
      justify-content: space-between;
      padding: 14px 20px;
      background: #ffffff;
      border-bottom: 1px solid var(--line);
      max-width: 1640px;
      margin: 0 auto;
    }
    h1 { margin: 0; font-size: 20px; font-weight: 650; }
    h2 { margin: 0 0 12px; font-size: 16px; font-weight: 650; }
    h3 { margin: 0 0 8px; font-size: 14px; font-weight: 650; }
    [hidden] { display: none !important; }
    .auth-gate {
      min-height: calc(100vh - 52px);
      display: grid;
      place-items: center;
      padding: 24px;
    }
    .auth-card {
      width: min(460px, 100%);
      background: var(--panel);
      border: 1px solid var(--line);
      border-radius: 6px;
      padding: 24px;
      box-shadow: 0 14px 40px rgba(15, 23, 42, .08);
    }
    .auth-card h1 { margin-bottom: 8px; }
    .auth-card .form-grid { grid-template-columns: 1fr; margin-top: 18px; }
    .auth-error {
      min-height: 22px;
      color: var(--danger);
      font-size: 12px;
    }
    main {
      display: grid;
      grid-template-columns: 260px minmax(0, 1fr);
      gap: 16px;
      max-width: 1640px;
      margin: 0 auto;
      padding: 16px;
      align-items: start;
    }
    .workspace section, aside, nav {
      background: var(--panel);
      border: 1px solid var(--line);
      border-radius: 6px;
      padding: 14px;
    }
    .nav-tree {
      position: sticky;
      top: 76px;
      display: grid;
      gap: 14px;
      max-height: calc(100vh - 96px);
      overflow: auto;
    }
    .resource-panel {
      display: grid;
      gap: 14px;
    }
    .resource-sample-bar {
      display: flex;
      gap: 12px;
      align-items: center;
      justify-content: space-between;
    }
    .runtime-stats-head {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 8px;
    }
    .runtime-stats-head button {
      min-height: 28px;
      padding: 0 8px;
      font-size: 12px;
    }
    .runtime-stats-grid {
      display: grid;
      grid-template-columns: repeat(2, minmax(0, 1fr));
      gap: 8px;
    }
    .runtime-stat {
      min-width: 0;
      padding: 8px;
      border: 1px solid #eef2f6;
      border-radius: 5px;
      background: #f8fafc;
    }
    .runtime-stat strong {
      display: block;
      margin-bottom: 4px;
      color: var(--muted);
      font-size: 11px;
      font-weight: 500;
    }
    .runtime-stat span {
      display: block;
      font-size: 13px;
      font-weight: 650;
      overflow-wrap: anywhere;
    }
    .resource-section {
      display: grid;
      gap: 0;
      padding: 0;
      border: 1px solid #eef2f6;
      border-radius: 6px;
      background: #ffffff;
      overflow: hidden;
    }
    .resource-section h3 {
      margin: 0;
      font-size: 15px;
    }
    .resource-section-title {
      display: flex;
      gap: 8px;
      align-items: center;
      justify-content: space-between;
      min-height: 34px;
      box-sizing: border-box;
      padding: 5px 10px;
      border-bottom: 1px solid #eef2f6;
      background: #fbfcfe;
    }
    .resource-section-meta {
      color: var(--muted);
      font-size: 12px;
      font-weight: 650;
      white-space: nowrap;
    }
    .resource-section > .runtime-stats-grid,
    .resource-section > .resource-gauge-row,
    .resource-section > .resource-runtime-list {
      padding: 10px;
    }
    .resource-section > .resource-gauge-row + .runtime-stats-grid {
      padding-top: 0;
    }
    .resource-dashboard-row {
      display: grid;
      grid-template-columns: repeat(2, minmax(0, 1fr));
      gap: 14px;
      align-items: stretch;
    }
    .resource-dashboard-row .resource-section {
      min-width: 0;
    }
    .resource-gauge-row {
      display: grid;
      grid-template-columns: repeat(2, minmax(0, 1fr));
      gap: 10px;
      align-items: stretch;
    }
    .resource-gauge {
      display: grid;
      justify-items: center;
      gap: 6px;
      min-width: 0;
      padding: 10px;
      border: 1px solid #eef2f6;
      border-radius: 6px;
      background: #f8fafc;
    }
    .resource-gauge-dial {
      position: relative;
      width: 112px;
      height: 112px;
      border-radius: 50%;
      background: conic-gradient(var(--gauge-color) var(--gauge-deg), #e5e7eb 0deg);
      display: grid;
      place-items: center;
    }
    .resource-gauge-dial::after {
      content: "";
      position: absolute;
      inset: 12px;
      border-radius: 50%;
      background: #fff;
      border: 1px solid #eef2f6;
    }
    .resource-gauge-value {
      position: relative;
      z-index: 1;
      font-size: 18px;
      font-weight: 750;
    }
    .resource-gauge-label {
      color: var(--muted);
      font-size: 12px;
      font-weight: 650;
    }
    .resource-gauge-detail {
      max-width: 100%;
      color: var(--text);
      font-size: 12px;
      overflow-wrap: anywhere;
    }
    .resource-switch-gauge {
      grid-template-columns: minmax(120px, .9fr) minmax(140px, 1fr);
      justify-items: stretch;
      align-items: center;
    }
    .resource-switch-gauge-main {
      display: grid;
      justify-items: center;
      gap: 6px;
      min-width: 0;
    }
    .resource-metric-list {
      display: grid;
      gap: 6px;
      min-width: 0;
    }
    .resource-metric-button {
      width: 100%;
      min-height: 42px;
      display: grid;
      gap: 2px;
      justify-content: stretch;
      text-align: left;
      padding: 6px 8px;
      border: 1px solid #e2e8f0;
      border-radius: 5px;
      background: #fff;
      color: var(--text);
    }
    .resource-metric-button strong,
    .resource-metric-button span {
      min-width: 0;
      overflow-wrap: anywhere;
    }
    .resource-metric-button strong {
      font-size: 12px;
    }
    .resource-metric-button span {
      color: var(--muted);
      font-size: 11px;
    }
    .resource-metric-button.active {
      border-color: #0f766e;
      background: #e6fffb;
      color: #0f766e;
    }
    .resource-runtime-list {
      display: grid;
      gap: 8px;
    }
    .resource-runtime-head,
    .resource-runtime-row {
      display: grid;
      grid-template-columns: minmax(130px, .95fr) minmax(110px, .85fr) minmax(110px, .85fr) minmax(80px, .55fr) minmax(150px, 1fr) minmax(74px, .45fr) minmax(86px, .55fr);
      gap: 8px;
      align-items: center;
    }
    .resource-runtime-head {
      padding: 0 10px;
      color: var(--muted);
      font-size: 11px;
      font-weight: 650;
    }
    .resource-runtime-row {
      padding: 10px;
      border: 1px solid #eef2f6;
      border-radius: 5px;
      background: #f8fafc;
    }
    .resource-runtime-row strong,
    .resource-runtime-row span,
    .resource-runtime-head span {
      min-width: 0;
      overflow-wrap: anywhere;
    }
    .resource-runtime-row .muted {
      font-size: 12px;
    }
    .nav-group {
      display: grid;
      gap: 6px;
    }
    .nav-title {
      color: var(--muted);
      font-size: 12px;
      font-weight: 650;
      text-transform: uppercase;
    }
    .nav-button {
      width: 100%;
      justify-content: flex-start;
      min-height: 32px;
      padding: 0 10px;
      border-color: transparent;
      background: transparent;
      text-align: left;
    }
    .nav-button:hover { background: #eef2f6; }
    .nav-button.active {
      background: #e6fffb;
      border-color: #99f6e4;
      color: var(--primary-dark);
      font-weight: 650;
    }
    .workspace {
      display: grid;
      gap: 16px;
      min-width: 0;
    }
    .view { display: none; }
    .view.active { display: block; }
    .toolbar {
      display: flex;
      flex-wrap: wrap;
      gap: 8px;
      align-items: center;
    }
    .ticket {
      display: grid;
      grid-template-columns: minmax(120px, auto) auto auto;
      gap: 8px;
      width: auto;
      align-items: center;
    }
    .ticket select { width: 140px; }
    input, textarea, select, button {
      min-height: 34px;
      border: 1px solid var(--line);
      border-radius: 5px;
      font: inherit;
    }
    input, textarea, select {
      width: 100%;
      padding: 7px 9px;
      background: #fff;
      color: var(--text);
    }
    textarea {
      min-height: 220px;
      resize: vertical;
      font-family: Consolas, "Courier New", monospace;
      font-size: 12px;
      line-height: 1.5;
    }
    button {
      padding: 0 12px;
      background: #fff;
      color: var(--text);
      cursor: pointer;
      white-space: nowrap;
    }
    .icon-button {
      width: 34px;
      min-width: 34px;
      padding: 0;
      display: inline-grid;
      place-items: center;
      font-size: 18px;
      line-height: 1;
    }
    .draw-plus-icon {
      position: relative;
      font-size: 0;
    }
    .draw-plus-icon::before,
    .draw-plus-icon::after {
      content: "";
      position: absolute;
      left: 50%;
      top: 50%;
      width: 14px;
      height: 2px;
      border-radius: 2px;
      background: currentColor;
      transform: translate(-50%, -50%);
    }
    .draw-plus-icon::after {
      width: 2px;
      height: 14px;
    }
    .toolbar-menu,
    .split-button {
      position: relative;
      display: inline-flex;
      align-items: center;
    }
    .split-button > button {
      border-radius: 0;
    }
    .split-button > button:first-child {
      border-top-left-radius: 5px;
      border-bottom-left-radius: 5px;
    }
    .split-button > button:nth-child(2) {
      border-top-right-radius: 5px;
      border-bottom-right-radius: 5px;
      border-left: 0;
    }
    .split-button .icon-button {
      width: 32px;
      min-width: 32px;
      font-size: 0;
    }
    .split-button .icon-button::before {
      content: "";
      width: 0;
      height: 0;
      border-left: 4px solid transparent;
      border-right: 4px solid transparent;
      border-top: 5px solid currentColor;
    }
    .toolbar-dropdown {
      position: absolute;
      right: 0;
      top: calc(100% + 6px);
      z-index: 4;
      min-width: 138px;
      padding: 8px;
      border: 1px solid var(--line);
      border-radius: 6px;
      background: #fff;
      box-shadow: 0 12px 32px rgba(15, 23, 42, .12);
    }
    .toolbar-dropdown button {
      width: 100%;
      justify-content: flex-start;
    }
    button.primary { background: var(--primary); color: #fff; border-color: var(--primary); }
    button.primary:hover { background: var(--primary-dark); }
    button.warning {
      background: var(--warn);
      color: #fff;
      border-color: var(--warn);
    }
    button.warning:hover {
      background: #7a4304;
      border-color: #7a4304;
    }
    button.danger,
    button.close-action {
      background: var(--danger);
      color: #fff;
      border-color: var(--danger);
    }
    button.danger:hover,
    button.close-action:hover {
      background: #912018;
      border-color: #912018;
    }
    button:disabled { opacity: .55; cursor: not-allowed; }
    #runAccessLog,
    #closeAccessLogModal {
      margin-bottom: 6px;
    }
    .grid { display: grid; gap: 16px; }
    .two { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; }
    .three { grid-template-columns: repeat(3, minmax(0, 1fr)); }
    .output-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 16px; }
    .card {
      border: 1px solid var(--line);
      border-radius: 6px;
      padding: 12px;
      background: #fff;
    }
    .row {
      display: flex;
      flex-wrap: wrap;
      gap: 8px;
      align-items: center;
      justify-content: space-between;
      padding: 9px 0;
      border-top: 1px solid #eef2f6;
    }
    .row:first-child { border-top: 0; }
    .mod-row {
      display: grid;
      grid-template-columns: minmax(0, 1fr);
      align-items: start;
      justify-content: stretch;
      gap: 10px;
    }
    .mod-row .toolbar {
      justify-content: flex-start;
    }
    .muted { color: var(--muted); }
    .danger { color: var(--danger); }
    .mono { font-family: Consolas, "Courier New", monospace; }
    .small { font-size: 12px; }
    .actions { justify-content: flex-end; }
    .file-summary {
      min-height: 34px;
      padding: 8px;
      border: 1px dashed var(--line);
      border-radius: 5px;
      background: #f8fafc;
      color: var(--muted);
      font-size: 12px;
      overflow-wrap: anywhere;
    }
    .package-detail-summary {
      display: grid;
      gap: 8px;
    }
    .package-detail-summary-item {
      display: grid;
      gap: 4px;
      padding: 8px 0;
      border-top: 1px solid #eef2f6;
    }
    .package-detail-summary-item:first-child { border-top: 0; }
    .upload-metadata-summary {
      display: flex;
      flex-wrap: wrap;
      gap: 8px;
      padding: 10px;
      border-left: 4px solid var(--primary);
      border-radius: 4px;
      background: #ecfeff;
      color: var(--text);
    }
    .upload-metadata-item {
      display: inline-grid;
      grid-template-columns: auto auto;
      gap: 5px;
      align-items: center;
      max-width: 100%;
      font-size: 12px;
    }
    .upload-metadata-item strong {
      color: var(--muted);
      font-weight: 500;
    }
    .upload-metadata-item span {
      overflow-wrap: anywhere;
    }
    .upload-result {
      min-height: 96px;
      max-height: 220px;
      margin-top: 10px;
    }
    .upload-result[hidden] { display: none; }
    .upload-metadata-field[hidden] { display: none; }
    input[readonly], textarea[readonly], select:disabled, input:disabled {
      background: #f1f5f9;
      color: #64748b;
      cursor: not-allowed;
    }
    .inventory-list {
      display: grid;
      gap: 12px;
      margin-top: 12px;
      margin-bottom: 14px;
    }
    .application-manager-layout {
      display: grid;
      grid-template-columns: minmax(220px, 300px) minmax(0, 1fr);
      gap: 16px;
      margin-top: 12px;
      align-items: start;
    }
    .application-manager-layout > .card > .toolbar {
      margin-bottom: 12px;
    }
    .application-list {
      display: grid;
      gap: 8px;
    }
    .application-item {
      width: 100%;
      display: grid;
      gap: 4px;
      min-height: auto;
      padding: 10px;
      text-align: left;
      white-space: normal;
    }
    .application-item.active {
      border-color: #99f6e4;
      background: #e6fffb;
      color: var(--primary-dark);
    }
    .package-version {
      padding: 9px 0;
      border-top: 1px solid #eef2f6;
    }
    .package-version-row {
      display: grid;
      grid-template-columns: minmax(0, 1fr) auto;
      gap: 10px;
      align-items: center;
      padding: 9px 0;
      border-top: 1px solid #eef2f6;
    }
    .package-version summary {
      cursor: pointer;
      font-weight: 650;
      overflow-wrap: anywhere;
    }
    .file-list {
      display: grid;
      gap: 6px;
      margin-top: 8px;
    }
    .file-row {
      display: grid;
      grid-template-columns: minmax(0, 1fr);
      gap: 6px;
      align-items: start;
      padding: 7px;
      border: 1px solid #eef2f6;
      border-radius: 5px;
      background: #f8fafc;
      font-size: 12px;
    }
    .file-row-meta {
      display: grid;
      grid-template-columns: minmax(0, 1fr);
      gap: 6px;
      align-items: center;
    }
    .file-row .mono { overflow-wrap: anywhere; }
    .badge {
      display: inline-flex;
      align-items: center;
      height: 22px;
      padding: 0 7px;
      border-radius: 999px;
      background: #eef2f6;
      color: #344054;
      font-size: 12px;
    }
    .badge.ok { background: #dcfae6; color: var(--ok); }
    .badge.warn { background: #fef0c7; color: var(--warn); }
    .badge.bad { background: #fee4e2; color: var(--danger); }
    .modal-backdrop {
      position: fixed;
      inset: 0;
      z-index: 10;
      display: grid;
      place-items: center;
      padding: 24px;
      background: rgba(15, 23, 42, .36);
    }
    .modal {
      width: min(1100px, 100%);
      max-height: min(760px, calc(100vh - 48px));
      overflow: auto;
      background: var(--panel);
      border: 1px solid var(--line);
      border-radius: 6px;
      padding: 16px;
      box-shadow: 0 20px 70px rgba(15, 23, 42, .22);
    }
    #packageUploadModal .modal {
      width: min(560px, 100%);
    }
    .modal-header {
      position: sticky;
      top: -16px;
      z-index: 2;
      margin: -16px -16px 12px;
      padding: 16px;
      border-bottom: 1px solid var(--line);
      background: var(--panel);
    }
    #packageUploadModal .toolbar:first-child {
      position: sticky;
      top: -16px;
      z-index: 2;
      margin: -16px -16px 12px;
      padding: 16px;
      border-bottom: 1px solid var(--line);
      background: var(--panel);
    }
    .modal-close-icon {
      width: 34px;
      min-width: 34px;
      padding: 0;
      display: inline-grid;
      place-items: center;
      font-size: 22px;
      line-height: 1;
    }
    .modal .row { align-items: flex-start; }
    .detail-sections {
      display: grid;
      gap: 14px;
    }
    .detail-section h3 {
      margin-bottom: 8px;
    }
    .detail-section pre {
      min-height: 120px;
      max-height: 260px;
    }
    .access-summary {
      display: grid;
      grid-template-columns: repeat(3, minmax(0, 1fr));
      gap: 10px;
      margin-bottom: 14px;
    }
    .access-summary div {
      padding: 10px;
      border: 1px solid var(--line);
      border-radius: 6px;
      background: #f8fafc;
    }
    .access-summary strong {
      display: block;
      margin-bottom: 4px;
      color: var(--muted);
      font-size: 12px;
      font-weight: 500;
    }
    .access-summary span {
      font-size: 15px;
      font-weight: 600;
      overflow-wrap: anywhere;
    }
    pre {
      margin: 0;
      padding: 12px;
      min-height: 160px;
      max-height: 420px;
      overflow: auto;
      border-radius: 6px;
      background: var(--code);
      color: #e6edf3;
      font-family: Consolas, "Courier New", monospace;
      font-size: 12px;
      line-height: 1.5;
    }
    label {
      display: grid;
      gap: 5px;
      color: var(--muted);
      font-size: 12px;
    }
    .field-hint {
      color: var(--muted);
      font-size: 11px;
      line-height: 1.35;
    }
    .form-grid {
      display: grid;
      grid-template-columns: repeat(2, minmax(0, 1fr));
      gap: 10px;
      margin-bottom: 10px;
    }
    #packageUploadModal .form-grid,
    .form-grid.package-upload-grid {
      grid-template-columns: minmax(0, 1fr);
    }
    .full { grid-column: 1 / -1; }
    @media (max-width: 1050px) {
      main { grid-template-columns: 1fr; }
      .nav-tree { position: static; max-height: none; }
      .two { grid-template-columns: 1fr; }
      .three { grid-template-columns: 1fr; }
      .output-grid { grid-template-columns: 1fr; }
      .application-manager-layout { grid-template-columns: 1fr; }
    }
    @media (max-width: 720px) {
      header { align-items: stretch; flex-direction: column; }
      .ticket { grid-template-columns: 1fr; }
      .form-grid { grid-template-columns: 1fr; }
      .access-summary { grid-template-columns: 1fr; }
      .file-row { grid-template-columns: minmax(0, 1fr); }
      .package-version-row { grid-template-columns: minmax(0, 1fr); }
      .resource-dashboard-row { grid-template-columns: minmax(0, 1fr); }
      .resource-gauge-row { grid-template-columns: minmax(0, 1fr); }
      .resource-switch-gauge { grid-template-columns: minmax(0, 1fr); }
      .resource-runtime-head { display: none; }
      .resource-runtime-row { grid-template-columns: minmax(0, 1fr); }
    }
  </style>
</head>
<body>
  <section id="authGate" class="auth-gate">
    <div class="auth-card">
      <h1>{{title}}</h1>
      <div class="muted" data-i18n="authSubtitle">Enter Manager Ticket to unlock the DynamicApp control panel.</div>
      <div class="form-grid">
        <label><span data-i18n="managerTicket">Manager Ticket</span><input id="ticket" type="password" placeholder="Manager Ticket" data-i18n-placeholder="managerTicket" autocomplete="off"></label>
      </div>
      <div id="authError" class="auth-error"></div>
      <div class="toolbar actions">
        <button class="primary" id="unlockPanel" data-i18n="unlock">Unlock</button>
      </div>
    </div>
  </section>
  <div id="panelShell" hidden>
    <header>
      <div>
        <h1>{{title}}</h1>
        <div class="muted" data-i18n="subtitle">Built-in control panel for mounted DynamicApp Mods</div>
      </div>
      <div class="ticket">
        <select id="language" aria-label="Language">
          <option value="zh-CN">中文</option>
          <option value="en-US">English</option>
        </select>
        <button id="logoutPanel" data-i18n="logout">Logout</button>
      </div>
    </header>
    <main>
    <nav class="nav-tree">
      <div class="nav-group">
        <div class="nav-title" data-i18n="resourceMonitor">Resource Monitor</div>
        <button class="nav-button active" data-view-target="resources" data-i18n="resourceMonitor">Resource Monitor</button>
      </div>
      <div class="nav-group">
        <div class="nav-title" data-i18n="appManagement">Application Management</div>
        <button class="nav-button" data-view-target="upload" data-i18n="packageManager">Application Manager</button>
        <button class="nav-button" data-view-target="applications"><span data-i18n="appRuntime">Application Runtime</span> <span id="navAppCount" class="muted"></span></button>
      </div>
      <div class="nav-group" hidden>
        <div class="nav-title" data-i18n="debug">Debug</div>
        <button class="nav-button" data-view-target="local-debug" data-i18n="localDebugBridge">Local Debug Bridge</button>
      </div>
      <div class="nav-group">
        <div class="nav-title" data-i18n="control">Control</div>
        <button class="nav-button" data-view-target="archives" data-i18n="archiveManager">Archive Manager</button>
        <button class="nav-button" data-view-target="api-console" data-i18n="managerApiConsole">Manager API Console</button>
      </div>
    </nav>
    <div class="workspace">
      <section id="view-resources" class="view active">
        <div class="toolbar">
          <h2 data-i18n="resourceMonitor">Resource Monitor</h2>
          <span id="runtimeUptime" class="badge"></span>
          <span style="flex:1"></span>
          <span id="runtimeStatsSummary" class="muted" data-i18n="notLoaded">Not loaded</span>
          <span id="runtimeAutoRefreshStatus" class="muted"></span>
          <div class="split-button">
            <button id="refreshRuntimeStats" data-i18n="refresh">Refresh</button>
            <button id="toggleRuntimeAutoRefreshMenu" class="icon-button" type="button" data-i18n-title="expand" title="Expand" aria-label="Expand"></button>
            <div id="runtimeAutoRefreshMenu" class="toolbar-dropdown" hidden>
              <button id="openRuntimeAutoRefresh" type="button" data-i18n="autoRefresh">Auto Refresh</button>
            </div>
          </div>
        </div>
        <div id="runtimeStats" class="resource-panel">
          <div class="muted" data-i18n="notLoaded">Not loaded</div>
        </div>
      </section>
      <section id="view-applications" class="view">
        <div class="toolbar">
          <h2 style="flex:1" data-i18n="appRuntime">Application Runtime</h2>
          <span id="summary" class="muted" data-i18n="notLoaded">Not loaded</span>
        </div>
        <div id="applications" class="grid"></div>
      </section>
      <section id="view-api-console" class="view">
        <h2 data-i18n="managerApiConsole">Manager API Console</h2>
        <div class="form-grid">
          <label><span data-i18n="quickApi">Quick API</span>
            <select id="apiOperation">
              <option value="GET /targets">GET /targets</option>
              <option value="GET /containers">GET /containers</option>
              <option value="GET /manifests">GET /manifests</option>
              <option value="GET /self-healing">GET /self-healing</option>
              <option value="GET /archived">GET /archived</option>
              <option value="GET /packages">GET /packages</option>
              <option value="GET /packages/applications">GET /packages/applications</option>
              <option value="POST /packages/applications">POST /packages/applications</option>
              <option value="POST /publish">POST /publish</option>
              <option value="POST /deploy">POST /deploy</option>
              <option value="POST /activate">POST /activate</option>
              <option value="POST /rollback">POST /rollback</option>
              <option value="POST /self-healing/reset">POST /self-healing/reset</option>
              <option value="POST /archive">POST /archive</option>
              <option value="POST /unarchive">POST /unarchive</option>
              <option value="POST /state">POST /state</option>
              <option value="POST /logs">POST /logs</option>
            </select>
          </label>
          <label class="full"><span data-i18n="body">Body</span><textarea id="apiBody" style="min-height:120px"></textarea></label>
        </div>
        <div class="toolbar actions">
          <button id="loadTargets" data-i18n="loadTargets">Load targets</button>
          <button id="loadManifests" data-i18n="loadManifests">Load manifests</button>
          <button class="primary" id="executeApi" data-i18n="execute">Execute</button>
        </div>
      </section>
      <section id="view-archives" class="view">
        <div class="toolbar">
          <h2 style="flex:1" data-i18n="archiveManager">Archive Manager</h2>
          <button class="icon-button" id="refreshArchives" data-i18n-title="refreshArchives" title="Refresh archives" aria-label="Refresh archives"><span aria-hidden="true">↻</span></button>
        </div>
        <div id="archiveList" class="inventory-list"></div>
      </section>
      <section id="view-local-debug" class="view" hidden>
        <h2 data-i18n="localDebugBridge">Local Debug Bridge</h2>
        <div class="form-grid">
          <label><span data-i18n="dynamicAppId">DynamicApp Id</span><input id="debugApp" value="security-platform"></label>
          <label><span data-i18n="modId">Mod Id</span><input id="debugMod" value="auth"></label>
          <label><span data-i18n="version">Version</span><input id="debugVersion" value="local-dev"></label>
          <label><span data-i18n="routePrefix">Route Prefix</span><input id="debugRoute" value="/mods/auth"></label>
          <label><span data-i18n="localEndpoint">Local Endpoint</span><input id="debugEndpoint" value="http://127.0.0.1:5106"></label>
          <label><span data-i18n="healthPath">Health Path</span><input id="debugHealth" value="/Health"></label>
          <label><span data-i18n="startupTimeout">Startup Timeout</span><input id="debugTimeout" value="10"></label>
          <label><span data-i18n="deploy">Deploy</span><select id="debugDeploy"><option value="true">true</option><option value="false">false</option></select></label>
        </div>
        <div class="toolbar">
          <button class="primary" id="submitLocalDebug" data-i18n="attachLocalService">Attach local service</button>
          <button id="resetLocalDebug" data-i18n="resetLocalSample">Reset local sample</button>
        </div>
      </section>
      <section id="view-upload" class="view">
        <div class="toolbar">
          <h2 style="flex:1" data-i18n="packageManager">Application Manager</h2>
        </div>
        <div class="application-manager-layout">
          <div class="card">
            <div class="toolbar">
              <h3 style="flex:1" data-i18n="applications">Applications</h3>
              <button class="icon-button primary draw-plus-icon" id="createPackageApplication" data-i18n-title="createApplication" title="Create Application" aria-label="Create Application"></button>
              <button class="icon-button" id="refreshPackageApplications" data-i18n-title="refreshApplications" title="Refresh applications" aria-label="Refresh applications"><span aria-hidden="true">↻</span></button>
            </div>
            <div id="packageApplicationList" class="application-list"></div>
          </div>
          <div>
            <div class="toolbar">
              <h3 id="selectedPackageApplicationTitle" style="flex:1" data-i18n="packageInventory">Package Inventory</h3>
              <button class="icon-button" id="refreshPackages" data-i18n-title="refreshPackages" title="Refresh packages" aria-label="Refresh packages"><span aria-hidden="true">↻</span></button>
              <button id="openExistingContainer" data-i18n="attachExistingContainer">Attach Container</button>
              <button class="primary" id="openPackageUpload" data-i18n="uploadPackage">Upload Package</button>
            </div>
            <div id="packageManagerList" class="inventory-list"></div>
          </div>
        </div>
      </section>
      <div hidden>
        <pre id="requestLog"></pre>
        <pre id="responseLog"></pre>
        <pre id="detailLog"></pre>
        <pre id="logsLog"></pre>
      </div>
    </div>
    </main>
    <div id="historyModal" class="modal-backdrop" hidden>
      <div class="modal">
        <div class="toolbar modal-header">
          <h2 id="historyTitle" style="flex:1" data-i18n="historyVersions">History Versions</h2>
          <button id="closeHistory" class="modal-close-icon close-action" type="button" data-i18n-title="close" aria-label="Close">&times;</button>
        </div>
        <div id="historyContent" class="grid"></div>
      </div>
    </div>
    <div id="detailModal" class="modal-backdrop" hidden>
      <div class="modal">
        <div class="toolbar">
          <h2 id="detailModalTitle" style="flex:1" data-i18n="details">Details</h2>
          <button id="closeDetailModal" class="close-action" type="button" data-i18n="close">Close</button>
        </div>
        <div class="detail-sections">
          <div class="detail-section">
            <h3 data-i18n="runtimeTarget">Runtime Target</h3>
            <pre id="detailTargetLog"></pre>
          </div>
          <div class="detail-section">
            <h3 data-i18n="publishManifest">Publish Manifest</h3>
            <pre id="detailManifestLog"></pre>
          </div>
          <div class="detail-section">
            <h3 data-i18n="managedLogs">Managed Logs</h3>
            <pre id="detailManagedLogs"></pre>
          </div>
        </div>
      </div>
    </div>
    <div id="packageDetailModal" class="modal-backdrop" hidden>
      <div class="modal">
        <div class="toolbar modal-header">
          <h2 id="packageDetailTitle" style="flex:1" data-i18n="packageDetails">Package Details</h2>
          <button id="closePackageDetail" class="modal-close-icon close-action" type="button" data-i18n-title="close" aria-label="Close">&times;</button>
        </div>
        <div id="packageDetailContent" class="inventory-list"></div>
      </div>
    </div>
    <div id="packageUploadModal" class="modal-backdrop" hidden>
      <div class="modal">
        <div class="toolbar modal-header">
          <h2 id="packageUploadTitle" style="flex:1" data-i18n="uploadPackage">Upload Package</h2>
          <button id="closePackageUpload" class="modal-close-icon close-action" type="button" data-i18n-title="close" aria-label="Close">&times;</button>
        </div>
        <div class="form-grid package-upload-grid">
          <div id="uploadMetadataSummary" class="upload-metadata-summary" role="alert" aria-live="polite" hidden></div>
          <label><span data-i18n="package">Package</span><input id="uploadPackage" type="file" accept=".zip"></label>
          <div class="file-summary" id="uploadSummary">No package selected.</div>
          <input id="uploadApp" type="hidden">
          <label class="upload-mode-field"><span data-i18n="executionMode">Execution Mode</span><select id="uploadExecutionMode"><option value="Process">Process</option></select><span class="field-hint" data-i18n="uploadExecutionModeHint">Zip package upload currently supports Process mode only.</span></label>
          <label class="upload-metadata-field"><span data-i18n="modId">Mod Id</span><input id="uploadMod"></label>
          <label class="upload-version-field"><span data-i18n="version">Version</span><input id="uploadVersion"></label>
          <label class="upload-metadata-field"><span data-i18n="routePrefix">Route Prefix</span><input id="uploadRoute"></label>
          <label class="upload-metadata-field"><span data-i18n="entryFile">Entry File</span><input id="uploadEntry"></label>
          <label class="upload-metadata-field"><span data-i18n="arguments">Arguments</span><input id="uploadArguments"></label>
          <label class="upload-metadata-field"><span data-i18n="endpoint">Endpoint</span><input id="uploadEndpoint"></label>
          <label class="upload-metadata-field"><span data-i18n="healthPath">Health Path</span><input id="uploadHealth"></label>
          <label class="upload-metadata-field"><span data-i18n="startupTimeout">Startup Timeout</span><input id="uploadTimeout"></label>
          <label><span data-i18n="deploy">Deploy</span><select id="uploadDeploy"><option value="true">true</option><option value="false">false</option></select><span class="field-hint" data-i18n="deployHint">true deploys and starts the Mod after upload; false only publishes the manifest.</span></label>
          <label><span data-i18n="overwrite">Overwrite</span><select id="uploadOverwrite"><option value="false">false</option><option value="true">true</option></select><span class="field-hint" data-i18n="overwriteHint">true allows replacing an existing package with the same application, Mod, and version.</span></label>
        </div>
        <pre id="uploadResultLog" class="upload-result" hidden></pre>
        <div class="toolbar actions">
          <button id="cancelPackageUpload" class="close-action" type="button" data-i18n="cancel">Cancel</button>
          <button class="primary" id="submitUpload" data-i18n="uploadAction">Upload</button>
        </div>
      </div>
    </div>
    <div id="existingContainerModal" class="modal-backdrop" hidden>
      <div class="modal">
        <div class="toolbar modal-header">
          <h2 id="existingContainerTitle" style="flex:1" data-i18n="attachExistingContainer">Attach Container</h2>
          <button id="closeExistingContainer" class="modal-close-icon close-action" type="button" data-i18n-title="close" aria-label="Close">&times;</button>
        </div>
        <div class="form-grid">
          <input id="existingContainerApp" type="hidden">
          <label class="full"><span data-i18n="containerName">Container Name</span><select id="existingContainerName"></select><span id="existingContainerStatus" class="field-hint">-</span></label>
          <label><span data-i18n="modId">Mod Id</span><input id="existingContainerMod" value="auth" data-i18n-placeholder="modId" placeholder="Mod Id"></label>
          <label><span data-i18n="version">Version</span><input id="existingContainerVersion" value="101-current"></label>
          <label><span data-i18n="routePrefix">Route Prefix</span><input id="existingContainerRoute" value="/mods/auth"></label>
          <label><span data-i18n="endpoint">Endpoint</span><input id="existingContainerEndpoint" value="http://air.auth:8080"></label>
          <input id="existingContainerHealth" type="hidden" value="/Health">
          <input id="existingContainerTimeout" type="hidden" value="60">
          <div class="upload-metadata-summary full" role="alert" aria-live="polite">
            <div class="upload-metadata-item"><strong data-i18n="executionMode">Execution Mode</strong><span class="mono">ExistingContainer</span></div>
            <div class="upload-metadata-item"><strong data-i18n="selectedApplication">Selected Application</strong><span id="existingContainerAppLabel" class="mono">-</span></div>
          </div>
        </div>
        <div class="toolbar actions">
          <button id="cancelExistingContainer" class="close-action" type="button" data-i18n="cancel">Cancel</button>
          <button class="primary" id="submitExistingContainer" data-i18n="attachExistingContainer">Register Container</button>
        </div>
      </div>
    </div>
    <div id="runtimeAutoRefreshModal" class="modal-backdrop" hidden>
      <div class="modal">
        <div class="toolbar modal-header">
          <h2 style="flex:1" data-i18n="autoRefresh">Auto Refresh</h2>
          <button id="closeRuntimeAutoRefresh" class="modal-close-icon close-action" type="button" data-i18n-title="close" aria-label="Close">&times;</button>
        </div>
        <div class="form-grid">
          <label><span data-i18n="refreshInterval">Refresh Interval</span>
            <select id="runtimeAutoRefreshInterval">
              <option value="10">10s</option>
              <option value="20">20s</option>
              <option value="30">30s</option>
              <option value="60">60s</option>
              <option value="custom" data-i18n="customSeconds">Custom seconds</option>
            </select>
          </label>
          <label id="runtimeAutoRefreshCustomField" hidden><span data-i18n="customSeconds">Custom seconds</span><input id="runtimeAutoRefreshCustom" type="number" min="10" step="1" value="10"></label>
          <div class="alert full" data-i18n="autoRefreshHint">The interval is kept only in the current page session and is lost after browser refresh.</div>
        </div>
        <div class="toolbar actions">
          <button id="stopRuntimeAutoRefresh" class="close-action" type="button" data-i18n="stopAutoRefresh">Stop auto refresh</button>
          <button id="cancelRuntimeAutoRefresh" type="button" data-i18n="cancel">Cancel</button>
          <button id="confirmRuntimeAutoRefresh" class="primary" type="button" data-i18n="confirm">Confirm</button>
        </div>
      </div>
    </div>
    <div id="accessLogModal" class="modal-backdrop" hidden>
      <div class="modal">
        <div class="toolbar">
          <h2 id="accessLogTitle" style="flex:1" data-i18n="accessLog">Access Log</h2>
          <button class="primary" id="runAccessLog" data-i18n="execute">Execute</button>
          <button id="closeAccessLogModal" class="close-action" type="button" data-i18n="close">Close</button>
        </div>
        <div class="access-summary">
          <div><strong data-i18n="statusCode">Status Code</strong><span id="accessStatus">-</span></div>
          <div><strong data-i18n="duration">Duration</strong><span id="accessDuration">-</span></div>
          <div><strong data-i18n="completedAt">Completed At</strong><span id="accessCompletedAt">-</span></div>
        </div>
        <div class="detail-sections">
          <div class="detail-section">
            <h3 data-i18n="request">Request</h3>
            <pre id="accessRequestLog"></pre>
          </div>
          <div class="detail-section">
            <h3 data-i18n="response">Response</h3>
            <pre id="accessResponseLog"></pre>
          </div>
          <div class="detail-section">
            <h3 data-i18n="body">Body</h3>
            <pre id="accessBodyLog"></pre>
          </div>
        </div>
      </div>
    </div>
  </div>
  <script>
    const managerPrefix = {{managerPrefix}};
    const runtimeStatsPath = {{runtimeStatsPath}};
    const ticketHeader = {{ticketHeader}};
    const ticketInput = document.getElementById('ticket');
    const requestLog = document.getElementById('requestLog');
    const responseLog = document.getElementById('responseLog');
    const detailLog = document.getElementById('detailLog');
    const logsLog = document.getElementById('logsLog');
    const appsNode = document.getElementById('applications');
    const summaryNode = document.getElementById('summary');
    const runtimeStatsNode = document.getElementById('runtimeStats');
    const runtimeStatsSummary = document.getElementById('runtimeStatsSummary');
    const runtimeUptimeNode = document.getElementById('runtimeUptime');
    const runtimeAutoRefreshStatus = document.getElementById('runtimeAutoRefreshStatus');
    const runtimeAutoRefreshModal = document.getElementById('runtimeAutoRefreshModal');
    const runtimeAutoRefreshMenu = document.getElementById('runtimeAutoRefreshMenu');
    const toggleRuntimeAutoRefreshMenuButton = document.getElementById('toggleRuntimeAutoRefreshMenu');
    const runtimeAutoRefreshInterval = document.getElementById('runtimeAutoRefreshInterval');
    const runtimeAutoRefreshCustom = document.getElementById('runtimeAutoRefreshCustom');
    const runtimeAutoRefreshCustomField = document.getElementById('runtimeAutoRefreshCustomField');
    const stopRuntimeAutoRefreshButton = document.getElementById('stopRuntimeAutoRefresh');
    const packageApplicationList = document.getElementById('packageApplicationList');
    const selectedPackageApplicationTitle = document.getElementById('selectedPackageApplicationTitle');
    const packageManagerList = document.getElementById('packageManagerList');
    const archiveList = document.getElementById('archiveList');
    const languageInput = document.getElementById('language');
    const authGate = document.getElementById('authGate');
    const panelShell = document.getElementById('panelShell');
    const authError = document.getElementById('authError');
    const unlockButton = document.getElementById('unlockPanel');
    const logoutButton = document.getElementById('logoutPanel');
    const historyModal = document.getElementById('historyModal');
    const historyTitle = document.getElementById('historyTitle');
    const historyContent = document.getElementById('historyContent');
    const detailModal = document.getElementById('detailModal');
    const detailModalTitle = document.getElementById('detailModalTitle');
    const detailTargetLog = document.getElementById('detailTargetLog');
    const detailManifestLog = document.getElementById('detailManifestLog');
    const detailManagedLogs = document.getElementById('detailManagedLogs');
    const packageDetailModal = document.getElementById('packageDetailModal');
    const packageDetailTitle = document.getElementById('packageDetailTitle');
    const packageDetailContent = document.getElementById('packageDetailContent');
    const packageUploadModal = document.getElementById('packageUploadModal');
    const packageUploadTitle = document.getElementById('packageUploadTitle');
    const uploadMetadataSummary = document.getElementById('uploadMetadataSummary');
    const uploadResultLog = document.getElementById('uploadResultLog');
    const submitUploadButton = document.getElementById('submitUpload');
    const existingContainerModal = document.getElementById('existingContainerModal');
    const existingContainerTitle = document.getElementById('existingContainerTitle');
    const existingContainerNameInput = document.getElementById('existingContainerName');
    const existingContainerStatus = document.getElementById('existingContainerStatus');
    const submitExistingContainerButton = document.getElementById('submitExistingContainer');
    const runAccessLogButton = document.getElementById('runAccessLog');
    const closeAccessLogButton = document.getElementById('closeAccessLogModal');
    const accessLogModal = document.getElementById('accessLogModal');
    const accessLogTitle = document.getElementById('accessLogTitle');
    const accessStatus = document.getElementById('accessStatus');
    const accessDuration = document.getElementById('accessDuration');
    const accessCompletedAt = document.getElementById('accessCompletedAt');
    const accessRequestLog = document.getElementById('accessRequestLog');
    const accessResponseLog = document.getElementById('accessResponseLog');
    const accessBodyLog = document.getElementById('accessBodyLog');
    let cachedApps = [];
    let isAuthorized = false;
    let isAccessLogLocked = false;
    let isAccessLogRunning = false;
    let pendingAccessLog = null;
    let cachedRuntimeStats = null;
    let runtimeAutoRefreshTimer = null;
    let runtimeAutoRefreshSeconds = 0;
    let runtimeAutoRefreshNextAt = 0;
    let runtimeUptimeBaseSeconds = 0;
    let runtimeUptimeBaseAt = 0;
    let runtimeUptimeTimer = null;
    let cachedPackageApplications = [];
    let cachedPackages = [];
    let currentPackageApplicationId = '';
    let packageUploadMode = 'create';
    let packageUploadSourceKey = null;
    let cachedExistingContainers = [];
    let cachedArchives = [];

    // zh-cn: 控制面板内置双语词典，避免引入前端构建链路。
    // en-us: Built-in bilingual dictionary for the panel, keeping the page build-free.
    const i18n = {
      'zh-CN': {
        subtitle: '用于管理已挂载 DynamicApp Mods 的内置控制面板',
        authSubtitle: '输入 Manager Ticket 后才能进入 DynamicApp 控制面板。',
        managerTicket: 'Manager Ticket',
        save: '保存',
        refresh: '刷新',
        unlock: '进入',
        logout: '退出',
        authorized: '已授权',
        ticketRequired: '请输入 Manager Ticket。',
        ticketInvalid: 'Manager Ticket 校验失败，请确认后重试。',
        runtime: '运行时',
        appManagement: '应用管理',
        appRuntime: '应用运行时',
        debug: '调试',
        resourceUsage: '资源使用',
        cpuUsage: 'CPU',
        memoryUsage: '内存',
        managedMemory: '托管内存',
        uptime: '运行时长',
        threads: '线程',
        processId: '进程ID',
        applications: '应用',
        configure: '配置',
        localDebugBridge: '本地联调桥接',
        packageUpload: '包上传',
        packageManager: '应用管理器',
        createApplication: '创建应用',
        createApplicationPrompt: '请输入 DynamicApp 应用标识',
        refreshApplications: '刷新应用',
        noPackageApplications: '暂无包管理应用，请先创建应用。',
        selectApplicationFirst: '请先选择或创建应用。',
        packageApplicationCount: '{count} 个应用',
        selectedApplication: '当前应用',
        uploadPackage: '上传包文件',
        attachExistingContainer: '登记已有容器',
        existingContainerSucceeded: '已有容器登记完成。',
        containerName: '容器名',
        containerStatus: '容器状态',
        containerRequired: '请选择要登记的容器。',
        noExistingContainers: '未读取到可登记的已有容器。',
        refreshPackages: '刷新包清单',
        packageInventory: '包库存',
        noPackages: '暂无已上传的 Mod 包。',
        packageFiles: '版本文件',
        noPackageFiles: '该登记项没有包文件。',
        packageDetails: '包详情',
        updatePackage: '更新包',
        extractedFiles: '解压文件',
        lastModified: '最后修改',
        filePath: '文件路径',
        control: '控制',
        managerApiConsole: 'Manager API 控制台',
        archiveManager: '归档管理',
        refreshArchives: '刷新归档',
        noArchives: '暂无归档记录。',
        archivedAt: '归档时间',
        unarchive: '解除归档',
        inspect: '检查',
        request: '请求',
        response: '响应',
        details: '详情',
        logs: '日志',
        notLoaded: '未加载',
        quickApi: '快捷 API',
        body: '请求体',
        loadTargets: '加载目标',
        loadManifests: '加载清单',
        execute: '执行',
        dynamicAppId: 'DynamicApp 标识',
        modId: 'Mod 标识',
        version: '版本',
        routePrefix: '路由前缀',
        localEndpoint: '本地端点',
        healthPath: '健康检查路径',
        startupTimeout: '启动超时',
        deploy: '部署',
        attachLocalService: '接入本地服务',
        resetLocalSample: '重置本地示例',
        executionMode: '执行模式',
        endpoint: '端点',
        entryFile: '入口文件',
        image: '镜像',
        arguments: '参数',
        package: '压缩包',
        noPackageSelected: '未选择压缩包。',
        overwrite: '覆盖',
        auto: '自动',
        loading: '加载中...',
        loadFailed: '加载失败：{message}',
        noApplications: '未发现 DynamicApp 运行目标或清单。',
        applicationCount: '{count} 个应用',
        latest: '最新发布',
        currentVersion: '当前版本',
        historyVersions: '历史版本',
        viewHistory: '查看历史版本',
        historyCount: '{count} 个历史版本',
        noHistory: '没有历史版本。',
        close: '关闭',
        cancel: '取消',
        active: '运行',
        mods: 'Mods',
        health: '健康检查',
        start: '启动',
        stop: '停止',
        restart: '重启',
        activate: '激活',
        rollback: '回滚',
        state: '设置状态',
        setState: '设置状态',
        routeTest: '路由测试',
        target: '目标',
        manifest: '清单',
        runtimeTarget: '运行目标',
        publishManifest: '发布清单',
        managedLogs: '托管日志',
        accessLog: '即时访问日志',
        pendingExecute: '待执行，点击执行按钮后开始调用。',
        statusCode: '状态码',
        duration: '耗时',
        completedAt: '完成时间',
        lifecycleState: '生命周期状态',
        healthStatus: '健康状态',
        statusMessage: '状态消息',
        manualStateMessage: '从 DynamicApp 控制面板手动更新状态。',
        confirmAction: '确认执行 {action} {target}？',
        confirmExecute: '确认执行 {operation}？',
        confirmUpdatePackage: '确认更新当前包版本？',
        packageRequired: '必须选择压缩包文件',
        versionRequired: '必须填写新版本。',
        modRequired: '必须填写 Mod 标识。',
        endpointRequired: '必须填写端点。',
        routeRequired: '必须填写路由前缀。',
        invalidEndpoint: '端点必须是 http 或 https 绝对地址。',
        versionMustChange: '新版本不能与当前版本相同。',
        packageZipRequired: '压缩包文件必须是 .zip 格式',
        confirmOverwrite: '确认覆盖已有包版本？',
        uploadAction: '上传',
        updateAction: '更新',
        deployHint: 'true 表示上传后立即部署、启动并激活；false 只保存包并发布 Manifest，不启动服务。',
        uploadExecutionModeHint: 'zip 包上传目前只支持 Process 模式；容器登记请使用“登记已有容器”。',
        overwriteHint: 'true 表示允许替换相同应用、Mod、版本下已经存在的包；false 遇到同版本会拒绝。',
        uploadStarted: '正在上传，请等待...',
        updateStarted: '正在更新，请等待...',
        uploadSucceeded: '上传完成。',
        updateSucceeded: '更新完成。',
        uploadFailed: '上传失败：{message}',
        name: '名称',
        size: '大小',
        type: '类型',
        invalid: '无效',
        unavailable: '不可用',
        selfHealing: '自愈',
        attempts: '重试',
        paused: '已暂停',
        nextRetry: '下次重试',
        windowReset: '窗口重置',
        resetSelfHealing: '重置自愈',
        archive: '归档',
        archiveReason: '归档原因',
        archiveDefaultReason: '坏包或错误 Manifest 治理归档',
        archiveCompleted: '归档完成'
      },
      'en-US': {
        subtitle: 'Built-in control panel for mounted DynamicApp Mods',
        authSubtitle: 'Enter Manager Ticket to unlock the DynamicApp control panel.',
        managerTicket: 'Manager Ticket',
        save: 'Save',
        refresh: 'Refresh',
        unlock: 'Unlock',
        logout: 'Logout',
        authorized: 'Authorized',
        ticketRequired: 'Manager Ticket is required.',
        ticketInvalid: 'Manager Ticket validation failed. Check it and retry.',
        runtime: 'Runtime',
        appManagement: 'Application Management',
        appRuntime: 'Application Runtime',
        debug: 'Debug',
        resourceUsage: 'Resource Usage',
        cpuUsage: 'CPU',
        memoryUsage: 'Memory',
        managedMemory: 'Managed',
        uptime: 'Uptime',
        threads: 'Threads',
        processId: 'Process ID',
        applications: 'Applications',
        configure: 'Configure',
        localDebugBridge: 'Local Debug Bridge',
        packageUpload: 'Package Upload',
        packageManager: 'Application Manager',
        createApplication: 'Create Application',
        createApplicationPrompt: 'Enter DynamicApp application id',
        refreshApplications: 'Refresh applications',
        noPackageApplications: 'No package applications. Create an application first.',
        selectApplicationFirst: 'Select or create an application first.',
        packageApplicationCount: '{count} application(s)',
        selectedApplication: 'Selected Application',
        uploadPackage: 'Upload Package',
        attachExistingContainer: 'Register Container',
        existingContainerSucceeded: 'Existing container registered.',
        containerName: 'Container Name',
        containerStatus: 'Container Status',
        containerRequired: 'Select a container to register.',
        noExistingContainers: 'No existing containers were loaded.',
        refreshPackages: 'Refresh packages',
        packageInventory: 'Package Inventory',
        noPackages: 'No uploaded Mod packages.',
        packageFiles: 'Version files',
        noPackageFiles: 'This registration has no package files.',
        packageDetails: 'Package Details',
        updatePackage: 'Update Package',
        extractedFiles: 'Extracted files',
        lastModified: 'Last modified',
        filePath: 'File path',
        control: 'Control',
        managerApiConsole: 'Manager API Console',
        archiveManager: 'Archive Manager',
        refreshArchives: 'Refresh archives',
        noArchives: 'No archived records.',
        archivedAt: 'Archived at',
        unarchive: 'Unarchive',
        inspect: 'Inspect',
        request: 'Request',
        response: 'Response',
        details: 'Details',
        logs: 'Logs',
        notLoaded: 'Not loaded',
        quickApi: 'Quick API',
        body: 'Body',
        loadTargets: 'Load targets',
        loadManifests: 'Load manifests',
        execute: 'Execute',
        dynamicAppId: 'DynamicApp Id',
        modId: 'Mod Id',
        version: 'Version',
        routePrefix: 'Route Prefix',
        localEndpoint: 'Local Endpoint',
        healthPath: 'Health Path',
        startupTimeout: 'Startup Timeout',
        deploy: 'Deploy',
        attachLocalService: 'Attach local service',
        resetLocalSample: 'Reset local sample',
        executionMode: 'Execution Mode',
        endpoint: 'Endpoint',
        entryFile: 'Entry File',
        image: 'Image',
        arguments: 'Arguments',
        package: 'Package',
        noPackageSelected: 'No package selected.',
        overwrite: 'Overwrite',
        auto: 'Auto',
        loading: 'Loading...',
        loadFailed: 'Load failed: {message}',
        noApplications: 'No DynamicApp targets or manifests found.',
        applicationCount: '{count} application(s)',
        latest: 'latest published',
        currentVersion: 'Current Version',
        historyVersions: 'History Versions',
        viewHistory: 'View history',
        historyCount: '{count} history version(s)',
        noHistory: 'No history versions.',
        close: 'Close',
        cancel: 'Cancel',
        active: 'active',
        mods: 'mods',
        health: 'health',
        start: 'Start',
        stop: 'Stop',
        restart: 'Restart',
        activate: 'Activate',
        rollback: 'Rollback',
        state: 'Set State',
        setState: 'Set State',
        routeTest: 'Route Test',
        target: 'Target',
        manifest: 'Manifest',
        runtimeTarget: 'Runtime Target',
        publishManifest: 'Publish Manifest',
        managedLogs: 'Managed Logs',
        accessLog: 'Access Log',
        pendingExecute: 'Pending. Click Execute to start the request.',
        statusCode: 'Status Code',
        duration: 'Duration',
        completedAt: 'Completed At',
        lifecycleState: 'Lifecycle state',
        healthStatus: 'Health status',
        statusMessage: 'Status message',
        manualStateMessage: 'Manual state update from DynamicApp panel.',
        confirmAction: 'Execute {action} {target}?',
        confirmExecute: 'Execute {operation}?',
        confirmUpdatePackage: 'Update the current package version?',
        packageRequired: 'package file is required',
        versionRequired: 'new version is required.',
        modRequired: 'Mod id is required.',
        endpointRequired: 'endpoint is required.',
        routeRequired: 'route prefix is required.',
        invalidEndpoint: 'endpoint must be an absolute http or https URL.',
        versionMustChange: 'new version must be different from the current version.',
        packageZipRequired: 'package file must be a .zip archive',
        confirmOverwrite: 'Overwrite existing package version?',
        uploadAction: 'Upload',
        updateAction: 'Update',
        deployHint: 'true deploys, starts, and activates the Mod after upload; false only saves the package and publishes the manifest.',
        uploadExecutionModeHint: 'Zip package upload currently supports Process mode only; use Register Container for existing-container registration.',
        overwriteHint: 'true allows replacing an existing package with the same application, Mod, and version; false rejects duplicate versions.',
        uploadStarted: 'Uploading. Please wait...',
        updateStarted: 'Updating. Please wait...',
        uploadSucceeded: 'Upload completed.',
        updateSucceeded: 'Update completed.',
        uploadFailed: 'Upload failed: {message}',
        name: 'name',
        size: 'size',
        type: 'type',
        invalid: 'invalid',
        unavailable: 'unavailable',
        selfHealing: 'Self-healing',
        attempts: 'attempts',
        paused: 'paused',
        nextRetry: 'next retry',
        windowReset: 'window reset',
        resetSelfHealing: 'Reset self-healing',
        archive: 'Archive',
        archiveReason: 'Archive reason',
        archiveDefaultReason: 'Bad package or invalid manifest governance archive',
        archiveCompleted: 'Archive completed.'
      }
    };
    Object.assign(i18n['zh-CN'], {
      resourceMonitor: '资源监控',
      hostProcess: '主进程',
      containerResource: '容器资源',
      modResource: '应用运行时',
      applicationName: '应用名',
      runtimeType: '运行时类型',
      runtimeTypeContainer: '容器',
      runtimeTypeMod: 'Mod',
      lowFrequencySample: '低频采样：每 {seconds} 秒最多采样一次，刷新会复用缓存。',
      sampledAt: '采样时间',
      privateMemory: '私有内存',
      containerMemoryLimit: '容器内存上限',
      unlimited: '无上限',
      resourceUnavailable: '资源不可用',
      noModResources: '暂无应用运行时资源目标。',
      autoRefresh: '定时刷新',
      refreshCountdown: '刷新倒计时: {seconds} s',
      refreshInterval: '刷新间隔',
      customSeconds: '自定义秒数',
      autoRefreshHint: '定时刷新只保存在当前页面会话中，浏览器刷新后会丢失。',
      autoRefreshEnabled: '定时刷新：{seconds}s',
      stopAutoRefresh: '停止定时刷新',
      confirm: '确认',
      expand: '展开',
      customSecondsInvalid: '自定义秒数不能小于 10 秒。'
    });
    Object.assign(i18n['en-US'], {
      resourceMonitor: 'Resource Monitor',
      hostProcess: 'Host Process',
      containerResource: 'Container Resource',
      modResource: 'Application Runtime',
      applicationName: 'Application',
      runtimeType: 'Runtime Type',
      runtimeTypeContainer: 'Container',
      runtimeTypeMod: 'Mod',
      lowFrequencySample: 'Low-frequency sample: at most once every {seconds}s; refresh reuses cached data.',
      sampledAt: 'Sampled At',
      privateMemory: 'Private Memory',
      containerMemoryLimit: 'Container Memory Limit',
      unlimited: 'unlimited',
      resourceUnavailable: 'Resource unavailable',
      noModResources: 'No application runtime resource targets.',
      autoRefresh: 'Auto Refresh',
      refreshCountdown: 'Refresh countdown: {seconds} s',
      refreshInterval: 'Refresh Interval',
      customSeconds: 'Custom seconds',
      autoRefreshHint: 'The interval is kept only in the current page session and is lost after browser refresh.',
      autoRefreshEnabled: 'Auto refresh: {seconds}s',
      stopAutoRefresh: 'Stop auto refresh',
      confirm: 'Confirm',
      expand: 'Expand',
      customSecondsInvalid: 'Custom seconds cannot be less than 10 seconds.'
    });
    Object.assign(i18n['zh-CN'], { processorCount: '处理器数', processorCores: '{count}核' });
    Object.assign(i18n['en-US'], { processorCount: 'Processors', processorCores: '{count} cores' });
    let currentLanguage = resolveLanguage();

    const currentVersionRank = {
      active: 1000,
      starting: 700,
      failed: 300,
      stopped: 200,
      none: 100,
      draining: 50
    };

    const sampleConfig = {
      dynamicAppId: 'security-platform',
      appVersion: '2026.07.25.1',
      deploy: true,
      gatewayPort: 8080,
      managerPort: 8081,
      managerInternalOnly: true,
      mod: {
        id: 'auth',
        version: '2026.07.25.1',
        executionMode: 'ExistingContainer',
        artifact: { endpoint: 'http://air.auth:8080' },
        health: { path: '/Health', startupTimeoutSeconds: 60 },
        route: { prefix: '/mods/auth' }
      }
    };

    const sampleLocalDebug = {
      dynamicAppId: 'security-platform',
      appVersion: 'local-dev',
      deploy: true,
      gatewayPort: 8080,
      managerPort: 8081,
      managerInternalOnly: true,
      mod: {
        id: 'auth',
        version: 'local-dev',
        executionMode: 'LocalDevelopment',
        artifact: { endpoint: 'http://127.0.0.1:5106' },
        health: { path: '/Health', startupTimeoutSeconds: 10 },
        route: { prefix: '/mods/auth' }
      }
    };

    function init() {
      ticketInput.value = sessionStorage.getItem('dynamicapp.manager.ticket') || '';
      languageInput.value = currentLanguage;
      applyLanguage();
      document.getElementById('apiBody').value = JSON.stringify(configureToManifest(sampleConfig), null, 2);
      languageInput.addEventListener('change', () => {
        currentLanguage = languageInput.value;
        localStorage.setItem('dynamicapp.panel.language', currentLanguage);
        applyLanguage();
      });
      unlockButton.addEventListener('click', validateTicket);
      ticketInput.addEventListener('keydown', event => {
        if (event.key === 'Enter') validateTicket();
      });
      logoutButton.addEventListener('click', logoutPanel);
      document.getElementById('refreshRuntimeStats').addEventListener('click', loadRuntimeStats);
      runtimeStatsNode.addEventListener('click', handleResourceMetricClick);
      toggleRuntimeAutoRefreshMenuButton.addEventListener('click', toggleRuntimeAutoRefreshMenu);
      document.getElementById('openRuntimeAutoRefresh').addEventListener('click', openRuntimeAutoRefreshModal);
      document.getElementById('closeRuntimeAutoRefresh').addEventListener('click', closeRuntimeAutoRefreshModal);
      document.getElementById('cancelRuntimeAutoRefresh').addEventListener('click', closeRuntimeAutoRefreshModal);
      document.getElementById('confirmRuntimeAutoRefresh').addEventListener('click', confirmRuntimeAutoRefresh);
      document.getElementById('stopRuntimeAutoRefresh').addEventListener('click', stopRuntimeAutoRefresh);
      runtimeAutoRefreshInterval.addEventListener('change', syncRuntimeAutoRefreshCustom);
      document.getElementById('loadTargets').addEventListener('click', () => api('/targets'));
      document.getElementById('loadManifests').addEventListener('click', () => api('/manifests'));
      document.getElementById('refreshPackageApplications').addEventListener('click', loadPackageApplications);
      document.getElementById('createPackageApplication').addEventListener('click', createPackageApplication);
      document.getElementById('refreshPackages').addEventListener('click', loadPackages);
      document.getElementById('openPackageUpload').addEventListener('click', () => openPackageUploadModal());
      document.getElementById('openExistingContainer').addEventListener('click', openExistingContainerModal);
      document.getElementById('refreshArchives').addEventListener('click', loadArchives);
      document.getElementById('executeApi').addEventListener('click', executeApiConsole);
      document.getElementById('apiOperation').addEventListener('change', fillApiConsoleSample);
      document.getElementById('submitLocalDebug').addEventListener('click', submitLocalDebug);
      document.getElementById('resetLocalDebug').addEventListener('click', resetLocalDebug);
      document.getElementById('uploadPackage').addEventListener('change', refreshUploadSummary);
      submitUploadButton.addEventListener('click', submitUpload);
      document.getElementById('cancelPackageUpload').addEventListener('click', closePackageUploadModal);
      document.getElementById('cancelExistingContainer').addEventListener('click', closeExistingContainerModal);
      existingContainerNameInput.addEventListener('change', syncExistingContainerSelection);
      submitExistingContainerButton.addEventListener('click', submitExistingContainer);
      document.querySelectorAll('[data-view-target]').forEach(button => {
        button.addEventListener('click', () => showView(button.dataset.viewTarget));
      });
      document.getElementById('closeHistory').addEventListener('click', closeHistory);
      document.getElementById('closeDetailModal').addEventListener('click', closeDetailModal);
      document.getElementById('closePackageDetail').addEventListener('click', closePackageDetailModal);
      document.getElementById('closePackageUpload').addEventListener('click', closePackageUploadModal);
      document.getElementById('closeExistingContainer').addEventListener('click', closeExistingContainerModal);
      runAccessLogButton.addEventListener('click', runPendingAccessLog);
      closeAccessLogButton.addEventListener('click', () => closeAccessLogModal(false));
      if (ticketInput.value.trim()) {
        validateTicket();
      } else {
        ticketInput.focus();
      }
    }

    function resolveLanguage() {
      const saved = localStorage.getItem('dynamicapp.panel.language');
      if (saved && i18n[saved]) return saved;
      const browser = (navigator.language || '').toLowerCase();
      return browser.startsWith('zh') ? 'zh-CN' : 'en-US';
    }

    function t(key, values) {
      const table = i18n[currentLanguage] || i18n['en-US'];
      const text = table[key] || i18n['en-US'][key] || key;
      return text.replace(/\{(\w+)\}/g, (_, name) => values && values[name] !== undefined ? values[name] : '');
    }

    function applyLanguage() {
      document.documentElement.lang = currentLanguage;
      document.querySelectorAll('[data-i18n]').forEach(node => {
        node.textContent = t(node.dataset.i18n);
      });
      document.querySelectorAll('[data-i18n-placeholder]').forEach(node => {
        node.setAttribute('placeholder', t(node.dataset.i18nPlaceholder));
      });
      document.querySelectorAll('[data-i18n-title]').forEach(node => {
        const text = t(node.dataset.i18nTitle);
        node.setAttribute('title', text);
        node.setAttribute('aria-label', text);
      });
      if (summaryNode.dataset.count) {
        summaryNode.textContent = t('applicationCount', { count: summaryNode.dataset.count });
      }
      if (cachedApps.length) {
        renderApplications(cachedApps);
      } else if (appsNode.dataset.status === 'loading') {
        appsNode.innerHTML = '<div class="muted">' + escapeHtml(t('loading')) + '</div>';
      } else if (appsNode.dataset.status === 'empty') {
        appsNode.innerHTML = '<div class="muted">' + escapeHtml(t('noApplications')) + '</div>';
      } else if (appsNode.dataset.status === 'failed') {
        appsNode.innerHTML = '<div class="muted">' + escapeHtml(t('loadFailed', { message: appsNode.dataset.message || '' })) + '</div>';
      }
      if (cachedRuntimeStats) {
        renderRuntimeStats(cachedRuntimeStats);
      }
      renderRuntimeAutoRefreshStatus();
      renderPackageApplications(cachedPackageApplications);
      renderPackages(cachedPackages);
      renderArchives(cachedArchives);
      refreshUploadSummary().catch(error => {
        logResponse({ error: error.message });
      });
      if (authError.dataset.key) {
        authError.textContent = t(authError.dataset.key);
      }
      renderUploadMetadataSummary();
      syncPackageUploadActionText();
    }

    function showView(name) {
      if (!name) return;
      document.querySelectorAll('.view').forEach(node => node.classList.remove('active'));
      document.querySelectorAll('.nav-button').forEach(node => node.classList.remove('active'));
      const view = document.getElementById('view-' + name);
      const button = document.querySelector('[data-view-target="' + cssEscape(name) + '"]');
      if (view) view.classList.add('active');
      if (button) button.classList.add('active');
    }

    async function validateTicket() {
      const ticket = ticketInput.value.trim();
      authError.textContent = '';
      authError.dataset.key = '';
      if (!ticket) {
        authError.dataset.key = 'ticketRequired';
        authError.textContent = t('ticketRequired');
        ticketInput.focus();
        return;
      }

      try {
        unlockButton.disabled = true;
        const result = await api('/applications');
        isAuthorized = true;
        sessionStorage.setItem('dynamicapp.manager.ticket', ticket);
        authGate.hidden = true;
        panelShell.hidden = false;
        renderApplicationResult(result);
        loadRuntimeStats().catch(error => {
          renderRuntimeStatsError(error.message);
        });
        loadPackageApplications().catch(error => {
          renderPackageApplicationsError(error.message);
        });
        loadPackages().catch(error => {
          renderPackagesError(error.message);
        });
        loadArchives().catch(error => {
          renderArchivesError(error.message);
        });
        showView('resources');
      } catch {
        isAuthorized = false;
        panelShell.hidden = true;
        authGate.hidden = false;
        authError.dataset.key = 'ticketInvalid';
        authError.textContent = t('ticketInvalid');
        ticketInput.focus();
      } finally {
        unlockButton.disabled = false;
      }
    }

    function logoutPanel() {
      isAuthorized = false;
      stopRuntimeAutoRefresh(false);
      stopRuntimeUptime();
      cachedApps = [];
      cachedPackageApplications = [];
      cachedPackages = [];
      currentPackageApplicationId = '';
      cachedArchives = [];
      cachedRuntimeStats = null;
      ticketInput.value = '';
      sessionStorage.removeItem('dynamicapp.manager.ticket');
      closeHistory();
      closeDetailModal();
      runtimeAutoRefreshMenu.hidden = true;
      closeRuntimeAutoRefreshModal();
      closeAccessLogModal(true);
      panelShell.hidden = true;
      authGate.hidden = false;
      appsNode.innerHTML = '';
      runtimeStatsNode.innerHTML = '<div class="muted">' + escapeHtml(t('notLoaded')) + '</div>';
      runtimeStatsSummary.textContent = t('notLoaded');
      runtimeUptimeNode.textContent = '';
      packageApplicationList.innerHTML = '';
      packageManagerList.innerHTML = '';
      archiveList.innerHTML = '';
      summaryNode.dataset.count = '';
      summaryNode.textContent = t('notLoaded');
      document.getElementById('navAppCount').textContent = '';
      requestLog.textContent = '';
      responseLog.textContent = '';
      detailLog.textContent = '';
      logsLog.textContent = '';
      ticketInput.focus();
    }

    function headers(contentType) {
      const result = {};
      const ticket = ticketInput.value.trim();
      if (ticket) result[ticketHeader] = ticket;
      if (contentType) result['Content-Type'] = contentType;
      return result;
    }

    async function api(path, options = {}) {
      const url = managerPrefix + path;
      const request = {
        method: options.method || 'GET',
        headers: options.headers || headers(options.contentType),
        body: options.body
      };
      logRequest({ url, method: request.method, headers: redactHeaders(request.headers), body: previewBody(request.body) });
      const response = await fetch(url, request);
      const text = await response.text();
      let payload = text;
      try { payload = text ? JSON.parse(text) : null; } catch { }
      logResponse({ status: response.status, body: payload });
      if (!response.ok) {
        const error = new Error(formatApiError(response.status, payload));
        error.status = response.status;
        error.payload = payload;
        throw error;
      }
      return payload;
    }

    function formatApiError(status, payload) {
      const parts = ['HTTP ' + status];
      if (payload && typeof payload === 'object') {
        if (payload.errorCode) parts.push(payload.errorCode);
        if (payload.message) parts.push(payload.message);
      } else if (payload) {
        parts.push(String(payload));
      }

      return parts.join(' | ');
    }

    function formatApiPayload(payload) {
      if (payload === null || payload === undefined) return '';
      if (typeof payload === 'string') return payload;
      try {
        return JSON.stringify(payload, null, 2);
      } catch {
        return String(payload);
      }
    }

    async function refreshAll() {
      await loadApplications();
      await Promise.all([
        loadPackageApplications().catch(error => renderPackageApplicationsError(error.message)),
        loadPackages().catch(error => renderPackagesError(error.message)),
        loadArchives().catch(error => renderArchivesError(error.message))
      ]);
    }

    async function loadApplications() {
      if (!isAuthorized) return;
      loadRuntimeStats().catch(error => {
        renderRuntimeStatsError(error.message);
      });
      appsNode.dataset.status = 'loading';
      appsNode.innerHTML = '<div class="muted">' + escapeHtml(t('loading')) + '</div>';
      try {
        const result = await api('/applications');
        renderApplicationResult(result);
      } catch (error) {
        cachedApps = [];
        appsNode.dataset.status = 'failed';
        appsNode.dataset.message = error.message;
        appsNode.innerHTML = '<div class="muted">' + escapeHtml(t('loadFailed', { message: error.message })) + '</div>';
      }
    }

    async function loadRuntimeStats() {
      if (!isAuthorized) return;
      const response = await fetch(runtimeStatsPath, { headers: headers() });
      const text = await response.text();
      let payload = text;
      try { payload = text ? JSON.parse(text) : null; } catch { }
      if (!response.ok) throw new Error('HTTP ' + response.status);
      cachedRuntimeStats = payload;
      renderRuntimeStats(payload);
      return payload;
    }

    function renderRuntimeStats(stats) {
      if (!stats) {
        runtimeStatsNode.innerHTML = '<div class="muted">' + escapeHtml(t('notLoaded')) + '</div>';
        runtimeStatsSummary.textContent = t('notLoaded');
        runtimeUptimeNode.textContent = '';
        return;
      }
      runtimeStatsSummary.textContent = '';
      const interval = Number(stats.sampleIntervalSeconds || 0).toFixed(0);
      const host = stats.hostProcess || stats;
      updateRuntimeUptime(Number(host.uptimeSeconds || stats.uptimeSeconds || 0));
      const container = stats.container || {};
      const mods = Array.isArray(stats.mods) ? stats.mods : [];
      const containerItems = container.isAvailable ? [] : [[t('resourceUnavailable'), container.message || '-']];
      const containerMeta = container.isAvailable
        ? t('processorCount') + ': ' + t('processorCores', { count: stats.processorCount || container.processorCount || '-' })
        : '';
      const containerMemoryPercent = container.memoryLimitBytes ? Number(container.memoryBytes || 0) / Number(container.memoryLimitBytes) * 100 : 100;
      const containerGauges = container.isAvailable ? [
        {
          label: t('cpuUsage'),
          percent: Number(container.cpuPercent || 0),
          detail: Number(container.cpuPercent || 0).toFixed(2) + '%',
          color: getResourceGaugeColor(Number(container.cpuPercent || 0)),
          visualFloorPercent: 2
        },
        {
          label: t('memoryUsage'),
          percent: containerMemoryPercent,
          detail: formatBytes(Number(container.memoryBytes || 0)) + ' / ' + (container.memoryLimitBytes ? formatBytes(Number(container.memoryLimitBytes)) : t('unlimited')),
          color: container.memoryLimitBytes ? getResourceGaugeColor(containerMemoryPercent) : getResourceGaugeColor(0),
          visualFloorPercent: 2
        }
      ] : [];
      const hostItems = [];
      const hostMeta = t('processId') + ': ' + String(host.processId ?? '-');
      const hostMemoryBase = container.memoryLimitBytes || stats.totalAvailableMemoryBytes || 0;
      const hostWorkingSetPercent = hostMemoryBase ? Number(host.workingSetBytes || 0) / Number(hostMemoryBase) * 100 : null;
      const hostPrivatePercent = hostMemoryBase ? Number(host.privateMemoryBytes || 0) / Number(hostMemoryBase) * 100 : null;
      const hostManagedPercent = hostMemoryBase ? Number(host.managedMemoryBytes || 0) / Number(hostMemoryBase) * 100 : null;
      const hostMemoryMetrics = [
        {
          key: 'working-set',
          label: t('memoryUsage'),
          percent: hostWorkingSetPercent,
          detail: formatBytes(Number(host.workingSetBytes || 0)) + ' / ' + formatPercent(hostWorkingSetPercent),
          color: getResourceGaugeColor(hostWorkingSetPercent),
          visualFloorPercent: 2
        },
        {
          key: 'private-memory',
          label: t('privateMemory'),
          percent: hostPrivatePercent,
          detail: formatBytes(Number(host.privateMemoryBytes || 0)) + ' / ' + formatPercent(hostPrivatePercent),
          color: getResourceGaugeColor(hostPrivatePercent),
          visualFloorPercent: 2
        },
        {
          key: 'managed-memory',
          label: t('managedMemory'),
          percent: hostManagedPercent,
          detail: formatBytes(Number(host.managedMemoryBytes || 0)) + ' / ' + formatPercent(hostManagedPercent),
          color: getResourceGaugeColor(hostManagedPercent),
          visualFloorPercent: 2
        }
      ];
      const hostGauges = [
        {
          label: t('cpuUsage'),
          percent: Number(host.cpuPercent || 0),
          detail: Number(host.cpuPercent || 0).toFixed(2) + '%',
          color: getResourceGaugeColor(Number(host.cpuPercent || 0)),
          visualFloorPercent: 2
        },
        {
          label: t('memoryUsage'),
          percent: hostWorkingSetPercent,
          detail: hostMemoryMetrics[0].detail,
          color: hostMemoryMetrics[0].color,
          metrics: hostMemoryMetrics
        }
      ];
      runtimeStatsNode.innerHTML =
        '<div class="alert resource-sample-bar"><span>' + escapeHtml(t('lowFrequencySample', { seconds: interval })) + '</span><span>' + escapeHtml(t('sampledAt')) + ': ' + escapeHtml(formatDateTime(stats.timestampUtc)) + '</span></div>' +
        '<div class="resource-dashboard-row">' +
        renderDashboardResourceSection(t('containerResource'), containerGauges, containerItems, containerMeta) +
        renderDashboardResourceSection(t('hostProcess'), hostGauges, hostItems, hostMeta) +
        '</div>' +
        renderApplicationRuntimeSection(mods);
    }

    function renderRuntimeStatsError(message) {
      runtimeStatsSummary.textContent = t('loadFailed', { message });
      runtimeStatsNode.innerHTML = '<div class="muted">' + escapeHtml(t('loadFailed', { message })) + '</div>';
    }

    function updateRuntimeUptime(seconds) {
      runtimeUptimeBaseSeconds = Math.max(0, Math.floor(seconds || 0));
      runtimeUptimeBaseAt = Date.now();
      renderRuntimeUptime();
      if (!runtimeUptimeTimer) {
        runtimeUptimeTimer = window.setInterval(renderRuntimeUptime, 1000);
      }
    }

    function renderRuntimeUptime() {
      if (!runtimeUptimeBaseAt) {
        runtimeUptimeNode.textContent = '';
        return;
      }

      const elapsed = Math.floor((Date.now() - runtimeUptimeBaseAt) / 1000);
      runtimeUptimeNode.textContent = t('uptime') + ': ' + formatDuration(runtimeUptimeBaseSeconds + elapsed);
    }

    function stopRuntimeUptime() {
      if (runtimeUptimeTimer) {
        window.clearInterval(runtimeUptimeTimer);
        runtimeUptimeTimer = null;
      }

      runtimeUptimeBaseSeconds = 0;
      runtimeUptimeBaseAt = 0;
      runtimeUptimeNode.textContent = '';
    }

    function openRuntimeAutoRefreshModal() {
      runtimeAutoRefreshMenu.hidden = true;
      runtimeAutoRefreshInterval.value = runtimeAutoRefreshSeconds > 0 && [10, 20, 30, 60].includes(runtimeAutoRefreshSeconds)
        ? String(runtimeAutoRefreshSeconds)
        : runtimeAutoRefreshSeconds > 0 ? 'custom' : '10';
      runtimeAutoRefreshCustom.value = runtimeAutoRefreshSeconds > 0 ? String(runtimeAutoRefreshSeconds) : '10';
      syncRuntimeAutoRefreshCustom();
      syncRuntimeAutoRefreshActions();
      runtimeAutoRefreshModal.hidden = false;
    }

    function toggleRuntimeAutoRefreshMenu() {
      runtimeAutoRefreshMenu.hidden = !runtimeAutoRefreshMenu.hidden;
    }

    function closeRuntimeAutoRefreshModal() {
      runtimeAutoRefreshModal.hidden = true;
    }

    function syncRuntimeAutoRefreshCustom() {
      runtimeAutoRefreshCustomField.hidden = runtimeAutoRefreshInterval.value !== 'custom';
    }

    function syncRuntimeAutoRefreshActions() {
      stopRuntimeAutoRefreshButton.hidden = runtimeAutoRefreshSeconds <= 0;
    }

    function confirmRuntimeAutoRefresh() {
      const seconds = runtimeAutoRefreshInterval.value === 'custom'
        ? Number(runtimeAutoRefreshCustom.value)
        : Number(runtimeAutoRefreshInterval.value);
      if (!Number.isFinite(seconds) || seconds < 10) {
        alert(t('customSecondsInvalid'));
        runtimeAutoRefreshCustom.focus();
        return;
      }

      startRuntimeAutoRefresh(Math.floor(seconds));
      closeRuntimeAutoRefreshModal();
      loadRuntimeStats().catch(error => renderRuntimeStatsError(error.message));
    }

    function startRuntimeAutoRefresh(seconds) {
      stopRuntimeAutoRefresh(false);
      runtimeAutoRefreshSeconds = Math.max(10, Math.floor(seconds));
      runtimeAutoRefreshNextAt = Date.now() + runtimeAutoRefreshSeconds * 1000;
      runtimeAutoRefreshTimer = window.setInterval(tickRuntimeAutoRefresh, 1000);
      renderRuntimeAutoRefreshStatus();
    }

    function tickRuntimeAutoRefresh() {
      if (!runtimeAutoRefreshSeconds || !runtimeAutoRefreshNextAt) {
        renderRuntimeAutoRefreshStatus();
        return;
      }

      if (Date.now() >= runtimeAutoRefreshNextAt) {
        runtimeAutoRefreshNextAt = Date.now() + runtimeAutoRefreshSeconds * 1000;
        loadRuntimeStats().catch(error => renderRuntimeStatsError(error.message));
      }

      renderRuntimeAutoRefreshStatus();
    }

    function stopRuntimeAutoRefresh(closeModal = true) {
      if (runtimeAutoRefreshTimer) {
        window.clearInterval(runtimeAutoRefreshTimer);
        runtimeAutoRefreshTimer = null;
      }

      runtimeAutoRefreshSeconds = 0;
      runtimeAutoRefreshNextAt = 0;
      renderRuntimeAutoRefreshStatus();
      if (closeModal) {
        closeRuntimeAutoRefreshModal();
      }
    }

    function renderRuntimeAutoRefreshStatus() {
      if (runtimeAutoRefreshSeconds <= 0 || runtimeAutoRefreshNextAt <= 0) {
        runtimeAutoRefreshStatus.textContent = '';
        syncRuntimeAutoRefreshActions();
        return;
      }

      const seconds = runtimeAutoRefreshSeconds > 0 && runtimeAutoRefreshNextAt > 0
        ? Math.max(0, Math.ceil((runtimeAutoRefreshNextAt - Date.now()) / 1000))
        : '--';
      runtimeAutoRefreshStatus.textContent = t('refreshCountdown', { seconds });
      syncRuntimeAutoRefreshActions();
    }

    function renderResourceSection(title, items) {
      return '<div class="resource-section"><h3>' + escapeHtml(title) + '</h3><div class="runtime-stats-grid">' + items.map(item =>
        '<div class="runtime-stat"><strong>' + escapeHtml(item[0]) + '</strong><span>' + escapeHtml(item[1]) + '</span></div>'
      ).join('') + '</div></div>';
    }

    function renderDashboardResourceSection(title, gauges, items, meta) {
      const gaugeHtml = gauges && gauges.length
        ? '<div class="resource-gauge-row">' + gauges.map(renderGauge).join('') + '</div>'
        : '';
      const titleHtml = '<div class="resource-section-title"><h3>' + escapeHtml(title) + '</h3>' +
        (meta ? '<span class="resource-section-meta">' + escapeHtml(meta) + '</span>' : '') + '</div>';
      const itemsHtml = items && items.length ? '<div class="runtime-stats-grid">' + items.map(item =>
        '<div class="runtime-stat"><strong>' + escapeHtml(item[0]) + '</strong><span>' + escapeHtml(item[1]) + '</span></div>'
      ).join('') + '</div>' : '';
      return '<div class="resource-section">' + titleHtml + gaugeHtml + itemsHtml + '</div>';
    }

    function renderGauge(gauge) {
      const metrics = Array.isArray(gauge.metrics) ? gauge.metrics : [];
      if (metrics.length) {
        return renderSwitchGauge(gauge, metrics);
      }

      return '<div class="resource-gauge">' + renderGaugeFace(gauge) + '</div>';
    }

    function renderSwitchGauge(gauge, metrics) {
      const activeMetric = metrics[0] || gauge;
      const metricButtons = metrics.map((metric, index) =>
        '<button type="button" class="resource-metric-button' + (index === 0 ? ' active' : '') + '"' +
        ' data-label="' + escapeHtml(metric.label || '') + '"' +
        ' data-percent="' + escapeHtml(Number.isFinite(metric.percent) ? String(metric.percent) : '') + '"' +
        ' data-detail="' + escapeHtml(metric.detail || '') + '"' +
        ' data-color="' + escapeHtml(metric.color || gauge.color || '#0f766e') + '"' +
        ' data-visual-floor="' + escapeHtml(Number.isFinite(metric.visualFloorPercent) ? String(metric.visualFloorPercent) : '') + '">' +
        '<strong>' + escapeHtml(metric.label || '') + '</strong>' +
        '<span>' + escapeHtml(metric.detail || '') + '</span>' +
        '</button>'
      ).join('');
      return '<div class="resource-gauge resource-switch-gauge">' +
        '<div class="resource-switch-gauge-main">' + renderGaugeFace(Object.assign({}, gauge, activeMetric)) + '</div>' +
        '<div class="resource-metric-list">' + metricButtons + '</div>' +
        '</div>';
    }

    function renderGaugeFace(gauge) {
      const hasPercent = Number.isFinite(gauge.percent);
      const percent = hasPercent ? Math.max(0, Math.min(100, Number(gauge.percent))) : 0;
      const visualFloor = Number.isFinite(gauge.visualFloorPercent) ? Math.max(0, Number(gauge.visualFloorPercent)) : 0;
      const visualPercent = hasPercent && percent > 0 ? Math.max(percent, visualFloor) : percent;
      const degrees = (visualPercent * 3.6).toFixed(1) + 'deg';
      const value = hasPercent ? percent.toFixed(percent >= 10 ? 1 : 2) + '%' : '-';
      const color = gauge.color || '#0f766e';
      return '<div class="resource-gauge-dial" style="--gauge-deg:' + escapeHtml(degrees) + ';--gauge-color:' + escapeHtml(color) + '">' +
        '<span class="resource-gauge-value">' + escapeHtml(value) + '</span></div>' +
        '<div class="resource-gauge-label">' + escapeHtml(gauge.label || '') + '</div>' +
        '<div class="resource-gauge-detail">' + escapeHtml(gauge.detail || '') + '</div>';
    }

    function getResourceGaugeColor(percent) {
      if (!Number.isFinite(percent) || Number(percent) < 30) return '#2563eb';
      if (Number(percent) < 70) return '#f97316';
      return '#eab308';
    }

    function handleResourceMetricClick(event) {
      const button = event.target.closest('.resource-metric-button');
      if (!button) return;
      const card = button.closest('.resource-switch-gauge');
      const main = card ? card.querySelector('.resource-switch-gauge-main') : null;
      if (!main) return;

      card.querySelectorAll('.resource-metric-button').forEach(item => item.classList.remove('active'));
      button.classList.add('active');
      const percent = button.dataset.percent === '' ? NaN : Number(button.dataset.percent);
      const visualFloor = button.dataset.visualFloor === '' ? NaN : Number(button.dataset.visualFloor);
      main.innerHTML = renderGaugeFace({
        label: button.dataset.label || '',
        percent,
        detail: button.dataset.detail || '',
        color: button.dataset.color || '#0f766e',
        visualFloorPercent: visualFloor
      });
    }

    function renderApplicationRuntimeSection(mods) {
      if (!mods.length) {
        return '<div class="resource-section"><h3>' + escapeHtml(t('modResource')) + '</h3><div class="muted">' + escapeHtml(t('noModResources')) + '</div></div>';
      }

      const ordered = mods.slice().sort((left, right) =>
        String(left.applicationName || left.dynamicAppId || '').localeCompare(String(right.applicationName || right.dynamicAppId || ''), undefined, { sensitivity: 'base' }) ||
        String(left.modId || '').localeCompare(String(right.modId || ''), undefined, { sensitivity: 'base' }) ||
        String(left.version || '').localeCompare(String(right.version || ''), undefined, { sensitivity: 'base' })
      );
      const head = '<div class="resource-runtime-head">' +
        '<span>' + escapeHtml(t('applicationName')) + '</span>' +
        '<span>' + escapeHtml(t('modId')) + '</span>' +
        '<span>' + escapeHtml(t('version')) + '</span>' +
        '<span>' + escapeHtml(t('runtimeType')) + '</span>' +
        '<span>' + escapeHtml(t('statusMessage')) + '</span>' +
        '<span>' + escapeHtml(t('cpuUsage')) + '</span>' +
        '<span>' + escapeHtml(t('memoryUsage')) + '</span>' +
        '</div>';
      return '<div class="resource-section"><div class="resource-section-title"><h3>' + escapeHtml(t('modResource')) + '</h3></div><div class="resource-runtime-list">' + head + ordered.map(mod => {
        const runtimeType = String(mod.runtimeType || '').toLowerCase() === 'container' ? t('runtimeTypeContainer') : t('runtimeTypeMod');
        const statusText = escapeHtml((mod.state || '-') + ' / ' + (mod.healthStatus || '-'));
        const status = mod.isAvailable
          ? statusText
          : statusText + '<div class="danger">' + escapeHtml(t('resourceUnavailable')) + '</div>';
        const cpu = mod.isAvailable && String(mod.runtimeType || '').toLowerCase() !== 'container'
          ? Number(mod.cpuPercent || 0).toFixed(2) + '%'
          : '-';
        const memory = mod.isAvailable && String(mod.runtimeType || '').toLowerCase() !== 'container'
          ? formatBytes(Number(mod.workingSetBytes || 0))
          : '-';
        const message = mod.message ? '<div class="muted">' + escapeHtml(mod.message) + '</div>' : '';
        return '<div class="resource-runtime-row">' +
          '<span><strong class="mono">' + escapeHtml(mod.applicationName || mod.dynamicAppId || '-') + '</strong></span>' +
          '<span class="mono">' + escapeHtml(mod.modId || '-') + '</span>' +
          '<span class="mono">' + escapeHtml(mod.version || '-') + '</span>' +
          '<span>' + escapeHtml(runtimeType) + '</span>' +
          '<span>' + status + message + '</span>' +
          '<span>' + escapeHtml(cpu) + '</span>' +
          '<span>' + escapeHtml(memory) + '</span>' +
          '</div>';
      }).join('') + '</div></div>';
    }

    async function loadPackageApplications() {
      if (!isAuthorized) return;
      packageApplicationList.innerHTML = '<div class="muted">' + escapeHtml(t('loading')) + '</div>';
      const result = await api('/packages/applications');
      cachedPackageApplications = result && result.data ? result.data : [];
      if (!currentPackageApplicationId && cachedPackageApplications.length) {
        currentPackageApplicationId = cachedPackageApplications[0].dynamicAppId || '';
      }
      if (currentPackageApplicationId && !cachedPackageApplications.some(item => sameText(item.dynamicAppId, currentPackageApplicationId))) {
        currentPackageApplicationId = cachedPackageApplications.length ? cachedPackageApplications[0].dynamicAppId || '' : '';
      }
      renderPackageApplications(cachedPackageApplications);
    }

    function renderPackageApplications(applications) {
      if (!packageApplicationList) return;
      if (!applications || !applications.length) {
        packageApplicationList.innerHTML = '<div class="muted">' + escapeHtml(t('noPackageApplications')) + '</div>';
        selectedPackageApplicationTitle.textContent = t('packageInventory');
        document.getElementById('openPackageUpload').disabled = true;
        document.getElementById('openExistingContainer').disabled = true;
        packageManagerList.innerHTML = '<div class="muted">' + escapeHtml(t('selectApplicationFirst')) + '</div>';
        return;
      }

      document.getElementById('openPackageUpload').disabled = false;
      document.getElementById('openExistingContainer').disabled = false;
      packageApplicationList.innerHTML = applications.slice()
        .sort((left, right) => String(left.dynamicAppId || '').localeCompare(String(right.dynamicAppId || ''), undefined, { sensitivity: 'base' }))
        .map(item => {
          const active = sameText(item.dynamicAppId, currentPackageApplicationId) ? ' active' : '';
          return '<button class="application-item' + active + '" data-action="select-package-application" data-app="' + escapeHtml(item.dynamicAppId || '') + '">' +
            '<strong>' + escapeHtml(item.dynamicAppId || '') + '</strong>' +
            '<span class="muted small">' + escapeHtml((item.modCount || 0) + ' ' + t('mods') + ' · ' + (item.versionCount || 0) + ' ' + t('version')) + '</span>' +
            '</button>';
        }).join('');
      selectedPackageApplicationTitle.textContent = currentPackageApplicationId
        ? t('selectedApplication') + ' - ' + currentPackageApplicationId
        : t('packageInventory');
    }

    function renderPackageApplicationsError(message) {
      packageApplicationList.innerHTML = '<div class="muted">' + escapeHtml(t('loadFailed', { message })) + '</div>';
    }

    async function createPackageApplication() {
      const dynamicAppId = prompt(t('createApplicationPrompt'), currentPackageApplicationId || 'security-platform');
      if (!dynamicAppId) return;
      const normalized = dynamicAppId.trim();
      if (!normalized) return;
      const result = await api('/packages/applications', jsonPost({ dynamicAppId: normalized }));
      currentPackageApplicationId = result && result.data ? result.data.dynamicAppId : normalized;
      await loadPackageApplications();
      await loadPackages();
    }

    async function loadPackages() {
      if (!isAuthorized) return;
      packageManagerList.innerHTML = '<div class="muted">' + escapeHtml(t('loading')) + '</div>';
      const result = await api('/packages');
      cachedPackages = result && result.data ? result.data : [];
      renderPackages(cachedPackages);
    }

    function renderPackages(packages) {
      if (!packageManagerList) return;
      if (!currentPackageApplicationId) {
        packageManagerList.innerHTML = '<div class="muted">' + escapeHtml(t('selectApplicationFirst')) + '</div>';
        return;
      }

      const visiblePackages = (packages || []).filter(item => sameText(item.dynamicAppId, currentPackageApplicationId));
      if (!visiblePackages.length) {
        packageManagerList.innerHTML = '<div class="muted">' + escapeHtml(t('noPackages')) + '</div>';
        return;
      }

      packageManagerList.innerHTML = visiblePackages.map(item => {
        const versions = sortPackageVersions(item.versions || []);
        const current = versions[0];
        const history = versions.slice(1);
        const isExistingContainer = isExistingContainerPackageVersion(current);
        const payload = current ? encodeURIComponent(JSON.stringify({
          dynamicAppId: current.dynamicAppId || item.dynamicAppId || '',
          modId: current.modId || item.modId || '',
          version: current.version || ''
        })) : '';
        const actions = current && !isExistingContainer
          ? '<div class="toolbar"><button class="primary" data-action="package-update" data-key="' + payload + '"><span aria-hidden="true">⤴</span> ' + escapeHtml(t('updatePackage')) + '</button>' +
            (history.length ? '<button class="warning" data-action="package-history" data-key="' + payload + '"><span aria-hidden="true">⚠</span> ' + escapeHtml(t('viewHistory')) + ' (' + history.length + ')</button>' : '') +
            '</div>'
          : current
            ? '<div class="toolbar"><button data-action="package-detail" data-key="' + payload + '">' + escapeHtml(t('details')) + '</button></div>'
            : '';
        const modeBadge = current && current.executionMode
          ? ' <span class="badge">' + escapeHtml(current.executionMode) + '</span>'
          : '';
        return '<div class="card">' +
          '<div class="row"><div><h3>' + escapeHtml(item.dynamicAppId || '') + ' / ' + escapeHtml(item.modId || '') + '</h3>' +
          '<div class="muted">' + escapeHtml(t('packageInventory')) + ' · ' + versions.length + ' ' + escapeHtml(t('version')) + modeBadge + '</div></div>' +
          actions + '</div>' +
          (current ? renderPackageVersion(item, current, { isCurrent: true }) : '') +
          '</div>';
      }).join('');
    }

    function renderPackageVersion(item, version, options = {}) {
      const files = (version.files || []);
      const payload = encodeURIComponent(JSON.stringify({
        dynamicAppId: version.dynamicAppId || item.dynamicAppId || '',
        modId: version.modId || item.modId || '',
        version: version.version || ''
      }));
      const modeBadge = version.executionMode
        ? ' <span class="badge">' + escapeHtml(version.executionMode) + '</span>'
        : '';
      const manifestLine = isExistingContainerPackageVersion(version)
        ? '<div class="muted small">' + escapeHtml(t('containerName')) + ': ' + escapeHtml(version.containerName || '-') + ' · ' + escapeHtml(t('endpoint')) + ': ' + escapeHtml(version.endpoint || '-') + '</div>'
        : '';
      return '<div class="package-version-row">' +
        '<div><div><span class="mono">' + escapeHtml(version.version || '') + '</span> <span class="badge">' + escapeHtml(options.isHistory ? t('historyVersions') : t('currentVersion')) + '</span> <span class="badge">' + escapeHtml(formatBytes(version.packageBytes || 0)) + '</span>' + modeBadge + '</div>' +
        '<div class="muted small">' + escapeHtml(t('packageFiles')) + ': ' + files.length + ' · ' + escapeHtml(t('lastModified')) + ': ' + escapeHtml(formatDateTime(version.savedAtUtc)) + '</div>' +
        manifestLine + '</div>' +
        '<div class="toolbar"><button data-action="package-detail" data-key="' + payload + '">' + escapeHtml(t('details')) + '</button></div>' +
        '</div>';
    }

    function sortPackageVersions(versions) {
      return (versions || []).slice().sort((left, right) => {
        const leftTime = Date.parse(left.savedAtUtc || '');
        const rightTime = Date.parse(right.savedAtUtc || '');
        if (!Number.isNaN(leftTime) || !Number.isNaN(rightTime)) {
          const diff = (Number.isNaN(rightTime) ? 0 : rightTime) - (Number.isNaN(leftTime) ? 0 : leftTime);
          if (diff !== 0) return diff;
        }

        return String(right.version || '').localeCompare(String(left.version || ''), undefined, { sensitivity: 'base', numeric: true });
      });
    }

    function isExistingContainerPackageVersion(version) {
      return String((version && version.executionMode) || '').toLowerCase() === 'existingcontainer';
    }

    function showPackageHistory(key) {
      const item = (cachedPackages || []).find(packageItem => sameText(packageItem.dynamicAppId, key.dynamicAppId) && sameText(packageItem.modId, key.modId));
      if (!item) return;
      const versions = sortPackageVersions(item.versions || []);
      const history = versions.filter(version => !sameText(version.version, key.version));
      historyTitle.textContent = t('historyVersions') + ' - ' + key.dynamicAppId + '/' + key.modId;
      historyContent.innerHTML = history.length
        ? history.map(version => renderPackageVersion(item, version, { isHistory: true })).join('')
        : '<div class="muted">' + escapeHtml(t('noHistory')) + '</div>';
      historyModal.hidden = false;
    }

    function renderPackageFile(file) {
      return '<div class="file-row">' +
        '<span class="mono" title="' + escapeHtml(file.fullPath || '') + '">' + escapeHtml(file.relativePath || file.name || '') + '</span>' +
        '<div class="file-row-meta"><span class="badge">' + escapeHtml(file.kind || '') + '</span>' +
        '<span class="muted">' + escapeHtml(formatBytes(file.length || 0)) + ' · ' + escapeHtml(formatDateTime(file.lastModifiedUtc)) + '</span></div>' +
        '</div>';
    }

    function showPackageDetails(key) {
      const version = findPackageVersion(key);
      if (!version) return;
      const files = (version.files || []).slice().sort((left, right) => String(left.relativePath || '').localeCompare(String(right.relativePath || ''), undefined, { sensitivity: 'base' }));
      packageDetailTitle.textContent = t('packageDetails') + ' - ' + key.dynamicAppId + '/' + key.modId + '/' + key.version;
      packageDetailContent.innerHTML = '<div class="card">' +
        '<div class="package-detail-summary"><h3>' + escapeHtml(key.dynamicAppId + ' / ' + key.modId) + '</h3>' +
        '<div class="package-detail-summary-item"><span class="muted">' + escapeHtml(t('version')) + '</span><span class="mono">' + escapeHtml(key.version) + '</span></div>' +
        '<div class="package-detail-summary-item"><span class="muted">' + escapeHtml(t('executionMode')) + '</span><span class="badge">' + escapeHtml(version.executionMode || '-') + '</span></div>' +
        (version.containerName ? '<div class="package-detail-summary-item"><span class="muted">' + escapeHtml(t('containerName')) + '</span><span class="mono">' + escapeHtml(version.containerName) + '</span></div>' : '') +
        (version.endpoint ? '<div class="package-detail-summary-item"><span class="muted">' + escapeHtml(t('endpoint')) + '</span><span class="mono">' + escapeHtml(version.endpoint) + '</span></div>' : '') +
        '<div class="package-detail-summary-item"><span class="muted">' + escapeHtml(t('size')) + '</span><span class="badge">' + escapeHtml(formatBytes(version.packageBytes || 0)) + '</span></div>' +
        '<div class="package-detail-summary-item"><span class="muted">' + escapeHtml(t('lastModified')) + '</span><span>' + escapeHtml(formatDateTime(version.savedAtUtc)) + '</span></div></div>' +
        '<div class="file-list">' + (files.length ? files.map(renderPackageFile).join('') : '<div class="muted">' + escapeHtml(t('noPackageFiles')) + '</div>') + '</div>' +
        '</div>';
      packageDetailModal.hidden = false;
    }

    function closePackageDetailModal() {
      packageDetailModal.hidden = true;
      packageDetailContent.innerHTML = '';
    }

    function openPackageUploadModal(key) {
      const dynamicAppId = key ? key.dynamicAppId : currentPackageApplicationId;
      if (!dynamicAppId) {
        alert(t('selectApplicationFirst'));
        return;
      }

      packageUploadMode = key ? 'update' : 'create';
      resetPackageUploadForm(dynamicAppId);
      packageUploadSourceKey = key || null;
      packageUploadTitle.textContent = key
        ? t('updatePackage') + ' - ' + key.dynamicAppId + '/' + key.modId + '/' + key.version
        : t('uploadPackage') + ' - ' + dynamicAppId;
      if (key) {
        const version = findPackageVersion(key);
        document.getElementById('uploadMod').value = key.modId || '';
        document.getElementById('uploadVersion').value = '';
        document.getElementById('uploadRoute').value = '/mods/' + (key.modId || '');
        document.getElementById('uploadEntry').value = inferPackageEntryFile(version);
        document.getElementById('uploadHealth').value = '/Health';
        document.getElementById('uploadTimeout').value = '60';
        document.getElementById('uploadOverwrite').value = 'true';
        document.getElementById('uploadExecutionMode').value = 'Process';
      }
      setPackageUploadMetadataLocked(packageUploadMode === 'update');
      renderUploadMetadataSummary();
      syncPackageUploadActionText();
      document.getElementById('uploadPackage').value = '';
      refreshUploadSummary().catch(error => logResponse({ error: error.message }));
      packageUploadModal.hidden = false;
    }

    function closePackageUploadModal() {
      packageUploadModal.hidden = true;
      submitUploadButton.disabled = false;
    }

    async function openExistingContainerModal() {
      if (!currentPackageApplicationId) {
        alert(t('selectApplicationFirst'));
        return;
      }

      const appId = currentPackageApplicationId;
      document.getElementById('existingContainerApp').value = appId;
      document.getElementById('existingContainerAppLabel').textContent = appId;
      existingContainerTitle.textContent = t('attachExistingContainer') + ' - ' + appId;
      existingContainerNameInput.innerHTML = '<option value="">' + escapeHtml(t('loading')) + '</option>';
      existingContainerStatus.textContent = t('loading');
      existingContainerModal.hidden = false;
      await loadExistingContainers();
      syncExistingContainerSelection();
    }

    function closeExistingContainerModal() {
      existingContainerModal.hidden = true;
      submitExistingContainerButton.disabled = false;
      submitExistingContainerButton.textContent = t('attachExistingContainer');
    }

    async function loadExistingContainers() {
      try {
        const result = await api('/containers');
        cachedExistingContainers = result && result.data ? result.data : [];
        renderExistingContainerOptions();
      } catch (error) {
        cachedExistingContainers = [];
        existingContainerNameInput.innerHTML = '<option value="">' + escapeHtml(t('loadFailed', { message: error.message })) + '</option>';
        existingContainerStatus.textContent = error.message;
      }
    }

    function renderExistingContainerOptions() {
      if (!cachedExistingContainers.length) {
        existingContainerNameInput.innerHTML = '<option value="">' + escapeHtml(t('noExistingContainers')) + '</option>';
        existingContainerStatus.textContent = t('noExistingContainers');
        return;
      }

      existingContainerNameInput.innerHTML = cachedExistingContainers.map(container => {
        const label = (container.name || '') + ' | ' + (container.state || '-') + ' | ' + (container.status || '-') + (container.dockerHealthStatus ? ' | ' + container.dockerHealthStatus : '');
        return '<option value="' + escapeHtml(container.name || '') + '">' + escapeHtml(label) + '</option>';
      }).join('');
    }

    function syncExistingContainerSelection() {
      const container = cachedExistingContainers.find(item => sameText(item.name, existingContainerNameInput.value));
      if (!container) {
        existingContainerStatus.textContent = t('containerStatus') + ': -';
        return;
      }

      const modId = inferModIdFromContainerName(container.name);
      document.getElementById('existingContainerMod').value = modId;
      document.getElementById('existingContainerRoute').value = '/mods/' + modId;
      document.getElementById('existingContainerEndpoint').value = 'http://' + container.name + ':8080';
      existingContainerStatus.textContent = t('containerStatus') + ': ' + (container.state || '-') + ' / ' + (container.status || '-') + (container.dockerHealthStatus ? ' / ' + container.dockerHealthStatus : '');
    }

    function inferModIdFromContainerName(name) {
      return String(name || '').replace(/^air[._-]/i, '') || 'mod';
    }

    function resetPackageUploadForm(dynamicAppId) {
      ['uploadMod', 'uploadVersion', 'uploadRoute', 'uploadEntry', 'uploadArguments', 'uploadEndpoint', 'uploadHealth', 'uploadTimeout'].forEach(id => {
        document.getElementById(id).value = '';
      });
      document.getElementById('uploadApp').value = dynamicAppId || '';
      document.getElementById('uploadExecutionMode').value = 'Process';
      packageUploadSourceKey = null;
      document.getElementById('uploadDeploy').value = 'false';
      document.getElementById('uploadOverwrite').value = 'false';
      uploadMetadataSummary.hidden = true;
      uploadMetadataSummary.innerHTML = '';
      uploadResultLog.textContent = '';
      uploadResultLog.hidden = true;
    }

    function setPackageUploadMetadataLocked(locked) {
      ['uploadMod', 'uploadRoute', 'uploadEntry', 'uploadArguments', 'uploadEndpoint', 'uploadHealth', 'uploadTimeout'].forEach(id => {
        document.getElementById(id).readOnly = locked;
      });
      document.querySelectorAll('#packageUploadModal .upload-metadata-field').forEach(node => {
        node.hidden = locked;
      });
      document.querySelectorAll('#packageUploadModal .upload-mode-field').forEach(node => {
        node.hidden = locked;
      });
    }

    function syncPackageUploadActionText() {
      submitUploadButton.textContent = packageUploadMode === 'update' ? t('updateAction') : t('uploadAction');
    }

    function renderUploadMetadataSummary() {
      if (packageUploadMode !== 'update') {
        uploadMetadataSummary.hidden = true;
        uploadMetadataSummary.innerHTML = '';
        return;
      }

      const fields = [
        ['modId', 'uploadMod'],
        ['executionMode', 'uploadExecutionMode'],
        ['currentVersion', null, packageUploadSourceKey ? packageUploadSourceKey.version : ''],
        ['routePrefix', 'uploadRoute'],
        ['entryFile', 'uploadEntry'],
        ['arguments', 'uploadArguments'],
        ['endpoint', 'uploadEndpoint'],
        ['healthPath', 'uploadHealth'],
        ['startupTimeout', 'uploadTimeout']
      ];
      uploadMetadataSummary.innerHTML = fields.map(field => {
        const value = field[2] !== undefined ? field[2] || '-' : valueOf(field[1]) || '-';
        return '<div class="upload-metadata-item"><strong>' + escapeHtml(t(field[0])) + '</strong><span class="mono">' + escapeHtml(value) + '</span></div>';
      }).join('');
      uploadMetadataSummary.hidden = false;
    }

    function inferPackageEntryFile(version) {
      const files = version && version.files ? Array.from(version.files) : [];
      const entry = files.find(file => sameText(file.kind, 'Content') && String(file.name || '').toLowerCase().endsWith('.dll'))
        || files.find(file => String(file.relativePath || '').toLowerCase().includes('/content/') && String(file.name || '').toLowerCase().endsWith('.dll'))
        || files.find(file => String(file.relativePath || '').toLowerCase().includes('\\content\\') && String(file.name || '').toLowerCase().endsWith('.dll'));
      if (!entry) return '';

      const relativePath = String(entry.relativePath || entry.name || '').replace(/\\/g, '/');
      const marker = '/content/';
      const markerIndex = relativePath.toLowerCase().indexOf(marker);
      return markerIndex >= 0 ? relativePath.slice(markerIndex + marker.length) : String(entry.name || '');
    }

    function findPackageVersion(key) {
      for (const item of cachedPackages || []) {
        for (const version of item.versions || []) {
          if (String(version.dynamicAppId || item.dynamicAppId).toLowerCase() === String(key.dynamicAppId).toLowerCase()
            && String(version.modId || item.modId).toLowerCase() === String(key.modId).toLowerCase()
            && String(version.version).toLowerCase() === String(key.version).toLowerCase()) {
            return version;
          }
        }
      }
      return null;
    }

    function renderPackagesError(message) {
      packageManagerList.innerHTML = '<div class="muted">' + escapeHtml(t('loadFailed', { message })) + '</div>';
    }

    async function loadArchives() {
      if (!isAuthorized) return;
      archiveList.innerHTML = '<div class="muted">' + escapeHtml(t('loading')) + '</div>';
      const result = await api('/archived');
      cachedArchives = result && result.data ? result.data : [];
      renderArchives(cachedArchives);
    }

    function renderArchives(archives) {
      if (!archiveList) return;
      if (!archives || !archives.length) {
        archiveList.innerHTML = '<div class="muted">' + escapeHtml(t('noArchives')) + '</div>';
        return;
      }

      archiveList.innerHTML = archives.slice()
        .sort((left, right) => (Date.parse(right.archivedAtUtc || '') || 0) - (Date.parse(left.archivedAtUtc || '') || 0))
        .map(record => {
          const key = normalizeArchiveKey(record.key || {});
          const encoded = encodeURIComponent(JSON.stringify(key));
          return '<div class="card">' +
            '<div class="row"><div><h3>' + escapeHtml(key.dynamicAppId + ' / ' + key.modId) + '</h3>' +
            '<div><span class="mono">' + escapeHtml(key.version) + '</span> <span class="badge bad">' + escapeHtml(t('archive')) + '</span></div>' +
            '<div class="muted">' + escapeHtml(t('archiveReason')) + ': ' + escapeHtml(record.reason || '') + '</div>' +
            '<div class="muted">' + escapeHtml(t('archivedAt')) + ': ' + escapeHtml(formatDateTime(record.archivedAtUtc)) + '</div></div>' +
            '<div class="toolbar"><button class="close-action" data-action="unarchive" data-key="' + encoded + '"><span aria-hidden="true">↩</span> ' + escapeHtml(t('unarchive')) + '</button></div></div>' +
            '</div>';
        }).join('');
    }

    function renderArchivesError(message) {
      archiveList.innerHTML = '<div class="muted">' + escapeHtml(t('loadFailed', { message })) + '</div>';
    }

    function normalizeArchiveKey(key) {
      return {
        dynamicAppId: keyPart(key.dynamicAppId),
        modId: keyPart(key.modId),
        version: keyPart(key.version)
      };
    }

    function keyPart(value) {
      if (value && typeof value === 'object' && value.value !== undefined) return value.value;
      return value || '';
    }

    function renderApplicationResult(result) {
      const apps = result && result.data ? result.data : [];
      cachedApps = apps;
      appsNode.dataset.status = apps.length ? 'loaded' : 'empty';
      appsNode.dataset.message = '';
      summaryNode.dataset.count = String(apps.length);
      summaryNode.textContent = t('applicationCount', { count: apps.length });
      document.getElementById('navAppCount').textContent = '(' + apps.length + ')';
      renderApplications(apps);
    }

    function renderApplications(apps) {
      if (!apps.length) {
        appsNode.innerHTML = '<div class="muted">' + escapeHtml(t('noApplications')) + '</div>';
        return;
      }
      appsNode.innerHTML = apps.map(app => {
        const groups = groupMods(app);
        const currentMods = groups.map(group => group.current);
        const historyCount = groups.reduce((count, group) => count + group.history.length, 0);
        return '<div class="card">' +
          '<div class="row"><div><h3>' + escapeHtml(app.dynamicAppId || '') + '</h3><div class="muted">' + escapeHtml(t('latest')) + ' ' + escapeHtml(app.latestVersion || '') + '</div></div>' +
          '<div><span class="badge ok">' + (app.activeModCount || 0) + ' ' + escapeHtml(t('active')) + '</span> <span class="badge">' + currentMods.length + ' ' + escapeHtml(t('mods')) + '</span> <span class="badge">' + escapeHtml(t('historyCount', { count: historyCount })) + '</span></div></div>' +
          currentMods.map(mod => renderMod(mod, { isCurrent: true, history: findHistory(app, mod) })).join('') +
          '</div>';
      }).join('');
    }

    function groupMods(app) {
      const map = new Map();
      for (const mod of (app.mods || [])) {
        const key = String(mod.modId || '').toLowerCase();
        if (!map.has(key)) map.set(key, []);
        map.get(key).push(mod);
      }

      return Array.from(map.values()).map(items => {
        const sorted = items.slice().sort((left, right) => compareCurrentVersionPriority(right) - compareCurrentVersionPriority(left));
        return { current: sorted[0], history: sorted.slice(1) };
      }).sort((left, right) => String(left.current.modId || '').localeCompare(String(right.current.modId || ''), undefined, { sensitivity: 'base' }));
    }

    function compareCurrentVersionPriority(mod) {
      const state = normalizeState(mod.state);
      const health = String(mod.healthStatus || '').toLowerCase();
      const runtimeAt = Date.parse(mod.updatedAtUtc || '') || 0;
      const manifestAt = Date.parse(mod.manifestSavedAtUtc || '') || 0;
      let score = currentVersionRank[state] || 0;
      if (state === 'active' && health === 'healthy') score += 200;
      return score * 10000000000 + Math.floor(Math.max(runtimeAt, manifestAt) / 1000);
    }

    function findHistory(app, current) {
      const group = groupMods(app).find(item => sameModIdentity(item.current, current));
      return group ? sortHistoryVersions(group.history) : [];
    }

    function sortHistoryVersions(items) {
      return items.slice().sort((left, right) => compareHistoryVersionPriority(right) - compareHistoryVersionPriority(left));
    }

    function compareHistoryVersionPriority(mod) {
      const manifestAt = Date.parse(mod.manifestSavedAtUtc || '') || 0;
      const runtimeAt = Date.parse(mod.updatedAtUtc || '') || 0;
      return Math.floor(Math.max(manifestAt, runtimeAt) / 1000);
    }

    function sameModIdentity(left, right) {
      return String(left.dynamicAppId).toLowerCase() === String(right.dynamicAppId).toLowerCase()
        && String(left.modId).toLowerCase() === String(right.modId).toLowerCase()
        && String(left.version).toLowerCase() === String(right.version).toLowerCase();
    }

    function renderMod(mod, options = {}) {
      const state = mod.state || 'None';
      const health = mod.healthStatus || 'None';
      const badge = health === 'Healthy' ? 'ok' : (health === 'Unhealthy' || state === 'Failed' ? 'bad' : 'warn');
      const healthLine = isExistingContainerMode(mod)
        ? ''
        : '<div class="muted">' + escapeHtml(t('health')) + ' ' + escapeHtml(mod.healthPath || '') + '</div>';
      const key = encodeURIComponent(JSON.stringify({
        dynamicAppId: mod.dynamicAppId,
        modId: mod.modId,
        version: mod.version
      }));
      const history = options.history || [];
      const historyButton = options.isCurrent && history.length && !isExistingContainerMode(mod)
        ? '<button data-action="history" data-key="' + key + '">' + escapeHtml(t('viewHistory')) + ' (' + history.length + ')</button>'
        : '';
      return '<div class="row mod-row">' +
        '<div><div><strong>' + escapeHtml(mod.modId || '') + '</strong> <span class="mono">' + escapeHtml(mod.version || '') + '</span> <span class="badge">' + escapeHtml(options.isCurrent ? t('currentVersion') : t('historyVersions')) + '</span></div>' +
        '<div class="muted">' + escapeHtml(mod.routePrefix || '') + ' -> ' + escapeHtml(mod.baseAddress || '') + '</div>' +
        healthLine +
        '<div class="muted">' + escapeHtml(mod.lastStatusMessage || '') + '</div>' +
        renderSelfHealing(mod.selfHealing) + '</div>' +
        '<div class="toolbar">' +
        '<span class="badge ' + badge + '">' + escapeHtml(state) + ' / ' + escapeHtml(health) + '</span>' +
        renderActionButtons(key, mod, options) +
        historyButton +
        '</div></div>';
    }

    function renderActionButtons(key, mod, options) {
      const state = normalizeState(mod.state);
      const health = String(mod.healthStatus || '').toLowerCase();
      const isExistingContainer = isExistingContainerMode(mod);
      const routeAttrs = ' data-route="' + escapeHtml(mod.routePrefix || '') + '" data-health="' + escapeHtml(mod.healthPath || '') + '"';
      const buttons = [];
      if (!['active', 'starting', 'draining'].includes(state)) {
        buttons.push('<button data-action="start" data-key="' + key + '">' + escapeHtml(t('start')) + '</button>');
      }
      if (['active', 'starting'].includes(state)) {
        buttons.push('<button data-action="stop" data-key="' + key + '" class="danger">' + escapeHtml(t('stop')) + '</button>');
      }
      if (state === 'active') {
        buttons.push('<button data-action="restart" data-key="' + key + '">' + escapeHtml(t('restart')) + '</button>');
      }
      if (!options.isCurrent) {
        buttons.push('<button data-action="activate" data-key="' + key + '">' + escapeHtml(t('activate')) + '</button>');
      }
      if (options.isCurrent && (options.history || []).length && !isExistingContainerMode(mod)) {
        buttons.push('<button data-action="rollback" data-key="' + key + '">' + escapeHtml(t('rollback')) + '</button>');
      }
      if (mod.selfHealing) {
        buttons.push('<button data-action="reset-healing" data-key="' + key + '">' + escapeHtml(t('resetSelfHealing')) + '</button>');
      }
      if (mod.hasManifest && (state === 'failed' || health === 'unhealthy' || !options.isCurrent)) {
        buttons.push('<button data-action="archive" data-key="' + key + '" class="danger">' + escapeHtml(t('archive')) + '</button>');
      }
      if (!isExistingContainer) {
        buttons.push('<button data-action="health" data-key="' + key + '">' + escapeHtml(t('health')) + '</button>');
        buttons.push('<button data-action="route-test"' + routeAttrs + '>' + escapeHtml(t('routeTest')) + '</button>');
      }
      buttons.push('<button data-action="details" data-key="' + key + '">' + escapeHtml(t('details')) + '</button>');
      return buttons.join('');
    }

    function renderSelfHealing(selfHealing) {
      if (!selfHealing) return '';
      const parts = [
        t('selfHealing') + ': ' + t('attempts') + ' ' + (selfHealing.attemptCount || 0) + '/' + (selfHealing.maxAttempts || 0)
      ];
      if (selfHealing.isPaused) parts.push(t('paused'));
      if (selfHealing.nextRetryAtUtc) parts.push(t('nextRetry') + ' ' + formatDateTime(selfHealing.nextRetryAtUtc));
      if (selfHealing.windowResetAtUtc) parts.push(t('windowReset') + ' ' + formatDateTime(selfHealing.windowResetAtUtc));
      if (selfHealing.lastFailureStage) parts.push(selfHealing.lastFailureStage);
      if (selfHealing.lastFailureMessage) parts.push(selfHealing.lastFailureMessage);
      return '<div class="muted small">' + escapeHtml(parts.join(' | ')) + '</div>';
    }

    function normalizeState(value) {
      return String(value || 'None').toLowerCase();
    }

    function isExistingContainerMode(mod) {
      return String((mod && mod.executionMode) || '').toLowerCase() === 'existingcontainer';
    }

    document.addEventListener('click', async event => {
      const button = event.target.closest('button[data-action]');
      if (!button) return;
      const action = button.dataset.action;
      try {
        button.disabled = true;
        if (action === 'history') {
          const key = JSON.parse(decodeURIComponent(button.dataset.key));
          showHistory(key);
        } else if (action === 'route-test') {
          prepareRouteTest(button.dataset.route || '', button.dataset.health || '');
        } else if (action === 'details') {
          const key = JSON.parse(decodeURIComponent(button.dataset.key));
          await showModDetails(key);
        } else if (action === 'rollback') {
          const key = JSON.parse(decodeURIComponent(button.dataset.key));
          prepareRollback(key);
        } else if (action === 'reset-healing') {
          const key = JSON.parse(decodeURIComponent(button.dataset.key));
          prepareResetHealing(key);
        } else if (action === 'archive') {
          const key = JSON.parse(decodeURIComponent(button.dataset.key));
          await executeArchive(key);
        } else if (action === 'unarchive') {
          const key = JSON.parse(decodeURIComponent(button.dataset.key));
          await executeUnarchive(key);
        } else if (action === 'package-detail') {
          const key = JSON.parse(decodeURIComponent(button.dataset.key));
          showPackageDetails(key);
        } else if (action === 'package-history') {
          const key = JSON.parse(decodeURIComponent(button.dataset.key));
          showPackageHistory(key);
        } else if (action === 'package-update') {
          const key = JSON.parse(decodeURIComponent(button.dataset.key));
          openPackageUploadModal(key);
        } else if (action === 'select-package-application') {
          currentPackageApplicationId = button.dataset.app || '';
          renderPackageApplications(cachedPackageApplications);
          renderPackages(cachedPackages);
        } else if (action === 'state') {
          const key = JSON.parse(decodeURIComponent(button.dataset.key));
          const state = prompt(t('lifecycleState'), 'Active');
          if (!state) return;
          const healthStatus = prompt(t('healthStatus'), state === 'Stopped' ? 'Stopped' : 'Healthy');
          if (!healthStatus) return;
          const message = prompt(t('statusMessage'), t('manualStateMessage')) || '';
          if (!confirmDanger(action, key)) return;
          await api('/state', jsonPost(Object.assign({}, key, { state, healthStatus, message })));
          await loadApplications();
        } else {
          const key = JSON.parse(decodeURIComponent(button.dataset.key));
          if (isDangerous(action) && !confirmDanger(action, key)) return;
          if (action === 'health') {
            prepareHealthCheck(key);
          } else if (action === 'start') {
            prepareStart(key);
          } else {
            await api('/' + action, jsonPost(key));
            await loadApplications();
          }
        }
      } finally {
        button.disabled = false;
      }
    });

    function isDangerous(action) {
      return ['stop', 'restart', 'activate'].includes(action);
    }

    function confirmDanger(action, key) {
      return confirm(t('confirmAction', { action: t(action) || action, target: key.dynamicAppId + '/' + key.modId + '/' + key.version }));
    }

    function prepareHealthCheck(key) {
      prepareAccessLog(t('health') + ' - ' + key.dynamicAppId + '/' + key.modId + '/' + key.version, managerPrefix + '/health', Object.assign(jsonPost(key), { lockClose: true, refreshApplications: true }));
    }

    function prepareStart(key) {
      prepareAccessLog(t('start') + ' - ' + key.dynamicAppId + '/' + key.modId + '/' + key.version, managerPrefix + '/start', Object.assign(jsonPost(key), { lockClose: true, refreshApplications: true }));
    }

    function prepareRollback(key) {
      const body = { dynamicAppId: key.dynamicAppId, modId: key.modId };
      prepareAccessLog(t('rollback') + ' - ' + key.dynamicAppId + '/' + key.modId, managerPrefix + '/rollback', Object.assign(jsonPost(body), { lockClose: true, refreshApplications: true }));
    }

    function prepareResetHealing(key) {
      prepareAccessLog(t('resetSelfHealing') + ' - ' + key.dynamicAppId + '/' + key.modId + '/' + key.version, managerPrefix + '/self-healing/reset', Object.assign(jsonPost(key), { lockClose: true, refreshApplications: true }));
    }

    async function executeArchive(key) {
      const reason = prompt(t('archiveReason'), t('archiveDefaultReason'));
      if (reason === null) return;
      if (!confirmDanger('archive', key)) return;
      const body = Object.assign({}, key, { reason });
      const result = await api('/archive', jsonPost(body));
      closeHistory();
      await loadApplications();
      await loadArchives();
      const message = result && result.message ? result.message : t('archiveCompleted');
      alert(message);
    }

    async function executeUnarchive(key) {
      if (!confirmDanger('unarchive', key)) return;
      await api('/unarchive', jsonPost(key));
      await loadArchives();
      await loadApplications();
    }

    function prepareRouteTest(routePrefix, healthPath) {
      const route = normalizePath(routePrefix) + normalizePath(healthPath || '/Health');
      prepareAccessLog(t('routeTest') + ' - ' + route, route, { method: 'GET', headers: {} });
    }

    function prepareAccessLog(title, url, options) {
      const preview = buildAccessLogRequest(url, options);
      pendingAccessLog = { title, url, options };
      showAccessLogModal(title, false);
      accessStatus.textContent = t('pendingExecute');
      accessDuration.textContent = '-';
      accessCompletedAt.textContent = '-';
      accessRequestLog.textContent = JSON.stringify(preview, null, 2);
      accessResponseLog.textContent = t('pendingExecute');
      accessBodyLog.textContent = '';
    }

    async function runPendingAccessLog() {
      if (!pendingAccessLog || runAccessLogButton.disabled) return;
      const request = pendingAccessLog;
      isAccessLogRunning = true;
      updateAccessLogControls();
      try {
        await executeAccessLog(request.title, request.url, request.options);
        if (request.options && request.options.refreshApplications === true) {
          await loadApplications();
        }
      } finally {
        isAccessLogRunning = false;
        updateAccessLogControls();
      }
    }

    async function executeAccessLog(title, url, options) {
      const timeoutMs = options.timeoutMs || 15000;
      const lockClose = options.lockClose === true;
      const controller = new AbortController();
      const request = Object.assign(buildAccessLogRequest(url, options), { signal: controller.signal });
      const startedAt = new Date();
      const timeoutId = setTimeout(() => controller.abort(), timeoutMs);
      showAccessLogModal(title, lockClose);
      accessRequestLog.textContent = JSON.stringify({ url, method: request.method, headers: redactHeaders(request.headers), body: previewBody(request.body), startedAt: startedAt.toISOString(), timeoutMs }, null, 2);
      logRequest({ url, method: request.method, headers: redactHeaders(request.headers), body: previewBody(request.body) });

      let response;
      let text = '';
      let payload = null;
      try {
        response = await fetch(url, request);
        text = await response.text();
        payload = parsePayload(text);

        const endedAt = new Date();
        updateAccessLogSummary(response.status, endedAt.getTime() - startedAt.getTime(), endedAt);
        accessResponseLog.textContent = JSON.stringify({ status: response.status, ok: response.ok, statusText: response.statusText, headers: responseHeaders(response), endedAt: endedAt.toISOString() }, null, 2);
        accessBodyLog.textContent = formatPayload(payload);
        logResponse({ status: response.status, body: payload });
        return payload;
      } catch (error) {
        const endedAt = new Date();
        const status = error && error.name === 'AbortError' ? 'Timeout' : 'NetworkError';
        updateAccessLogSummary(status, endedAt.getTime() - startedAt.getTime(), endedAt);
        accessResponseLog.textContent = JSON.stringify({ status, error: error.message, endedAt: endedAt.toISOString() }, null, 2);
        accessBodyLog.textContent = '';
        logResponse({ error: error.message });
        return { status, error: error.message };
      } finally {
        clearTimeout(timeoutId);
        unlockAccessLogModal();
      }
    }

    function buildAccessLogRequest(url, options) {
      return {
        url,
        method: options.method || 'GET',
        headers: options.headers || headers(options.contentType),
        body: options.body,
        timeoutMs: options.timeoutMs || 15000
      };
    }

    function normalizePath(value) {
      const path = String(value || '').trim();
      if (!path) return '';
      return path.startsWith('/') ? path : '/' + path;
    }

    async function loadTargetDetail(key) {
      const path = '/target/' + encodeURIComponent(key.dynamicAppId) + '/' + encodeURIComponent(key.modId) + '/' + encodeURIComponent(key.version);
      const result = await api(path);
      detailLog.textContent = JSON.stringify(result, null, 2);
      showView('details');
    }

    async function loadManifestDetail(key) {
      const result = await api('/manifests/' + encodeURIComponent(key.dynamicAppId));
      const records = result && result.data ? result.data : [];
      const matched = records.filter(record => {
        const mods = record.manifest && record.manifest.mods ? record.manifest.mods : [];
        return mods.some(mod => String(mod.id).toLowerCase() === String(key.modId).toLowerCase() && String(mod.version).toLowerCase() === String(key.version).toLowerCase());
      });
      detailLog.textContent = JSON.stringify(matched.length ? matched : records, null, 2);
      showView('details');
    }

    function showHistory(key) {
      const app = cachedApps.find(item => String(item.dynamicAppId).toLowerCase() === String(key.dynamicAppId).toLowerCase());
      if (!app) return;
      const group = groupMods(app).find(item => String(item.current.modId).toLowerCase() === String(key.modId).toLowerCase());
      if (group && isExistingContainerMode(group.current)) return;
      const history = group ? sortHistoryVersions(group.history) : [];
      historyTitle.textContent = t('historyVersions') + ' - ' + key.dynamicAppId + '/' + key.modId;
      if (!history.length) {
        historyContent.innerHTML = '<div class="muted">' + escapeHtml(t('noHistory')) + '</div>';
      } else {
        historyContent.innerHTML = history.map(mod => renderMod(mod, { isCurrent: false, history: [] })).join('');
      }
      historyModal.hidden = false;
    }

    function closeHistory() {
      historyModal.hidden = true;
      historyContent.innerHTML = '';
    }

    async function showModDetails(key) {
      detailModalTitle.textContent = t('details') + ' - ' + key.dynamicAppId + '/' + key.modId + '/' + key.version;
      detailTargetLog.textContent = t('loading');
      detailManifestLog.textContent = t('loading');
      detailManagedLogs.textContent = t('loading');
      detailModal.hidden = false;

      const target = await loadTargetSection(key);
      detailTargetLog.textContent = JSON.stringify(target, null, 2);

      const manifest = await loadManifestSection(key);
      detailManifestLog.textContent = JSON.stringify(manifest, null, 2);

      const logs = await loadManagedLogsSection(key);
      detailManagedLogs.textContent = JSON.stringify(logs, null, 2);
    }

    function closeDetailModal() {
      detailModal.hidden = true;
      detailTargetLog.textContent = '';
      detailManifestLog.textContent = '';
      detailManagedLogs.textContent = '';
    }

    function showAccessLogModal(title, lockClose) {
      isAccessLogLocked = lockClose === true;
      updateAccessLogControls();
      accessLogTitle.textContent = title;
      accessStatus.textContent = t('loading');
      accessDuration.textContent = t('loading');
      accessCompletedAt.textContent = t('loading');
      accessRequestLog.textContent = t('loading');
      accessResponseLog.textContent = t('loading');
      accessBodyLog.textContent = t('loading');
      accessLogModal.hidden = false;
    }

    function closeAccessLogModal(force) {
      if (isAccessLogLocked && force !== true) return false;
      unlockAccessLogModal();
      pendingAccessLog = null;
      isAccessLogRunning = false;
      updateAccessLogControls();
      accessLogModal.hidden = true;
      accessStatus.textContent = '-';
      accessDuration.textContent = '-';
      accessCompletedAt.textContent = '-';
      accessRequestLog.textContent = '';
      accessResponseLog.textContent = '';
      accessBodyLog.textContent = '';
      return true;
    }

    function unlockAccessLogModal() {
      isAccessLogLocked = false;
      updateAccessLogControls();
    }

    function updateAccessLogControls() {
      runAccessLogButton.disabled = isAccessLogRunning || !pendingAccessLog;
      closeAccessLogButton.disabled = isAccessLogLocked;
    }

    function updateAccessLogSummary(status, durationMs, endedAt) {
      accessStatus.textContent = String(status);
      accessDuration.textContent = durationMs + ' ms';
      accessCompletedAt.textContent = endedAt.toLocaleString();
    }

    function responseHeaders(response) {
      const result = {};
      response.headers.forEach((value, key) => {
        result[key] = value;
      });
      return result;
    }

    function parsePayload(text) {
      try { return text ? JSON.parse(text) : null; } catch { return text; }
    }

    function formatPayload(payload) {
      if (typeof payload === 'string') return payload;
      return JSON.stringify(payload, null, 2);
    }

    async function loadTargetSection(key) {
      try {
        const path = '/target/' + encodeURIComponent(key.dynamicAppId) + '/' + encodeURIComponent(key.modId) + '/' + encodeURIComponent(key.version);
        return await api(path);
      } catch (error) {
        return { error: error.message };
      }
    }

    async function loadManifestSection(key) {
      try {
        const result = await api('/manifests/' + encodeURIComponent(key.dynamicAppId));
        const records = result && result.data ? result.data : [];
        const matched = records.filter(record => {
          const mods = record.manifest && record.manifest.mods ? record.manifest.mods : [];
          return mods.some(mod => String(mod.id).toLowerCase() === String(key.modId).toLowerCase() && String(mod.version).toLowerCase() === String(key.version).toLowerCase());
        });
        return matched.length ? matched : [];
      } catch (error) {
        return { error: error.message };
      }
    }

    async function loadManagedLogsSection(key) {
      try {
        return await api('/logs', jsonPost(key));
      } catch (error) {
        return { error: error.message };
      }
    }

    function findCachedMod(key) {
      for (const app of cachedApps) {
        for (const mod of (app.mods || [])) {
          if (String(mod.dynamicAppId).toLowerCase() === String(key.dynamicAppId).toLowerCase()
            && String(mod.modId).toLowerCase() === String(key.modId).toLowerCase()
            && String(mod.version).toLowerCase() === String(key.version).toLowerCase()) {
            return mod;
          }
        }
      }
      return null;
    }

    async function executeApiConsole() {
      const value = document.getElementById('apiOperation').value;
      const parts = value.split(' ');
      const method = parts[0];
      const path = parts[1];
      if (method === 'GET') {
        const result = await api(path);
        if (path === '/packages') {
          cachedPackages = result && result.data ? result.data : [];
          renderPackages(cachedPackages);
        } else if (path === '/packages/applications') {
          cachedPackageApplications = result && result.data ? result.data : [];
          renderPackageApplications(cachedPackageApplications);
        } else if (path === '/archived') {
          cachedArchives = result && result.data ? result.data : [];
          renderArchives(cachedArchives);
        }
        return;
      }

      const body = document.getElementById('apiBody').value.trim();
      JSON.parse(body);
      if (['POST /deploy', 'POST /publish', 'POST /activate', 'POST /rollback', 'POST /state', 'POST /archive', 'POST /unarchive', 'POST /self-healing/reset'].includes(value)
        && !confirm(t('confirmExecute', { operation: value }))) {
        return;
      }

      await api(path, { method, contentType: 'application/json', body });
      if (['POST /deploy', 'POST /activate', 'POST /rollback', 'POST /state', 'POST /archive', 'POST /unarchive', 'POST /self-healing/reset'].includes(value)) {
        await loadApplications();
      }
      if (['POST /archive', 'POST /unarchive'].includes(value)) {
        await loadArchives();
      }
      if (value === 'POST /packages/applications') {
        await loadPackageApplications();
      }
    }

    function fillApiConsoleSample() {
      const value = document.getElementById('apiOperation').value;
      if (value === 'POST /publish' || value === 'POST /deploy') {
        document.getElementById('apiBody').value = JSON.stringify(configureToManifest(sampleConfig), null, 2);
      } else if (value === 'POST /rollback') {
        document.getElementById('apiBody').value = JSON.stringify({ dynamicAppId: 'security-platform', modId: 'auth' }, null, 2);
      } else if (value === 'POST /state') {
        document.getElementById('apiBody').value = JSON.stringify({ dynamicAppId: 'security-platform', modId: 'auth', version: 'local-dev', state: 'Active', healthStatus: 'Healthy', message: 'Manual state update from DynamicApp panel.' }, null, 2);
      } else if (value === 'POST /archive') {
        document.getElementById('apiBody').value = JSON.stringify({ dynamicAppId: 'security-platform', modId: 'auth', version: 'local-dev', reason: t('archiveDefaultReason') }, null, 2);
      } else if (value === 'POST /packages/applications') {
        document.getElementById('apiBody').value = JSON.stringify({ dynamicAppId: 'security-platform' }, null, 2);
      } else {
        document.getElementById('apiBody').value = JSON.stringify({ dynamicAppId: 'security-platform', modId: 'auth', version: 'local-dev' }, null, 2);
      }
    }

    function configureToManifest(value) {
      return {
        id: value.dynamicAppId,
        version: value.appVersion || (value.mod ? value.mod.version : 'local-dev'),
        runtime: { profile: 'Compact' },
        endpoints: {
          gatewayPort: value.gatewayPort || 8080,
          managerPort: value.managerPort || 8081,
          managerInternalOnly: value.managerInternalOnly !== false
        },
        mods: value.mod ? [value.mod] : []
      };
    }

    async function submitLocalDebug() {
      const request = {
        dynamicAppId: valueOf('debugApp'),
        appVersion: valueOf('debugVersion'),
        deploy: valueOf('debugDeploy') === 'true',
        gatewayPort: 8080,
        managerPort: 8081,
        managerInternalOnly: true,
        mod: {
          id: valueOf('debugMod'),
          version: valueOf('debugVersion'),
          executionMode: 'LocalDevelopment',
          artifact: { endpoint: valueOf('debugEndpoint') },
          health: { path: valueOf('debugHealth'), startupTimeoutSeconds: Number(valueOf('debugTimeout') || 10) },
          route: { prefix: valueOf('debugRoute') }
        }
      };
      await api('/mods/configure', { method: 'POST', contentType: 'application/json', body: JSON.stringify(request) });
      await loadApplications();
    }

    function resetLocalDebug() {
      document.getElementById('debugApp').value = sampleLocalDebug.dynamicAppId;
      document.getElementById('debugMod').value = sampleLocalDebug.mod.id;
      document.getElementById('debugVersion').value = sampleLocalDebug.mod.version;
      document.getElementById('debugRoute').value = sampleLocalDebug.mod.route.prefix;
      document.getElementById('debugEndpoint').value = sampleLocalDebug.mod.artifact.endpoint;
      document.getElementById('debugHealth').value = sampleLocalDebug.mod.health.path;
      document.getElementById('debugTimeout').value = String(sampleLocalDebug.mod.health.startupTimeoutSeconds);
      document.getElementById('debugDeploy').value = String(sampleLocalDebug.deploy);
    }

    async function submitExistingContainer() {
      submitExistingContainerButton.disabled = true;
      submitExistingContainerButton.textContent = t('loading');
      try {
        const dynamicAppId = valueOf('existingContainerApp') || currentPackageApplicationId;
        const containerName = valueOf('existingContainerName').trim();
        const modId = valueOf('existingContainerMod').trim();
        const version = valueOf('existingContainerVersion').trim();
        const routePrefix = valueOf('existingContainerRoute').trim();
        const endpoint = valueOf('existingContainerEndpoint').trim();
        if (!dynamicAppId) throw new Error(t('selectApplicationFirst'));
        if (!containerName) throw new Error(t('containerRequired'));
        if (!modId) throw new Error(t('modRequired'));
        if (!version) throw new Error(t('versionRequired'));
        if (!routePrefix) throw new Error(t('routeRequired'));
        if (!endpoint) throw new Error(t('endpointRequired'));
        if (!/^https?:\/\//i.test(endpoint)) throw new Error(t('invalidEndpoint'));

        const request = {
          dynamicAppId,
          appVersion: version,
          deploy: false,
          gatewayPort: 8080,
          managerPort: 8081,
          managerInternalOnly: true,
          mod: {
            id: modId,
            version,
            executionMode: 'ExistingContainer',
            artifact: { containerName, endpoint },
            health: { path: valueOf('existingContainerHealth') || '/Health', startupTimeoutSeconds: Number(valueOf('existingContainerTimeout') || 60) },
            route: { prefix: routePrefix }
          }
        };
        const result = await api('/mods/configure', { method: 'POST', contentType: 'application/json', body: JSON.stringify(request) });
        closeExistingContainerModal();
        await loadPackageApplications();
        await loadPackages();
        await loadApplications();
        alert(result && result.message ? result.message : t('existingContainerSucceeded'));
      } catch (error) {
        alert(error.message);
      } finally {
        submitExistingContainerButton.disabled = false;
        submitExistingContainerButton.textContent = t('attachExistingContainer');
      }
    }

    async function submitUpload() {
      uploadResultLog.hidden = true;
      uploadResultLog.textContent = '';
      try {
        const file = document.getElementById('uploadPackage').files[0];
        if (!valueOf('uploadApp')) throw new Error(t('selectApplicationFirst'));
        if (!file) throw new Error(t('packageRequired'));
        if (!file.name.toLowerCase().endsWith('.zip')) throw new Error(t('packageZipRequired'));
        if (!valueOf('uploadVersion').trim()) throw new Error(t('versionRequired'));
        if (packageUploadMode === 'update'
          && packageUploadSourceKey
          && sameText(valueOf('uploadVersion').trim(), packageUploadSourceKey.version)) {
          throw new Error(t('versionMustChange'));
        }
        if (packageUploadMode === 'update' && !confirm(t('confirmUpdatePackage'))) {
          return;
        }

        if (packageUploadMode !== 'update' && valueOf('uploadOverwrite') === 'true' && !confirm(t('confirmOverwrite'))) {
          return;
        }

        submitUploadButton.disabled = true;
        submitUploadButton.textContent = packageUploadMode === 'update' ? t('updateStarted') : t('uploadStarted');
        const form = new FormData();
        form.append('package', file);
        form.append('dynamicAppId', valueOf('uploadApp'));
        form.append('modId', valueOf('uploadMod'));
        form.append('version', valueOf('uploadVersion'));
        if (packageUploadMode !== 'update') {
          form.append('executionMode', valueOf('uploadExecutionMode') || 'Process');
          form.append('routePrefix', valueOf('uploadRoute'));
          form.append('entryFile', valueOf('uploadEntry'));
          form.append('arguments', valueOf('uploadArguments'));
          form.append('endpoint', valueOf('uploadEndpoint'));
          form.append('healthPath', valueOf('uploadHealth'));
          form.append('startupTimeoutSeconds', valueOf('uploadTimeout'));
        }
        form.append('deploy', valueOf('uploadDeploy'));
        form.append('overwrite', valueOf('uploadOverwrite'));
        await api('/mods/upload', { method: 'POST', headers: headers(), body: form });
        await loadPackages();
        await loadApplications();
        alert(packageUploadMode === 'update' ? t('updateSucceeded') : t('uploadSucceeded'));
        closePackageUploadModal();
      } catch (error) {
        uploadResultLog.textContent = '';
        uploadResultLog.hidden = true;
        const payload = formatApiPayload(error.payload);
        alert(t('uploadFailed', { message: error.message }) + (payload ? '\n' + payload : ''));
      } finally {
        submitUploadButton.disabled = false;
        syncPackageUploadActionText();
      }
    }

    async function refreshUploadSummary() {
      const file = document.getElementById('uploadPackage').files[0];
      const node = document.getElementById('uploadSummary');
      if (!file) {
        node.textContent = t('noPackageSelected');
        return;
      }

      const parts = [t('name') + '=' + file.name, t('size') + '=' + formatBytes(file.size)];
      if (file.name.toLowerCase().endsWith('.zip')) {
        parts.push(t('type') + '=zip');
      } else {
        parts.push(t('type') + '=' + t('invalid'));
      }

      if (window.crypto && window.crypto.subtle) {
        const hash = await sha256(file);
        parts.push('sha256=' + hash);
      } else {
        parts.push('sha256=' + t('unavailable'));
      }

      node.textContent = parts.join(' | ');
    }

    async function sha256(file) {
      const buffer = await file.arrayBuffer();
      const hash = await crypto.subtle.digest('SHA-256', buffer);
      return Array.from(new Uint8Array(hash)).map(value => value.toString(16).padStart(2, '0')).join('');
    }

    function formatBytes(value) {
      if (value < 1024) return value + ' B';
      if (value < 1048576) return (value / 1024).toFixed(1) + ' KB';
      return (value / 1048576).toFixed(1) + ' MB';
    }

    function formatPercent(value) {
      return Number.isFinite(value) ? Number(value).toFixed(value >= 10 ? 1 : 2) + '%' : '-';
    }

    function formatDuration(seconds) {
      const value = Math.max(0, Math.floor(seconds || 0));
      const hours = Math.floor(value / 3600);
      const minutes = Math.floor((value % 3600) / 60);
      const rest = value % 60;
      if (hours > 0) return hours + 'h ' + minutes + 'm';
      if (minutes > 0) return minutes + 'm ' + rest + 's';
      return rest + 's';
    }

    function formatDateTime(value) {
      const time = new Date(value);
      return Number.isNaN(time.getTime()) ? String(value || '') : time.toLocaleString();
    }

    function jsonPost(value) {
      return { method: 'POST', contentType: 'application/json', body: JSON.stringify(value) };
    }

    function valueOf(id) {
      return document.getElementById(id).value;
    }

    function sameText(left, right) {
      return String(left || '').toLowerCase() === String(right || '').toLowerCase();
    }

    function logRequest(value) {
      requestLog.textContent = JSON.stringify(value, null, 2);
    }

    function logResponse(value) {
      responseLog.textContent = JSON.stringify(value, null, 2);
    }

    function redactHeaders(value) {
      const copy = Object.assign({}, value || {});
      if (copy[ticketHeader]) copy[ticketHeader] = '***';
      return copy;
    }

    function previewBody(body) {
      if (!body) return null;
      if (body instanceof FormData) return 'multipart/form-data';
      return body.length > 2000 ? body.slice(0, 2000) + '...' : body;
    }

    function escapeHtml(value) {
      return String(value ?? '').replace(/[&<>"']/g, char => ({
        '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'
      }[char]));
    }

    function cssEscape(value) {
      if (window.CSS && CSS.escape) return CSS.escape(value);
      return String(value || '').replace(/["\\]/g, '\\$&');
    }

    window.addEventListener('error', event => {
      logResponse({ error: event.message });
    });

    init();
  </script>
</body>
</html>
""";
    }

    private static string NormalizePrefix(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return "/dynamicapp-panel";
        }

        return "/" + prefix.Trim('/');
    }

    private static string ToJavaScriptString(string value)
    {
        return JsonSerializer.Serialize(value);
    }

    private readonly record struct ResourceCounter(double TotalCpuMilliseconds, DateTimeOffset SampledAtUtc);

    private sealed class RuntimeStatsSnapshot
    {
        public DateTimeOffset TimestampUtc { get; set; }

        public DateTimeOffset CacheExpiresAtUtc { get; set; }

        public double SampleIntervalSeconds { get; set; }

        public int ProcessorCount { get; set; }

        public int ProcessId { get; set; }

        public double CpuPercent { get; set; }

        public long WorkingSetBytes { get; set; }

        public long PrivateMemoryBytes { get; set; }

        public long ManagedMemoryBytes { get; set; }

        public long TotalAvailableMemoryBytes { get; set; }

        public int ThreadCount { get; set; }

        public int HandleCount { get; set; }

        public double UptimeSeconds { get; set; }

        public RuntimeResourceSnapshot HostProcess { get; set; } = new();

        public ContainerResourceSnapshot Container { get; set; } = new();

        public IReadOnlyCollection<RuntimeResourceSnapshot> Mods { get; set; } = Array.Empty<RuntimeResourceSnapshot>();
    }

    private sealed class ContainerResourceSnapshot
    {
        public bool IsAvailable { get; set; }

        public string Message { get; set; } = string.Empty;

        public double CpuPercent { get; set; }

        public long MemoryBytes { get; set; }

        public long? MemoryLimitBytes { get; set; }

        public int ProcessorCount { get; set; }
    }

    private sealed class RuntimeResourceSnapshot
    {
        public string Scope { get; set; } = string.Empty;

        public string Key { get; set; } = string.Empty;

        public string DynamicAppId { get; set; } = string.Empty;

        public string ApplicationName { get; set; } = string.Empty;

        public string ModId { get; set; } = string.Empty;

        public string Version { get; set; } = string.Empty;

        public string ExecutionMode { get; set; } = string.Empty;

        public string RuntimeType { get; set; } = string.Empty;

        public string ContainerName { get; set; } = string.Empty;

        public string State { get; set; } = string.Empty;

        public string HealthStatus { get; set; } = string.Empty;

        public DateTimeOffset CreatedAtUtc { get; set; }

        public DateTimeOffset UpdatedAtUtc { get; set; }

        public bool IsAvailable { get; set; }

        public string Message { get; set; } = string.Empty;

        public int ProcessId { get; set; }

        public double CpuPercent { get; set; }

        public long WorkingSetBytes { get; set; }

        public long PrivateMemoryBytes { get; set; }

        public long ManagedMemoryBytes { get; set; }

        public int ThreadCount { get; set; }

        public int HandleCount { get; set; }

        public double UptimeSeconds { get; set; }

        public static RuntimeResourceSnapshot Unavailable(string scope, string key, DynamicAppRouteTarget target, DynamicModExecutionMode executionMode, string message) => new()
        {
            Scope = scope,
            Key = key,
            DynamicAppId = target.Key.DynamicAppId.Value,
            ApplicationName = target.Key.DynamicAppId.Value,
            ModId = target.Key.ModId.Value,
            Version = target.Key.Version.Value,
            ExecutionMode = executionMode.ToString(),
            RuntimeType = executionMode is DynamicModExecutionMode.Container or DynamicModExecutionMode.ExistingContainer ? "Container" : "Mod",
            ContainerName = executionMode is DynamicModExecutionMode.Container or DynamicModExecutionMode.ExistingContainer ? target.InstanceId : string.Empty,
            State = target.State.ToString(),
            HealthStatus = target.HealthStatus.ToString(),
            CreatedAtUtc = target.CreatedAtUtc,
            UpdatedAtUtc = target.UpdatedAtUtc,
            IsAvailable = false,
            Message = message,
            ProcessId = 0
        };
    }
}

