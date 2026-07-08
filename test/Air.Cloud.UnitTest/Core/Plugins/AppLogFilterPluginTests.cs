using Air.Cloud.Core.Plugins.LogFiltering;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Air.Cloud.UnitTest.Core.Plugins
{
    /// <summary>
    /// <para>zh-cn:Air.Cloud 日志过滤插件单元测试，验证健康检查路径登记、请求上下文过滤和高优先级日志保留。</para>
    /// <para>en-us:Unit tests for the Air.Cloud log filtering plugin, verifying health-check path registration, request-context filtering, and high-priority log preservation.</para>
    /// </summary>
    public class AppLogFilterPluginTests
    {
        /// <summary>
        /// <para>zh-cn:验证忽略路径会被规范化，完整 URL、查询字符串和未带斜杠的路径都可以匹配。</para>
        /// <para>en-us:Verifies ignored paths are normalized so full URLs, query strings, and paths without leading slash can match.</para>
        /// </summary>
        [Theory]
        [InlineData("health", "/health")]
        [InlineData("/health?from=consul", "/health")]
        [InlineData("http://127.0.0.1:5000/Health?x=1", "/Health")]
        public void AddIgnorePath_should_normalize_path(string input, string expected)
        {
            var plugin = new DefaultAppLogFilterPlugin();

            plugin.AddIgnorePath(input);

            Assert.Contains(plugin.IgnorePaths, path => string.Equals(path, expected.TrimEnd('/'), StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// <para>zh-cn:验证日志内容带有忽略路径时，低级别框架日志会被过滤。</para>
        /// <para>en-us:Verifies low-level framework logs are filtered when their formatted content contains an ignored path.</para>
        /// </summary>
        [Fact]
        public void ShouldIgnore_should_filter_framework_log_when_content_contains_path()
        {
            var plugin = new DefaultAppLogFilterPlugin();
            plugin.AddIgnorePath("/Health");

            var ignored = plugin.ShouldIgnore(new AppLogFilterContext
            {
                CategoryName = "Microsoft.AspNetCore.Hosting.Diagnostics",
                LogLevel = LogLevel.Information,
                Content = "Request finished HTTP/1.1 GET http://127.0.0.1:5000/Health - 200"
            });

            Assert.True(ignored);
        }

        /// <summary>
        /// <para>zh-cn:验证日志内容不带路径时，可以使用当前请求上下文路径过滤框架日志。</para>
        /// <para>en-us:Verifies framework logs can be filtered by current request path when the formatted content does not include a path.</para>
        /// </summary>
        [Fact]
        public void ShouldIgnore_should_filter_framework_log_by_request_context_path()
        {
            var plugin = new DefaultAppLogFilterPlugin();
            plugin.AddIgnorePath("/Health");

            var ignored = plugin.ShouldIgnore(new AppLogFilterContext
            {
                CategoryName = "Microsoft.AspNetCore.ResponseCaching.ResponseCachingMiddleware",
                LogLevel = LogLevel.Information,
                Content = "No cached response available for this request.",
                RequestPath = "/Health"
            });

            Assert.True(ignored);
        }

        /// <summary>
        /// <para>zh-cn:验证 Warning 及以上日志即使命中健康检查路径也会保留。</para>
        /// <para>en-us:Verifies Warning and above are kept even when the health-check path matches.</para>
        /// </summary>
        [Fact]
        public void ShouldIgnore_should_keep_warning_and_above()
        {
            var plugin = new DefaultAppLogFilterPlugin();
            plugin.AddIgnorePath("/Health");

            var ignored = plugin.ShouldIgnore(new AppLogFilterContext
            {
                CategoryName = "Microsoft.AspNetCore.Hosting.Diagnostics",
                LogLevel = LogLevel.Warning,
                Content = "Request finished HTTP/1.1 GET http://127.0.0.1:5000/Health - 500"
            });

            Assert.False(ignored);
        }

        /// <summary>
        /// <para>zh-cn:验证业务日志分类默认不会因为请求路径命中而被过滤。</para>
        /// <para>en-us:Verifies business log categories are not filtered by default even when the request path matches.</para>
        /// </summary>
        [Fact]
        public void ShouldIgnore_should_keep_business_category_by_default()
        {
            var plugin = new DefaultAppLogFilterPlugin();
            plugin.AddIgnorePath("/Health");

            var ignored = plugin.ShouldIgnore(new AppLogFilterContext
            {
                CategoryName = "My.Business.Service",
                LogLevel = LogLevel.Information,
                Content = "business log",
                RequestPath = "/Health"
            });

            Assert.False(ignored);
        }

        /// <summary>
        /// <para>zh-cn:验证配置项可以调整允许过滤的分类范围。</para>
        /// <para>en-us:Verifies options can adjust the category scope where filtering is allowed.</para>
        /// </summary>
        [Fact]
        public void ShouldIgnore_should_use_configured_category_prefixes()
        {
            var plugin = new DefaultAppLogFilterPlugin(Options.Create(new AppLogFilterOptions
            {
                IgnorePaths = new List<string> { "/internal-health" },
                CategoryPrefixes = new List<string>()
            }));

            var ignored = plugin.ShouldIgnore(new AppLogFilterContext
            {
                CategoryName = "Custom.Category",
                LogLevel = LogLevel.Information,
                RequestPath = "/internal-health"
            });

            Assert.True(ignored);
        }
    }
}
