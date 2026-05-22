using unit.webapp.model.Dto;

namespace Air.Cloud.UnitTest.Modules.ActionLog
{
    /// <summary>
    /// <para>zh-cn:操作日志过滤器测试服务迁移测试集合。</para>
    /// <para>en-us:Migrated test suite for the action log filter test service.</para>
    /// </summary>
    public class ActionLogFilterTests
    {
        /// <summary>
        /// <para>zh-cn:验证 Test 方法会原样返回传入的 DTO 对象。</para>
        /// <para>en-us:Verifies that the Test method returns the original DTO instance unchanged.</para>
        /// </summary>
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
            var service = new unit.webapp.service.services.ActionLogFilterTestService.ActionLogFilterTestService();

            var result = service.Test(dto);

            Assert.Same(dto, result);
        }
    }
}
