using Air.Cloud.Core.App;
using Air.Cloud.Core.App.Startups;
using Air.Cloud.Core.Attributes;
using Air.Cloud.Core.Extensions;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

using System.Reflection;

namespace Air.Cloud.UnitTest.Extensions
{
    [CollectionDefinition("AppCoreStartupTests", DisableParallelization = true)]
    public class AppCoreStartupTestsCollection
    {
    }

    [Collection("AppCoreStartupTests")]
    public class AppServiceCollectionExtensionsStartupsTests
    {
        private static readonly object ExecutionSyncRoot = new();
        private static bool ThrowInConstructorEnabled;
        private static bool ThrowInConfigureServicesEnabled;
        /// <summary>
        /// <para>zh-cn:测试依赖优先场景：当 DependType 与 Order 冲突时，必须先执行被依赖的 Startup。</para>
        /// <para>en-us:Tests dependency precedence: when DependType conflicts with Order, the depended Startup must run first.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：构造“First 依赖 Second”的两节点图并调用 AddStartups，断言 ConfigureServices 与存储顺序均为 Second -> First。</para>
        /// <para>en-us:Process: build a two-node graph where First depends on Second, call AddStartups, then assert both execution and stored order are Second -> First.</para>
        /// </remarks>
        [Fact]
        public void AddStartups_should_apply_dependency_order_before_attribute_order()
        {
            var result = ExecuteAddStartups(typeof(DependencyFirstStartup), typeof(DependencySecondStartup));

            Assert.Equal(new[] { nameof(DependencySecondStartup), nameof(DependencyFirstStartup) }, result.ConfigureServicesOrder);
            Assert.Equal(new[] { nameof(DependencySecondStartup), nameof(DependencyFirstStartup) }, result.StoredStartupOrder);
        }

        /// <summary>
        /// <para>zh-cn:测试缺失依赖回退场景：依赖目标不在候选集合时，应退回按 Order 规则排序。</para>
        /// <para>en-us:Tests missing-dependency fallback: when dependency target is absent, ordering should fall back to Order.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：提供一个依赖缺失节点与一个普通节点，执行 AddStartups 后断言按 Order 先后加载与存储。</para>
        /// <para>en-us:Process: provide one startup with missing dependency and one regular startup, run AddStartups, and assert load/store order follows Order.</para>
        /// </remarks>
        [Fact]
        public void AddStartups_should_use_order_when_dependency_target_not_in_scope()
        {
            var result = ExecuteAddStartups(typeof(MissingDependencyStartup), typeof(OrderOnlyStartup));

            Assert.Equal(new[] { nameof(MissingDependencyStartup), nameof(OrderOnlyStartup) }, result.ConfigureServicesOrder);
            Assert.Equal(new[] { nameof(MissingDependencyStartup), nameof(OrderOnlyStartup) }, result.StoredStartupOrder);
        }

        /// <summary>
        /// <para>zh-cn:测试环依赖回退场景：当检测到循环依赖时，排序应退回到稳定顺序而非抛弃节点。</para>
        /// <para>en-us:Tests cycle fallback: when a dependency cycle is detected, ordering should fall back to a stable sequence instead of dropping nodes.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：构造 A↔B 双向依赖，调用 AddStartups 并断言结果为约定的稳定顺序。</para>
        /// <para>en-us:Process: build a bidirectional A↔B dependency, call AddStartups, and assert the agreed stable fallback order.</para>
        /// </remarks>
        [Fact]
        public void AddStartups_should_fallback_to_stable_order_when_cycle_detected()
        {
            var result = ExecuteAddStartups(typeof(CycleStartupA), typeof(CycleStartupB));

            Assert.Equal(new[] { nameof(CycleStartupB), nameof(CycleStartupA) }, result.ConfigureServicesOrder);
            Assert.Equal(new[] { nameof(CycleStartupB), nameof(CycleStartupA) }, result.StoredStartupOrder);
        }

        /// <summary>
        /// <para>zh-cn:测试同 Order 稳定排序场景：同优先级节点应按类型名稳定排序，避免启动抖动。</para>
        /// <para>en-us:Tests stable tie-breaking: nodes with the same Order should be deterministically sorted by type name.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：准备两个 Order 相同但名称不同的 Startup，调用 AddStartups 后断言名称字典序生效。</para>
        /// <para>en-us:Process: prepare two startups with identical Order but different names, run AddStartups, and assert lexical name ordering is applied.</para>
        /// </remarks>
        [Fact]
        public void AddStartups_should_apply_stable_name_order_when_order_is_same()
        {
            var result = ExecuteAddStartups(typeof(SameOrderZStartup), typeof(SameOrderAStartup));

            Assert.Equal(new[] { nameof(SameOrderAStartup), nameof(SameOrderZStartup) }, result.ConfigureServicesOrder);
            Assert.Equal(new[] { nameof(SameOrderAStartup), nameof(SameOrderZStartup) }, result.StoredStartupOrder);
        }

        /// <summary>
        /// <para>zh-cn:测试传递依赖链场景：A 依赖 B、B 依赖 C 时，最终顺序必须满足 C -> B -> A。</para>
        /// <para>en-us:Tests transitive chain ordering: with A depends on B and B depends on C, final order must be C -> B -> A.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：构造三层依赖链并执行 AddStartups，分别校验执行顺序和存储顺序都满足链式拓扑。</para>
        /// <para>en-us:Process: create a three-level dependency chain and run AddStartups, then verify both execution and storage order satisfy chain topology.</para>
        /// </remarks>
        [Fact]
        public void AddStartups_should_apply_transitive_dependency_chain()
        {
            var result = ExecuteAddStartups(typeof(ChainStartupA), typeof(ChainStartupB), typeof(ChainStartupC));

            Assert.Equal(new[] { nameof(ChainStartupC), nameof(ChainStartupB), nameof(ChainStartupA) }, result.ConfigureServicesOrder);
            Assert.Equal(new[] { nameof(ChainStartupC), nameof(ChainStartupB), nameof(ChainStartupA) }, result.StoredStartupOrder);
        }

