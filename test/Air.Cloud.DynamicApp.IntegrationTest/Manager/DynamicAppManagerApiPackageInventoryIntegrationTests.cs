using System.Net;
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
/// <para>zh-cn:DynamicApp Manager API 包库存集成测试，验证真实管线中的应用统计、Manifest-only 容器登记和 Ticket 授权。</para>
/// <para>en-us:Integration tests for DynamicApp Manager API package inventory, verifying application counts, manifest-only container registration, and Ticket authorization in the real pipeline.</para>
/// </summary>
public sealed class DynamicAppManagerApiPackageInventoryIntegrationTests
{
    /// <summary>
    /// <para>zh-cn:验证包库存接口会把已上传包和 ExistingContainer Manifest-only 登记合并，并按应用/Mod 统计唯一版本数。</para>
    /// <para>en-us:Verifies that the package inventory endpoint merges uploaded packages with ExistingContainer manifest-only registrations and counts unique versions by application and Mod.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "DynamicApp")]
    public async Task PackageApis_should_merge_uploaded_packages_and_manifest_only_existing_container()
    {
        using var workspace = DynamicAppTestWorkspace.Create();
        await using var app = await CreateAppAsync(workspace.RootPath);
        var client = app.GetTestClient();
        await SeedManifestAsync(app.Services.GetRequiredService<IDynamicAppManifestStore>());

        var applications = await GetJsonAsync(client, "/dynamicapp-manager/packages/applications");
        var applicationRows = applications.RootElement.GetProperty("data").EnumerateArray().ToArray();
        var securityPlatform = Assert.Single(applicationRows, row => row.GetProperty("dynamicAppId").GetString() == "security-platform");
        Assert.Equal(2, securityPlatform.GetProperty("modCount").GetInt32());
        Assert.Equal(3, securityPlatform.GetProperty("versionCount").GetInt32());

        var packages = await GetJsonAsync(client, "/dynamicapp-manager/packages");
        var packageRows = packages.RootElement.GetProperty("data").EnumerateArray().ToArray();
        Assert.Equal(2, packageRows.Length);
        var account = Assert.Single(packageRows, row => row.GetProperty("modId").GetString() == "account");
        Assert.Equal(2, account.GetProperty("versions").GetArrayLength());
        var existingContainer = Assert.Single(packageRows, row => row.GetProperty("modId").GetString() == "agentsprint-api");
        var version = Assert.Single(existingContainer.GetProperty("versions").EnumerateArray());
        Assert.Equal("ManifestOnly", version.GetProperty("sourceKind").GetString());
        Assert.Equal("ExistingContainer", version.GetProperty("executionMode").GetString());
        Assert.Equal("agentsprint-api", version.GetProperty("containerName").GetString());
    }

    /// <summary>
    /// <para>zh-cn:验证 Manager API 包库存接口在缺少 Manager Ticket 时拒绝访问，防止控制面板数据被未授权读取。</para>
    /// <para>en-us:Verifies that Manager API package-inventory endpoints reject requests without a Manager Ticket, preventing unauthorized reads of control-panel data.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "DynamicApp")]
    public async Task PackageApis_should_require_manager_ticket()
    {
        using var workspace = DynamicAppTestWorkspace.Create();
        await using var app = await CreateAppAsync(workspace.RootPath);
        var client = app.GetTestClient();

        var response = await client.GetAsync("/dynamicapp-manager/packages");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task<WebApplication> CreateAppAsync(string packageRootPath)
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
            options => options.RootPath = packageRootPath);

        var app = builder.Build();
        await SeedPackagesAsync(app.Services.GetRequiredService<IDynamicAppModPackageStore>());
        app.UseDynamicAppManagerApi();
        app.Run(context =>
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return Task.CompletedTask;
        });
        await app.StartAsync();
        return app;
    }

    private static async Task<JsonDocument> GetJsonAsync(HttpClient client, string path)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.TryAddWithoutValidation("X-DynamicApp-Manager-Ticket", "integration-ticket");
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }

    private static async Task SeedPackagesAsync(IDynamicAppModPackageStore store)
    {
        await SavePackageAsync(store, "101-process-current");
        await SavePackageAsync(store, "101-process-previous");
    }

    private static async Task SavePackageAsync(IDynamicAppModPackageStore store, string version)
    {
        await using var package = CreatePackageStream("air.account.dll");
        await store.SaveAsync(new DynamicAppModPackageSaveRequest
        {
            Key = new DynamicModVersionKey(new DynamicAppId("security-platform"), new DynamicModId("account"), new DynamicAppVersion(version)),
            OriginalFileName = "air-account.zip",
            EntryFile = "air.account.dll",
            Overwrite = false
        }, package);
    }

    private static async Task SeedManifestAsync(IDynamicAppManifestStore manifestStore)
    {
        await manifestStore.SaveAsync(new DynamicAppManifest
        {
            Id = new DynamicAppId("security-platform"),
            Version = new DynamicAppVersion("101-current"),
            Endpoints = new DynamicAppEndpointManifest
            {
                GatewayPort = 8080,
                ManagerPort = 8081,
                ManagerInternalOnly = true
            },
            Mods =
            [
                new DynamicModManifest
                {
                    Id = new DynamicModId("agentsprint-api"),
                    Version = new DynamicAppVersion("101-current"),
                    ExecutionMode = DynamicModExecutionMode.ExistingContainer,
                    Artifact = new DynamicModArtifactManifest
                    {
                        Endpoint = "http://agentsprint-api:8080",
                        ContainerName = "agentsprint-api"
                    },
                    Route = new DynamicModRouteManifest { Prefix = "/mods/agentsprint-api" },
                    Health = new DynamicModHealthManifest { Path = "/Health", StartupTimeoutSeconds = 10 }
                }
            ]
        });
    }

    private static MemoryStream CreatePackageStream(string entryFile)
    {
        var stream = new MemoryStream();
        using (var archive = new System.IO.Compression.ZipArchive(stream, System.IO.Compression.ZipArchiveMode.Create, leaveOpen: true))
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
/// <para>zh-cn:DynamicApp 集成测试临时工作目录，负责隔离文件型包存储并在测试完成后清理。</para>
/// <para>en-us:Temporary DynamicApp integration-test workspace that isolates file-based package storage and cleans it after the test finishes.</para>
/// </summary>
internal sealed class DynamicAppTestWorkspace : IDisposable
{
    private DynamicAppTestWorkspace(string rootPath)
    {
        RootPath = rootPath;
    }

    /// <summary>
    /// <para>zh-cn:测试使用的临时根目录。</para>
    /// <para>en-us:Temporary root directory used by the test.</para>
    /// </summary>
    public string RootPath { get; }

    /// <summary>
    /// <para>zh-cn:创建新的测试工作目录，目录名使用随机值避免并发测试冲突。</para>
    /// <para>en-us:Creates a new test workspace using a random directory name to avoid conflicts between parallel tests.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:可释放的测试工作目录。</para>
    /// <para>en-us:A disposable test workspace.</para>
    /// </returns>
    public static DynamicAppTestWorkspace Create()
    {
        var root = Path.Combine(Path.GetTempPath(), "air-cloud-dynamicapp-integration-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return new DynamicAppTestWorkspace(root);
    }

    /// <summary>
    /// <para>zh-cn:删除测试临时目录；若文件已被外部占用则忽略清理异常，避免掩盖测试断言结果。</para>
    /// <para>en-us:Deletes the temporary test directory; cleanup exceptions are ignored when files are externally locked so assertion results are not hidden.</para>
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
