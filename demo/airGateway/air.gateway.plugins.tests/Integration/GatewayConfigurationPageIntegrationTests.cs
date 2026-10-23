using air.gateway.Manage;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;

namespace air.gateway.plugins.tests.Integration;

/// <summary>
/// <para>zh-cn:AirGateway 网关配置页面集成测试。</para>
/// <para>en-us:Integration tests for the AirGateway gateway-configuration page.</para>
/// </summary>
public sealed class GatewayConfigurationPageIntegrationTests
{
    /// <summary>
    /// <para>zh-cn:验证网关配置页面端点可以从程序集嵌入资源返回路由维护页面骨架。</para>
    /// <para>en-us:Verifies that the gateway-configuration endpoint returns the route-maintenance page skeleton from the embedded assembly resource.</para>
    /// </summary>
    [Fact]
    public async Task GatewayConfigurationPage_should_render_route_management_skeleton()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        var app = builder.Build();
        app.UseRouting();
        app.UseEndpoints(endpoints => endpoints.MapGatewayConfigurationPage());

        await app.StartAsync();
        try
        {
            var response = await app.GetTestClient().GetAsync("/_airgateway/control-plane/gateway-config");
            var html = await response.Content.ReadAsStringAsync();

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("网关配置", html, StringComparison.Ordinal);
            Assert.Contains("网关路由列表", html, StringComparison.Ordinal);
            Assert.Contains("新增路由", html, StringComparison.Ordinal);
            Assert.Contains("上游路径", html, StringComparison.Ordinal);
            Assert.Contains("路由管理接口待接入", html, StringComparison.Ordinal);
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }
}
