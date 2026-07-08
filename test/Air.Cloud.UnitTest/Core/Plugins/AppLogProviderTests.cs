using Air.Cloud.Core.Extensions;
using Air.Cloud.Core.Plugins.LogFiltering;

using Microsoft.Extensions.Logging;

namespace Air.Cloud.UnitTest.Core.Plugins
{
    /// <summary>
    /// <para>zh-cn:Air.Cloud 自定义 ILogger Provider 单元测试，验证统一输出前会执行日志过滤插件。</para>
    /// <para>en-us:Unit tests for the Air.Cloud custom ILogger provider, verifying the log filtering plugin runs before unified output.</para>
    /// </summary>
    public class AppLogProviderTests
    {
        /// <summary>
        /// <para>zh-cn:验证命中过滤规则的日志不会写入控制台输出。</para>
        /// <para>en-us:Verifies logs that match filtering rules are not written to console output.</para>
        /// </summary>
        [Fact]
        public void CustomConsoleLogger_should_skip_output_when_filter_matches()
        {
            var plugin = new DefaultAppLogFilterPlugin();
            plugin.AddIgnorePath("/Health");
            var logger = new CustomConsoleLogger("Microsoft.AspNetCore.Hosting.Diagnostics", plugin);
            var originalOut = Console.Out;
            using var writer = new StringWriter();
            Console.SetOut(writer);

            try
            {
                logger.LogInformation("Request finished HTTP/1.1 GET http://127.0.0.1:5000/Health - 200");
            }
            finally
            {
                Console.SetOut(originalOut);
            }

            Assert.Equal(string.Empty, writer.ToString());
        }

        /// <summary>
        /// <para>zh-cn:验证未命中过滤规则的日志仍会进入 Air.Cloud 统一输出。</para>
        /// <para>en-us:Verifies logs that do not match filtering rules still enter Air.Cloud unified output.</para>
        /// </summary>
        [Fact]
        public void CustomConsoleLogger_should_write_output_when_filter_does_not_match()
        {
            var plugin = new DefaultAppLogFilterPlugin();
            plugin.AddIgnorePath("/Health");
            var logger = new CustomConsoleLogger("Microsoft.Hosting.Lifetime", plugin);
            var originalOut = Console.Out;
            using var writer = new StringWriter();
            Console.SetOut(writer);

            try
            {
                logger.LogInformation("Application started.");
            }
            finally
            {
                Console.SetOut(originalOut);
            }

            Assert.Contains("Application started.", writer.ToString());
        }
    }
}
