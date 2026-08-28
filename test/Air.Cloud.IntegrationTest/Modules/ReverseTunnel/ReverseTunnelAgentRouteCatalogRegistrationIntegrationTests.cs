using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Routing;
using Air.Cloud.Modules.ReverseTunnel.Common.Runtime;
using Air.Cloud.Modules.ReverseTunnel.Extensions;
using Air.Cloud.Modules.ReverseTunnel.Server;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.IntegrationTest.Modules.ReverseTunnel;

/// <summary>
/// <para>zh-cn:验证 Agent 派生路由目录已接入模块注册和持久化边界。</para>
/// <para>en-us:Integration coverage for Agent-derived route catalog registration and persistence boundaries.</para>
/// </summary>
public sealed class ReverseTunnelAgentRouteCatalogRegistrationIntegrationTests
{
    /// <summary>
    /// <para>zh-cn:验证模块注册后，目录可以从 Agent 目标配置构建默认 HTTP 路由。</para>
    /// <para>en-us:Verifies that the registered catalog builds a default HTTP route from a desired Agent configuration.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "ReverseTunnel")]
    public async Task Registered_catalog_should_derive_route_from_desired_agent_store()
    {
        var configuration = new ReverseTunnelOptions
        {
            Enabled = true,
            LocalServices =
            [
                new ReverseTunnelLocalServiceOptions
                {
                    PublicPath = "/billing",
                    LocalAddress = "http://127.0.0.1:6201"
                }
            ]
        };
        ReverseTunnelServiceKeyFactory.Apply(configuration);
        configuration.PostConfigure(configuration, new ConfigurationBuilder().Build());
        var snapshot = ReverseTunnelAgentConfigurationSnapshotSerializer.Create("agent-billing", configuration, 1);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddReverseTunnel();
        services.AddSingleton<IReverseTunnelDesiredAgentConfigurationStore>(new StubStore(snapshot));

        using var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<IReverseTunnelAgentRouteCatalog>();
        await catalog.RefreshAsync();

        Assert.True(catalog.TryResolve("/billing", out var route));
        Assert.NotNull(route);
        Assert.Matches("^[0-9a-f]{32}$", route.ServiceKey);
    }

    private sealed class StubStore : IReverseTunnelDesiredAgentConfigurationStore
    {
        private readonly ReverseTunnelAgentConfigurationSnapshot snapshot;

        public StubStore(ReverseTunnelAgentConfigurationSnapshot snapshot)
        {
            this.snapshot = snapshot;
        }

        public Task<ReverseTunnelAgentConfigurationSnapshot?> GetAsync(string agentId, CancellationToken cancellationToken = default)
            => Task.FromResult<ReverseTunnelAgentConfigurationSnapshot?>(snapshot);

        public Task SaveAsync(ReverseTunnelAgentConfigurationSnapshot snapshot, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<ReverseTunnelAgentConfigurationSnapshot>> ListAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ReverseTunnelAgentConfigurationSnapshot>>([snapshot]);
    }
}


