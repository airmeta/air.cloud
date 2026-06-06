using Akka.Actor;

namespace Air.Cloud.Modules.Akka.Models;

/// <summary>
/// <para>zh-cn:Akka Actor 注册描述信息，记录业务名称、领域、角色、类型和运行时引用。</para>
/// <para>en-us:Akka actor registration descriptor that records the business name, domain, role, type, and runtime reference.</para>
/// </summary>
public class AkkaActorDescriptor
{
    /// <summary>
    /// <para>zh-cn:Actor 注册名称，发送消息和注册表查找都会使用该值。</para>
    /// <para>en-us:Actor registration name used for message sending and registry lookup.</para>
    /// </summary>
    public string ActorName { get; set; }

    /// <summary>
    /// <para>zh-cn:Actor 所属业务领域；为空表示不启用领域前缀。</para>
    /// <para>en-us:Business domain of the actor; null or empty means no domain prefix is applied.</para>
    /// </summary>
    public string Domain { get; set; }

    /// <summary>
    /// <para>zh-cn:Actor 期望运行的节点角色；为空表示不限制节点角色。</para>
    /// <para>en-us:Node role expected by the actor; null or empty means no node role restriction.</para>
    /// </summary>
    public string Role { get; set; }

    /// <summary>
    /// <para>zh-cn:Actor CLR 类型，用于诊断、注册验证和运行时创建。</para>
    /// <para>en-us:Actor CLR type used for diagnostics, registration validation, and runtime creation.</para>
    /// </summary>
    public Type ActorType { get; set; }

    /// <summary>
    /// <para>zh-cn:Akka.NET 运行时 Actor 引用，消息发送最终会转发到该引用。</para>
    /// <para>en-us:Akka.NET runtime actor reference to which message sending is ultimately forwarded.</para>
    /// </summary>
    public IActorRef ActorRef { get; set; }
}
