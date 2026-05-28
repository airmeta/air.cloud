using Air.Cloud.Core.Standard.DataBase.Model;
using Air.Cloud.Core.Standard.KVCenter;
using Air.Cloud.Core.Standard.ServerCenter;
using Air.Cloud.Modules.Consul.Model;
using System.Reflection;
using Air.Cloud.UnitTest.Compatibility.Domains;
using Air.Cloud.UnitTest.Compatibility.Services;
using Air.Cloud.UnitTest.Compatibility.Entities;

namespace Air.Cloud.UnitTest.Modules.DataBase
{
    /// <summary>
    /// <para>zh-cn:数据库查询服务行为测试集合。</para>
    /// <para>en-us:Migrated test suite for database query service behaviors.</para>
    /// </summary>
    public class DataBaseQueryServiceTests
    {
        /// <summary>
        /// <para>zh-cn:测试固定标识透传场景，确认 Query 始终以内置 id 调用领域 Search 并返回原始结果引用。</para>
        /// <para>en-us:Tests fixed-identifier forwarding to ensure Query always invokes domain Search with built-in id and returns the original result reference.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：注入校验 id 的领域桩，执行 Query 后断言返回集合引用未变且 LastSearchId 为预期固定值。</para>
        /// <para>en-us:Process: inject a domain stub that validates id, execute Query, then assert collection reference is unchanged and LastSearchId equals expected constant.</para>
        /// </remarks>
        [Fact]
        public void Query_should_call_domain_search_with_fixed_identifier()
        {
            const string expectedId = "a09cdb089b7f48498090d1f7f11c0e7b";
            var expectedEntities = new List<IEntity> { new StubEntity() };
            var domain = new StubTestDomain
            {
                SearchHandler = id =>
                {
                    Assert.Equal(expectedId, id);
                    return expectedEntities;
                }
            };

            var service = CreateService(domain: domain);

            var result = service.Query();

            Assert.Same(expectedEntities, result);
            Assert.Equal(expectedId, domain.LastSearchId);
        }

        /// <summary>
        /// <para>zh-cn:测试服务中心结果排序场景，确认 Sq 会按 ServiceName 升序输出查询结果。</para>
        /// <para>en-us:Tests server-center ordering to ensure Sq returns query results sorted by ServiceName ascending.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：构造乱序服务名列表作为查询返回值，执行 Sq 后断言结果顺序为 a-b-c 且仅调用一次查询。</para>
        /// <para>en-us:Process: provide out-of-order service names, execute Sq, assert a-b-c ordering and single query invocation.</para>
        /// </remarks>
        [Fact]
        public async Task Sq_should_return_server_center_results_ordered_by_service_name()
        {
            var serverCenter = new StubServerCenterStandard
            {
                QueryHandler = () => Task.FromResult<IList<ConsulServerCenterServiceOptions>>(new List<ConsulServerCenterServiceOptions>
                {
                    new() { ServiceName = "service-c" },
                    new() { ServiceName = "service-a" },
                    new() { ServiceName = "service-b" }
                })
            };

            var service = CreateService(serverCenter: serverCenter);

            var result = await service.Sq();
            var ordered = Assert.IsAssignableFrom<IEnumerable<ConsulServerCenterServiceOptions>>(result).ToList();

            Assert.Equal(new[] { "service-a", "service-b", "service-c" }, ordered.Select(item => item.ServiceName));
            Assert.Equal(1, serverCenter.QueryCallCount);
        }

