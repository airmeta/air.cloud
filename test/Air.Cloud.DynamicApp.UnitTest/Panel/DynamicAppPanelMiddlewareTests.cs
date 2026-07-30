using System.Text.Json;

using Air.Cloud.DynamicApp.Contracts.Identifiers;
using Air.Cloud.DynamicApp.Contracts.Lifecycle;
using Air.Cloud.DynamicApp.Contracts.Manifest;
using Air.Cloud.DynamicApp.Contracts.Runtime;
using Air.Cloud.DynamicApp.Manager;
using Air.Cloud.DynamicApp.Panel;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.DynamicApp.UnitTest.Panel;

/// <summary>
/// <para>zh-cn:DynamicApp 内置控制面板中间件单元测试，验证页面可见性和资源监控快照的本地归并规则。</para>
/// <para>en-us:Unit tests for the built-in DynamicApp panel middleware, covering page visibility and local runtime-stat snapshot grouping rules.</para>
/// </summary>
public sealed class DynamicAppPanelMiddlewareTests
{
    /// <summary>
    /// <para>zh-cn:验证控制面板 HTML 保留本地联调实现但隐藏左侧入口和视图，避免当前版本暴露未开放功能。</para>
    /// <para>en-us:Verifies that the panel HTML keeps the local-debug implementation while hiding the navigation entry and view so the current version does not expose the unfinished feature.</para>
    /// </summary>
    [Fact]
    public async Task PanelHtml_should_hide_local_debug_navigation_and_view()
    {
        var middleware = CreateMiddleware(requireTicket: false);
        var context = CreateHttpContext("/dynamicapp-panel");

        await middleware.InvokeAsync(context);

        var html = await ReadResponseAsync(context);
        Assert.Contains("<div class=\"nav-group\" hidden>", html, StringComparison.Ordinal);
        Assert.Contains("<section id=\"view-local-debug\" class=\"view\" hidden>", html, StringComparison.Ordinal);
        Assert.Contains("data-view-target=\"local-debug\"", html, StringComparison.Ordinal);
    }

    /// <summary>
    /// <para>zh-cn:验证资源监控只返回每个应用/Mod 的当前运行时，并把 Manifest-only ExistingContainer 登记项纳入列表。</para>
    /// <para>en-us:Verifies that runtime stats return only the current runtime per application/Mod and include manifest-only ExistingContainer registrations.</para>
    /// </summary>
    [Fact]
    public async Task RuntimeStats_should_return_current_runtime_per_mod_and_include_existing_container()
    {
        var services = new ServiceCollection();
        var stateStore = new InMemoryDynamicAppStateStore();
        var manifestStore = new InMemoryDynamicAppManifestStore();
        services.AddSingleton<IDynamicAppStateStore>(stateStore);
        services.AddSingleton<IDynamicAppManifestStore>(manifestStore);
        services.AddSingleton<IDynamicAppExistingContainerCatalog>(new FakeExistingContainerCatalog("agentsprint-api"));
        await SeedRuntimeAsync(stateStore, manifestStore);
        var context = CreateHttpContext("/dynamicapp-panel/runtime-stats", services.BuildServiceProvider());
        context.Request.Headers["X-DynamicApp-Manager-Ticket"] = "unit-ticket";

        await CreateMiddleware(requireTicket: true).InvokeAsync(context);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        using var document = JsonDocument.Parse(await ReadResponseAsync(context));
        var mods = document.RootElement.GetProperty("mods").EnumerateArray().ToArray();
        Assert.Contains(mods, item =>
            item.GetProperty("dynamicAppId").GetString() == "chaos-platform"
            && item.GetProperty("modId").GetString() == "agentsprint-api"
            && item.GetProperty("runtimeType").GetString() == "Container"
            && item.GetProperty("executionMode").GetString() == "ExistingContainer"
            && item.GetProperty("state").GetString() == "Active"
            && item.GetProperty("healthStatus").GetString() == "Healthy");
        var accountRows = mods.Where(item =>
            item.GetProperty("dynamicAppId").GetString() == "security-platform"
            && item.GetProperty("modId").GetString() == "account").ToArray();
        var accountRow = Assert.Single(accountRows);
        Assert.Equal("101-process-current", accountRow.GetProperty("version").GetString());
        Assert.DoesNotContain(mods, item =>
            item.GetProperty("dynamicAppId").GetString() == "security-platform"
            && item.GetProperty("modId").GetString() == "account"
            && item.GetProperty("version").GetString() == "101-current");
    }

    private static DynamicAppPanelMiddleware CreateMiddleware(bool requireTicket)
    {
        var options = new DynamicAppPanelOptions
        {
            Enabled = true,
            RoutePrefix = "/dynamicapp-panel",
            ResourceStatsSampleInterval = TimeSpan.FromSeconds(10)
        };
        var managerOptions = new DynamicAppManagerApiOptions
        {
            RequireTicket = requireTicket,
            Tickets = requireTicket ? ["unit-ticket"] : []
        };
        return new DynamicAppPanelMiddleware(_ => Task.CompletedTask, options, managerOptions);
    }

    private static DefaultHttpContext CreateHttpContext(string path, IServiceProvider? services = null)
    {
        var context = new DefaultHttpContext
        {
            RequestServices = services ?? new ServiceCollection().BuildServiceProvider()
        };
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<string> ReadResponseAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, leaveOpen: true);
        return await reader.ReadToEndAsync();
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
