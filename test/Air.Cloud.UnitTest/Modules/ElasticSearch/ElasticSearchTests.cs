using Air.Cloud.Core.App;
using Air.Cloud.Core.Standard.DataBase.Repositories;
using Air.Cloud.DataBase.ElasticSearch;
using Air.Cloud.UnitTest.Modules.ElasticSearch.Model;
using Nest;

namespace Air.Cloud.UnitTest.Modules.ElasticSearch
{
    /// <summary>
    /// <para>zh-cn:ElasticSearch 集成测试集合。</para>
    /// <para>en-us:Integration test suite for ElasticSearch behaviors.</para>
    /// </summary>
    public class ElasticSearchTests
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
        /// <para>zh-cn:测试异步保存返回值场景，确认 SaveAsync 返回文档与输入文档保持同一标识。</para>
        /// <para>en-us:Tests async save return behavior to ensure SaveAsync result carries the same identifier as input document.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：调用 SaveAsync 保存新文档并断言返回对象不为空且 Id 与输入一致。</para>
        /// <para>en-us:Process: execute SaveAsync for a new document, then assert non-null result and matching Id.</para>
        /// </remarks>
        [Fact]
        public async Task SaveAsync_should_return_saved_document_with_same_identifier()
        {
            var repository = AppCore.GetService<INoSqlRepository<TraceLogDocument>>();
            var document = CreateTraceLogDocument();

            var savedDocument = await ExecuteWithTimeout(() => repository.SaveAsync(document), nameof(SaveAsync_should_return_saved_document_with_same_identifier));

            Assert.NotNull(savedDocument);
            Assert.Equal(document.Id, savedDocument.Id);
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

        /// <summary>
        /// <para>zh-cn:测试仓储检索场景，确认 QueryAsync 可命中刚保存的目标文档。</para>
        /// <para>en-us:Tests repository query retrieval to ensure QueryAsync can find the recently saved target document.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：先保存文档，再按 Id 条件执行查询并断言结果集中包含该文档。</para>
        /// <para>en-us:Process: save document first, query by Id condition, and assert result set contains the document.</para>
        /// </remarks>
        [Fact]
        public async Task Query_should_return_saved_document()
        {
            var repository = AppCore.GetService<INoSqlRepository<TraceLogDocument>>();
            var document = CreateTraceLogDocument();

            await ExecuteWithTimeout(() => repository.SaveAsync(document), nameof(Query_should_return_saved_document));

            var results = await ExecuteWithTimeout(() => repository.QueryAsync(async context =>
            {
                var response = await context.Client<IElasticClient>().SearchAsync<TraceLogDocument>(descriptor =>
                    descriptor.From(0)
                        .Size(10)
                        .Query(query => query.Term(term => term.Field(field => field.Id).Value(document.Id))));

                return response.Documents.AsQueryable();
            }), nameof(Query_should_return_saved_document));
            Assert.Contains(results, item => item.Id == document.Id);
        }

        /// <summary>
        /// <para>zh-cn:测试更新持久化场景，确认 UpdateAsync 后重新读取可获得修改后的字段值。</para>
        /// <para>en-us:Tests update persistence to ensure fields modified by UpdateAsync are retrievable afterward.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：保存文档后修改 Tags 与 Url，执行更新并再次读取，断言新值已持久化。</para>
        /// <para>en-us:Process: save document, modify Tags and Url, update, then read again and assert new values persisted.</para>
        /// </remarks>
        [Fact]
        public async Task Update_should_persist_modified_document_values()
        {
            var repository = AppCore.GetService<INoSqlRepository<TraceLogDocument>>();
            var document = CreateTraceLogDocument();

            await ExecuteWithTimeout(() => repository.SaveAsync(document), nameof(Update_should_persist_modified_document_values));
            document.Tags = "TEST,更新,日志";
            document.Url = "/test/log/update";

            var updatedDocument = await ExecuteWithTimeout(() => repository.UpdateAsync(document), nameof(Update_should_persist_modified_document_values));
            var savedDocument = await ExecuteWithTimeout(() => repository.FirstOrDefaultAsync(document.Id), nameof(Update_should_persist_modified_document_values));

            Assert.NotNull(updatedDocument);
            Assert.NotNull(savedDocument);
            Assert.Equal(document.Tags, savedDocument.Tags);
            Assert.Equal(document.Url, savedDocument.Url);
        }

        /// <summary>
        /// <para>zh-cn:测试删除后不可读场景，确认 DeleteAsync 成功后按 Id 再查询应返回空。</para>
        /// <para>en-us:Tests post-delete unavailability to ensure querying by Id returns null after successful DeleteAsync.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：先保存文档再删除，随后按同一 Id 查询并断言删除结果为 true 且文档为空。</para>
        /// <para>en-us:Process: save document, delete it, then query by same Id and assert delete=true with null document.</para>
        /// </remarks>
        [Fact]
        public async Task Delete_should_remove_saved_document()
        {
            var repository = AppCore.GetService<INoSqlRepository<TraceLogDocument>>();
            var document = CreateTraceLogDocument();

            await ExecuteWithTimeout(() => repository.SaveAsync(document), nameof(Delete_should_remove_saved_document));

            var deleted = await ExecuteWithTimeout(() => repository.DeleteAsync(document.Id), nameof(Delete_should_remove_saved_document));
            var savedDocument = await ExecuteWithTimeout(() => repository.FirstOrDefaultAsync(document.Id), nameof(Delete_should_remove_saved_document));

            Assert.True(deleted);
            Assert.Null(savedDocument);
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
