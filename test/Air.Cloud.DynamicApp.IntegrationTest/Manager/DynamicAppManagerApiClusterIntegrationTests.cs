using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;

using Air.Cloud.DynamicApp.Contracts.Identifiers;
using Air.Cloud.DynamicApp.Contracts.Lifecycle;
using Air.Cloud.DynamicApp.Contracts.Manifest;
using Air.Cloud.DynamicApp.Contracts.Runtime;
using Air.Cloud.DynamicApp.Manager;
using Air.Cloud.DynamicApp.Manager.Extensions;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.DynamicApp.IntegrationTest.Manager;

/// <summary>
/// <para>zh-cn:DynamicApp 集群部署创建接口的集成测试，验证真实 ASP.NET Core 管线中的副本登记和冲突响应。</para>
/// <para>en-us:Integration tests for the DynamicApp clustered deployment-create API, verifying replica registration and conflict responses through the real ASP.NET Core pipeline.</para>
/// </summary>
public sealed class DynamicAppManagerApiClusterIntegrationTests
{
    /// <summary>
    /// <para>zh-cn:验证真实管线可以创建新的隔离部署副本，并返回副本目录和监听地址。</para>
    /// <para>en-us:Verifies that the real pipeline creates an isolated deployment replica and returns its directory and listener address.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "DynamicApp")]
    public async Task Create_deployment_should_register_isolated_replica_in_pipeline()
    {
        using var workspace = ClusterIntegrationWorkspace.Create();
        await using var app = await CreateAppAsync(workspace);
        var response = await SendCreateAsync(app.GetTestClient(), "blue", 5111);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var data = document.RootElement.GetProperty("data");
        Assert.Equal("blue", data.GetProperty("deploymentId").GetString());
        Assert.Contains("deployments", data.GetProperty("deploymentDirectory").GetString(), StringComparison.OrdinalIgnoreCase);
        var target = Assert.Single(app.Services.GetRequiredService<IDynamicAppStateStore>().ListTargets());
        Assert.Equal(new Uri("http://127.0.0.1:5111"), target.BaseAddress);
    }