        /// <summary>
        /// <para>zh-cn:测试 KV 结果排序场景，确认 Sq1 会按 Value 升序返回键值中心查询数据。</para>
        /// <para>en-us:Tests KV-center ordering to ensure Sq1 returns items sorted by Value ascending.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：注入乱序 value 列表并执行 Sq1，断言排序结果为 a-b-c 且查询前缀保持默认“/”。</para>
        /// <para>en-us:Process: inject unordered values, execute Sq1, assert a-b-c order and default prefix "/" usage.</para>
        /// </remarks>
        [Fact]
        public async Task Sq1_should_return_kv_center_results_ordered_by_value()
        {
            var kvCenter = new StubKvCenterStandard
            {
                QueryHandler = _ => Task.FromResult<IList<ConsulKvCenterServiceInformation>>(new List<ConsulKvCenterServiceInformation>
                {
                    new() { Value = "value-c" },
                    new() { Value = "value-a" },
                    new() { Value = "value-b" }
                })
            };

            var service = CreateService(kvCenter: kvCenter);

            var result = await service.Sq1();
            var ordered = Assert.IsAssignableFrom<IEnumerable<ConsulKvCenterServiceInformation>>(result).ToList();

            Assert.Equal(new[] { "value-a", "value-b", "value-c" }, ordered.Select(item => item.Value));
            Assert.Equal("/", kvCenter.LastPrefix);
        }

        /// <summary>
        /// <para>zh-cn:测试批量写入结果透传场景，确认 Batch 直接返回领域 BatchInsertAsync 的布尔结果。</para>
        /// <para>en-us:Tests batch-insert result forwarding to ensure Batch directly returns domain BatchInsertAsync boolean result.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：将领域桩的 BatchInsertAsync 固定为 true，执行 Batch 后断言返回 true 且调用计数为 1。</para>
        /// <para>en-us:Process: set domain BatchInsertAsync stub to true, execute Batch, then assert true result and one invocation.</para>
        /// </remarks>
        [Fact]
        public async Task Batch_should_return_domain_batch_insert_result()
        {
            var domain = new StubTestDomain
            {
                BatchInsertAsyncHandler = () => Task.FromResult(true)
            };

            var service = CreateService(domain: domain);

            var result = await service.Batch();

            Assert.True(result);
            Assert.Equal(1, domain.BatchInsertCallCount);
        }

        /// <summary>
        /// <para>zh-cn:创建数据库查询服务测试实例。</para>
        /// <para>en-us:Creates a service instance for the database query service tests.</para>
        /// </summary>
        /// <param name="domain">
        /// <para>zh-cn:领域桩依赖。</para>
        /// <para>en-us:The stub domain dependency.</para>
        /// </param>
        /// <param name="serverCenter">
        /// <para>zh-cn:服务中心桩依赖。</para>
        /// <para>en-us:The stub server center dependency.</para>
        /// </param>
        /// <param name="kvCenter">
        /// <para>zh-cn:键值中心桩依赖。</para>
        /// <para>en-us:The stub key-value center dependency.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:返回待测试的数据库查询服务实例。</para>
        /// <para>en-us:Returns the database query service instance under test.</para>
        /// </returns>
        private static DataBaseQueryService CreateService(
            StubTestDomain? domain = null,
            StubServerCenterStandard? serverCenter = null,
            StubKvCenterStandard? kvCenter = null)
        {
            return new DataBaseQueryService(
                domain ?? new StubTestDomain(),
                serverCenter ?? new StubServerCenterStandard(),
                kvCenter ?? new StubKvCenterStandard());
        }

        /// <summary>
        /// <para>zh-cn:数据库领域最小桩实现。</para>
        /// <para>en-us:Minimal stub implementation for the database domain.</para>
        /// </summary>
        private sealed class StubTestDomain : ITestDomain
        {
            /// <summary>
            /// <para>zh-cn:处理 Search 调用的委托。</para>
            /// <para>en-us:Delegate that handles Search invocations.</para>
            /// </summary>
            public Func<string, IEnumerable<IEntity>> SearchHandler { get; set; } = _ => Array.Empty<IEntity>();

            /// <summary>
            /// <para>zh-cn:处理 BatchInsertAsync 调用的委托。</para>
            /// <para>en-us:Delegate that handles BatchInsertAsync invocations.</para>
            /// </summary>
            public Func<Task<bool>> BatchInsertAsyncHandler { get; set; } = () => Task.FromResult(false);

            /// <summary>
            /// <para>zh-cn:记录最近一次 Search 使用的标识。</para>
            /// <para>en-us:Tracks the identifier used by the latest Search call.</para>
            /// </summary>
            public string? LastSearchId { get; private set; }

