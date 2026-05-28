using Air.Cloud.Core;
using Air.Cloud.Core.Standard.Taxin.Client;
using Air.Cloud.Core.Standard.TraceLog;
using Air.Cloud.UnitTest.Compatibility.Dto;

namespace Air.Cloud.UnitTest.Compatibility.Services
{
    /// <summary>
    /// <para>zh-cn:用于迁移测试的本地 SkyWalking 模块服务。</para>
    /// <para>en-us:Local SkyWalking module service used by migrated tests.</para>
    /// </summary>
    public class SkyWalkingModuleService
    {
        private readonly ITraceLogStandard traceLog;
        private readonly ITaxinClientStandard taxinClientStandard;

        /// <summary>
        /// <para>zh-cn:创建 SkyWalking 模块服务实例。</para>
        /// <para>en-us:Creates a SkyWalking module service instance.</para>
        /// </summary>
        public SkyWalkingModuleService(ITraceLogStandard log, ITaxinClientStandard taxinClientStandard)
        {
            traceLog = log;
            this.taxinClientStandard = taxinClientStandard;
        }

        /// <summary>
        /// <para>zh-cn:记录 DTO 日志并调用 Taxin，返回 DTO 与远端结果。</para>
        /// <para>en-us:Writes DTO log and invokes Taxin, then returns DTO with remote result.</para>
        /// </summary>
        public async Task<object> Test(TestSDto dto)
        {
            traceLog.Write(AppRealization.JSON.Serialize(dto));
            var data = await taxinClientStandard.SendAsync<object>("taxin.service.test");
            return new
            {
                dto,
                data
            };
        }
    }
}
