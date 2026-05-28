using Air.Cloud.UnitTest.Compatibility.Dto;
using Air.Cloud.UnitTest.Compatibility.Services;

namespace Air.Cloud.UnitTest.Modules.ActionLog
{
    /// <summary>
    /// <para>zh-cn:操作日志过滤器测试服务的迁移测试集合。</para>
    /// <para>en-us:Migrated test suite for the action log filter test service.</para>
    /// </summary>
    public class ActionLogFilterTests
    {
        /// <summary>
        /// <para>zh-cn:测试 DTO 透传场景，确认过滤器测试服务不会修改输入对象实例。</para>
        /// <para>en-us:Tests DTO pass-through behavior to ensure the service returns the same input object instance unchanged.</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:测试过程：构造 TestSDto 并调用 Test，断言返回对象与输入对象引用相同。</para>
        /// <para>en-us:Process: create TestSDto and invoke Test, then assert returned object reference is the same as input.</para>
        /// </remarks>
        [Fact]
        public void Test_should_return_same_dto_instance()
        {
            var dto = new TestSDto
            {
                Id = "dto-id",
                UserId = "user-01",
                ServiceNo = "service-99",
                LoseTime = new DateTime(2026, 3, 25, 10, 0, 0, DateTimeKind.Utc)
            };
            var service = new ActionLogFilterTestService();

            var result = service.Test(dto);

            Assert.Same(dto, result);
        }
    }
}
