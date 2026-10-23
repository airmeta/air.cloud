using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Air.Cloud.Capabilities.ControlPlaneAuth.Options;
using Air.Cloud.Capabilities.ControlPlaneAuth.Services;
using Air.Cloud.DynamicApp.Contracts.Lifecycle;
using Air.Cloud.DynamicApp.Contracts.Manifest;
using Air.Cloud.DynamicApp.Contracts.Runtime;
using Air.Cloud.DynamicApp.Manager;
using Air.Cloud.AirForge.Panel.FileEditing;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Air.Cloud.AirForge.Panel;

/// <summary>
/// <para>zh-cn:AirForge 管理面板中间件，提供一个无外部依赖的单页运维界面。</para>
/// <para>en-us:AirForge management panel middleware that serves a dependency-free single-page operations UI.</para>
/// </summary>
public sealed class AirForgePanelMiddleware
{
    private static readonly JsonSerializerOptions RuntimeStatsJsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IControlPlaneAuthService _controlPlaneAuthService;
    private readonly IOptionsMonitor<ControlPlaneAuthOptions> _controlPlaneOptions;
    private readonly AirForgeTextFileEditor _fileEditor;
    private readonly DynamicAppManagerApiOptions _managerOptions;
    private readonly RequestDelegate _next;
    private readonly AirForgePanelOptions _options;
    private readonly Dictionary<string, ResourceCounter> _resourceCounters = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _resourceStatsLock = new();
    private readonly TimeSpan _resourceStatsSampleInterval;
    private RuntimeStatsSnapshot? _cachedRuntimeStats;

    /// <summary>
    /// <para>zh-cn:创建 AirForge 管理面板中间件；页面会根据 Manager API 配置生成默认请求前缀和 Ticket 请求头名称。</para>
    /// <para>en-us:Creates the AirForge management panel middleware; the page uses Manager API options to generate the default request prefix and Ticket header name.</para>
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
    /// <param name="controlPlaneAuthService">
    /// <para>zh-cn:控制面认证服务，用于校验当前管理会话，并在同进程内桥接到 DynamicApp Manager API。</para>
    /// <para>en-us:Control-plane authentication service used to validate the current management session and bridge it to the in-process DynamicApp Manager API.</para>
    /// </param>
    /// <param name="controlPlaneOptions">
    /// <para>zh-cn:控制面认证选项，用于读取登录页路径和会话策略。</para>
    /// <para>en-us:Control-plane authentication options used to read the sign-in path and session policy.</para>
    /// </param>
    public AirForgePanelMiddleware(
        RequestDelegate next,
        AirForgePanelOptions options,
        DynamicAppManagerApiOptions managerOptions,
        IControlPlaneAuthService controlPlaneAuthService,
        IOptionsMonitor<ControlPlaneAuthOptions> controlPlaneOptions,
        AirForgeTextFileEditor fileEditor)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _managerOptions = managerOptions ?? throw new ArgumentNullException(nameof(managerOptions));
        _controlPlaneAuthService = controlPlaneAuthService ?? throw new ArgumentNullException(nameof(controlPlaneAuthService));
        _controlPlaneOptions = controlPlaneOptions ?? throw new ArgumentNullException(nameof(controlPlaneOptions));
        _fileEditor = fileEditor ?? throw new ArgumentNullException(nameof(fileEditor));
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

        if (IsManagerApiPath(context.Request.Path))
        {
            if (await IsControlPlaneAuthorizedAsync(context).ConfigureAwait(false))
            {
                AttachManagerApiTicket(context);
            }

            await _next(context).ConfigureAwait(false);
            return;
        }

        if (IsFileEditingPath(context.Request.Path))
        {
            await HandleFileEditingAsync(context).ConfigureAwait(false);
            return;
        }

        if (IsRuntimeStatsPath(context.Request.Path))
        {
            if (!await IsControlPlaneAuthorizedAsync(context).ConfigureAwait(false) && !IsAuthorized(context))
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
        if (!_options.AllowIframeEmbedding)
        {
            context.Response.Headers.XFrameOptions = "DENY";
        }

        context.Response.Headers.XContentTypeOptions = "nosniff";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
        context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        context.Response.Headers.ContentSecurityPolicy = BuildContentSecurityPolicy();
        await context.Response.WriteAsync(BuildHtml(), Encoding.UTF8, context.RequestAborted).ConfigureAwait(false);
    }

    private string BuildContentSecurityPolicy()
    {
        var frameAncestors = _options.AllowIframeEmbedding
            ? string.IsNullOrWhiteSpace(_options.FrameAncestors) ? "'self'" : _options.FrameAncestors.Trim()
            : "'none'";
        return "default-src 'none'; style-src 'unsafe-inline'; script-src 'unsafe-inline'; connect-src 'self'; img-src 'self' data:; font-src 'self'; base-uri 'none'; frame-ancestors " + frameAncestors + "; form-action 'none'";
    }

    private bool IsPanelPath(PathString path)
    {
        var prefix = NormalizePrefix(_options.RoutePrefix);
        var value = path.Value ?? string.Empty;
        return value.Equals(prefix, StringComparison.OrdinalIgnoreCase)
            || value.Equals(prefix + "/", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsManagerApiPath(PathString path)
    {
        var prefix = NormalizePrefix(_managerOptions.RoutePrefix);
        var value = path.Value ?? string.Empty;
        return value.Equals(prefix, StringComparison.OrdinalIgnoreCase)
            || value.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsRuntimeStatsPath(PathString path)
    {
        var value = path.Value ?? string.Empty;
        return value.Equals(GetRuntimeStatsPath(), StringComparison.OrdinalIgnoreCase);
    }

    private bool IsFileEditingPath(PathString path)
    {
        var value = path.Value ?? string.Empty;
        var prefix = GetFileEditingPath();
        return value.Equals(prefix, StringComparison.OrdinalIgnoreCase);
    }

    private string GetFileEditingPath() => NormalizePrefix(_options.RoutePrefix) + "/files";

    private async Task HandleFileEditingAsync(HttpContext context)
    {
        if (!_options.FileEditingEnabled)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        if (!await IsControlPlaneAuthorizedAsync(context).ConfigureAwait(false) && !IsAuthorized(context))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await WriteFileEditingResponseAsync(context, new
            {
                succeeded = false,
                message = "Unauthorized AirForge file-editing request.",
                errorCode = "airforge.file_editing_unauthorized"
            }).ConfigureAwait(false);
            return;
        }

        try
        {
            if (HttpMethods.IsGet(context.Request.Method))
            {
                var path = context.Request.Query["path"].FirstOrDefault() ?? string.Empty;
                var snapshot = await _fileEditor.ReadAsync(path, context.RequestAborted).ConfigureAwait(false);
                await WriteFileEditingResponseAsync(context, new
                {
                    succeeded = true,
                    message = "AirForge text file loaded.",
                    data = snapshot
                }).ConfigureAwait(false);
                return;
            }

            if (HttpMethods.IsPut(context.Request.Method))
            {
                var request = await JsonSerializer.DeserializeAsync<AirForgeTextFileWriteRequest>(
                    context.Request.Body,
                    RuntimeStatsJsonOptions,
                    context.RequestAborted).ConfigureAwait(false)
                    ?? throw new JsonException("Text file save request cannot be empty.");
                var snapshot = await _fileEditor.WriteAsync(request, context.RequestAborted).ConfigureAwait(false);
                await WriteFileEditingResponseAsync(context, new
                {
                    succeeded = true,
                    message = "AirForge text file saved.",
                    data = snapshot
                }).ConfigureAwait(false);
                return;
            }

            context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
            context.Response.Headers.Allow = "GET, PUT";
        }
        catch (FileNotFoundException exception)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await WriteFileEditingResponseAsync(context, new { succeeded = false, message = exception.Message, errorCode = "airforge.file_not_found" }).ConfigureAwait(false);
        }
        catch (AirForgeTextFileConflictException exception)
        {
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            await WriteFileEditingResponseAsync(context, new { succeeded = false, message = exception.Message, errorCode = "airforge.file_conflict" }).ConfigureAwait(false);
        }
        catch (UnauthorizedAccessException exception)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await WriteFileEditingResponseAsync(context, new { succeeded = false, message = exception.Message, errorCode = "airforge.file_forbidden" }).ConfigureAwait(false);
        }
        catch (NotSupportedException exception)
        {
            context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
            await WriteFileEditingResponseAsync(context, new { succeeded = false, message = exception.Message, errorCode = "airforge.file_type_not_supported" }).ConfigureAwait(false);
        }
        catch (Exception exception)
            when (exception is ArgumentException or InvalidOperationException or JsonException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await WriteFileEditingResponseAsync(context, new { succeeded = false, message = exception.Message, errorCode = "airforge.file_editing_bad_request" }).ConfigureAwait(false);
        }
    }

    private static async Task WriteFileEditingResponseAsync(HttpContext context, object payload)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, RuntimeStatsJsonOptions), Encoding.UTF8, context.RequestAborted).ConfigureAwait(false);
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

    private async Task<bool> IsControlPlaneAuthorizedAsync(HttpContext context)
    {
        var authorized = await _controlPlaneAuthService.AuthorizeAsync(context, context.RequestAborted).ConfigureAwait(false);
        return authorized.Succeeded;
    }

    private void AttachManagerApiTicket(HttpContext context)
    {
        if (!_managerOptions.RequireTicket ||
            string.IsNullOrWhiteSpace(_managerOptions.HeaderName) ||
            _managerOptions.Tickets.Length == 0)
        {
            return;
        }

        context.Request.Headers[_managerOptions.HeaderName] = _managerOptions.Tickets[0];
    }

    private string GetControlPlanePath()
    {
        var options = _controlPlaneOptions.CurrentValue;
        return NormalizePrefix(options.BasePath);
    }

    private string GetControlAuthPathWithReturnUrl()
    {
        var options = _controlPlaneOptions.CurrentValue;
        var planePath = ResolveControlPlaneShellPath(options);
        return ResolveControlAuthPath(options) + "?returnUrl=" + Uri.EscapeDataString(planePath);
    }

    private string GetControlPlaneQueryName()
    {
        var options = _controlPlaneOptions.CurrentValue;
        return string.IsNullOrWhiteSpace(options.Session.QueryName) ? "airControlPlaneTicket" : options.Session.QueryName;
    }

    private string GetControlPlanePathWithReturnUrl(HttpContext context)
    {
        var returnUrl = (context.Request.PathBase + context.Request.Path + context.Request.QueryString).ToString();
        var options = _controlPlaneOptions.CurrentValue;
        return ResolveControlAuthPath(options) + "?returnUrl=" + Uri.EscapeDataString(returnUrl);
    }

    private static string ResolveControlAuthPath(ControlPlaneAuthOptions options)
    {
        var basePath = NormalizePrefix(options.BasePath);
        return string.IsNullOrWhiteSpace(options.AuthPath) ? basePath + "/auth" : NormalizePrefix(options.AuthPath);
    }

    private static string ResolveControlPlaneShellPath(ControlPlaneAuthOptions options)
    {
        return string.IsNullOrWhiteSpace(options.PlanePath) ? NormalizePrefix(options.BasePath) : NormalizePrefix(options.PlanePath);
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
        var title = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(_options.Title) ? "AirForge Manager" : _options.Title);
        var managerPrefix = ToJavaScriptString(NormalizePrefix(_managerOptions.RoutePrefix));
        var runtimeStatsPath = ToJavaScriptString(GetRuntimeStatsPath());
        var fileEditingPath = ToJavaScriptString(GetFileEditingPath());
        var processLogHubPath = ToJavaScriptString(NormalizePrefix(_options.RoutePrefix) + "/hubs/process-logs");
        var controlPlanePath = ToJavaScriptString(GetControlPlanePath());
        var controlAuthPath = ToJavaScriptString(GetControlAuthPathWithReturnUrl());
        var controlPlaneQueryName = ToJavaScriptString(GetControlPlaneQueryName());
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
    body.embedded header { display: none; }
    body.embedded main {
      grid-template-columns: 260px minmax(0, 1fr);
      max-width: none;
      padding: 16px;
    }
    body.embedded .nav-tree {
      top: 16px;
      max-height: calc(100vh - 32px);
    }
    body.embedded .workspace { min-width: 0; }
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
      align-items: start;
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
    .toolbar > h1,
    .toolbar > h2,
    .toolbar > h3 {
      margin: 0;
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
    .service-list {
      display: grid;
      gap: 14px;
    }
    .service-row {
      display: grid;
      gap: 12px;
      padding: 14px;
      border: 1px solid var(--line);
      border-radius: 6px;
      background: #fff;
    }
    .service-row-header {
      display: flex;
      flex-wrap: wrap;
      gap: 10px;
      align-items: center;
      justify-content: space-between;
      min-width: 0;
      padding-bottom: 10px;
      border-bottom: 1px solid #eef2f6;
    }
    .service-row-title {
      display: flex;
      flex-wrap: wrap;
      gap: 8px;
      align-items: center;
      min-width: 0;
    }
    .service-row-title strong {
      overflow-wrap: anywhere;
    }
    .instance-grid {
      display: flex;
      flex-wrap: wrap;
      gap: 10px;
      overflow: visible;
      padding-bottom: 2px;
    }
    .instance-card {
      display: grid;
      gap: 8px;
      flex: 0 0 350px;
      width: 350px;
      height: 125px;
      box-sizing: border-box;
      grid-template-rows: auto minmax(0, 1fr) auto;
      align-content: start;
      padding: 12px;
      border: 1px solid #dfe7ef;
      border-radius: 6px;
      background: #f8fafc;
      overflow: visible;
    }
    .instance-card-header,
    .route-card-header {
      display: flex;
      flex-wrap: wrap;
      gap: 8px;
      align-items: center;
      justify-content: space-between;
      min-width: 0;
    }
    .instance-card-header-actions {
      display: flex;
      flex: 0 0 auto;
      flex-wrap: wrap;
      gap: 8px;
      align-items: center;
      justify-content: flex-end;
      margin-left: auto;
    }
    .instance-card-header-actions .instance-deployment-badge {
      margin-right: 0;
    }
    .instance-card-title {
      display: flex;
      flex-wrap: wrap;
      gap: 6px;
      align-items: center;
      min-width: 0;
    }
    .instance-card-title strong,
    .instance-card-title span {
      overflow-wrap: anywhere;
    }
    .instance-card-meta {
      display: grid;
      gap: 4px;
      min-width: 0;
      color: var(--muted);
      font-size: 12px;
      align-content: start;
      overflow: hidden;
    }
    .instance-card-meta div {
      overflow-wrap: anywhere;
    }
    .instance-card-footer {
      display: flex;
      min-width: 0;
      align-items: center;
      justify-content: space-between;
      gap: 8px;
      padding-top: 6px;
      padding-bottom: 2px;
    }
    .instance-footer-actions {
      display: flex;
      flex-wrap: wrap;
      gap: 8px;
      align-items: center;
      justify-content: flex-end;
      margin-left: auto;
    }
    .instance-card .toolbar {
      justify-content: flex-start;
    }
    .instance-direct-actions {
      display: flex;
      flex-wrap: wrap;
      gap: 8px;
      align-items: center;
      min-width: 0;
    }
    .instance-action-link {
      min-height: 24px;
      padding: 0;
      border: 0;
      border-radius: 0;
      background: transparent;
      color: var(--primary);
      font-size: 12px;
      text-decoration: underline;
    }
    .instance-action-link:hover {
      color: var(--primary-dark);
    }
    .instance-action-link:disabled {
      color: #94a3b8;
      cursor: not-allowed;
      text-decoration: none;
    }
    .instance-action-link.danger {
      color: var(--danger);
      background: transparent;
      border-color: transparent;
    }
    .instance-action-menu {
      position: relative;
      margin-left: auto;
    }
    .instance-action-trigger {
      display: inline-flex;
      width: 28px;
      min-width: 28px;
      height: 28px;
      min-height: 26px;
      padding: 0;
      align-items: center;
      justify-content: center;
      border: 1px solid #cbd5e1;
      border-radius: 5px;
      color: var(--muted);
      font-size: 18px;
      line-height: 1;
      text-align: center;
    }
    .instance-action-trigger:hover {
      color: var(--text);
      background: #eef2f6;
    }
    .instance-action-menu-list {
      position: fixed;
      top: 0;
      left: 0;
      z-index: 1000;
      display: grid;
      gap: 3px;
      min-width: 148px;
      padding: 6px;
      border: 1px solid var(--line);
      border-radius: 6px;
      background: #fff;
      box-shadow: 0 12px 32px rgba(15, 23, 42, .14);
    }
    .instance-action-menu-list button {
      width: 100%;
      min-height: 30px;
      padding: 0 8px;
      border: 0;
      border-radius: 4px;
      text-align: left;
    }
    .instance-action-menu-list button:hover {
      background: #eef2f6;
    }
    .route-list {
      display: grid;
      gap: 14px;
    }
    #view-routes > .toolbar {
      margin-bottom: 14px;
    }
    #routeModal .form-grid > label {
      align-self: start;
    }
    #routeModal .form-grid > label > input,
    #routeModal .form-grid > label > select {
      height: 34px;
      min-height: 34px;
    }
    .route-card {
      display: grid;
      gap: 12px;
      padding: 14px;
      border: 1px solid var(--line);
      border-radius: 6px;
      background: #fff;
    }
    .route-card-grid {
      display: grid;
      grid-template-columns: repeat(4, minmax(0, 1fr));
      gap: 10px;
    }
    .route-card-field {
      min-width: 0;
      padding: 9px;
      border: 1px solid #eef2f6;
      border-radius: 5px;
      background: #f8fafc;
    }
    .route-card-field strong {
      display: block;
      margin-bottom: 4px;
      color: var(--muted);
      font-size: 11px;
      font-weight: 650;
    }
    .route-card-field span {
      display: block;
      overflow-wrap: anywhere;
      font-size: 12px;
    }
    .route-edit-icon {
      display: inline-block;
      transform: scaleX(-1);
    }
    .route-card-editor-row {
      display: flex;
      gap: 8px;
      align-items: center;
    }
    .route-prefix-input {
      width: 100%;
      min-width: 0;
      height: 30px;
      padding: 0 8px;
      border: 1px solid #cbd5e1;
      border-radius: 4px;
      background: #fff;
      color: var(--text);
      font-size: 12px;
    }
    .small-action-button {
      flex: 0 0 auto;
      min-height: 30px;
      padding: 0 9px;
      font-size: 12px;
    }
    .route-card-note {
      padding: 9px 10px;
      border-left: 3px solid var(--warn);
      background: #fffbeb;
      color: #7a4304;
      font-size: 12px;
      line-height: 1.45;
    }
    .muted { color: var(--muted); }
    .danger { color: var(--danger); }
    .mono { font-family: Consolas, "Courier New", monospace; }
    .small { font-size: 12px; }
    .actions { justify-content: flex-end; }
    .modal > .toolbar.actions {
      gap: 12px;
      margin-top: 14px;
    }
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
      grid-template-columns: repeat(5, minmax(0, 1fr));
      gap: 8px;
    }
    .package-detail-summary-item {
      display: grid;
      gap: 4px;
      min-width: 0;
      padding: 9px;
      border: 1px solid #eef2f6;
      border-radius: 5px;
      background: #f8fafc;
    }
    .package-detail-summary-item span:last-child {
      min-width: 0;
      overflow-wrap: anywhere;
    }
    .package-detail-card {
      display: grid;
      gap: 12px;
      padding: 12px;
      border: 1px solid var(--line);
      border-radius: 6px;
      background: #fff;
    }
    .package-detail-card-title {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 10px;
      min-width: 0;
    }
    .package-detail-card-title h3 {
      min-width: 0;
      margin: 0;
      overflow-wrap: anywhere;
    }
    .package-file-explorer {
      display: grid;
      gap: 10px;
      min-width: 0;
      margin-top: 12px;
    }
    .package-file-toolbar {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 10px;
      min-width: 0;
    }
    .package-folder-path {
      min-width: 0;
      flex: 1;
      padding: 8px 10px;
      border: 1px solid var(--line);
      border-radius: 5px;
      background: #f8fafc;
      overflow-wrap: anywhere;
    }
    .package-file-filter {
      width: min(260px, 42%);
      min-width: 160px;
    }
    .runtime-file-tree {
      display: grid;
      gap: 6px;
    }
    .runtime-folder {
      border-left: 2px solid var(--line);
      padding-left: 8px;
    }
    .runtime-folder > summary {
      cursor: pointer;
      padding: 6px 8px;
      color: var(--text);
      font-weight: 600;
    }
    .runtime-folder > summary:hover {
      background: #f8fafc;
    }
    .runtime-folder-content {
      display: grid;
      gap: 4px;
      padding-left: 8px;
    }
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
    .runtime-application-manager-layout {
      display: block;
      margin-top: 12px;
    }
    .runtime-application-shell {
      overflow: hidden;
      border: 1px solid var(--line);
      border-radius: 6px;
      background: #fff;
    }
    .runtime-application-tabs-header {
      display: grid;
      gap: 0;
    }
    .runtime-application-tabs-header > .toolbar {
      min-height: 42px;
      margin: 0;
      padding: 10px 14px 6px;
    }
    .application-list {
      display: grid;
      gap: 8px;
    }
    .runtime-application-tabs {
      display: flex;
      flex-wrap: nowrap;
      gap: 0;
      overflow-x: auto;
      overflow-y: hidden;
      padding: 0 14px;
      border-bottom: 1px solid var(--line);
      scrollbar-width: none;
      -ms-overflow-style: none;
    }
    .runtime-application-tabs::-webkit-scrollbar {
      display: none;
      width: 0;
      height: 0;
    }
    .runtime-application-tabs .application-item {
      flex: 0 0 auto;
      width: auto;
      max-width: 280px;
      min-height: 42px;
      margin-bottom: -1px;
      padding: 8px 14px 9px;
      border-color: transparent;
      border-bottom: 2px solid transparent;
      border-radius: 0;
      background: transparent;
    }
    .runtime-application-tabs .application-item.active {
      border-color: transparent;
      border-bottom-color: var(--primary);
      background: transparent;
      color: var(--primary-dark);
    }
    .runtime-application-content {
      min-width: 0;
      height: calc(100vh - 230px);
      min-height: 240px;
      overflow-x: hidden;
      overflow-y: auto;
      scrollbar-width: none;
      -ms-overflow-style: none;
    }
    .runtime-application-content::-webkit-scrollbar {
      display: none;
      width: 0;
      height: 0;
    }
    .runtime-application-detail {
      display: flex;
      min-height: 100%;
      flex-direction: column;
      padding: 14px;
    }
    .runtime-application-summary {
      display: flex;
      flex-wrap: wrap;
      justify-content: center;
      gap: 8px;
      margin: auto 0 4px;
      padding-top: 16px;
    }
    .package-manager-shell {
      overflow: hidden;
      border: 1px solid var(--line);
      border-radius: 6px;
      background: #fff;
    }
    .package-manager-content {
      height: calc(100vh - 230px);
      min-height: 240px;
      overflow-x: hidden;
      overflow-y: auto;
      padding: 0 14px 14px;
      scrollbar-width: none;
      -ms-overflow-style: none;
    }
    .package-manager-content::-webkit-scrollbar {
      display: none;
      width: 0;
      height: 0;
    }
    .package-manager-content > .toolbar {
      min-height: 42px;
      margin: 0;
      padding: 10px 0 6px;
    }
    .package-manager-list {
      display: flex;
      flex-wrap: wrap;
      align-items: flex-start;
      gap: 12px;
      margin-top: 12px;
      margin-bottom: 0;
    }
    .package-manager-list > .card {
      flex: 0 1 calc(50% - 6px);
      width: calc(50% - 6px);
      max-width: 100%;
      margin: 0;
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
      min-width: 0;
      overflow-x: auto;
    }
    .file-table {
      display: grid;
      min-width: 640px;
      border: 1px solid #eef2f6;
      border-radius: 5px;
      overflow: hidden;
      font-size: 12px;
    }
    .file-row,
    .file-table-head {
      display: grid;
      grid-template-columns: minmax(260px, 1fr) minmax(100px, .35fr) minmax(170px, .55fr) 64px;
      gap: 10px;
      align-items: center;
      min-width: 0;
      padding: 9px 10px;
    }
    .file-table-head {
      color: var(--muted);
      font-weight: 650;
      background: #f8fafc;
      border-bottom: 1px solid #eef2f6;
    }
    .file-row {
      background: #fff;
      border-bottom: 1px solid #eef2f6;
    }
    .file-row:last-child { border-bottom: 0; }
    .file-row > span {
      min-width: 0;
      overflow-wrap: anywhere;
    }
    .file-row .mono { overflow-wrap: anywhere; }
    .file-row-action {
      display: inline-flex;
      justify-content: flex-start;
    }
    .file-edit-button {
      width: 30px;
      min-width: 30px;
      height: 30px;
      min-height: 30px;
      padding: 0;
      font-size: 16px;
      line-height: 1;
    }
    .file-edit-icon {
      display: inline-block;
      transform: scaleX(-1);
    }
    .file-action-disabled {
      color: #98a2b3;
      font-size: 15px;
    }
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
    #routeModal .modal {
      width: min(780px, 100%);
    }
    .route-form-alert {
      min-height: 0;
      padding: 10px 12px;
      border-left: 4px solid var(--warn);
      background: #fffbeb;
      color: #7a4304;
      font-size: 12px;
      line-height: 1.45;
    }
    .route-form-alert.error {
      border-left-color: var(--danger);
      background: #fff1f2;
      color: #9f1239;
    }
    .route-form-alert[hidden] { display: none; }
    #packageDetailModal .modal {
      height: min(720px, calc(100vh - 48px));
      max-height: none;
      display: flex;
      flex-direction: column;
      overflow: hidden;
    }
    #packageDetailContent {
      display: flex;
      flex: 1;
      flex-direction: column;
      min-height: 0;
      margin: 0;
      overflow: hidden;
    }
    #packageDetailContent .package-file-explorer {
      display: grid;
      flex: 1;
      grid-template-rows: auto minmax(0, 1fr);
      min-height: 0;
      overflow: hidden;
    }
    #packageDetailContent .file-list {
      min-height: 0;
      overflow: auto;
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
    .process-log-output {
      min-height: 360px;
      max-height: 520px;
      white-space: pre-wrap;
      overflow-wrap: anywhere;
    }
    .process-log-toolbar {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: 8px;
      margin-bottom: 10px;
    }
    .process-log-query {
      display: flex;
      flex: 1 1 320px;
      align-items: center;
      gap: 8px;
      min-width: 240px;
    }
    .process-log-query input {
      flex: 1;
      min-width: 0;
    }
    .sr-only {
      position: absolute;
      width: 1px;
      height: 1px;
      padding: 0;
      margin: -1px;
      overflow: hidden;
      clip: rect(0, 0, 0, 0);
      white-space: nowrap;
      border: 0;
    }
    .process-log-status {
      margin-left: auto;
      color: var(--muted);
      font-size: 12px;
    }
    .file-editor-path {
      margin-bottom: 10px;
      overflow-wrap: anywhere;
    }
    #fileEditorContent {
      width: 100%;
      min-height: 460px;
      resize: vertical;
      padding: 12px;
      border: 1px solid var(--line);
      border-radius: 6px;
      background: var(--code);
      color: #e6edf3;
      font-family: Consolas, "Courier New", monospace;
      font-size: 12px;
      line-height: 1.5;
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
    .deployment-step {
      display: grid;
      gap: 10px;
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
    @media (min-width: 1001px) and (max-width: 1399px) {
      .package-manager-list > .card {
        flex-basis: min(700px, 100%);
        width: min(700px, 100%);
        max-width: 100%;
      }
    }
    @media (max-width: 1000px) {
      header { align-items: stretch; flex-direction: column; }
      .ticket { grid-template-columns: 1fr; }
      .form-grid { grid-template-columns: 1fr; }
      .access-summary { grid-template-columns: 1fr; }
      .package-detail-summary { grid-template-columns: repeat(2, minmax(0, 1fr)); }
      .package-file-toolbar { align-items: stretch; flex-direction: column; }
      .package-file-filter { width: 100%; min-width: 0; }
      .file-row { grid-template-columns: minmax(260px, 1fr) minmax(100px, .35fr) minmax(170px, .55fr) 64px; }
      .package-version-row { grid-template-columns: minmax(0, 1fr); }
      .resource-dashboard-row { grid-template-columns: minmax(0, 1fr); }
      .resource-gauge-row { grid-template-columns: minmax(0, 1fr); }
      .resource-switch-gauge { grid-template-columns: minmax(0, 1fr); }
      .resource-runtime-head { display: none; }
      .resource-runtime-row { grid-template-columns: minmax(0, 1fr); }
      .package-manager-list > .card {
        flex-basis: 100%;
        width: 100%;
        max-width: 100%;
      }
      .route-card-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); }
    }
    @media (max-width: 520px) {
      .route-card-grid { grid-template-columns: minmax(0, 1fr); }
    }
  </style>
