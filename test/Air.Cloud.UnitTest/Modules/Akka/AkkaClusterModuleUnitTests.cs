using Akka.Actor;
using Air.Cloud.Modules.Akka.Abstractions;
using Air.Cloud.Modules.Akka.Attributes;
using Air.Cloud.Modules.Akka.Builders;
using Air.Cloud.Modules.Akka.Extensions;
using Air.Cloud.Modules.Akka.Hosting;
using Air.Cloud.Modules.Akka.Models;
using Air.Cloud.Modules.Akka.Options;
using Air.Cloud.Modules.Akka.Registries;
using Air.Cloud.Modules.Akka.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Air.Cloud.UnitTest.Modules.Akka;

/// <summary>
/// <para>zh-cn:Akka.Cluster 模块单元测试，覆盖配置默认值、HOCON 构建、注册表行为、DI 注册和未启动生命周期边界。</para>
/// <para>en-us:Unit tests for the Akka.Cluster module, covering option defaults, HOCON building, registry behavior, DI registration, and unstarted lifecycle boundaries.</para>
/// </summary>
public class AkkaClusterModuleUnitTests
{
    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `AkkaSettingsOptions should provide cluster safe defaults` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `AkkaSettingsOptions should provide cluster safe defaults` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    public void AkkaSettingsOptions_should_provide_cluster_safe_defaults()
    {
        var options = new AkkaSettingsOptions();

        Assert.True(options.Enabled);
        Assert.Equal("air-cloud", options.SystemName);
        Assert.Equal("0.0.0.0", options.Host);
        Assert.Equal(0, options.Port);
        Assert.Equal(10, options.AskTimeoutSeconds);
        Assert.Equal(30, options.ShutdownTimeoutSeconds);
    }

    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `AkkaSettingsOptions should initialize mutable collections` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `AkkaSettingsOptions should initialize mutable collections` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    public void AkkaSettingsOptions_should_initialize_mutable_collections()
    {
        var options = new AkkaSettingsOptions();

        options.Roles.Add("worker");
        options.SeedNodes.Add("akka.tcp://air@127.0.0.1:4053");
        options.Domains.Add("Order", new AkkaDomainOptions());

        Assert.Contains("worker", options.Roles);
        Assert.Single(options.SeedNodes);
        Assert.True(options.Domains.ContainsKey("order"));
    }

    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `AkkaSettingsOptions should keep domains case insensitive for business names` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `AkkaSettingsOptions should keep domains case insensitive for business names` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    public void AkkaSettingsOptions_should_keep_domains_case_insensitive_for_business_names()
    {
        var options = new AkkaSettingsOptions();

        options.Domains["Order"] = new AkkaDomainOptions { ActorNamePrefix = "order" };

        Assert.True(options.Domains.ContainsKey("order"));
        Assert.Equal("order", options.Domains["ORDER"].ActorNamePrefix);
    }

    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `AkkaDomainOptions should default to cross domain denied` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `AkkaDomainOptions should default to cross domain denied` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    public void AkkaDomainOptions_should_default_to_cross_domain_denied()
    {
        var options = new AkkaDomainOptions();

        Assert.False(options.AllowCrossDomainMessages);
    }

    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `AkkaDomainOptions should allow business prefix and role to be configured` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `AkkaDomainOptions should allow business prefix and role to be configured` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    public void AkkaDomainOptions_should_allow_business_prefix_and_role_to_be_configured()
    {
        var options = new AkkaDomainOptions
        {
            ActorNamePrefix = "inventory",
            Role = "inventory",
            AllowCrossDomainMessages = true
        };

        Assert.Equal("inventory", options.ActorNamePrefix);
        Assert.Equal("inventory", options.Role);
        Assert.True(options.AllowCrossDomainMessages);
    }

    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `AkkaHoconBuilder should enable cluster actor provider` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `AkkaHoconBuilder should enable cluster actor provider` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    public void AkkaHoconBuilder_should_enable_cluster_actor_provider()
    {
        var hocon = AkkaHoconBuilder.Build(new AkkaSettingsOptions());

        Assert.Contains("actor.provider = cluster", hocon);
    }

    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `AkkaHoconBuilder should render remote host and port` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `AkkaHoconBuilder should render remote host and port` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    public void AkkaHoconBuilder_should_render_remote_host_and_port()
    {
        var hocon = AkkaHoconBuilder.Build(new AkkaSettingsOptions
        {
            Host = "127.0.0.1",
            Port = 4053
        });

        Assert.Contains("hostname = \"127.0.0.1\"", hocon);
        Assert.Contains("port = 4053", hocon);
    }

    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `AkkaHoconBuilder should render roles` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `AkkaHoconBuilder should render roles` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    public void AkkaHoconBuilder_should_render_roles()
    {
        var hocon = AkkaHoconBuilder.Build(new AkkaSettingsOptions
        {
            Roles = new List<string> { "order", "inventory" }
        });

        Assert.Contains("roles = [\"order\", \"inventory\"]", hocon);
    }

    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `AkkaHoconBuilder should render seed nodes` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `AkkaHoconBuilder should render seed nodes` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    public void AkkaHoconBuilder_should_render_seed_nodes()
    {
        var hocon = AkkaHoconBuilder.Build(new AkkaSettingsOptions
        {
            SeedNodes = new List<string> { "akka.tcp://air@node-0:4053", "akka.tcp://air@node-1:4053" }
        });

        Assert.Contains("seed-nodes = [\"akka.tcp://air@node-0:4053\", \"akka.tcp://air@node-1:4053\"]", hocon);
    }

    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `AkkaHoconBuilder should append custom hocon` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `AkkaHoconBuilder should append custom hocon` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    public void AkkaHoconBuilder_should_append_custom_hocon()
    {
        var hocon = AkkaHoconBuilder.Build(new AkkaSettingsOptions
        {
            Hocon = "akka.loglevel = DEBUG"
        });

        Assert.Contains("akka.loglevel = DEBUG", hocon);
    }

    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `AkkaActorAttribute should store actor metadata` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `AkkaActorAttribute should store actor metadata` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    public void AkkaActorAttribute_should_store_actor_metadata()
    {
        var attribute = new AkkaActorAttribute("order-worker")
        {
            Domain = "Order",
            Role = "order"
        };

        Assert.Equal("order-worker", attribute.ActorName);
        Assert.Equal("Order", attribute.Domain);
        Assert.Equal("order", attribute.Role);
    }

    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `AkkaActorDescriptor should store registration state` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `AkkaActorDescriptor should store registration state` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    public void AkkaActorDescriptor_should_store_registration_state()
    {
        var descriptor = new AkkaActorDescriptor
        {
            ActorName = "actor",
            Domain = "Domain",
            Role = "role",
            ActorType = typeof(TestActor),
            ActorRef = ActorRefs.Nobody
        };

        Assert.Equal("actor", descriptor.ActorName);
        Assert.Equal(typeof(TestActor), descriptor.ActorType);
        Assert.Same(ActorRefs.Nobody, descriptor.ActorRef);
    }

    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `AkkaActorRegistry should reject null descriptor` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `AkkaActorRegistry should reject null descriptor` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    public void AkkaActorRegistry_should_reject_null_descriptor()
    {
        var registry = new AkkaActorRegistry();

        Assert.Throws<ArgumentNullException>(() => registry.Register(null!));
    }

    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `AkkaActorRegistry should reject empty actor name` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `AkkaActorRegistry should reject empty actor name` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    public void AkkaActorRegistry_should_reject_empty_actor_name()
    {
        var registry = new AkkaActorRegistry();

        Assert.Throws<ArgumentException>(() => registry.Register(new AkkaActorDescriptor()));
    }

    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `AkkaActorRegistry should register and resolve actor ref` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `AkkaActorRegistry should register and resolve actor ref` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    public void AkkaActorRegistry_should_register_and_resolve_actor_ref()
    {
        var registry = new AkkaActorRegistry();

        registry.Register(new AkkaActorDescriptor
        {
            ActorName = "worker",
            ActorRef = ActorRefs.Nobody
        });

        var found = registry.TryGet("worker", out var actorRef);

        Assert.True(found);
        Assert.Same(ActorRefs.Nobody, actorRef);
    }

    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `AkkaActorRegistry should resolve actor name case insensitively` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `AkkaActorRegistry should resolve actor name case insensitively` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    public void AkkaActorRegistry_should_resolve_actor_name_case_insensitively()
    {
        var registry = new AkkaActorRegistry();

        registry.Register(new AkkaActorDescriptor
        {
            ActorName = "OrderWorker",
            ActorRef = ActorRefs.Nobody
        });

        Assert.True(registry.TryGet("orderworker", out _));
    }

    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `AkkaActorRegistry should replace existing descriptor` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `AkkaActorRegistry should replace existing descriptor` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    public void AkkaActorRegistry_should_replace_existing_descriptor()
    {
        var registry = new AkkaActorRegistry();

        registry.Register(new AkkaActorDescriptor { ActorName = "worker", Domain = "A", ActorRef = ActorRefs.Nobody });
        registry.Register(new AkkaActorDescriptor { ActorName = "worker", Domain = "B", ActorRef = ActorRefs.Nobody });

        Assert.Equal("B", registry.GetDescriptors().Single().Domain);
    }

    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `AkkaActorRegistry should keep same actor name singleton even across domains` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `AkkaActorRegistry should keep same actor name singleton even across domains` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    public void AkkaActorRegistry_should_keep_same_actor_name_singleton_even_across_domains()
    {
        var registry = new AkkaActorRegistry();

        registry.Register(new AkkaActorDescriptor { ActorName = "worker", Domain = "Order", ActorRef = ActorRefs.Nobody });
        registry.Register(new AkkaActorDescriptor { ActorName = "worker", Domain = "Inventory", ActorRef = ActorRefs.Nobody });

        Assert.Single(registry.GetDescriptors());
        Assert.Equal("Inventory", registry.GetDescriptors().Single().Domain);
    }

    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `AkkaActorRegistry should allow business isolation when actor names are prefixed` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `AkkaActorRegistry should allow business isolation when actor names are prefixed` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    public void AkkaActorRegistry_should_allow_business_isolation_when_actor_names_are_prefixed()
    {
        var registry = new AkkaActorRegistry();

        registry.Register(new AkkaActorDescriptor { ActorName = "order-worker", Domain = "Order", ActorRef = ActorRefs.Nobody });
        registry.Register(new AkkaActorDescriptor { ActorName = "inventory-worker", Domain = "Inventory", ActorRef = ActorRefs.Nobody });

        Assert.Equal(2, registry.GetDescriptors().Count);
        Assert.True(registry.TryGet("order-worker", out _));
        Assert.True(registry.TryGet("inventory-worker", out _));
    }

    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `AkkaActorRegistry should return nobody for missing actor` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `AkkaActorRegistry should return nobody for missing actor` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    public void AkkaActorRegistry_should_return_nobody_for_missing_actor()
    {
        var registry = new AkkaActorRegistry();

        var found = registry.TryGet("missing", out var actorRef);

        Assert.False(found);
        Assert.Same(ActorRefs.Nobody, actorRef);
    }

    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `AddAkkaCluster should register cluster services` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `AddAkkaCluster should register cluster services` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    public void AddAkkaCluster_should_register_cluster_services()
    {
        var services = new ServiceCollection();

        services.AddAkkaCluster();
        using var provider = services.BuildServiceProvider();

        Assert.IsType<AkkaActorRegistry>(provider.GetRequiredService<IAkkaActorRegistry>());
        Assert.IsType<AkkaClusterService>(provider.GetRequiredService<IAkkaClusterService>());
        Assert.Contains(provider.GetServices<IAkkaMessageAuthorizationHandler>(), handler => handler is DefaultAkkaMessageAuthorizationHandler);
        Assert.Contains(provider.GetServices<IHostedService>(), service => service is AkkaClusterHostedService);
    }

    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `AddAkkaCluster should apply configure delegate` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `AddAkkaCluster should apply configure delegate` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    public void AddAkkaCluster_should_apply_configure_delegate()
    {
        var services = new ServiceCollection();

        services.AddAkkaCluster(options =>
        {
            options.SystemName = "configured-system";
            options.Roles.Add("configured-role");
        });
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AkkaSettingsOptions>>().Value;

        Assert.Equal("configured-system", options.SystemName);
        Assert.Contains("configured-role", options.Roles);
    }

    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `AddAkkaCluster should not override custom registry` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `AddAkkaCluster should not override custom registry` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    public void AddAkkaCluster_should_not_override_custom_registry()
    {
        var services = new ServiceCollection();
        var registry = new CustomRegistry();

        services.AddSingleton<IAkkaActorRegistry>(registry);
        services.AddAkkaCluster();
        using var provider = services.BuildServiceProvider();

        Assert.Same(registry, provider.GetRequiredService<IAkkaActorRegistry>());
    }

    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `AddAkkaCluster should not override custom cluster service` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `AddAkkaCluster should not override custom cluster service` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    public void AddAkkaCluster_should_not_override_custom_cluster_service()
    {
        var services = new ServiceCollection();
        var clusterService = new CustomClusterService();

        services.AddSingleton<IAkkaClusterService>(clusterService);
        services.AddAkkaCluster();
        using var provider = services.BuildServiceProvider();

        Assert.Same(clusterService, provider.GetRequiredService<IAkkaClusterService>());
    }

    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `DefaultAkkaMessageAuthorizationHandler should allow messages` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `DefaultAkkaMessageAuthorizationHandler should allow messages` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    public void DefaultAkkaMessageAuthorizationHandler_should_allow_messages()
    {
        var handler = new DefaultAkkaMessageAuthorizationHandler();

        var allowed = handler.CanSend(new AkkaActorDescriptor { ActorName = "worker" }, new object());

        Assert.True(allowed);
    }

    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `AkkaClusterService should ignore start when disabled` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `AkkaClusterService should ignore start when disabled` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    public async Task AkkaClusterService_should_ignore_start_when_disabled()
    {
        var service = BuildService(new AkkaSettingsOptions { Enabled = false });

        await service.StartAsync();

        Assert.Throws<InvalidOperationException>(() => service.ActorSystem);
    }

    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `AkkaClusterService should allow stop before start` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `AkkaClusterService should allow stop before start` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    public async Task AkkaClusterService_should_allow_stop_before_start()
    {
        var service = BuildService(new AkkaSettingsOptions());

        await service.StopAsync();

        Assert.Throws<InvalidOperationException>(() => service.ActorSystem);
    }

    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `AkkaClusterService should reject tell before start` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `AkkaClusterService should reject tell before start` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    public void AkkaClusterService_should_reject_tell_before_start()
    {
        var service = BuildService(new AkkaSettingsOptions());

        Assert.Throws<InvalidOperationException>(() => service.Tell("missing", new object()));
    }

    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `AkkaClusterHostedService should delegate lifecycle` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `AkkaClusterHostedService should delegate lifecycle` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    public async Task AkkaClusterHostedService_should_delegate_lifecycle()
    {
        var service = new CustomClusterService();
        var hostedService = new AkkaClusterHostedService(service);

        await hostedService.StartAsync(CancellationToken.None);
        await hostedService.StopAsync(CancellationToken.None);

        Assert.True(service.Started);
        Assert.True(service.Stopped);
    }

    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `IAkkaClusterService should expose parameterized actor creation contract` 场景，确保业务实现可以接收显式构造参数创建 Actor。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `IAkkaClusterService should expose parameterized actor creation contract` scenario, ensuring business implementations can accept explicit constructor arguments when creating actors.</para>
    /// </summary>
    [Fact]
    public void IAkkaClusterService_should_expose_parameterized_actor_creation_contract()
    {
        IAkkaClusterService service = new CustomClusterService();

        service.ActorOf<TestActor>("contract-actor", "arg-1", 2);

        var customService = Assert.IsType<CustomClusterService>(service);
        Assert.Equal("contract-actor", customService.LastActorName);
        Assert.Equal(new object[] { "arg-1", 2 }, customService.LastActorArgs);
    }

