using Air.Cloud.Core.App.Startups;
using Air.Cloud.Modules.Akka.Extensions;
using Air.Cloud.Modules.Akka.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.Modules.Akka;

/// <summary>
/// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
/// <para>en-us:Akka.Cluster module startup configuration.</para>
/// </summary>
public class Startup : AppStartup
{
    /// <summary>
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:Registers the Akka.Cluster module runtime services, binds the `AkkaSettings` configuration section, and lets the hosted service manage the ActorSystem lifecycle.</para>
    /// </summary>
    /// <param name="services">
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:The current application service collection.</para>
    /// </param>
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddOptions<AkkaSettingsOptions>()
            .BindConfiguration("AkkaSettings");

        services.AddAkkaCluster();
    }

    /// <summary>
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:Configures the Akka module web pipeline; the current Cluster runtime does not require middleware, so this method intentionally stays empty.</para>
    /// </summary>
    /// <param name="app">
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:The application builder.</para>
    /// </param>
    /// <param name="env">
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:The web hosting environment.</para>
    /// </param>
    public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
    }
}