    /// <summary>
    /// <para>zh-cn:验证真实 DI 管线中的实例级激活只切换指定副本，不会整体激活同版本兄弟副本。</para>
    /// <para>en-us:Verifies through the real DI pipeline that instance activation switches only the requested replica instead of activating all siblings.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "DynamicApp")]
    public async Task Activate_instance_should_preserve_sibling_replica_state_in_pipeline()
    {
        using var workspace = ClusterIntegrationWorkspace.Create();
        await using var app = await CreateAppAsync(workspace);
        var stateStore = app.Services.GetRequiredService<IDynamicAppStateStore>();
        var key = ClusterIntegrationWorkspace.PackageKey;
        stateStore.UpsertTarget(new DynamicAppRouteTarget { Key = key, DeploymentId = "blue", State = DynamicModLifecycleState.Pending, HealthStatus = DynamicModHealthStatus.Unknown });
        stateStore.UpsertTarget(new DynamicAppRouteTarget { Key = key, DeploymentId = "gray", State = DynamicModLifecycleState.Pending, HealthStatus = DynamicModHealthStatus.Unknown });

        var result = await app.Services.GetRequiredService<IDynamicAppManager>()
            .ActivateInstanceAsync(new DynamicAppRuntimeKey(key, "blue"));

        Assert.True(result.Succeeded, result.Message);
        var targets = stateStore.ListTargets().ToDictionary(item => item.DeploymentId);
        Assert.Equal(DynamicModLifecycleState.Active, targets["blue"].State);
        Assert.Equal(DynamicModLifecycleState.Pending, targets["gray"].State);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "DynamicApp")]
    public async Task Dynamic_route_api_should_require_confirmation_then_persist_route()
    {
        using var workspace = ClusterIntegrationWorkspace.Create();
        await using var app = await CreateAppAsync(workspace);
        var client = app.GetTestClient();
        var payload = new
        {
            route = new
            {
                routeId = "integration-health",
                name = "Integration health",
                upstreamPathTemplate = "/integration/{id}",
                downstreamPathTemplate = "/internal/{id}",
                targetDynamicAppId = "security-platform",
                targetModId = "auth",
                targetVersion = "missing",
                order = 10,
                enabled = true
            },
            confirmUnavailable = false
        };
        using var first = new HttpRequestMessage(HttpMethod.Post, "/dynamicapp-manager/routes")
        {
            Content = JsonContent.Create(payload)
        };
        first.Headers.TryAddWithoutValidation("X-DynamicApp-Manager-Ticket", "integration-ticket");
        using var firstResponse = await client.SendAsync(first);
        Assert.Equal(HttpStatusCode.Conflict, firstResponse.StatusCode);

        using var second = new HttpRequestMessage(HttpMethod.Post, "/dynamicapp-manager/routes")
        {
            Content = JsonContent.Create(new { route = payload.route, confirmUnavailable = true })
        };
        second.Headers.TryAddWithoutValidation("X-DynamicApp-Manager-Ticket", "integration-ticket");
        using var secondResponse = await client.SendAsync(second);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        Assert.Single(app.Services.GetRequiredService<IDynamicAppGatewayRouteStore>().List());
    }

    /// <summary>
    /// <para>zh-cn:验证真实管线会在创建阶段拒绝已占用的监听端点，并返回可读的统一错误内容。</para>
    /// <para>en-us:Verifies that the real pipeline rejects an occupied listener endpoint during creation and returns a readable unified error payload.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "DynamicApp")]
    public async Task Create_deployment_should_return_bad_request_for_endpoint_conflict_in_pipeline()
    {
        using var workspace = ClusterIntegrationWorkspace.Create();
        await using var app = await CreateAppAsync(workspace);
        var stateStore = app.Services.GetRequiredService<IDynamicAppStateStore>();
        var key = ClusterIntegrationWorkspace.PackageKey;
        stateStore.UpsertTarget(new DynamicAppRouteTarget
        {
            Key = key,
            DeploymentId = "blue",
            BaseAddress = new Uri("http://127.0.0.1:5111/existing"),
            State = DynamicModLifecycleState.Pending,
            HealthStatus = DynamicModHealthStatus.Unknown
        });

        var response = await SendCreateAsync(app.GetTestClient(), "gray", 5111, "http://127.0.0.1:5111/new");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Contains(
            "Deployment endpoint `http://127.0.0.1:5111/new` is already used by",
            document.RootElement.GetProperty("message").GetString() ?? string.Empty,
            StringComparison.Ordinal);
    }

    /// <summary>
    /// <para>zh-cn:验证真实管线在并发创建相同监听端点时只登记一个部署副本。</para>
    /// <para>en-us:Verifies that the real pipeline registers only one deployment replica when concurrent requests use the same listener endpoint.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "DynamicApp")]
    public async Task Create_deployment_should_serialize_concurrent_endpoint_requests_in_pipeline()
    {
        using var workspace = ClusterIntegrationWorkspace.Create();
        await using var app = await CreateAppAsync(workspace);
        var client = app.GetTestClient();

        var responses = await Task.WhenAll(
            SendCreateAsync(client, "blue", 5111),
            SendCreateAsync(client, "gray", 5111));

        Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.OK));
        Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.BadRequest));
        Assert.Single(app.Services.GetRequiredService<IDynamicAppStateStore>().ListTargets());
        foreach (var response in responses)
        {
            response.Dispose();
        }
    }

    /// <summary>
    /// <para>zh-cn:验证真实管线会在目录复制前拒绝已经被本机监听的部署端口。</para>
    /// <para>en-us:Verifies that the real pipeline rejects a locally occupied deployment port before copying the deployment directory.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "DynamicApp")]
    public async Task Create_deployment_should_return_bad_request_for_locally_occupied_port_in_pipeline()
    {
        using var workspace = ClusterIntegrationWorkspace.Create();
        await using var app = await CreateAppAsync(workspace);
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var response = await SendCreateAsync(app.GetTestClient(), "occupied", port);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Contains(
            "is not available for local binding",
            document.RootElement.GetProperty("message").GetString() ?? string.Empty,
            StringComparison.Ordinal);
        Assert.Empty(app.Services.GetRequiredService<IDynamicAppStateStore>().ListTargets());
    }

    /// <summary>
    /// <para>zh-cn:验证真实管理 API 拒绝非 DLL 入口，并且不会创建运行副本或状态记录。</para>
    /// <para>en-us:Verifies that the real Manager API rejects a non-DLL entry and creates neither a runtime copy nor state.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "DynamicApp")]
    public async Task Create_deployment_should_reject_non_dll_entry_in_pipeline()
    {
        using var workspace = ClusterIntegrationWorkspace.Create();
        await using var app = await CreateAppAsync(workspace);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/dynamicapp-manager/deployments/create");
        request.Headers.TryAddWithoutValidation("X-DynamicApp-Manager-Ticket", "integration-ticket");
        request.Content = JsonContent.Create(new
        {
            dynamicAppId = ClusterIntegrationWorkspace.PackageKey.DynamicAppId.Value,
            modId = ClusterIntegrationWorkspace.PackageKey.ModId.Value,
            version = ClusterIntegrationWorkspace.PackageKey.Version.Value,
            deploymentId = "invalid-entry",
            port = 5111,
            entryFile = "appsettings.json",
            start = false
        });

        var response = await app.GetTestClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(app.Services.GetRequiredService<IDynamicAppStateStore>().ListTargets());
        Assert.False(Directory.Exists(Path.Combine(workspace.RootPath, "deployments", "security-platform", "testmod", "1.0.0", "invalid-entry")));
    }

    /// <summary>
    /// <para>zh-cn:验证管理 API 删除副本时会清理隔离目录、运行状态和自愈状态，且不会影响同包其他副本。</para>
    /// <para>en-us:Verifies that the Manager API removes one replica directory, runtime state, and self-healing state without affecting another replica of the same package.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "DynamicApp")]
    public async Task Remove_deployment_should_remove_only_requested_replica_in_pipeline()
    {
        using var workspace = ClusterIntegrationWorkspace.Create();
        await using var app = await CreateAppAsync(workspace);
        var client = app.GetTestClient();
        var createBlue = await SendCreateAsync(client, "blue", 5111);
        var createGray = await SendCreateAsync(client, "gray", 5112);
        Assert.Equal(HttpStatusCode.OK, createBlue.StatusCode);
        Assert.Equal(HttpStatusCode.OK, createGray.StatusCode);

        var blueTarget = app.Services.GetRequiredService<IDynamicAppStateStore>()
            .ListTargets()
            .Single(item => item.DeploymentId == "blue");
        var remove = await SendRemoveAsync(client, "blue");
        var removeBody = await remove.Content.ReadAsStringAsync();

        Assert.True(remove.StatusCode == HttpStatusCode.OK, removeBody);
        Assert.DoesNotContain(
            app.Services.GetRequiredService<IDynamicAppStateStore>().ListTargets(),
            item => item.DeploymentId == "blue");
        Assert.Contains(
            app.Services.GetRequiredService<IDynamicAppStateStore>().ListTargets(),
            item => item.DeploymentId == "gray");
        Assert.False(Directory.Exists(blueTarget.DeploymentDirectory));
    }

    /// <summary>
    /// <para>zh-cn:验证管理 API 可以列出磁盘上没有运行状态登记的孤儿部署目录。</para>
    /// <para>en-us:Verifies that the Manager API lists an orphan deployment directory that has no runtime-state registration.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "DynamicApp")]
    public async Task Orphan_deployment_endpoint_should_list_unregistered_directory_in_pipeline()
    {
        using var workspace = ClusterIntegrationWorkspace.Create();
        await using var app = await CreateAppAsync(workspace);
        await app.Services.GetRequiredService<IDynamicAppModPackageStore>()
            .CreateDeploymentAsync(ClusterIntegrationWorkspace.PackageKey, "orphan");

        using var request = new HttpRequestMessage(HttpMethod.Get, "/dynamicapp-manager/deployments/orphans");
        request.Headers.TryAddWithoutValidation("X-DynamicApp-Manager-Ticket", "integration-ticket");
        var response = await app.GetTestClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var orphan = Assert.Single(document.RootElement.GetProperty("data").EnumerateArray());
        Assert.Equal("orphan", orphan.GetProperty("deploymentId").GetString());
    }

    private static async Task<WebApplication> CreateAppAsync(ClusterIntegrationWorkspace workspace)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddDynamicAppManager(
            options =>
            {
                options.RoutePrefix = "/dynamicapp-manager";
                options.RequireTicket = true;
                options.Tickets = ["integration-ticket"];
            },
            options => options.RootPath = workspace.RootPath);
        builder.Services.AddSingleton<IDynamicAppGatewayRouteStore, InMemoryDynamicAppGatewayRouteStore>();

        var app = builder.Build();
        await SeedPackageAsync(
            app.Services.GetRequiredService<IDynamicAppModPackageStore>(),
            app.Services.GetRequiredService<IDynamicAppManifestStore>());
        app.UseDynamicAppManagerApi();
        app.Run(context =>
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return Task.CompletedTask;
        });
        await app.StartAsync();
        return app;
    }

    private static async Task<HttpResponseMessage> SendCreateAsync(
        HttpClient client,
        string deploymentId,
        int port,
        string? endpoint = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/dynamicapp-manager/deployments/create");
        request.Headers.TryAddWithoutValidation("X-DynamicApp-Manager-Ticket", "integration-ticket");
        request.Content = JsonContent.Create(new
        {
            dynamicAppId = ClusterIntegrationWorkspace.PackageKey.DynamicAppId.Value,
            modId = ClusterIntegrationWorkspace.PackageKey.ModId.Value,
            version = ClusterIntegrationWorkspace.PackageKey.Version.Value,
            deploymentId,
            port,
            endpoint,
            start = false
        });
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> SendRemoveAsync(HttpClient client, string deploymentId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/dynamicapp-manager/deployments/remove");
        request.Headers.TryAddWithoutValidation("X-DynamicApp-Manager-Ticket", "integration-ticket");
        request.Content = JsonContent.Create(new
        {
            dynamicAppId = ClusterIntegrationWorkspace.PackageKey.DynamicAppId.Value,
            modId = ClusterIntegrationWorkspace.PackageKey.ModId.Value,
            version = ClusterIntegrationWorkspace.PackageKey.Version.Value,
            deploymentId
        });
        return await client.SendAsync(request);
    }

    private static async Task SeedPackageAsync(
        IDynamicAppModPackageStore packageStore,
        IDynamicAppManifestStore manifestStore)
    {
        await using var package = CreatePackageStream("testmod.dll");
        await packageStore.SaveAsync(new DynamicAppModPackageSaveRequest
        {
            Key = ClusterIntegrationWorkspace.PackageKey,
            OriginalFileName = "testmod.zip",
            EntryFile = "testmod.dll"
        }, package);
        await manifestStore.SaveAsync(new DynamicAppManifest
        {
            Id = ClusterIntegrationWorkspace.PackageKey.DynamicAppId,
            Version = new DynamicAppVersion("1.0.0"),
            Mods =
            [
                new DynamicModManifest
                {
                    Id = ClusterIntegrationWorkspace.PackageKey.ModId,
                    Version = ClusterIntegrationWorkspace.PackageKey.Version,
                    ExecutionMode = DynamicModExecutionMode.Process,
                    Artifact = new DynamicModArtifactManifest
                    {
                        File = "testmod.dll",
                        Endpoint = "http://127.0.0.1:5100"
                    },
                    Health = new DynamicModHealthManifest { Path = "/Health", StartupTimeoutSeconds = 10 },
                }
            ]
        });
    }

    private static MemoryStream CreatePackageStream(string entryFile)
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry(entryFile);
            using var writer = new StreamWriter(entry.Open());
            writer.Write("test package payload");
        }

        stream.Position = 0;
        return stream;
    }
}