        /// <summary>
        /// <para>zh-cn:测试默认 Order 场景：未标注 AppStartupAttribute 的类型应按 Order=0 参与排序。</para>
        /// <para>en-us:Tests default Order behavior: a startup without AppStartupAttribute should participate with Order=0.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：提供一个无特性节点与一个负序节点，执行 AddStartups 并断言负序节点先于默认节点。</para>
        /// <para>en-us:Process: provide one no-attribute startup and one negative-order startup, run AddStartups, and assert negative order precedes default order.</para>
        /// </remarks>
        [Fact]
        public void AddStartups_should_treat_startup_without_attribute_as_order_zero()
        {
            var result = ExecuteAddStartups(typeof(NoAttributeStartup), typeof(NegativeOrderStartup));

            Assert.Equal(new[] { nameof(NegativeOrderStartup), nameof(NoAttributeStartup) }, result.ConfigureServicesOrder);
            Assert.Equal(new[] { nameof(NegativeOrderStartup), nameof(NoAttributeStartup) }, result.StoredStartupOrder);
        }

        /// <summary>
        /// <para>zh-cn:测试方法筛选场景：仅允许调用单参数 IServiceCollection 的配置方法，避免误调用重载。</para>
        /// <para>en-us:Tests method-filtering behavior: only single-parameter IServiceCollection configure methods should be invoked.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：构造含合法与非法重载的方法类型，执行 AddStartups 后断言仅合法方法被执行且未触发异常。</para>
        /// <para>en-us:Process: prepare a startup with valid and invalid overloads, run AddStartups, and assert only the valid method executes without exception.</para>
        /// </remarks>
        [Fact]
        public void AddStartups_should_only_invoke_single_parameter_service_method()
        {
            var result = ExecuteAddStartups(typeof(OverloadedMethodStartup));

            Assert.Equal(new[] { nameof(OverloadedMethodStartup) }, result.ConfigureServicesOrder);
            Assert.Equal(new[] { nameof(OverloadedMethodStartup) }, result.StoredStartupOrder);
        }

        /// <summary>
        /// <para>zh-cn:测试混合依赖图稳定化场景：缺失依赖、根节点与叶子节点混合输入时，结果顺序应稳定可复现。</para>
        /// <para>en-us:Tests mixed-graph stabilization: with missing dependencies, roots, and leaves mixed together, final order must remain stable.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：构造五节点混合图并执行 AddStartups，断言输出顺序与预期拓扑及稳定规则一致。</para>
        /// <para>en-us:Process: build a five-node mixed graph, run AddStartups, and assert output order matches expected topology and stability rules.</para>
        /// </remarks>
        [Fact]
        public void AddStartups_should_handle_mixed_graph_with_stable_result()
        {
            var result = ExecuteAddStartups(
                typeof(MixedFeatureBStartup),
                typeof(MixedLeafStartup),
                typeof(MixedRootStartup),
                typeof(MixedMissingDependencyStartup),
                typeof(MixedFeatureAStartup));

            Assert.Equal(
                new[]
                {
                    nameof(MixedMissingDependencyStartup),
                    nameof(MixedRootStartup),
                    nameof(MixedFeatureAStartup),
                    nameof(MixedFeatureBStartup),
                    nameof(MixedLeafStartup)
                },
                result.ConfigureServicesOrder);
            Assert.Equal(result.ConfigureServicesOrder, result.StoredStartupOrder);
        }

        /// <summary>
        /// <para>zh-cn:测试重复类型去重场景：同一 Startup 多次输入时应只注册与执行一次。</para>
        /// <para>en-us:Tests duplicate-type deduplication: repeated input of the same startup should be registered and executed only once.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：传入重复 Startup 类型调用 AddStartups，断言执行序列和存储序列都只包含单个实例。</para>
        /// <para>en-us:Process: pass duplicate startup types to AddStartups and assert both execution and storage sequences contain only one instance.</para>
        /// </remarks>
        [Fact]
        public void AddStartups_should_deduplicate_duplicate_startup_types()
        {
            var result = ExecuteAddStartups(typeof(DuplicateStartup), typeof(DuplicateStartup));

            Assert.Equal(new[] { nameof(DuplicateStartup) }, result.ConfigureServicesOrder);
            Assert.Equal(new[] { nameof(DuplicateStartup) }, result.StoredStartupOrder);
        }

        /// <summary>
        /// <para>zh-cn:测试局部环依赖回退场景：当部分节点成环而其他节点独立时，应回退到全局稳定顺序。</para>
        /// <para>en-us:Tests partial-cycle fallback: when only part of the graph cycles, ordering should fall back to the global stable sequence.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：构造“环 + 独立节点”组合图并执行 AddStartups，断言输出采用约定的全局稳定顺序。</para>
        /// <para>en-us:Process: build a graph with a cycle plus an independent node, run AddStartups, and assert agreed global stable fallback ordering.</para>
        /// </remarks>
        [Fact]
        public void AddStartups_should_fallback_to_global_stable_order_when_cycle_exists_in_partial_graph()
        {
            var result = ExecuteAddStartups(typeof(CycleStartupA), typeof(CycleStartupB), typeof(CycleIndependentStartup));

            Assert.Equal(new[] { nameof(CycleIndependentStartup), nameof(CycleStartupB), nameof(CycleStartupA) }, result.ConfigureServicesOrder);
            Assert.Equal(new[] { nameof(CycleIndependentStartup), nameof(CycleStartupB), nameof(CycleStartupA) }, result.StoredStartupOrder);
        }

        /// <summary>
        /// <para>zh-cn:测试输入排列不变性：同一节点集合即使输入顺序不同，也应产生相同排序结果。</para>
        /// <para>en-us:Tests permutation invariance: the same node set should produce identical ordering despite different input permutations.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：对同一集合给出两种排列执行 AddStartups，比较两次执行与存储顺序必须完全一致。</para>
        /// <para>en-us:Process: run AddStartups with two permutations of the same set and assert both execution and storage orders are identical.</para>
        /// </remarks>
        [Fact]
        public void AddStartups_should_produce_same_order_for_different_input_permutations()
        {
            var first = ExecuteAddStartups(
                typeof(MixedFeatureBStartup),
                typeof(MixedLeafStartup),
                typeof(MixedRootStartup),
                typeof(MixedMissingDependencyStartup),
                typeof(MixedFeatureAStartup));

            var second = ExecuteAddStartups(
                typeof(MixedFeatureAStartup),
                typeof(MixedRootStartup),
                typeof(MixedFeatureBStartup),
                typeof(MixedMissingDependencyStartup),
                typeof(MixedLeafStartup));

            Assert.Equal(first.ConfigureServicesOrder, second.ConfigureServicesOrder);
            Assert.Equal(first.StoredStartupOrder, second.StoredStartupOrder);
        }