</head>
<body>
  <section id="authGate" class="auth-gate">
    <div class="auth-card">
      <h1>{{title}}</h1>
      <div class="muted" data-i18n="authSubtitle">Use ControlPlaneAuth to sign in before opening the AirForge control panel.</div>
      <div id="authError" class="auth-error"></div>
      <div class="toolbar actions">
        <button class="primary" id="unlockPanel" data-i18n="unlock">Open sign-in page</button>
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
        <button class="nav-button" data-view-target="upload"><span data-i18n="packageManager">Package Manager</span> <span id="navPackageAppCount" class="muted"></span></button>
        <button class="nav-button" data-view-target="applications" data-i18n="appRuntime">Application Runtime</button>
      </div>
      <div class="nav-group">
        <div class="nav-title" data-i18n="routeManagement">Route Management</div>
        <button class="nav-button" data-view-target="routes"><span data-i18n="routeMappingManagement">Internal Route Mapping</span> <span id="navRouteCount" class="muted"></span></button>
      </div>
      <div class="nav-group" hidden>
        <div class="nav-title" data-i18n="debug">Debug</div>
        <button class="nav-button" data-view-target="local-debug" data-i18n="localDebugBridge">Local Debug Bridge</button>
      </div>
      <div class="nav-group">
        <div class="nav-title" data-i18n="control">Control</div>
        <button class="nav-button" data-view-target="archives" data-i18n="archiveManager">Archive Manager</button>
        <button class="nav-button" data-view-target="orphans" data-i18n="orphanDeployments">Orphan Deployments</button>
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
          <button class="primary" id="openCreateDeployment" data-i18n="createDeployment">Create Instance</button>
        </div>
        <div class="runtime-application-manager-layout">
          <div class="runtime-application-shell">
            <div class="runtime-application-tabs-header">
            <div class="toolbar">
              <h3 style="flex:1" data-i18n="applications">Applications</h3>
              <button class="icon-button" id="refreshRuntimeApplications" data-i18n-title="refreshApplications" title="Refresh applications" aria-label="Refresh applications"><span aria-hidden="true">↻</span></button>
            </div>
            <div id="runtimeApplicationList" class="runtime-application-tabs" role="tablist"></div>
            </div>
            <div id="applications" class="runtime-application-content"></div>
          </div>
        </div>
      </section>
      <section id="view-routes" class="view">
        <div class="toolbar">
          <h2 style="flex:1" data-i18n="routeMappingManagement">Route Mapping</h2>
          <span id="routeSummary" class="muted" data-i18n="notLoaded">Not loaded</span>
          <button class="icon-button" id="refreshRoutes" data-i18n-title="refreshRoutes" title="Refresh routes" aria-label="Refresh routes"><span aria-hidden="true">↻</span></button>
          <button class="primary" id="createRoute" data-i18n="createRoute">Create route</button>
        </div>
        <div id="routeList" class="route-list"></div>
      </section>
      <div id="routeModal" class="modal-backdrop" hidden>
        <div class="modal">
          <div class="toolbar modal-header">
            <h2 id="routeModalTitle" style="flex:1" data-i18n="createRoute">Create dynamic route</h2>
            <button id="closeRouteModal" class="modal-close-icon close-action" type="button" data-i18n-title="close" aria-label="Close">&times;</button>
          </div>
          <div class="form-grid">
            <label><span data-i18n="routeId">Route ID</span><input id="routeId" required autocomplete="off"></label>
            <label><span data-i18n="routeName">Name</span><input id="routeName" autocomplete="off"></label>
            <label class="full"><span data-i18n="upstreamTemplate">Upstream path template</span><input id="routeUpstreamTemplate" required placeholder="/api/{id}"><span class="field-hint" data-i18n="upstreamTemplateHint">Use Ocelot-style complete path parameters such as {id} or trailing {everything}.</span></label>
            <label class="full"><span data-i18n="downstreamTemplate">Downstream path template</span><input id="routeDownstreamTemplate" required placeholder="/internal/{id}"><span class="field-hint" data-i18n="downstreamTemplateHint">Parameter names must match the upstream template.</span></label>
            <label><span data-i18n="methods">HTTP methods</span><input id="routeMethods" placeholder="GET, POST"><span class="field-hint" data-i18n="methodsHint">Leave empty to match all methods.</span></label>
            <label><span data-i18n="routeOrder">Order</span><input id="routeOrder" type="number" step="1" value="100"></label>
            <label><span data-i18n="targetDynamicAppId">Target application</span><select id="routeTargetDynamicAppId" required></select></label>
            <label><span data-i18n="targetModId">Target service</span><select id="routeTargetModId" required disabled></select></label>
            <label><span data-i18n="targetMode">Target mode</span><select id="routeTargetMode" required><option value="version" data-i18n="targetModeVersion">By version</option><option value="instance" data-i18n="targetModeInstance">By instance</option></select></label>
            <label><span data-i18n="targetVersion">Target version</span><select id="routeTargetVersion" disabled></select><span class="field-hint" data-i18n="targetVersionHint">Leave automatic to round-robin across Active + Healthy instances.</span></label>
            <label><span data-i18n="targetDeploymentId">Target instance</span><select id="routeTargetDeploymentId" required disabled></select><span class="field-hint" data-i18n="targetDeploymentHint">Select one instance; its version is implied by the instance.</span></label>
            <label><span data-i18n="routeStatus">Status</span><select id="routeEnabled"><option value="true" data-i18n="routeEnabledOption">Enabled</option><option value="false" data-i18n="routeDisabledOption">Disabled</option></select></label>
            <div id="routeFormAlert" class="route-form-alert full" role="alert" aria-live="polite" hidden></div>
          </div>
          <div class="toolbar actions">
            <button id="cancelRoute" class="close-action" type="button" data-i18n="cancel">Cancel</button>
            <button id="saveRoute" class="primary" type="button" data-i18n="saveRoute">Save route</button>
          </div>
        </div>
      </div>
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
              <option value="GET /deployments/orphans">GET /deployments/orphans</option>
              <option value="GET /packages">GET /packages</option>
              <option value="GET /deployments">GET /deployments</option>
              <option value="GET /packages/applications">GET /packages/applications</option>
              <option value="POST /packages/applications">POST /packages/applications</option>
              <option value="POST /packages/mods">POST /packages/mods</option>
              <option value="POST /deployments/create">POST /deployments/create</option>
              <option value="POST /deployments/remove">POST /deployments/remove</option>
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
      <section id="view-orphans" class="view">
        <div class="toolbar">
          <h2 style="flex:1" data-i18n="orphanDeployments">Orphan Deployments</h2>
          <button class="icon-button" id="refreshOrphans" data-i18n-title="refreshOrphans" title="Refresh orphan deployments" aria-label="Refresh orphan deployments"><span aria-hidden="true">↻</span></button>
        </div>
        <div id="orphanList" class="inventory-list"></div>
      </section>
      <section id="view-local-debug" class="view" hidden>
        <h2 data-i18n="localDebugBridge">Local Debug Bridge</h2>
        <div class="form-grid">
          <label><span data-i18n="dynamicAppId">DynamicApp Id</span><input id="debugApp" value="security-platform"></label>
          <label><span data-i18n="modId">Mod Id</span><input id="debugMod" value="auth"></label>
          <label><span data-i18n="version">Version</span><input id="debugVersion" value="local-dev"></label>
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
          <h2 style="flex:1" data-i18n="packageManager">Package Manager</h2>
        </div>
        <div class="package-manager-shell">
          <div class="runtime-application-tabs-header">
            <div class="toolbar">
              <h3 style="flex:1" data-i18n="applications">Applications</h3>
              <button class="icon-button primary draw-plus-icon" id="createPackageApplication" data-i18n-title="createApplication" title="Create Application" aria-label="Create Application"></button>
              <button class="icon-button" id="refreshPackageApplications" data-i18n-title="refreshApplications" title="Refresh applications" aria-label="Refresh applications"><span aria-hidden="true">↻</span></button>
            </div>
            <div id="packageApplicationList" class="runtime-application-tabs package-application-tabs" role="tablist"></div>
          </div>
          <div class="package-manager-content">
            <div class="toolbar">
              <h3 id="selectedPackageApplicationTitle" style="flex:1" data-i18n="packageInventory">Package Inventory</h3>
              <button class="icon-button" id="refreshPackages" data-i18n-title="refreshPackages" title="Refresh packages" aria-label="Refresh packages"><span aria-hidden="true">↻</span></button>
              <button class="primary" id="createPackageMod" data-i18n="createService">Create Service</button>
            </div>
            <div id="packageManagerList" class="inventory-list package-manager-list"></div>
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
          <div class="detail-section">
            <h3 data-i18n="runtimeFiles">Runtime Files</h3>
            <div id="detailRuntimeFiles"></div>
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
          <label class="upload-version-field"><span data-i18n="version">Version</span><input id="uploadVersion"></label>
        </div>
        <pre id="uploadResultLog" class="upload-result" hidden></pre>
        <div class="toolbar actions">
          <button id="cancelPackageUpload" class="close-action" type="button" data-i18n="cancel">Cancel</button>
          <button class="primary" id="submitUpload" data-i18n="uploadAction">Upload</button>
        </div>
      </div>
    </div>
    <div id="deploymentModal" class="modal-backdrop" hidden>
      <div class="modal">
        <div class="toolbar modal-header">
          <h2 id="deploymentTitle" style="flex:1" data-i18n="createDeployment">Create Deployment</h2>
          <button id="closeDeployment" class="modal-close-icon close-action" type="button" data-i18n-title="close" aria-label="Close">&times;</button>
        </div>
        <div id="deploymentStepOne" class="deployment-step">
          <div id="deploymentSource" class="upload-metadata-summary full" role="alert" aria-live="polite"></div>
          <label class="full"><span data-i18n="deploymentPackage">Package</span><select id="deploymentPackage"></select></label>
          <div id="deploymentAlertStepOne" class="alert full" role="alert" aria-live="polite" hidden></div>
        </div>
        <div id="deploymentStepTwo" class="deployment-step" hidden>
          <div class="upload-metadata-summary full" role="alert" aria-live="polite">
            <div class="upload-metadata-item"><strong data-i18n="deploymentStep">Step</strong><span data-i18n="deploymentStepTwo">Configure instance parameters</span></div>
          </div>
          <div class="form-grid">
          <label><span data-i18n="deploymentId">Deployment ID</span><input id="deploymentId"></label>
          <label><span data-i18n="deploymentPort">Instance Port</span><input id="deploymentPort" type="number" min="1" max="65535" step="1"></label>
          <label class="full"><span data-i18n="endpoint">Endpoint</span><input id="deploymentEndpoint" placeholder="http://127.0.0.1:5111" readonly aria-readonly="true"><span class="field-hint" data-i18n="endpointAutoGenerated">Generated automatically from the instance port.</span></label>
          <label><span data-i18n="entryFile">Entry File</span><select id="deploymentEntry" required></select></label>
          <label class="full"><span data-i18n="arguments">Arguments</span><input id="deploymentArguments"></label>
          <label><span data-i18n="healthPath">Health Path</span><input id="deploymentHealth" value="/Health"></label>
          <label><span data-i18n="startupTimeout">Startup Timeout</span><input id="deploymentTimeout" type="number" min="1" step="1" value="60"></label>
          </div>
          <div id="deploymentAlert" class="alert full" role="alert" aria-live="polite" hidden></div>
        </div>
        <div id="deploymentStepThree" class="deployment-step" hidden>
          <div class="upload-metadata-summary full" role="alert" aria-live="polite">
            <div class="upload-metadata-item"><strong data-i18n="deploymentConfirmation">Confirm deployment command</strong><span data-i18n="deploymentConfirmationHint">Review the command before the instance is created.</span></div>
          </div>
          <pre id="deploymentCommandPreview" class="upload-result full"></pre>
          <div id="deploymentConfirmationAlert" class="alert full" role="alert" aria-live="polite" hidden></div>
        </div>
        <div class="toolbar actions">
          <button id="cancelDeployment" class="close-action" type="button" data-i18n="cancel">Cancel</button>
          <button id="backDeploymentStep" type="button" data-i18n="back">Back</button>
          <button id="previewDeploymentCommand" class="primary" type="button" data-i18n="previewCommand" hidden>Preview command</button>
          <button id="editDeploymentParameters" type="button" data-i18n="editParameters" hidden>Edit parameters</button>
          <button id="nextDeploymentStep" class="primary" type="button" data-i18n="next">Next</button>
          <button id="submitDeployment" class="primary" type="button" data-i18n="confirmCreateDeployment" hidden>Confirm and create</button>
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
    <div id="processLogModal" class="modal-backdrop" hidden>
      <div class="modal">
        <div class="toolbar modal-header">
          <h2 id="processLogTitle" style="flex:1" data-i18n="processLogs">Process Logs</h2>
          <button id="closeProcessLog" class="modal-close-icon close-action" type="button" data-i18n-title="close" aria-label="Close">&times;</button>
        </div>
        <div class="process-log-toolbar">
          <label class="process-log-query">
            <span class="sr-only" data-i18n="processLogQuery">查询日志</span>
            <input id="processLogQuery" type="search" data-i18n-placeholder="processLogQueryPlaceholder" placeholder="输入关键字查询日志">
          </label>
          <button id="queryProcessLog" type="button" data-i18n="processLogQueryAction">查询</button>
          <button id="toggleProcessLogReading" class="primary" type="button" data-i18n="processLogStartReading">开始读取</button>
          <span id="processLogStatus" class="process-log-status" data-i18n="processLogNotRead">未读取</span>
        </div>
        <pre id="processLogOutput" class="process-log-output"></pre>
      </div>
    </div>
    <div id="fileEditorModal" class="modal-backdrop" hidden>
      <div class="modal">
        <div class="toolbar modal-header">
          <h2 style="flex:1" data-i18n="fileEditor">Text File Editor</h2>
          <button id="closeFileEditor" class="modal-close-icon close-action" type="button" data-i18n-title="close" aria-label="Close">&times;</button>
        </div>
        <div id="fileEditorPath" class="file-editor-path mono"></div>
        <div id="fileEditorAlert" class="alert" role="alert" aria-live="polite" hidden></div>
        <textarea id="fileEditorContent" spellcheck="false" aria-label="Text file content"></textarea>
        <div class="toolbar actions">
          <button id="cancelFileEditor" class="close-action" type="button" data-i18n="cancel">Cancel</button>
          <button id="saveFileEditor" class="primary" type="button" data-i18n="save">Save</button>
        </div>
      </div>
    </div>
  </div>
  <script>
    const managerPrefix = {{managerPrefix}};
    const runtimeStatsPath = {{runtimeStatsPath}};
    const fileEditingPath = {{fileEditingPath}};
    const processLogHubPath = {{processLogHubPath}};
    const controlPlanePath = {{controlPlanePath}};
    const controlAuthPath = {{controlAuthPath}};
    const controlPlaneQueryName = {{controlPlaneQueryName}};
    const requestLog = document.getElementById('requestLog');
    const responseLog = document.getElementById('responseLog');
    const detailLog = document.getElementById('detailLog');
    const logsLog = document.getElementById('logsLog');
    const appsNode = document.getElementById('applications');
    const runtimeApplicationList = document.getElementById('runtimeApplicationList');
    const routeList = document.getElementById('routeList');
    const routeSummary = document.getElementById('routeSummary');
    const routeModal = document.getElementById('routeModal');
    const routeModalTitle = document.getElementById('routeModalTitle');
    const routeIdInput = document.getElementById('routeId');
    const routeNameInput = document.getElementById('routeName');
    const routeUpstreamTemplateInput = document.getElementById('routeUpstreamTemplate');
    const routeDownstreamTemplateInput = document.getElementById('routeDownstreamTemplate');
    const routeMethodsInput = document.getElementById('routeMethods');
    const routeOrderInput = document.getElementById('routeOrder');
    const routeTargetDynamicAppIdInput = document.getElementById('routeTargetDynamicAppId');
    const routeTargetModIdInput = document.getElementById('routeTargetModId');
    const routeTargetModeInput = document.getElementById('routeTargetMode');
    const routeTargetVersionInput = document.getElementById('routeTargetVersion');
    const routeTargetDeploymentIdInput = document.getElementById('routeTargetDeploymentId');
    const routeEnabledInput = document.getElementById('routeEnabled');
    const routeFormAlert = document.getElementById('routeFormAlert');
    const saveRouteButton = document.getElementById('saveRoute');
    let editingRouteId = '';
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
    const orphanList = document.getElementById('orphanList');
    const languageInput = document.getElementById('language');
    const authGate = document.getElementById('authGate');
    const panelShell = document.getElementById('panelShell');
    const authError = document.getElementById('authError');
    const unlockButton = document.getElementById('unlockPanel');
    const logoutButton = document.getElementById('logoutPanel');
    const pageQuery = new URLSearchParams(window.location.search);
    const embeddedMode = pageQuery.get('embedded') === '1';
    const controlPlaneTicket = pageQuery.get(controlPlaneQueryName) || localStorage.getItem('air.controlPlane.ticket') || '';
    const historyModal = document.getElementById('historyModal');
    const historyTitle = document.getElementById('historyTitle');
    const historyContent = document.getElementById('historyContent');
    const detailModal = document.getElementById('detailModal');
    const detailModalTitle = document.getElementById('detailModalTitle');
    const detailTargetLog = document.getElementById('detailTargetLog');
    const detailManifestLog = document.getElementById('detailManifestLog');
    const detailManagedLogs = document.getElementById('detailManagedLogs');
    const detailRuntimeFiles = document.getElementById('detailRuntimeFiles');
    const packageDetailModal = document.getElementById('packageDetailModal');
    const packageDetailTitle = document.getElementById('packageDetailTitle');
    const packageDetailContent = document.getElementById('packageDetailContent');
    const packageUploadModal = document.getElementById('packageUploadModal');
    const packageUploadTitle = document.getElementById('packageUploadTitle');
    const uploadMetadataSummary = document.getElementById('uploadMetadataSummary');
    const uploadResultLog = document.getElementById('uploadResultLog');
    const submitUploadButton = document.getElementById('submitUpload');
    const deploymentModal = document.getElementById('deploymentModal');
    const deploymentTitle = document.getElementById('deploymentTitle');
    const deploymentSource = document.getElementById('deploymentSource');
    const deploymentPackageInput = document.getElementById('deploymentPackage');
    const deploymentStepOne = document.getElementById('deploymentStepOne');
    const deploymentStepTwo = document.getElementById('deploymentStepTwo');
    const deploymentStepThree = document.getElementById('deploymentStepThree');
    const deploymentAlertStepOne = document.getElementById('deploymentAlertStepOne');
    const backDeploymentStepButton = document.getElementById('backDeploymentStep');
    const previewDeploymentCommandButton = document.getElementById('previewDeploymentCommand');
    const editDeploymentParametersButton = document.getElementById('editDeploymentParameters');
    const nextDeploymentStepButton = document.getElementById('nextDeploymentStep');
    const deploymentIdInput = document.getElementById('deploymentId');
    const deploymentPortInput = document.getElementById('deploymentPort');
    const deploymentEndpointInput = document.getElementById('deploymentEndpoint');
    const deploymentEntryInput = document.getElementById('deploymentEntry');
    const deploymentArgumentsInput = document.getElementById('deploymentArguments');
    const deploymentHealthInput = document.getElementById('deploymentHealth');
    const deploymentTimeoutInput = document.getElementById('deploymentTimeout');
    const deploymentAlert = document.getElementById('deploymentAlert');
    const deploymentConfirmationAlert = document.getElementById('deploymentConfirmationAlert');
    const deploymentCommandPreview = document.getElementById('deploymentCommandPreview');
    const submitDeploymentButton = document.getElementById('submitDeployment');
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
    const processLogModal = document.getElementById('processLogModal');
    const processLogTitle = document.getElementById('processLogTitle');
    const processLogQueryInput = document.getElementById('processLogQuery');
    const processLogQueryButton = document.getElementById('queryProcessLog');
    const processLogReadButton = document.getElementById('toggleProcessLogReading');
    const processLogStatus = document.getElementById('processLogStatus');
    const processLogOutput = document.getElementById('processLogOutput');
    const fileEditorModal = document.getElementById('fileEditorModal');
    const fileEditorPath = document.getElementById('fileEditorPath');
    const fileEditorAlert = document.getElementById('fileEditorAlert');
    const fileEditorContent = document.getElementById('fileEditorContent');
    const saveFileEditorButton = document.getElementById('saveFileEditor');
    let cachedApps = [];
    let cachedRoutes = [];
    let routeConfirmUnavailable = false;
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
    let currentPackageModKey = null;
    let cachedArchives = [];
    let cachedOrphans = [];
    let processLogSocket = null;
    let processLogSocketPingTimer = null;
    let processLogTargetKey = null;
    let processLogEntries = [];
    let processLogReading = false;
    let processLogRequestVersion = 0;
    const maxProcessLogLines = 10000;
    let fileEditorSnapshot = null;
    let currentRuntimeApplicationId = '';
    let deploymentSourceKey = null;
    let deploymentCurrentKey = null;
    let deploymentMode = 'create';
    let deploymentStep = 1;
    const requestTimeoutMilliseconds = 30000;
    const requestSequences = {
      applications: 0,
      packageApplications: 0,
      packages: 0,
      archives: 0,
      runtimeStats: 0
    };

    // zh-cn: 控制面板内置双语词典，避免引入前端构建链路。
    // en-us: Built-in bilingual dictionary for the panel, keeping the page build-free.
    const i18n = {
      'zh-CN': {
        subtitle: '用于管理已挂载 DynamicApp Mods 的内置控制面板',
        authSubtitle: '请先通过 ControlPlaneAuth 登录后再进入 AirForge 控制面板。',
        managerTicket: 'ControlPlaneAuth',
        save: '保存',
        refresh: '刷新',
        unlock: '打开登录页',
        logout: '退出',
        authorized: '已授权',
        ticketRequired: '请先登录 ControlPlaneAuth。',
        ticketInvalid: 'ControlPlaneAuth 会话无效，请重新登录。',
        runtime: '运行时',
         appManagement: '应用管理',
         appRuntime: '应用运行时',
         targetManagement: '目标管理',
         routeManagement: '路由管理',
         routeMappingManagement: '路由映射',
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
        packageManager: '包管理器',
        deploymentPackage: '运行包',
        runtimeFiles: '运行文件',
        createApplication: '创建应用',
        createApplicationPrompt: '请输入 DynamicApp 应用标识',
        createService: '创建服务',
        createServicePrompt: '请输入 Mod 标识',
        refreshApplications: '刷新应用',
        refreshRoutes: '刷新路由',
        createRoute: '新建动态路由',
        editRoute: '编辑动态路由',
        routeId: '路由标识',
        routeName: '路由名称',
        upstreamTemplate: '入口模板',
        upstreamTemplateHint: '支持 Ocelot 风格的完整路径参数，例如 {id} 或尾部 {everything}。',
        downstreamTemplate: '下游模板',
        downstreamTemplateHint: '下游模板中的参数名称必须与入口模板完全一致。',
        routeOrder: '排序',
        targetMode: '目标方式',
        targetModeVersion: '按版本',
        targetModeInstance: '按实例',
        targetVersion: '目标版本',
        targetVersionHint: '自动选择时，在当前 Active + Healthy 实例中轮询；选择版本后只使用该版本。',
        targetVersionAutomatic: '自动选择 Active + Healthy',
        targetVersionImplied: '由目标实例确定',
        targetDeploymentId: '目标实例',
        targetDeploymentHint: '选择实例后只使用该实例；实例所属版本由实例自身确定。',
        targetInstanceAutomatic: '自动轮询',
        targetDynamicAppId: '目标应用',
        targetModId: '目标服务',
        selectTargetApplication: '请选择目标应用',
        selectTargetService: '请选择目标服务',
        selectTargetVersion: '请选择目标版本',
        selectTargetInstance: '请选择目标实例',
        noTargetServices: '当前应用没有可选服务',
        noTargetVersions: '当前服务没有可选版本',
        noTargetInstances: '当前服务没有可选实例',
        methods: '方法',
        methodsHint: '留空表示匹配所有 HTTP 方法。',
        routeEnabledOption: '启用',
        routeDisabledOption: '禁用',
        saveRoute: '保存路由',
        editRouteAction: '编辑',
        confirmUnavailableSave: '确认仍然保存',
        routeFormRequired: '请填写路由标识、入口模板、下游模板、目标应用和目标服务。',
        routeOrderInvalid: '排序必须是整数。',
        routeTemplateAbsolute: '入口模板和下游模板必须是以 / 开头的绝对路径。',
        routeSaveFailed: '路由保存失败。',
        routeEnabled: '已启用',
        routeDisabled: '已禁用',
        enableRoute: '启用',
        disableRoute: '禁用',
        deleteRoute: '删除路由',
        confirmDeleteRoute: '确认删除路由 {target}？',
        confirmUnavailableRoute: '目标当前不可用，确认仍然保存？',
        noPackageApplications: '暂无包管理应用，请先创建应用。',
        selectApplicationFirst: '请先选择或创建应用。',
        packageApplicationCount: '{count} 个应用',
        selectedApplication: '当前应用',
        uploadPackage: '上传包文件',
        refreshPackages: '刷新包清单',
        packageInventory: '包库存',
        noPackages: '暂无已上传的 Mod 包。',
        packageFiles: '版本文件',
        noPackageFiles: '该登记项没有包文件。',
        packageDetails: '包详情',
        updatePackage: '更新包',
        createDeployment: '创建部署实例',
        updateDeployment: '更新',
        selectPackage: '请选择运行包',
        noDeployablePackages: '没有包含可用 DLL 的本地包版本。请先上传并解压包含 DLL 的 ZIP 包。',
        requestTimeout: '请求超时，请稍后重试。',
        deploymentId: '部署标识',
        deploymentPort: '实例端口',
        deploymentSource: '源包版本',
        deploymentStep: '步骤',
        deploymentStepOne: '选择运行包',
        deploymentStepTwo: '配置实例参数',
        previewCommand: '预览命令',
        endpointAutoGenerated: '端点根据实例端口自动生成，不能手工修改。',
        deploymentConfirmation: '确认部署命令',
        deploymentConfirmationHint: '请确认最终命令，确认后才会创建实例。',
        editParameters: '返回修改',
        confirmCreateDeployment: '确认并创建',
        next: '下一步',
        back: '上一步',
        deploymentCreated: '部署实例创建完成。',
        deploymentIdRequired: '必须填写部署标识。',
        deploymentPortRequired: '实例端口必须是 1 到 65535 之间的数字。',
        extractedFiles: '解压文件',
        lastModified: '最后修改',
        filePath: '文件路径',
        folder: '文件夹',
        fileName: '文件名',
        filterFiles: '按文件名过滤',
        noMatchingFiles: '没有匹配的文件。',
        notEditable: '不可编辑',
        control: '控制',
        managerApiConsole: 'Manager API 控制台',
        archiveManager: '归档管理',
        refreshArchives: '刷新归档',
        noArchives: '暂无归档记录。',
        orphanDeployments: '孤儿副本',
        refreshOrphans: '刷新孤儿副本',
        noOrphans: '暂无孤儿副本。',
        orphanDirectory: '副本目录',
        removeOrphan: '删除副本',
        confirmRemoveOrphan: '确认删除孤儿副本 {target}？',
        orphanRemoved: '孤儿副本已删除。',
        archivedAt: '归档时间',
        unarchive: '解除归档',
        destroy: '销毁',
        destroyRequiresStopped: '实例停止后才能销毁。',
        deleteArchive: '删除归档',
        deleteService: '删除服务',
        confirmDestroy: '确认销毁实例 {target}？实例配置和运行文件会移入归档。',
        confirmDeleteArchive: '确认删除归档 {target}？归档文件会被永久清理。',
        confirmDeleteService: '确认删除空服务 {target}？',
        archiveFiles: '归档文件',
        archiveInstance: '实例归档',
        archiveVersion: '版本归档',
        serviceHasArchives: '存在归档文件，需先删除全部归档。',
        serviceDeleteRequiresEmpty: '仅允许删除没有包版本的服务。',
        deletePackage: '删除包',
        confirmDeletePackage: '确认删除包 {target}？仅删除包文件，不影响其他服务。',
        packageInUse: '包已被部署实例或归档使用，不能删除。',
        packageNotStored: '该版本不是可删除的本地包。',
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
        localEndpoint: '本地端点',
        healthPath: '健康检查路径',
        startupTimeout: '启动超时',
        deploy: '部署',
        attachLocalService: '接入本地服务',
        resetLocalSample: '重置本地示例',
        executionMode: '执行模式',
        endpoint: '端点',
        entryFile: '入口文件',
        noDllEntryFiles: '该运行包没有可用的 DLL 入口文件。',
        image: '镜像',
        arguments: '参数',
        package: '压缩包',
        noPackageSelected: '未选择压缩包。',
        auto: '自动',
        loading: '加载中...',
        loadFailed: '加载失败：{message}',
        noApplications: '未发现 DynamicApp 运行目标或清单。',
        noTargets: '暂无运行目标。',
        noRoutes: '暂无路由声明。',
         runtimeSummary: '运行统计',
         runtimeInstances: '{count} 个实例',
         instance: '实例',
         internalRoute: 'DynamicApp 内部路由',
         application: '应用',
         service: '服务',
         activeVersion: '当前激活版本',
         forwardableInstances: '可转发实例',
         instanceSelection: '实例选择',
         roundRobin: 'Active + Healthy / Round Robin',
         routeReady: '可转发',
         routeUnavailable: '无可用目标',
         routeStatus: '路由状态',
         routeActive: '可转发',
         routeInactive: '无可用目标',
        moreActions: '更多操作',
        applicationCount: '{count} 个应用',
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
        invalidEndpoint: '端点必须是 http 或 https 绝对地址。',
        versionMustChange: '新版本不能与当前版本相同。',
        packageZipRequired: '压缩包文件必须是 .zip 格式',
        uploadAction: '上传',
        updateAction: '更新',
        deployHint: '上传只保存包库存；实例创建和运行参数在应用运行时中配置。',
        uploadExecutionModeHint: '包管理器只负责上传、解压、查看和编辑包文件。',
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
        authSubtitle: 'Sign in through ControlPlaneAuth before opening the AirForge control panel.',
        managerTicket: 'ControlPlaneAuth',
        save: 'Save',
        refresh: 'Refresh',
        unlock: 'Open sign-in page',
        logout: 'Logout',
        authorized: 'Authorized',
        ticketRequired: 'ControlPlaneAuth sign-in is required.',
        ticketInvalid: 'ControlPlaneAuth session is invalid. Sign in again.',
        runtime: 'Runtime',
         appManagement: 'Application Management',
         appRuntime: 'Application Runtime',
         targetManagement: 'Target Management',
         routeManagement: 'Route Management',
         routeMappingManagement: 'Route Mapping',
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
        packageManager: 'Package Manager',
        deploymentPackage: 'Runtime Package',
        runtimeFiles: 'Runtime Files',
        createApplication: 'Create Application',
        createApplicationPrompt: 'Enter DynamicApp application id',
        createService: 'Create Service',
        createServicePrompt: 'Enter Mod identifier',
        refreshApplications: 'Refresh applications',
        refreshRoutes: 'Refresh routes',
        createRoute: 'Create dynamic route',
        editRoute: 'Edit dynamic route',
        routeId: 'Route ID',
        routeName: 'Route name',
        upstreamTemplate: 'Upstream template',
        upstreamTemplateHint: 'Use Ocelot-style complete path parameters such as {id} or trailing {everything}.',
        downstreamTemplate: 'Downstream template',
        downstreamTemplateHint: 'Parameter names must match the upstream template exactly.',
        routeOrder: 'Order',
        targetMode: 'Target mode',
        targetModeVersion: 'By version',
        targetModeInstance: 'By instance',
        targetVersion: 'Target version',
        targetVersionHint: 'Automatic selects current Active + Healthy instances; a value pins the version.',
        targetVersionAutomatic: 'Automatic Active + Healthy selection',
        targetVersionImplied: 'Determined by target instance',
        targetDeploymentId: 'Target instance',
        targetDeploymentHint: 'Select one instance; its version is determined by the instance.',
        targetInstanceAutomatic: 'Automatic round-robin',
        targetDynamicAppId: 'Target application',
        targetModId: 'Target service',
        selectTargetApplication: 'Select a target application',
        selectTargetService: 'Select a target service',
        selectTargetVersion: 'Select a target version',
        selectTargetInstance: 'Select a target instance',
        noTargetServices: 'No selectable services in this application',
        noTargetVersions: 'No selectable versions in this service',
        noTargetInstances: 'No selectable instances in this service',
        methods: 'Methods',
        methodsHint: 'Leave empty to match all HTTP methods.',
        routeEnabledOption: 'Enabled',
        routeDisabledOption: 'Disabled',
        saveRoute: 'Save route',
        editRouteAction: 'Edit',
        confirmUnavailableSave: 'Confirm save anyway',
        routeFormRequired: 'Route ID, templates, target application, and target service are required.',
        routeOrderInvalid: 'Order must be an integer.',
        routeTemplateAbsolute: 'Upstream and downstream templates must be absolute paths starting with /.',
        routeSaveFailed: 'Route save failed.',
        routeEnabled: 'Enabled',
        routeDisabled: 'Disabled',
        enableRoute: 'Enable',
        disableRoute: 'Disable',
        deleteRoute: 'Delete route',
        confirmDeleteRoute: 'Delete route {target}?',
        confirmUnavailableRoute: 'The target is currently unavailable. Save anyway?',
        noPackageApplications: 'No package applications. Create an application first.',
        selectApplicationFirst: 'Select or create an application first.',
        packageApplicationCount: '{count} application(s)',
        selectedApplication: 'Selected Application',
        uploadPackage: 'Upload Package',
        refreshPackages: 'Refresh packages',
        packageInventory: 'Package Inventory',
        noPackages: 'No uploaded Mod packages.',
        packageFiles: 'Version files',
        noPackageFiles: 'This registration has no package files.',
        packageDetails: 'Package Details',
        updatePackage: 'Update Package',
        createDeployment: 'Create Deployment',
        updateDeployment: 'Update',
        selectPackage: 'Select a runtime package',
        noDeployablePackages: 'No local package version with an extracted DLL is available. Upload a ZIP package containing a DLL first.',
        requestTimeout: 'The request timed out. Please try again.',
        deploymentId: 'Deployment ID',
        deploymentPort: 'Instance Port',
        deploymentSource: 'Source Package Version',
        deploymentStep: 'Step',
        deploymentStepOne: 'Select runtime package',
        deploymentStepTwo: 'Configure instance parameters',
        previewCommand: 'Preview command',
        endpointAutoGenerated: 'Generated automatically from the instance port and cannot be edited.',
        deploymentConfirmation: 'Confirm deployment command',
        deploymentConfirmationHint: 'Review the final command. The instance is created only after confirmation.',
        editParameters: 'Edit parameters',
        confirmCreateDeployment: 'Confirm and create',
        next: 'Next',
        back: 'Back',
        deploymentCreated: 'Deployment instance created.',
        deploymentIdRequired: 'Deployment ID is required.',
        deploymentPortRequired: 'Instance port must be a number between 1 and 65535.',
        extractedFiles: 'Extracted files',
        lastModified: 'Last modified',
        filePath: 'File path',
        folder: 'Folder',
        fileName: 'File name',
        filterFiles: 'Filter by file name',
        noMatchingFiles: 'No matching files.',
        notEditable: 'Not editable',
        control: 'Control',
        managerApiConsole: 'Manager API Console',
        archiveManager: 'Archive Manager',
        refreshArchives: 'Refresh archives',
        noArchives: 'No archived records.',
        orphanDeployments: 'Orphan Deployments',
        refreshOrphans: 'Refresh orphan deployments',
        noOrphans: 'No orphan deployments.',
        orphanDirectory: 'Deployment directory',
        removeOrphan: 'Remove deployment',
        confirmRemoveOrphan: 'Remove orphan deployment {target}?',
        orphanRemoved: 'Orphan deployment removed.',
        archivedAt: 'Archived at',
        unarchive: 'Unarchive',
        destroy: 'Destroy',
        destroyRequiresStopped: 'The instance must be stopped before it can be destroyed.',
        deleteArchive: 'Delete archive',
        deleteService: 'Delete service',
        confirmDestroy: 'Destroy instance {target}? Its configuration and runtime files will be moved to archive.',
        confirmDeleteArchive: 'Delete archive {target}? Archived files will be permanently removed.',
        confirmDeleteService: 'Delete empty service {target}?',
        archiveFiles: 'Archived files',
        archiveInstance: 'Instance archive',
        archiveVersion: 'Version archive',
        serviceHasArchives: 'Archived files exist. Delete all archives first.',
        serviceDeleteRequiresEmpty: 'Only services with no package versions can be deleted.',
        deletePackage: 'Delete package',
        confirmDeletePackage: 'Delete package {target}? Only package files will be removed.',
        packageInUse: 'The package is used by a deployment instance or archive and cannot be deleted.',
        packageNotStored: 'This version is not a deletable local package.',
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
        localEndpoint: 'Local Endpoint',
        healthPath: 'Health Path',
        startupTimeout: 'Startup Timeout',
        deploy: 'Deploy',
        attachLocalService: 'Attach local service',
        resetLocalSample: 'Reset local sample',
        executionMode: 'Execution Mode',
        endpoint: 'Endpoint',
        entryFile: 'Entry File',
        noDllEntryFiles: 'This runtime package has no DLL entry file.',
        image: 'Image',
        arguments: 'Arguments',
        package: 'Package',
        noPackageSelected: 'No package selected.',
        auto: 'Auto',
        loading: 'Loading...',
        loadFailed: 'Load failed: {message}',
        noApplications: 'No DynamicApp targets or manifests found.',
        noTargets: 'No runtime targets.',
        noRoutes: 'No route declarations.',
         runtimeSummary: 'Runtime summary',
         runtimeInstances: '{count} instance(s)',
         instance: 'Instance',
         internalRoute: 'DynamicApp Internal Route',
         application: 'Application',
         service: 'Service',
         activeVersion: 'Active Version',
         forwardableInstances: 'Forwardable Instances',
         instanceSelection: 'Instance Selection',
         roundRobin: 'Active + Healthy / Round Robin',
         routeReady: 'Forwardable',
         routeUnavailable: 'No Available Target',
         routeStatus: 'Route Status',
         routeActive: 'Forwardable',
         routeInactive: 'No Available Target',
        moreActions: 'More actions',
        applicationCount: '{count} application(s)',
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
        invalidEndpoint: 'endpoint must be an absolute http or https URL.',
        versionMustChange: 'new version must be different from the current version.',
        packageZipRequired: 'package file must be a .zip archive',
        uploadAction: 'Upload',
        updateAction: 'Update',
        deployHint: 'Upload only stores and extracts the package; instance parameters are configured in Application Runtime.',
        uploadExecutionModeHint: 'Package Manager only uploads, extracts, views, and edits package files.',
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
      processLogs: '进程日志',
      processLogQuery: '查询日志',
      processLogQueryPlaceholder: '输入关键字查询日志',
      processLogQueryAction: '查询',
      processLogStartReading: '开始读取',
      processLogStopReading: '停止读取',
      processLogNotRead: '未读取',
      processLogQuerying: '正在读取历史日志...',
      processLogQueryCompleted: '已读取 {count} 行',
      processLogReadStopped: '已停止读取',
      processLogNoMatches: '没有匹配的日志。',
      processLogConnecting: '正在连接...',
      processLogConnected: '已连接',
      processLogDisconnected: '已断开',
      processLogFailed: '连接失败：{message}',
      processLogEmpty: '暂无进程日志。',
      fileEditor: '文本文件编辑',
      editFile: '编辑',
      fileEditorLoading: '正在加载文件...',
      fileEditorSaving: '正在保存...',
      fileEditorSaved: '文件已保存。',
      fileEditorConflict: '文件已被其他操作修改，请重新加载后再保存。',
      fileEditorFailed: '文件操作失败：{message}'
    });
    Object.assign(i18n['en-US'], {
      processLogs: 'Process Logs',
      processLogQuery: 'Query logs',
      processLogQueryPlaceholder: 'Enter a keyword to query logs',
      processLogQueryAction: 'Query',
      processLogStartReading: 'Start reading',
      processLogStopReading: 'Stop reading',
      processLogNotRead: 'Not read',
      processLogQuerying: 'Reading historical logs...',
      processLogQueryCompleted: 'Read {count} lines',
      processLogReadStopped: 'Reading stopped',
      processLogNoMatches: 'No matching logs.',
      processLogConnecting: 'Connecting...',
      processLogConnected: 'Connected',
      processLogDisconnected: 'Disconnected',
      processLogFailed: 'Connection failed: {message}',
      processLogEmpty: 'No process logs.',
      fileEditor: 'Text File Editor',
      editFile: 'Edit',
      fileEditorLoading: 'Loading file...',
      fileEditorSaving: 'Saving...',
      fileEditorSaved: 'File saved.',
      fileEditorConflict: 'The file was changed by another operation. Reload it before saving.',
      fileEditorFailed: 'File operation failed: {message}'
    });
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
      }
    };

    function init() {
      document.body.classList.toggle('embedded', embeddedMode);
      if (controlPlaneTicket) {
        localStorage.setItem('air.controlPlane.ticket', controlPlaneTicket);
      }

      languageInput.value = currentLanguage;
      applyLanguage();
      document.getElementById('apiBody').value = JSON.stringify(configureToManifest(sampleConfig), null, 2);
      languageInput.addEventListener('change', () => {
        currentLanguage = languageInput.value;
        localStorage.setItem('dynamicapp.panel.language', currentLanguage);
        applyLanguage();
      });
      window.addEventListener('message', handleControlPlaneMessage);
      unlockButton.addEventListener('click', () => {
        window.location.href = controlAuthPath;
      });
      logoutButton.addEventListener('click', logoutPanel);
      document.getElementById('refreshRuntimeStats').addEventListener('click', loadRuntimeStats);
      document.getElementById('refreshRuntimeApplications').addEventListener('click', loadApplications);
      document.getElementById('refreshRoutes').addEventListener('click', loadRoutesView);
      document.getElementById('createRoute').addEventListener('click', openDynamicRouteModal);
      document.getElementById('closeRouteModal').addEventListener('click', closeDynamicRouteModal);
      document.getElementById('cancelRoute').addEventListener('click', closeDynamicRouteModal);
      saveRouteButton.addEventListener('click', saveDynamicRoute);
      [routeIdInput, routeNameInput, routeUpstreamTemplateInput, routeDownstreamTemplateInput, routeMethodsInput,
        routeOrderInput, routeTargetDynamicAppIdInput, routeTargetModIdInput, routeTargetModeInput,
        routeTargetVersionInput, routeTargetDeploymentIdInput].forEach(input => {
        input.addEventListener('input', resetRouteUnavailableConfirmation);
        input.addEventListener('change', resetRouteUnavailableConfirmation);
      });
      routeTargetDynamicAppIdInput.addEventListener('change', () => {
        populateDynamicRouteServices();
        syncDynamicRouteTargetMode();
      });
      routeTargetModIdInput.addEventListener('change', () => {
        populateDynamicRouteTargetOptions();
        syncDynamicRouteTargetMode();
      });
      routeTargetModeInput.addEventListener('change', syncDynamicRouteTargetMode);
      routeTargetVersionInput.addEventListener('change', syncDynamicRouteTargetMode);
      routeTargetDeploymentIdInput.addEventListener('change', syncDynamicRouteTargetMode);
      function resetRouteUnavailableConfirmation() {
          if (routeConfirmUnavailable) {
            routeConfirmUnavailable = false;
            saveRouteButton.textContent = t('saveRoute');
            saveRouteButton.classList.remove('danger');
            showRouteFormAlert('');
          }
      }
      routeModal.addEventListener('click', event => {
        if (event.target === routeModal) closeDynamicRouteModal();
      });
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
      document.getElementById('createPackageMod').addEventListener('click', createPackageMod);
      document.getElementById('openCreateDeployment').addEventListener('click', () => openDeploymentModal());
      deploymentPortInput.addEventListener('input', syncDeploymentEndpointFromPort);
      deploymentPackageInput.addEventListener('change', () => {
        const selectedPackage = deploymentPackageInput.value
          ? JSON.parse(decodeURIComponent(deploymentPackageInput.value))
          : null;
        populateDeploymentEntryOptions(selectedPackage, '');
      });
      document.getElementById('refreshArchives').addEventListener('click', loadArchives);
      document.getElementById('refreshOrphans').addEventListener('click', loadOrphans);
      document.getElementById('executeApi').addEventListener('click', executeApiConsole);
      document.getElementById('apiOperation').addEventListener('change', fillApiConsoleSample);
      document.getElementById('submitLocalDebug').addEventListener('click', submitLocalDebug);
      document.getElementById('resetLocalDebug').addEventListener('click', resetLocalDebug);
      document.getElementById('uploadPackage').addEventListener('change', refreshUploadSummary);
      submitUploadButton.addEventListener('click', submitUpload);
      document.getElementById('cancelPackageUpload').addEventListener('click', closePackageUploadModal);
      document.getElementById('closeDeployment').addEventListener('click', closeDeploymentModal);
      document.getElementById('cancelDeployment').addEventListener('click', closeDeploymentModal);
      backDeploymentStepButton.addEventListener('click', () => setDeploymentStep(1));
      nextDeploymentStepButton.addEventListener('click', () => setDeploymentStep(2));
      previewDeploymentCommandButton.addEventListener('click', submitDeployment);
      editDeploymentParametersButton.addEventListener('click', () => setDeploymentStep(2));
      submitDeploymentButton.addEventListener('click', submitDeployment);
      document.querySelectorAll('[data-view-target]').forEach(button => {
        button.addEventListener('click', () => button.dataset.viewTarget === 'routes'
          ? loadRoutesView()
          : showView(button.dataset.viewTarget));
      });
      document.getElementById('closeHistory').addEventListener('click', closeHistory);
      document.getElementById('closeDetailModal').addEventListener('click', closeDetailModal);
      document.getElementById('closePackageDetail').addEventListener('click', closePackageDetailModal);
      document.getElementById('closePackageUpload').addEventListener('click', closePackageUploadModal);
      document.getElementById('closeProcessLog').addEventListener('click', closeProcessLogModal);
      processLogQueryButton.addEventListener('click', queryProcessLogs);
      processLogReadButton.addEventListener('click', toggleProcessLogReading);
      processLogQueryInput.addEventListener('keydown', event => {
        if (event.key === 'Enter') {
          event.preventDefault();
          queryProcessLogs();
        }
      });
      document.getElementById('closeFileEditor').addEventListener('click', closeFileEditorModal);
      document.getElementById('cancelFileEditor').addEventListener('click', closeFileEditorModal);
      saveFileEditorButton.addEventListener('click', saveFileEditor);
      runAccessLogButton.addEventListener('click', runPendingAccessLog);
      closeAccessLogButton.addEventListener('click', () => closeAccessLogModal(false));
      runtimeApplicationList.addEventListener('click', event => {
        const button = event.target.closest('button[data-runtime-app-id]');
        if (!button) return;
        currentRuntimeApplicationId = button.dataset.runtimeAppId || '';
        renderRuntimeApplicationList(cachedApps);
        renderApplications(cachedApps);
      });
      validateTicket();
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
      if (cachedApps.length) {
        renderApplications(cachedApps);
        renderRoutes(cachedRoutes);
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
      renderOrphans(cachedOrphans);
      updateProcessLogReadingControls();
      refreshUploadSummary().catch(error => {
        logResponse({ error: error.message });
      });
      if (authError.dataset.key) {
        authError.textContent = t(authError.dataset.key);
      }
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
      authError.textContent = '';
      authError.dataset.key = '';

      try {
        unlockButton.disabled = true;
        const result = await api('/applications');
        isAuthorized = true;
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
        loadOrphans().catch(error => {
          renderOrphansError(error.message);
        });
        showView('resources');
      } catch {
        isAuthorized = false;
        panelShell.hidden = true;
        authGate.hidden = false;
        authError.dataset.key = 'ticketInvalid';
        authError.textContent = t('ticketInvalid');
      } finally {
        unlockButton.disabled = false;
      }
    }

    async function logoutPanel() {
      isAuthorized = false;
      stopRuntimeAutoRefresh(false);
      stopRuntimeUptime();
      cachedApps = [];
      cachedPackageApplications = [];
      cachedPackages = [];
      currentPackageApplicationId = '';
      currentPackageModKey = null;
      cachedOrphans = [];
      renderOrphans(cachedOrphans);
      cachedArchives = [];
      cachedRuntimeStats = null;
      closeHistory();
      closeDetailModal();
      closeDynamicRouteModal();
      runtimeAutoRefreshMenu.hidden = true;
      closeRuntimeAutoRefreshModal();
      closeAccessLogModal(true);
      closeProcessLogModal();
      closeFileEditorModal();
      panelShell.hidden = true;
      authGate.hidden = false;
      appsNode.innerHTML = '';
      runtimeStatsNode.innerHTML = '<div class="muted">' + escapeHtml(t('notLoaded')) + '</div>';
      runtimeStatsSummary.textContent = t('notLoaded');
      runtimeUptimeNode.textContent = '';
      packageApplicationList.innerHTML = '';
      packageManagerList.innerHTML = '';
      archiveList.innerHTML = '';
      requestLog.textContent = '';
      responseLog.textContent = '';
      detailLog.textContent = '';
      logsLog.textContent = '';
      try {
        await fetch(controlPlanePath + '/api/logout', { method: 'POST' });
      } finally {
        window.location.href = controlAuthPath;
      }
    }

    function handleControlPlaneMessage(event) {
      if (event.origin !== window.location.origin || !event.data || typeof event.data !== 'object') return;
      if (event.data.type === 'air-control-plane:set-language') {
        const language = event.data.language === 'zh-CN' ? 'zh-CN' : 'en-US';
        currentLanguage = language;
        languageInput.value = language;
        localStorage.setItem('dynamicapp.panel.language', language);
        applyLanguage();
        return;
      }

      if (event.data.type === 'air-control-plane:logout') {
        isAuthorized = false;
        stopRuntimeAutoRefresh(false);
        stopRuntimeUptime();
        panelShell.hidden = true;
        authGate.hidden = false;
      }
    }

    function headers(contentType) {
      const result = {};
      if (contentType) result['Content-Type'] = contentType;
      if (controlPlaneTicket) result['X-Air-ControlPlane-Ticket'] = controlPlaneTicket;
      return result;
    }

    async function api(path, options = {}) {
      const url = options.absolute ? path : managerPrefix + path;
      const controller = new AbortController();
      const timeout = window.setTimeout(() => controller.abort(), requestTimeoutMilliseconds);
      const request = {
        method: options.method || 'GET',
        headers: options.headers || headers(options.contentType),
        body: options.body,
        signal: controller.signal
      };
      logRequest({ url, method: request.method, headers: redactHeaders(request.headers), body: previewBody(request.body) });
      try {
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
      } catch (error) {
        if (error && error.name === 'AbortError') {
          throw new Error(t('requestTimeout'));
        }
        throw error;
      } finally {
        window.clearTimeout(timeout);
      }
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
        loadArchives().catch(error => renderArchivesError(error.message)),
        loadOrphans().catch(error => renderOrphansError(error.message))
      ]);
    }

    async function loadApplications() {
      if (!isAuthorized) return;
      const sequence = ++requestSequences.applications;
      loadRuntimeStats().catch(error => {
        renderRuntimeStatsError(error.message);
      });
      appsNode.dataset.status = 'loading';
      appsNode.innerHTML = '<div class="muted">' + escapeHtml(t('loading')) + '</div>';
      try {
        const result = await api('/applications');
        if (sequence !== requestSequences.applications) return;
        renderApplicationResult(result);
      } catch (error) {
        if (sequence !== requestSequences.applications) return;
        cachedApps = [];
        appsNode.dataset.status = 'failed';
        appsNode.dataset.message = error.message;
        appsNode.innerHTML = '<div class="muted">' + escapeHtml(t('loadFailed', { message: error.message })) + '</div>';
      }
    }

    async function loadRoutesView() {
      if (!isAuthorized) return;
      routeList.innerHTML = '<div class="muted">' + escapeHtml(t('loading')) + '</div>';
      try {
        const result = await api('/routes');
        renderRoutes(result && result.data ? result.data : []);
        showView('routes');
      } catch (error) {
        routeList.innerHTML = '<div class="muted">' + escapeHtml(t('loadFailed', { message: error.message })) + '</div>';
      }
    }

    function openDynamicRouteModal(route = null) {
      editingRouteId = route && route.routeId ? String(route.routeId) : '';
      routeConfirmUnavailable = false;
      routeModalTitle.dataset.i18n = editingRouteId ? 'editRoute' : 'createRoute';
      routeModalTitle.textContent = t(editingRouteId ? 'editRoute' : 'createRoute');
      routeIdInput.disabled = Boolean(editingRouteId);
      routeIdInput.value = editingRouteId;
      routeNameInput.value = route && route.name ? route.name : '';
      routeUpstreamTemplateInput.value = route && route.upstreamPathTemplate ? route.upstreamPathTemplate : '/api/{id}';
      routeDownstreamTemplateInput.value = route && route.downstreamPathTemplate ? route.downstreamPathTemplate : '/internal/{id}';
      routeMethodsInput.value = route && Array.isArray(route.upstreamHttpMethods) ? route.upstreamHttpMethods.join(', ') : '';
      routeOrderInput.value = route && Number.isInteger(Number(route.order)) ? String(route.order) : '100';
      routeTargetModeInput.value = 'version';
      populateDynamicRouteApplications();
      if (editingRouteId) {
        routeTargetDynamicAppIdInput.value = route.targetDynamicAppId || '';
        populateDynamicRouteServices();
        routeTargetModIdInput.value = route.targetModId || '';
        populateDynamicRouteTargetOptions();
        routeTargetModeInput.value = route.targetDeploymentId ? 'instance' : 'version';
        if (route.targetVersion && !Array.from(routeTargetVersionInput.options).some(option => sameText(option.value, route.targetVersion))) {
          routeTargetVersionInput.add(new Option(route.targetVersion, route.targetVersion));
        }
        if (route.targetDeploymentId && !Array.from(routeTargetDeploymentIdInput.options).some(option => sameText(option.value, route.targetDeploymentId))) {
          routeTargetDeploymentIdInput.add(new Option(route.targetDeploymentId, route.targetDeploymentId));
        }
        routeTargetVersionInput.value = route.targetVersion || '';
        routeTargetDeploymentIdInput.value = route.targetDeploymentId || '';
        syncDynamicRouteTargetMode();
      }
      routeEnabledInput.value = route && route.enabled === false ? 'false' : 'true';
      routeFormAlert.hidden = true;
      routeFormAlert.textContent = '';
      routeFormAlert.classList.remove('error');
      saveRouteButton.disabled = false;
      saveRouteButton.textContent = t('saveRoute');
      saveRouteButton.classList.remove('danger');
      routeModal.hidden = false;
      window.setTimeout(() => routeIdInput.focus(), 0);
    }

    function populateDynamicRouteApplications() {
      const options = ['<option value="">' + escapeHtml(t('selectTargetApplication')) + '</option>'];
      (cachedApps || []).slice().sort((left, right) => String(left.dynamicAppId || '').localeCompare(String(right.dynamicAppId || ''), undefined, { sensitivity: 'base' }))
        .forEach(app => {
          const value = keyPart(app.dynamicAppId);
          if (value) options.push('<option value="' + escapeHtml(value) + '">' + escapeHtml(value) + '</option>');
        });
      routeTargetDynamicAppIdInput.innerHTML = options.join('');
      routeTargetDynamicAppIdInput.disabled = !(cachedApps || []).length;
      routeTargetDynamicAppIdInput.value = '';
      populateDynamicRouteServices();
    }

    function findDynamicRouteApplication() {
      return (cachedApps || []).find(app => sameText(keyPart(app.dynamicAppId), routeTargetDynamicAppIdInput.value)) || null;
    }

    function getDynamicRouteServices(app) {
      const services = new Map();
      (app && app.mods || []).forEach(mod => {
        const modId = keyPart(mod.modId);
        if (modId && !services.has(String(modId).toLowerCase())) services.set(String(modId).toLowerCase(), modId);
      });
      return Array.from(services.values()).sort((left, right) => String(left).localeCompare(String(right), undefined, { sensitivity: 'base' }));
    }

    function getDynamicRouteServiceVersions(app, modId) {
      const versions = new Map();
      (app && app.mods || []).filter(mod => sameText(keyPart(mod.modId), modId)).forEach(mod => {
        const version = keyPart(mod.version);
        if (version && !versions.has(String(version).toLowerCase())) versions.set(String(version).toLowerCase(), version);
      });
      return Array.from(versions.values()).sort((left, right) => String(left).localeCompare(String(right), undefined, { sensitivity: 'base', numeric: true }));
    }

    function getDynamicRouteServiceInstances(app, modId) {
      const instances = [];
      const seen = new Set();
      (app && app.mods || []).filter(mod => sameText(keyPart(mod.modId), modId)).forEach(mod => {
        const version = keyPart(mod.version);
        const deployments = Array.isArray(mod.deployments) && mod.deployments.length
          ? mod.deployments
          : [{ deploymentId: mod.deploymentId || 'default', state: mod.state, healthStatus: mod.healthStatus }];
        deployments.forEach(deployment => {
          const deploymentId = deployment.deploymentId || 'default';
          const identity = String(version).toLowerCase() + '|' + String(deploymentId).toLowerCase();
          if (!version || seen.has(identity)) return;
          seen.add(identity);
          instances.push({
            version,
            deploymentId,
            state: deployment.state || mod.state || '',
            healthStatus: deployment.healthStatus || mod.healthStatus || ''
          });
        });
      });
      return instances.sort((left, right) => {
        const versionOrder = String(left.version).localeCompare(String(right.version), undefined, { sensitivity: 'base', numeric: true });
        return versionOrder || String(left.deploymentId).localeCompare(String(right.deploymentId), undefined, { sensitivity: 'base' });
      });
    }

    function populateDynamicRouteServices() {
      const services = getDynamicRouteServices(findDynamicRouteApplication());
      routeTargetModIdInput.innerHTML = '<option value="">' + escapeHtml(services.length ? t('selectTargetService') : t('noTargetServices')) + '</option>' +
        services.map(service => '<option value="' + escapeHtml(service) + '">' + escapeHtml(service) + '</option>').join('');
      routeTargetModIdInput.disabled = !services.length;
      routeTargetModIdInput.value = '';
      populateDynamicRouteTargetOptions();
    }

    function populateDynamicRouteTargetOptions() {
      const app = findDynamicRouteApplication();
      const versions = getDynamicRouteServiceVersions(app, routeTargetModIdInput.value);
      const instances = getDynamicRouteServiceInstances(app, routeTargetModIdInput.value);
      routeTargetVersionInput.innerHTML = '<option value="">' + escapeHtml(t('targetVersionAutomatic')) + '</option>' +
        versions.map(version => '<option value="' + escapeHtml(version) + '">' + escapeHtml(version) + '</option>').join('');
      routeTargetDeploymentIdInput.innerHTML = '<option value="">' + escapeHtml(instances.length ? t('selectTargetInstance') : t('noTargetInstances')) + '</option>' +
        instances.map(instance => {
          const state = instance.state || '-';
          const health = instance.healthStatus || '-';
          const label = instance.version + ' / ' + instance.deploymentId + ' (' + state + ' / ' + health + ')';
          return '<option value="' + escapeHtml(instance.deploymentId) + '">' + escapeHtml(label) + '</option>';
        }).join('');
      routeTargetVersionInput.value = '';
      routeTargetDeploymentIdInput.value = '';
      syncDynamicRouteTargetMode();
    }

    function syncDynamicRouteTargetMode() {
      const hasService = Boolean(routeTargetModIdInput.value);
      const versionMode = routeTargetModeInput.value !== 'instance';
      routeTargetModeInput.disabled = !hasService;
      routeTargetVersionInput.disabled = !hasService || !versionMode;
      routeTargetVersionInput.required = false;
      routeTargetDeploymentIdInput.disabled = !hasService || versionMode;
      routeTargetDeploymentIdInput.required = hasService && !versionMode;
      if (versionMode) {
        routeTargetDeploymentIdInput.value = '';
      } else {
        routeTargetVersionInput.value = '';
      }
    }

    function closeDynamicRouteModal() {
      routeModal.hidden = true;
      editingRouteId = '';
      routeModalTitle.dataset.i18n = 'createRoute';
      routeModalTitle.textContent = t('createRoute');
      routeIdInput.disabled = false;
      routeConfirmUnavailable = false;
      saveRouteButton.disabled = false;
      saveRouteButton.textContent = t('saveRoute');
      saveRouteButton.classList.remove('danger');
    }

    function showRouteFormAlert(message, error = false) {
      routeFormAlert.textContent = message || '';
      routeFormAlert.hidden = !message;
      routeFormAlert.classList.toggle('error', Boolean(error));
    }

    function readDynamicRouteForm() {
      const routeId = routeIdInput.value.trim();
      const upstream = routeUpstreamTemplateInput.value.trim();
      const downstream = routeDownstreamTemplateInput.value.trim();
      const dynamicAppId = routeTargetDynamicAppIdInput.value.trim();
      const modId = routeTargetModIdInput.value.trim();
      const targetMode = routeTargetModeInput.value === 'instance' ? 'instance' : 'version';
      const targetVersion = targetMode === 'version' ? routeTargetVersionInput.value.trim() : '';
      const targetDeploymentId = targetMode === 'instance' ? routeTargetDeploymentIdInput.value.trim() : '';
      const order = Number(routeOrderInput.value);
      if (!routeId || !upstream || !downstream || !dynamicAppId || !modId || (targetMode === 'instance' && !targetDeploymentId)) {
        showRouteFormAlert(t('routeFormRequired'), true);
        return null;
      }
      if (!Number.isInteger(order)) {
        showRouteFormAlert(t('routeOrderInvalid'), true);
        return null;
      }
      if (!upstream.startsWith('/') || !downstream.startsWith('/')) {
        showRouteFormAlert(t('routeTemplateAbsolute'), true);
        return null;
      }

      const methods = routeMethodsInput.value
        .split(',')
        .map(method => method.trim().toUpperCase())
        .filter(Boolean);
      return {
        routeId,
        name: routeNameInput.value.trim() || routeId,
        upstreamPathTemplate: upstream,
        upstreamHttpMethods: methods,
        downstreamPathTemplate: downstream,
        targetDynamicAppId: dynamicAppId,
        targetModId: modId,
        targetVersion: targetVersion || null,
        targetDeploymentId: targetDeploymentId || null,
        order,
        enabled: routeEnabledInput.value === 'true'
      };
    }

    async function saveDynamicRoute() {
      const route = readDynamicRouteForm();
      if (!route) return;
      const editing = Boolean(editingRouteId);
      saveRouteButton.disabled = true;
      try {
        await api(editing ? '/routes/' + encodeURIComponent(editingRouteId) : '/routes', {
          method: editing ? 'PUT' : 'POST',
          contentType: 'application/json',
          body: JSON.stringify({ route, confirmUnavailable: routeConfirmUnavailable })
        });
        closeDynamicRouteModal();
        await loadRoutesView();
      } catch (error) {
        const notice = error && error.payload && error.payload.data;
        if (error && error.status === 409 && notice && notice.requiresConfirmation) {
          routeConfirmUnavailable = true;
          showRouteFormAlert(error.payload.message || t('confirmUnavailableRoute'));
          saveRouteButton.textContent = t('confirmUnavailableSave');
          saveRouteButton.classList.add('danger');
          saveRouteButton.disabled = false;
          return;
        }
        showRouteFormAlert(error && error.message ? error.message : t('routeSaveFailed'), true);
        saveRouteButton.disabled = false;
      }
    }

    async function loadRuntimeStats() {
      if (!isAuthorized) return;
      const sequence = ++requestSequences.runtimeStats;
      const payload = await api(runtimeStatsPath, { headers: headers(), absolute: true });
      if (sequence !== requestSequences.runtimeStats) return payload;
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
      const sequence = ++requestSequences.packageApplications;
      packageApplicationList.innerHTML = '<div class="muted">' + escapeHtml(t('loading')) + '</div>';
      const result = await api('/packages/applications');
      if (sequence !== requestSequences.packageApplications) return;
      cachedPackageApplications = result && result.data ? result.data : [];
      if (!currentPackageApplicationId && cachedPackageApplications.length) {
        currentPackageApplicationId = cachedPackageApplications[0].dynamicAppId || '';
      }
      if (currentPackageApplicationId && !cachedPackageApplications.some(item => sameText(item.dynamicAppId, currentPackageApplicationId))) {
        currentPackageApplicationId = cachedPackageApplications.length ? cachedPackageApplications[0].dynamicAppId || '' : '';
      }
      renderPackageApplications(cachedPackageApplications);
      renderPackages(cachedPackages);
    }

    function renderPackageApplications(applications) {
      if (!packageApplicationList) return;
      if (!applications || !applications.length) {
        packageApplicationList.innerHTML = '<div class="muted">' + escapeHtml(t('noPackageApplications')) + '</div>';
        selectedPackageApplicationTitle.textContent = t('packageInventory');
        document.getElementById('createPackageMod').disabled = true;
        packageManagerList.innerHTML = '<div class="muted">' + escapeHtml(t('selectApplicationFirst')) + '</div>';
        return;
      }

      document.getElementById('createPackageMod').disabled = false;
      packageApplicationList.innerHTML = applications.slice()
        .sort((left, right) => String(left.dynamicAppId || '').localeCompare(String(right.dynamicAppId || ''), undefined, { sensitivity: 'base' }))
        .map(item => {
          const active = sameText(item.dynamicAppId, currentPackageApplicationId) ? ' active' : '';
          return '<button class="application-item' + active + '" role="tab" aria-selected="' + (active ? 'true' : 'false') + '" data-action="select-package-application" data-app="' + escapeHtml(item.dynamicAppId || '') + '">' +
            '<strong>' + escapeHtml(item.dynamicAppId || '') + '</strong>' +
            '</button>';
        }).join('');
      selectedPackageApplicationTitle.textContent = t('packageInventory');
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

    async function createPackageMod() {
      if (!currentPackageApplicationId) {
        alert(t('selectApplicationFirst'));
        return;
      }

      const modId = prompt(t('createServicePrompt'), '');
      if (!modId) return;
      const normalized = modId.trim();
      if (!normalized) return;

      const result = await api('/packages/mods', jsonPost({
        dynamicAppId: currentPackageApplicationId,
        modId: normalized
      }));
      currentPackageModKey = {
        dynamicAppId: currentPackageApplicationId,
        modId: normalized
      };
      await loadPackageApplications();
      await loadPackages();
      alert(result && result.message ? result.message : t('createService'));
    }

    async function loadPackages() {
      if (!isAuthorized) return;
      const sequence = ++requestSequences.packages;
      packageManagerList.innerHTML = '<div class="muted">' + escapeHtml(t('loading')) + '</div>';
      const result = await api('/packages');
      if (sequence !== requestSequences.packages) return;
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
        const groupKey = encodeURIComponent(JSON.stringify({
          dynamicAppId: item.dynamicAppId || '',
          modId: item.modId || ''
        }));
        const serviceKey = {
          dynamicAppId: item.dynamicAppId || '',
          modId: item.modId || ''
        };
        const hasArchives = hasServiceArchives(serviceKey);
        const deleteServiceButton = !versions.length
          ? '<button data-action="delete-package-mod" data-key="' + groupKey + '" class="danger"' +
            (hasArchives ? ' disabled title="' + escapeHtml(t('serviceHasArchives')) + '"' : '') + '>' +
            escapeHtml(t('deleteService')) + '</button>'
          : '';
        const actions = '<div class="toolbar">' +
            '<button data-action="package-upload" data-key="' + groupKey + '">' + escapeHtml(t('uploadPackage')) + '</button>' +
            (current && !isExistingContainer && history.length ? '<button class="warning" data-action="package-history" data-key="' + payload + '"><span aria-hidden="true">⚠</span> ' + escapeHtml(t('viewHistory')) + ' (' + history.length + ')</button>' : '') +
            (current && isExistingContainer ? '<button data-action="package-detail" data-key="' + payload + '">' + escapeHtml(t('details')) + '</button>' : '') +
            deleteServiceButton +
            '</div>';
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

    function hasServiceArchives(key) {
      return (cachedArchives || []).some(record => {
        const archiveKey = normalizeArchiveKey(record.key || {});
        return sameText(archiveKey.dynamicAppId, key.dynamicAppId)
          && sameText(archiveKey.modId, key.modId);
      });
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
      const packageKey = { dynamicAppId: version.dynamicAppId || item.dynamicAppId || '', modId: version.modId || item.modId || '', version: version.version || '' };
      const sourceKind = String(version.sourceKind || 'PackageStore').toLowerCase();
      const canDelete = (sourceKind === '' || sourceKind === 'packagestore') && !isPackageVersionUsed(packageKey);
      const deleteDisabledReason = sourceKind !== '' && sourceKind !== 'packagestore'
        ? t('packageNotStored')
        : (canDelete ? '' : t('packageInUse'));
      const deleteButton = '<button data-action="delete-package-version" data-key="' + payload + '" class="danger"' +
        (canDelete ? '' : ' disabled title="' + escapeHtml(deleteDisabledReason) + '"') + '>' +
        escapeHtml(t('deletePackage')) + '</button>';
      const versionBadge = options.isHistory
        ? '<span class="badge">' + escapeHtml(t('historyVersions')) + '</span>'
        : '';
      return '<div class="package-version-row">' +
        '<div><div><span class="mono">' + escapeHtml(version.version || '') + '</span> ' + versionBadge + ' <span class="badge">' + escapeHtml(formatBytes(version.packageBytes || 0)) + '</span>' + modeBadge + '</div>' +
        '<div class="muted small">' + escapeHtml(t('packageFiles')) + ': ' + files.length + ' · ' + escapeHtml(t('lastModified')) + ': ' + escapeHtml(formatDateTime(version.savedAtUtc)) + '</div>' +
        manifestLine + '</div>' +
        '<div class="toolbar"><button data-action="package-detail" data-key="' + payload + '">' + escapeHtml(t('details')) + '</button>' + deleteButton + '</div>' +
        '</div>';
    }

    function isPackageVersionUsed(key) {
      const usedByRuntime = (cachedApps || []).some(app => groupAllMods(app).some(group => group.versions.some(mod =>
        sameText(mod.dynamicAppId, key.dynamicAppId) && sameText(mod.modId, key.modId) && sameText(mod.version, key.version) &&
        (Array.isArray(mod.deployments) ? mod.deployments.length > 0 : normalizeState(mod.state) !== 'none'))));
      const usedByArchive = (cachedArchives || []).some(record => {
        const archiveKey = normalizeArchiveKey(record.key || {});
        return sameText(archiveKey.dynamicAppId, key.dynamicAppId) && sameText(archiveKey.modId, key.modId) && sameText(archiveKey.version, key.version);
      });
      return usedByRuntime || usedByArchive;
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

    function normalizePackagePath(path) {
      return String(path || '').replace(/\\/g, '/').replace(/^\.\/+/, '');
    }

    function getPackageFileName(file) {
      const explicitName = String((file && file.name) || '').trim();
      if (explicitName) return explicitName;
      const path = normalizePackagePath(file && file.relativePath);
      const segments = path.split('/').filter(Boolean);
      return segments[segments.length - 1] || '-';
    }

    function getPackageFolderPath(files) {
      const directories = (files || []).map(file => {
        const path = normalizePackagePath(file && file.relativePath);
        const segments = path.split('/').filter(Boolean);
        segments.pop();
        return segments;
      }).filter(segments => segments.length);
      if (!directories.length) return '.';

      const common = directories[0].slice();
      directories.slice(1).forEach(segments => {
        let index = 0;
        while (index < common.length && index < segments.length && sameText(common[index], segments[index])) {
          index++;
        }
        common.splice(index);
      });
      return common.join('/') || '.';
    }

    function sortPackageFiles(files) {
      return (files || []).slice().sort((left, right) => {
        const leftName = getPackageFileName(left);
        const rightName = getPackageFileName(right);
        return leftName.localeCompare(rightName, undefined, { sensitivity: 'base', numeric: true });
      });
    }

    function renderPackageFile(file) {
      const fileName = getPackageFileName(file);
      const editable = isEditableTextFile(file.relativePath || fileName);
      const filePath = file.fullPath || '';
      const editButton = editable && filePath
        ? '<button class="icon-button file-edit-button" data-action="edit-file" data-path="' + escapeHtml(filePath) + '" data-i18n-title="editFile" title="' + escapeHtml(t('editFile')) + '" aria-label="' + escapeHtml(t('editFile')) + '"><span class="file-edit-icon" aria-hidden="true">&#9998;</span></button>'
        : '<span class="file-action-disabled" title="' + escapeHtml(t('notEditable')) + '" aria-label="' + escapeHtml(t('notEditable')) + '">&#8212;</span>';
      return '<div class="file-row">' +
        '<span class="mono" title="' + escapeHtml(file.fullPath || file.relativePath || fileName) + '">' + escapeHtml(fileName) + '</span>' +
        '<span>' + escapeHtml(formatBytes(file.length || 0)) + '</span>' +
        '<span class="muted">' + escapeHtml(formatDateTime(file.lastModifiedUtc)) + '</span>' +
        '<span class="file-row-action">' + editButton + '</span>' +
        '</div>';
    }

    function renderPackageFileTreeNode(node) {
      const folders = Array.from(node.folders.entries()).sort((left, right) => left[0].localeCompare(right[0], undefined, { sensitivity: 'base' }));
      const files = node.files.slice().sort((left, right) => left.name.localeCompare(right.name, undefined, { sensitivity: 'base', numeric: true }));
      return folders.map(([name, child]) =>
        '<details class="runtime-folder" open><summary>' + escapeHtml(name) + '</summary><div class="runtime-folder-content">' + renderPackageFileTreeNode(child) + '</div></details>'
      ).join('') + files.map(item => renderPackageFile(item.file)).join('');
    }

    function buildPackageFileTree(files) {
      const root = { folders: new Map(), files: [] };
      (files || []).forEach(file => {
        const relativePath = normalizePackagePath(file && (file.relativePath || file.name)).replace(/^\/+|\/+$/g, '');
        const segments = relativePath.split('/').filter(Boolean);
        if (!segments.length) return;
        let node = root;
        segments.slice(0, -1).forEach(folder => {
          if (!node.folders.has(folder)) node.folders.set(folder, { folders: new Map(), files: [] });
          node = node.folders.get(folder);
        });
        node.files.push({ file, name: segments[segments.length - 1] });
      });
      return root;
    }

    function renderPackageFileRows(files, filterText) {
      const query = String(filterText || '').trim().toLowerCase();
      if (!query) {
        const tree = renderPackageFileTreeNode(buildPackageFileTree(files));
        return tree || '<div class="file-row"><span class="muted">' + escapeHtml(t('noMatchingFiles')) + '</span><span></span><span></span><span></span></div>';
      }
      const visibleFiles = sortPackageFiles(files).filter(file => {
        return getPackageFileName(file).toLowerCase().includes(query);
      });
      return visibleFiles.length
        ? visibleFiles.map(renderPackageFile).join('')
        : '<div class="file-row"><span class="muted">' + escapeHtml(t('noMatchingFiles')) + '</span><span></span><span></span><span></span></div>';
    }

    function renderRuntimeFileRow(file, displayName) {
      const path = file.fullPath || '';
      const editable = path && isEditableTextFile(path);
      const action = editable
        ? '<button class="icon-button file-edit-button" data-action="edit-file" data-path="' + escapeHtml(path) + '" data-i18n-title="editFile" title="' + escapeHtml(t('editFile')) + '" aria-label="' + escapeHtml(t('editFile')) + '"><span class="file-edit-icon" aria-hidden="true">&#9998;</span></button>'
        : '<span class="file-action-disabled" title="' + escapeHtml(t('notEditable')) + '" aria-label="' + escapeHtml(t('notEditable')) + '">&#8212;</span>';
      return '<div class="file-row"><span class="mono" title="' + escapeHtml(path) + '">' + escapeHtml(displayName || file.name || '-') + '</span>' +
        '<span>' + escapeHtml(formatBytes(file.length || 0)) + '</span><span class="muted">' + escapeHtml(formatDateTime(file.lastModifiedUtc)) + '</span>' +
        '<span class="file-row-action">' + action + '</span></div>';
    }

    function buildRuntimeFileTree(files) {
      const root = { folders: new Map(), files: [] };
      (files || []).forEach(file => {
        const relativePath = String(file.relativePath || file.name || '').replace(/\\/g, '/').replace(/^\/+|\/+$/g, '');
        const segments = relativePath.split('/').filter(Boolean);
        if (!segments.length) return;
        let node = root;
        segments.slice(0, -1).forEach(folder => {
          if (!node.folders.has(folder)) node.folders.set(folder, { folders: new Map(), files: [] });
          node = node.folders.get(folder);
        });
        node.files.push({ file, name: segments[segments.length - 1] });
      });
      return root;
    }

    function renderRuntimeFileTreeNode(node) {
      const folders = Array.from(node.folders.entries()).sort((left, right) => left[0].localeCompare(right[0], undefined, { sensitivity: 'base' }));
      const files = node.files.slice().sort((left, right) => left.name.localeCompare(right.name, undefined, { sensitivity: 'base' }));
      return folders.map(([name, child]) =>
        '<details class="runtime-folder" open><summary>' + escapeHtml(name) + '</summary><div class="runtime-folder-content">' + renderRuntimeFileTreeNode(child) + '</div></details>'
      ).join('') + files.map(item => renderRuntimeFileRow(item.file, item.name)).join('');
    }

    function renderRuntimeFiles(files) {
      if (!detailRuntimeFiles) return;
      const values = Array.isArray(files) ? files : [];
      if (!values.length) {
        detailRuntimeFiles.innerHTML = '<div class="muted">' + escapeHtml(t('noRuntimeFiles')) + '</div>';
        return;
      }

      const rows = renderRuntimeFileTreeNode(buildRuntimeFileTree(values));
      detailRuntimeFiles.innerHTML =
        '<div class="file-list"><div class="file-table"><div class="file-table-head"><span>' + escapeHtml(t('fileName')) + '</span><span>' + escapeHtml(t('size')) + '</span><span>' + escapeHtml(t('lastModified')) + '</span><span>' + escapeHtml(t('control')) + '</span></div><div class="runtime-file-tree">' +
        rows + '</div></div></div>';
    }

    function showPackageDetails(key) {
      const version = findPackageVersion(key);
      if (!version) return;
      const files = sortPackageFiles(version.files || []);
      packageDetailTitle.textContent = t('packageDetails') + ' - ' + key.dynamicAppId + '/' + key.modId + '/' + key.version;
      packageDetailContent.innerHTML =
        '<div class="package-detail-card">' +
        '<div class="package-detail-card-title"><h3>' + escapeHtml(key.dynamicAppId + ' / ' + key.modId) + '</h3><span class="badge">' + escapeHtml(key.version) + '</span></div>' +
        '<div class="package-detail-summary">' +
        '<div class="package-detail-summary-item"><span class="muted">' + escapeHtml(t('version')) + '</span><span class="mono">' + escapeHtml(key.version) + '</span></div>' +
        '<div class="package-detail-summary-item"><span class="muted">' + escapeHtml(t('executionMode')) + '</span><span class="badge">' + escapeHtml(version.executionMode || '-') + '</span></div>' +
        '<div class="package-detail-summary-item"><span class="muted">' + escapeHtml(t('endpoint')) + '</span><span class="mono">' + escapeHtml(version.endpoint || '-') + '</span></div>' +
        '<div class="package-detail-summary-item"><span class="muted">' + escapeHtml(t('size')) + '</span><span class="badge">' + escapeHtml(formatBytes(version.packageBytes || 0)) + '</span></div>' +
        '<div class="package-detail-summary-item"><span class="muted">' + escapeHtml(t('lastModified')) + '</span><span>' + escapeHtml(formatDateTime(version.savedAtUtc)) + '</span></div>' +
        '</div>' +
        '</div>' +
        '<div class="package-file-explorer">' +
        '<div class="package-file-toolbar">' +
        '<div class="package-folder-path"><span class="muted">' + escapeHtml(t('folder')) + '</span>: <span class="mono">' + escapeHtml(getPackageFolderPath(files)) + '</span></div>' +
        '<input class="package-file-filter" data-package-file-filter type="search" placeholder="' + escapeHtml(t('filterFiles')) + '" aria-label="' + escapeHtml(t('filterFiles')) + '">' +
        '</div>' +
        '<div class="file-list"><div class="file-table"><div class="file-table-head"><span>' + escapeHtml(t('fileName')) + '</span><span>' + escapeHtml(t('size')) + '</span><span>' + escapeHtml(t('lastModified')) + '</span><span>' + escapeHtml(t('control')) + '</span></div><div data-package-file-rows>' + renderPackageFileRows(files, '') + '</div></div></div>' +
        '</div>';
      const filter = packageDetailContent.querySelector('[data-package-file-filter]');
      const rows = packageDetailContent.querySelector('[data-package-file-rows]');
      filter?.addEventListener('input', () => {
        if (rows) rows.innerHTML = renderPackageFileRows(files, filter.value);
      });
      packageDetailModal.hidden = false;
    }

    function closePackageDetailModal() {
      packageDetailModal.hidden = true;
      packageDetailContent.innerHTML = '';
    }

    function openPackageUploadModal(key) {
      const dynamicAppId = key && key.dynamicAppId;
      const modId = key && key.modId;
      if (!dynamicAppId || !modId) {
        alert(t('selectApplicationFirst'));
        return;
      }

      currentPackageModKey = { dynamicAppId, modId };
      resetPackageUploadForm(currentPackageModKey);
      packageUploadTitle.textContent = t('uploadPackage') + ' - ' + dynamicAppId + '/' + modId;
      refreshUploadSummary().catch(error => logResponse({ error: error.message }));
      packageUploadModal.hidden = false;
    }

    function closePackageUploadModal() {
      packageUploadModal.hidden = true;
      currentPackageModKey = null;
      resetPackageUploadForm(null);
    }

    function openDeploymentModal(key) {
      const createMode = !key;
      const mod = key ? findCachedMod(key) : null;
      if (!createMode && !mod) {
        alert(t('loadFailed', { message: 'Runtime instance' }));
        return;
      }

      deploymentMode = createMode ? 'create' : 'update';
      deploymentCurrentKey = key || null;
      deploymentTitle.textContent = t(createMode ? 'createDeployment' : 'updateDeployment') +
        (key ? ' - ' + key.dynamicAppId + '/' + key.modId + '/' + key.version : '');
      deploymentSourceKey = key ? { dynamicAppId: key.dynamicAppId, modId: key.modId, version: key.version } : null;
      deploymentSource.innerHTML =
        '<div class="upload-metadata-item"><strong>' + escapeHtml(t('deploymentSource')) + '</strong><span class="mono">' +
        escapeHtml(key ? key.dynamicAppId + '/' + key.modId + '/' + key.version : t('selectPackage')) + '</span></div>';
      const packages = (cachedPackages || []).flatMap(item => (item.versions || []).map(version => ({
        dynamicAppId: version.dynamicAppId || item.dynamicAppId,
        modId: version.modId || item.modId,
        version: version.version,
        entryFile: inferPackageEntryFile(version),
        files: Array.isArray(version.files) ? version.files : [],
        sourceKind: String(version.sourceKind || 'PackageStore')
      }))).filter(item => item.dynamicAppId && item.modId && item.version
        && item.sourceKind.toLowerCase() === 'packagestore'
        && getPackageEntryFiles(item).length > 0);
      deploymentPackageInput.innerHTML = packages.length ? packages.map(item => {
        const value = encodeURIComponent(JSON.stringify(item));
        const selected = key && sameText(item.dynamicAppId, key.dynamicAppId) && sameText(item.modId, key.modId) && !sameText(item.version, key.version)
          ? ' selected' : '';
        return '<option value="' + value + '"' + selected + '>' + escapeHtml(item.dynamicAppId + '/' + item.modId + '/' + item.version) + '</option>';
      }).join('') : '<option value="">' + escapeHtml(t('noDeployablePackages')) + '</option>';
      deploymentPackageInput.disabled = !packages.length;
      if (!createMode && !deploymentPackageInput.value) {
        const first = packages.find(item => sameText(item.dynamicAppId, key.dynamicAppId) && sameText(item.modId, key.modId) && !sameText(item.version, key.version));
        if (first) deploymentPackageInput.value = encodeURIComponent(JSON.stringify(first));
      }
      const current = key && (mod.deployments || []).find(item => sameText(item.deploymentId, key.deploymentId)) || mod;
      const currentEndpoint = current && current.baseAddress || mod && mod.baseAddress || '';
      const port = createMode ? suggestDeploymentPort(mod || {}) : getEndpointPort(currentEndpoint);
      deploymentIdInput.value = createMode ? suggestDeploymentId(mod || {}) : (key.deploymentId || 'default') + '-next';
      deploymentPortInput.value = String(port || 5111);
      deploymentPortInput.readOnly = !createMode;
      deploymentPortInput.setAttribute('aria-readonly', String(!createMode));
      deploymentEndpointInput.value = createMode
        ? 'http://127.0.0.1:' + (port || 5111)
        : (currentEndpoint || 'http://127.0.0.1:' + (port || 5111));
      populateDeploymentEntryOptions(
        deploymentPackageInput.value ? JSON.parse(decodeURIComponent(deploymentPackageInput.value)) : null,
        (current && current.entryFile) || (mod && mod.entryFile) || '');
      deploymentArgumentsInput.value = (current && current.arguments) || (mod && mod.arguments) || '';
      deploymentHealthInput.value = (current && current.healthPath) || (mod && mod.healthPath) || '/Health';
      deploymentTimeoutInput.value = String((current && current.startupTimeoutSeconds) || (mod && mod.startupTimeoutSeconds) || 60);
      setDeploymentAlert('', false);
      deploymentAlertStepOne.hidden = true;
      deploymentAlertStepOne.textContent = '';
        deploymentStep = 1;
      setDeploymentStep(1);
      submitDeploymentButton.disabled = false;
      deploymentModal.hidden = false;
    }

    function syncDeploymentEndpointFromPort() {
      const port = Number(deploymentPortInput.value);
      deploymentEndpointInput.value = Number.isInteger(port) && port > 0 && port <= 65535
        ? 'http://127.0.0.1:' + port
        : '';
    }

    function shellQuote(value) {
      return '"' + String(value || '').replace(/"/g, '\\"') + '"';
    }

    function stripDeploymentUrlsArguments(argumentsText) {
      const tokens = String(argumentsText || '').match(/"[^"]*"|'[^']*'|\S+/g) || [];
      const filtered = [];
      for (let index = 0; index < tokens.length; index++) {
        const token = tokens[index];
        if (token.toLowerCase().startsWith('--urls=')) continue;
        if (token.toLowerCase() === '--urls') {
          index++;
          continue;
        }
        filtered.push(token);
      }
      return filtered.join(' ');
    }

    function buildDeploymentCommand(source, endpoint, entryFile, argumentsText) {
      const displayEntry = entryFile || source.entryFile || '<entry.dll>';
      const commandArguments = stripDeploymentUrlsArguments(argumentsText);
      const normalizedEndpoint = String(endpoint || '').replace(/\\:/g, ':').replace(/\/+$/, '');
      const suffix = '--urls ' + normalizedEndpoint;
      return 'dotnet ' + shellQuote(displayEntry) + (commandArguments ? ' ' + commandArguments : '') + ' ' + suffix;
    }

    function buildDeploymentPayload() {
      const selectedPackage = deploymentPackageInput.value ? JSON.parse(decodeURIComponent(deploymentPackageInput.value)) : null;
      if (!selectedPackage) return null;
      const port = Number(deploymentPortInput.value);
      return {
        source: selectedPackage,
        deploymentId: deploymentIdInput.value.trim(),
        port,
        endpoint: deploymentEndpointInput.value.trim(),
        entryFile: deploymentEntryInput.value.trim(),
        arguments: deploymentArgumentsInput.value.trim(),
        healthPath: deploymentHealthInput.value.trim() || '/Health',
        startupTimeoutSeconds: Number(deploymentTimeoutInput.value) || 60
      };
    }

    function getPackageEntryFiles(packageVersion) {
      return (Array.isArray(packageVersion && packageVersion.files) ? packageVersion.files : [])
        .map(file => {
          const relativePath = String(file && file.relativePath || '').replace(/\\/g, '/');
          const name = String(file && file.name || relativePath.split('/').pop() || '');
          const contentMarker = relativePath.toLowerCase().indexOf('/content/');
          const entryFile = contentMarker >= 0 ? relativePath.slice(contentMarker + '/content/'.length) : name;
          return { entryFile: entryFile.replace(/^\/+/, ''), name, kind: String(file && file.kind || '') };
        })
        .filter(file => file.entryFile && file.name.toLowerCase().endsWith('.dll')
          && (!file.kind || file.kind.toLowerCase() === 'content'))
        .map(file => file.entryFile)
        .filter((entryFile, index, entries) => entries.findIndex(item => sameText(item, entryFile)) === index)
        .sort((left, right) => left.localeCompare(right, undefined, { sensitivity: 'base' }));
    }

    function populateDeploymentEntryOptions(packageVersion, preferredEntryFile) {
      const entryFiles = getPackageEntryFiles(packageVersion);
      deploymentEntryInput.innerHTML = entryFiles.length
        ? entryFiles.map(entryFile => '<option value="' + escapeHtml(entryFile) + '">' + escapeHtml(entryFile) + '</option>').join('')
        : '<option value="">' + escapeHtml(t('noDllEntryFiles')) + '</option>';
      deploymentEntryInput.disabled = !entryFiles.length;
      const preferred = String(preferredEntryFile || '').replace(/\\/g, '/').replace(/^.*\/content\//i, '');
      deploymentEntryInput.value = entryFiles.find(entryFile => sameText(entryFile, preferred)) || entryFiles[0] || '';
    }

    function setDeploymentStep(step) {
      deploymentStep = step === 3 ? 3 : (step === 2 ? 2 : 1);
      const isSecondStep = deploymentStep === 2;
      const isConfirmationStep = deploymentStep === 3;
      deploymentStepOne.hidden = deploymentStep !== 1;
      deploymentStepTwo.hidden = !isSecondStep;
      deploymentStepThree.hidden = !isConfirmationStep;
      backDeploymentStepButton.hidden = deploymentStep !== 2;
      previewDeploymentCommandButton.hidden = deploymentStep !== 2;
      editDeploymentParametersButton.hidden = !isConfirmationStep;
      nextDeploymentStepButton.hidden = deploymentStep !== 1;
      submitDeploymentButton.hidden = !isConfirmationStep;
      if (deploymentStep === 1) {
        deploymentAlert.hidden = true;
        deploymentAlert.textContent = '';
        return;
      }

      if (isConfirmationStep) return;

      const selectedPackage = deploymentPackageInput.value
        ? JSON.parse(decodeURIComponent(deploymentPackageInput.value))
        : null;
      if (!selectedPackage) {
        deploymentAlertStepOne.hidden = false;
        deploymentAlertStepOne.textContent = t('selectPackage');
        setDeploymentStep(1);
        return;
      }

      deploymentAlertStepOne.hidden = true;
      deploymentAlertStepOne.textContent = '';
      deploymentSource.innerHTML =
        '<div class="upload-metadata-item"><strong>' + escapeHtml(t('deploymentSource')) + '</strong><span class="mono">' +
        escapeHtml(selectedPackage.dynamicAppId + '/' + selectedPackage.modId + '/' + selectedPackage.version) + '</span></div>';
      populateDeploymentEntryOptions(selectedPackage, deploymentEntryInput.value);
    }

    function suggestDeploymentId(mod) {
      const deploymentIds = (mod.deployments || []).map(item => String(item.deploymentId || '').toLowerCase());
      let suffix = 1;
      while (deploymentIds.includes('replica-' + suffix)) suffix++;
      return 'replica-' + suffix;
    }

    function getEndpointPort(endpoint) {
      try { return Number(new URL(String(endpoint || '')).port) || 5111; } catch { return 5111; }
    }

    function suggestDeploymentPort(mod) {
      const ports = [];
      const candidates = [mod.baseAddress].concat((mod.deployments || []).map(item => item.baseAddress));
      candidates.forEach(value => {
        try {
          const parsed = new URL(String(value || ''));
          if (parsed.port) ports.push(Number(parsed.port));
        } catch {
        }
      });
      const highest = ports.filter(Number.isFinite).sort((left, right) => right - left)[0];
      return highest && highest < 65535 ? highest + 1 : 5111;
    }

    function buildDeploymentEndpoint(mod, port) {
      try {
        const parsed = new URL(String(mod.baseAddress || ''));
        parsed.port = String(port);
        return parsed.toString().replace(/\/$/, '');
      } catch {
        return 'http://127.0.0.1:' + port;
      }
    }

    function setDeploymentAlert(message, isError) {
      deploymentAlert.hidden = !message;
      deploymentAlert.textContent = message || '';
      deploymentAlert.classList.toggle('danger', isError);
    }

    function closeDeploymentModal() {
      deploymentModal.hidden = true;
      deploymentSourceKey = null;
      deploymentCurrentKey = null;
      deploymentMode = 'create';
      deploymentPackageInput.innerHTML = '';
      deploymentIdInput.value = '';
      deploymentPortInput.value = '';
      deploymentPortInput.readOnly = false;
      deploymentPortInput.setAttribute('aria-readonly', 'false');
      deploymentEndpointInput.value = '';
      deploymentEntryInput.innerHTML = '';
      deploymentEntryInput.disabled = true;
      deploymentArgumentsInput.value = '';
      deploymentHealthInput.value = '/Health';
      deploymentTimeoutInput.value = '60';
      setDeploymentAlert('', false);
      deploymentAlertStepOne.hidden = true;
      deploymentAlertStepOne.textContent = '';
      deploymentStepThree.hidden = true;
      deploymentCommandPreview.textContent = '';
      deploymentConfirmationAlert.hidden = true;
      deploymentConfirmationAlert.textContent = '';
      setDeploymentStep(1);
      submitDeploymentButton.disabled = false;
    }

    async function submitDeployment() {
      const draft = buildDeploymentPayload();
      const source = draft && draft.source;
      if (!source) {
        setDeploymentAlert(t('noDeployablePackages'), true);
        return;
      }
      const deploymentId = draft.deploymentId;
      const port = draft.port;
      const endpoint = draft.endpoint;
      if (!deploymentId) {
        setDeploymentAlert(t('deploymentIdRequired'), true);
        deploymentIdInput.focus();
        return;
      }
      if (!Number.isInteger(port) || port < 1 || port > 65535) {
        setDeploymentAlert(t('deploymentPortRequired'), true);
        deploymentPortInput.focus();
        return;
      }
      if (!deploymentEntryInput.value || !deploymentEntryInput.value.toLowerCase().endsWith('.dll')) {
        setDeploymentAlert(t('noDllEntryFiles'), true);
        deploymentEntryInput.focus();
        return;
      }

      if (deploymentStep !== 3) {
        deploymentCommandPreview.textContent = buildDeploymentCommand(source, endpoint, draft.entryFile, draft.arguments);
        deploymentConfirmationAlert.hidden = true;
        setDeploymentStep(3);
        return;
      }

      submitDeploymentButton.disabled = true;
      setDeploymentAlert(t('loading'), false);
      try {
        const payload = {
          dynamicAppId: source.dynamicAppId,
          modId: source.modId,
          version: source.version,
          deploymentId,
          port,
          endpoint,
          entryFile: draft.entryFile,
          arguments: draft.arguments,
          healthPath: draft.healthPath,
          startupTimeoutSeconds: draft.startupTimeoutSeconds
        };
        const request = deploymentMode === 'update'
          ? Object.assign({}, payload, {
            currentDynamicAppId: deploymentCurrentKey.dynamicAppId,
            currentModId: deploymentCurrentKey.modId,
            currentVersion: deploymentCurrentKey.version,
            currentDeploymentId: deploymentCurrentKey.deploymentId || 'default'
          })
          : Object.assign({}, payload, { start: true });
        const result = await api(deploymentMode === 'update' ? '/deployments/update' : '/deployments/create', {
          method: 'POST',
          contentType: 'application/json',
          body: JSON.stringify(request)
        });
        closeDeploymentModal();
        alert(result && result.message ? result.message : t(deploymentMode === 'update' ? 'updateSucceeded' : 'deploymentCreated'));
        await loadApplications();
      } catch (error) {
        setDeploymentAlert(error.message, true);
      } finally {
        submitDeploymentButton.disabled = false;
      }
    }

    function resetPackageUploadForm(key) {
      document.getElementById('uploadPackage').value = '';
      document.getElementById('uploadVersion').value = '';
      document.getElementById('uploadApp').value = key && key.dynamicAppId || '';
      document.getElementById('uploadSummary').textContent = t('noPackageSelected');
      uploadMetadataSummary.innerHTML = key
        ? '<div class="upload-metadata-item"><strong>' + escapeHtml(t('selectedApplication')) + '</strong><span class="mono">' + escapeHtml(key.dynamicAppId + '/' + key.modId) + '</span></div>'
        : '';
      uploadMetadataSummary.hidden = !key;
      uploadResultLog.textContent = '';
      uploadResultLog.hidden = true;
      packageUploadTitle.textContent = t('uploadPackage');
      submitUploadButton.disabled = false;
      submitUploadButton.textContent = t('uploadAction');
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
      const sequence = ++requestSequences.archives;
      archiveList.innerHTML = '<div class="muted">' + escapeHtml(t('loading')) + '</div>';
      const result = await api('/archived');
      if (sequence !== requestSequences.archives) return;
      cachedArchives = result && result.data ? result.data : [];
      renderArchives(cachedArchives);
      renderPackages(cachedPackages);
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
          const target = Object.assign({}, key);
          if (record.deploymentId) target.deploymentId = record.deploymentId;
          const encoded = encodeURIComponent(JSON.stringify(target));
          const isInstanceArchive = Boolean(record.deploymentId);
          const archiveType = isInstanceArchive ? t('archiveInstance') : t('archiveVersion');
          const archiveTarget = key.dynamicAppId + ' / ' + key.modId + ' / ' + key.version +
            (record.deploymentId ? ' / ' + record.deploymentId : '');
          return '<div class="card">' +
            '<div class="row"><div><h3>' + escapeHtml(key.dynamicAppId + ' / ' + key.modId) + '</h3>' +
            '<div><span class="mono">' + escapeHtml(key.version) + '</span> <span class="badge bad">' + escapeHtml(archiveType) + '</span>' +
            (record.deploymentId ? ' <span class="badge">' + escapeHtml(record.deploymentId) + '</span>' : '') + '</div>' +
            '<div class="muted">' + escapeHtml(t('archiveReason')) + ': ' + escapeHtml(record.reason || '') + '</div>' +
            '<div class="muted">' + escapeHtml(t('archivedAt')) + ': ' + escapeHtml(formatDateTime(record.archivedAtUtc)) + '</div>' +
            (record.archiveDirectory ? '<div class="muted">' + escapeHtml(t('archiveFiles')) + ': <span class="mono">' + escapeHtml(record.archiveDirectory) + '</span></div>' : '') +
            '</div>' +
            '<div class="toolbar"><button class="close-action" data-action="unarchive" data-key="' + encoded + '"><span aria-hidden="true">↩</span> ' + escapeHtml(t('unarchive')) + '</button>' +
            '<button class="danger" data-action="delete-archive" data-key="' + encoded + '">' + escapeHtml(t('deleteArchive')) + '</button></div></div>' +
            '</div>';
        }).join('');
    }

    function renderArchivesError(message) {
      archiveList.innerHTML = '<div class="muted">' + escapeHtml(t('loadFailed', { message })) + '</div>';
    }

    async function loadOrphans() {
      if (!isAuthorized) return;
      orphanList.innerHTML = '<div class="muted">' + escapeHtml(t('loading')) + '</div>';
      const result = await api('/deployments/orphans');
      cachedOrphans = result && result.data ? result.data : [];
      renderOrphans(cachedOrphans);
    }

    function renderOrphans(orphans) {
      if (!orphanList) return;
      if (!orphans || !orphans.length) {
        orphanList.innerHTML = '<div class="muted">' + escapeHtml(t('noOrphans')) + '</div>';
        return;
      }

      orphanList.innerHTML = orphans.slice()
        .sort((left, right) => {
          const leftKey = normalizeArchiveKey(left.key || {});
          const rightKey = normalizeArchiveKey(right.key || {});
          return (leftKey.dynamicAppId + '/' + leftKey.modId + '/' + leftKey.version + '/' + (left.deploymentId || ''))
            .localeCompare(rightKey.dynamicAppId + '/' + rightKey.modId + '/' + rightKey.version + '/' + (right.deploymentId || ''), undefined, { sensitivity: 'base' });
        })
        .map(orphan => {
          const key = normalizeArchiveKey(orphan.key || {});
          const target = Object.assign({}, key, { deploymentId: orphan.deploymentId || 'default' });
          const encoded = encodeURIComponent(JSON.stringify(target));
          return '<div class="card">' +
            '<div class="row"><div>' +
            '<h3>' + escapeHtml(key.dynamicAppId + ' / ' + key.modId) + '</h3>' +
            '<div><span class="mono">' + escapeHtml(key.version) + '</span> <span class="badge warn">' + escapeHtml(orphan.deploymentId || '-') + '</span></div>' +
            '<div class="muted">' + escapeHtml(t('orphanDirectory')) + ': <span class="mono">' + escapeHtml(orphan.deploymentDirectory || '') + '</span></div>' +
            '<div class="muted">' + escapeHtml(t('lastModified')) + ': ' + escapeHtml(formatDateTime(orphan.lastModifiedUtc)) + '</div>' +
            '</div><div class="toolbar"><button data-action="remove-orphan" data-key="' + encoded + '" class="danger">' + escapeHtml(t('removeOrphan')) + '</button></div></div>' +
            '</div>';
        }).join('');
    }

    function renderOrphansError(message) {
      orphanList.innerHTML = '<div class="muted">' + escapeHtml(t('loadFailed', { message })) + '</div>';
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
      if (!currentRuntimeApplicationId || !apps.some(app => sameText(app.dynamicAppId, currentRuntimeApplicationId))) {
        currentRuntimeApplicationId = apps.length ? apps[0].dynamicAppId || '' : '';
      }
      document.getElementById('navPackageAppCount').textContent = '(' + apps.length + ')';
      renderRuntimeApplicationList(apps);
      renderApplications(apps);
    }

    function renderRuntimeApplicationList(apps) {
      if (!runtimeApplicationList) return;
      if (!apps || !apps.length) {
        runtimeApplicationList.innerHTML = '<div class="muted">' + escapeHtml(t('noApplications')) + '</div>';
        return;
      }

      runtimeApplicationList.innerHTML = apps.slice()
        .sort((left, right) => String(left.dynamicAppId || '').localeCompare(String(right.dynamicAppId || ''), undefined, { sensitivity: 'base' }))
        .map(app => {
          const active = sameText(app.dynamicAppId, currentRuntimeApplicationId) ? ' active' : '';
          return '<button class="application-item' + active + '" role="tab" aria-selected="' + (active ? 'true' : 'false') + '" data-runtime-app-id="' + escapeHtml(app.dynamicAppId || '') + '">' +
            '<strong>' + escapeHtml(app.dynamicAppId || '') + '</strong>' +
            '</button>';
        }).join('');
    }

    function renderApplications(apps) {
      if (!apps.length) {
        appsNode.innerHTML = '<div class="muted">' + escapeHtml(t('noApplications')) + '</div>';
        return;
      }
      const app = apps.find(item => sameText(item.dynamicAppId, currentRuntimeApplicationId)) || apps[0];
      currentRuntimeApplicationId = app.dynamicAppId || '';
      renderRuntimeApplicationList(apps);
      const services = groupAllMods(app);
      const modCount = Array.isArray(app.mods) ? app.mods.length : 0;
      const activeCount = Number(app.activeModCount || 0);
      const instanceCount = services.reduce((count, service) =>
        count + service.versions.reduce((versionCount, mod) =>
          versionCount + Math.max(1, Array.isArray(mod.deployments) ? mod.deployments.length : 0), 0), 0);
      appsNode.innerHTML = '<div class="runtime-application-detail">' +
        '<div class="service-list">' + services.map(service => renderRuntimeServiceRow(service)).join('') + '</div>' +
        '<div class="runtime-application-summary" aria-label="' + escapeHtml(t('runtimeSummary')) + '">' +
        '<span class="badge">' + escapeHtml(modCount + ' ' + t('mods')) + '</span>' +
        '<span class="badge ok">' + escapeHtml(activeCount + ' ' + t('active')) + '</span>' +
        '<span class="badge">' + escapeHtml(t('runtimeInstances', { count: instanceCount })) + '</span>' +
        '</div>' +
        '</div>';
    }

    function renderRuntimeServiceRow(service) {
      const versions = service.versions.slice().sort((left, right) => compareCurrentVersionPriority(right) - compareCurrentVersionPriority(left));
      const activeVersion = versions.find(version =>
        normalizeState(version.state) === 'active'
        && String(version.healthStatus || '').toLowerCase() === 'healthy');
      return '<div class="service-row">' +
        '<div class="service-row-header"><div class="service-row-title"><strong>' + escapeHtml(service.modId || '') + '</strong>' +
        (activeVersion ? '<span class="badge ok">' + escapeHtml(activeVersion.version || '') + '</span>' : '<span class="badge warn">' + escapeHtml(t('routeInactive')) + '</span>') +
        '</div></div>' +
        '<div class="instance-grid">' + versions.map(mod => renderTargetVersionCards(mod)).join('') + '</div>' +
        '</div>';
    }

    function renderTargetVersionCards(mod) {
      const deployments = Array.isArray(mod.deployments) && mod.deployments.length
        ? mod.deployments
        : [{
          deploymentId: mod.deploymentId || 'default',
          baseAddress: mod.baseAddress,
          state: mod.state,
          healthStatus: mod.healthStatus,
          instanceId: mod.instanceId,
          lastStatusMessage: mod.lastStatusMessage,
          updatedAtUtc: mod.updatedAtUtc,
          selfHealing: mod.selfHealing
        }];
      return deployments.map(deployment => {
        const displayMod = Object.assign({}, mod, deployment, { deploymentId: deployment.deploymentId || 'default' });
        const state = displayMod.state || 'None';
        const health = displayMod.healthStatus || 'None';
        const badge = health === 'Healthy' ? 'ok' : (health === 'Unhealthy' || state === 'Failed' ? 'bad' : 'warn');
        const key = encodeRuntimeKey(displayMod, displayMod.deploymentId);
        return '<div class="instance-card">' +
          '<div class="instance-card-header"><div class="instance-card-title"><strong>' + escapeHtml(mod.version || '') + '</strong></div>' +
          renderInstanceActions(key, displayMod, { isCurrent: normalizeState(mod.state) === 'active', history: [] }) +
          '</div>' +
          '<div class="instance-card-meta"><div>' + escapeHtml(displayMod.baseAddress || '-') + '</div></div>' +
          '<div class="instance-card-footer"><span class="badge ' + badge + '">' + escapeHtml(state) + ' / ' + escapeHtml(health) + '</span>' +
          '<div class="instance-footer-actions">' + renderInstanceDirectActions(key, state, displayMod) + '</div></div>' +
          '</div>';
      }).join('');
    }

    function renderInstanceActions(key, mod, options = {}) {
      const state = normalizeState(mod.state);
      const health = String(mod.healthStatus || '').toLowerCase();
      const isExistingContainer = isExistingContainerMode(mod);
      const menuActions = [];
      if (!options.isCurrent) {
        menuActions.push('<button data-action="activate" data-key="' + key + '">' + escapeHtml(t('activate')) + '</button>');
      }
      if (options.isCurrent && (options.history || []).length && !isExistingContainer) {
        menuActions.push('<button data-action="rollback" data-key="' + key + '">' + escapeHtml(t('rollback')) + '</button>');
      }
      if (mod.selfHealing) {
        menuActions.push('<button data-action="reset-healing" data-key="' + key + '">' + escapeHtml(t('resetSelfHealing')) + '</button>');
      }
      const destroyDisabled = state !== 'stopped';
      menuActions.push('<button data-action="destroy" data-key="' + key + '" class="danger"' +
        (destroyDisabled ? ' disabled title="' + escapeHtml(t('destroyRequiresStopped')) + '"' : '') + '>' +
        escapeHtml(t('destroy')) + '</button>');
      if (mod.hasManifest && (state === 'failed' || health === 'unhealthy' || !options.isCurrent)) {
        menuActions.push('<button data-action="archive" data-key="' + key + '" class="danger">' + escapeHtml(t('archive')) + '</button>');
      }
      if (!isExistingContainer) {
        menuActions.push('<button data-action="health" data-key="' + key + '">' + escapeHtml(t('health')) + '</button>');
        if (String(mod.executionMode || '').toLowerCase() === 'process') {
          menuActions.push('<button data-action="process-logs" data-key="' + key + '">' + escapeHtml(t('processLogs')) + '</button>');
        }
      }
      menuActions.push('<button data-action="details" data-key="' + key + '">' + escapeHtml(t('details')) + '</button>');
      const menu = '<div class="instance-action-menu">' +
        '<button class="instance-action-trigger" type="button" data-action="toggle-instance-menu" aria-label="' + escapeHtml(t('moreActions')) + '" title="' + escapeHtml(t('moreActions')) + '">...</button>' +
        '<div class="instance-action-menu-list" hidden>' + menuActions.join('') + '</div>' +
        '</div>';
      return '<div class="instance-card-header-actions"><span class="badge instance-deployment-badge">' + escapeHtml(mod.deploymentId || 'default') + '</span>' + menu + '</div>';
    }

    function renderInstanceDirectActions(key, state, mod) {
      state = normalizeState(state);
      const startDisabled = ['active', 'starting', 'draining'].includes(state);
      const stopDisabled = !['active', 'starting'].includes(state);
      const restartDisabled = state !== 'active';
      return '<button class="instance-action-link" data-action="start" data-key="' + key + '"' + (startDisabled ? ' disabled' : '') + '>' + escapeHtml(t('start')) + '</button>' +
        '<button class="instance-action-link danger" data-action="stop" data-key="' + key + '"' + (stopDisabled ? ' disabled' : '') + '>' + escapeHtml(t('stop')) + '</button>' +
        '<button class="instance-action-link" data-action="restart" data-key="' + key + '"' + (restartDisabled ? ' disabled' : '') + '>' + escapeHtml(t('restart')) + '</button>' +
        '<button class="instance-action-link" data-action="update" data-key="' + key + '">' + escapeHtml(t('updateDeployment')) + '</button>';
    }

    function renderRoutes(routes) {
      if (!routeList || !routeSummary) return;
      cachedRoutes = Array.isArray(routes) ? routes : [];
      routeSummary.dataset.count = String(cachedRoutes.length);
      routeSummary.textContent = String(cachedRoutes.length) + ' ' + (currentLanguage === 'zh-CN' ? '条规则' : 'route(s)');
      document.getElementById('navRouteCount').textContent = '(' + cachedRoutes.length + ')';
      if (!cachedRoutes.length) {
        routeList.innerHTML = '<div class="muted">' + escapeHtml(t('noRoutes')) + '</div>';
        return;
      }

      routeList.innerHTML = cachedRoutes.slice().sort((left, right) => Number(left.order || 0) - Number(right.order || 0)).map(route => {
        const status = route.enabled === false ? t('routeDisabled') : (route.lastStatusMessage || t('routeEnabled'));
        const badge = route.enabled === false ? 'warn' : 'ok';
        const key = encodeURIComponent(String(route.routeId || ''));
        return '<div class="route-card">' +
          '<div class="route-card-header"><div class="service-row-title"><strong>' + escapeHtml(route.name || route.routeId || '') + '</strong>' +
          '<span class="badge">' + escapeHtml(route.routeId || '') + '</span></div>' +
          '<span class="badge ' + badge + '">' + escapeHtml(status) + '</span></div>' +
          '<div class="route-card-grid">' +
          renderRouteField('upstreamTemplate', route.upstreamPathTemplate) +
          renderRouteField('downstreamTemplate', route.downstreamPathTemplate) +
          renderRouteField('routeOrder', route.order) +
          renderRouteField('targetDynamicAppId', route.targetDynamicAppId) +
          renderRouteField('targetModId', route.targetModId) +
          renderRouteField('targetVersion', route.targetVersion || (route.targetDeploymentId ? t('targetVersionImplied') : t('targetVersionAutomatic'))) +
          renderRouteField('targetDeploymentId', route.targetDeploymentId || t('targetInstanceAutomatic')) +
          renderRouteField('methods', Array.isArray(route.upstreamHttpMethods) && route.upstreamHttpMethods.length ? route.upstreamHttpMethods.join(', ') : '*') +
          '</div>' +
          '<div class="toolbar actions"><button class="primary" data-action="edit-route" data-route-id="' + key + '"><span class="route-edit-icon" aria-hidden="true">&#9998;</span> ' + escapeHtml(t('editRouteAction')) + '</button>' +
          '<button data-action="toggle-route" data-route-id="' + key + '">' + escapeHtml(route.enabled === false ? t('enableRoute') : t('disableRoute')) + '</button>' +
          '<button class="danger" data-action="delete-route" data-route-id="' + key + '">' + escapeHtml(t('deleteRoute')) + '</button></div>' +
          '</div>';
      }).join('');
    }

    function renderRouteField(label, value) {
      return '<div class="route-card-field"><strong>' + escapeHtml(t(label)) + '</strong><span class="mono">' + escapeHtml(value || '-') + '</span></div>';
    }


    function renderRouteInstanceCard(mod, deployment) {
      const state = deployment.state || 'None';
      const health = deployment.healthStatus || 'None';
      const forwardable = normalizeState(state) === 'active' && String(health).toLowerCase() === 'healthy';
      const badge = forwardable ? 'ok' : (String(health).toLowerCase() === 'unhealthy' || normalizeState(state) === 'failed' ? 'bad' : 'warn');
      return '<div class="instance-card">' +
        '<div class="instance-card-header"><div class="instance-card-title"><strong>' + escapeHtml(t('instance')) + '</strong><span class="badge">' + escapeHtml(deployment.deploymentId || 'default') + '</span></div>' +
        '</div>' +
        '<div class="instance-card-meta"><div>' + escapeHtml(t('activeVersion')) + ': ' + escapeHtml(mod.version || '-') + '</div><div>' + escapeHtml(deployment.baseAddress || '-') + '</div></div>' +
        '<div class="instance-card-footer"><span class="badge ' + badge + '">' + escapeHtml(forwardable ? t('routeReady') : state + ' / ' + health) + '</span></div>' +
        '</div>';
    }

    function groupAllMods(app) {
      const map = new Map();
      (app.mods || []).forEach(mod => {
        const key = String(mod.modId || '').toLowerCase();
        if (!map.has(key)) map.set(key, []);
        map.get(key).push(mod);
      });
      return Array.from(map.entries()).map(([key, versions]) => ({
        modId: versions[0].modId || key,
        versions
      })).sort((left, right) => String(left.modId).localeCompare(String(right.modId), undefined, { sensitivity: 'base' }));
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
      const history = options.history || [];
      const key = encodeRuntimeKey(mod, mod.deploymentId || 'default');
      const historyButton = options.isCurrent && history.length && !isExistingContainerMode(mod)
        ? '<button data-action="history" data-key="' + key + '">' + escapeHtml(t('viewHistory')) + ' (' + history.length + ')</button>'
        : '';
      const deploymentButton = options.isCurrent
        && String(mod.executionMode || '').toLowerCase() === 'process'
        ? '<button data-action="create-deployment" data-key="' + key + '">' + escapeHtml(t('createDeployment')) + '</button>'
        : '';
      const deployments = Array.isArray(mod.deployments) ? mod.deployments : [];
      const deploymentRows = deployments.length
        ? deployments.map(deployment => renderModDeployment(mod, deployment, Object.assign({}, options, { isDeployment: true, history: [] }))).join('')
        : renderModDeployment(mod, {
          deploymentId: mod.deploymentId || 'default',
          baseAddress: mod.baseAddress,
          state: mod.state,
          healthStatus: mod.healthStatus,
          instanceId: mod.instanceId,
          lastStatusMessage: mod.lastStatusMessage,
          updatedAtUtc: mod.updatedAtUtc,
          selfHealing: mod.selfHealing
        }, options);
      return '<div class="row mod-row mod-version-row">' +
        '<div><strong>' + escapeHtml(mod.modId || '') + '</strong> <span class="mono">' + escapeHtml(mod.version || '') + '</span> <span class="badge">' + escapeHtml(options.isCurrent ? t('currentVersion') : t('historyVersions')) + '</span></div>' +
        '<div class="toolbar">' + deploymentButton + historyButton + '</div></div>' +
        deploymentRows;
    }

    function encodeRuntimeKey(mod, deploymentId) {
      return encodeURIComponent(JSON.stringify({
        dynamicAppId: mod.dynamicAppId,
        modId: mod.modId,
        version: mod.version,
        deploymentId: deploymentId || 'default'
      }));
    }

    function renderModDeployment(mod, deployment, options = {}) {
      const deploymentId = deployment.deploymentId || 'default';
      const displayMod = Object.assign({}, mod, deployment, { deploymentId });
      const state = displayMod.state || 'None';
      const health = displayMod.healthStatus || 'None';
      const badge = health === 'Healthy' ? 'ok' : (health === 'Unhealthy' || state === 'Failed' ? 'bad' : 'warn');
      const healthLine = isExistingContainerMode(displayMod)
        ? ''
        : '<div class="muted">' + escapeHtml(t('health')) + ': ' + escapeHtml(displayMod.healthPath || '') + '</div>';
      const key = encodeRuntimeKey(displayMod, deploymentId);
      return '<div class="row mod-row mod-deployment-row">' +
        '<div><div><strong>' + escapeHtml(mod.modId || '') + '</strong> <span class="mono">' + escapeHtml(mod.version || '') + '</span> <span class="badge">' + escapeHtml(deploymentId) + '</span></div>' +
        '<div class="muted">' + escapeHtml(displayMod.baseAddress || '') + '</div>' +
        healthLine +
        '<div class="muted">' + escapeHtml(displayMod.lastStatusMessage || '') + '</div>' +
        renderSelfHealing(displayMod.selfHealing) + '</div>' +
        '<div class="toolbar">' +
        '<span class="badge ' + badge + '">' + escapeHtml(state) + ' / ' + escapeHtml(health) + '</span>' +
        renderActionButtons(key, displayMod, options) +
        '</div></div>';
    }

    function renderActionButtons(key, mod, options) {
      const state = normalizeState(mod.state);
      const health = String(mod.healthStatus || '').toLowerCase();
      const isExistingContainer = isExistingContainerMode(mod);
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
      const destroyDisabled = state !== 'stopped';
      buttons.push('<button data-action="destroy" data-key="' + key + '" class="danger"' +
        (destroyDisabled ? ' disabled title="' + escapeHtml(t('destroyRequiresStopped')) + '"' : '') + '>' +
        escapeHtml(t('destroy')) + '</button>');
      if (mod.hasManifest && (state === 'failed' || health === 'unhealthy' || !options.isCurrent)) {
        buttons.push('<button data-action="archive" data-key="' + key + '" class="danger">' + escapeHtml(t('archive')) + '</button>');
      }
      if (!isExistingContainer) {
        buttons.push('<button data-action="health" data-key="' + key + '">' + escapeHtml(t('health')) + '</button>');
        buttons.push('<button data-action="route-test"' + routeAttrs + '>' + escapeHtml(t('routeTest')) + '</button>');
        if (String(mod.executionMode || '').toLowerCase() === 'process') {
          buttons.push('<button data-action="process-logs" data-key="' + key + '">' + escapeHtml(t('processLogs')) + '</button>');
        }
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
      const menuTrigger = event.target.closest('[data-action="toggle-instance-menu"]');
      if (menuTrigger) {
        event.stopPropagation();
        const menu = menuTrigger.parentElement.querySelector('.instance-action-menu-list');
        document.querySelectorAll('.instance-action-menu-list').forEach(node => {
          if (node !== menu) node.hidden = true;
        });
        if (menu) {
          const opening = menu.hidden;
          menu.hidden = !opening;
          if (opening) {
            const triggerRect = menuTrigger.getBoundingClientRect();
            const gap = 4;
            const viewportPadding = 8;
            const menuWidth = menu.offsetWidth;
            const menuHeight = menu.offsetHeight;
            let left = triggerRect.right - menuWidth;
            let top = triggerRect.bottom + gap;
            if (left < viewportPadding) left = viewportPadding;
            if (left + menuWidth > window.innerWidth - viewportPadding) {
              left = Math.max(viewportPadding, window.innerWidth - menuWidth - viewportPadding);
            }
            if (top + menuHeight > window.innerHeight - viewportPadding && triggerRect.top - menuHeight - gap >= viewportPadding) {
              top = triggerRect.top - menuHeight - gap;
            }
            menu.style.left = left + 'px';
            menu.style.top = top + 'px';
          }
        }
        return;
      }
      if (!event.target.closest('.instance-action-menu')) {
        document.querySelectorAll('.instance-action-menu-list').forEach(node => {
          node.hidden = true;
        });
      }
      const button = event.target.closest('button[data-action]');
      if (!button) return;
      const action = button.dataset.action;
      if (action === 'toggle-instance-menu') return;
      try {
        button.disabled = true;
        if (action === 'edit-route') {
          const routeId = decodeURIComponent(button.dataset.routeId || '');
          const route = cachedRoutes.find(item => sameText(item.routeId, routeId));
          if (route) openDynamicRouteModal(route);
        } else if (action === 'history') {
          const key = JSON.parse(decodeURIComponent(button.dataset.key));
          showHistory(key);
        } else if (action === 'create-deployment') {
          const key = JSON.parse(decodeURIComponent(button.dataset.key));
          openDeploymentModal(key);
        } else if (action === 'update') {
          const key = JSON.parse(decodeURIComponent(button.dataset.key));
          openDeploymentModal(key);
        } else if (action === 'toggle-route') {
          const routeId = decodeURIComponent(button.dataset.routeId || '');
          const route = cachedRoutes.find(item => sameText(item.routeId, routeId));
          if (!route) return;
          await api('/routes/' + encodeURIComponent(routeId) + '/enabled', {
            method: 'PUT',
            contentType: 'application/json',
            body: JSON.stringify({ enabled: route.enabled === false })
          });
          await loadRoutesView();
        } else if (action === 'delete-route') {
          const routeId = decodeURIComponent(button.dataset.routeId || '');
          if (!confirm(t('confirmDeleteRoute', { target: routeId }))) return;
          await api('/routes/' + encodeURIComponent(routeId), { method: 'DELETE' });
          await loadRoutesView();
        } else if (action === 'process-logs') {
          const key = JSON.parse(decodeURIComponent(button.dataset.key));
          await openProcessLogModal(key);
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
        } else if (action === 'destroy') {
          const key = JSON.parse(decodeURIComponent(button.dataset.key));
          await executeDestroy(key);
        } else if (action === 'unarchive') {
          const key = JSON.parse(decodeURIComponent(button.dataset.key));
          await executeUnarchive(key);
        } else if (action === 'delete-archive') {
          const key = JSON.parse(decodeURIComponent(button.dataset.key));
          await executeDeleteArchive(key);
        } else if (action === 'remove-orphan') {
          const key = JSON.parse(decodeURIComponent(button.dataset.key));
          const target = key.dynamicAppId + '/' + key.modId + '/' + key.version + '/' + key.deploymentId;
          if (!confirm(t('confirmRemoveOrphan', { target }))) return;
          const result = await api('/deployments/remove', jsonPost(key));
          alert(result && result.message ? result.message : t('orphanRemoved'));
          await Promise.all([
            loadOrphans(),
            loadApplications()
          ]);
        } else if (action === 'package-detail') {
          const key = JSON.parse(decodeURIComponent(button.dataset.key));
          showPackageDetails(key);
        } else if (action === 'package-history') {
          const key = JSON.parse(decodeURIComponent(button.dataset.key));
          showPackageHistory(key);
        } else if (action === 'package-upload') {
          const key = JSON.parse(decodeURIComponent(button.dataset.key));
          openPackageUploadModal(key);
        } else if (action === 'delete-package-mod') {
          const key = JSON.parse(decodeURIComponent(button.dataset.key));
          await executeDeleteService(key);
        } else if (action === 'delete-package-version') {
          const key = JSON.parse(decodeURIComponent(button.dataset.key));
          await executeDeletePackageVersion(key);
        } else if (action === 'edit-file') {
          await openFileEditor(button.dataset.path || '');
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

    async function executeDestroy(key) {
      const target = key.dynamicAppId + '/' + key.modId + '/' + key.version + '/' + (key.deploymentId || 'default');
      if (!confirm(t('confirmDestroy', { target }))) return;
      const result = await api('/deployments/destroy', jsonPost(key));
      await Promise.all([
        loadApplications(),
        loadArchives(),
        loadOrphans()
      ]);
      alert(result && result.message ? result.message : t('destroy'));
    }

    async function executeUnarchive(key) {
      if (!confirmDanger('unarchive', key)) return;
      await api('/unarchive', jsonPost(key));
      await loadArchives();
      await loadApplications();
    }

    async function executeDeleteArchive(key) {
      const target = key.dynamicAppId + '/' + key.modId + '/' + key.version +
        (key.deploymentId ? '/' + key.deploymentId : '');
      if (!confirm(t('confirmDeleteArchive', { target }))) return;
      await api('/archived/remove', jsonPost(key));
      await Promise.all([
        loadArchives(),
        loadApplications(),
        loadPackages()
      ]);
    }

    async function executeDeleteService(key) {
      const target = key.dynamicAppId + '/' + key.modId;
      if (hasServiceArchives(key)) {
        alert(t('serviceHasArchives'));
        return;
      }
      if (!confirm(t('confirmDeleteService', { target }))) return;
      await api('/packages/mods/remove', jsonPost(key));
      await Promise.all([
        loadPackageApplications(),
        loadPackages(),
        loadApplications()
      ]);
    }

    async function executeDeletePackageVersion(key) {
      if (isPackageVersionUsed(key)) {
        alert(t('packageInUse'));
        return;
      }
      const target = key.dynamicAppId + '/' + key.modId + '/' + key.version;
      if (!confirm(t('confirmDeletePackage', { target }))) return;
      await api('/packages/versions/remove', jsonPost(key));
      closeHistory();
      await Promise.all([loadPackages(), loadPackageApplications(), loadApplications()]);
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
      const path = buildTargetPath(key);
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
      const runtimeSuffix = key.deploymentId && key.deploymentId !== 'default' ? '/' + key.deploymentId : '';
      detailModalTitle.textContent = t('details') + ' - ' + key.dynamicAppId + '/' + key.modId + '/' + key.version + runtimeSuffix;
      detailTargetLog.textContent = t('loading');
      detailManifestLog.textContent = t('loading');
      detailManagedLogs.textContent = t('loading');
      detailRuntimeFiles.innerHTML = '<div class="muted">' + escapeHtml(t('loading')) + '</div>';
      detailModal.hidden = false;

      const target = await loadTargetSection(key);
      detailTargetLog.textContent = JSON.stringify(target, null, 2);

      const manifest = await loadManifestSection(key);
      detailManifestLog.textContent = JSON.stringify(manifest, null, 2);

      const logs = await loadManagedLogsSection(key);
      detailManagedLogs.textContent = JSON.stringify(logs, null, 2);

      const files = await loadRuntimeFilesSection(key);
      renderRuntimeFiles(files);
    }

    function closeDetailModal() {
      detailModal.hidden = true;
      detailTargetLog.textContent = '';
      detailManifestLog.textContent = '';
      detailManagedLogs.textContent = '';
      detailRuntimeFiles.innerHTML = '';
    }

    async function openProcessLogModal(key) {
      closeProcessLogModal();
      processLogTargetKey = key;
      const runtimeSuffix = key.deploymentId && key.deploymentId !== 'default' ? '/' + key.deploymentId : '';
      processLogTitle.textContent = t('processLogs') + ' - ' + key.dynamicAppId + '/' + key.modId + '/' + key.version + runtimeSuffix;
      processLogStatus.textContent = t('processLogNotRead');
      processLogModal.hidden = false;
      updateProcessLogReadingControls();
    }

    async function queryProcessLogs() {
      if (!processLogTargetKey) return false;
      const requestVersion = processLogRequestVersion;
      processLogStatus.textContent = t('processLogQuerying');
      processLogQueryButton.disabled = true;
      try {
        const result = await api('/logs', jsonPost(processLogTargetKey));
        if (!processLogTargetKey || requestVersion !== processLogRequestVersion) return false;
        processLogEntries = Array.isArray(result && result.data)
          ? result.data.slice(-maxProcessLogLines)
          : [];
        renderProcessLogs();
        processLogStatus.textContent = t('processLogQueryCompleted', { count: processLogEntries.length });
        return true;
      } catch (error) {
        if (processLogTargetKey && requestVersion === processLogRequestVersion) {
          processLogStatus.textContent = t('processLogFailed', { message: error.message });
          processLogOutput.textContent = t('processLogFailed', { message: error.message });
        }
        return false;
      } finally {
        if (processLogTargetKey && requestVersion === processLogRequestVersion) {
          updateProcessLogReadingControls();
        }
      }
    }

    async function toggleProcessLogReading() {
      if (!processLogTargetKey) return;
      if (processLogReading) {
        stopProcessLogReading(true);
        return;
      }

      processLogReading = true;
      updateProcessLogReadingControls();
      connectProcessLog(processLogTargetKey);
    }

    function connectProcessLog(key) {
      closeProcessLogSocket();
      const scheme = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
      const query = controlPlaneTicket
        ? '?' + encodeURIComponent(controlPlaneQueryName) + '=' + encodeURIComponent(controlPlaneTicket)
        : '';
      const socket = new WebSocket(scheme + '//' + window.location.host + processLogHubPath + query);
      processLogSocket = socket;
      let handshakeComplete = false;
      let invocationSent = false;
      socket.onopen = () => {
        if (processLogSocket !== socket || !processLogReading) return;
        processLogStatus.textContent = t('processLogConnecting');
        socket.send(JSON.stringify({ protocol: 'json', version: 1 }) + '\u001e');
      };
      socket.onmessage = event => {
        const messages = String(event.data || '').split('\u001e').filter(Boolean);
        messages.forEach(message => {
          let payload;
          try {
            payload = JSON.parse(message);
          } catch {
            return;
          }

          if (!handshakeComplete && !payload.type) {
            if (payload.error) {
              processLogStatus.textContent = t('processLogFailed', { message: payload.error });
              return;
            }
            handshakeComplete = true;
            if (!invocationSent && processLogSocket === socket && processLogReading) {
              invocationSent = true;
              socket.send(JSON.stringify({
                type: 1,
                invocationId: 'process-log-stream',
                target: 'Stream',
                arguments: [key.dynamicAppId, key.modId, key.version, key.deploymentId || 'default']
              }) + '\u001e');
              processLogStatus.textContent = t('processLogConnected');
              processLogSocketPingTimer = window.setInterval(() => {
                if (socket.readyState === WebSocket.OPEN) {
                  socket.send(JSON.stringify({ type: 6 }) + '\u001e');
                }
              }, 15000);
            }
            return;
          }

          if (payload.type === 1 && payload.target === 'processLog' && processLogReading) {
            appendProcessLog(payload.arguments && payload.arguments[0]);
          } else if (payload.type === 1 && payload.target === 'processLogReady') {
            processLogStatus.textContent = t('processLogConnected');
          } else if (payload.type === 3 && payload.error) {
            processLogStatus.textContent = t('processLogFailed', { message: payload.error });
          }
        });
      };
      socket.onerror = () => {
        if (processLogSocket === socket) {
          processLogReading = false;
          processLogStatus.textContent = t('processLogFailed', { message: 'WebSocket' });
          updateProcessLogReadingControls();
        }
      };
      socket.onclose = () => {
        if (processLogSocket === socket) {
          processLogSocket = null;
          if (processLogSocketPingTimer) {
            window.clearInterval(processLogSocketPingTimer);
            processLogSocketPingTimer = null;
          }
          processLogReading = false;
          processLogStatus.textContent = t('processLogDisconnected');
          updateProcessLogReadingControls();
        }
      };
    }

    function appendProcessLog(entry) {
      if (!entry) return;
      processLogEntries.push(entry);
      if (processLogEntries.length > maxProcessLogLines) {
        processLogEntries = processLogEntries.slice(-maxProcessLogLines);
      }
      renderProcessLogs();
    }

    function formatProcessLogEntry(entry) {
      const timestamp = formatDateTime(entry.timestampUtc || '');
      return '[' + timestamp + '] [' + String(entry.stream || 'log') + '] ' + String(entry.message || '');
    }

    function renderProcessLogs() {
      const query = String(processLogQueryInput.value || '').trim().toLowerCase();
      const entries = processLogEntries
        .map(formatProcessLogEntry)
        .filter(line => !query || line.toLowerCase().includes(query));
      if (!entries.length) {
        processLogOutput.textContent = query && processLogEntries.length
          ? t('processLogNoMatches') + '\n'
          : t('processLogEmpty') + '\n';
      } else {
        processLogOutput.textContent = entries.join('\n') + '\n';
      }
      processLogOutput.scrollTop = processLogOutput.scrollHeight;
    }

    function closeProcessLogSocket() {
      if (processLogSocketPingTimer) {
        window.clearInterval(processLogSocketPingTimer);
        processLogSocketPingTimer = null;
      }
      if (processLogSocket) {
        const socket = processLogSocket;
        processLogSocket = null;
        socket.close();
      }
    }

    function stopProcessLogReading(showStoppedStatus) {
      processLogReading = false;
      closeProcessLogSocket();
      if (showStoppedStatus && processLogTargetKey) {
        processLogStatus.textContent = t('processLogReadStopped');
      }
      updateProcessLogReadingControls();
    }

    function closeProcessLogModal() {
      processLogRequestVersion++;
      stopProcessLogReading(false);
      processLogTargetKey = null;
      processLogModal.hidden = true;
      processLogEntries = [];
      processLogQueryInput.value = '';
      processLogOutput.textContent = '';
      processLogStatus.textContent = '';
      updateProcessLogReadingControls();
    }

    function updateProcessLogReadingControls() {
      processLogReadButton.textContent = t(processLogReading ? 'processLogStopReading' : 'processLogStartReading');
      processLogReadButton.classList.toggle('danger', processLogReading);
      processLogReadButton.classList.toggle('primary', !processLogReading);
      processLogReadButton.disabled = !processLogTargetKey;
      processLogQueryButton.disabled = !processLogTargetKey;
      processLogQueryInput.disabled = !processLogTargetKey;
    }

    function isEditableTextFile(path) {
      return ['.txt', '.yaml', '.yml', '.json', '.properties', '.xml', '.ini', '.conf', '.config', '.toml', '.md']
        .includes(String(path || '').toLowerCase().slice(String(path || '').lastIndexOf('.')));
    }

    async function openFileEditor(path) {
      if (!path) return;
      fileEditorSnapshot = null;
      fileEditorPath.textContent = path;
      fileEditorContent.value = '';
      fileEditorContent.disabled = true;
      saveFileEditorButton.disabled = true;
      setFileEditorAlert(t('fileEditorLoading'), false);
      fileEditorModal.hidden = false;
      try {
        const response = await fetch(fileEditingPath + '?path=' + encodeURIComponent(path), { headers: headers() });
        const payload = await response.json();
        if (!response.ok) throw new Error(formatApiError(response.status, payload));
        fileEditorSnapshot = payload.data || payload;
        fileEditorContent.value = fileEditorSnapshot.content || '';
        fileEditorContent.disabled = false;
        saveFileEditorButton.disabled = false;
        setFileEditorAlert('', false);
      } catch (error) {
        setFileEditorAlert(t('fileEditorFailed', { message: error.message }), true);
      }
    }

    async function saveFileEditor() {
      if (!fileEditorSnapshot) return;
      saveFileEditorButton.disabled = true;
      setFileEditorAlert(t('fileEditorSaving'), false);
      try {
        const response = await fetch(fileEditingPath, {
          method: 'PUT',
          headers: headers('application/json'),
          body: JSON.stringify({
            path: fileEditorSnapshot.path,
            content: fileEditorContent.value,
            expectedLastModifiedUtc: fileEditorSnapshot.lastModifiedUtc
          })
        });
        const payload = await response.json();
        if (!response.ok) throw new Error(formatApiError(response.status, payload));
        fileEditorSnapshot = payload.data || payload;
        alert(t('fileEditorSaved'));
        closeFileEditorModal();
      } catch (error) {
        setFileEditorAlert(error.status === 409 ? t('fileEditorConflict') : t('fileEditorFailed', { message: error.message }), true);
      } finally {
        saveFileEditorButton.disabled = false;
      }
    }

    function setFileEditorAlert(message, isError) {
      fileEditorAlert.hidden = !message;
      fileEditorAlert.textContent = message || '';
      fileEditorAlert.classList.toggle('danger', isError);
    }

    function closeFileEditorModal() {
      fileEditorModal.hidden = true;
      fileEditorSnapshot = null;
      fileEditorContent.value = '';
      fileEditorContent.disabled = false;
      saveFileEditorButton.disabled = false;
      setFileEditorAlert('', false);
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
        const path = buildTargetPath(key);
        return await api(path);
      } catch (error) {
        return { error: error.message };
      }
    }

    function buildTargetPath(key) {
      const segments = [
        encodeURIComponent(key.dynamicAppId),
        encodeURIComponent(key.modId),
        encodeURIComponent(key.version)
      ];
      if (key.deploymentId && key.deploymentId !== 'default') {
        segments.push(encodeURIComponent(key.deploymentId));
      }
      return '/target/' + segments.join('/');
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

    async function loadRuntimeFilesSection(key) {
      try {
        const query = new URLSearchParams({
          dynamicAppId: key.dynamicAppId,
          modId: key.modId,
          version: key.version,
          deploymentId: key.deploymentId || 'default'
        });
        const result = await api('/deployments/files?' + query.toString());
        return result && Array.isArray(result.data) ? result.data : [];
      } catch (error) {
        return [];
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
        } else if (path === '/deployments/orphans') {
          cachedOrphans = result && result.data ? result.data : [];
          renderOrphans(cachedOrphans);
        }
        return;
      }

      const body = document.getElementById('apiBody').value.trim();
      JSON.parse(body);
      if (['POST /deploy', 'POST /publish', 'POST /activate', 'POST /rollback', 'POST /state', 'POST /archive', 'POST /unarchive', 'POST /self-healing/reset', 'POST /deployments/remove'].includes(value)
        && !confirm(t('confirmExecute', { operation: value }))) {
        return;
      }

      await api(path, { method, contentType: 'application/json', body });
      if (['POST /deploy', 'POST /activate', 'POST /rollback', 'POST /state', 'POST /archive', 'POST /unarchive', 'POST /self-healing/reset', 'POST /deployments/create', 'POST /deployments/remove'].includes(value)) {
        await loadApplications();
      }
      if (['POST /archive', 'POST /unarchive'].includes(value)) {
        await loadArchives();
      }
      if (value === 'POST /deployments/remove') {
        await loadOrphans();
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
      } else if (value === 'POST /deployments/create') {
        document.getElementById('apiBody').value = JSON.stringify({
          dynamicAppId: 'security-platform',
          modId: 'auth',
          version: '101-process-current',
          deploymentId: 'replica-02',
          port: 5111,
          endpoint: 'http://127.0.0.1:5111',
          start: false
        }, null, 2);
      } else if (value === 'POST /deployments/remove') {
        document.getElementById('apiBody').value = JSON.stringify({
          dynamicAppId: 'security-platform',
          modId: 'auth',
          version: '101-process-current',
          deploymentId: 'replica-02'
        }, null, 2);
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
        }
      };
      await api('/mods/configure', { method: 'POST', contentType: 'application/json', body: JSON.stringify(request) });
      await loadApplications();
    }

    function resetLocalDebug() {
      document.getElementById('debugApp').value = sampleLocalDebug.dynamicAppId;
      document.getElementById('debugMod').value = sampleLocalDebug.mod.id;
      document.getElementById('debugVersion').value = sampleLocalDebug.mod.version;
      document.getElementById('debugEndpoint').value = sampleLocalDebug.mod.artifact.endpoint;
      document.getElementById('debugHealth').value = sampleLocalDebug.mod.health.path;
      document.getElementById('debugTimeout').value = String(sampleLocalDebug.mod.health.startupTimeoutSeconds);
      document.getElementById('debugDeploy').value = String(sampleLocalDebug.deploy);
    }

    async function submitUpload() {
      uploadResultLog.hidden = true;
      uploadResultLog.textContent = '';
      try {
        const file = document.getElementById('uploadPackage').files[0];
        if (!currentPackageModKey || !valueOf('uploadApp')) throw new Error(t('selectApplicationFirst'));
        if (!file) throw new Error(t('packageRequired'));
        if (!file.name.toLowerCase().endsWith('.zip')) throw new Error(t('packageZipRequired'));
        if (!valueOf('uploadVersion').trim()) throw new Error(t('versionRequired'));

        submitUploadButton.disabled = true;
        submitUploadButton.textContent = t('uploadStarted');
        const form = new FormData();
        form.append('package', file);
        form.append('dynamicAppId', currentPackageModKey.dynamicAppId);
        form.append('modId', currentPackageModKey.modId);
        form.append('version', valueOf('uploadVersion'));
        await api('/mods/upload', { method: 'POST', headers: headers(), body: form });
        await loadPackages();
        alert(t('uploadSucceeded'));
        closePackageUploadModal();
      } catch (error) {
        uploadResultLog.textContent = '';
        uploadResultLog.hidden = true;
        const payload = formatApiPayload(error.payload);
        alert(t('uploadFailed', { message: error.message }) + (payload ? '\n' + payload : ''));
      } finally {
        submitUploadButton.disabled = false;
        submitUploadButton.textContent = t('uploadAction');
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



