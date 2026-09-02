using Air.Cloud.DynamicApp.Contracts.Runtime;
using Air.Cloud.DynamicApp.Gateway;

namespace Air.Cloud.DynamicApp.Manager;

/// <summary>
/// <para>zh-cn:进程内动态入口路由存储，供默认 Host 和测试使用。</para>
/// <para>en-us:In-process dynamic entry-route store used by the default Host and tests.</para>
/// </summary>
public sealed class InMemoryDynamicAppGatewayRouteStore : IDynamicAppGatewayRouteStore
{
    private readonly object _syncRoot = new();
    private readonly Dictionary<string, DynamicAppGatewayRoute> _routes = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public IReadOnlyCollection<DynamicAppGatewayRoute> List()
    {
        lock (_syncRoot)
        {
            return _routes.Values.Select(Clone).OrderBy(item => item.Order).ThenBy(item => item.RouteId, StringComparer.OrdinalIgnoreCase).ToArray();
        }
    }

    /// <inheritdoc />
    public DynamicAppGatewayRoute? Get(string routeId)
    {
        lock (_syncRoot)
        {
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
            return Task.FromResult(DynamicAppOperationResult.Success($"Route `{candidate.RouteId}` updated."));
        }
    }

    /// <inheritdoc />
    public Task<DynamicAppOperationResult> RemoveAsync(string routeId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_syncRoot)
        {
            return Task.FromResult(_routes.Remove(routeId)
                ? DynamicAppOperationResult.Success($"Route `{routeId}` removed.")
                : DynamicAppOperationResult.Failure($"Route `{routeId}` was not found."));
        }
    }

    /// <inheritdoc />
    public Task<DynamicAppOperationResult> SetEnabledAsync(string routeId, bool enabled, DateTimeOffset? expectedUpdatedAtUtc = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_syncRoot)
        {
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
            return Task.FromResult(DynamicAppOperationResult.Success($"Route `{routeId}` {(enabled ? "enabled" : "disabled")}."));
        }
    }

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
}
