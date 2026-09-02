using System.Text;
using System.Text.Json;

using Air.Cloud.DynamicApp.Contracts.Identifiers;
using Air.Cloud.DynamicApp.Contracts.Lifecycle;
using Air.Cloud.DynamicApp.Contracts.Runtime;
using Air.Cloud.DynamicApp.Manager;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.DynamicApp.UnitTest.Manager;

/// <summary>
/// <para>zh-cn:动态路由管理 API 测试，覆盖不可用目标二次确认和独立路由保存。</para>
/// <para>en-us:Dynamic route management API tests covering unavailable-target confirmation and independent route persistence.</para>
/// </summary>
public sealed class DynamicAppGatewayRouteManagerApiTests
{
    [Fact]
    public async Task Create_should_require_confirmation_when_target_is_unavailable()
    {
        var routeStore = new InMemoryDynamicAppGatewayRouteStore();
        var stateStore = new InMemoryDynamicAppStateStore();
        var services = new ServiceCollection()
            .AddSingleton<IDynamicAppGatewayRouteStore>(routeStore)
            .AddSingleton<IDynamicAppStateStore>(stateStore)
            .BuildServiceProvider();
        var route = CreateRoute();

        var first = await InvokeAsync(services, HttpMethods.Post, "/dynamicapp-manager/routes", new DynamicAppGatewayRouteSaveRequest { Route = route });

        Assert.Equal(StatusCodes.Status409Conflict, first.StatusCode);
        Assert.Contains("confirmation", first.Body, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(routeStore.List());

        var second = await InvokeAsync(services, HttpMethods.Post, "/dynamicapp-manager/routes", new DynamicAppGatewayRouteSaveRequest
        {
            Route = route,
            ConfirmUnavailable = true
        });

        Assert.Equal(StatusCodes.Status200OK, second.StatusCode);
        Assert.Single(routeStore.List());
    }

    [Fact]
    public async Task Create_should_save_without_confirmation_for_active_healthy_target()
    {
        var routeStore = new InMemoryDynamicAppGatewayRouteStore();
        var stateStore = new InMemoryDynamicAppStateStore();
        var key = new DynamicModVersionKey(new DynamicAppId("app"), new DynamicModId("mod"), new DynamicAppVersion("1.0.0"));
        stateStore.UpsertTarget(new DynamicAppRouteTarget
        {
            Key = key,
            BaseAddress = new Uri("http://127.0.0.1:5100"),
            State = DynamicModLifecycleState.Active,
            HealthStatus = DynamicModHealthStatus.Healthy
        });
        var services = new ServiceCollection()
            .AddSingleton<IDynamicAppGatewayRouteStore>(routeStore)
            .AddSingleton<IDynamicAppStateStore>(stateStore)
            .BuildServiceProvider();

        var response = await InvokeAsync(services, HttpMethods.Post, "/dynamicapp-manager/routes", new DynamicAppGatewayRouteSaveRequest { Route = CreateRoute() });

        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
        Assert.Single(routeStore.List());
    }

    [Fact]
    public async Task Create_should_allow_version_and_instance_together()
    {
        var routeStore = new InMemoryDynamicAppGatewayRouteStore();
        var stateStore = new InMemoryDynamicAppStateStore();
        stateStore.UpsertTarget(new DynamicAppRouteTarget
        {
            Key = new DynamicModVersionKey(new DynamicAppId("app"), new DynamicModId("mod"), new DynamicAppVersion("1.0.0")),
            DeploymentId = "replica-1",
            BaseAddress = new Uri("http://127.0.0.1:5100"),
            State = DynamicModLifecycleState.Active,
            HealthStatus = DynamicModHealthStatus.Healthy
        });
        var services = new ServiceCollection()
            .AddSingleton<IDynamicAppGatewayRouteStore>(routeStore)
            .AddSingleton<IDynamicAppStateStore>(stateStore)
            .BuildServiceProvider();
        var route = CreateRoute();
        route.TargetDeploymentId = "replica-1";

        var response = await InvokeAsync(services, HttpMethods.Post, "/dynamicapp-manager/routes", new DynamicAppGatewayRouteSaveRequest { Route = route });

        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
        Assert.Single(routeStore.List());
    }

    private static DynamicAppGatewayRoute CreateRoute() => new()
    {
        RouteId = "app-mod-health",
        Name = "Health",
        UpstreamPathTemplate = "/api/{id}",
        DownstreamPathTemplate = "/internal/{id}",
        TargetDynamicAppId = "app",
        TargetModId = "mod",
        TargetVersion = "1.0.0",
        Order = 10
    };

    private static async Task<(int StatusCode, string Body)> InvokeAsync(IServiceProvider services, string method, string path, object body)
    {
        var context = new DefaultHttpContext { RequestServices = services };
        context.Request.Method = method;
        context.Request.Path = path;
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(body)));
        context.Response.Body = new MemoryStream();
        var middleware = new DynamicAppManagerApiMiddleware(_ => Task.CompletedTask, new DynamicAppManagerApiOptions { RequireTicket = false });
        await middleware.InvokeAsync(context);
        context.Response.Body.Position = 0;
        return ((int)context.Response.StatusCode, await new StreamReader(context.Response.Body).ReadToEndAsync());
    }
}
