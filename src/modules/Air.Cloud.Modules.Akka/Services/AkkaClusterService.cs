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
/// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
/// <para>en-us:Default Akka.Cluster service entry.</para>
/// </summary>
public class AkkaClusterService : IAkkaClusterService
{
    private readonly AkkaSettingsOptions _options;
    private readonly IAkkaActorRegistry _actorRegistry;
    private readonly IEnumerable<IAkkaMessageAuthorizationHandler> _authorizationHandlers;
    private readonly SemaphoreSlim _lifecycleLocker = new(1, 1);
    private ActorSystem _actorSystem;

    /// <summary>
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:Creates the default Akka.Cluster service entry by combining settings, the actor registry, and message authorization handlers; lifecycle operations are made idempotent with an internal asynchronous lock.</para>
    /// </summary>
    /// <param name="options">
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:The Akka.Cluster module settings options.</para>
    /// </param>
    /// <param name="actorRegistry">
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:The actor registry.</para>
    /// </param>
    /// <param name="authorizationHandlers">
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
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
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:The current ActorSystem; accessing it before `StartAsync` succeeds throws `InvalidOperationException`.</para>
    /// </summary>
    public ActorSystem ActorSystem => _actorSystem ?? throw new InvalidOperationException("Akka ActorSystem has not started.");

    /// <summary>
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:Starts the ActorSystem and registers actors marked with `AkkaActorAttribute`; when no seed nodes are configured, it joins itself to form a single-node Cluster.</para>
    /// </summary>
    /// <param name="cancellationToken">
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:The startup cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
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
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:Stops the ActorSystem by asking the current node to leave the Cluster first, then waiting for termination within the configured timeout.</para>
    /// </summary>
    /// <param name="cancellationToken">
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:The shutdown cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
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
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:Creates an actor of the specified type and registers it in the module registry; if an actor with the same name already exists, the registered reference is returned.</para>
    /// </summary>
    /// <typeparam name="TActor">
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:The actor type, which must derive from `ActorBase`.</para>
    /// </typeparam>
    /// <param name="actorName">
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:The actor registration name.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
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
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:Sends a one-way message to the specified actor after checking ActorSystem state, registry lookup, and message authorization.</para>
    /// </summary>
    /// <param name="actorName">
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:The target actor registration name.</para>
    /// </param>
    /// <param name="message">
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:The message object.</para>
    /// </param>
    public void Tell(string actorName, object message)
    {
        var descriptor = GetAuthorizedDescriptor(actorName, message);
        descriptor.ActorRef.Tell(message);
    }

    /// <summary>
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:Sends a request to the specified actor and waits for a typed response; when no timeout is supplied, the configured default Ask timeout is used.</para>
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
    /// <para>en-us:The optional timeout.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:A task containing the actor response.</para>
    /// </returns>
    public Task<TResponse> Ask<TResponse>(string actorName, object message, TimeSpan? timeout = null)
    {
        var descriptor = GetAuthorizedDescriptor(actorName, message);
        var askTimeout = timeout ?? TimeSpan.FromSeconds(_options.AskTimeoutSeconds);
        return descriptor.ActorRef.Ask<TResponse>(message, askTimeout);
    }

    /// <summary>
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
    /// <para>en-us:Gets a snapshot of the current node Cluster state, including address, member status, roles, and whether the node is Up.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:该成员属于 Akka.Cluster 模块公开契约，提供配置、注册、生命周期、节点状态或消息发送相关行为，并遵循模块的默认值、异常和授权约定。</para>
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

