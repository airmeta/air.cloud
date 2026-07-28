using Air.Cloud.DynamicApp.Contracts.Runtime;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Air.Cloud.DynamicApp.Gateway.Extensions;

/// <summary>
/// <para>zh-cn:DynamicApp Gateway 服务注册扩展。</para>
/// <para>en-us:Service registration extensions for DynamicApp Gateway.</para>
/// </summary>
public static class DynamicAppGatewayServiceCollectionExtensions
{
    /// <summary>
    /// <para>zh-cn:注册 DynamicApp Gateway 路由目录、转发选项和 HttpClient。</para>
    /// <para>en-us:Registers the DynamicApp Gateway route catalog, forwarding options, and HttpClient.</para>
    /// </summary>
    /// <param name="services">
    /// <para>zh-cn:服务集合。</para>
    /// <para>en-us:The service collection.</para>
    /// </param>
    /// <param name="configure">
    /// <para>zh-cn:可选 Gateway 配置委托。</para>
    /// <para>en-us:Optional Gateway configuration delegate.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:原服务集合。</para>
    /// <para>en-us:The original service collection.</para>
    /// </returns>
    public static IServiceCollection AddDynamicAppGateway(this IServiceCollection services, Action<DynamicAppGatewayOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        var options = new DynamicAppGatewayOptions();
        configure?.Invoke(options);
        services.TryAddSingleton(options);
        services.TryAddSingleton<IDynamicAppRouteCatalog, DefaultDynamicAppRouteCatalog>();
        services.AddHttpClient(nameof(DynamicAppGatewayMiddleware));
        return services;
    }
}
