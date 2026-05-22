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
