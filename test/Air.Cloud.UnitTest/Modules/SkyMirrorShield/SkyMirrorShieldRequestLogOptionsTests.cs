using Air.Cloud.Modules.SkyMirrorShield.Options;
using Air.Cloud.Modules.SkyMirrorShield.Utils;

using Microsoft.Extensions.Configuration;

namespace Air.Cloud.UnitTest.Modules.SkyMirrorShield
{
    /// <summary>
    /// <para>zh-cn:天镜盾请求运行日志配置单元测试，验证默认值、运行期配置读取、放行日志开关、拒绝日志开关和采样行为。</para>
    /// <para>en-us:Unit tests for SkyMirrorShield request runtime log options, covering defaults, runtime configuration reads, allowed-log switches, rejected-log switches, and sampling behavior.</para>
    /// </summary>
    public sealed class SkyMirrorShieldRequestLogOptionsTests
    {
        /// <summary>
        /// <para>zh-cn:验证未提供配置时保持兼容默认值，放行请求和拒绝请求运行日志均启用。</para>
        /// <para>en-us:Verifies backward-compatible defaults when no configuration is supplied: both allowed-request and rejected-request runtime logs are enabled.</para>
        /// </summary>
        [Fact]
        public void ReadRequestLogOptions_should_return_backward_compatible_defaults()
        {
            var configuration = new ConfigurationBuilder().Build();

            var options = SkyMirrorShieldUtil.ReadRequestLogOptions(configuration);

            Assert.True(options.Enabled);
            Assert.True(options.LogAllowedRequests);
            Assert.True(options.LogRejectedRequests);
            Assert.Equal(0, options.SampleRate);
        }

        /// <summary>
        /// <para>zh-cn:验证配置可以关闭放行请求运行日志，同时保留拒绝请求运行日志。</para>
        /// <para>en-us:Verifies configuration can disable allowed-request runtime logs while keeping rejected-request runtime logs.</para>
        /// </summary>
        [Fact]
        public void ReadRequestLogOptions_should_bind_allowed_and_rejected_log_switches()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["SkyMirrorShieldSettings:RequestLog:Enabled"] = "true",
                    ["SkyMirrorShieldSettings:RequestLog:LogAllowedRequests"] = "false",
                    ["SkyMirrorShieldSettings:RequestLog:LogRejectedRequests"] = "true",
                    ["SkyMirrorShieldSettings:RequestLog:SampleRate"] = "0"
                })
                .Build();

            var options = SkyMirrorShieldUtil.ReadRequestLogOptions(configuration);

            Assert.True(options.Enabled);
            Assert.False(options.LogAllowedRequests);
            Assert.True(options.LogRejectedRequests);
            Assert.Equal(0, options.SampleRate);
            Assert.False(SkyMirrorShieldUtil.ShouldLogAllowedRequest(options));
            Assert.True(SkyMirrorShieldUtil.ShouldLogRejectedRequest(options));
        }

        /// <summary>
        /// <para>zh-cn:验证放行请求日志关闭后可以按采样比例输出，采样不会影响拒绝请求日志判断。</para>
        /// <para>en-us:Verifies allowed-request logs can still be written by sample rate after normal allowed logging is disabled, and sampling does not affect rejected-request decisions.</para>
        /// </summary>
        [Fact]
        public void ShouldLogAllowedRequest_should_respect_sample_rate_when_allowed_logs_are_disabled()
        {
            var options = new SkyMirrorShieldRequestLogOptions
            {
                Enabled = true,
                LogAllowedRequests = false,
                LogRejectedRequests = true,
                SampleRate = 2
            };

            var first = SkyMirrorShieldUtil.ShouldLogAllowedRequest(options);
            var second = SkyMirrorShieldUtil.ShouldLogAllowedRequest(options);
            var third = SkyMirrorShieldUtil.ShouldLogAllowedRequest(options);
            var fourth = SkyMirrorShieldUtil.ShouldLogAllowedRequest(options);

            Assert.Equal(1, new[] { first, second }.Count(item => item));
            Assert.Equal(1, new[] { third, fourth }.Count(item => item));
            Assert.True(SkyMirrorShieldUtil.ShouldLogRejectedRequest(options));
        }

        /// <summary>
        /// <para>zh-cn:验证总开关关闭后，放行和拒绝请求的天镜盾运行日志都不会输出。</para>
        /// <para>en-us:Verifies disabling the master switch suppresses SkyMirrorShield runtime logs for both allowed and rejected requests.</para>
        /// </summary>
        [Fact]
        public void ShouldLogRequest_should_return_false_when_request_log_is_disabled()
        {
            var options = new SkyMirrorShieldRequestLogOptions
            {
                Enabled = false,
                LogAllowedRequests = true,
                LogRejectedRequests = true,
                SampleRate = 1
            };

            Assert.False(SkyMirrorShieldUtil.ShouldLogAllowedRequest(options));
            Assert.False(SkyMirrorShieldUtil.ShouldLogRejectedRequest(options));
        }

        /// <summary>
        /// <para>zh-cn:验证负数采样配置会被标准化为 0，避免错误配置产生不确定行为。</para>
        /// <para>en-us:Verifies negative sample-rate configuration is normalized to zero to avoid nondeterministic behavior from invalid configuration.</para>
        /// </summary>
        [Fact]
        public void Normalize_should_reset_negative_sample_rate()
        {
            var options = new SkyMirrorShieldRequestLogOptions
            {
                LogAllowedRequests = false,
                SampleRate = -10
            };

            options.Normalize();

            Assert.Equal(0, options.SampleRate);
            Assert.False(SkyMirrorShieldUtil.ShouldLogAllowedRequest(options));
        }
    }
}
