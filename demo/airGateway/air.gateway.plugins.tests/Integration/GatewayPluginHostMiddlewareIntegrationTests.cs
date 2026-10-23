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

    /// <summary>
    /// <para>zh-cn:验证控制面路径绕过业务 DLL 插件，避免插件认证策略抢先返回 401。</para>
    /// <para>en-us:Verifies that control-plane paths bypass business DLL plugins so plugin authentication cannot return 401 before the management endpoint runs.</para>
    /// </summary>
    [Fact]
    public async Task GatewayPluginHostMiddleware_should_bypass_control_plane_path()
    {
        var sampleAssemblyPath = typeof(SampleGatewayHeaderPlugin).Assembly.Location;
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["GatewayPluginSettings:Enabled"] = "true",
            ["GatewayPluginSettings:PluginDirectory"] = Path.GetDirectoryName(sampleAssemblyPath),
            ["GatewayPluginSettings:Plugins:0:Name"] = "SampleGatewayHeader",
            ["GatewayPluginSettings:Plugins:0:Enabled"] = "true",
            ["GatewayPluginSettings:Plugins:0:Assembly"] = Path.GetFileName(sampleAssemblyPath),
            ["GatewayPluginSettings:Plugins:0:Type"] = typeof(SampleGatewayHeaderPlugin).FullName,
            ["ControlPlaneAuth:Enabled"] = "true",
            ["ControlPlaneAuth:BasePath"] = "/_airgateway/control-plane",
            ["ControlPlaneAuth:AuthPath"] = "/_airgateway/control-auth",
            ["ControlPlaneAuth:PlanePath"] = "/_airgateway/control-plane"
        });
        builder.Services.AddGatewayPluginRuntime(builder.Configuration.GetSection("GatewayPluginSettings"));

        var app = builder.Build();
        app.UseMiddleware<GatewayPluginHostMiddleware>();
        app.Run(async context => await context.Response.WriteAsync("ok"));

        await app.StartAsync();
        try
        {
            var response = await app.GetTestClient().GetAsync("/_airgateway/control-auth");

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.False(response.Headers.Contains("X-Air-Gateway-Plugin"));
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    /// <summary>
    /// <para>zh-cn:验证关闭管理页面后，控制面路径在插件和 Ocelot 之前返回 404。</para>
    /// <para>en-us:Verifies that disabling the management page returns 404 for control-plane paths before the plugin and Ocelot pipelines.</para>
    /// </summary>
    [Fact]
    public async Task ControlPlaneManagementMiddleware_should_return_not_found_when_disabled()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ControlPlaneAuth:Enabled"] = "true",
            ["ControlPlaneAuth:Management:Enabled"] = "false",
            ["ControlPlaneAuth:BasePath"] = "/_airgateway/control-plane",
            ["ControlPlaneAuth:AuthPath"] = "/_airgateway/control-auth",
            ["ControlPlaneAuth:PlanePath"] = "/_airgateway/control-plane"
        });

        var app = builder.Build();
        app.UseMiddleware<air.gateway.Middleware.ControlPlaneManagementMiddleware>();
        app.Run(async context => await context.Response.WriteAsync("unexpected"));

        await app.StartAsync();
        try
        {
            var response = await app.GetTestClient().GetAsync("/_airgateway/control-auth");

            Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }
}
