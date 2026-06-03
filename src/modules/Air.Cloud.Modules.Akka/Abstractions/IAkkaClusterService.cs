using Akka.Actor;
using Air.Cloud.Modules.Akka.Models;

namespace Air.Cloud.Modules.Akka.Abstractions;

/// <summary>
/// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
/// <para>en-us:Akka.Cluster service entry.</para>
/// </summary>
public interface IAkkaClusterService
{
    /// <summary>
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:The current ActorSystem instance; accessing it before service startup throws an exception.</para>
    /// </summary>
    ActorSystem ActorSystem { get; }

    /// <summary>
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:Starts the Akka.Cluster runtime, creates the ActorSystem, and registers scannable actors; this method is intended to be called by the host lifecycle.</para>
    /// </summary>
    /// <param name="cancellationToken">
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:The startup cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:A task representing the startup operation.</para>
    /// </returns>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:Stops the Akka.Cluster runtime by attempting to leave the cluster first and then terminating the ActorSystem.</para>
    /// </summary>
    /// <param name="cancellationToken">
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:The shutdown cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:A task representing the shutdown operation.</para>
    /// </returns>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:Creates an actor by name or returns an already registered one; duplicate names reuse the actor reference stored in the registry.</para>
    /// </summary>
    /// <typeparam name="TActor">
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:The actor type.</para>
    /// </typeparam>
    /// <param name="actorName">
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:The actor registration name.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:The created or existing actor reference.</para>
    /// </returns>
    IActorRef ActorOf<TActor>(string actorName) where TActor : ActorBase;

    /// <summary>
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:Sends a fire-and-forget message to the specified actor; missing targets, unstarted runtime, or authorization failures throw exceptions.</para>
    /// </summary>
    /// <param name="actorName">
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:The target actor registration name.</para>
    /// </param>
    /// <param name="message">
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:The message object to send.</para>
    /// </param>
    void Tell(string actorName, object message);

    /// <summary>
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:Sends a request to the specified actor and waits for the response; the default timeout comes from `AkkaSettings.AskTimeoutSeconds`.</para>
    /// </summary>
    /// <typeparam name="TResponse">
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:The response type.</para>
    /// </typeparam>
    /// <param name="actorName">
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:The target actor registration name.</para>
    /// </param>
    /// <param name="message">
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:The request message.</para>
    /// </param>
    /// <param name="timeout">
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:The optional timeout; when null, the module default is used.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:A task containing the actor response.</para>
    /// </returns>
    Task<TResponse> Ask<TResponse>(string actorName, object message, TimeSpan? timeout = null);

    /// <summary>
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:Reads a snapshot of the current node Cluster state for health checks, diagnostics, and test verification.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:The current node address, roles, and availability status.</para>
    /// </returns>
    AkkaClusterNodeInfo GetCurrentNode();
}