            /// <summary>
            /// <para>zh-cn:记录 BatchInsertAsync 的调用次数。</para>
            /// <para>en-us:Tracks how many times BatchInsertAsync was called.</para>
            /// </summary>
            public int BatchInsertCallCount { get; private set; }

            /// <inheritdoc />
            public IEnumerable<IEntity> Search(string id)
            {
                LastSearchId = id;
                return SearchHandler(id);
            }

            /// <inheritdoc />
            public async Task<bool> BatchInsertAsync()
            {
                BatchInsertCallCount++;
                return await BatchInsertAsyncHandler();
            }

            /// <inheritdoc />
            public Task Delete(Test entity, bool FakeDelete = false) => throw new NotSupportedException();

            /// <inheritdoc />
            public Task<bool> Insert(Test entity) => throw new NotSupportedException();

            /// <inheritdoc />
            public (int, List<Test>) Page(System.Linq.Expressions.Expression<Func<Test, bool>>? filter, int Page, int Limit) => throw new NotSupportedException();

            /// <inheritdoc />
            public IEntity Update(IEntity entity) => throw new NotSupportedException();

            /// <inheritdoc />
            public Task<bool> AddAsync<TEntity>(TEntity entity) where TEntity : class, IEntity, new() => throw new NotSupportedException();

            /// <inheritdoc />
            public Task<bool> AddRangeAsync<TEntity>(IEnumerable<TEntity> entitys) where TEntity : class, IEntity, new() => throw new NotSupportedException();

            /// <inheritdoc />
            public bool Delete<TEntity>(string id, bool fakeDelete = true) where TEntity : class, IEntity, new() => throw new NotSupportedException();

            /// <inheritdoc />
            public Task<bool> DeleteAsync<TEntity>(string id, bool fakeDelete = true) where TEntity : class, IEntity, new() => throw new NotSupportedException();

            /// <inheritdoc />
            public bool Delete<TEntity>(TEntity entity, bool fakeDelete = true) where TEntity : class, IEntity, new() => throw new NotSupportedException();

            /// <inheritdoc />
            public Task<bool> DeleteAsync<TEntity>(TEntity entity, bool fakeDelete = true) where TEntity : class, IEntity, new() => throw new NotSupportedException();

            /// <inheritdoc />
            public bool DeleteRange<TEntity>(IEnumerable<TEntity> entitys, bool fakeDelete = true) where TEntity : class, IEntity, new() => throw new NotSupportedException();

            /// <inheritdoc />
            public Task<bool> DeleteRangeAsync<TEntity>(IEnumerable<TEntity> entitys, bool fakeDelete = true) where TEntity : class, IEntity, new() => throw new NotSupportedException();

            /// <inheritdoc />
            public TEntity First<TEntity>(string id) where TEntity : class, IEntity, new() => throw new NotSupportedException();

            /// <inheritdoc />
            public Task<TEntity> FirstAsync<TEntity>(string id) where TEntity : class, IEntity, new() => throw new NotSupportedException();

            /// <inheritdoc />
            public TEntity First<TEntity>(object values) where TEntity : class, IEntity, new() => throw new NotSupportedException();

            /// <inheritdoc />
            public Task<TEntity> FirstAsync<TEntity>(object values) where TEntity : class, IEntity, new() => throw new NotSupportedException();

            /// <inheritdoc />
            public IEnumerable<TEntity> Query<TEntity>(object? values = null) where TEntity : class, IEntity, new() => throw new NotSupportedException();

            /// <inheritdoc />
            public Task<IEnumerable<TEntity>> QueryAsync<TEntity>(object? values = null) where TEntity : class, IEntity, new() => throw new NotSupportedException();

            /// <inheritdoc />
            public (int, List<TEntity>) Page<TEntity>(System.Linq.Expressions.Expression<Func<TEntity, bool>> filter, int page, int limit) where TEntity : class, IEntity, new() => throw new NotSupportedException();

            /// <inheritdoc />
            public Task<(int, List<TEntity>)> PageAsync<TEntity>(System.Linq.Expressions.Expression<Func<TEntity, bool>> filter, int page, int limit) where TEntity : class, IEntity, new() => throw new NotSupportedException();