        /// <summary>
        /// <para>zh-cn:测试候选过滤场景：抽象类与开放泛型 Startup 必须被过滤，仅可实例化类型参与排序。</para>
        /// <para>en-us:Tests candidate filtering: abstract and open-generic startups must be excluded, leaving only instantiable types.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：输入抽象、开放泛型和普通三类节点，执行 AddStartups 后断言仅普通节点被执行与保存。</para>
        /// <para>en-us:Process: provide abstract, open-generic, and normal nodes, run AddStartups, and assert only the normal node is executed and stored.</para>
        /// </remarks>
        [Fact]
        public void AddStartups_should_skip_abstract_and_open_generic_startups()
        {
            var result = ExecuteAddStartups(typeof(AbstractIgnoredStartup), typeof(GenericIgnoredStartup<>), typeof(NormalIncludedStartup));

            Assert.Equal(new[] { nameof(NormalIncludedStartup) }, result.ConfigureServicesOrder);
            Assert.Equal(new[] { nameof(NormalIncludedStartup) }, result.StoredStartupOrder);
        }

        /// <summary>
        /// <para>zh-cn:测试执行与存储一致性场景：复杂图下 ConfigureServices 执行序列应与 AppStartups 存储序列一致。</para>
        /// <para>en-us:Tests execution-storage consistency: in a complex graph, ConfigureServices order should match AppStartups storage order.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：构造复杂图执行 AddStartups，断言执行顺序与存储顺序完全相同且节点总数符合预期。</para>
        /// <para>en-us:Process: run AddStartups on a complex graph and assert execution order equals storage order with expected node count.</para>
        /// </remarks>
        [Fact]
        public void AddStartups_should_keep_configure_services_and_storage_order_consistent_in_complex_graph()
        {
            var result = ExecuteAddStartups(
                typeof(MixedFeatureBStartup),
                typeof(CycleIndependentStartup),
                typeof(MixedLeafStartup),
                typeof(MixedRootStartup),
                typeof(MixedMissingDependencyStartup),
                typeof(MixedFeatureAStartup));

            Assert.Equal(result.ConfigureServicesOrder, result.StoredStartupOrder);
            Assert.Equal(6, result.ConfigureServicesOrder.Count);
        }

        /// <summary>
        /// <para>zh-cn:测试多轮随机排列稳定性：同一图在大量随机输入顺序下应保持确定性结果。</para>
        /// <para>en-us:Tests stability across many random permutations: the same graph should keep deterministic ordering.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：先生成基准顺序，再进行 100 轮随机打乱并执行 AddStartups，逐轮比对执行与存储顺序一致。</para>
        /// <para>en-us:Process: generate a baseline order, run 100 randomized permutations through AddStartups, and compare execution/storage order in each round.</para>
        /// </remarks>
        [Fact]
        public void AddStartups_should_keep_deterministic_result_under_many_random_permutations()
        {
            var baseline = ExecuteAddStartups(
                typeof(MixedFeatureBStartup),
                typeof(MixedLeafStartup),
                typeof(MixedRootStartup),
                typeof(MixedMissingDependencyStartup),
                typeof(MixedFeatureAStartup));

            var startupTypes = new List<Type>
            {
                typeof(MixedFeatureBStartup),
                typeof(MixedLeafStartup),
                typeof(MixedRootStartup),
                typeof(MixedMissingDependencyStartup),
                typeof(MixedFeatureAStartup)
            };

            for (var seed = 1; seed <= 100; seed++)
            {
                var random = new Random(seed);
                var shuffled = startupTypes
                    .OrderBy(_ => random.Next())
                    .ToArray();

                var current = ExecuteAddStartups(shuffled);

                Assert.Equal(baseline.ConfigureServicesOrder, current.ConfigureServicesOrder);
                Assert.Equal(baseline.StoredStartupOrder, current.StoredStartupOrder);
            }
        }

        /// <summary>
        /// <para>zh-cn:测试大依赖图约束满足场景：在多层依赖网络中，所有关键前后约束都必须被满足。</para>
        /// <para>en-us:Tests large-graph constraint satisfaction: all critical before/after dependency constraints must hold in a multi-layer graph.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：构造 8 节点依赖图执行 AddStartups，逐条断言 Core/Infra/Domain/API/Worker 等关键拓扑关系。</para>
        /// <para>en-us:Process: build an 8-node dependency graph, run AddStartups, and assert each key topology relation among Core/Infra/Domain/API/Worker.</para>
        /// </remarks>
        [Fact]
        public void AddStartups_should_satisfy_all_dependency_constraints_in_large_graph()
        {
            var result = ExecuteAddStartups(
                typeof(BigApiStartup),
                typeof(BigDomainStartup),
                typeof(BigWorkerStartup),
                typeof(BigInfraStartup),
                typeof(BigCacheStartup),
                typeof(BigCoreStartup),
                typeof(BigObserverStartup),
                typeof(BigMetricsStartup));

            Assert.Equal(result.ConfigureServicesOrder, result.StoredStartupOrder);
            Assert.Equal(8, result.ConfigureServicesOrder.Count);

            AssertBefore(result.ConfigureServicesOrder, nameof(BigCoreStartup), nameof(BigInfraStartup));
            AssertBefore(result.ConfigureServicesOrder, nameof(BigCoreStartup), nameof(BigCacheStartup));
            AssertBefore(result.ConfigureServicesOrder, nameof(BigCoreStartup), nameof(BigMetricsStartup));
            AssertBefore(result.ConfigureServicesOrder, nameof(BigInfraStartup), nameof(BigDomainStartup));
            AssertBefore(result.ConfigureServicesOrder, nameof(BigDomainStartup), nameof(BigApiStartup));
            AssertBefore(result.ConfigureServicesOrder, nameof(BigCacheStartup), nameof(BigWorkerStartup));
            AssertBefore(result.ConfigureServicesOrder, nameof(BigWorkerStartup), nameof(BigObserverStartup));
        }

