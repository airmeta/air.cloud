using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace air.gateway.Manage;

/// <summary>
/// <para>zh-cn:AirGateway 网关配置管理页面 endpoint 映射扩展。</para>
/// <para>en-us:Endpoint-mapping extensions for the AirGateway gateway-configuration management page.</para>
/// </summary>
public static class GatewayConfigurationEndpointRouteBuilderExtensions
{
    /// <summary>
    /// <para>zh-cn:映射网关配置管理页面。页面路径位于 ControlPlaneAuth 的 PlanePath 下，以便独立管理端口和插件绕过策略将其识别为控制面流量。</para>
    /// <para>en-us:Maps the gateway-configuration management page below the ControlPlaneAuth PlanePath so the dedicated management listener and plugin-bypass policy identify it as control-plane traffic.</para>
    /// </summary>
    /// <param name="endpoints">
    /// <para>zh-cn:ASP.NET Core endpoint 路由构建器。</para>
    /// <para>en-us:The ASP.NET Core endpoint route builder.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:原 endpoint 路由构建器，便于继续映射其他端点。</para>
    /// <para>en-us:The original endpoint route builder for chaining additional mappings.</para>
    /// </returns>
    public static IEndpointRouteBuilder MapGatewayConfigurationPage(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet("/_airgateway/control-plane/gateway-config", () =>
            Results.Content(GatewayConfigurationPageRenderer.Create(), "text/html; charset=utf-8"))
            .WithDisplayName("AirGateway gateway configuration page");

        return endpoints;
    }
}
