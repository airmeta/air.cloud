using Air.Cloud.UnitTest.Compatibility.Dto;

namespace Air.Cloud.UnitTest.Compatibility.Services
{
    /// <summary>
    /// <para>zh-cn:用于迁移测试的本地操作日志过滤器测试服务。</para>
    /// <para>en-us:Local action log filter test service used by migrated tests.</para>
    /// </summary>
    public class ActionLogFilterTestService
    {
        /// <summary>
        /// <para>zh-cn:返回传入的 DTO 对象。</para>
        /// <para>en-us:Returns the input DTO instance.</para>
        /// </summary>
        /// <param name="dto">
        /// <para>zh-cn:测试 DTO。</para>
        /// <para>en-us:The test DTO.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:返回原始 DTO。</para>
        /// <para>en-us:Returns the original DTO.</para>
        /// </returns>
        public object Test(TestSDto dto) => dto;
    }
}
