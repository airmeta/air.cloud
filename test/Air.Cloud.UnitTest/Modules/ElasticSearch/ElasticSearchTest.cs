using Air.Cloud.Core.App;
using Air.Cloud.Core.Standard.DataBase.Repositories;
using Air.Cloud.Core.Standard.TraceLog;
using Air.Cloud.DataBase.ElasticSearch;
using Air.Cloud.UnitTest.Modules.ElasticSearch.Model;

namespace Air.Cloud.UnitTest.Modules.ElasticSearch
{
    /// <summary>
    /// <para>zh-cn:ES查询</para>
    /// </summary>
    public  class ElasticSearchTest
    {
        /// <summary>
        /// <para>zh-cn:尝试保存一条数据到ES中</para>
        /// </summary>
        [Fact]
        public void TryGetElasticSearch()
        {
            TraceLogDocument logContent = new TraceLogDocument();
            logContent.Id = AppCore.Guid();
            logContent.Tags = "TEST,测试,日志";
            logContent.Url = "/test/log/create";
            INoSqlRepository<TraceLogDocument> repository = AppCore.GetService<INoSqlRepository<TraceLogDocument>>();
            
            try
            {
                var documents = repository.Save(logContent);

                var Data =  repository.FirstOrDefaultAsync(logContent.Id).GetAwaiter().GetResult();

                Assert.NotNull(Data);

            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }

        }
        /// <summary>
        /// <para>zh-cn:模拟当前切分周期完成后 获取下一个切分周期的数据</para>
        /// </summary>
        [Fact]
        public void TryGetNextElasticSearch()
        {
            //模拟滚动效果 把所有连接信息清空掉 这样就获取不到下一个信息了
            ElasticSearchConnection.Pool.Clear();
            //重新获取

            ITraceLogContent logContent = new TraceLogDocument();

            INoSqlRepository<TraceLogDocument> repository = AppCore.GetService<INoSqlRepository<TraceLogDocument>>();

            try
            {
                var documents = repository.Save(logContent as TraceLogDocument);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }

        }

    }
}
