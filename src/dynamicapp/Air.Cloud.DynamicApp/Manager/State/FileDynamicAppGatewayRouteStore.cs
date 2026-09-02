using System.Text.Json;
using System.Text.Json.Serialization;

using Air.Cloud.DynamicApp.Contracts.Runtime;
using Air.Cloud.DynamicApp.Gateway;

namespace Air.Cloud.DynamicApp.Manager;

/// <summary>
/// <para>zh-cn:文件型动态入口路由存储，使用原子 JSON 写入并在启动时恢复快照。</para>
/// <para>en-us:File-based dynamic entry-route store using atomic JSON replacement and startup snapshot recovery.</para>
/// </summary>
public sealed class FileDynamicAppGatewayRouteStore : IDynamicAppGatewayRouteStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };
    private readonly string _filePath;
    private readonly FileDynamicAppGatewayRouteStoreOptions _options;
    private readonly object _syncRoot = new();
    private readonly Dictionary<string, DynamicAppGatewayRoute> _routes = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// <para>zh-cn:创建文件型路由存储并加载现有快照；损坏文件会被重命名保留。</para>
    /// <para>en-us:Creates the file route store and loads its snapshot; a corrupt file is retained under a renamed backup.</para>
    /// </summary>
    public FileDynamicAppGatewayRouteStore(FileDynamicAppGatewayRouteStoreOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _filePath = ResolveFilePath(options.FilePath);
        Load();
    }

    /// <inheritdoc />
    public IReadOnlyCollection<DynamicAppGatewayRoute> List()
    {
        lock (_syncRoot)
        {
            using var fileLock = _options.SaveOnChange ? DynamicAppFileSnapshotLock.Acquire(_filePath) : null;
            if (_options.SaveOnChange) Load();
            return _routes.Values.Select(Clone).OrderBy(item => item.Order).ThenBy(item => item.RouteId, StringComparer.OrdinalIgnoreCase).ToArray();
        }
    }

    /// <inheritdoc />
    public DynamicAppGatewayRoute? Get(string routeId)
    {
        lock (_syncRoot)
        {
            using var fileLock = _options.SaveOnChange ? DynamicAppFileSnapshotLock.Acquire(_filePath) : null;
            if (_options.SaveOnChange) Load();
            return _routes.TryGetValue(routeId, out var route) ? Clone(route) : null;
        }
    }

    /// <inheritdoc />
    public Task<DynamicAppOperationResult> CreateAsync(DynamicAppGatewayRoute route, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(route);
        cancellationToken.ThrowIfCancellationRequested();
        lock (_syncRoot)
        {
            using var fileLock = _options.SaveOnChange ? DynamicAppFileSnapshotLock.Acquire(_filePath) : null;
            if (_options.SaveOnChange) Load();
            if (_routes.ContainsKey(route.RouteId))
            {
                return Task.FromResult(DynamicAppOperationResult.Failure($"Route `{route.RouteId}` already exists."));
            }

            var candidate = Clone(route);
            var errors = DynamicAppGatewayRouteValidator.Validate(_routes.Values.Append(candidate));
            if (errors.Count > 0)
            {
                return Task.FromResult(DynamicAppOperationResult.Failure(string.Join(Environment.NewLine, errors)));
            }

            candidate.CreatedAtUtc = DateTimeOffset.UtcNow;
            candidate.UpdatedAtUtc = candidate.CreatedAtUtc;
            _routes[candidate.RouteId] = candidate;
            SaveIfNeeded();
            return Task.FromResult(DynamicAppOperationResult.Success($"Route `{candidate.RouteId}` created."));
        }
    }

    /// <inheritdoc />
    public Task<DynamicAppOperationResult> UpdateAsync(DynamicAppGatewayRoute route, DateTimeOffset? expectedUpdatedAtUtc = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(route);
        cancellationToken.ThrowIfCancellationRequested();
        lock (_syncRoot)
        {
            using var fileLock = _options.SaveOnChange ? DynamicAppFileSnapshotLock.Acquire(_filePath) : null;
            if (_options.SaveOnChange) Load();
            if (!_routes.TryGetValue(route.RouteId, out var current))
            {
                return Task.FromResult(DynamicAppOperationResult.Failure($"Route `{route.RouteId}` was not found."));
            }

            if (expectedUpdatedAtUtc.HasValue && current.UpdatedAtUtc != expectedUpdatedAtUtc.Value)
            {
                return Task.FromResult(DynamicAppOperationResult.Failure($"Route `{route.RouteId}` was modified by another request."));
            }

            var candidate = Clone(route);
            candidate.CreatedAtUtc = current.CreatedAtUtc;
            candidate.UpdatedAtUtc = DateTimeOffset.UtcNow;
            var errors = DynamicAppGatewayRouteValidator.Validate(_routes.Values.Where(item => !string.Equals(item.RouteId, route.RouteId, StringComparison.OrdinalIgnoreCase)).Append(candidate));
            if (errors.Count > 0)
            {
                return Task.FromResult(DynamicAppOperationResult.Failure(string.Join(Environment.NewLine, errors)));
            }

            _routes[candidate.RouteId] = candidate;
            SaveIfNeeded();
            return Task.FromResult(DynamicAppOperationResult.Success($"Route `{candidate.RouteId}` updated."));
        }
    }

    /// <inheritdoc />
    public Task<DynamicAppOperationResult> RemoveAsync(string routeId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_syncRoot)
        {
            using var fileLock = _options.SaveOnChange ? DynamicAppFileSnapshotLock.Acquire(_filePath) : null;
            if (_options.SaveOnChange) Load();
            var result = _routes.Remove(routeId)
                ? DynamicAppOperationResult.Success($"Route `{routeId}` removed.")
                : DynamicAppOperationResult.Failure($"Route `{routeId}` was not found.");
            if (result.Succeeded)
            {
                SaveIfNeeded();
            }

            return Task.FromResult(result);
        }
    }

    /// <inheritdoc />
    public Task<DynamicAppOperationResult> SetEnabledAsync(string routeId, bool enabled, DateTimeOffset? expectedUpdatedAtUtc = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_syncRoot)
        {
            using var fileLock = _options.SaveOnChange ? DynamicAppFileSnapshotLock.Acquire(_filePath) : null;
            if (_options.SaveOnChange) Load();
            if (!_routes.TryGetValue(routeId, out var route))
            {
                return Task.FromResult(DynamicAppOperationResult.Failure($"Route `{routeId}` was not found."));
            }

            if (expectedUpdatedAtUtc.HasValue && route.UpdatedAtUtc != expectedUpdatedAtUtc.Value)
            {
                return Task.FromResult(DynamicAppOperationResult.Failure($"Route `{routeId}` was modified by another request."));
            }

            route.Enabled = enabled;
            route.UpdatedAtUtc = DateTimeOffset.UtcNow;
            SaveIfNeeded();
            return Task.FromResult(DynamicAppOperationResult.Success($"Route `{routeId}` {(enabled ? "enabled" : "disabled")}."));
        }
    }

    private void Load()
    {
        _routes.Clear();
        if (!File.Exists(_filePath))
        {
            return;
        }

        try
        {
            var snapshot = JsonSerializer.Deserialize<RouteSnapshot>(File.ReadAllText(_filePath), JsonOptions);
            if (snapshot?.Routes == null)
            {
                return;
            }

            var errors = DynamicAppGatewayRouteValidator.Validate(snapshot.Routes);
            if (errors.Count > 0)
            {
                PreserveCorruptFile();
                return;
            }

            foreach (var route in snapshot.Routes)
            {
                _routes[route.RouteId] = Clone(route);
            }
        }
        catch (JsonException)
        {
            PreserveCorruptFile();
        }
        catch (IOException)
        {
            // Startup may continue; a later management write can recreate the snapshot.
        }
    }

    private void SaveIfNeeded()
    {
        if (!_options.SaveOnChange)
        {
            return;
        }

        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        DynamicAppAtomicJsonFile.Write(_filePath, JsonSerializer.Serialize(new RouteSnapshot
        {
            Routes = _routes.Values.Select(Clone).OrderBy(item => item.Order).ThenBy(item => item.RouteId, StringComparer.OrdinalIgnoreCase).ToList()
        }, JsonOptions));
    }

    private void PreserveCorruptFile()
    {
        try
        {
            var backup = _filePath + ".corrupt." + DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmssfff") + ".json";
            File.Move(_filePath, backup, overwrite: false);
        }
        catch (IOException)
        {
        }
    }

    private static string ResolveFilePath(string filePath)
        => Path.IsPathFullyQualified(filePath)
            ? Path.GetFullPath(filePath)
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, filePath));

    private static DynamicAppGatewayRoute Clone(DynamicAppGatewayRoute route) => new()
    {
        RouteId = route.RouteId,
        Name = route.Name,
        UpstreamPathTemplate = route.UpstreamPathTemplate,
        UpstreamHttpMethods = route.UpstreamHttpMethods.ToArray(),
        TargetDynamicAppId = route.TargetDynamicAppId,
        TargetModId = route.TargetModId,
        TargetVersion = route.TargetVersion,
        TargetDeploymentId = route.TargetDeploymentId,
        DownstreamPathTemplate = route.DownstreamPathTemplate,
        Order = route.Order,
        Enabled = route.Enabled,
        CreatedAtUtc = route.CreatedAtUtc,
        UpdatedAtUtc = route.UpdatedAtUtc,
        LastStatusMessage = route.LastStatusMessage
    };

    private sealed class RouteSnapshot
    {
        public List<DynamicAppGatewayRoute> Routes { get; set; } = new();
    }
}
