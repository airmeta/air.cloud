using System.Net;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text.Json;

using Air.Cloud.AirForge.Panel.Extensions;
using Air.Cloud.AirForge.Panel.Logging;
using Air.Cloud.Capabilities.ControlPlaneAuth.Options;
using Air.Cloud.Capabilities.ControlPlaneAuth.Services;
using Air.Cloud.DynamicApp.Contracts.Identifiers;
using Air.Cloud.DynamicApp.Contracts.Lifecycle;
using Air.Cloud.DynamicApp.Contracts.Manifest;
using Air.Cloud.DynamicApp.Contracts.Runtime;
using Air.Cloud.DynamicApp.Host;
using Air.Cloud.DynamicApp.Manager;
using Air.Cloud.DynamicApp.Manager.Extensions;
using Air.Cloud.DynamicApp.IntegrationTest.Support;

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
        Assert.Contains("data-action=\"create-deployment\"", html, StringComparison.Ordinal);
        Assert.Contains("POST /deployments/create", html, StringComparison.Ordinal);
        Assert.Contains("data-i18n=\"packageManager\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"createPackageMod\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("id=\"openPackageUpload\"", html, StringComparison.Ordinal);
        Assert.Contains("data-action=\"package-upload\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("id=\"uploadMod\"", html, StringComparison.Ordinal);
        Assert.Contains("POST /packages/mods", html, StringComparison.Ordinal);
        Assert.Contains(".modal > .toolbar.actions", html, StringComparison.Ordinal);
        Assert.Contains("gap: 12px;", html, StringComparison.Ordinal);
        Assert.Contains("id=\"deploymentStepOne\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"deploymentStepTwo\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"deploymentStepThree\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"deploymentCommandPreview\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"deploymentEndpoint\" placeholder=\"http://127.0.0.1:5111\" readonly", html, StringComparison.Ordinal);
        Assert.Contains("function syncDeploymentEndpointFromPort()", html, StringComparison.Ordinal);
        Assert.Contains("function buildRuntimeFileTree(files)", html, StringComparison.Ordinal);
        Assert.Contains("class=\"runtime-folder\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"nextDeploymentStep\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"backDeploymentStep\"", html, StringComparison.Ordinal);
        Assert.Contains("Package Manager only uploads, extracts, views, and edits package files.", html, StringComparison.Ordinal);
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
        Assert.DoesNotContain("Register Container", html, StringComparison.Ordinal);
        Assert.DoesNotContain("true deploys, starts, and activates the Mod after upload", html, StringComparison.Ordinal);
        Assert.Contains("data-view-target=\"orphans\"", html, StringComparison.Ordinal);
        Assert.Contains("GET /deployments/orphans", html, StringComparison.Ordinal);
        Assert.Contains("POST /deployments/remove", html, StringComparison.Ordinal);
        Assert.Contains("data-i18n=\"routeManagement\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"routeModal\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"routeModalTitle\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"routeUpstreamTemplate\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"routeDownstreamTemplate\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"routeTargetMode\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"routeTargetVersion\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"routeTargetDeploymentId\"", html, StringComparison.Ordinal);
        Assert.Contains("function populateDynamicRouteTargetOptions()", html, StringComparison.Ordinal);
        Assert.Contains("function saveDynamicRoute()", html, StringComparison.Ordinal);
        Assert.Contains("function openDynamicRouteModal(route = null)", html, StringComparison.Ordinal);
        Assert.Contains("data-action=\"edit-route\"", html, StringComparison.Ordinal);
        Assert.Contains("route-edit-icon", html, StringComparison.Ordinal);
        Assert.Contains("transform: scaleX(-1)", html, StringComparison.Ordinal);
        Assert.Contains("method: editing ? 'PUT' : 'POST'", html, StringComparison.Ordinal);
        Assert.Contains("#view-routes > .toolbar", html, StringComparison.Ordinal);
        Assert.Contains("#routeModal .form-grid > label", html, StringComparison.Ordinal);
        Assert.DoesNotContain("const routeId = prompt", html, StringComparison.Ordinal);
        Assert.Contains("display: inline-flex;", html, StringComparison.Ordinal);
        Assert.Contains("align-items: center;", html, StringComparison.Ordinal);
        Assert.Contains("justify-content: center;", html, StringComparison.Ordinal);
        Assert.Contains("forwardableInstances", html, StringComparison.Ordinal);
        Assert.Contains("instance-card-footer", html, StringComparison.Ordinal);
        Assert.Contains("instance-footer-actions", html, StringComparison.Ordinal);
        Assert.Contains("position: fixed;", html, StringComparison.Ordinal);
        Assert.Contains("z-index: 1000;", html, StringComparison.Ordinal);
        Assert.Contains("getBoundingClientRect()", html, StringComparison.Ordinal);
        Assert.Contains("menu.style.left", html, StringComparison.Ordinal);
        Assert.Contains("menu.style.top", html, StringComparison.Ordinal);
        Assert.Contains("runtime-application-tabs", html, StringComparison.Ordinal);
        Assert.DoesNotContain("runtime-application-tab-stats", html, StringComparison.Ordinal);
        Assert.Contains("runtime-application-manager-layout", html, StringComparison.Ordinal);
        Assert.Contains("runtime-application-shell", html, StringComparison.Ordinal);
        Assert.Contains("runtime-application-tabs-header", html, StringComparison.Ordinal);
        Assert.Contains("role=\"tablist\"", html, StringComparison.Ordinal);
        Assert.Contains("role=\"tab\"", html, StringComparison.Ordinal);
        Assert.Contains("flex: 0 0 350px;", html, StringComparison.Ordinal);
        Assert.Contains("width: 350px;", html, StringComparison.Ordinal);
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
        var resourceGridStart = html.IndexOf(".resource-dashboard-row {", StringComparison.Ordinal);
        Assert.True(resourceGridStart >= 0);
        var resourceGridEnd = html.IndexOf('}', resourceGridStart);
        Assert.True(resourceGridEnd > resourceGridStart);
        Assert.Contains("align-items: start;", html[resourceGridStart..resourceGridEnd], StringComparison.Ordinal);
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

    /// <summary>
    /// <para>zh-cn:验证真实 ASP.NET Core 管线可以按 DeploymentId 返回指定集群副本的详情。</para>
    /// <para>en-us:Verifies that the real ASP.NET Core pipeline returns the requested clustered replica by DeploymentId.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "DynamicApp")]
    public async Task Manager_target_detail_should_resolve_specific_deployment_in_pipeline()
    {
        await using var app = await CreateAppAsync(includeCluster: true);
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/dynamicapp-manager/target/security-platform/account/101-process-current/Gray-A");
        request.Headers.TryAddWithoutValidation("X-DynamicApp-Manager-Ticket", "integration-ticket");

        var response = await app.GetTestClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Gray-A", document.RootElement.GetProperty("data").GetProperty("deploymentId").GetString());
    }

    /// <summary>
    /// <para>zh-cn:验证 AirForge 面板会先校验 ControlPlaneAuth，再把服务端 Manager Ticket 注入到动态应用管理请求中；未授权请求仍会被拒绝。</para>
    /// <para>en-us:Verifies that AirForge validates ControlPlaneAuth first and injects the server-side Manager Ticket into DynamicApp Manager requests, while an unauthorized request remains rejected.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "ControlPlaneAuth")]
    public async Task Manager_api_should_bridge_control_plane_auth_and_reject_anonymous_requests()
    {
        await using var authorizedApp = await CreateAppAsync(controlPlaneAuthorized: true);
        var authorizedResponse = await authorizedApp.GetTestClient().GetAsync("/dynamicapp-manager/targets");

        Assert.Equal(HttpStatusCode.OK, authorizedResponse.StatusCode);

        await using var anonymousApp = await CreateAppAsync(controlPlaneAuthorized: false);
        var anonymousResponse = await anonymousApp.GetTestClient().GetAsync("/dynamicapp-manager/targets");

        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);
    }

    /// <summary>
    /// <para>zh-cn:验证文本文件接口在真实管线中要求授权，并能读取和保存 UTF-8 内容。</para>
    /// <para>en-us:Verifies the text-file endpoint requires authorization in the real pipeline and can read and save UTF-8 content.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "AirForge")]
    public async Task FileEditing_should_require_ticket_and_round_trip_content()
    {
        var root = Directory.CreateTempSubdirectory("airforge-integration-");
        await using var app = await CreateAppAsync(root.FullName);
        try
        {
            var path = Path.Combine(root.FullName, "settings.json");
            await File.WriteAllTextAsync(path, "{\"value\":1}");
            var client = app.GetTestClient();

            var unauthorized = await client.GetAsync("/dynamicapp-panel/files?path=" + Uri.EscapeDataString(path));
            Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);

            using var readRequest = new HttpRequestMessage(HttpMethod.Get, "/dynamicapp-panel/files?path=" + Uri.EscapeDataString(path));
            readRequest.Headers.TryAddWithoutValidation("X-DynamicApp-Manager-Ticket", "integration-ticket");
            var readResponse = await client.SendAsync(readRequest);
            Assert.Equal(HttpStatusCode.OK, readResponse.StatusCode);
            using var readDocument = JsonDocument.Parse(await readResponse.Content.ReadAsStringAsync());
            var snapshot = readDocument.RootElement.GetProperty("data");
            Assert.Equal("{\"value\":1}", snapshot.GetProperty("content").GetString());

            using var writeRequest = new HttpRequestMessage(HttpMethod.Put, "/dynamicapp-panel/files");
            writeRequest.Headers.TryAddWithoutValidation("X-DynamicApp-Manager-Ticket", "integration-ticket");
            writeRequest.Content = JsonContent.Create(new
            {
                path,
                content = "{\"value\":\"中文\"}",
                expectedLastModifiedUtc = snapshot.GetProperty("lastModifiedUtc").GetDateTimeOffset()
            });
            var writeResponse = await client.SendAsync(writeRequest);
            Assert.Equal(HttpStatusCode.OK, writeResponse.StatusCode);
            Assert.Equal("{\"value\":\"中文\"}", await File.ReadAllTextAsync(path));
        }
        finally
        {
            root.Delete(true);
        }
    }

    /// <summary>
    /// <para>zh-cn:验证未登录客户端连接 Process 日志 Hub 后会被立即关闭。</para>
    /// <para>en-us:Verifies an unauthenticated client is immediately disconnected after connecting to the Process log Hub.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "AirForge")]
    public async Task ProcessLogHub_should_close_unauthorized_connection()
    {
        await using var app = await CreateAppAsync();
        var socketClient = app.GetTestServer().CreateWebSocketClient();
        var disconnected = false;

        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            using var socket = await socketClient.ConnectAsync(new Uri("ws://localhost/dynamicapp-panel/hubs/process-logs"), timeout.Token);
            var buffer = new byte[1024];
            var result = await socket.ReceiveAsync(buffer, timeout.Token);
            disconnected = result.MessageType == WebSocketMessageType.Close;
        }
        catch (WebSocketException)
        {
            disconnected = true;
        }
        catch (OperationCanceledException)
        {
            disconnected = true;
        }

        Assert.True(disconnected);
    }

    private static async Task<WebApplication> CreateAppAsync(
        string? packageRoot = null,
        bool includeCluster = false,
        bool controlPlaneAuthorized = false)
    {
        var stateStore = new InMemoryDynamicAppStateStore();
        var manifestStore = new InMemoryDynamicAppManifestStore();
        await SeedRuntimeAsync(stateStore, manifestStore, includeCluster);
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
        builder.Services.AddSingleton(new DynamicAppPackageStoreOptions
        {
            RootPath = packageRoot ?? Path.Combine(AppContext.BaseDirectory, "dynamicapp-packages")
        });
        builder.Services.AddSingleton<IControlPlaneAuthService>(
            controlPlaneAuthorized
                ? ControlPlaneAuthTestDoubles.CreateAuthorizedService()
                : ControlPlaneAuthTestDoubles.CreateUnauthorizedService());
        builder.Services.AddSingleton(ControlPlaneAuthTestDoubles.CreateOptionsMonitor(new ControlPlaneAuthOptions()));
        builder.Services.AddSingleton<DynamicAppProcessLogBroadcaster>();
        builder.Services.AddSignalR();
        builder.Services.AddAirForgePanel(options =>
        {
            options.Enabled = true;
            options.RoutePrefix = "/dynamicapp-panel";
            options.ResourceStatsSampleInterval = TimeSpan.FromSeconds(10);
        });

        var app = builder.Build();
        app.UseRouting();
        app.UseAirForgePanel();
        app.UseDynamicAppManagerApi();
        app.MapHub<AirForgeProcessLogHub>("/dynamicapp-panel/hubs/process-logs");
        app.UseEndpoints(_ => { });
        app.Run(context =>
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return Task.CompletedTask;
        });
        await app.StartAsync();
        return app;
    }

    private static async Task SeedRuntimeAsync(InMemoryDynamicAppStateStore stateStore, InMemoryDynamicAppManifestStore manifestStore, bool includeCluster = false)
    {
        var appId = new DynamicAppId("security-platform");
        var accountId = new DynamicModId("account");
        var oldAccountKey = new DynamicModVersionKey(appId, accountId, new DynamicAppVersion("101-current"));
        var currentAccountKey = new DynamicModVersionKey(appId, accountId, new DynamicAppVersion("101-process-current"));
        stateStore.UpsertTarget(CreateTarget(oldAccountKey, "air.account", DynamicModLifecycleState.Draining, DynamicModHealthStatus.Draining));
        stateStore.UpsertTarget(CreateTarget(currentAccountKey, Environment.ProcessId.ToString(), DynamicModLifecycleState.Active, DynamicModHealthStatus.Healthy));
        if (includeCluster)
        {
            var clusterTarget = CreateTarget(currentAccountKey, "process-gray-a", DynamicModLifecycleState.Active, DynamicModHealthStatus.Healthy);
            clusterTarget.DeploymentId = "Gray-A";
            clusterTarget.BaseAddress = new Uri("http://127.0.0.1:5111");
            stateStore.UpsertTarget(clusterTarget);
        }
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