        /// <summary>
        /// <para>zh-cn:测试全量过滤边界：当输入全部为不可实例化候选时，结果应为空且不产生副作用。</para>
        /// <para>en-us:Tests all-filtered boundary: when all inputs are non-instantiable candidates, result should be empty with no side effects.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：仅输入抽象与开放泛型节点，执行 AddStartups 后断言执行序列和存储序列均为空。</para>
        /// <para>en-us:Process: pass only abstract and open-generic nodes to AddStartups and assert both execution and storage sequences are empty.</para>
        /// </remarks>
        [Fact]
        public void AddStartups_should_return_empty_when_all_candidates_are_filtered_out()
        {
            var result = ExecuteAddStartups(typeof(AbstractIgnoredStartup), typeof(GenericIgnoredStartup<>));

            Assert.Empty(result.ConfigureServicesOrder);
            Assert.Empty(result.StoredStartupOrder);
        }

        /// <summary>
        /// <para>zh-cn:测试同父依赖兄弟节点排序：同一依赖父节点下应先按 Order，再按名称稳定排序。</para>
        /// <para>en-us:Tests sibling ordering under same parent dependency: sort by Order first, then stable type name ordering.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：构造一个根节点和三个同父子节点，执行 AddStartups 后断言兄弟节点按 Order/Name 规则排列。</para>
        /// <para>en-us:Process: build one root with three sibling children, run AddStartups, and assert siblings are ordered by Order then Name.</para>
        /// </remarks>
        [Fact]
        public void AddStartups_should_sort_siblings_by_order_then_name_under_same_dependency()
        {
            var result = ExecuteAddStartups(
                typeof(SiblingByNameZStartup),
                typeof(SiblingByOrderStartup),
                typeof(SiblingByNameAStartup),
                typeof(SiblingRootStartup));

            Assert.Equal(
                new[]
                {
                    nameof(SiblingRootStartup),
                    nameof(SiblingByOrderStartup),
                    nameof(SiblingByNameAStartup),
                    nameof(SiblingByNameZStartup)
                },
                result.ConfigureServicesOrder);
            Assert.Equal(result.ConfigureServicesOrder, result.StoredStartupOrder);
        }

        /// <summary>
        /// <para>zh-cn:测试无效签名忽略策略：返回值非 void 或首参非 IServiceCollection 的方法必须被忽略。</para>
        /// <para>en-us:Tests invalid-signature ignore policy: methods with non-void return or wrong first parameter must be ignored.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：提供包含两种非法签名重载的 Startup，执行 AddStartups 后断言仅合法路径生效。</para>
        /// <para>en-us:Process: use a startup containing two invalid overload signatures, run AddStartups, and assert only the valid path is applied.</para>
        /// </remarks>
        [Fact]
        public void AddStartups_should_ignore_non_void_or_non_first_parameter_service_methods()
        {
            var result = ExecuteAddStartups(typeof(InvalidServiceMethodStartup));

            Assert.Equal(new[] { nameof(InvalidServiceMethodStartup) }, result.ConfigureServicesOrder);
            Assert.Equal(new[] { nameof(InvalidServiceMethodStartup) }, result.StoredStartupOrder);
        }

        /// <summary>
        /// <para>zh-cn:测试“重复 + 过滤”混合输入确定性：存在重复类型和被过滤类型时仍应产出稳定结果。</para>
        /// <para>en-us:Tests determinism with mixed duplicate-and-filtered inputs: stable ordering must hold even with duplicates and filtered types.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：构造含重复、抽象、泛型与有效节点的候选集，随机多轮执行 AddStartups 并与基准顺序比较。</para>
        /// <para>en-us:Process: build candidates with duplicates, abstract/generic, and valid nodes, run multiple randomized rounds, and compare to baseline order.</para>
        /// </remarks>
        [Fact]
        public void AddStartups_should_keep_deterministic_result_with_duplicates_and_filtered_types()
        {
            var baseline = ExecuteAddStartups(
                typeof(MixedRootStartup),
                typeof(MixedFeatureAStartup),
                typeof(MixedFeatureBStartup),
                typeof(MixedLeafStartup),
                typeof(MixedMissingDependencyStartup));

            var random = new Random(2026);
            var candidates = new List<Type>
            {
                typeof(MixedFeatureBStartup),
                typeof(AbstractIgnoredStartup),
                typeof(MixedLeafStartup),
                typeof(MixedRootStartup),
                typeof(GenericIgnoredStartup<>),
                typeof(MixedFeatureAStartup),
                typeof(MixedMissingDependencyStartup),
                typeof(MixedFeatureBStartup),
                typeof(MixedRootStartup)
            };

            for (var i = 0; i < 60; i++)
            {
                var shuffled = candidates.OrderBy(_ => random.Next()).ToArray();
                var current = ExecuteAddStartups(shuffled);

                Assert.Equal(baseline.ConfigureServicesOrder, current.ConfigureServicesOrder);
                Assert.Equal(baseline.StoredStartupOrder, current.StoredStartupOrder);
            }
        }

        /// <summary>
        /// <para>zh-cn:测试构造异常透出场景：Startup 构造函数失败时应能被测试捕获并保留错误语义。</para>
        /// <para>en-us:Tests constructor-exception propagation: startup constructor failures should be captured with preserved error semantics.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：启用构造异常开关后执行 AddStartups，解包异常并断言消息与预期一致。</para>
        /// <para>en-us:Process: enable constructor-throw switch, run AddStartups, unwrap the exception, and assert the expected error message.</para>
        /// </remarks>
        [Fact]
        public void AddStartups_should_throw_when_startup_constructor_throws()
        {
            ThrowInConstructorEnabled = true;
            try
            {
                var exception = Record.Exception(() => ExecuteAddStartups(typeof(ConstructorThrowStartup)));
                var actual = UnwrapInvalidOperation(exception);

                Assert.NotNull(actual);
                Assert.Equal("Constructor failed for startup stress test.", actual!.Message);
            }
            finally
            {
                ThrowInConstructorEnabled = false;
            }
        }

