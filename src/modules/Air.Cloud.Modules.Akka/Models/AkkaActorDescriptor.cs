using Akka.Actor;

namespace Air.Cloud.Modules.Akka.Models;

/// <summary>
/// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
/// <para>en-us:Akka Actor registration descriptor.</para>
/// </summary>
public class AkkaActorDescriptor
{
    public string ActorName { get; set; }

    public string Domain { get; set; }

    public string Role { get; set; }

    public Type ActorType { get; set; }

    public IActorRef ActorRef { get; set; }
}
