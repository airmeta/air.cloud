using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Management;

using Microsoft.AspNetCore.Http;

using System.Net;

namespace Air.Cloud.IntegrationTest.Modules.ReverseTunnel;

/// <summary>
/// <para>zh-cn:AirTunnel 管理端授权器测试。</para>
/// <para>en-us:Tests for AirTunnel management authorizers.</para>
/// </summary>
public sealed class ReverseTunnelManagementAuthorizerTests
{
    /// <summary>
    /// <para>zh-cn:验证默认管理端授权器继续接受环境变量中的兼容 Token。</para>
    /// <para>en-us:Verifies that the default management authorizer continues to accept the compatible token from an environment variable.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "ReverseTunnel")]
    public async Task AccessToken_authorizer_should_accept_compatible_header_token()
    {
        const string variable = "AIR_TUNNEL_AUTHORIZE_TEST_TOKEN";
        var previous = Environment.GetEnvironmentVariable(variable);
        Environment.SetEnvironmentVariable(variable, "test-token");
        try
        {
            var context = new DefaultHttpContext();
            context.Connection.LocalPort = 18643;
            context.Connection.RemoteIpAddress = IPAddress.Parse("192.168.1.10");
            context.Request.Headers["X-AirTunnel-Admin-Token"] = "test-token";
            var options = new ReverseTunnelManagementOptions
            {
                Enabled = true,
                BindUrl = "https://0.0.0.0:18643",
                AccessTokenEnvironmentVariable = variable
            };

            var result = await new AccessTokenReverseTunnelManagementAuthorizer().AuthorizeAsync(context, options);

            Assert.Null(result);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variable, previous);
        }
    }

    /// <summary>
    /// <para>zh-cn:验证默认管理端授权器拒绝错误端口，避免管理 API 在隧道控制端口上被误用。</para>
    /// <para>en-us:Verifies that the default management authorizer rejects the wrong port so management APIs are not accidentally used on the tunnel control port.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "ReverseTunnel")]
    public async Task AccessToken_authorizer_should_reject_non_management_port()
    {
        var context = new DefaultHttpContext();
        context.Connection.LocalPort = 18443;
        var options = new ReverseTunnelManagementOptions
        {
            Enabled = true,
            BindUrl = "https://0.0.0.0:18643"
        };

        var result = await new AccessTokenReverseTunnelManagementAuthorizer().AuthorizeAsync(context, options);

        Assert.NotNull(result);
    }
}