        /// <summary>
        /// <para>zh-cn:测试配置阶段异常透出场景：ConfigureServices 内部抛错应被准确捕获并可定位。</para>
        /// <para>en-us:Tests ConfigureServices exception propagation: failures inside ConfigureServices should be accurately captured and traceable.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：启用配置异常开关执行 AddStartups，捕获并解包异常后校验错误消息。</para>
        /// <para>en-us:Process: enable configure-throw switch, run AddStartups, capture/unpack the exception, and verify error message.</para>
        /// </remarks>
        [Fact]
        public void AddStartups_should_throw_when_configure_services_throws()
        {
            ThrowInConfigureServicesEnabled = true;
            try
            {
                var exception = Record.Exception(() => ExecuteAddStartups(typeof(ConfigureServicesThrowStartup)));
                var actual = UnwrapInvalidOperation(exception);

                Assert.NotNull(actual);
                Assert.Equal("ConfigureServices failed for startup stress test.", actual!.Message);
            }
            finally
            {
                ThrowInConfigureServicesEnabled = false;
            }
        }

        /// <summary>
        /// <para>zh-cn:测试空输入边界：当没有任何 Startup 候选时，应返回空序列且不污染全局状态。</para>
        /// <para>en-us:Tests empty-input boundary: with no startup candidates, result should be empty without polluting global state.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：以空参数调用 AddStartups，断言执行/存储序列均为空。</para>
        /// <para>en-us:Process: invoke AddStartups with empty input and assert both execution and storage sequences are empty.</para>
        /// </remarks>
        [Fact]
        public void AddStartups_should_return_empty_for_empty_input()
        {
            var result = ExecuteAddStartups();

            Assert.Empty(result.ConfigureServicesOrder);
            Assert.Empty(result.StoredStartupOrder);
        }

        /// <summary>
        /// <para>zh-cn:测试并发隔离场景：并发多次调用 AddStartups 时，每轮结果应一致且无共享状态串扰。</para>
        /// <para>en-us:Tests concurrency isolation: under concurrent AddStartups calls, each run should be consistent with no shared-state interference.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：并发执行 80 轮两种输入排列，收集异常与快照，最终断言无失败且全部顺序等于基准。</para>
        /// <para>en-us:Process: execute 80 parallel rounds with two permutations, collect failures and snapshots, then assert no failures and all orders equal baseline.</para>
        /// </remarks>
        [Fact]
        public void AddStartups_should_keep_isolation_under_concurrent_invocations()
        {
            var baseline = ExecuteAddStartups(
                typeof(MixedFeatureBStartup),
                typeof(MixedLeafStartup),
                typeof(MixedRootStartup),
                typeof(MixedMissingDependencyStartup),
                typeof(MixedFeatureAStartup));

            var failures = new List<Exception>();
            var snapshots = new List<IReadOnlyList<string>>();
            var syncRoot = new object();

            Parallel.For(0, 80, i =>
            {
                try
                {
                    var result = (i % 2) == 0
                        ? ExecuteAddStartups(typeof(MixedFeatureAStartup), typeof(MixedRootStartup), typeof(MixedFeatureBStartup), typeof(MixedMissingDependencyStartup), typeof(MixedLeafStartup))
                        : ExecuteAddStartups(typeof(MixedFeatureBStartup), typeof(MixedLeafStartup), typeof(MixedRootStartup), typeof(MixedMissingDependencyStartup), typeof(MixedFeatureAStartup));

                    Assert.Equal(result.ConfigureServicesOrder, result.StoredStartupOrder);

                    lock (syncRoot)
                    {
                        snapshots.Add(result.ConfigureServicesOrder);
                    }
                }
                catch (Exception ex)
                {
                    lock (syncRoot)
                    {
                        failures.Add(ex);
                    }
                }
            });

            Assert.Empty(failures);
            Assert.Equal(80, snapshots.Count);
            Assert.All(snapshots, order => Assert.Equal(baseline.ConfigureServicesOrder, order));
        }

        /// <summary>
        /// <para>zh-cn:测试更大依赖图随机化场景：十节点拓扑在多轮随机输入下仍需满足全部依赖约束。</para>
        /// <para>en-us:Tests larger randomized dependency graph: a ten-node topology must satisfy all dependency constraints under repeated random permutations.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：构造十节点集合，执行 120 轮随机打乱并调用 AddStartups，逐轮校验关键前后约束与执行/存储一致性。</para>
        /// <para>en-us:Process: build a ten-node set, run 120 randomized permutations through AddStartups, and verify key precedence constraints plus execution/storage consistency each round.</para>
        /// </remarks>
        [Fact]
        public void AddStartups_should_keep_constraints_in_ten_node_graph_under_many_random_permutations()
        {
            var startupTypes = new List<Type>
            {
                typeof(BigApiStartup),
                typeof(BigDomainStartup),
                typeof(BigWorkerStartup),
                typeof(BigInfraStartup),
                typeof(BigCacheStartup),
                typeof(BigCoreStartup),
                typeof(BigObserverStartup),
                typeof(BigMetricsStartup),
                typeof(BigAuditStartup),
                typeof(BigAlertStartup)
            };

            var random = new Random(4096);
            for (var i = 0; i < 120; i++)
            {
                var shuffled = startupTypes.OrderBy(_ => random.Next()).ToArray();
                var result = ExecuteAddStartups(shuffled);

                Assert.Equal(result.ConfigureServicesOrder, result.StoredStartupOrder);
                AssertTenNodeGraphConstraints(result.ConfigureServicesOrder);
            }
        }

