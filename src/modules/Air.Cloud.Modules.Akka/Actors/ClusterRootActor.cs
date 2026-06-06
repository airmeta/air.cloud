using Akka.Actor;

namespace Air.Cloud.Modules.Akka.Actors;

/// <summary>
/// <para>zh-cn:Akka.Cluster 根 Actor，作为集群消息统一观察和扩展转发的默认入口。</para>
/// <para>en-us:Akka.Cluster root actor used as the default entry for unified cluster message observation and forwarding extensions.</para>
/// </summary>
public class ClusterRootActor : ReceiveActor
{
    /// <summary>
    /// <para>zh-cn:创建集群根 Actor；默认把收到的所有消息发布到 ActorSystem 事件流，便于后续统一观察或转发扩展接入。</para>
    /// <para>en-us:Creates the Cluster root actor; by default it publishes every received message to the ActorSystem event stream for future unified observation or forwarding extensions.</para>
    /// </summary>
    public ClusterRootActor()
    {
        ReceiveAny(message => Context.System.EventStream.Publish(message));
    }
}
