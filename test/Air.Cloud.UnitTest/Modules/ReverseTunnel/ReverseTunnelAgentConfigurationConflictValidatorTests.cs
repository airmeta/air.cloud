using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Runtime;
using Air.Cloud.Modules.ReverseTunnel.Server;

using Microsoft.Extensions.Configuration;

namespace Air.Cloud.UnitTest.Modules.ReverseTunnel;

public sealed class ReverseTunnelAgentConfigurationConflictValidatorTests
{
    /// <summary>
    /// <para>zh-cn:验证待保存 Agent 不能复用其他 Agent 已占用的 ServiceKey。</para>
    /// <para>en-us:Verifies that a candidate Agent cannot reuse a ServiceKey already owned by another Agent.</para>
    /// </summary>
    [Fact]
    public void Validate_should_reject_service_key_used_by_another_agent()
    {
        var conflicts = ReverseTunnelAgentConfigurationConflictValidator.Validate(
            "agent-b",
            HttpConfiguration("orders", "http://127.0.0.1:6102"),
            [CreateSnapshot("agent-a", HttpConfiguration("orders", "http://127.0.0.1:6101"))]);

        Assert.Contains(conflicts, item => item.Contains("ServiceKey 'orders'", StringComparison.Ordinal));
        Assert.Contains(conflicts, item => item.Contains("agent-a", StringComparison.Ordinal));
    }

    /// <summary>
    /// <para>zh-cn:验证多个不同 ServiceKey 不能映射到同一个协议目标。</para>
    /// <para>en-us:Verifies that different ServiceKeys cannot map to the same protocol target.</para>
    /// </summary>
    [Fact]
    public void Validate_should_reject_multiple_service_keys_for_the_same_target()
    {
        var configuration = new ReverseTunnelOptions
        {
            Enabled = true,
            LocalServices =
            [
                new ReverseTunnelLocalServiceOptions { ServiceKey = "orders", LocalAddress = "http://127.0.0.1:6101" },
                new ReverseTunnelLocalServiceOptions { ServiceKey = "orders-alias", LocalAddress = "http://127.0.0.1:6101/" }
            ]
        };

        var conflicts = ReverseTunnelAgentConfigurationConflictValidator.Validate("agent-a", configuration, []);

        Assert.Contains(conflicts, item => item.Contains("orders", StringComparison.Ordinal) && item.Contains("orders-alias", StringComparison.Ordinal));
    }

    /// <summary>
    /// <para>zh-cn:验证不同 Agent 不能使用不同 ServiceKey 映射到同一个 HTTP 目标。</para>
    /// <para>en-us:Verifies that different Agents cannot use different ServiceKeys for the same HTTP target.</para>
    /// </summary>
    [Fact]
    public void Validate_should_reject_same_http_target_used_by_another_agent()
    {
        var conflicts = ReverseTunnelAgentConfigurationConflictValidator.Validate(
            "agent-b",
            HttpConfiguration("orders-read", "http://127.0.0.1:6101/"),
            [CreateSnapshot("agent-a", HttpConfiguration("orders", "http://127.0.0.1:6101"))]);

        Assert.Contains(conflicts, item => item.Contains("orders", StringComparison.Ordinal) && item.Contains("orders-read", StringComparison.Ordinal));
    }

    /// <summary>
    /// <para>zh-cn:验证不同 ServiceKey 不能占用同一个 TCP Server 对外监听入口。</para>
    /// <para>en-us:Verifies that different ServiceKeys cannot use the same TCP Server listener endpoint.</para>
    /// </summary>
    [Fact]
    public void Validate_should_reject_conflicting_tcp_server_listener()
    {
        var existing = new ReverseTunnelOptions
        {
            Enabled = true,
            Tcp = new ReverseTunnelTcpOptions
            {
                Enabled = true,
                LocalServices = [new ReverseTunnelTcpLocalServiceOptions
                {
                    ServiceKey = "redis-a", Host = "10.0.0.5", Port = 6379,
                    ServerListenHost = "0.0.0.0", ServerListenPort = 16379
                }]
            }
        };
        var candidate = new ReverseTunnelOptions
        {
            Enabled = true,
            Tcp = new ReverseTunnelTcpOptions
            {
                Enabled = true,
                LocalServices = [new ReverseTunnelTcpLocalServiceOptions
                {
                    ServiceKey = "redis-b", Host = "10.0.0.6", Port = 6380,
                    ServerListenHost = "0.0.0.0", ServerListenPort = 16379
                }]
            }
        };

        var conflicts = ReverseTunnelAgentConfigurationConflictValidator.Validate(
            "agent-b",
            candidate,
            [CreateSnapshot("agent-a", existing)]);

        Assert.Contains(conflicts, item => item.Contains("Server listener", StringComparison.Ordinal));
    }

    private static ReverseTunnelOptions HttpConfiguration(string serviceKey, string localAddress)
        => new()
        {
            Enabled = true,
            LocalServices = [new ReverseTunnelLocalServiceOptions { ServiceKey = serviceKey, LocalAddress = localAddress }]
        };

    private static ReverseTunnelAgentConfigurationSnapshot CreateSnapshot(string agentId, ReverseTunnelOptions configuration)
    {
        configuration.PostConfigure(configuration, new ConfigurationBuilder().Build());
        return ReverseTunnelAgentConfigurationSnapshotSerializer.Create(agentId, configuration, 1);
    }
}


