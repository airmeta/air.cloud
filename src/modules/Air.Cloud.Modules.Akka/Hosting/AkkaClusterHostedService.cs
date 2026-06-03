using Air.Cloud.Modules.Akka.Abstractions;
using Microsoft.Extensions.Hosting;

namespace Air.Cloud.Modules.Akka.Hosting;

/// <summary>
/// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
/// <para>en-us:Akka.Cluster hosted lifecycle service.</para>
/// </summary>
public class AkkaClusterHostedService : IHostedService
{
    private readonly IAkkaClusterService _akkaClusterService;

    /// <summary>
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:Creates the Akka.Cluster hosted lifecycle adapter, delegating .NET Generic Host start and stop events to the module service entry.</para>
    /// </summary>
    /// <param name="akkaClusterService">
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:The Akka.Cluster service entry.</para>
    /// </param>
    public AkkaClusterHostedService(IAkkaClusterService akkaClusterService)
    {
        _akkaClusterService = akkaClusterService;
    }

    /// <summary>
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:Starts the Akka ActorSystem when the host starts; when the module is disabled by configuration, this call does not create an ActorSystem.</para>
    /// </summary>
    /// <param name="cancellationToken">
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:The host startup cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:A task representing the startup operation.</para>
    /// </returns>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return _akkaClusterService.StartAsync(cancellationToken);
    }

    /// <summary>
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:Triggers Akka.Cluster leave and ActorSystem termination when the host stops.</para>
    /// </summary>
    /// <param name="cancellationToken">
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:The host shutdown cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:A task representing the shutdown operation.</para>
    /// </returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return _akkaClusterService.StopAsync(cancellationToken);
    }
}