        /// <summary>
        /// <para>zh-cn:测试异常后恢复场景：一次失败执行后，后续正常输入仍应得到正确且稳定的排序结果。</para>
        /// <para>en-us:Tests post-failure recovery: after one failing run, subsequent normal input should still produce correct and stable ordering.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：先触发 ConfigureServices 异常并确认可捕获，再关闭异常开关执行正常图并断言顺序恢复正确。</para>
        /// <para>en-us:Process: trigger and capture ConfigureServices exception first, then disable it and run a normal graph to assert ordering recovery.</para>
        /// </remarks>
        [Fact]
        public void AddStartups_should_recover_and_keep_order_after_exception_case()
        {
            ThrowInConfigureServicesEnabled = true;
            try
            {
                var exception = Record.Exception(() => ExecuteAddStartups(typeof(ConfigureServicesThrowStartup)));
                var actual = UnwrapInvalidOperation(exception);

                Assert.NotNull(actual);
            }
            finally
            {
                ThrowInConfigureServicesEnabled = false;
            }

            var result = ExecuteAddStartups(
                typeof(MixedFeatureBStartup),
                typeof(MixedLeafStartup),
                typeof(MixedRootStartup),
                typeof(MixedMissingDependencyStartup),
                typeof(MixedFeatureAStartup));

            Assert.Equal(
                new[]
                {
                    nameof(MixedMissingDependencyStartup),
                    nameof(MixedRootStartup),
                    nameof(MixedFeatureAStartup),
                    nameof(MixedFeatureBStartup),
                    nameof(MixedLeafStartup)
                },
                result.ConfigureServicesOrder);
            Assert.Equal(result.ConfigureServicesOrder, result.StoredStartupOrder);
        }

        private static void AssertBefore(IReadOnlyList<string> order, string first, string second)
        {
            var firstIndex = order.ToList().IndexOf(first);
            var secondIndex = order.ToList().IndexOf(second);

            Assert.True(firstIndex >= 0, $"{first} was not found in startup order.");
            Assert.True(secondIndex >= 0, $"{second} was not found in startup order.");
            Assert.True(firstIndex < secondIndex, $"Expected {first} before {second}, but order was: {string.Join(",", order)}");
        }

        private static InvalidOperationException? UnwrapInvalidOperation(Exception? exception)
        {
            var current = exception;
            while (current != null)
            {
                if (current is InvalidOperationException invalidOperationException)
                {
                    return invalidOperationException;
                }

                current = current.InnerException;
            }

            return null;
        }

        private static void AssertTenNodeGraphConstraints(IReadOnlyList<string> order)
        {
            Assert.Equal(10, order.Count);
            AssertBefore(order, nameof(BigCoreStartup), nameof(BigInfraStartup));
            AssertBefore(order, nameof(BigCoreStartup), nameof(BigCacheStartup));
            AssertBefore(order, nameof(BigCoreStartup), nameof(BigMetricsStartup));
            AssertBefore(order, nameof(BigInfraStartup), nameof(BigDomainStartup));
            AssertBefore(order, nameof(BigDomainStartup), nameof(BigApiStartup));
            AssertBefore(order, nameof(BigApiStartup), nameof(BigAuditStartup));
            AssertBefore(order, nameof(BigMetricsStartup), nameof(BigAlertStartup));
            AssertBefore(order, nameof(BigCacheStartup), nameof(BigWorkerStartup));
            AssertBefore(order, nameof(BigWorkerStartup), nameof(BigObserverStartup));
        }

        private static (IReadOnlyList<string> ConfigureServicesOrder, IReadOnlyList<string> StoredStartupOrder) ExecuteAddStartups(params Type[] startupTypes)
        {
            lock (ExecutionSyncRoot)
            {
                var previousCrucialTypes = AppCore.CrucialTypes;
                var previousStartups = AppCore.AppStartups;
                StartupExecutionRecorder.Clear();

                try
                {
                    AppCore.CrucialTypes = startupTypes;
                    AppCore.AppStartups = new List<AppStartup>();

                    var services = new ServiceCollection();
                    var method = typeof(AppServiceCollectionExtensions)
                        .GetMethod("AddStartups", BindingFlags.Static | BindingFlags.NonPublic);

                    Assert.NotNull(method);
                    _ = method.Invoke(null, new object[] { services });

                    var storedOrder = AppCore.AppStartups.Select(s => s.GetType().Name).ToList();
                    return (StartupExecutionRecorder.ConfigureServicesOrder.ToList(), storedOrder);
                }
                finally
                {
                    AppCore.CrucialTypes = previousCrucialTypes;
                    AppCore.AppStartups = previousStartups;
                    StartupExecutionRecorder.Clear();
                }
            }
        }

        private static class StartupExecutionRecorder
        {
            public static readonly List<string> ConfigureServicesOrder = new();

            public static void Clear()
            {
                ConfigureServicesOrder.Clear();
            }
        }

        [AppStartup(Order = 1, DependType = typeof(DependencySecondStartup))]
        private sealed class DependencyFirstStartup : AppStartup
        {
            public override void ConfigureServices(IServiceCollection services)
            {
                StartupExecutionRecorder.ConfigureServicesOrder.Add(nameof(DependencyFirstStartup));
            }

            public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            {
            }
        }

        [AppStartup(Order = 100)]
        private sealed class DependencySecondStartup : AppStartup
        {
            public override void ConfigureServices(IServiceCollection services)
            {
                StartupExecutionRecorder.ConfigureServicesOrder.Add(nameof(DependencySecondStartup));
            }

            public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            {
            }
        }

        [AppStartup(Order = 1, DependType = typeof(NotIncludedDependencyStartup))]
        private sealed class MissingDependencyStartup : AppStartup
        {
            public override void ConfigureServices(IServiceCollection services)
            {
                StartupExecutionRecorder.ConfigureServicesOrder.Add(nameof(MissingDependencyStartup));
            }

            public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            {
            }
        }

        [AppStartup(Order = 2)]
        private sealed class OrderOnlyStartup : AppStartup
        {
            public override void ConfigureServices(IServiceCollection services)
            {
                StartupExecutionRecorder.ConfigureServicesOrder.Add(nameof(OrderOnlyStartup));
            }

            public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            {
            }
        }

        private sealed class NotIncludedDependencyStartup : AppStartup
        {
            public override void ConfigureServices(IServiceCollection services)
            {
            }

            public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            {
            }
        }

        [AppStartup(Order = 2, DependType = typeof(CycleStartupB))]
        private sealed class CycleStartupA : AppStartup
        {
            public override void ConfigureServices(IServiceCollection services)
            {
                StartupExecutionRecorder.ConfigureServicesOrder.Add(nameof(CycleStartupA));
            }

            public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            {
            }
        }

