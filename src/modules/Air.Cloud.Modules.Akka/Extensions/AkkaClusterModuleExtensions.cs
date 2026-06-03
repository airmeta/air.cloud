using Air.Cloud.Modules.Akka.Abstractions;
using Air.Cloud.Modules.Akka.Hosting;
using Air.Cloud.Modules.Akka.Options;
using Air.Cloud.Modules.Akka.Registries;
using Air.Cloud.Modules.Akka.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Air.Cloud.Modules.Akka.Extensions;

/// <summary>
/// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
/// <para>en-us:Akka.Cluster module service registration extensions.</para>
/// </summary>
public static class AkkaClusterModuleExtensions
{
    /// <summary>
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:Registers the default Akka.Cluster module services, including the actor registry, message authorization handler, Cluster service entry, and hosted lifecycle service; core implementations registered earlier by business code are not replaced.</para>
    /// </summary>
    /// <param name="services">
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:Service collection.</para>
    /// </param>
    /// <param name="configureOptions">
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:An optional configuration delegate used to override `AkkaSettings` defaults in code.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:Service collection.</para>
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
