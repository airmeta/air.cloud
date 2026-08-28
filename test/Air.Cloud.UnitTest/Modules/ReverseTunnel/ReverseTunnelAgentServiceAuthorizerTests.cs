using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Runtime;
using Air.Cloud.Modules.ReverseTunnel.Server;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Air.Cloud.UnitTest.Modules.ReverseTunnel;

/// <summary>
/// <para>zh-cn:Agent 服务声明授权器测试集合。</para>
/// <para>en-us:Test suite for Agent service declaration authorization.</para>
/// </summary>
public sealed class ReverseTunnelAgentServiceAuthorizerTests
{
    /// <summary>
    /// <para>zh-cn:验证未配置 AgentAuthorizations 时保持兼容行为，已认证 Agent 可声明服务。</para>
    /// <para>en-us:Verifies backward-compatible behavior when AgentAuthorizations is not configured and an authenticated Agent declares services.</para>
    /// </summary>
    [Fact]
    public void Authorize_should_allow_all_service_keys_when_no_authorizations_are_configured()
    {
        var authorizer = CreateAuthorizer(new ReverseTunnelOptions());

        var result = authorizer.Authorize("agent-alpha", ["account", "system"]);

        Assert.True(result.Succeeded);
        Assert.Null(result.FailureReason);
    }

    /// <summary>
    /// <para>zh-cn:验证 AgentId 和 ServiceKey 都命中白名单时授权通过。</para>
    /// <para>en-us:Verifies that authorization succeeds when both AgentId and ServiceKey match the allowlist.</para>
    /// </summary>
    [Fact]
    public void Authorize_should_allow_matching_agent_and_service_keys()
    {
        var options = new ReverseTunnelOptions
        {
            AgentAuthorizations =
            [
                new ReverseTunnelAgentAuthorizationOptions
                {
                    AgentId = "agent-alpha",
                    AllowedServiceKeys = ["account", "system"]
                }
            ]
        };
        options.PostConfigure(options, new ConfigurationBuilder().Build());
        var authorizer = CreateAuthorizer(options);

        var result = authorizer.Authorize("agent-alpha", ["account", "system"]);

        Assert.True(result.Succeeded);
        Assert.Null(result.FailureReason);
    }

    /// <summary>
    /// <para>zh-cn:验证配置了白名单但 AgentId 未登记时会拒绝服务声明。</para>
    /// <para>en-us:Verifies that a service declaration is rejected when allowlists are configured but the AgentId is not registered.</para>
    /// </summary>
    [Fact]
    public void Authorize_should_reject_unknown_agent_when_authorizations_are_configured()
    {
        var authorizer = CreateAuthorizer(CreateAuthorizedOptions());

        var result = authorizer.Authorize("agent-beta", ["account"]);

        Assert.False(result.Succeeded);
        Assert.Equal("Agent 'agent-beta' is not authorized to declare reverse tunnel services.", result.FailureReason);
    }

    /// <summary>
    /// <para>zh-cn:验证 Agent 声明未授权 ServiceKey 时会拒绝整个连接。</para>
    /// <para>en-us:Verifies that the entire connection is rejected when an Agent declares an unauthorized ServiceKey.</para>
    /// </summary>
    [Fact]
    public void Authorize_should_reject_unauthorized_service_key()
    {
        var authorizer = CreateAuthorizer(CreateAuthorizedOptions());

        var result = authorizer.Authorize("agent-alpha", ["account", "order"]);

        Assert.False(result.Succeeded);
        Assert.Equal("Agent 'agent-alpha' is not authorized to declare ServiceKey 'order'.", result.FailureReason);
    }

    /// <summary>
    /// <para>zh-cn:验证 AgentId 和 ServiceKey 会裁剪空白并按不区分大小写规则匹配。</para>
    /// <para>en-us:Verifies that AgentId and ServiceKey matching trims whitespace and is case-insensitive.</para>
    /// </summary>
    [Fact]
    public void Authorize_should_normalize_agent_and_service_keys()
    {
        var options = new ReverseTunnelOptions
        {
            AgentAuthorizations =
            [
                new ReverseTunnelAgentAuthorizationOptions
                {
                    AgentId = " AGENT-ALPHA ",
                    AllowedServiceKeys = [" Account ", "ACCOUNT", "System"]
                }
            ]
        };
        options.PostConfigure(options, new ConfigurationBuilder().Build());
        var authorizer = CreateAuthorizer(options);

        var result = authorizer.Authorize(" agent-alpha ", [" account ", "SYSTEM"]);

        Assert.True(result.Succeeded);
        Assert.Equal("AGENT-ALPHA", options.AgentAuthorizations[0].AgentId);
        Assert.Equal(["Account", "System"], options.AgentAuthorizations[0].AllowedServiceKeys);
    }

    /// <summary>
    /// <para>zh-cn:验证授权器读取运行期配置访问器，AgentAuthorizations 热更新后新授权请求立即按新白名单判断。</para>
    /// <para>en-us:Verifies that the authorizer reads the runtime-options accessor so new authorization requests use the updated allowlist after AgentAuthorizations hot updates.</para>
    /// </summary>
    [Fact]
    public void Authorize_should_use_runtime_authorization_updates()
    {
        var initial = CreateAuthorizedOptions();
        var accessor = new ReverseTunnelRuntimeOptionsAccessor(Options.Create(initial));
        var authorizer = new ReverseTunnelAgentServiceAuthorizer(accessor);

        Assert.True(authorizer.Authorize("agent-alpha", ["account"]).Succeeded);

        var updated = new ReverseTunnelOptions
        {
            AgentAuthorizations =
            [
                new ReverseTunnelAgentAuthorizationOptions
                {
                    AgentId = "agent-alpha",
                    AllowedServiceKeys = ["system"]
                }
            ]
        };
        updated.PostConfigure(updated, new ConfigurationBuilder().Build());
        accessor.Update(updated);

        Assert.False(authorizer.Authorize("agent-alpha", ["account"]).Succeeded);
        Assert.True(authorizer.Authorize("agent-alpha", ["system"]).Succeeded);
    }

    private static ReverseTunnelAgentServiceAuthorizer CreateAuthorizer(ReverseTunnelOptions options)
    {
        return new ReverseTunnelAgentServiceAuthorizer(Options.Create(options));
    }

    private static ReverseTunnelOptions CreateAuthorizedOptions()
    {
        var options = new ReverseTunnelOptions
        {
            AgentAuthorizations =
            [
                new ReverseTunnelAgentAuthorizationOptions
                {
                    AgentId = "agent-alpha",
                    AllowedServiceKeys = ["account", "system"]
                }
            ]
        };
        options.PostConfigure(options, new ConfigurationBuilder().Build());
        return options;
    }
}


