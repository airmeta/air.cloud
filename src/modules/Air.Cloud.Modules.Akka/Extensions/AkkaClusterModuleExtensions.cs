using Air.Cloud.Modules.Akka.Abstractions;
using Air.Cloud.Modules.Akka.Hosting;
using Air.Cloud.Modules.Akka.Options;
using Air.Cloud.Modules.Akka.Registries;
using Air.Cloud.Modules.Akka.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Air.Cloud.Modules.Akka.Extensions;

/// <summary>
/// <para>zh-cn:Akka.Cluster 模块服务注册扩展，用于把集群运行时接入依赖注入容器。</para>
/// <para>en-us:Akka.Cluster module service registration extensions used to connect the cluster runtime to the dependency injection container.</para>
/// </summary>
public static class AkkaClusterModuleExtensions
{
    /// <summary>
    /// <para>zh-cn:注册默认 Akka.Cluster 服务，包括 Actor 注册表、消息授权处理器、集群服务入口和宿主生命周期服务；业务代码已提前注册的核心实现不会被替换。</para>
    /// <para>en-us:Registers the default Akka.Cluster module services, including the actor registry, message authorization handler, Cluster service entry, and hosted lifecycle service; core implementations registered earlier by business code are not replaced.</para>
    /// </summary>
    /// <param name="services">
    /// <para>zh-cn:服务集合。</para>
    /// <para>en-us:The service collection to which the Akka.Cluster services are added.</para>
    /// </param>
    /// <param name="configureOptions">
    /// <para>zh-cn:可选的代码配置委托，用于覆盖 `AkkaSettings` 默认值。</para>
    /// <para>en-us:An optional configuration delegate used to override `AkkaSettings` defaults in code.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:注册完成后的同一个服务集合。</para>
    /// <para>en-us:The same service collection after registration.</para>
    /// </returns>
    public static IServiceCollection AddAkkaCluster(this IServiceCollection services, Action<AkkaSettingsOptions> configureOptions = null)
    {
        services.AddOptions<AkkaSettingsOptions>();

        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }

        services.TryAddSingleton<IAkkaActorRegistry, AkkaActorRegistry>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAkkaMessageAuthorizationHandler, DefaultAkkaMessageAuthorizationHandler>());
        services.TryAddSingleton<IAkkaClusterService, AkkaClusterService>();
        services.AddHostedService<AkkaClusterHostedService>();

        return services;
    }
}
