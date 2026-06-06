using System.Reflection;
using Akka.Actor;
using Akka.Configuration;
using Akka.Cluster;
using Air.Cloud.Modules.Akka.Abstractions;
using Air.Cloud.Modules.Akka.Attributes;
using Air.Cloud.Modules.Akka.Builders;
using Air.Cloud.Modules.Akka.Models;
using Air.Cloud.Modules.Akka.Options;
using Microsoft.Extensions.Options;

namespace Air.Cloud.Modules.Akka.Services;

/// <summary>
/// <para>zh-cn:默认 Akka.Cluster 服务入口，统一管理 ActorSystem 生命周期、Actor 注册、消息发送和节点状态读取。</para>
/// <para>en-us:Default Akka.Cluster service entry that manages ActorSystem lifecycle, actor registration, message sending, and node state reads.</para>
/// </summary>
public class AkkaClusterService : IAkkaClusterService
{
    private readonly AkkaSettingsOptions _options;
    private readonly IAkkaActorRegistry _actorRegistry;
    private readonly IEnumerable<IAkkaMessageAuthorizationHandler> _authorizationHandlers;
    private readonly SemaphoreSlim _lifecycleLocker = new(1, 1);
    private ActorSystem _actorSystem;

    /// <summary>
    /// <para>zh-cn:创建默认 Akka.Cluster 服务入口，组合模块配置、Actor 注册表和消息授权处理器；生命周期操作通过内部异步锁保证幂等。</para>
    /// <para>en-us:Creates the default Akka.Cluster service entry by combining settings, the actor registry, and message authorization handlers; lifecycle operations are made idempotent with an internal asynchronous lock.</para>
    /// </summary>
    /// <param name="options">
    /// <para>zh-cn:Akka.Cluster 模块配置选项。</para>
    /// <para>en-us:The Akka.Cluster module settings options.</para>
    /// </param>
    /// <param name="actorRegistry">
    /// <para>zh-cn:Actor 注册表。</para>
    /// <para>en-us:The actor registry.</para>
    /// </param>
    /// <param name="authorizationHandlers">
    /// <para>zh-cn:消息授权处理器集合；任一处理器拒绝时消息发送会被阻止。</para>
    /// <para>en-us:The message authorization handlers; if any handler denies a message, sending is blocked.</para>
    /// </param>
    public AkkaClusterService(
        IOptions<AkkaSettingsOptions> options,
        IAkkaActorRegistry actorRegistry,
        IEnumerable<IAkkaMessageAuthorizationHandler> authorizationHandlers)
    {
        _options = options.Value;
        _actorRegistry = actorRegistry;
        _authorizationHandlers = authorizationHandlers;
    }

    /// <summary>
    /// <para>zh-cn:当前 ActorSystem；在 `StartAsync` 成功前访问会抛出 `InvalidOperationException`。</para>
    /// <para>en-us:The current ActorSystem; accessing it before `StartAsync` succeeds throws `InvalidOperationException`.</para>
    /// </summary>
    public ActorSystem ActorSystem => _actorSystem ?? throw new InvalidOperationException("Akka ActorSystem has not started.");

