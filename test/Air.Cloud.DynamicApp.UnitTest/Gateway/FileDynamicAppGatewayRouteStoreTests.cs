using Air.Cloud.DynamicApp.Contracts.Runtime;
using Air.Cloud.DynamicApp.Manager;

namespace Air.Cloud.DynamicApp.UnitTest.Gateway;

/// <summary>
/// <para>zh-cn:动态路由文件 Store 持久化和损坏快照恢复测试。</para>
/// <para>en-us:Persistence and corrupt-snapshot recovery tests for the file dynamic route store.</para>
/// </summary>
public sealed class FileDynamicAppGatewayRouteStoreTests
{
    [Fact]
    public async Task Store_should_persist_routes_and_preserve_corrupt_snapshot()
    {
        var workspace = Directory.CreateTempSubdirectory("dynamicapp-routes-");
        try
        {
            var path = Path.Combine(workspace.FullName, "routes.json");
            var store = new FileDynamicAppGatewayRouteStore(new FileDynamicAppGatewayRouteStoreOptions
            {
                FilePath = path,
                SaveOnChange = true
            });

            Assert.True((await store.CreateAsync(new DynamicAppGatewayRoute
            {
                RouteId = "health",
                UpstreamPathTemplate = "/health",
                DownstreamPathTemplate = "/health",
                TargetDynamicAppId = "app",
                TargetModId = "mod"
            })).Succeeded);
            Assert.Single(new FileDynamicAppGatewayRouteStore(new FileDynamicAppGatewayRouteStoreOptions { FilePath = path }).List());

            File.WriteAllText(path, "{ invalid json");
            var recovered = new FileDynamicAppGatewayRouteStore(new FileDynamicAppGatewayRouteStoreOptions { FilePath = path });
            Assert.Empty(recovered.List());
            Assert.Contains(Directory.EnumerateFiles(workspace.FullName), file => file.Contains(".corrupt.", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            workspace.Delete(true);
        }
    }
}
