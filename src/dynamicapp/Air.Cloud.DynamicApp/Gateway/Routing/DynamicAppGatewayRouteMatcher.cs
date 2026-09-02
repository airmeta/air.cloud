using Air.Cloud.DynamicApp.Contracts.Runtime;

namespace Air.Cloud.DynamicApp.Gateway;

/// <summary>
/// <para>zh-cn:按显式 Order 顺序执行动态路由匹配；不会自动把父路由移到子路由之后。</para>
/// <para>en-us:Matches dynamic routes in explicit Order sequence and never moves a parent route behind a child route automatically.</para>
/// </summary>
public sealed class DynamicAppGatewayRouteMatcher
{
    /// <summary>
    /// <para>zh-cn:按 Order 升序逐条匹配，返回第一条启用且方法、路径均命中的规则。</para>
    /// <para>en-us:Matches enabled rules in ascending Order and returns the first rule matching both method and path.</para>
    /// </summary>
    /// <param name="routes">
    /// <para>zh-cn:动态路由快照。</para>
    /// <para>en-us:Dynamic route snapshot.</para>
    /// </param>
    /// <param name="httpMethod">
    /// <para>zh-cn:请求 HTTP 方法。</para>
    /// <para>en-us:Request HTTP method.</para>
    /// </param>
    /// <param name="requestPath">
    /// <para>zh-cn:请求路径。</para>
    /// <para>en-us:Request path.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:第一条匹配结果；没有命中时返回 null。</para>
    /// <para>en-us:The first matching result, or null when no rule matches.</para>
    /// </returns>
    public DynamicAppGatewayRouteMatch? Match(IEnumerable<DynamicAppGatewayRoute> routes, string httpMethod, string requestPath)
    {
        ArgumentNullException.ThrowIfNull(routes);
        if (string.IsNullOrWhiteSpace(httpMethod))
        {
            throw new ArgumentException("HTTP method cannot be empty.", nameof(httpMethod));
        }

        var index = 0;
        foreach (var route in routes
            .Where(item => item.Enabled)
            .Select(item => (Route: item, Index: index++))
            .OrderBy(item => item.Route.Order)
            .ThenBy(item => item.Index))
        {
            if (!AllowsMethod(route.Route, httpMethod))
            {
                continue;
            }

            var template = DynamicAppGatewayRouteTemplate.Parse(route.Route.UpstreamPathTemplate);
            if (template.TryMatch(requestPath, out var parameters))
            {
                return new DynamicAppGatewayRouteMatch(route.Route, parameters, template);
            }
        }

        return null;
    }

    private static bool AllowsMethod(DynamicAppGatewayRoute route, string httpMethod)
        => route.UpstreamHttpMethods.Length == 0
            || route.UpstreamHttpMethods.Any(method => string.Equals(method?.Trim(), httpMethod.Trim(), StringComparison.OrdinalIgnoreCase));
}

/// <summary>
/// <para>zh-cn:动态路由命中结果，包含规则、提取参数和下游路径构建能力。</para>
/// <para>en-us:Dynamic route match containing the rule, extracted parameters, and downstream path construction.</para>
/// </summary>
public sealed class DynamicAppGatewayRouteMatch
{
    internal DynamicAppGatewayRouteMatch(
        DynamicAppGatewayRoute route,
        IReadOnlyDictionary<string, string> parameters,
        DynamicAppGatewayRouteTemplate upstreamTemplate)
    {
        Route = route;
        Parameters = parameters;
        UpstreamTemplate = upstreamTemplate;
    }

    /// <summary>
    /// <para>zh-cn:命中的路由规则。</para>
    /// <para>en-us:Matched route rule.</para>
    /// </summary>
    public DynamicAppGatewayRoute Route { get; }

    /// <summary>
    /// <para>zh-cn:上游模板提取的路径参数。</para>
    /// <para>en-us:Path parameters extracted from the upstream template.</para>
    /// </summary>
    public IReadOnlyDictionary<string, string> Parameters { get; }

    /// <summary>
    /// <para>zh-cn:解析后的上游模板。</para>
    /// <para>en-us:Parsed upstream template.</para>
    /// </summary>
    public DynamicAppGatewayRouteTemplate UpstreamTemplate { get; }

    /// <summary>
    /// <para>zh-cn:按照命中参数构建下游请求路径。</para>
    /// <para>en-us:Builds the downstream request path from the matched parameters.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:下游路径。</para>
    /// <para>en-us:Downstream path.</para>
    /// </returns>
    public string BuildDownstreamPath()
        => DynamicAppGatewayRouteTemplate.BuildDownstreamPath(Route.DownstreamPathTemplate, Parameters);
}