    /// <summary>
    /// <para>zh-cn:启动 ActorSystem 并注册带 `AkkaActorAttribute` 标记的 Actor；未配置种子节点时会让当前节点自加入形成单节点集群。</para>
    /// <para>en-us:Starts the ActorSystem and registers actors marked with `AkkaActorAttribute`; when no seed nodes are configured, it joins itself to form a single-node Cluster.</para>
    /// </summary>
    /// <param name="cancellationToken">
    /// <para>zh-cn:启动取消令牌。</para>
    /// <para>en-us:The startup cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:表示启动过程的任务；重复调用会复用已启动的 ActorSystem。</para>
    /// <para>en-us:A task representing startup; repeated calls reuse the already started ActorSystem.</para>
    /// </returns>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return;
        }

        await _lifecycleLocker.WaitAsync(cancellationToken);
        try
        {
            if (_actorSystem != null)
            {
                return;
            }

            var config = ConfigurationFactory.ParseString(AkkaHoconBuilder.Build(_options));
            _actorSystem = ActorSystem.Create(_options.SystemName, config);

            if (!_options.SeedNodes.Any())
            {
                Cluster.Get(_actorSystem).Join(Cluster.Get(_actorSystem).SelfAddress);
            }

            RegisterAttributedActors();
        }
        finally
        {
            _lifecycleLocker.Release();
        }
    }

    /// <summary>
    /// <para>zh-cn:停止 ActorSystem，先请求当前节点离开集群，再在配置的超时时间内等待系统终止。</para>
    /// <para>en-us:Stops the ActorSystem by asking the current node to leave the Cluster first, then waiting for termination within the configured timeout.</para>
    /// </summary>
    /// <param name="cancellationToken">
    /// <para>zh-cn:停止取消令牌。</para>
    /// <para>en-us:The shutdown cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:表示停止过程的任务；未启动时调用不会抛出异常。</para>
    /// <para>en-us:A task representing shutdown; calling it before startup does not throw.</para>
    /// </returns>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycleLocker.WaitAsync(cancellationToken);
        try
        {
            if (_actorSystem == null)
            {
                return;
            }

            var cluster = Cluster.Get(_actorSystem);
            cluster.Leave(cluster.SelfAddress);
            await _actorSystem.Terminate().WaitAsync(TimeSpan.FromSeconds(_options.ShutdownTimeoutSeconds), cancellationToken);
            _actorSystem = null;
        }
        finally
        {
            _lifecycleLocker.Release();
        }
    }

    /// <summary>
    /// <para>zh-cn:创建指定类型的 Actor 并写入模块注册表；如果同名 Actor 已存在，则直接返回已注册引用。</para>
    /// <para>en-us:Creates an actor of the specified type and registers it in the module registry; if an actor with the same name already exists, the registered reference is returned.</para>
    /// </summary>
    /// <typeparam name="TActor">
    /// <para>zh-cn:Actor 类型，必须继承 `ActorBase`。</para>
    /// <para>en-us:The actor type, which must derive from `ActorBase`.</para>
    /// </typeparam>
    /// <param name="actorName">
    /// <para>zh-cn:Actor 注册名称。</para>
    /// <para>en-us:The actor registration name.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:新建或已有的 Actor 引用。</para>
    /// <para>en-us:The created or existing actor reference.</para>
    /// </returns>
    public IActorRef ActorOf<TActor>(string actorName) where TActor : ActorBase
    {
        EnsureStarted();

        if (_actorRegistry.TryGet(actorName, out var registeredActor))
        {
            return registeredActor;
        }

        var actorRef = _actorSystem.ActorOf(Props.Create(typeof(TActor)), actorName);
        _actorRegistry.Register(new AkkaActorDescriptor
        {
            ActorName = actorName,
            ActorType = typeof(TActor),
            ActorRef = actorRef
        });

        return actorRef;
    }

    /// <summary>
    /// <para>zh-cn:向指定 Actor 发送单向消息，发送前会检查 ActorSystem 状态、注册表命中情况和消息授权结果。</para>
    /// <para>en-us:Sends a one-way message to the specified actor after checking ActorSystem state, registry lookup, and message authorization.</para>
    /// </summary>
    /// <param name="actorName">
    /// <para>zh-cn:目标 Actor 注册名称。</para>
    /// <para>en-us:The target actor registration name.</para>
    /// </param>
    /// <param name="message">
    /// <para>zh-cn:消息对象。</para>
    /// <para>en-us:The message object.</para>
    /// </param>
    public void Tell(string actorName, object message)
    {
        var descriptor = GetAuthorizedDescriptor(actorName, message);
        descriptor.ActorRef.Tell(message);
    }

    /// <summary>
    /// <para>zh-cn:向指定 Actor 发送请求并等待强类型响应；未传入超时时使用配置中的默认 `Ask` 超时时间。</para>
    /// <para>en-us:Sends a request to the specified actor and waits for a typed response; when no timeout is supplied, the configured default Ask timeout is used.</para>
    /// </summary>
    /// <typeparam name="TResponse">
    /// <para>zh-cn:期望响应类型。</para>
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
    /// <para>zh-cn:可选超时时间；为空时使用模块默认值。</para>
    /// <para>en-us:The optional timeout.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:包含 Actor 响应的任务。</para>
    /// <para>en-us:A task containing the actor response.</para>
    /// </returns>
    public Task<TResponse> Ask<TResponse>(string actorName, object message, TimeSpan? timeout = null)
    {
        var descriptor = GetAuthorizedDescriptor(actorName, message);
        var askTimeout = timeout ?? TimeSpan.FromSeconds(_options.AskTimeoutSeconds);
        return descriptor.ActorRef.Ask<TResponse>(message, askTimeout);
    }

    /// <summary>
    /// <para>zh-cn:读取当前节点集群状态快照，包括节点地址、成员状态、角色集合和是否可用。</para>
    /// <para>en-us:Gets a snapshot of the current node Cluster state, including address, member status, roles, and whether the node is Up.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:当前节点状态信息。</para>
    /// <para>en-us:The current node status information.</para>
    /// </returns>
    public AkkaClusterNodeInfo GetCurrentNode()
    {
        EnsureStarted();

        var cluster = Cluster.Get(_actorSystem);
        var member = cluster.State.Members.FirstOrDefault(item => item.Address.Equals(cluster.SelfAddress));

        return new AkkaClusterNodeInfo
        {
            Address = cluster.SelfAddress.ToString(),
            Status = member?.Status.ToString(),
            Roles = cluster.SelfRoles.ToArray(),
            IsAvailable = member?.Status == MemberStatus.Up
        };
    }

    private void RegisterAttributedActors()
    {
        var actorTypes = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic)
            .SelectMany(LoadTypes)
            .Where(type => !type.IsAbstract && typeof(ActorBase).IsAssignableFrom(type))
            .Select(type => new
            {
                Type = type,
                Attribute = type.GetCustomAttribute<AkkaActorAttribute>()
            })
            .Where(item => item.Attribute != null);

        foreach (var item in actorTypes)
        {
            if (!ShouldRegisterOnCurrentNode(item.Attribute))
            {
                continue;
            }

            var actorName = BuildActorName(item.Attribute);
            var actorRef = _actorSystem.ActorOf(Props.Create(item.Type), actorName);
            _actorRegistry.Register(new AkkaActorDescriptor
            {
                ActorName = actorName,
                Domain = item.Attribute.Domain,
                Role = item.Attribute.Role,
                ActorType = item.Type,
                ActorRef = actorRef
            });
        }
    }

    private bool ShouldRegisterOnCurrentNode(AkkaActorAttribute attribute)
    {
        if (string.IsNullOrWhiteSpace(attribute.Role))
        {
            return true;
        }

        return _options.Roles.Any(role => role.Equals(attribute.Role, StringComparison.OrdinalIgnoreCase));
    }

    private AkkaActorDescriptor GetAuthorizedDescriptor(string actorName, object message)
    {
        EnsureStarted();

        var descriptor = _actorRegistry.GetDescriptors().FirstOrDefault(item => item.ActorName.Equals(actorName, StringComparison.OrdinalIgnoreCase));
        if (descriptor == null || descriptor.ActorRef == null)
        {
            throw new InvalidOperationException($"Akka actor '{actorName}' has not registered.");
        }

        if (_authorizationHandlers.Any(handler => !handler.CanSend(descriptor, message)))
        {
            throw new UnauthorizedAccessException($"Akka actor '{actorName}' denied the message.");
        }

        return descriptor;
    }

    private string BuildActorName(AkkaActorAttribute attribute)
    {
        if (string.IsNullOrWhiteSpace(attribute.Domain))
        {
            return attribute.ActorName;
        }

        if (_options.Domains.TryGetValue(attribute.Domain, out var domainOptions) &&
            !string.IsNullOrWhiteSpace(domainOptions.ActorNamePrefix))
        {
            return $"{domainOptions.ActorNamePrefix}-{attribute.ActorName}";
        }

        return $"{attribute.Domain}-{attribute.ActorName}";
    }

    private void EnsureStarted()
    {
        if (_actorSystem == null)
        {
            throw new InvalidOperationException("Akka ActorSystem has not started.");
        }
    }

    private static IEnumerable<Type> LoadTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.Where(type => type != null);
        }
    }
}

