using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Air.Cloud.DynamicApp.Panel.Extensions;

/// <summary>
/// <para>zh-cn:DynamicApp 内置控制面板服务注册扩展。</para>
/// <para>en-us:Service registration extensions for the built-in DynamicApp control panel.</para>
/// </summary>
public static class DynamicAppPanelServiceCollectionExtensions
{
    /// <summary>
    /// <para>zh-cn:注册内置控制面板选项；该方法不会注册额外前端框架或后台服务。</para>
    /// <para>en-us:Registers built-in control panel options; this method does not register additional frontend frameworks or background services.</para>
    /// </summary>
    /// <param name="services">
    /// <para>zh-cn:服务集合。</para>
    /// <para>en-us:The service collection.</para>
    /// </param>
    /// <param name="configure">
    /// <para>zh-cn:可选控制面板配置委托。</para>
    /// <para>en-us:Optional control panel configuration delegate.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:原服务集合。</para>
    /// <para>en-us:The original service collection.</para>
    /// </returns>
    public static IServiceCollection AddDynamicAppPanel(this IServiceCollection services, Action<DynamicAppPanelOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        var options = new DynamicAppPanelOptions();
        configure?.Invoke(options);
        services.TryAddSingleton(options);
        return services;
    }
}
