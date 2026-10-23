using System.Reflection;

namespace air.gateway.Manage;

/// <summary>
/// <para>zh-cn:渲染 AirGateway 网关配置管理页面。页面先提供路由维护工作区骨架，后续由网关路由 API 接入真实数据。</para>
/// <para>en-us:Renders the AirGateway gateway-configuration management page. The page currently provides the route-maintenance workspace skeleton and will be connected to the real gateway-route API later.</para>
/// </summary>
internal static class GatewayConfigurationPageRenderer
{
    private const string ResourceSuffix = ".Manage.Assets.gateway-configuration.html";

    /// <summary>
    /// <para>zh-cn:读取嵌入的网关配置管理页面。页面资源随 air.gateway 程序集发布，不依赖外部静态文件目录。</para>
    /// <para>en-us:Reads the embedded gateway-configuration page. The page is published with the air.gateway assembly and does not depend on an external static-file directory.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:完整 HTML 文本。</para>
    /// <para>en-us:The complete HTML document.</para>
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// <para>zh-cn:当嵌入资源不存在时抛出。</para>
    /// <para>en-us:Thrown when the embedded resource cannot be found.</para>
    /// </exception>
    public static string Create()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .SingleOrDefault(name => name.EndsWith(ResourceSuffix, StringComparison.Ordinal));
        if (resourceName is null)
        {
            throw new InvalidOperationException($"Cannot find embedded gateway configuration page resource ending with '{ResourceSuffix}'.");
        }

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Cannot open embedded gateway configuration page resource '{resourceName}'.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
