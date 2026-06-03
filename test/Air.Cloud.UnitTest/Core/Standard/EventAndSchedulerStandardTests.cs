using Air.Cloud.Core.Standard.Event.Extensions;
using Air.Cloud.Core.Standard.SchedulerStandard.Extensions;

namespace Air.Cloud.UnitTest.Core.Standard
{
    /// <summary>
    /// <para>zh-cn:事件与调度标准扩展的单元测试。</para>
    /// <para>en-us:Unit tests for event and scheduler standard extensions.</para>
    /// </summary>
    public class EventAndSchedulerStandardTests
    {
        /// <summary>
        /// <para>zh-cn:验证事件枚举可以序列化为包含程序集、完整类型名和枚举名的稳定字符串。</para>
        /// <para>en-us:Verifies event enums serialize to a stable string containing assembly, full type name, and enum name.</para>
        /// </summary>
        [Fact]
        public void ParseToString_should_include_assembly_type_and_enum_name()
        {
            var value = TestEventKind.OrderCreated;

            var text = value.ParseToString();

            Assert.Contains(typeof(TestEventKind).Assembly.GetName().Name!, text);
            Assert.Contains(typeof(TestEventKind).FullName!, text);
            Assert.EndsWith($".{nameof(TestEventKind.OrderCreated)}", text);
        }

        /// <summary>
        /// <para>zh-cn:验证事件枚举字符串可以还原为原枚举值，用于事件存储和跨进程传递。</para>
        /// <para>en-us:Verifies event enum strings can be restored to the original enum value for event storage and cross-process transfer.</para>
        /// </summary>
        [Fact]
        public void ParseToEnum_should_restore_enum_from_string()
        {
            var value = TestEventKind.OrderPaid;
            var text = value.ParseToString();

            var restored = text.ParseToEnum();

            Assert.Equal(value, restored);
        }

        /// <summary>
        /// <para>zh-cn:记录当前 Cron 校验扩展的兼容行为：当前实现对任意字符串返回 true。</para>
        /// <para>en-us:Documents current compatibility behavior of the Cron validation extension: any string currently returns true.</para>
        /// </summary>
        [Theory]
        [InlineData("0/5 * * * * ?")]
        [InlineData("not-a-cron")]
        [InlineData("")]
        public void IsValidExpression_should_keep_current_compatibility_contract(string expression)
        {
            Assert.True(expression.IsValidExpression());
        }

        private enum TestEventKind
        {
            OrderCreated,
            OrderPaid
        }
    }
}