/// <summary>
/// <para>zh-cn:集群部署集成测试的临时包存储工作区，负责隔离文件并在测试结束后清理。</para>
/// <para>en-us:Temporary package-storage workspace for clustered deployment integration tests, isolating files and cleaning them after each test.</para>
/// </summary>
internal sealed class ClusterIntegrationWorkspace : IDisposable
{
    private ClusterIntegrationWorkspace(string rootPath)
    {
        RootPath = rootPath;
    }

    /// <summary>
    /// <para>zh-cn:测试包存储根目录。</para>
    /// <para>en-us:The package-storage root directory for the test.</para>
    /// </summary>
    public string RootPath { get; }

    /// <summary>
    /// <para>zh-cn:集群创建测试使用的固定包键。</para>
    /// <para>en-us:The fixed package key used by clustered deployment tests.</para>
    /// </summary>
    public static DynamicModVersionKey PackageKey { get; } = new(
        new DynamicAppId("security-platform"),
        new DynamicModId("testmod"),
        new DynamicAppVersion("1.0.0"));

    /// <summary>
    /// <para>zh-cn:创建隔离的集成测试工作区。</para>
    /// <para>en-us:Creates an isolated integration-test workspace.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:可释放的临时工作区。</para>
    /// <para>en-us:A disposable temporary workspace.</para>
    /// </returns>
    public static ClusterIntegrationWorkspace Create()
    {
        var root = Path.Combine(Path.GetTempPath(), "air-cloud-dynamicapp-cluster-integration-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return new ClusterIntegrationWorkspace(root);
    }

    /// <summary>
    /// <para>zh-cn:删除测试工作区；文件被外部占用时忽略清理异常。</para>
    /// <para>en-us:Deletes the test workspace and ignores cleanup failures caused by externally locked files.</para>
    /// </summary>
    public void Dispose()
    {
        try
        {
            if (Directory.Exists(RootPath))
            {
                Directory.Delete(RootPath, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
