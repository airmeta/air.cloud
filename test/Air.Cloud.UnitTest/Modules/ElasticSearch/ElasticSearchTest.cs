using Air.Cloud.Core.App;
using Air.Cloud.Core.Standard.DataBase.Repositories;
using Air.Cloud.DataBase.ElasticSearch;
using Air.Cloud.UnitTest.Modules.ElasticSearch.Model;

namespace Air.Cloud.UnitTest.Modules.ElasticSearch
{
    /// <summary>
    /// <para>zh-cn:ElasticSearch 集成测试集合。</para>
    /// <para>en-us:Integration test suite for ElasticSearch behaviors.</para>
    /// </summary>
    public class ElasticSearchTest
    {
        private const int OperationTimeoutMilliseconds = 10_000;

        /// <summary>
        /// <para>zh-cn:测试同步保存闭环场景，确认 Save 后可按文档 Id 读回相同核心字段。</para>
        /// <para>en-us:Tests synchronous save round-trip to ensure Save persists a document retrievable by Id with matching core fields.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：保存 TraceLog 文档后按 Id 查询，断言 Id、Url、Tags 与原文档一致。</para>
        /// <para>en-us:Process: save a TraceLog document, query by Id, and assert Id/Url/Tags equality with original document.</para>
        /// </remarks>
        [Fact]
        public async Task Save_should_persist_trace_log_document()
        {
            var repository = AppCore.GetService<INoSqlRepository<TraceLogDocument>>();
            var document = CreateTraceLogDocument();

            await ExecuteWithTimeout(() => Task.Run(() => repository.Save(document)), nameof(Save_should_persist_trace_log_document));

            var savedDocument = await ExecuteWithTimeout(() => repository.FirstOrDefaultAsync(document.Id), nameof(Save_should_persist_trace_log_document));

            Assert.NotNull(savedDocument);
            Assert.Equal(document.Id, savedDocument.Id);
            Assert.Equal(document.Url, savedDocument.Url);
            Assert.Equal(document.Tags, savedDocument.Tags);
        }

        /// <summary>
        /// <para>zh-cn:测试连接池清空后的恢复场景，确认 Save 可重新建立连接并完成写入。</para>
        /// <para>en-us:Tests post-pool-clear recovery to ensure Save can recreate connection and complete persistence.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：先清空连接池再执行保存操作，断言返回结果非空以验证恢复成功。</para>
        /// <para>en-us:Process: clear connection pool first, then perform save and assert non-null result to verify recovery.</para>
        /// </remarks>
        [Fact]
        public async Task Save_should_recreate_connection_after_pool_is_cleared()
        {
            ElasticSearchConnection.Pool.Clear();
            var repository = AppCore.GetService<INoSqlRepository<TraceLogDocument>>();
            var document = CreateTraceLogDocument();

            var result = await ExecuteWithTimeout(() => Task.Run(() => repository.Save(document)), nameof(Save_should_recreate_connection_after_pool_is_cleared));

            Assert.NotNull(result);
        }

        private static async Task ExecuteWithTimeout(Func<Task> action, string operationName)
        {
            var task = action();
            var completedTask = await Task.WhenAny(task, Task.Delay(OperationTimeoutMilliseconds));
            if (completedTask != task)
            {
                throw new TimeoutException($"Operation '{operationName}' timed out after {OperationTimeoutMilliseconds} ms.");
            }

            await task;
        }

        private static async Task<T> ExecuteWithTimeout<T>(Func<Task<T>> action, string operationName)
        {
            var task = action();
            var completedTask = await Task.WhenAny(task, Task.Delay(OperationTimeoutMilliseconds));
            if (completedTask != task)
            {
                throw new TimeoutException($"Operation '{operationName}' timed out after {OperationTimeoutMilliseconds} ms.");
            }

            return await task;
        }

        /// <summary>
        /// <para>zh-cn:创建用于 ElasticSearch 测试的日志文档对象。</para>
        /// <para>en-us:Creates a log document instance used by the ElasticSearch tests.</para>
        /// </summary>
        /// <returns>
        /// <para>zh-cn:返回一条带有测试标识信息的日志文档。</para>
        /// <para>en-us:Returns a log document populated with test-specific data.</para>
        /// </returns>
        private static TraceLogDocument CreateTraceLogDocument()
        {
            return new TraceLogDocument
            {
                Id = AppCore.Guid(),
                Tags = "TEST,测试,日志",
                Url = "/test/log/create"
            };
        }
    }
}