            /// <inheritdoc />
            public bool Update<TEntity>(TEntity entity) where TEntity : class, IEntity, new() => throw new NotSupportedException();

            /// <inheritdoc />
            public Task<bool> UpdateAsync<TEntity>(TEntity entity) where TEntity : class, IEntity, new() => throw new NotSupportedException();
        }

        /// <summary>
        /// <para>zh-cn:服务中心最小桩实现。</para>
        /// <para>en-us:Minimal stub implementation for the server center.</para>
        /// </summary>
        private sealed class StubServerCenterStandard : IServerCenterStandard
        {
            /// <summary>
            /// <para>zh-cn:处理服务中心查询的委托。</para>
            /// <para>en-us:Delegate that handles server center queries.</para>
            /// </summary>
            public Func<Task<IList<ConsulServerCenterServiceOptions>>> QueryHandler { get; set; } = () => Task.FromResult<IList<ConsulServerCenterServiceOptions>>(new List<ConsulServerCenterServiceOptions>());

            /// <summary>
            /// <para>zh-cn:记录服务中心查询的调用次数。</para>
            /// <para>en-us:Tracks how many times the server center query was invoked.</para>
            /// </summary>
            public int QueryCallCount { get; private set; }

            /// <inheritdoc />
            public async Task<IList<T>> QueryAsync<T>() where T : IServerCenterServiceOptions, new()
            {
                QueryCallCount++;
                var result = await QueryHandler();
                return result.Cast<T>().ToList();
            }

            /// <inheritdoc />
            public Task<object> GetAsync(string Key) => throw new NotSupportedException();

            /// <inheritdoc />
            public Task<bool> RegisterAsync<T>(T serverCenterServiceInformation) where T : class, IServerCenterServiceRegisterOptions, new() => throw new NotSupportedException();
        }

        /// <summary>
        /// <para>zh-cn:键值中心最小桩实现。</para>
        /// <para>en-us:Minimal stub implementation for the key-value center.</para>
        /// </summary>
        private sealed class StubKvCenterStandard : IKVCenterStandard
        {
            /// <summary>
            /// <para>zh-cn:处理 KV 查询的委托。</para>
            /// <para>en-us:Delegate that handles key-value center queries.</para>
            /// </summary>
            public Func<string, Task<IList<ConsulKvCenterServiceInformation>>> QueryHandler { get; set; } = _ => Task.FromResult<IList<ConsulKvCenterServiceInformation>>(new List<ConsulKvCenterServiceInformation>());

            /// <summary>
            /// <para>zh-cn:记录最近一次查询前缀。</para>
            /// <para>en-us:Tracks the prefix used by the latest query.</para>
            /// </summary>
            public string? LastPrefix { get; private set; }

            /// <inheritdoc />
            public async Task<IList<T>> QueryAsync<T>(string Prefix = "/") where T : class, new()
            {
                LastPrefix = Prefix;
                var result = await QueryHandler(Prefix);
                return result.Cast<T>().ToList();
            }

            /// <inheritdoc />
            public Task<bool> AddOrUpdateAsync(string Key, string Value) => throw new NotSupportedException();

            /// <inheritdoc />
            public Task<bool> DeleteAsync(string Key) => throw new NotSupportedException();

            /// <inheritdoc />
            public Task<T> GetAsync<T>(string Key) where T : IKVCenterServiceOptions, new() => throw new NotSupportedException();
        }

        /// <summary>
        /// <para>zh-cn:用于领域查询结果的最小实体桩。</para>
        /// <para>en-us:Minimal entity stub used for domain query results.</para>
        /// </summary>
        private sealed class StubEntity : IEntity
        {
            /// <inheritdoc />
            public string Id { get; set; } = Guid.NewGuid().ToString("N");

            /// <inheritdoc />
            public DateTime CreateTime { get; set; }

            /// <inheritdoc />
            public DateTime UpdateTime { get; set; }

            /// <inheritdoc />
            public bool Deleted { get; set; }
        }
    }
}
