using Air.Cloud.DynamicApp.Contracts.Identifiers;
using Air.Cloud.DynamicApp.Contracts.Runtime;
using Air.Cloud.DynamicApp.Host;
using Air.Cloud.DynamicApp.Host.Extensions;
using Air.Cloud.DynamicApp.Manager;

using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.DynamicApp.IntegrationTest.Host;

/// <summary>
/// <para>zh-cn:验证 DynamicApp Host 注册的生命周期协调器可以在共享包目录上取得运行实例锁。</para>
/// <para>en-us:Verifies that the Host-registered lifecycle coordinator acquires a runtime lock under the shared package directory.</para>
/// </summary>
public sealed class DynamicAppLifecycleCoordinationIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "DynamicApp")]
    public async Task AddDynamicAppHost_should_register_cross_process_runtime_coordinator()
    {
        var root = Path.Combine(Path.GetTempPath(), $"air-cloud-dynamicapp-{Guid.NewGuid():N}");
        try
        {
            var services = new ServiceCollection();
            services.AddDynamicAppHost(configurePackageStore: options => options.RootPath = root);
            await using var provider = services.BuildServiceProvider();
            var coordinator = provider.GetRequiredService<IDynamicAppRuntimeOperationCoordinator>();
            var key = new DynamicAppRuntimeKey(
                new DynamicModVersionKey(
                    new DynamicAppId("integration"),
                    new DynamicModId("workflow"),
                    new DynamicAppVersion("1.0.0")),
                "blue");

            await using var handle = await coordinator.AcquireAsync(key);
            Assert.NotNull(handle);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
