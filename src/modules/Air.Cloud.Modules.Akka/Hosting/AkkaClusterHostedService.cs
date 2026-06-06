using Air.Cloud.Modules.Akka.Abstractions;
using Microsoft.Extensions.Hosting;

namespace Air.Cloud.Modules.Akka.Hosting;

/// <summary>
/// <para>zh-cn:Akka.Cluster 宿主生命周期服务，将 .NET Generic Host 的启动和停止事件转发给集群服务入口。</para>
/// <para>en-us:Akka.Cluster hosted lifecycle service that forwards .NET Generic Host start and stop events to the cluster service entry.</para>
/// </summary>
public class AkkaClusterHostedService : IHostedService
{
    private readonly IAkkaClusterService _akkaClusterService;

    /// <summary>
    /// <para>zh-cn:创建 Akka.Cluster 宿主生命周期适配器，把宿主启动和停止事件委托给模块服务入口。</para>
    /// <para>en-us:Creates the Akka.Cluster hosted lifecycle adapter, delegating .NET Generic Host start and stop events to the module service entry.</para>
    /// </summary>
    /// <param name="akkaClusterService">
    /// <para>zh-cn:Akka.Cluster 服务入口。</para>
    /// <para>en-us:The Akka.Cluster service entry.</para>
    /// </param>
    public AkkaClusterHostedService(IAkkaClusterService akkaClusterService)
    {
        _akkaClusterService = akkaClusterService;
    }

    /// <summary>
    /// <para>zh-cn:宿主启动时启动 Akka ActorSystem；如果配置禁用模块，该调用不会创建 ActorSystem。</para>
    /// <para>en-us:Starts the Akka ActorSystem when the host starts; when the module is disabled by configuration, this call does not create an ActorSystem.</para>
    /// </summary>
    /// <param name="cancellationToken">
    /// <para>zh-cn:宿主启动取消令牌。</para>
    /// <para>en-us:The host startup cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:表示启动操作的任务。</para>
    /// <para>en-us:A task representing the startup operation.</para>
    /// </returns>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return _akkaClusterService.StartAsync(cancellationToken);
    }

    /// <summary>
    /// <para>zh-cn:宿主停止时触发 Akka.Cluster 离开流程并终止 ActorSystem。</para>
    /// <para>en-us:Triggers Akka.Cluster leave and ActorSystem termination when the host stops.</para>
    /// </summary>
    /// <param name="cancellationToken">
    /// <para>zh-cn:宿主停止取消令牌。</para>
    /// <para>en-us:The host shutdown cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:表示停止操作的任务。</para>
    /// <para>en-us:A task representing the shutdown operation.</para>
    /// </returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return _akkaClusterService.StopAsync(cancellationToken);
    }
}

