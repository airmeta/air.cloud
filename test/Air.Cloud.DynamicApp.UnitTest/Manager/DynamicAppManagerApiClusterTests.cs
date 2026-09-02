using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

using Air.Cloud.DynamicApp.Contracts.Identifiers;
using Air.Cloud.DynamicApp.Contracts.Lifecycle;
using Air.Cloud.DynamicApp.Contracts.Manifest;
using Air.Cloud.DynamicApp.Contracts.Runtime;
using Air.Cloud.DynamicApp.Host;
using Air.Cloud.DynamicApp.Manager;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.DynamicApp.UnitTest.Manager;

/// <summary>
/// <para>zh-cn:DynamicApp Manager 集群实例路由单元测试。</para>
/// <para>en-us:Unit tests for DynamicApp Manager clustered-instance routes.</para>
/// </summary>
public sealed class DynamicAppManagerApiClusterTests
{
    /// <summary>
    /// <para>zh-cn:验证带 DeploymentId 的详情路由不会被统一路由小写逻辑破坏，并能返回指定部署实例。</para>
    /// <para>en-us:Verifies that a DeploymentId-qualified detail route survives route normalization and returns the requested deployment instance.</para>
    /// </summary>
    [Fact]
    public async Task Target_route_should_preserve_deployment_id_and_return_matching_instance()
    {
        var key = new DynamicModVersionKey(
            new DynamicAppId("security-platform"),
            new DynamicModId("auth"),
            new DynamicAppVersion("101-process-current"));
        var stateStore = new InMemoryDynamicAppStateStore();
        stateStore.UpsertTarget(new DynamicAppRouteTarget
        {
            Key = key,
            DeploymentId = "Gray-A",
            BaseAddress = new Uri("http://127.0.0.1:5111"),
            HealthPath = "/Health",
            State = DynamicModLifecycleState.Active,
            HealthStatus = DynamicModHealthStatus.Healthy,
            InstanceId = "process-1"
        });

        var services = new ServiceCollection()
            .AddSingleton<IDynamicAppStateStore>(stateStore)
            .BuildServiceProvider();
        var context = new DefaultHttpContext
        {
            RequestServices = services
        };
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/dynamicapp-manager/target/security-platform/auth/101-process-current/Gray-A";
        context.Request.Headers["X-DynamicApp-Manager-Ticket"] = "cluster-ticket";
        context.Response.Body = new MemoryStream();

        var middleware = new DynamicAppManagerApiMiddleware(
            _ => Task.CompletedTask,
            new DynamicAppManagerApiOptions
            {
                RequireTicket = true,
                Tickets = ["cluster-ticket"]
            });

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        using var responseReader = new StreamReader(context.Response.Body, leaveOpen: true);
        var responseBody = await responseReader.ReadToEndAsync();
        Assert.True(context.Response.StatusCode == StatusCodes.Status200OK, responseBody);
        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        var target = document.RootElement.GetProperty("data");
        Assert.Equal("Gray-A", target.GetProperty("deploymentId").GetString());
        Assert.Equal("http://127.0.0.1:5111", target.GetProperty("baseAddress").GetString());
        Assert.Equal("process-1", target.GetProperty("instanceId").GetString());
    }

    /// <summary>
    /// <para>zh-cn:验证创建部署接口可以从已有包复制独立目录并登记新的集群副本。</para>
    /// <para>en-us:Verifies that the deployment-create API copies an existing package into an isolated directory and registers a new clustered replica.</para>
    /// </summary>
    [Fact]
    public async Task Create_deployment_should_register_new_isolated_replica()
    {
        using var workspace = DynamicAppTestWorkspace.Create();
        var key = await SeedProcessPackageAsync(workspace);
        var stateStore = new InMemoryDynamicAppStateStore();
        var manifestStore = workspace.ManifestStore;
        var context = CreateDeploymentContext(
            stateStore,
            manifestStore,
            workspace.PackageStore,
            workspace.RootPath,
            new
            {
                dynamicAppId = key.DynamicAppId.Value,
                modId = key.ModId.Value,
                version = key.Version.Value,
                deploymentId = "blue",
                port = 5111,
                start = false
            });

        await InvokeManagerAsync(context);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        var target = Assert.Single(stateStore.ListTargets());
        Assert.Equal("blue", target.DeploymentId);
        Assert.Equal(new Uri("http://127.0.0.1:5111"), target.BaseAddress);
        Assert.True(File.Exists(target.DeploymentEntryFilePath));
        Assert.True(File.Exists(Path.Combine(workspace.RootPath, ".locks", "deployment-create.lock")));
    }

