using Air.Cloud.DynamicApp.Contracts.Identifiers;
using Air.Cloud.DynamicApp.Contracts.Lifecycle;
using Air.Cloud.DynamicApp.Contracts.Runtime;
using Air.Cloud.DynamicApp.Gateway;
using Air.Cloud.DynamicApp.Manager;

namespace Air.Cloud.DynamicApp.UnitTest.Gateway;

/// <summary>
/// <para>zh-cn:DynamicApp 集群路由单元测试，验证同一包版本的多个健康部署可以被网关轮询选择。</para>
/// <para>en-us:Unit tests for DynamicApp clustered routing, verifying that multiple healthy deployments of one package version are selected by gateway round-robin.</para>
/// </summary>
public sealed class DynamicAppRouteCatalogClusterTests
{
    /// <summary>
    /// <para>zh-cn:验证同一 DeploymentId 被多个版本复用且路由未指定版本时返回歧义错误，不随机转发到错误版本。</para>
    /// <para>en-us:Verifies that reusing one DeploymentId across versions without a route version returns an ambiguity error instead of randomly forwarding to the wrong version.</para>
    /// </summary>
    [Fact]
    public async Task ResolveAsync_should_reject_ambiguous_fixed_deployment()
    {
        var store = new InMemoryDynamicAppStateStore();
        var app = new DynamicAppId("routing-app");
        var mod = new DynamicModId("api");
        var first = new DynamicModVersionKey(app, mod, new DynamicAppVersion("1.0.0"));
        var second = new DynamicModVersionKey(app, mod, new DynamicAppVersion("2.0.0"));
        store.UpsertTarget(CreateActiveTarget(first, "blue", 5111));
        store.UpsertTarget(CreateActiveTarget(second, "blue", 5112));

        var routes = new InMemoryDynamicAppGatewayRouteStore();
        await routes.CreateAsync(new DynamicAppGatewayRoute
        {
            RouteId = "fixed-blue",
            UpstreamPathTemplate = "/api",
            DownstreamPathTemplate = "/api",
            TargetDynamicAppId = app.Value,
            TargetModId = mod.Value,
            TargetDeploymentId = "blue"
        });

        var result = await new DefaultDynamicAppRouteCatalog(routes, store).ResolveAsync(new DynamicAppRouteRequest { HttpMethod = "GET", Path = "/api" });

        Assert.False(result.Succeeded);
        Assert.Equal("dynamicapp.specified_target_ambiguous", result.ErrorCode);
    }

    /// <summary>
    /// <para>zh-cn:验证激活同一包版本后不会排空同版本的其他部署实例，并且网关能够选择两个实例。</para>
    /// <para>en-us:Verifies that activating one package version does not drain its other deployments and that the gateway can select both instances.</para>
    /// </summary>
    [Fact]
    public async Task ResolveAsync_should_round_robin_same_package_deployments()
    {
        var store = new InMemoryDynamicAppStateStore();
        var packageKey = new DynamicModVersionKey(
            new DynamicAppId("security-platform"),
            new DynamicModId("auth"),
            new DynamicAppVersion("1.0.0"));

        store.UpsertTarget(CreateTarget(packageKey, "blue", 5111));
        store.UpsertTarget(CreateTarget(packageKey, "gray", 5112));
        Assert.True(store.Activate(packageKey).Succeeded);

        var routeStore = new InMemoryDynamicAppGatewayRouteStore();
        Assert.True((await routeStore.CreateAsync(new DynamicAppGatewayRoute
        {
            RouteId = "auth-health",
            UpstreamPathTemplate = "/health",
            DownstreamPathTemplate = "/health",
            TargetDynamicAppId = packageKey.DynamicAppId.Value,
            TargetModId = packageKey.ModId.Value,
            TargetVersion = packageKey.Version.Value
        })).Succeeded);
        var catalog = new DefaultDynamicAppRouteCatalog(routeStore, store);
        var request = new DynamicAppRouteRequest
        {
            HttpMethod = "GET",
            Path = "/health"
        };

        var first = await catalog.ResolveAsync(request);
        var second = await catalog.ResolveAsync(request);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.NotEqual(first!.Target!.DeploymentId, second!.Target!.DeploymentId);
        Assert.Equal(2, store.ListActiveTargets(packageKey.DynamicAppId, packageKey.ModId).Count);
    }

    private static DynamicAppRouteTarget CreateTarget(DynamicModVersionKey key, string deploymentId, int port) => new()
    {
        Key = key,
        DeploymentId = deploymentId,
        BaseAddress = new Uri($"http://127.0.0.1:{port}"),
        HealthPath = "/Health",
        State = DynamicModLifecycleState.Pending,
        HealthStatus = DynamicModHealthStatus.Unknown
    };

    private static DynamicAppRouteTarget CreateActiveTarget(DynamicModVersionKey key, string deploymentId, int port)
    {
        var target = CreateTarget(key, deploymentId, port);
        target.State = DynamicModLifecycleState.Active;
        target.HealthStatus = DynamicModHealthStatus.Healthy;
        return target;
    }
}
