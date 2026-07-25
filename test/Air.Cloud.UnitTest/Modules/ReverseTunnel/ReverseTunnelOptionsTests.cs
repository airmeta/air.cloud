using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Routing;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Air.Cloud.UnitTest.Modules.ReverseTunnel;

/// <summary>
/// <para>zh-cn:反向隧道配置和路由解析测试集合。</para>
/// <para>en-us:Test suite for reverse tunnel configuration and route resolution.</para>
/// </summary>
public class ReverseTunnelOptionsTests
{
    /// <summary>
    /// <para>zh-cn:验证后置配置会标准化虚拟主机、本地地址和安全默认限制，且不会因为配置存在而自动启用模块。</para>
    /// <para>en-us:Verifies that post-configuration normalizes virtual hosts, local addresses, and safe limits without automatically enabling the module because configuration exists.</para>
    /// </summary>
    [Fact]
    public void PostConfigure_should_normalize_values_without_enabling_module()
    {
        var options = new ReverseTunnelOptions
        {
            HeartbeatIntervalSeconds = 1,
            SessionIdleTimeoutSeconds = 1,
            MaxConcurrentStreams = 9_999,
            MaxRequestBodyBytes = 1,
            MaxResponseBodyBytes = 1,
            MaxFramePayloadBytes = 1,
            OutboundFrameQueueCapacity = 1,
            AgentReconnectInitialDelaySeconds = 0,
            AgentReconnectMaxDelaySeconds = 0,
            Routes =
            [
                new ReverseTunnelRouteOptions
                {
                    VirtualHost = " TUNNEL-ORDER-SERVICE. ",
                    ServiceKey = " order-service ",
                    TimeoutSeconds = 999
                }
            ],
            LocalServices =
            [
                new ReverseTunnelLocalServiceOptions
                {
                    ServiceKey = " order-service ",
                    LocalAddress = " http://127.0.0.1:5101/ "
                }
            ],
            AgentAuthorizations =
            [
                new ReverseTunnelAgentAuthorizationOptions
                {
                    AgentId = " AGENT-ALPHA ",
                    AllowedServiceKeys = [" order-service ", "ORDER-SERVICE", " catalog-service "]
                }
            ]
        };

        options.PostConfigure(options, new ConfigurationBuilder().Build());

        Assert.False(options.Enabled);
        Assert.Equal(5, options.HeartbeatIntervalSeconds);
        Assert.Equal(10, options.SessionIdleTimeoutSeconds);
        Assert.Equal(4_096, options.MaxConcurrentStreams);
        Assert.Equal(1_024, options.MaxRequestBodyBytes);
        Assert.Equal(1_024, options.MaxResponseBodyBytes);
        Assert.Equal(1_024, options.MaxFramePayloadBytes);
        Assert.Equal(16, options.OutboundFrameQueueCapacity);
        Assert.Equal(1, options.AgentReconnectInitialDelaySeconds);
        Assert.Equal(1, options.AgentReconnectMaxDelaySeconds);
        Assert.Equal("tunnel-order-service", options.Routes[0].VirtualHost);
        Assert.Equal("order-service", options.Routes[0].ServiceKey);
        Assert.Equal(300, options.Routes[0].TimeoutSeconds);
        Assert.Equal("http://127.0.0.1:5101", options.LocalServices[0].LocalAddress);
        Assert.Equal("AGENT-ALPHA", options.AgentAuthorizations[0].AgentId);
        Assert.Equal(["order-service", "catalog-service"], options.AgentAuthorizations[0].AllowedServiceKeys);
    }

    /// <summary>
    /// <para>zh-cn:验证路由解析器仅在模块已显式启用时按不区分大小写的虚拟主机返回已配置服务。</para>
    /// <para>en-us:Verifies that the route resolver returns a configured service by case-insensitive virtual host only when the module has been explicitly enabled.</para>
    /// </summary>
    [Fact]
    public void TryResolve_should_require_enabled_module_and_match_normalized_virtual_host()
    {
        var options = new ReverseTunnelOptions
        {
            Enabled = true,
            Routes =
            [
                new ReverseTunnelRouteOptions
                {
                    VirtualHost = "tunnel-order-service",
                    ServiceKey = "order-service"
                }
            ]
        };
        options.PostConfigure(options, new ConfigurationBuilder().Build());
        var provider = new ReverseTunnelRouteProvider(Options.Create(options));

        var found = provider.TryResolve("TUNNEL-ORDER-SERVICE.", out var route);

        Assert.True(found);
        Assert.NotNull(route);
        Assert.Equal("order-service", route.ServiceKey);
    }

    /// <summary>
    /// <para>zh-cn:验证禁用模块时不会解析任何隧道路由，确保仅添加模块引用不会改变现有 Ocelot 转发。</para>
    /// <para>en-us:Verifies that no tunnel route resolves while the module is disabled, ensuring adding the module reference alone does not change existing Ocelot forwarding.</para>
    /// </summary>
    [Fact]
    public void TryResolve_should_not_return_routes_when_module_is_disabled()
    {
        var options = new ReverseTunnelOptions
        {
            Enabled = false,
            Routes =
            [
                new ReverseTunnelRouteOptions
                {
                    VirtualHost = "tunnel-order-service",
                    ServiceKey = "order-service"
                }
            ]
        };
        options.PostConfigure(options, new ConfigurationBuilder().Build());
        var provider = new ReverseTunnelRouteProvider(Options.Create(options));

        var found = provider.TryResolve("tunnel-order-service", out var route);

        Assert.False(found);
        Assert.Null(route);
    }

    /// <summary>
    /// <para>zh-cn:验证 Server 角色启用且配置 AgentAuthorizations 时，会拒绝空 AgentId、空 ServiceKey 和重复 AgentId。</para>
    /// <para>en-us:Verifies that when the Server role is enabled and AgentAuthorizations is configured, empty AgentId, empty ServiceKeys, and duplicate AgentId values are rejected.</para>
    /// </summary>
    [Fact]
    public void Validate_should_reject_invalid_agent_authorizations_when_server_role_is_enabled()
    {
        var options = new ReverseTunnelOptions
        {
            Enabled = true,
            Roles = ReverseTunnelRole.Server,
            AgentAuthorizations =
            [
                new ReverseTunnelAgentAuthorizationOptions
                {
                    AgentId = "agent-alpha",
                    AllowedServiceKeys = ["account"]
                },
                new ReverseTunnelAgentAuthorizationOptions
                {
                    AgentId = " AGENT-ALPHA ",
                    AllowedServiceKeys = ["system"]
                },
                new ReverseTunnelAgentAuthorizationOptions
                {
                    AgentId = " ",
                    AllowedServiceKeys = [" "]
                }
            ]
        };

        var result = new ReverseTunnelOptionsValidation().Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("ReverseTunnelSettings:AgentAuthorizations contains duplicate AgentId 'AGENT-ALPHA'.", result.Failures);
        Assert.Contains("ReverseTunnelSettings:AgentAuthorizations:2:AgentId is required when an authorization entry is configured.", result.Failures);
        Assert.Contains("ReverseTunnelSettings:AgentAuthorizations:2:AllowedServiceKeys must contain at least one ServiceKey when an authorization entry is configured.", result.Failures);
    }
}
