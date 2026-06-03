using Air.Cloud.Core.Plugins.Security.MD5;
using Air.Cloud.Core.Standard.DistributedLock.Attributes;
using Air.Cloud.Core.Standard.DistributedLock.Plugins;

namespace Air.Cloud.UnitTest.Core.Standard
{
    /// <summary>
    /// <para>zh-cn:分布式锁标准辅助能力的单元测试，覆盖锁 Key 生成与特性默认值。</para>
    /// <para>en-us:Unit tests for distributed-lock standard helpers, covering lock-key generation and attribute defaults.</para>
    /// </summary>
    public class DistributedLockStandardTests
    {
        /// <summary>
        /// <para>zh-cn:验证默认锁 Key 工厂使用稳定 MD5 结果，保证相同业务参数命中同一把锁。</para>
        /// <para>en-us:Verifies the default lock-key factory produces a stable MD5 value so identical business parameters hit the same lock.</para>
        /// </summary>
        [Fact]
        public void DistributedLockKeyFactoryPlugin_should_generate_stable_md5_key()
        {
            var plugin = new DistributedLockKeyFactoryPlugin();
            const string parameter = "{\"OrderId\":\"A001\"}";

            var firstKey = plugin.GetKey(parameter);
            var secondKey = plugin.GetKey(parameter);

            Assert.Equal(MD5Encryption.GetMd5By32(parameter), firstKey);
            Assert.Equal(firstKey, secondKey);
            Assert.Equal(32, firstKey.Length);
        }

        /// <summary>
        /// <para>zh-cn:验证不同参数生成不同锁 Key，避免不同业务请求被错误串行化。</para>
        /// <para>en-us:Verifies different parameters generate different lock keys so unrelated business requests are not serialized incorrectly.</para>
        /// </summary>
        [Fact]
        public void DistributedLockKeyFactoryPlugin_should_generate_different_keys_for_different_params()
        {
            var plugin = new DistributedLockKeyFactoryPlugin();

            var firstKey = plugin.GetKey("order-a");
            var secondKey = plugin.GetKey("order-b");

            Assert.NotEqual(firstKey, secondKey);
        }

        /// <summary>
        /// <para>zh-cn:验证默认分布式锁特性值符合框架约定。</para>
        /// <para>en-us:Verifies the default distributed-lock attribute values match the framework contract.</para>
        /// </summary>
        [Fact]
        public void DistributedLockAttribute_should_use_default_values()
        {
            var attribute = new DistributedLockAttribute();

            Assert.Equal("系统繁忙,请稍后再试", attribute.FailMessage);
            Assert.Equal(30000, attribute.LockMilliseconds);
            Assert.Equal(10000, attribute.WaitLockMilliseconds);
            Assert.Equal(200, attribute.StepWaitMilliseconds);
            Assert.Equal(string.Empty, attribute.LockKey);
        }

        /// <summary>
        /// <para>zh-cn:验证锁持有时间小于等待时间时会自动延长，避免锁在等待窗口内过早释放。</para>
        /// <para>en-us:Verifies lock holding time is extended when shorter than wait time, preventing premature release within the wait window.</para>
        /// </summary>
        [Fact]
        public void DistributedLockAttribute_should_extend_lock_time_when_shorter_than_wait_time()
        {
            var attribute = new DistributedLockAttribute(
                WaitLockMilliseconds: 5000,
                LockKey: "order-id",
                FailMessage: "busy",
                StepWaitMilliseconds: 100,
                LockMilliseconds: 3000);

            Assert.Equal("order-id", attribute.LockKey);
            Assert.Equal("busy", attribute.FailMessage);
            Assert.Equal(100, attribute.StepWaitMilliseconds);
            Assert.Equal(5000, attribute.WaitLockMilliseconds);
            Assert.Equal(6000, attribute.LockMilliseconds);
        }
    }
}
