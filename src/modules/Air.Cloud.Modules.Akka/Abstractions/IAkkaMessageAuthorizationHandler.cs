using Air.Cloud.Modules.Akka.Models;

namespace Air.Cloud.Modules.Akka.Abstractions;

/// <summary>
/// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
/// <para>en-us:Akka message authorization handler.</para>
/// </summary>
public interface IAkkaMessageAuthorizationHandler
{
    /// <summary>
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:Determines whether a message may be sent to the target actor; returning `false` blocks `Tell` or `Ask` and raises an authorization exception.</para>
    /// </summary>
    /// <param name="descriptor">
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:The registration descriptor of the target actor.</para>
    /// </param>
    /// <param name="message">
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:The message about to be sent.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:Returns `true` when sending is allowed.</para>
    /// </returns>
    bool CanSend(AkkaActorDescriptor descriptor, object message);
}