    /// <summary>
    /// <para>zh-cn:验证同一 DynamicApp、Mod、版本下重复使用 DeploymentId 会在复制文件前被拒绝。</para>
    /// <para>en-us:Verifies that reusing a DeploymentId for the same DynamicApp, Mod, and version is rejected before files are copied.</para>
    /// </summary>
    [Fact]
    public async Task Create_deployment_should_reject_duplicate_deployment_id()
    {
        using var workspace = DynamicAppTestWorkspace.Create();
        var key = await SeedProcessPackageAsync(workspace);
        var stateStore = new InMemoryDynamicAppStateStore();
        stateStore.UpsertTarget(new DynamicAppRouteTarget
        {
            Key = key,
            DeploymentId = "blue",
            BaseAddress = new Uri("http://127.0.0.1:5111"),
            State = DynamicModLifecycleState.Pending,
            HealthStatus = DynamicModHealthStatus.Unknown
        });
        var context = CreateDeploymentContext(
            stateStore,
            workspace.ManifestStore,
            workspace.PackageStore,
            workspace.RootPath,
            new
            {
                dynamicAppId = key.DynamicAppId.Value,
                modId = key.ModId.Value,
                version = key.Version.Value,
                deploymentId = "blue",
                port = 5112,
                start = false
            });

        await InvokeManagerAsync(context);

        var body = await ReadResponseMessageAsync(context);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Contains($"DynamicApp deployment `{key}/blue` already exists.", body, StringComparison.Ordinal);
        Assert.Single(stateStore.ListTargets());
        Assert.False(Directory.Exists(Path.Combine(workspace.RootPath, "deployments")));
    }

    /// <summary>
    /// <para>zh-cn:验证不同副本使用相同协议、主机和端口时会被拒绝，即使 URL 路径不同也不能绕过监听冲突。</para>
    /// <para>en-us:Verifies that different replicas using the same scheme, host, and port are rejected even when their URL paths differ.</para>
    /// </summary>
    [Fact]
    public async Task Create_deployment_should_reject_listener_endpoint_conflict()
    {
        using var workspace = DynamicAppTestWorkspace.Create();
        var key = await SeedProcessPackageAsync(workspace);
        var stateStore = new InMemoryDynamicAppStateStore();
        stateStore.UpsertTarget(new DynamicAppRouteTarget
        {
            Key = key,
            DeploymentId = "blue",
            BaseAddress = new Uri("http://127.0.0.1:5111/existing"),
            State = DynamicModLifecycleState.Pending,
            HealthStatus = DynamicModHealthStatus.Unknown
        });
        var context = CreateDeploymentContext(
            stateStore,
            workspace.ManifestStore,
            workspace.PackageStore,
            workspace.RootPath,
            new
            {
                dynamicAppId = key.DynamicAppId.Value,
                modId = key.ModId.Value,
                version = key.Version.Value,
                deploymentId = "gray",
                port = 5111,
                endpoint = "http://127.0.0.1:5111/new",
                start = false
            });

        await InvokeManagerAsync(context);

        var body = await ReadResponseMessageAsync(context);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Contains("Deployment endpoint `http://127.0.0.1:5111/new` is already used by", body, StringComparison.Ordinal);
        Assert.Single(stateStore.ListTargets());
        Assert.False(Directory.Exists(Path.Combine(workspace.RootPath, "deployments")));
    }