        [AppStartup(Order = 1, DependType = typeof(CycleStartupA))]
        private sealed class CycleStartupB : AppStartup
        {
            public override void ConfigureServices(IServiceCollection services)
            {
                StartupExecutionRecorder.ConfigureServicesOrder.Add(nameof(CycleStartupB));
            }

            public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            {
            }
        }

        [AppStartup(Order = 10)]
        private sealed class SameOrderZStartup : AppStartup
        {
            public override void ConfigureServices(IServiceCollection services)
            {
                StartupExecutionRecorder.ConfigureServicesOrder.Add(nameof(SameOrderZStartup));
            }

            public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            {
            }
        }

        [AppStartup(Order = 10)]
        private sealed class SameOrderAStartup : AppStartup
        {
            public override void ConfigureServices(IServiceCollection services)
            {
                StartupExecutionRecorder.ConfigureServicesOrder.Add(nameof(SameOrderAStartup));
            }

            public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            {
            }
        }

        [AppStartup(Order = 3, DependType = typeof(ChainStartupB))]
        private sealed class ChainStartupA : AppStartup
        {
            public override void ConfigureServices(IServiceCollection services)
            {
                StartupExecutionRecorder.ConfigureServicesOrder.Add(nameof(ChainStartupA));
            }

            public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            {
            }
        }

        [AppStartup(Order = 2, DependType = typeof(ChainStartupC))]
        private sealed class ChainStartupB : AppStartup
        {
            public override void ConfigureServices(IServiceCollection services)
            {
                StartupExecutionRecorder.ConfigureServicesOrder.Add(nameof(ChainStartupB));
            }

            public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            {
            }
        }

        [AppStartup(Order = 1)]
        private sealed class ChainStartupC : AppStartup
        {
            public override void ConfigureServices(IServiceCollection services)
            {
                StartupExecutionRecorder.ConfigureServicesOrder.Add(nameof(ChainStartupC));
            }

            public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            {
            }
        }

        private sealed class NoAttributeStartup : AppStartup
        {
            public override void ConfigureServices(IServiceCollection services)
            {
                StartupExecutionRecorder.ConfigureServicesOrder.Add(nameof(NoAttributeStartup));
            }

            public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            {
            }
        }

        [AppStartup(Order = -1)]
        private sealed class NegativeOrderStartup : AppStartup
        {
            public override void ConfigureServices(IServiceCollection services)
            {
                StartupExecutionRecorder.ConfigureServicesOrder.Add(nameof(NegativeOrderStartup));
            }

            public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            {
            }
        }

        private sealed class OverloadedMethodStartup : AppStartup
        {
            public override void ConfigureServices(IServiceCollection services)
            {
                StartupExecutionRecorder.ConfigureServicesOrder.Add(nameof(OverloadedMethodStartup));
            }

            public void ConfigureServices(IServiceCollection services, string marker)
            {
                throw new InvalidOperationException("This overload must not be invoked by AddStartups.");
            }

            public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            {
            }
        }

        [AppStartup(Order = 0)]
        private sealed class MixedRootStartup : AppStartup
        {
            public override void ConfigureServices(IServiceCollection services)
            {
                StartupExecutionRecorder.ConfigureServicesOrder.Add(nameof(MixedRootStartup));
            }

            public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            {
            }
        }

        [AppStartup(Order = 5, DependType = typeof(MixedRootStartup))]
        private sealed class MixedFeatureAStartup : AppStartup
        {
            public override void ConfigureServices(IServiceCollection services)
            {
                StartupExecutionRecorder.ConfigureServicesOrder.Add(nameof(MixedFeatureAStartup));
            }

            public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            {
            }
        }

        [AppStartup(Order = 5, DependType = typeof(MixedRootStartup))]
        private sealed class MixedFeatureBStartup : AppStartup
        {
            public override void ConfigureServices(IServiceCollection services)
            {
                StartupExecutionRecorder.ConfigureServicesOrder.Add(nameof(MixedFeatureBStartup));
            }

            public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            {
            }
        }

        [AppStartup(Order = 1, DependType = typeof(MixedFeatureAStartup))]
        private sealed class MixedLeafStartup : AppStartup
        {
            public override void ConfigureServices(IServiceCollection services)
            {
                StartupExecutionRecorder.ConfigureServicesOrder.Add(nameof(MixedLeafStartup));
            }

            public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            {
            }
        }

        [AppStartup(Order = -2, DependType = typeof(NotIncludedDependencyStartup))]
        private sealed class MixedMissingDependencyStartup : AppStartup
        {
            public override void ConfigureServices(IServiceCollection services)
            {
                StartupExecutionRecorder.ConfigureServicesOrder.Add(nameof(MixedMissingDependencyStartup));
            }

            public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            {
            }
        }

        [AppStartup(Order = 6)]
        private sealed class DuplicateStartup : AppStartup
        {
            public override void ConfigureServices(IServiceCollection services)
            {
                StartupExecutionRecorder.ConfigureServicesOrder.Add(nameof(DuplicateStartup));
            }

            public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            {
            }
        }

        [AppStartup(Order = 0)]
        private sealed class CycleIndependentStartup : AppStartup
        {
            public override void ConfigureServices(IServiceCollection services)
            {
                StartupExecutionRecorder.ConfigureServicesOrder.Add(nameof(CycleIndependentStartup));
            }

            public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            {
            }
        }

        private abstract class AbstractIgnoredStartup : AppStartup
        {
            public override void ConfigureServices(IServiceCollection services)
            {
                StartupExecutionRecorder.ConfigureServicesOrder.Add(nameof(AbstractIgnoredStartup));
            }

            public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            {
            }
        }

        private sealed class GenericIgnoredStartup<T> : AppStartup
        {
            public override void ConfigureServices(IServiceCollection services)
            {
                StartupExecutionRecorder.ConfigureServicesOrder.Add(nameof(GenericIgnoredStartup<T>));
            }

            public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            {
            }
        }

        [AppStartup(Order = 1)]
        private sealed class NormalIncludedStartup : AppStartup
        {
            public override void ConfigureServices(IServiceCollection services)
            {
                StartupExecutionRecorder.ConfigureServicesOrder.Add(nameof(NormalIncludedStartup));
            }

