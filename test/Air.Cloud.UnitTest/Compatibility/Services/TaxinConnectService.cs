using Air.Cloud.Core.Standard.Taxin.Attributes;
using Air.Cloud.Core.Standard.Taxin.Client;
using Microsoft.AspNetCore.Routing;

namespace Air.Cloud.UnitTest.Compatibility.Services
{
    /// <summary>
    /// <para>zh-cn:用于迁移测试的本地 Taxin 连接服务。</para>
    /// <para>en-us:Local Taxin connection service used by migrated tests.</para>
    /// </summary>
    public class TaxinConnectService
    {
        private readonly ITaxinClientStandard? client;

        /// <summary>
        /// <para>zh-cn:创建 Taxin 连接服务实例。</para>
        /// <para>en-us:Creates a Taxin connection service instance.</para>
        /// </summary>
        public TaxinConnectService(ITaxinClientStandard? taxinClient = null)
        {
            client = taxinClient;
        }

        /// <summary>
        /// <para>zh-cn:调用 Taxin 客户端并返回固定结构结果。</para>
        /// <para>en-us:Invokes the Taxin client and returns a fixed payload structure.</para>
        /// </summary>
        public async Task<object> ClientA()
        {
            try
            {
                var data = await client!.SendAsync<object>("taxin.service.test");
                return new { name = "132", data };
            }
            catch (Exception ex)
            {
                return new { name = "132", data = ex.Message };
            }
        }

        /// <summary>
        /// <para>zh-cn:Taxin 服务方法，返回固定名称结果。</para>
        /// <para>en-us:Taxin service method that returns a fixed name payload.</para>
        /// </summary>
        [TaxinService("taxin.service.test")]
        public object ClientB(TaxinResponseModel taxinResponseModel)
        {
            return new { name = "TaxinServiceTest" };
        }
    }

    /// <summary>
    /// <para>zh-cn:用于迁移测试的 Taxin 响应模型。</para>
    /// <para>en-us:Taxin response model used by migrated tests.</para>
    /// </summary>
    public class TaxinResponseModel
    {
        public string? name { get; set; }

        public MethodAccessException? Exception { get; set; }

        public string? Name { get; set; }

        public List<string>? Description { get; set; }

        public IList<RouteValueDictionary>? Routes { get; set; }
    }
}
