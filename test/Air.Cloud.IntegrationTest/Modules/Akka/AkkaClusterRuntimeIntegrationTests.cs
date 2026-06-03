using System.Net;
using System.Net.Sockets;
using Akka.Actor;
using Air.Cloud.Modules.Akka.Abstractions;
using Air.Cloud.Modules.Akka.Attributes;
using Air.Cloud.Modules.Akka.Models;
using Air.Cloud.Modules.Akka.Options;
using Air.Cloud.Modules.Akka.Registries;
using Air.Cloud.Modules.Akka.Services;
using Microsoft.Extensions.Options;

namespace Air.Cloud.IntegrationTest.Modules.Akka;

/// <summary>
/// <para>zh-cn:Akka.Cluster 运行时集成测试，启动真实 ActorSystem 验证单节点 Cluster、Actor 收发、自动扫描、Role/Domain 隔离和跨业务域授权策略。</para>
/// <para>en-us:Runtime integration tests for Akka.Cluster, starting a real ActorSystem to verify single-node Cluster behavior, actor messaging, auto scanning, Role/Domain isolation, and cross-business-domain authorization policies.</para>
/// </summary>
public class AkkaClusterRuntimeIntegrationTests
{
    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `StartAsync should create real actor system` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `StartAsync should create real actor system` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Akka")]
    public async Task StartAsync_should_create_real_actor_system()
    {
        var module = BuildModule();

        await module.Service.StartAsync();
        try
        {
            Assert.NotNull(module.Service.ActorSystem);
            Assert.False(module.Service.ActorSystem.WhenTerminated.IsCompleted);
        }
        finally
        {
            await module.Service.StopAsync();
        }
    }

    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `StartAsync should be idempotent for same service` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `StartAsync should be idempotent for same service` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Akka")]
    public async Task StartAsync_should_be_idempotent_for_same_service()
    {
        var module = BuildModule();

        await module.Service.StartAsync();
        try
        {
            var actorSystem = module.Service.ActorSystem;

            await module.Service.StartAsync();

            Assert.Same(actorSystem, module.Service.ActorSystem);
        }
        finally
        {
            await module.Service.StopAsync();
        }
    }

    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `StopAsync should terminate real actor system` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `StopAsync should terminate real actor system` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Akka")]
    public async Task StopAsync_should_terminate_real_actor_system()
    {
        var module = BuildModule();

        await module.Service.StartAsync();
        var actorSystem = module.Service.ActorSystem;

        await module.Service.StopAsync();

        Assert.True(actorSystem.WhenTerminated.IsCompleted);
    }

    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `Empty seed nodes should join single node cluster` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `Empty seed nodes should join single node cluster` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Akka")]
    public async Task Empty_seed_nodes_should_join_single_node_cluster()
    {
        var module = BuildModule();

        await module.Service.StartAsync();
        try
        {
            var node = await WaitForAvailableNodeAsync(module.Service);

            Assert.True(node.IsAvailable);
            Assert.Contains("akka-integration", node.Roles);
        }
        finally
        {
            await module.Service.StopAsync();
        }
    }

    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `GetCurrentNode should report self address` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `GetCurrentNode should report self address` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Akka")]
    public async Task GetCurrentNode_should_report_self_address()
    {
        var module = BuildModule();

        await module.Service.StartAsync();
        try
        {
            var node = module.Service.GetCurrentNode();

            Assert.Contains(module.Options.SystemName, node.Address);
            Assert.Contains(module.Options.Port.ToString(), node.Address);
        }
        finally
        {
            await module.Service.StopAsync();
        }
    }

    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `ActorOf should create actor and register descriptor` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `ActorOf should create actor and register descriptor` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Akka")]
    public async Task ActorOf_should_create_actor_and_register_descriptor()
    {
        var module = BuildModule();

        await module.Service.StartAsync();
        try
        {
            var actorRef = module.Service.ActorOf<EchoActor>("manual-echo");

            Assert.NotNull(actorRef);
            Assert.Contains(module.Registry.GetDescriptors(), descriptor => descriptor.ActorName == "manual-echo");
        }
        finally
        {
            await module.Service.StopAsync();
        }
    }

    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `ActorOf should return registered actor for same name` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `ActorOf should return registered actor for same name` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Akka")]
    public async Task ActorOf_should_return_registered_actor_for_same_name()
    {
        var module = BuildModule();

        await module.Service.StartAsync();
        try
        {
            var first = module.Service.ActorOf<EchoActor>("shared-echo");
            var second = module.Service.ActorOf<EchoActor>("shared-echo");

            Assert.Same(first, second);
        }
        finally
        {
            await module.Service.StopAsync();
        }
    }

    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `Tell should deliver message to manual actor` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `Tell should deliver message to manual actor` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Akka")]
    public async Task Tell_should_deliver_message_to_manual_actor()
    {
        var module = BuildModule();

        await module.Service.StartAsync();
        try
        {
            module.Service.ActorOf<EchoActor>("tell-echo");
            var completion = NewCompletion<string>();

            module.Service.Tell("tell-echo", new CompleteMessage("tell-payload", completion));

            Assert.Equal("tell-payload", await WithTimeout(completion.Task));
        }
        finally
        {
            await module.Service.StopAsync();
        }
    }

    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `Ask should return response from manual actor` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `Ask should return response from manual actor` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Akka")]
    public async Task Ask_should_return_response_from_manual_actor()
    {
        var module = BuildModule();

        await module.Service.StartAsync();
        try
        {
            module.Service.ActorOf<EchoActor>("ask-echo");

            var response = await module.Service.Ask<string>("ask-echo", new QueryMessage("ask-payload"));

            Assert.Equal("ask-payload", response);
        }
        finally
        {
            await module.Service.StopAsync();
        }
    }

    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `Tell should throw for missing actor` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `Tell should throw for missing actor` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Akka")]
    public async Task Tell_should_throw_for_missing_actor()
    {
        var module = BuildModule();

        await module.Service.StartAsync();
        try
        {
            Assert.Throws<InvalidOperationException>(() => module.Service.Tell("missing-actor", new object()));
        }
        finally
        {
            await module.Service.StopAsync();
        }
    }

    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `Ask should throw for missing actor` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `Ask should throw for missing actor` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Akka")]
    public async Task Ask_should_throw_for_missing_actor()
    {
        var module = BuildModule();

        await module.Service.StartAsync();
        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => module.Service.Ask<string>("missing-actor", new QueryMessage("missing")));
        }
        finally
        {
            await module.Service.StopAsync();
        }
    }

    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `Authorization handler should deny tell` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `Authorization handler should deny tell` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Akka")]
    public async Task Authorization_handler_should_deny_tell()
    {
        var module = BuildModule(handlers: new IAkkaMessageAuthorizationHandler[] { new DenyAllAuthorizationHandler() });

        await module.Service.StartAsync();
        try
        {
            module.Service.ActorOf<EchoActor>("denied-tell");

            Assert.Throws<UnauthorizedAccessException>(() => module.Service.Tell("denied-tell", new object()));
        }
        finally
        {
            await module.Service.StopAsync();
        }
    }

    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `Authorization handler should deny ask` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `Authorization handler should deny ask` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Akka")]
    public async Task Authorization_handler_should_deny_ask()
    {
        var module = BuildModule(handlers: new IAkkaMessageAuthorizationHandler[] { new DenyAllAuthorizationHandler() });

        await module.Service.StartAsync();
        try
        {
            module.Service.ActorOf<EchoActor>("denied-ask");

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => module.Service.Ask<string>("denied-ask", new QueryMessage("denied")));
        }
        finally
        {
            await module.Service.StopAsync();
        }
    }

    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `Authorization handler should allow tell when policy passes` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `Authorization handler should allow tell when policy passes` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Akka")]
    public async Task Authorization_handler_should_allow_tell_when_policy_passes()
    {
        var module = BuildModule(handlers: new IAkkaMessageAuthorizationHandler[] { new AllowAllAuthorizationHandler() });

        await module.Service.StartAsync();
        try
        {
            module.Service.ActorOf<EchoActor>("allowed-tell");
            var completion = NewCompletion<string>();

            module.Service.Tell("allowed-tell", new CompleteMessage("allowed", completion));

            Assert.Equal("allowed", await WithTimeout(completion.Task));
        }
        finally
        {
            await module.Service.StopAsync();
        }
    }

    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `Attributed actor should be registered when role matches` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `Attributed actor should be registered when role matches` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Akka")]
    public async Task Attributed_actor_should_be_registered_when_role_matches()
    {
        var module = BuildModule(options =>
        {
            options.Roles.Clear();
            options.Roles.Add("akka-integration");
        });

        await module.Service.StartAsync();
        try
        {
            Assert.Contains(module.Registry.GetDescriptors(), descriptor => descriptor.ActorType == typeof(AttributedEchoActor));
        }
        finally
        {
            await module.Service.StopAsync();
        }
    }

    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `Attributed actor should receive message by domain prefixed name` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `Attributed actor should receive message by domain prefixed name` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Akka")]
    public async Task Attributed_actor_should_receive_message_by_domain_prefixed_name()
    {
        var module = BuildModule(options =>
        {
            options.Domains["Integration"] = new AkkaDomainOptions { ActorNamePrefix = "it" };
        });

        await module.Service.StartAsync();
        try
        {
            var completion = NewCompletion<string>();

            module.Service.Tell("it-attributed-echo", new CompleteMessage("attributed", completion));

            Assert.Equal("attributed", await WithTimeout(completion.Task));
        }
        finally
        {
            await module.Service.StopAsync();
        }
    }

    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `Attributed actor without matching role should not register` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `Attributed actor without matching role should not register` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Akka")]
    public async Task Attributed_actor_without_matching_role_should_not_register()
    {
        var module = BuildModule(options =>
        {
            options.Roles.Clear();
            options.Roles.Add("another-role");
        });

        await module.Service.StartAsync();
        try
        {
            Assert.DoesNotContain(module.Registry.GetDescriptors(), descriptor => descriptor.ActorType == typeof(AttributedEchoActor));
        }
        finally
        {
            await module.Service.StopAsync();
        }
    }

    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `Roleless attributed actor should register on any role` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `Roleless attributed actor should register on any role` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Akka")]
    public async Task Roleless_attributed_actor_should_register_on_any_role()
    {
        var module = BuildModule(options =>
        {
            options.Roles.Clear();
            options.Roles.Add("any-role");
        });

        await module.Service.StartAsync();
        try
        {
            Assert.Contains(module.Registry.GetDescriptors(), descriptor => descriptor.ActorType == typeof(RolelessAttributedActor));
        }
        finally
        {
            await module.Service.StopAsync();
        }
    }

    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `Domain without prefix should use domain name for actor registration` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `Domain without prefix should use domain name for actor registration` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Akka")]
    public async Task Domain_without_prefix_should_use_domain_name_for_actor_registration()
    {
        var module = BuildModule(options => options.Domains.Clear());

        await module.Service.StartAsync();
        try
        {
            var completion = NewCompletion<string>();

            module.Service.Tell("Integration-attributed-echo", new CompleteMessage("domain-name", completion));

            Assert.Equal("domain-name", await WithTimeout(completion.Task));
        }
        finally
        {
            await module.Service.StopAsync();
        }
    }

    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `Different manual actors should process independent messages` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `Different manual actors should process independent messages` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Akka")]
    public async Task Different_manual_actors_should_process_independent_messages()
    {
        var module = BuildModule();

        await module.Service.StartAsync();
        try
        {
            module.Service.ActorOf<EchoActor>("echo-a");
            module.Service.ActorOf<EchoActor>("echo-b");
            var completionA = NewCompletion<string>();
            var completionB = NewCompletion<string>();

            module.Service.Tell("echo-a", new CompleteMessage("A", completionA));
            module.Service.Tell("echo-b", new CompleteMessage("B", completionB));

            Assert.Equal("A", await WithTimeout(completionA.Task));
            Assert.Equal("B", await WithTimeout(completionB.Task));
        }
        finally
        {
            await module.Service.StopAsync();
        }
    }

    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `Actor registry should keep domain metadata for attributed actor` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `Actor registry should keep domain metadata for attributed actor` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Akka")]
    public async Task Actor_registry_should_keep_domain_metadata_for_attributed_actor()
    {
        var module = BuildModule();

        await module.Service.StartAsync();
        try
        {
            var descriptor = module.Registry.GetDescriptors().Single(item => item.ActorType == typeof(AttributedEchoActor));

            Assert.Equal("Integration", descriptor.Domain);
            Assert.Equal("akka-integration", descriptor.Role);
        }
        finally
        {
            await module.Service.StopAsync();
        }
    }

    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `ActorOf should support multiple actor types in one system` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `ActorOf should support multiple actor types in one system` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Akka")]
    public async Task ActorOf_should_support_multiple_actor_types_in_one_system()
    {
        var module = BuildModule();

        await module.Service.StartAsync();
        try
        {
            module.Service.ActorOf<EchoActor>("multi-echo");
            module.Service.ActorOf<UppercaseActor>("multi-upper");

            var echo = await module.Service.Ask<string>("multi-echo", new QueryMessage("mixed"));
            var upper = await module.Service.Ask<string>("multi-upper", new QueryMessage("mixed"));

            Assert.Equal("mixed", echo);
            Assert.Equal("MIXED", upper);
        }
        finally
        {
            await module.Service.StopAsync();
        }
    }

    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `Multi business cluster should register multiple business domains` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `Multi business cluster should register multiple business domains` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Akka")]
    public async Task Multi_business_cluster_should_register_multiple_business_domains()
    {
        var module = BuildMultiBusinessModule();

        await module.Service.StartAsync();
        try
        {
            var descriptors = module.Registry.GetDescriptors();

            Assert.Contains(descriptors, descriptor => descriptor.ActorType == typeof(OrderWorkerActor));
            Assert.Contains(descriptors, descriptor => descriptor.ActorType == typeof(InventoryWorkerActor));
        }
        finally
        {
            await module.Service.StopAsync();
        }
    }

    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `Multi business cluster should isolate same actor name by domain prefix` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `Multi business cluster should isolate same actor name by domain prefix` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Akka")]
    public async Task Multi_business_cluster_should_isolate_same_actor_name_by_domain_prefix()
    {
        var module = BuildMultiBusinessModule();

        await module.Service.StartAsync();
        try
        {
            var actorNames = module.Registry.GetDescriptors().Select(descriptor => descriptor.ActorName).ToArray();

            Assert.Contains("order-worker", actorNames);
            Assert.Contains("inventory-worker", actorNames);
        }
        finally
        {
            await module.Service.StopAsync();
        }
    }

    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `Multi business cluster should route messages to correct domain actor` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `Multi business cluster should route messages to correct domain actor` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Akka")]
    public async Task Multi_business_cluster_should_route_messages_to_correct_domain_actor()
    {
        var module = BuildMultiBusinessModule();

        await module.Service.StartAsync();
        try
        {
            var orderCompletion = NewCompletion<string>();
            var inventoryCompletion = NewCompletion<string>();

            module.Service.Tell("order-worker", new CompleteMessage("order-message", orderCompletion));
            module.Service.Tell("inventory-worker", new CompleteMessage("inventory-message", inventoryCompletion));

            Assert.Equal("order-message", await WithTimeout(orderCompletion.Task));
            Assert.Equal("inventory-message", await WithTimeout(inventoryCompletion.Task));
        }
        finally
        {
            await module.Service.StopAsync();
        }
    }

    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `Multi business cluster should not register actor when business role absent` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `Multi business cluster should not register actor when business role absent` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Akka")]
    public async Task Multi_business_cluster_should_not_register_actor_when_business_role_absent()
    {
        var module = BuildMultiBusinessModule();

        await module.Service.StartAsync();
        try
        {
            Assert.DoesNotContain(module.Registry.GetDescriptors(), descriptor => descriptor.ActorType == typeof(PaymentWorkerActor));
        }
        finally
        {
            await module.Service.StopAsync();
        }
    }

    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `Multi business cluster should report all current business roles` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `Multi business cluster should report all current business roles` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Akka")]
    public async Task Multi_business_cluster_should_report_all_current_business_roles()
    {
        var module = BuildMultiBusinessModule();

        await module.Service.StartAsync();
        try
        {
            var node = module.Service.GetCurrentNode();

            Assert.Contains("order", node.Roles);
            Assert.Contains("inventory", node.Roles);
            Assert.DoesNotContain("payment", node.Roles);
        }
        finally
        {
            await module.Service.StopAsync();
        }
    }

    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `Multi business cluster should keep business domain metadata separated` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `Multi business cluster should keep business domain metadata separated` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Akka")]
    public async Task Multi_business_cluster_should_keep_business_domain_metadata_separated()
    {
        var module = BuildMultiBusinessModule();

        await module.Service.StartAsync();
        try
        {
            var order = module.Registry.GetDescriptors().Single(descriptor => descriptor.ActorType == typeof(OrderWorkerActor));
            var inventory = module.Registry.GetDescriptors().Single(descriptor => descriptor.ActorType == typeof(InventoryWorkerActor));

            Assert.Equal("Order", order.Domain);
            Assert.Equal("order", order.Role);
            Assert.Equal("Inventory", inventory.Domain);
            Assert.Equal("inventory", inventory.Role);
        }
        finally
        {
            await module.Service.StopAsync();
        }
    }

    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `Multi business cluster should block cross domain message when target disallows it` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `Multi business cluster should block cross domain message when target disallows it` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Akka")]
    public async Task Multi_business_cluster_should_block_cross_domain_message_when_target_disallows_it()
    {
        var options = BuildMultiBusinessOptions();
        options.Domains["Inventory"].AllowCrossDomainMessages = false;
        var module = BuildModuleFromOptions(options, new IAkkaMessageAuthorizationHandler[] { new DomainPolicyAuthorizationHandler(options) });

        await module.Service.StartAsync();
        try
        {
            var completion = NewCompletion<string>();

            Assert.Throws<UnauthorizedAccessException>(() =>
                module.Service.Tell("inventory-worker", new CrossDomainCompleteMessage("Order", "blocked", completion)));
        }
        finally
        {
            await module.Service.StopAsync();
        }
    }

    /// <summary>
    /// <para>zh-cn:验证 Akka.Cluster 模块的 `Multi business cluster should allow cross domain message when target allows it` 场景，确保该边界下的配置、注册或运行时行为符合框架契约。</para>
    /// <para>en-us:Verifies the Akka.Cluster module `Multi business cluster should allow cross domain message when target allows it` scenario, ensuring the configuration, registration, or runtime behavior matches the framework contract for this boundary.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "Akka")]
    public async Task Multi_business_cluster_should_allow_cross_domain_message_when_target_allows_it()
    {
        var options = BuildMultiBusinessOptions();
        options.Domains["Inventory"].AllowCrossDomainMessages = true;
        var module = BuildModuleFromOptions(options, new IAkkaMessageAuthorizationHandler[] { new DomainPolicyAuthorizationHandler(options) });

        await module.Service.StartAsync();
        try
        {
            var completion = NewCompletion<string>();

            module.Service.Tell("inventory-worker", new CrossDomainCompleteMessage("Order", "allowed", completion));

            Assert.Equal("allowed", await WithTimeout(completion.Task));
        }
        finally
        {
            await module.Service.StopAsync();
        }
    }

    private static ModuleRuntime BuildModule(
        Action<AkkaSettingsOptions>? configureOptions = null,
        IEnumerable<IAkkaMessageAuthorizationHandler>? handlers = null)
    {
        var options = new AkkaSettingsOptions
        {
            SystemName = "AirCloudAkkaTest" + Guid.NewGuid().ToString("N"),
            Host = "127.0.0.1",
            Port = GetFreeTcpPort(),
            Roles = new List<string> { "akka-integration" },
            Domains = new Dictionary<string, AkkaDomainOptions>(StringComparer.OrdinalIgnoreCase)
            {
                ["Integration"] = new AkkaDomainOptions { ActorNamePrefix = "it" }
            },
            AskTimeoutSeconds = 3,
            ShutdownTimeoutSeconds = 5
        };

        configureOptions?.Invoke(options);

        var registry = new AkkaActorRegistry();
        var service = new AkkaClusterService(
            Options.Create(options),
            registry,
            handlers ?? new IAkkaMessageAuthorizationHandler[] { new DefaultAkkaMessageAuthorizationHandler() });

        return new ModuleRuntime(service, registry, options);
    }

    private static ModuleRuntime BuildMultiBusinessModule()
    {
        return BuildModuleFromOptions(BuildMultiBusinessOptions());
    }

    private static ModuleRuntime BuildModuleFromOptions(
        AkkaSettingsOptions options,
        IEnumerable<IAkkaMessageAuthorizationHandler>? handlers = null)
    {
        var registry = new AkkaActorRegistry();
        var service = new AkkaClusterService(
            Options.Create(options),
            registry,
            handlers ?? new IAkkaMessageAuthorizationHandler[] { new DefaultAkkaMessageAuthorizationHandler() });

        return new ModuleRuntime(service, registry, options);
    }

    private static AkkaSettingsOptions BuildMultiBusinessOptions()
    {
        return new AkkaSettingsOptions
        {
            SystemName = "AirCloudAkkaBusinessTest" + Guid.NewGuid().ToString("N"),
            Host = "127.0.0.1",
            Port = GetFreeTcpPort(),
            Roles = new List<string> { "order", "inventory" },
            Domains = new Dictionary<string, AkkaDomainOptions>(StringComparer.OrdinalIgnoreCase)
            {
                ["Order"] = new AkkaDomainOptions { ActorNamePrefix = "order", AllowCrossDomainMessages = true },
                ["Inventory"] = new AkkaDomainOptions { ActorNamePrefix = "inventory", AllowCrossDomainMessages = false },
                ["Payment"] = new AkkaDomainOptions { ActorNamePrefix = "payment", AllowCrossDomainMessages = false }
            },
            AskTimeoutSeconds = 3,
            ShutdownTimeoutSeconds = 5
        };
    }

    private static TaskCompletionSource<T> NewCompletion<T>()
    {
        return new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private static async Task<T> WithTimeout<T>(Task<T> task)
    {
        var completed = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(5)));
        if (completed != task)
        {
            throw new TimeoutException("Akka integration test operation timed out.");
        }

        return await task;
    }

    private static async Task<AkkaClusterNodeInfo> WaitForAvailableNodeAsync(IAkkaClusterService service)
    {
        for (var index = 0; index < 30; index++)
        {
            var node = service.GetCurrentNode();
            if (node.IsAvailable)
            {
                return node;
            }

            await Task.Delay(100);
        }

        return service.GetCurrentNode();
    }

    private static int GetFreeTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }

    private sealed record ModuleRuntime(AkkaClusterService Service, AkkaActorRegistry Registry, AkkaSettingsOptions Options);

    public sealed record CompleteMessage(string Value, TaskCompletionSource<string> Completion);

    public sealed record CrossDomainCompleteMessage(string SourceDomain, string Value, TaskCompletionSource<string> Completion);

    public sealed record QueryMessage(string Value);

    public class EchoActor : ReceiveActor
    {
        public EchoActor()
        {
            Receive<CompleteMessage>(message => message.Completion.TrySetResult(message.Value));
            Receive<CrossDomainCompleteMessage>(message => message.Completion.TrySetResult(message.Value));
            Receive<QueryMessage>(message => Sender.Tell(message.Value));
        }
    }

    public sealed class UppercaseActor : ReceiveActor
    {
        public UppercaseActor()
        {
            Receive<QueryMessage>(message => Sender.Tell(message.Value.ToUpperInvariant()));
        }
    }

    [AkkaActor("attributed-echo", Domain = "Integration", Role = "akka-integration")]
    public sealed class AttributedEchoActor : EchoActor
    {
    }

    [AkkaActor("roleless-attributed", Domain = "Integration")]
    public sealed class RolelessAttributedActor : EchoActor
    {
    }

    [AkkaActor("worker", Domain = "Order", Role = "order")]
    public sealed class OrderWorkerActor : EchoActor
    {
    }

    [AkkaActor("worker", Domain = "Inventory", Role = "inventory")]
    public sealed class InventoryWorkerActor : EchoActor
    {
    }

    [AkkaActor("worker", Domain = "Payment", Role = "payment")]
    public sealed class PaymentWorkerActor : EchoActor
    {
    }

    private sealed class DenyAllAuthorizationHandler : IAkkaMessageAuthorizationHandler
    {
        public bool CanSend(AkkaActorDescriptor descriptor, object message)
        {
            return false;
        }
    }

    private sealed class AllowAllAuthorizationHandler : IAkkaMessageAuthorizationHandler
    {
        public bool CanSend(AkkaActorDescriptor descriptor, object message)
        {
            return true;
        }
    }

    private sealed class DomainPolicyAuthorizationHandler : IAkkaMessageAuthorizationHandler
    {
        private readonly AkkaSettingsOptions _options;

        public DomainPolicyAuthorizationHandler(AkkaSettingsOptions options)
        {
            _options = options;
        }

        public bool CanSend(AkkaActorDescriptor descriptor, object message)
        {
            if (message is not CrossDomainCompleteMessage crossDomainMessage)
            {
                return true;
            }

            if (descriptor.Domain.Equals(crossDomainMessage.SourceDomain, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return _options.Domains.TryGetValue(descriptor.Domain, out var domainOptions) &&
                   domainOptions.AllowCrossDomainMessages;
        }
    }
}