            public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            {
            }
        }

        [AppStartup(Order = 0)]
        private sealed class BigCoreStartup : AppStartup
        {
            public override void ConfigureServices(IServiceCollection services)
            {
                StartupExecutionRecorder.ConfigureServicesOrder.Add(nameof(BigCoreStartup));
            }

            public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            {
            }
        }

        [AppStartup(Order = 1, DependType = typeof(BigCoreStartup))]
        private sealed class BigInfraStartup : AppStartup
        {
            public override void ConfigureServices(IServiceCollection services)
            {
                StartupExecutionRecorder.ConfigureServicesOrder.Add(nameof(BigInfraStartup));
            }

            public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            {
            }
        }

        [AppStartup(Order = 1, DependType = typeof(BigCoreStartup))]
        private sealed class BigCacheStartup : AppStartup
        {
            public override void ConfigureServices(IServiceCollection services)
            {
                StartupExecutionRecorder.ConfigureServicesOrder.Add(nameof(BigCacheStartup));
            }

            public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            {
            }
        }

        [AppStartup(Order = 2, DependType = typeof(BigInfraStartup))]
        private sealed class BigDomainStartup : AppStartup
        {
            public override void ConfigureServices(IServiceCollection services)
            {
                StartupExecutionRecorder.ConfigureServicesOrder.Add(nameof(BigDomainStartup));
            }

            public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            {
            }
        }

        [AppStartup(Order = 2, DependType = typeof(BigCoreStartup))]
        private sealed class BigMetricsStartup : AppStartup
        {
            public override void ConfigureServices(IServiceCollection services)
            {
                StartupExecutionRecorder.ConfigureServicesOrder.Add(nameof(BigMetricsStartup));
            }

            public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            {
            }
        }

        [AppStartup(Order = 3, DependType = typeof(BigDomainStartup))]
        private sealed class BigApiStartup : AppStartup
        {
            public override void ConfigureServices(IServiceCollection services)
            {
                StartupExecutionRecorder.ConfigureServicesOrder.Add(nameof(BigApiStartup));
            }

            public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            {
            }
        }

        [AppStartup(Order = 3, DependType = typeof(BigCacheStartup))]
        private sealed class BigWorkerStartup : AppStartup
        {
            public override void ConfigureServices(IServiceCollection services)
            {
                StartupExecutionRecorder.ConfigureServicesOrder.Add(nameof(BigWorkerStartup));
            }

            public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            {
            }
        }

        [AppStartup(Order = 4, DependType = typeof(BigWorkerStartup))]
        private sealed class BigObserverStartup : AppStartup
        {
            public override void ConfigureServices(IServiceCollection services)
            {
                StartupExecutionRecorder.ConfigureServicesOrder.Add(nameof(BigObserverStartup));
            }

            public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            {
            }
        }

        [AppStartup(Order = 5, DependType = typeof(BigApiStartup))]
        private sealed class BigAuditStartup : AppStartup
        {
            public override void ConfigureServices(IServiceCollection services)
            {
                StartupExecutionRecorder.ConfigureServicesOrder.Add(nameof(BigAuditStartup));
            }

            public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            {
            }
        }

        [AppStartup(Order = 5, DependType = typeof(BigMetricsStartup))]
        private sealed class BigAlertStartup : AppStartup
        {
            public override void ConfigureServices(IServiceCollection services)
            {
                StartupExecutionRecorder.ConfigureServicesOrder.Add(nameof(BigAlertStartup));
            }

            public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            {
            }
        }

        [AppStartup(Order = 0)]
        private sealed class SiblingRootStartup : AppStartup
        {
            public override void ConfigureServices(IServiceCollection services)
            {
                StartupExecutionRecorder.ConfigureServicesOrder.Add(nameof(SiblingRootStartup));
            }

            public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            {
            }
        }

        [AppStartup(Order = 1, DependType = typeof(SiblingRootStartup))]
        private sealed class SiblingByOrderStartup : AppStartup
        {
            public override void ConfigureServices(IServiceCollection services)
            {
                StartupExecutionRecorder.ConfigureServicesOrder.Add(nameof(SiblingByOrderStartup));
            }

            public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            {
            }
        }

        [AppStartup(Order = 2, DependType = typeof(SiblingRootStartup))]
        private sealed class SiblingByNameAStartup : AppStartup
        {
            public override void ConfigureServices(IServiceCollection services)
            {
                StartupExecutionRecorder.ConfigureServicesOrder.Add(nameof(SiblingByNameAStartup));
            }

            public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            {
            }
        }

        [AppStartup(Order = 2, DependType = typeof(SiblingRootStartup))]
        private sealed class SiblingByNameZStartup : AppStartup
        {
            public override void ConfigureServices(IServiceCollection services)
            {
                StartupExecutionRecorder.ConfigureServicesOrder.Add(nameof(SiblingByNameZStartup));
            }

            public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            {
            }
        }

        private sealed class InvalidServiceMethodStartup : AppStartup
        {
            public override void ConfigureServices(IServiceCollection services)
            {
                StartupExecutionRecorder.ConfigureServicesOrder.Add(nameof(InvalidServiceMethodStartup));
            }

            public int ConfigureServices(string marker, IServiceCollection services)
            {
                throw new InvalidOperationException("This invalid signature must not be invoked by AddStartups.");
            }

            public string ConfigureServices(IServiceCollection services, int level)
            {
                throw new InvalidOperationException("This invalid signature must not be invoked by AddStartups.");
            }

            public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            {
            }
        }

        private sealed class ConstructorThrowStartup : AppStartup
        {
            public ConstructorThrowStartup()
            {
                if (ThrowInConstructorEnabled)
                {
                    throw new InvalidOperationException("Constructor failed for startup stress test.");
                }
            }

            public override void ConfigureServices(IServiceCollection services)
            {
            }

            public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            {
            }
        }

        private sealed class ConfigureServicesThrowStartup : AppStartup
        {
            public override void ConfigureServices(IServiceCollection services)
            {
                if (ThrowInConfigureServicesEnabled)
                {
                    throw new InvalidOperationException("ConfigureServices failed for startup stress test.");
                }
            }

            public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            {
            }
        }
    }
}
