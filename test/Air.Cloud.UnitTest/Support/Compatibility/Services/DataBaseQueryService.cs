using Air.Cloud.Core.Standard.KVCenter;
using Air.Cloud.Core.Standard.ServerCenter;
using Air.Cloud.Modules.Consul.Model;
using Air.Cloud.UnitTest.Compatibility.Domains;

namespace Air.Cloud.UnitTest.Compatibility.Services
{
    /// <summary>
    /// <para>zh-cn:用于迁移测试的本地数据库查询服务。</para>
    /// <para>en-us:Local database query service used by migrated tests.</para>
    /// </summary>
    public class DataBaseQueryService
    {
        private readonly ITestDomain domain;
        private readonly IServerCenterStandard serverCenterStandard;
        private readonly IKVCenterStandard kvCenterStandard;

        /// <summary>
        /// <para>zh-cn:创建数据库查询服务实例。</para>
        /// <para>en-us:Creates a database query service instance.</para>
        /// </summary>
        public DataBaseQueryService(ITestDomain domain, IServerCenterStandard serverCenterStandard, IKVCenterStandard kvCenterStandard)
        {
            this.domain = domain;
            this.serverCenterStandard = serverCenterStandard;
            this.kvCenterStandard = kvCenterStandard;
        }

        /// <summary>
        /// <para>zh-cn:按固定标识查询数据。</para>
        /// <para>en-us:Queries data using the fixed identifier.</para>
        /// </summary>
        public object Query()
        {
            return domain.Search("a09cdb089b7f48498090d1f7f11c0e7b");
        }

        /// <summary>
        /// <para>zh-cn:查询并按服务名称排序服务中心结果。</para>
        /// <para>en-us:Queries server center results and orders them by service name.</para>
        /// </summary>
        public async Task<object> Sq()
        {
            var result = (await serverCenterStandard.QueryAsync<ConsulServerCenterServiceOptions>())
                .OrderBy(item => item.ServiceName)
                .ToList();
            return result;
        }

        /// <summary>
        /// <para>zh-cn:查询并按值排序 KV 中心结果。</para>
        /// <para>en-us:Queries KV center results and orders them by value.</para>
        /// </summary>
        public async Task<object> Sq1()
        {
            var result = (await kvCenterStandard.QueryAsync<ConsulKvCenterServiceInformation>())
                .OrderBy(item => item.Value)
                .ToList();
            return result;
        }

        /// <summary>
        /// <para>zh-cn:执行批量插入。</para>
        /// <para>en-us:Executes batch insertion.</para>
        /// </summary>
        public Task<bool> Batch()
        {
            return domain.BatchInsertAsync();
        }
    }
}
