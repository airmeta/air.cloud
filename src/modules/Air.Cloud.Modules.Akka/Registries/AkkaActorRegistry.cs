using Akka.Actor;
using Air.Cloud.Modules.Akka.Abstractions;
using Air.Cloud.Modules.Akka.Models;

namespace Air.Cloud.Modules.Akka.Registries;

/// <summary>
/// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
/// <para>en-us:Default Akka Actor registry.</para>
/// </summary>
public class AkkaActorRegistry : IAkkaActorRegistry
{
    private readonly Dictionary<string, AkkaActorDescriptor> _descriptors = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _locker = new();

    /// <summary>
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:Registers or replaces an actor descriptor; this implementation protects the internal dictionary with a lock and is suitable for host startup and runtime manual registration.</para>
    /// </summary>
    /// <param name="descriptor">
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:The actor registration descriptor, which must contain a non-empty actor name.</para>
    /// </param>
    public void Register(AkkaActorDescriptor descriptor)
    {
        if (descriptor == null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (string.IsNullOrWhiteSpace(descriptor.ActorName))
        {
            throw new ArgumentException("ActorName can not be empty.", nameof(descriptor));
        }

        lock (_locker)
        {
            _descriptors[descriptor.ActorName] = descriptor;
        }
    }

    /// <summary>
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:Finds an actor reference by name using case-insensitive matching; misses output `ActorRefs.Nobody`.</para>
    /// </summary>
    /// <param name="actorName">
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:The actor registration name.</para>
    /// </param>
    /// <param name="actorRef">
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:The resolved actor reference.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:Returns `true` when a registered actor is found.</para>
    /// </returns>
    public bool TryGet(string actorName, out IActorRef actorRef)
    {
        lock (_locker)
        {
            if (_descriptors.TryGetValue(actorName, out var descriptor) && descriptor.ActorRef != null)
            {
                actorRef = descriptor.ActorRef;
                return true;
            }
        }

        actorRef = ActorRefs.Nobody;
        return false;
    }

    /// <summary>
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:Returns a snapshot of current registration descriptors; the copy is made under lock to avoid external enumeration over the mutable internal collection.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:A snapshot of actor registration descriptors.</para>
    /// </returns>
    public IReadOnlyCollection<AkkaActorDescriptor> GetDescriptors()
    {
        lock (_locker)
        {
            return _descriptors.Values.ToArray();
        }
    }
}