    private static AkkaClusterService BuildService(AkkaSettingsOptions options)
    {
        var services = new ServiceCollection();

        return new AkkaClusterService(
            Options.Create(options),
            new AkkaActorRegistry(),
            new[] { new DefaultAkkaMessageAuthorizationHandler() },
            services.BuildServiceProvider());
    }

    private sealed class TestActor : ReceiveActor
    {
    }

    private sealed class CustomRegistry : IAkkaActorRegistry
    {
        public void Register(AkkaActorDescriptor descriptor)
        {
        }

        public bool TryGet(string actorName, out IActorRef actorRef)
        {
            actorRef = ActorRefs.Nobody;
            return false;
        }

        public IReadOnlyCollection<AkkaActorDescriptor> GetDescriptors()
        {
            return Array.Empty<AkkaActorDescriptor>();
        }
    }

    private sealed class CustomClusterService : IAkkaClusterService
    {
        public bool Started { get; private set; }

        public bool Stopped { get; private set; }

        public string LastActorName { get; private set; } = string.Empty;

        public object[] LastActorArgs { get; private set; } = Array.Empty<object>();

        public ActorSystem ActorSystem => throw new NotSupportedException();

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            Started = true;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            Stopped = true;
            return Task.CompletedTask;
        }

        public IActorRef ActorOf<TActor>(string actorName) where TActor : ActorBase
        {
            LastActorName = actorName;
            LastActorArgs = Array.Empty<object>();
            return ActorRefs.Nobody;
        }

        public IActorRef ActorOf<TActor>(string actorName, params object[] args) where TActor : ActorBase
        {
            LastActorName = actorName;
            LastActorArgs = args;
            return ActorRefs.Nobody;
        }

        public void Tell(string actorName, object message)
        {
        }

        public Task<TResponse> Ask<TResponse>(string actorName, object message, TimeSpan? timeout = null)
        {
            return Task.FromResult(default(TResponse)!);
        }

        public AkkaClusterNodeInfo GetCurrentNode()
        {
            return new AkkaClusterNodeInfo();
        }
    }
}
