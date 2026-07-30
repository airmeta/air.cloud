using System.Net;
using System.Text.Json;

using Air.Cloud.DynamicApp.Contracts.Identifiers;
using Air.Cloud.DynamicApp.Contracts.Lifecycle;
using Air.Cloud.DynamicApp.Contracts.Manifest;
using Air.Cloud.DynamicApp.Contracts.Runtime;
using Air.Cloud.DynamicApp.Manager;
using Air.Cloud.DynamicApp.Panel.Extensions;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.DynamicApp.IntegrationTest.Panel;

/// <summary>
/// <para>zh-cn:DynamicApp 控制面板中间件集成测试，验证真实 ASP.NET Core 管线中的授权、页面渲染和资源监控输出。</para>
/// <para>en-us:Integration tests for the DynamicApp panel middleware, verifying authorization, page rendering, and runtime-stat output in a real ASP.NET Core pipeline.</para>
/// </summary>
public sealed class DynamicAppPanelMiddlewareIntegrationTests
{
    /// <summary>
    /// <para>zh-cn:验证运行在 TestServer 中的面板页面会隐藏本地联调入口，且仍能正常返回 HTML。</para>
    /// <para>en-us:Verifies that the panel page running in TestServer hides the local-debug entry while still returning HTML normally.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "DynamicApp")]
    public async Task DynamicAppPanel_should_hide_local_debug_entry_in_pipeline()
    {
        await using var app = await CreateAppAsync();
        var client = app.GetTestClient();

        var html = await client.GetStringAsync("/dynamicapp-panel");

        Assert.Contains("<div class=\"nav-group\" hidden>", html, StringComparison.Ordinal);
        Assert.Contains("<section id=\"view-local-debug\" class=\"view\" hidden>", html, StringComparison.Ordinal);
    }

    /// <summary>
    /// <para>zh-cn:验证资源监控接口在真实管线中要求 Manager Ticket，并在授权后只返回每个应用/Mod 的当前运行时。</para>
    /// <para>en-us:Verifies that the runtime-stat endpoint requires a Manager Ticket in the real pipeline and returns only the current runtime per application/Mod after authorization.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "DynamicApp")]
    public async Task RuntimeStats_should_require_ticket_and_return_current_runtime_in_pipeline()
    {
        await using var app = await CreateAppAsync();
        var client = app.GetTestClient();

        var unauthorized = await client.GetAsync("/dynamicapp-panel/runtime-stats");
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/dynamicapp-panel/runtime-stats");
        request.Headers.TryAddWithoutValidation("X-DynamicApp-Manager-Ticket", "integration-ticket");
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var mods = document.RootElement.GetProperty("mods").EnumerateArray().ToArray();
        Assert.Equal(2, mods.Length);
        Assert.Contains(mods, item =>
            item.GetProperty("dynamicAppId").GetString() == "chaos-platform"
            && item.GetProperty("modId").GetString() == "agentsprint-api"
            && item.GetProperty("runtimeType").GetString() == "Container"
            && item.GetProperty("executionMode").GetString() == "ExistingContainer");
        Assert.Contains(mods, item =>
            item.GetProperty("dynamicAppId").GetString() == "security-platform"
            && item.GetProperty("modId").GetString() == "account"
            && item.GetProperty("version").GetString() == "101-process-current");
        Assert.DoesNotContain(mods, item =>
            item.GetProperty("dynamicAppId").GetString() == "security-platform"
            && item.GetProperty("modId").GetString() == "account"
            && item.GetProperty("version").GetString() == "101-current");
    }