    /// <summary>
    /// <para>zh-cn:验证并发创建相同监听端点时只有一个请求能够完成登记，另一个请求会在创建闸门内收到冲突响应。</para>
    /// <para>en-us:Verifies that concurrent creation requests for one listener endpoint allow only one registration and return a conflict for the other request inside the creation gate.</para>
    /// </summary>
    [Fact]
    public async Task Create_deployment_should_serialize_concurrent_listener_conflicts()
    {
        using var workspace = DynamicAppTestWorkspace.Create();
        var key = await SeedProcessPackageAsync(workspace);
        var stateStore = new InMemoryDynamicAppStateStore();
        var contextA = CreateDeploymentContext(
            stateStore,
            workspace.ManifestStore,
            workspace.PackageStore,
            workspace.RootPath,
            new
            {
                dynamicAppId = key.DynamicAppId.Value,
                modId = key.ModId.Value,
                version = key.Version.Value,
                deploymentId = "blue",
                port = 5111,
                start = false
            });
        var contextB = CreateDeploymentContext(
            stateStore,
            workspace.ManifestStore,
            workspace.PackageStore,
            workspace.RootPath,
            new
            {
                dynamicAppId = key.DynamicAppId.Value,
                modId = key.ModId.Value,
                version = key.Version.Value,
                deploymentId = "gray",
                port = 5111,
                start = false
            });

        await Task.WhenAll(InvokeManagerAsync(contextA), InvokeManagerAsync(contextB));

        Assert.Equal(1, new[] { contextA.Response.StatusCode, contextB.Response.StatusCode }.Count(status => status == StatusCodes.Status200OK));
        Assert.Equal(1, new[] { contextA.Response.StatusCode, contextB.Response.StatusCode }.Count(status => status == StatusCodes.Status400BadRequest));
        Assert.Single(stateStore.ListTargets());
    }

    /// <summary>
    /// <para>zh-cn:验证创建部署前会拒绝已经被其他本机进程监听的端口，并且不会复制部署目录。</para>
    /// <para>en-us:Verifies that deployment creation rejects a port already listened to by another local process and does not copy a deployment directory.</para>
    /// </summary>
    [Fact]
    public async Task Create_deployment_should_reject_port_already_bound_locally()
    {
        using var workspace = DynamicAppTestWorkspace.Create();
        var key = await SeedProcessPackageAsync(workspace);
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var stateStore = new InMemoryDynamicAppStateStore();
        var context = CreateDeploymentContext(
            stateStore,
            workspace.ManifestStore,
            workspace.PackageStore,
            workspace.RootPath,
            new
            {
                dynamicAppId = key.DynamicAppId.Value,
                modId = key.ModId.Value,
                version = key.Version.Value,
                deploymentId = "occupied",
                port,
                start = false
            });

        await InvokeManagerAsync(context);

        var message = await ReadResponseMessageAsync(context);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Contains("is not available for local binding", message, StringComparison.Ordinal);
        Assert.Empty(stateStore.ListTargets());
        Assert.False(Directory.Exists(Path.Combine(workspace.RootPath, "deployments")));
    }