/// <summary>
/// <para>zh-cn:动态路由配置校验器，检查模板、参数、完全重复规则和父子路由排序。</para>
/// <para>en-us:Dynamic route configuration validator checking templates, parameters, exact duplicates, and parent-child ordering.</para>
/// </summary>
public static class DynamicAppGatewayRouteValidator
{
    /// <summary>
    /// <para>zh-cn:返回路由配置错误；返回空集合表示配置合法。</para>
    /// <para>en-us:Returns route configuration errors; an empty collection means the configuration is valid.</para>
    /// </summary>
    /// <param name="routes">
    /// <para>zh-cn:待校验路由集合。</para>
    /// <para>en-us:Routes to validate.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:稳定、可展示的校验错误。</para>
    /// <para>en-us:Stable, displayable validation errors.</para>
    /// </returns>
    public static IReadOnlyCollection<string> Validate(IEnumerable<DynamicAppGatewayRoute> routes)
    {
        ArgumentNullException.ThrowIfNull(routes);
        var items = routes.ToArray();
        var errors = new List<string>();
        var parsed = new Dictionary<string, DynamicAppGatewayRouteTemplate>(StringComparer.OrdinalIgnoreCase);

        foreach (var route in items)
        {
            if (string.IsNullOrWhiteSpace(route.RouteId))
            {
                errors.Add("RouteId cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(route.TargetDynamicAppId))
            {
                errors.Add($"Route `{route.RouteId}` TargetDynamicAppId cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(route.TargetModId))
            {
                errors.Add($"Route `{route.RouteId}` TargetModId cannot be empty.");
            }

            try
            {
                var upstream = DynamicAppGatewayRouteTemplate.Parse(route.UpstreamPathTemplate);
                var downstream = DynamicAppGatewayRouteTemplate.Parse(route.DownstreamPathTemplate);
                parsed[route.RouteId] = upstream;
                var upstreamParameters = upstream.Segments.Where(item => item.ParameterName != null).Select(item => item.ParameterName!).ToHashSet(StringComparer.OrdinalIgnoreCase);
                var downstreamParameters = downstream.Segments.Where(item => item.ParameterName != null).Select(item => item.ParameterName!).ToHashSet(StringComparer.OrdinalIgnoreCase);
                if (!upstreamParameters.SetEquals(downstreamParameters))
                {
                    errors.Add($"Route `{route.RouteId}` upstream and downstream parameters must match exactly.");
                }
            }
            catch (ArgumentException exception)
            {
                errors.Add($"Route `{route.RouteId}` template is invalid: {exception.Message}");
            }
        }

        foreach (var duplicateGroup in items
            .GroupBy(route => BuildDuplicateKey(route), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1))
        {
            errors.Add($"Routes `{string.Join("`, `", duplicateGroup.Select(route => route.RouteId))}` are exactly identical.");
        }

        for (var parentIndex = 0; parentIndex < items.Length; parentIndex++)
        {
            if (!parsed.TryGetValue(items[parentIndex].RouteId, out var parentTemplate))
            {
                continue;
            }

            for (var childIndex = 0; childIndex < items.Length; childIndex++)
            {
                if (parentIndex == childIndex || !parsed.TryGetValue(items[childIndex].RouteId, out var childTemplate))
                {
                    continue;
                }

                if (AllowsParent(parentTemplate, childTemplate)
                    && items[parentIndex].Order <= items[childIndex].Order)
                {
                    errors.Add($"Child route `{items[childIndex].RouteId}` must have a lower Order than parent route `{items[parentIndex].RouteId}`.");
                }
            }
        }

        return errors;
    }

    private static string BuildDuplicateKey(DynamicAppGatewayRoute route)
    {
        var methods = route.UpstreamHttpMethods.Length == 0
            ? "*"
            : string.Join(',', route.UpstreamHttpMethods.Select(method => method.Trim().ToUpperInvariant()).OrderBy(method => method, StringComparer.Ordinal));
        return route.UpstreamPathTemplate.Trim().TrimEnd('/') + "|" + methods;
    }

    private static bool AllowsParent(DynamicAppGatewayRouteTemplate parent, DynamicAppGatewayRouteTemplate child)
    {
        var parentSegments = parent.Segments;
        var childSegments = child.Segments;
        var parentCatchAll = parentSegments.LastOrDefault().IsCatchAll;
        if (!parentCatchAll && parentSegments.Count > childSegments.Count)
        {
            return false;
        }

        if (parentCatchAll && parentSegments.Count - 1 > childSegments.Count)
        {
            return false;
        }

        var comparableCount = parentCatchAll ? parentSegments.Count - 1 : parentSegments.Count;
        var hasBroadSegment = parentCatchAll;
        for (var index = 0; index < comparableCount; index++)
        {
            var parentSegment = parentSegments[index];
            var childSegment = childSegments[index];
            hasBroadSegment |= parentSegment.ParameterName != null;
            if (parentSegment.ParameterName == null
                && childSegment.ParameterName == null
                && !string.Equals(parentSegment.Literal, childSegment.Literal, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return hasBroadSegment;
    }
}
