using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Air.Cloud.AirForge.Panel.FileEditing;

namespace Air.Cloud.AirForge.Panel.Extensions;

/// <summary>
/// <para>zh-cn:AirForge 管理面板服务注册扩展。</para>
/// <para>en-us:Service registration extensions for the AirForge management panel.</para>
/// </summary>
public static class AirForgePanelServiceCollectionExtensions
{
    /// <summary>
    /// <para>zh-cn:注册 AirForge 管理面板选项；该方法只注册面板配置，不注册额外前端框架或后台任务。</para>
    /// <para>en-us:Registers AirForge management panel options; this method only registers panel configuration and does not register extra frontend frameworks or background services.</para>
    /// </summary>
    /// <param name="services">
    /// <para>zh-cn:服务集合。</para>
    /// <para>en-us:The service collection.</para>
    /// </param>
    /// <param name="configure">
    /// <para>zh-cn:可选的管理面板配置委托。</para>
    /// <para>en-us:Optional control panel configuration delegate.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:原服务集合。</para>
    /// <para>en-us:The original service collection.</para>
    /// </returns>
    public static IServiceCollection AddAirForgePanel(this IServiceCollection services, Action<AirForgePanelOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        var options = new AirForgePanelOptions();
        configure?.Invoke(options);
        services.TryAddSingleton(options);
        services.TryAddSingleton<AirForgeTextFileEditor>();
        return services;
    }
}


