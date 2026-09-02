using System.Collections.Concurrent;

using Air.Cloud.DynamicApp.Contracts.Lifecycle;
using Air.Cloud.DynamicApp.Contracts.Runtime;

namespace Air.Cloud.DynamicApp.Gateway;

/// <summary>
/// <para>zh-cn:默认动态路由目录，先按 Order 匹配独立规则，再从运行状态中选择目标实例。</para>
/// <para>en-us:Default dynamic route catalog that matches independent rules by Order and then selects a runtime target from state.</para>
/// </summary>
public sealed class DefaultDynamicAppRouteCatalog : IDynamicAppRouteCatalog
{
    private readonly ConcurrentDictionary<string, long> _roundRobinCounters = new(StringComparer.OrdinalIgnoreCase);
    private readonly IDynamicAppGatewayRouteStore _routeStore;
    private readonly IDynamicAppStateStore _stateStore;
    private readonly DynamicAppGatewayRouteMatcher _matcher = new();

    /// <summary>
    /// <para>zh-cn:创建动态路由目录。</para>
    /// <para>en-us:Creates the dynamic route catalog.</para>
    /// </summary>
    /// <param name="routeStore">
    /// <para>zh-cn:独立入口路由规则存储。</para>
    /// <para>en-us:Independent entry-route store.</para>
    /// </param>
    /// <param name="stateStore">
    /// <para>zh-cn:运行目标状态存储。</para>
    /// <para>en-us:Runtime target state store.</para>
    /// </param>
    public DefaultDynamicAppRouteCatalog(IDynamicAppGatewayRouteStore routeStore, IDynamicAppStateStore stateStore)
    {
        _routeStore = routeStore ?? throw new ArgumentNullException(nameof(routeStore));
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
    }

    /// <inheritdoc />
    public ValueTask<DynamicAppGatewayRouteResolution> ResolveAsync(DynamicAppRouteRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromResult(DynamicAppGatewayRouteResolution.Failure("dynamicapp.request_cancelled", "Dynamic route resolution was cancelled."));
        }

        DynamicAppGatewayRouteMatch? match;
        try
        {
            match = _matcher.Match(_routeStore.List(), request.HttpMethod, request.Path);
        }
        catch (ArgumentException exception)
        {
            return ValueTask.FromResult(DynamicAppGatewayRouteResolution.Failure("dynamicapp.route_invalid", exception.Message));
        }

        if (match == null)
        {
            return ValueTask.FromResult(DynamicAppGatewayRouteResolution.Failure("dynamicapp.route_not_found", "No enabled dynamic route matched the request."));
        }

        var route = match.Route;
        var candidates = _stateStore.ListTargets()
            .Where(target => string.Equals(target.Key.DynamicAppId.Value, route.TargetDynamicAppId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(target.Key.ModId.Value, route.TargetModId, StringComparison.OrdinalIgnoreCase))
            .Where(target => string.IsNullOrWhiteSpace(route.TargetVersion)
                || string.Equals(target.Key.Version.Value, route.TargetVersion, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (candidates.Length == 0)
        {
            return ValueTask.FromResult(DynamicAppGatewayRouteResolution.Failure("dynamicapp.target_not_found", $"DynamicApp target `{route.TargetDynamicAppId}/{route.TargetModId}/{route.TargetVersion ?? "active"}` was not found.", match));
        }

        if (!string.IsNullOrWhiteSpace(route.TargetDeploymentId))
        {
            var specifiedCandidates = candidates
                .Where(target => string.Equals(target.DeploymentId, route.TargetDeploymentId, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (string.IsNullOrWhiteSpace(route.TargetVersion) && specifiedCandidates.Select(target => target.Key.Version.Value).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
            {
                return ValueTask.FromResult(DynamicAppGatewayRouteResolution.Failure(
                    "dynamicapp.specified_target_ambiguous",
                    $"Deployment `{route.TargetDeploymentId}` is registered under multiple versions for route `{route.RouteId}`; configure TargetVersion explicitly.",
                    match));
            }

            var specified = specifiedCandidates.FirstOrDefault();
            if (specified == null)
            {
                return ValueTask.FromResult(DynamicAppGatewayRouteResolution.Failure("dynamicapp.specified_target_not_found", $"Deployment `{route.TargetDeploymentId}` was not found for route `{route.RouteId}`.", match));
            }

            if (!IsForwardable(specified))
            {
                return ValueTask.FromResult(DynamicAppGatewayRouteResolution.Failure("dynamicapp.specified_target_unavailable", $"Deployment `{route.TargetDeploymentId}` is not currently available for route `{route.RouteId}`.", match));
            }

            return ValueTask.FromResult(DynamicAppGatewayRouteResolution.Success(match, specified));
        }

        var available = candidates
            .Where(IsForwardable)
            .OrderBy(target => target.DeploymentId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(target => target.InstanceId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (available.Length == 0)
        {
            return ValueTask.FromResult(DynamicAppGatewayRouteResolution.Failure("dynamicapp.target_unavailable", $"No Active and Healthy target is available for route `{route.RouteId}`.", match));
        }

        var counter = _roundRobinCounters.AddOrUpdate(route.RouteId, 0, (_, value) => value + 1);
        var selected = available[(int)(Math.Abs(counter) % available.Length)];
        return ValueTask.FromResult(DynamicAppGatewayRouteResolution.Success(match, selected));
    }

    private static bool IsForwardable(DynamicAppRouteTarget target)
        => target.State == DynamicModLifecycleState.Active && target.HealthStatus == DynamicModHealthStatus.Healthy;
}