    private static async Task<WebApplication> CreateAppAsync()
    {
        var stateStore = new InMemoryDynamicAppStateStore();
        var manifestStore = new InMemoryDynamicAppManifestStore();
        await SeedRuntimeAsync(stateStore, manifestStore);
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<IDynamicAppStateStore>(stateStore);
        builder.Services.AddSingleton<IDynamicAppManifestStore>(manifestStore);
        builder.Services.AddSingleton<IDynamicAppExistingContainerCatalog>(new FakeExistingContainerCatalog("agentsprint-api"));
        builder.Services.AddSingleton(new DynamicAppManagerApiOptions
        {
            RequireTicket = true,
            Tickets = ["integration-ticket"]
        });
        builder.Services.AddDynamicAppPanel(options =>
        {
            options.Enabled = true;
            options.RoutePrefix = "/dynamicapp-panel";
            options.ResourceStatsSampleInterval = TimeSpan.FromSeconds(10);
        });

        var app = builder.Build();
        app.UseDynamicAppPanel();
        app.Run(context =>
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return Task.CompletedTask;
        });
        await app.StartAsync();
        return app;
    }

    private static async Task SeedRuntimeAsync(InMemoryDynamicAppStateStore stateStore, InMemoryDynamicAppManifestStore manifestStore)
    {
        var appId = new DynamicAppId("security-platform");
        var accountId = new DynamicModId("account");
        var oldAccountKey = new DynamicModVersionKey(appId, accountId, new DynamicAppVersion("101-current"));
        var currentAccountKey = new DynamicModVersionKey(appId, accountId, new DynamicAppVersion("101-process-current"));
        stateStore.UpsertTarget(CreateTarget(oldAccountKey, "air.account", DynamicModLifecycleState.Draining, DynamicModHealthStatus.Draining));
        stateStore.UpsertTarget(CreateTarget(currentAccountKey, Environment.ProcessId.ToString(), DynamicModLifecycleState.Active, DynamicModHealthStatus.Healthy));
        await manifestStore.SaveAsync(CreateManifest(appId, accountId, "101-current", DynamicModExecutionMode.Container, "air.account"));
        await manifestStore.SaveAsync(CreateManifest(appId, accountId, "101-process-current", DynamicModExecutionMode.Process, string.Empty));

        await manifestStore.SaveAsync(CreateManifest(
            new DynamicAppId("chaos-platform"),
            new DynamicModId("agentsprint-api"),
            "101-current",
            DynamicModExecutionMode.ExistingContainer,
            "agentsprint-api"));
    }

    private static DynamicAppRouteTarget CreateTarget(DynamicModVersionKey key, string instanceId, DynamicModLifecycleState state, DynamicModHealthStatus healthStatus) => new()
    {
        Key = key,
        BaseAddress = new Uri("http://127.0.0.1"),
        RoutePrefix = "/mods/" + key.ModId.Value,
        HealthPath = "/Health",
        HealthProbeTimeoutSeconds = 10,
        InstanceId = instanceId,
        State = state,
        HealthStatus = healthStatus
    };

    private static DynamicAppManifest CreateManifest(DynamicAppId appId, DynamicModId modId, string version, DynamicModExecutionMode mode, string containerName) => new()
    {
        Id = appId,
        Version = new DynamicAppVersion(version),
        Mods =
        [
            new DynamicModManifest
            {
                Id = modId,
                Version = new DynamicAppVersion(version),
                ExecutionMode = mode,
                Artifact = new DynamicModArtifactManifest
                {
                    ContainerName = containerName,
                    Endpoint = string.IsNullOrWhiteSpace(containerName) ? string.Empty : "http://" + containerName + ":8080"
                },
                Route = new DynamicModRouteManifest { Prefix = "/mods/" + modId.Value },
                Health = new DynamicModHealthManifest { Path = "/Health", StartupTimeoutSeconds = 10 }
            }
        ]
    };

    private sealed class FakeExistingContainerCatalog(string containerName) : IDynamicAppExistingContainerCatalog
    {
        public Task<IReadOnlyCollection<DynamicAppExistingContainerView>> ListAsync(CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<DynamicAppExistingContainerView> containers =
            [
                CreateView()
            ];
            return Task.FromResult(containers);
        }

        public Task<DynamicAppExistingContainerView> InspectAsync(DynamicModVersionKey key, CancellationToken cancellationToken = default) => Task.FromResult(CreateView());

        private DynamicAppExistingContainerView CreateView() => new()
        {
            Id = "container-1",
            Name = containerName,
            Image = "agentsprint/api:test",
            State = "running",
            Status = "running",
            IsRunning = true,
            IsAvailable = true
        };
    }
}
