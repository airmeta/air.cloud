using System.Text.Json;

using Air.Cloud.AirForge.Panel;
using Air.Cloud.AirForge.Panel.FileEditing;
using Air.Cloud.Capabilities.ControlPlaneAuth.Options;
using Air.Cloud.DynamicApp.Contracts.Identifiers;
using Air.Cloud.DynamicApp.Contracts.Lifecycle;
using Air.Cloud.DynamicApp.Contracts.Manifest;
using Air.Cloud.DynamicApp.Contracts.Runtime;
using Air.Cloud.DynamicApp.Manager;
using Air.Cloud.DynamicApp.UnitTest.Support;

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
        var resourceGridStart = html.IndexOf(".resource-dashboard-row {", StringComparison.Ordinal);
        Assert.True(resourceGridStart >= 0);
        var resourceGridEnd = html.IndexOf('}', resourceGridStart);
        Assert.True(resourceGridEnd > resourceGridStart);
        Assert.Contains("align-items: start;", html[resourceGridStart..resourceGridEnd], StringComparison.Ordinal);
    }

    /// <summary>
    /// <para>zh-cn:验证运行时、目标和路由映射使用独立菜单，并保留服务行与实例卡片的结构。</para>
    /// <para>en-us:Verifies that runtime, target, and route mapping use separate menu entries while preserving service-row and instance-card structure.</para>
    /// </summary>
    [Fact]
    public async Task PanelHtml_should_expose_separate_target_and_route_management_views()
    {
        var middleware = CreateMiddleware(requireTicket: false);
        var context = CreateHttpContext("/dynamicapp-panel");

        await middleware.InvokeAsync(context);

        var html = await ReadResponseAsync(context);
        Assert.Contains("data-view-target=\"applications\"", html, StringComparison.Ordinal);
        Assert.Contains("data-view-target=\"routes\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("data-view-target=\"targets\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("id=\"view-targets\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"view-routes\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"routeModal\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"routeModalTitle\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"routeUpstreamTemplate\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"routeDownstreamTemplate\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"routeTargetDynamicAppId\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"routeTargetModId\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"routeTargetMode\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"routeTargetVersion\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"routeTargetDeploymentId\"", html, StringComparison.Ordinal);
        Assert.Contains("function populateDynamicRouteApplications()", html, StringComparison.Ordinal);
        Assert.Contains("function syncDynamicRouteTargetMode()", html, StringComparison.Ordinal);
        Assert.Contains("id=\"saveRoute\"", html, StringComparison.Ordinal);
        Assert.Contains("function openDynamicRouteModal(route = null)", html, StringComparison.Ordinal);
        Assert.Contains("data-action=\"edit-route\"", html, StringComparison.Ordinal);
        Assert.Contains("route-edit-icon", html, StringComparison.Ordinal);
        Assert.Contains("transform: scaleX(-1)", html, StringComparison.Ordinal);
        Assert.Contains("method: editing ? 'PUT' : 'POST'", html, StringComparison.Ordinal);
        Assert.Contains("#view-routes > .toolbar", html, StringComparison.Ordinal);
        Assert.Contains("#routeModal .form-grid > label", html, StringComparison.Ordinal);
        Assert.DoesNotContain("const routeId = prompt", html, StringComparison.Ordinal);
        Assert.DoesNotContain("confirm(error.payload.message", html, StringComparison.Ordinal);
        Assert.Contains(".service-row {", html, StringComparison.Ordinal);
        Assert.Contains(".instance-card {", html, StringComparison.Ordinal);
        Assert.Contains("runtime-application-manager-layout", html, StringComparison.Ordinal);
        Assert.Contains("runtime-application-shell", html, StringComparison.Ordinal);
        Assert.Contains("runtime-application-tabs-header", html, StringComparison.Ordinal);
        Assert.Contains("grid-template-columns: minmax(0, 1fr);", html, StringComparison.Ordinal);
        Assert.Contains("runtime-application-tabs", html, StringComparison.Ordinal);
        Assert.DoesNotContain("runtime-application-tab-stats", html, StringComparison.Ordinal);
        Assert.Contains("role=\"tablist\"", html, StringComparison.Ordinal);
        Assert.Contains("role=\"tab\"", html, StringComparison.Ordinal);
        Assert.Contains("flex: 0 0 350px;", html, StringComparison.Ordinal);
        Assert.Contains("width: 350px;", html, StringComparison.Ordinal);
        Assert.Contains("border-bottom: 1px solid var(--line);", html, StringComparison.Ordinal);
        Assert.Contains("runtime-application-detail", html, StringComparison.Ordinal);
        Assert.Contains("runtime-application-summary", html, StringComparison.Ordinal);
        Assert.Contains("min-height: 100%;", html, StringComparison.Ordinal);
        Assert.Contains("margin: auto 0 4px;", html, StringComparison.Ordinal);
        Assert.DoesNotContain("'<div class=\"row\"><div><h3>' + escapeHtml(app.dynamicAppId || '')", html, StringComparison.Ordinal);
        Assert.Contains("height: calc(100vh - 230px);", html, StringComparison.Ordinal);
        Assert.Contains("overflow-y: auto;", html, StringComparison.Ordinal);
        Assert.Contains("scrollbar-width: none;", html, StringComparison.Ordinal);
        Assert.Contains("runtimeSummary", html, StringComparison.Ordinal);
        var instanceGridStart = html.IndexOf(".instance-grid {", StringComparison.Ordinal);
        Assert.True(instanceGridStart >= 0);
        var instanceGridEnd = html.IndexOf('}', instanceGridStart);
        Assert.True(instanceGridEnd > instanceGridStart);
        Assert.DoesNotContain("grid-template-columns:", html[instanceGridStart..instanceGridEnd], StringComparison.Ordinal);
        Assert.Contains("instance-action-menu-list", html, StringComparison.Ordinal);
        Assert.Contains("position: fixed;", html, StringComparison.Ordinal);
        Assert.Contains("z-index: 1000;", html, StringComparison.Ordinal);
        Assert.Contains("getBoundingClientRect()", html, StringComparison.Ordinal);
        Assert.Contains("menu.style.left", html, StringComparison.Ordinal);
        Assert.Contains("menu.style.top", html, StringComparison.Ordinal);
        Assert.Contains("data-action=\"toggle-instance-menu\"", html, StringComparison.Ordinal);
        Assert.Contains("display: inline-flex;", html, StringComparison.Ordinal);
        Assert.Contains("align-items: center;", html, StringComparison.Ordinal);
        Assert.Contains("justify-content: center;", html, StringComparison.Ordinal);
        Assert.Contains("data-action=\"start\"", html, StringComparison.Ordinal);
        Assert.Contains("data-action=\"stop\"", html, StringComparison.Ordinal);
        Assert.Contains("data-action=\"restart\"", html, StringComparison.Ordinal);
        Assert.Contains("data-action=\"destroy\"", html, StringComparison.Ordinal);
        Assert.Contains("destroyRequiresStopped", html, StringComparison.Ordinal);
        Assert.Contains("const destroyDisabled = state !== 'stopped';", html, StringComparison.Ordinal);
        Assert.Contains("<select id=\"deploymentEntry\" required></select>", html, StringComparison.Ordinal);
        Assert.Contains("id=\"deploymentEndpoint\" placeholder=\"http://127.0.0.1:5111\" readonly", html, StringComparison.Ordinal);
        Assert.Contains("deploymentPortInput.readOnly = !createMode;", html, StringComparison.Ordinal);
        Assert.Contains("const currentEndpoint = current && current.baseAddress || mod && mod.baseAddress || '';", html, StringComparison.Ordinal);
        Assert.Contains("function buildRuntimeFileTree(files)", html, StringComparison.Ordinal);
        Assert.Contains("class=\"runtime-folder\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"deploymentStepThree\"", html, StringComparison.Ordinal);
        Assert.Contains("function buildDeploymentCommand(", html, StringComparison.Ordinal);
        Assert.Contains("function stripDeploymentUrlsArguments(argumentsText)", html, StringComparison.Ordinal);
        Assert.Contains("match(/\"[^\"]*\"|'[^']*'|\\S+/g)", html, StringComparison.Ordinal);
        Assert.Contains("const commandArguments = stripDeploymentUrlsArguments(argumentsText);", html, StringComparison.Ordinal);
        Assert.Contains("replace(/\\\\:/g, ':').replace(/\\/+$/, '')", html, StringComparison.Ordinal);
        Assert.Contains("return 'dotnet ' + shellQuote", html, StringComparison.Ordinal);
        Assert.Contains("editDeploymentParameters", html, StringComparison.Ordinal);
        Assert.Contains("function getPackageEntryFiles(packageVersion)", html, StringComparison.Ordinal);
        Assert.Contains("file.name.toLowerCase().endsWith('.dll')", html, StringComparison.Ordinal);
        Assert.Contains("populateDeploymentEntryOptions", html, StringComparison.Ordinal);
        Assert.Contains("noDllEntryFiles", html, StringComparison.Ordinal);
        Assert.Contains("const deleteButton = '<button data-action=\"delete-package-version\"", html, StringComparison.Ordinal);
        Assert.Contains("packageNotStored", html, StringComparison.Ordinal);
        Assert.Contains("package-manager-shell", html, StringComparison.Ordinal);
        Assert.Contains("package-manager-content", html, StringComparison.Ordinal);
        Assert.Contains("package-manager-list", html, StringComparison.Ordinal);
        Assert.Contains("flex: 0 1 calc(50% - 6px);", html, StringComparison.Ordinal);
        Assert.Contains("width: calc(50% - 6px);", html, StringComparison.Ordinal);
        Assert.Contains("max-width: 100%;", html, StringComparison.Ordinal);
        Assert.Contains("@media (min-width: 1001px) and (max-width: 1399px)", html, StringComparison.Ordinal);
        Assert.Contains("flex-basis: min(700px, 100%);", html, StringComparison.Ordinal);
        Assert.Contains("@media (max-width: 1000px)", html, StringComparison.Ordinal);
        Assert.Contains("flex-basis: 100%;", html, StringComparison.Ordinal);
        Assert.Contains("flex-wrap: wrap;", html, StringComparison.Ordinal);
        Assert.Contains("id=\"packageApplicationList\" class=\"runtime-application-tabs package-application-tabs\" role=\"tablist\"", html, StringComparison.Ordinal);
        Assert.Contains("role=\"tab\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("id=\"uploadOverwrite\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("valueOf('uploadOverwrite')", html, StringComparison.Ordinal);
        Assert.DoesNotContain("form.append('overwrite'", html, StringComparison.Ordinal);
        Assert.Contains("currentPackageModKey = null;", html, StringComparison.Ordinal);
        Assert.Contains("resetPackageUploadForm(null);", html, StringComparison.Ordinal);
        Assert.Contains("submitUploadButton.textContent = t('uploadAction');", html, StringComparison.Ordinal);
        Assert.Contains("submitUploadButton.disabled = false;", html, StringComparison.Ordinal);
        Assert.Contains("document.getElementById('uploadSummary').textContent = t('noPackageSelected');", html, StringComparison.Ordinal);
        var packageVersionRendererStart = html.IndexOf("function renderPackageVersion(", StringComparison.Ordinal);
        var packageVersionRendererEnd = html.IndexOf("function isPackageVersionUsed(", packageVersionRendererStart, StringComparison.Ordinal);
        Assert.True(packageVersionRendererStart >= 0);
        Assert.True(packageVersionRendererEnd > packageVersionRendererStart);
        var packageVersionRenderer = html[packageVersionRendererStart..packageVersionRendererEnd];
        Assert.Contains("const versionBadge = options.isHistory", packageVersionRenderer, StringComparison.Ordinal);
        Assert.DoesNotContain("t('currentVersion')", packageVersionRenderer, StringComparison.Ordinal);
        Assert.Contains(".instance-card-footer", html, StringComparison.Ordinal);
        Assert.Contains(".instance-footer-actions", html, StringComparison.Ordinal);
        Assert.Contains("renderInstanceDirectActions", html, StringComparison.Ordinal);
        var packageApplicationLoaderStart = html.IndexOf("async function loadPackageApplications()", StringComparison.Ordinal);
        Assert.True(packageApplicationLoaderStart >= 0);
        var packageApplicationLoaderEnd = html.IndexOf("function renderPackageApplications(", packageApplicationLoaderStart, StringComparison.Ordinal);
        Assert.True(packageApplicationLoaderEnd > packageApplicationLoaderStart);
        var packageApplicationLoader = html[packageApplicationLoaderStart..packageApplicationLoaderEnd];
        Assert.Contains("renderPackageApplications(cachedPackageApplications);", packageApplicationLoader, StringComparison.Ordinal);
        Assert.Contains("renderPackages(cachedPackages);", packageApplicationLoader, StringComparison.Ordinal);
        Assert.Contains("state = normalizeState(state);", html, StringComparison.Ordinal);
        Assert.Contains("height: 125px;", html, StringComparison.Ordinal);
        Assert.Contains("grid-template-rows: auto minmax(0, 1fr) auto;", html, StringComparison.Ordinal);
        Assert.Contains("padding-top: 6px;", html, StringComparison.Ordinal);
        Assert.Contains("padding-bottom: 2px;", html, StringComparison.Ordinal);
        Assert.Contains("instance-deployment-badge", html, StringComparison.Ordinal);
        Assert.DoesNotContain("t('latest')", html, StringComparison.Ordinal);
        Assert.DoesNotContain("displayMod.instanceId || '-'", html, StringComparison.Ordinal);
        Assert.DoesNotContain("displayMod.lastStatusMessage || '-'", html, StringComparison.Ordinal);
        Assert.Contains(".route-card {", html, StringComparison.Ordinal);
        Assert.Contains("routeManagement", html, StringComparison.Ordinal);
        Assert.Contains("forwardableInstances", html, StringComparison.Ordinal);
        Assert.Contains("roundRobin", html, StringComparison.Ordinal);
        Assert.DoesNotContain("externalRoute", html, StringComparison.Ordinal);
        Assert.DoesNotContain("routeNotManaged", html, StringComparison.Ordinal);
        Assert.Contains("id=\"processLogQuery\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"queryProcessLog\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"toggleProcessLogReading\"", html, StringComparison.Ordinal);
        Assert.Contains("processLogStartReading", html, StringComparison.Ordinal);
        Assert.Contains("processLogStopReading", html, StringComparison.Ordinal);
        Assert.Contains("processLogReadButton.classList.toggle('danger', processLogReading);", html, StringComparison.Ordinal);
        Assert.Contains("const maxProcessLogLines = 10000;", html, StringComparison.Ordinal);
        Assert.Contains("processLogEntries = processLogEntries.slice(-maxProcessLogLines);", html, StringComparison.Ordinal);
        var processLogModalStart = html.IndexOf("async function openProcessLogModal(", StringComparison.Ordinal);
        Assert.True(processLogModalStart >= 0);
        var processLogQueryStart = html.IndexOf("async function queryProcessLogs(", processLogModalStart, StringComparison.Ordinal);
        Assert.True(processLogQueryStart > processLogModalStart);
        var processLogModalSource = html[processLogModalStart..processLogQueryStart];
        Assert.DoesNotContain("api('/logs'", processLogModalSource, StringComparison.Ordinal);
        Assert.DoesNotContain("connectProcessLog(", processLogModalSource, StringComparison.Ordinal);
        var processLogToggleStart = html.IndexOf("async function toggleProcessLogReading(", processLogQueryStart, StringComparison.Ordinal);
        var processLogConnectStart = html.IndexOf("function connectProcessLog(", processLogToggleStart, StringComparison.Ordinal);
        Assert.True(processLogToggleStart > processLogQueryStart);
        Assert.True(processLogConnectStart > processLogToggleStart);
        var processLogToggleSource = html[processLogToggleStart..processLogConnectStart];
        Assert.Contains("connectProcessLog(processLogTargetKey);", processLogToggleSource, StringComparison.Ordinal);
        Assert.DoesNotContain("queryProcessLogs();", processLogToggleSource, StringComparison.Ordinal);
        Assert.DoesNotContain("processLogEntries = [];", processLogToggleSource, StringComparison.Ordinal);
        Assert.Contains("stopProcessLogReading(true);", processLogToggleSource, StringComparison.Ordinal);
        var processLogCloseStart = html.IndexOf("function closeProcessLogModal()", processLogConnectStart, StringComparison.Ordinal);
        Assert.True(processLogCloseStart > processLogConnectStart);
        var processLogCloseSource = html[processLogCloseStart..];
        Assert.Contains("stopProcessLogReading(false);", processLogCloseSource, StringComparison.Ordinal);
        Assert.Contains("processLogEntries = [];", processLogCloseSource, StringComparison.Ordinal);
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

    private static AirForgePanelMiddleware CreateMiddleware(bool requireTicket)
    {
        var options = new AirForgePanelOptions
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
        var authOptions = new ControlPlaneAuthOptions();
        var editor = new AirForgeTextFileEditor(options, new DynamicAppPackageStoreOptions());
        return new AirForgePanelMiddleware(
            _ => Task.CompletedTask,
            options,
            managerOptions,
            ControlPlaneAuthTestDoubles.CreateUnauthorizedService(),
            ControlPlaneAuthTestDoubles.CreateOptionsMonitor(authOptions),
            editor);
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
