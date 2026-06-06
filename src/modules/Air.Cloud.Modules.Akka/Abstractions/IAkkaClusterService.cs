using Akka.Actor;
using Air.Cloud.Modules.Akka.Models;

namespace Air.Cloud.Modules.Akka.Abstractions;

/// <summary>
/// <para>zh-cn:Akka.Cluster 服务入口，向业务代码暴露 ActorSystem 生命周期、Actor 创建、消息发送和节点状态能力。</para>
/// <para>en-us:Akka.Cluster service entry that exposes ActorSystem lifecycle, actor creation, message sending, and node state capabilities to business code.</para>
/// </summary>
public interface IAkkaClusterService
{
    /// <summary>
    /// <para>zh-cn:当前 ActorSystem 实例；服务启动前访问会抛出异常。</para>
    /// <para>en-us:The current ActorSystem instance; accessing it before service startup throws an exception.</para>
    /// </summary>
    ActorSystem ActorSystem { get; }

    /// <summary>
    /// <para>zh-cn:启动 Akka.Cluster 运行时，创建 ActorSystem 并注册可扫描 Actor；该方法通常由宿主生命周期调用。</para>
    /// <para>en-us:Starts the Akka.Cluster runtime, creates the ActorSystem, and registers scannable actors; this method is intended to be called by the host lifecycle.</para>
    /// </summary>
    /// <param name="cancellationToken">
    /// <para>zh-cn:启动取消令牌。</para>
    /// <para>en-us:The startup cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:表示启动操作的任务。</para>
    /// <para>en-us:A task representing the startup operation.</para>
    /// </returns>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:停止 Akka.Cluster 运行时，先尝试离开集群，再终止 ActorSystem。</para>
    /// <para>en-us:Stops the Akka.Cluster runtime by attempting to leave the cluster first and then terminating the ActorSystem.</para>
    /// </summary>
    /// <param name="cancellationToken">
    /// <para>zh-cn:停止取消令牌。</para>
    /// <para>en-us:The shutdown cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:表示停止操作的任务。</para>
    /// <para>en-us:A task representing the shutdown operation.</para>
    /// </returns>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:按名称创建 Actor 或返回已注册实例；同名重复创建会复用注册表中的 Actor 引用。</para>
    /// <para>en-us:Creates an actor by name or returns an already registered one; duplicate names reuse the actor reference stored in the registry.</para>
    /// </summary>
    /// <typeparam name="TActor">
    /// <para>zh-cn:要创建的 Actor 类型。</para>
    /// <para>en-us:The actor type.</para>
    /// </typeparam>
    /// <param name="actorName">
    /// <para>zh-cn:Actor 注册名称。</para>
    /// <para>en-us:The actor registration name.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:新建或已有的 Actor 引用。</para>
    /// <para>en-us:The created or existing actor reference.</para>
    /// </returns>
    IActorRef ActorOf<TActor>(string actorName) where TActor : ActorBase;

    /// <summary>
    /// <para>zh-cn:向指定 Actor 发送即发即弃消息；目标缺失、运行时未启动或授权失败都会抛出异常。</para>
    /// <para>en-us:Sends a fire-and-forget message to the specified actor; missing targets, unstarted runtime, or authorization failures throw exceptions.</para>
    /// </summary>
    /// <param name="actorName">
    /// <para>zh-cn:目标 Actor 注册名称。</para>
    /// <para>en-us:The target actor registration name.</para>
    /// </param>
    /// <param name="message">
    /// <para>zh-cn:要发送的消息对象。</para>
    /// <para>en-us:The message object to send.</para>
    /// </param>
    void Tell(string actorName, object message);

    /// <summary>
    /// <para>zh-cn:向指定 Actor 发送请求并等待响应；默认超时时间来自 `AkkaSettings.AskTimeoutSeconds`。</para>
    /// <para>en-us:Sends a request to the specified actor and waits for the response; the default timeout comes from `AkkaSettings.AskTimeoutSeconds`.</para>
    /// </summary>
    /// <typeparam name="TResponse">
    /// <para>zh-cn:响应类型。</para>
    /// <para>en-us:The response type.</para>
    /// </typeparam>
    /// <param name="actorName">
    /// <para>zh-cn:目标 Actor 注册名称。</para>
    /// <para>en-us:The target actor registration name.</para>
    /// </param>
    /// <param name="message">
    /// <para>zh-cn:请求消息对象。</para>
    /// <para>en-us:The request message.</para>
    /// </param>
    /// <param name="timeout">
    /// <para>zh-cn:可选超时时间；为空时使用模块默认超时时间。</para>
    /// <para>en-us:The optional timeout; when null, the module default is used.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:包含 Actor 响应的任务。</para>
    /// <para>en-us:A task containing the actor response.</para>
    /// </returns>
    Task<TResponse> Ask<TResponse>(string actorName, object message, TimeSpan? timeout = null);

    /// <summary>
    /// <para>zh-cn:读取当前节点集群状态快照，用于健康检查、诊断和测试验证。</para>
    /// <para>en-us:Reads a snapshot of the current node Cluster state for health checks, diagnostics, and test verification.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:当前节点地址、角色和可用状态。</para>
    /// <para>en-us:The current node address, roles, and availability status.</para>
    /// </returns>
    AkkaClusterNodeInfo GetCurrentNode();
}