    /// <summary>
    /// <para>zh-cn:验证更新会先停止旧实例并复用旧监听端点，即使请求体携带了不同端口也不会触发自身端点冲突。</para>
    /// <para>en-us:Verifies that update stops the old instance first and reuses its listener, so a different port in the request cannot trigger a self-conflict.</para>
    /// </summary>
    [Fact]
    public async Task Update_deployment_should_stop_old_target_and_keep_its_listener()
    {
        using var workspace = DynamicAppTestWorkspace.Create();
        var oldKey = new DynamicModVersionKey(
            new DynamicAppId("security-platform"),
            new DynamicModId("testmod"),
            new DynamicAppVersion("1.0.0"));
        var newKey = new DynamicModVersionKey(oldKey.DynamicAppId, oldKey.ModId, new DynamicAppVersion("2.0.0"));
        await SaveLocalPackageAsync(workspace, oldKey);
        await SaveLocalPackageAsync(workspace, newKey);
        await SaveLocalManifestAsync(workspace.ManifestStore, oldKey);
        await SaveLocalManifestAsync(workspace.ManifestStore, newKey);

        var oldDeployment = await workspace.PackageStore.CreateDeploymentAsync(oldKey, "default");
        var stateStore = new InMemoryDynamicAppStateStore();
        stateStore.UpsertTarget(new DynamicAppRouteTarget
        {
            Key = oldKey,
            DeploymentId = "default",
            DeploymentDirectory = oldDeployment.DeploymentDirectory,
            DeploymentEntryFilePath = oldDeployment.EntryFilePath,
            BaseAddress = new Uri("http://127.0.0.1:5112"),
            HealthPath = "/Health",
            HealthProbeTimeoutSeconds = 10,
            State = DynamicModLifecycleState.Active,
            HealthStatus = DynamicModHealthStatus.Healthy,
            InstanceId = "local-test"
        });

        var services = new ServiceCollection()
            .AddSingleton<IDynamicAppStateStore>(stateStore)
            .AddSingleton<IDynamicAppManifestStore>(workspace.ManifestStore)
            .AddSingleton<IDynamicAppModPackageStore>(workspace.PackageStore)
            .AddSingleton(new DynamicAppPackageStoreOptions { RootPath = workspace.RootPath })
            .AddSingleton<IDynamicAppRuntimeOperationCoordinator, DynamicAppRuntimeOperationCoordinator>()
            .AddSingleton<IDynamicAppManifestValidator, DefaultDynamicAppManifestValidator>()
            .AddSingleton<IDynamicAppManager, DefaultDynamicAppManager>()
            .AddSingleton<IDynamicAppTargetArchiveStore>(new FileDynamicAppTargetArchiveStore(new FileDynamicAppTargetArchiveStoreOptions
            {
                FilePath = Path.Combine(workspace.RootPath, "archives.json")
            }))
            .AddSingleton<IDynamicAppSelfHealingStateStore>(new FileDynamicAppSelfHealingStateStore(new FileDynamicAppSelfHealingStateStoreOptions
            {
                FilePath = Path.Combine(workspace.RootPath, "self-healing.json")
            }))
            .AddSingleton<IDynamicAppHealthProbe, AlwaysHealthyProbe>()
            .AddSingleton<IDynamicAppModRunner, SuccessfulRunner>()
            .BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = services };
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/dynamicapp-manager/deployments/update";
        context.Request.ContentType = "application/json";
        context.Request.Body = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(new
        {
            currentDynamicAppId = oldKey.DynamicAppId.Value,
            currentModId = oldKey.ModId.Value,
            currentVersion = oldKey.Version.Value,
            currentDeploymentId = "default",
            dynamicAppId = newKey.DynamicAppId.Value,
            modId = newKey.ModId.Value,
            version = newKey.Version.Value,
            deploymentId = "default-next",
            port = 5120,
            endpoint = "http://127.0.0.1:5120",
            entryFile = "testmod.dll"
        }));
        context.Request.Headers["X-DynamicApp-Manager-Ticket"] = "cluster-ticket";
        context.Response.Body = new MemoryStream();

        await InvokeManagerAsync(context);

        var body = await ReadResponseMessageAsync(context);
        Assert.True(context.Response.StatusCode == StatusCodes.Status200OK, body);
        var updated = Assert.Single(stateStore.ListTargets().Where(target => target.Key == newKey));
        Assert.Equal(new Uri("http://127.0.0.1:5112"), updated.BaseAddress);
        Assert.Equal(DynamicModLifecycleState.Active, updated.State);
        Assert.Equal(DynamicModLifecycleState.Stopped, Assert.Single(stateStore.ListTargets().Where(target => target.Key == oldKey)).State);
        Assert.False(body.Contains("already used", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// <para>zh-cn:验证新版本启动失败时，更新流程会重新启动并激活已经停止的旧实例。</para>
    /// <para>en-us:Verifies that an update restarts and reactivates the stopped previous instance when the new version fails to start.</para>
    /// </summary>
    [Fact]
    public async Task Update_deployment_should_restore_previous_target_when_new_start_fails()
    {
        using var workspace = DynamicAppTestWorkspace.Create();
        var oldKey = new DynamicModVersionKey(new DynamicAppId("security-platform"), new DynamicModId("testmod"), new DynamicAppVersion("1.0.0"));
        var newKey = new DynamicModVersionKey(oldKey.DynamicAppId, oldKey.ModId, new DynamicAppVersion("2.0.0"));
        await SaveLocalPackageAsync(workspace, oldKey);
        await SaveLocalPackageAsync(workspace, newKey);
        await SaveLocalManifestAsync(workspace.ManifestStore, oldKey);
        await SaveLocalManifestAsync(workspace.ManifestStore, newKey);

        var oldDeployment = await workspace.PackageStore.CreateDeploymentAsync(oldKey, "default");
        var stateStore = new InMemoryDynamicAppStateStore();
        stateStore.UpsertTarget(new DynamicAppRouteTarget
        {
            Key = oldKey,
            DeploymentId = "default",
            DeploymentDirectory = oldDeployment.DeploymentDirectory,
            DeploymentEntryFilePath = oldDeployment.EntryFilePath,
            BaseAddress = new Uri("http://127.0.0.1:5112"),
            HealthPath = "/Health",
            HealthProbeTimeoutSeconds = 10,
            State = DynamicModLifecycleState.Active,
            HealthStatus = DynamicModHealthStatus.Healthy
        });

        var services = new ServiceCollection()
            .AddSingleton<IDynamicAppStateStore>(stateStore)
            .AddSingleton<IDynamicAppManifestStore>(workspace.ManifestStore)
            .AddSingleton<IDynamicAppModPackageStore>(workspace.PackageStore)
            .AddSingleton(new DynamicAppPackageStoreOptions { RootPath = workspace.RootPath })
            .AddSingleton<IDynamicAppRuntimeOperationCoordinator, DynamicAppRuntimeOperationCoordinator>()
            .AddSingleton<IDynamicAppManifestValidator, DefaultDynamicAppManifestValidator>()
            .AddSingleton<IDynamicAppManager, DefaultDynamicAppManager>()
            .AddSingleton<IDynamicAppTargetArchiveStore>(new FileDynamicAppTargetArchiveStore(new FileDynamicAppTargetArchiveStoreOptions
            {
                FilePath = Path.Combine(workspace.RootPath, "archives.json")
            }))
            .AddSingleton<IDynamicAppHealthProbe, AlwaysHealthyProbe>()
            .AddSingleton<IDynamicAppSelfHealingStateStore>(new FileDynamicAppSelfHealingStateStore(new FileDynamicAppSelfHealingStateStoreOptions
            {
                FilePath = Path.Combine(workspace.RootPath, "self-healing.json")
            }))
            .AddSingleton<IDynamicAppModRunner, FailNewVersionRunner>()
            .BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = services };
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/dynamicapp-manager/deployments/update";
        context.Request.ContentType = "application/json";
        context.Request.Body = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(new
        {
            currentDynamicAppId = oldKey.DynamicAppId.Value,
            currentModId = oldKey.ModId.Value,
            currentVersion = oldKey.Version.Value,
            currentDeploymentId = "default",
            dynamicAppId = newKey.DynamicAppId.Value,
            modId = newKey.ModId.Value,
            version = newKey.Version.Value,
            deploymentId = "default-next",
            port = 5120,
            entryFile = "testmod.dll"
        }));
        context.Request.Headers["X-DynamicApp-Manager-Ticket"] = "cluster-ticket";
        context.Response.Body = new MemoryStream();

        await InvokeManagerAsync(context);

        var body = await ReadResponseMessageAsync(context);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Contains("restored successfully", body, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(DynamicModLifecycleState.Active, Assert.Single(stateStore.ListTargets().Where(target => target.Key == oldKey)).State);
        Assert.Equal(DynamicModLifecycleState.Failed, Assert.Single(stateStore.ListTargets().Where(target => target.Key == newKey)).State);
    }

    private static async Task<DynamicModVersionKey> SeedProcessPackageAsync(DynamicAppTestWorkspace workspace)
    {
        var key = new DynamicModVersionKey(
            new DynamicAppId("security-platform"),
            new DynamicModId("testmod"),
            new DynamicAppVersion("1.0.0"));
        await using var package = CreatePackageStream("testmod.dll");
        await workspace.PackageStore.SaveAsync(new DynamicAppModPackageSaveRequest
        {
            Key = key,
            OriginalFileName = "testmod.zip",
            EntryFile = "testmod.dll"
        }, package);
        await workspace.ManifestStore.SaveAsync(new DynamicAppManifest
        {
            Id = key.DynamicAppId,
            Version = new DynamicAppVersion("1.0.0"),
            Mods =
            [
                new DynamicModManifest
                {
                    Id = key.ModId,
                    Version = key.Version,
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
        return key;
    }

    private static async Task SaveLocalPackageAsync(DynamicAppTestWorkspace workspace, DynamicModVersionKey key)
    {
        await using var package = CreatePackageStream("testmod.dll");
        await workspace.PackageStore.SaveAsync(new DynamicAppModPackageSaveRequest
        {
            Key = key,
            OriginalFileName = "testmod.zip",
            EntryFile = "testmod.dll"
        }, package);
    }

    private static Task SaveLocalManifestAsync(InMemoryDynamicAppManifestStore manifestStore, DynamicModVersionKey key)
        => manifestStore.SaveAsync(new DynamicAppManifest
        {
            Id = key.DynamicAppId,
            Version = key.Version,
            Mods =
            [
                new DynamicModManifest
                {
                    Id = key.ModId,
                    Version = key.Version,
                    ExecutionMode = DynamicModExecutionMode.LocalDevelopment,
                    Artifact = new DynamicModArtifactManifest { Endpoint = "http://127.0.0.1:5112" },
                    Health = new DynamicModHealthManifest { Path = "/Health", StartupTimeoutSeconds = 10 }
                }
            ]
        });

    private sealed class AlwaysHealthyProbe : IDynamicAppHealthProbe
    {
        public Task<DynamicAppHealthProbeResult> ProbeAsync(
            DynamicAppRouteTarget target,
            DynamicModHealthManifest health,
            CancellationToken cancellationToken = default)
            => Task.FromResult(DynamicAppHealthProbeResult.Healthy($"{target.BaseAddress}{health.Path}"));
    }

    private sealed class SuccessfulRunner : IDynamicAppModRunner
    {
        public Task<DynamicAppOperationResult> StartAsync(DynamicAppManifest manifest, DynamicModManifest mod, CancellationToken cancellationToken = default)
            => Task.FromResult(DynamicAppOperationResult.Success("started"));

        public Task<DynamicAppOperationResult> InspectAsync(DynamicModVersionKey key, CancellationToken cancellationToken = default)
            => Task.FromResult(DynamicAppOperationResult.Success("alive"));

        public Task<DynamicAppOperationResult> StopAsync(DynamicModVersionKey key, CancellationToken cancellationToken = default)
            => Task.FromResult(DynamicAppOperationResult.Success("stopped"));

        public Task<DynamicAppOperationResult> StopAsync(DynamicAppRuntimeKey key, CancellationToken cancellationToken = default)
            => Task.FromResult(DynamicAppOperationResult.Success("stopped"));
    }

    private sealed class FailNewVersionRunner : IDynamicAppModRunner
    {
        public Task<DynamicAppOperationResult> StartAsync(DynamicAppManifest manifest, DynamicModManifest mod, CancellationToken cancellationToken = default)
            => Task.FromResult(mod.Version.Value == "2.0.0"
                ? DynamicAppOperationResult.Failure("new package failed to start")
                : DynamicAppOperationResult.Success("started"));

        public Task<DynamicAppOperationResult> InspectAsync(DynamicModVersionKey key, CancellationToken cancellationToken = default)
            => Task.FromResult(DynamicAppOperationResult.Success("alive"));

        public Task<DynamicAppOperationResult> StopAsync(DynamicModVersionKey key, CancellationToken cancellationToken = default)
            => Task.FromResult(DynamicAppOperationResult.Success("stopped"));

        public Task<DynamicAppOperationResult> StopAsync(DynamicAppRuntimeKey key, CancellationToken cancellationToken = default)
            => Task.FromResult(DynamicAppOperationResult.Success("stopped"));
    }

    private static DefaultHttpContext CreateDeploymentContext(
        IDynamicAppStateStore stateStore,
        IDynamicAppManifestStore manifestStore,
        IDynamicAppModPackageStore packageStore,
        string packageRootPath,
        object request)
    {
        var services = new ServiceCollection()
            .AddSingleton(stateStore)
            .AddSingleton(manifestStore)
            .AddSingleton(packageStore)
            .AddSingleton(new DynamicAppPackageStoreOptions { RootPath = packageRootPath })
            .BuildServiceProvider();
        var context = new DefaultHttpContext
        {
            RequestServices = services
        };
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/dynamicapp-manager/deployments/create";
        context.Request.ContentType = "application/json";
        context.Request.Body = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(request));
        context.Request.Headers["X-DynamicApp-Manager-Ticket"] = "cluster-ticket";
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task InvokeManagerAsync(DefaultHttpContext context)
    {
        var middleware = new DynamicAppManagerApiMiddleware(
            _ => Task.CompletedTask,
            new DynamicAppManagerApiOptions
            {
                RequireTicket = true,
                Tickets = ["cluster-ticket"]
            });
        await middleware.InvokeAsync(context);
    }

    private static async Task<string> ReadResponseMessageAsync(DefaultHttpContext context)
    {
        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        return document.RootElement.GetProperty("message").GetString() ?? string.Empty;
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
