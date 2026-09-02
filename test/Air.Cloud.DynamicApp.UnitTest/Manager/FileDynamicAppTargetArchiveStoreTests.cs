using Air.Cloud.DynamicApp.Contracts.Identifiers;
using Air.Cloud.DynamicApp.Contracts.Lifecycle;
using Air.Cloud.DynamicApp.Contracts.Runtime;
using Air.Cloud.DynamicApp.Manager;

namespace Air.Cloud.DynamicApp.UnitTest.Manager;

/// <summary>
/// <para>zh-cn:DynamicApp 目标归档存储单元测试，覆盖版本级归档与实例级归档的隔离规则。</para>
/// <para>en-us:Unit tests for DynamicApp target archive storage, covering isolation between version-level and instance-level archives.</para>
/// </summary>
public sealed class FileDynamicAppTargetArchiveStoreTests
{
    /// <summary>
    /// <para>zh-cn:验证实例级归档只阻断对应 DeploymentId，版本级归档才阻断同版本全部实例。</para>
    /// <para>en-us:Verifies that an instance archive blocks only its DeploymentId while a version archive blocks every deployment of that version.</para>
    /// </summary>
    [Fact]
    public void IsArchived_should_distinguish_version_and_instance_archives()
    {
        var workspace = Directory.CreateTempSubdirectory("dynamicapp-archive-store-");
        try
        {
            var key = new DynamicModVersionKey(
                new DynamicAppId("security-platform"),
                new DynamicModId("workflow"),
                new DynamicAppVersion("1.0.0"));
            var blue = new DynamicAppRuntimeKey(key, "blue");
            var gray = new DynamicAppRuntimeKey(key, "gray");
            var store = new FileDynamicAppTargetArchiveStore(new FileDynamicAppTargetArchiveStoreOptions
            {
                FilePath = Path.Combine(workspace.FullName, "archives.json")
            });

            store.ArchiveDeployment(key, "blue", CreateTarget(key, "blue"), null, null, "destroyed");

            Assert.False(store.IsArchived(key));
            Assert.True(store.IsArchived(blue));
            Assert.False(store.IsArchived(gray));

            store.Archive(key, "governance");

            Assert.True(store.IsArchived(key));
            Assert.True(store.IsArchived(blue));
            Assert.True(store.IsArchived(gray));
        }
        finally
        {
            workspace.Delete(true);
        }
    }

    private static DynamicAppRouteTarget CreateTarget(DynamicModVersionKey key, string deploymentId) => new()
    {
        Key = key,
        DeploymentId = deploymentId,
        BaseAddress = new Uri("http://127.0.0.1:5100"),
        HealthPath = "/Health",
        State = DynamicModLifecycleState.Stopped,
        HealthStatus = DynamicModHealthStatus.Stopped
    };
}
