using Akka.Actor;

namespace Air.Cloud.Modules.Akka.Actors;

/// <summary>
/// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
/// <para>en-us:Akka.Cluster root actor.</para>
/// </summary>
public class ClusterRootActor : ReceiveActor
{
    /// <summary>
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:Creates the Cluster root actor; by default it publishes every received message to the ActorSystem event stream for future unified observation or forwarding extensions.</para>
    /// </summary>
    public ClusterRootActor()
    {
        ReceiveAny(message => Context.System.EventStream.Publish(message));
    }
}
