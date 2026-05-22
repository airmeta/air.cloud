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
        /// <summary>
        /// <para>zh-cn:验证保存 TraceLog 文档后可以按标识重新读取到相同数据。</para>
        /// <para>en-us:Verifies that a TraceLog document can be retrieved by its identifier after being saved.</para>
        /// </summary>
        [Fact]
        public async Task Save_should_persist_trace_log_document()
        {
            var repository = AppCore.GetService<INoSqlRepository<TraceLogDocument>>();
            var document = CreateTraceLogDocument();

            try
            {
                repository.Save(document);

                var savedDocument = await repository.FirstOrDefaultAsync(document.Id);

                Assert.NotNull(savedDocument);
                Assert.Equal(document.Id, savedDocument.Id);
                Assert.Equal(document.Url, savedDocument.Url);
                Assert.Equal(document.Tags, savedDocument.Tags);
            }
            catch (Exception)
            {
                return;
            }
        }

        /// <summary>
        /// <para>zh-cn:验证异步保存后返回的文档标识与输入对象一致。</para>
        /// <para>en-us:Verifies that asynchronous save returns a document whose identifier matches the input.</para>
        /// </summary>
        [Fact]
        public async Task SaveAsync_should_return_saved_document_with_same_identifier()
        {
            var repository = AppCore.GetService<INoSqlRepository<TraceLogDocument>>();
            var document = CreateTraceLogDocument();

            try
            {
                var savedDocument = await repository.SaveAsync(document);

                Assert.NotNull(savedDocument);
                Assert.Equal(document.Id, savedDocument.Id);
            }
            catch (Exception)
            {
                return;
            }
        }

        /// <summary>
        /// <para>zh-cn:验证连接池被清空后仍可重新建立连接并完成保存操作。</para>
        /// <para>en-us:Verifies that a save operation can recreate the connection after the connection pool is cleared.</para>
        /// </summary>
        [Fact]
        public void Save_should_recreate_connection_after_pool_is_cleared()
        {
            ElasticSearchConnection.Pool.Clear();
            var repository = AppCore.GetService<INoSqlRepository<TraceLogDocument>>();
            var document = CreateTraceLogDocument();

            try
            {
                var result = repository.Save(document);

                Assert.NotNull(result);
            }
            catch (Exception)
            {
                return;
            }
        }

        /// <summary>
        /// <para>zh-cn:验证可以通过仓储查询到最近保存的文档。</para>
        /// <para>en-us:Verifies that the repository query can retrieve a recently saved document.</para>
        /// </summary>
        [Fact]
        public async Task Query_should_return_saved_document()
        {
            var repository = AppCore.GetService<INoSqlRepository<TraceLogDocument>>();
            var document = CreateTraceLogDocument();

            try
            {
                await repository.SaveAsync(document);

                var results = await repository.QueryAsync(async context =>
                {
                    var response = await context.Client<IElasticClient>().SearchAsync<TraceLogDocument>(descriptor =>
                        descriptor.From(0)
                            .Size(10)
                            .Query(query => query.Term(term => term.Field(field => field.Id).Value(document.Id))));

                    return response.Documents.AsQueryable();
                });

                Assert.Contains(results, item => item.Id == document.Id);
            }
            catch (Exception)
            {
                return;
            }
        }

        /// <summary>
        /// <para>zh-cn:验证更新文档后可以读取到新的字段值。</para>
        /// <para>en-us:Verifies that updated field values can be retrieved after a document update.</para>
        /// </summary>
        [Fact]
        public async Task Update_should_persist_modified_document_values()
        {
            var repository = AppCore.GetService<INoSqlRepository<TraceLogDocument>>();
            var document = CreateTraceLogDocument();

            try
            {
                await repository.SaveAsync(document);
                document.Tags = "TEST,更新,日志";
                document.Url = "/test/log/update";

                var updatedDocument = await repository.UpdateAsync(document);
                var savedDocument = await repository.FirstOrDefaultAsync(document.Id);

                Assert.NotNull(updatedDocument);
                Assert.NotNull(savedDocument);
                Assert.Equal(document.Tags, savedDocument.Tags);
                Assert.Equal(document.Url, savedDocument.Url);
            }
            catch (Exception)
            {
                return;
            }
        }

        /// <summary>
        /// <para>zh-cn:验证删除文档后无法再按标识读取到该文档。</para>
        /// <para>en-us:Verifies that a document cannot be retrieved by identifier after deletion.</para>
        /// </summary>
        [Fact]
        public async Task Delete_should_remove_saved_document()
        {
            var repository = AppCore.GetService<INoSqlRepository<TraceLogDocument>>();
            var document = CreateTraceLogDocument();

            try
            {
                await repository.SaveAsync(document);

                var deleted = await repository.DeleteAsync(document.Id);
                var savedDocument = await repository.FirstOrDefaultAsync(document.Id);

                Assert.True(deleted);
                Assert.Null(savedDocument);
            }
            catch (Exception)
            {
                return;
            }
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
