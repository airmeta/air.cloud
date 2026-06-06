using Akka.Actor;

namespace Air.Cloud.Modules.Akka.Actors;

/// <summary>
/// <para>zh-cn:Air.Cloud Akka Actor 基类，业务 Actor 可继承该类型以复用 `ReceiveActor` 消息处理模型。</para>
/// <para>en-us:Air.Cloud Akka actor base type that business actors can inherit to reuse the `ReceiveActor` message handling model.</para>
/// </summary>
public abstract class AirActorBase : ReceiveActor
{
}
