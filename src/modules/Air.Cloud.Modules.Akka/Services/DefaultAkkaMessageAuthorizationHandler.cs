using Air.Cloud.Modules.Akka.Abstractions;
using Air.Cloud.Modules.Akka.Models;

namespace Air.Cloud.Modules.Akka.Services;

/// <summary>
/// <para>zh-cn:默认 Akka 消息授权处理器，在业务未提供自定义策略时作为开放发送策略使用。</para>
/// <para>en-us:Default Akka message authorization handler used as an open send policy when business code does not provide a custom policy.</para>
/// </summary>
public class DefaultAkkaMessageAuthorizationHandler : IAkkaMessageAuthorizationHandler
{
    /// <summary>
    /// <para>zh-cn:默认允许所有消息通过，调用方如需限制跨域、角色或消息类型，应注册额外的授权处理器。</para>
    /// <para>en-us:Allows every message by default, serving as the open fallback implementation when business code does not provide a custom authorization policy.</para>
    /// </summary>
    /// <param name="descriptor">
    /// <para>zh-cn:目标 Actor 的注册描述信息；默认实现不读取该参数。</para>
    /// <para>en-us:The registration descriptor of the target actor.</para>
    /// </param>
    /// <param name="message">
    /// <para>zh-cn:发送给 Actor 的消息对象；默认实现不读取该参数。</para>
    /// <para>en-us:The message being sent to the actor.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:始终返回 `true`。</para>
    /// <para>en-us:Always returns `true`.</para>
    /// </returns>
    public bool CanSend(AkkaActorDescriptor descriptor, object message)
    {
        return true;
    }
}

