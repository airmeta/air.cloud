using air.cloud.gateway.Plugins.Extensions;
using air.cloud.gateway.Plugins.Runtime;
using air.gateway.plugins.sample;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;

namespace air.gateway.plugins.tests.Integration;

/// <summary>
/// <para>zh-cn:网关 DLL 插件宿主中间件集成测试。</para>
/// <para>en-us:Integration tests for the gateway DLL plugin host middleware.</para>
/// </summary>
public sealed class GatewayPluginHostMiddlewareIntegrationTests
{
    /// <summary>
    /// <para>zh-cn:验证 TestServer 启动后，配置启用的 DLL 插件会进入请求链路并修改响应头。</para>
    /// <para>en-us:Verifies that after TestServer starts, a DLL plugin enabled by configuration enters the request pipeline and modifies the response header.</para>
    /// </summary>
    [Fact]
    public async Task GatewayPluginHostMiddleware_should_execute_enabled_dll_plugin()
    {
        var sampleAssemblyPath = typeof(SampleGatewayHeaderPlugin).Assembly.Location;
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["GatewayPluginSettings:Enabled"] = "true",
            ["GatewayPluginSettings:UseDefaultGatewayPlugins"] = "false",
            ["GatewayPluginSettings:PluginDirectory"] = Path.GetDirectoryName(sampleAssemblyPath),
            ["GatewayPluginSettings:Plugins:0:Name"] = "SampleGatewayHeader",
            ["GatewayPluginSettings:Plugins:0:Enabled"] = "true",
            ["GatewayPluginSettings:Plugins:0:Assembly"] = Path.GetFileName(sampleAssemblyPath),
            ["GatewayPluginSettings:Plugins:0:Type"] = typeof(SampleGatewayHeaderPlugin).FullName
        });
        builder.Services.AddGatewayPluginRuntime(builder.Configuration.GetSection("GatewayPluginSettings"));

        var app = builder.Build();
        app.UseMiddleware<GatewayPluginHostMiddleware>();
        app.Run(async context => await context.Response.WriteAsync("ok"));

        await app.StartAsync();
        try
        {
            var response = await app.GetTestClient().GetAsync("/");

            Assert.True(response.Headers.TryGetValues("X-Air-Gateway-Plugin", out var values));
            Assert.Contains("SampleGatewayHeader", values);
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }
}
