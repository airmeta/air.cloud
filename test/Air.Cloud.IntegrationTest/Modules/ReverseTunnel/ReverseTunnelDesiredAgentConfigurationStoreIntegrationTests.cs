using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Runtime;
using Air.Cloud.Modules.ReverseTunnel.Server;

using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;

namespace Air.Cloud.IntegrationTest.Modules.ReverseTunnel;

/// <summary>
/// <para>zh-cn:验证 Server 端 Agent 目标配置的持久化与重启恢复。</para>
/// <para>en-us:Verifies persistence and restart recovery for Server-side desired Agent configuration.</para>
/// </summary>
public sealed class ReverseTunnelDesiredAgentConfigurationStoreIntegrationTests
{
    /// <summary>
    /// <para>zh-cn:验证新建存储实例可以读取此前保存的目标配置。</para>
    /// <para>en-us:Verifies that a new store instance can read a previously persisted desired configuration.</para>
    /// </summary>
    [Fact]
    public async Task Store_should_restore_desired_configuration_after_recreation()
    {
        var directory = Path.Combine(Path.GetTempPath(), "air-cloud-reverse-tunnel", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var options = new ReverseTunnelOptions
            {
                AgentId = "agent-alpha",
                Management = new ReverseTunnelManagementOptions
                {
                    ConfigDirectory = directory
                }
            };
            options.PostConfigure(options, new ConfigurationBuilder().Build());
            var runtimeOptions = new ReverseTunnelRuntimeOptionsAccessor(Options.Create(options));
            var environment = new TestHostEnvironment(directory);
            var snapshot = ReverseTunnelAgentConfigurationSnapshotSerializer.Create(
                "agent-alpha",
                options,
                17);

            var first = new MountedReverseTunnelDesiredAgentConfigurationStore(runtimeOptions, environment);
            await first.SaveAsync(snapshot);

            var second = new MountedReverseTunnelDesiredAgentConfigurationStore(runtimeOptions, environment);
            var restored = await second.GetAsync("agent-alpha");

            Assert.NotNull(restored);
            Assert.Equal(17, restored!.Version);
            Assert.True(ReverseTunnelAgentConfigurationSnapshotSerializer.VerifyHash(restored));
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }

    /// <summary>
    /// <para>zh-cn:验证两个请求使用同一旧版本并发保存时只有一个可以提交，另一个返回版本冲突。</para>
    /// <para>en-us:Verifies that concurrent saves using the same stale version allow only one commit while the other returns a version conflict.</para>
    /// </summary>
    [Fact]
    public async Task Store_should_reject_concurrent_save_with_stale_expected_version()
    {
        var directory = Path.Combine(Path.GetTempPath(), "air-cloud-reverse-tunnel", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var options = new ReverseTunnelOptions
            {
                AgentId = "agent-concurrent",
                Management = new ReverseTunnelManagementOptions { ConfigDirectory = directory }
            };
            options.PostConfigure(options, new ConfigurationBuilder().Build());
            var runtimeOptions = new ReverseTunnelRuntimeOptionsAccessor(Options.Create(options));
            var store = new MountedReverseTunnelDesiredAgentConfigurationStore(runtimeOptions, new TestHostEnvironment(directory));

            var first = store.TrySaveAsync("agent-concurrent", options, 0);
            var second = store.TrySaveAsync("agent-concurrent", options, 0);
            var results = await Task.WhenAll(first, second);

            Assert.Single(results, item => item.Succeeded);
            Assert.Single(results, item => item.VersionConflict);
            var saved = await store.GetAsync("agent-concurrent");
            Assert.NotNull(saved);
            Assert.True(saved!.Version > 0);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public TestHostEnvironment(string contentRootPath)
        {
            ContentRootPath = contentRootPath;
            ContentRootFileProvider = new PhysicalFileProvider(contentRootPath);
            EnvironmentName = Environments.Development;
            ApplicationName = "ReverseTunnelIntegrationTests";
        }

        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; }
        public string ContentRootPath { get; set; }
        public IFileProvider ContentRootFileProvider { get; set; }
    }
}


