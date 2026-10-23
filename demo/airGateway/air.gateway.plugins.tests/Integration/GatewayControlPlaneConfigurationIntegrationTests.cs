using System.Text.Json;

namespace air.gateway.plugins.tests.Integration;

/// <summary>
/// <para>zh-cn:AirGateway 部署控制面配置集成测试。</para>
/// <para>en-us:Deployment control-plane configuration integration tests for AirGateway.</para>
/// </summary>
public sealed class GatewayControlPlaneConfigurationIntegrationTests
{
    /// <summary>
    /// <para>zh-cn:验证发布目录中的控制面菜单只暴露网关配置，不再暴露 AirTunnel 管理页面。</para>
    /// <para>en-us:Verifies that the packaged control-plane menu exposes only gateway configuration and no longer exposes the AirTunnel management page.</para>
    /// </summary>
    [Fact]
    public void Control_plane_menu_should_not_expose_airtunnel_management()
    {
        var configurationPath = Path.Combine(AppContext.BaseDirectory, "GatewayDeploymentConfig", "control-plane-auth.json");
        using var document = JsonDocument.Parse(File.ReadAllText(configurationPath));
        var controlPlane = document.RootElement.GetProperty("ControlPlaneAuth");
        var shell = controlPlane.GetProperty("Shell");
        var frames = shell.GetProperty("Frames").EnumerateArray().ToArray();

        Assert.Equal("gateway-config", shell.GetProperty("DefaultMenuKey").GetString());
        var frame = Assert.Single(frames);
        Assert.Equal("gateway-config", frame.GetProperty("Key").GetString());
        Assert.Equal("/_airgateway/control-plane/gateway-config", frame.GetProperty("Url").GetString());
    }
}
