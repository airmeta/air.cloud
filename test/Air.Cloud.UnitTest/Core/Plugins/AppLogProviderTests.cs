using Air.Cloud.Core.Extensions;
using Air.Cloud.Core.Plugins.LogFiltering;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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

        /// <summary>
        /// <para>zh-cn:验证分类最低级别配置会在日志格式化前关闭 Ocelot Information 日志，避免高频网关日志进入统一输出。</para>
        /// <para>en-us:Verifies category minimum-level options disable Ocelot Information logs before formatting so high-frequency gateway logs do not enter unified output.</para>
        /// </summary>
        [Fact]
        public void CustomConsoleLogger_should_skip_output_below_category_minimum_level()
        {
            var logger = new CustomConsoleLogger(
                "Ocelot.Requester.Middleware.HttpRequesterMiddleware",
                options: Options.Create(new AppLogFilterOptions
                {
                    CategoryMinimumLevels = new Dictionary<string, LogLevel>
                    {
                        ["Ocelot"] = LogLevel.Warning
                    }
                }));
            var originalOut = Console.Out;
            using var writer = new StringWriter();
            Console.SetOut(writer);

            try
            {
                logger.LogInformation("request uri: http://air.dynamicapp:8080/mods/auth/v1/auth/store/Ticket/temp");
            }
            finally
            {
                Console.SetOut(originalOut);
            }

            Assert.False(logger.IsEnabled(LogLevel.Information));
            Assert.Equal(string.Empty, writer.ToString());
        }

        /// <summary>
        /// <para>zh-cn:验证达到分类最低级别的日志仍会输出，避免性能过滤误吞 Warning 及以上故障信息。</para>
        /// <para>en-us:Verifies logs that meet the category minimum level are still written so performance filtering does not hide Warning and above failure information.</para>
        /// </summary>
        [Fact]
        public void CustomConsoleLogger_should_write_output_at_category_minimum_level()
        {
            var logger = new CustomConsoleLogger(
                "Ocelot.Requester.Middleware.HttpRequesterMiddleware",
                options: Options.Create(new AppLogFilterOptions
                {
                    CategoryMinimumLevels = new Dictionary<string, LogLevel>
                    {
                        ["Ocelot"] = LogLevel.Warning
                    }
                }));
            var originalOut = Console.Out;
            using var writer = new StringWriter();
            Console.SetOut(writer);

            try
            {
                logger.LogWarning("Ocelot downstream request failed.");
            }
            finally
            {
                Console.SetOut(originalOut);
            }

            Assert.True(logger.IsEnabled(LogLevel.Warning));
            Assert.Contains("Ocelot downstream request failed.", writer.ToString());
        }

        /// <summary>
        /// <para>zh-cn:验证分类最低级别使用最长前缀优先，允许对更窄的框架分类保留更详细日志。</para>
        /// <para>en-us:Verifies category minimum levels use the longest prefix first, allowing narrower framework categories to keep more detailed logs.</para>
        /// </summary>
        [Fact]
        public void CustomConsoleLogger_should_use_longest_category_minimum_level_prefix()
        {
            var logger = new CustomConsoleLogger(
                "Microsoft.AspNetCore.Hosting.Lifetime",
                options: Options.Create(new AppLogFilterOptions
                {
                    MinimumLevel = LogLevel.Warning,
                    CategoryMinimumLevels = new Dictionary<string, LogLevel>
                    {
                        ["Microsoft.AspNetCore"] = LogLevel.Error,
                        ["Microsoft.AspNetCore.Hosting"] = LogLevel.Information
                    }
                }));

            Assert.True(logger.IsEnabled(LogLevel.Information));
            Assert.False(logger.IsEnabled(LogLevel.Debug));
        }

        /// <summary>
        /// <para>zh-cn:验证日志过滤选项后期配置会补齐空集合、移除空分类前缀，并保留不区分大小写的分类级别匹配。</para>
        /// <para>en-us:Verifies log-filter option post-configuration restores null collections, removes blank category prefixes, and preserves case-insensitive category-level matching.</para>
        /// </summary>
        [Fact]
        public void AppLogFilterOptions_should_normalize_null_collections_and_category_levels()
        {
            var options = new AppLogFilterOptions
            {
                IgnorePaths = null!,
                CategoryPrefixes = null!,
                CategoryMinimumLevels = new Dictionary<string, LogLevel>
                {
                    [""] = LogLevel.Trace,
                    ["ocelot"] = LogLevel.Warning
                }
            };

            options.PostConfigure(options, new ConfigurationBuilder().Build());
            var logger = new CustomConsoleLogger(
                "Ocelot.Requester.Middleware.HttpRequesterMiddleware",
                options: Options.Create(options));

            Assert.NotNull(options.IgnorePaths);
            Assert.NotNull(options.CategoryPrefixes);
            Assert.Single(options.CategoryMinimumLevels);
            Assert.True(options.CategoryMinimumLevels.ContainsKey("OCELOT"));
            Assert.False(logger.IsEnabled(LogLevel.Information));
        }

        /// <summary>
        /// <para>zh-cn:验证 `MinimumLevelOverrides` 作为分类最低级别别名时会合并到 `CategoryMinimumLevels`，从而兼容网关运行参数中的新命名。</para>
        /// <para>en-us:Verifies `MinimumLevelOverrides` is merged into `CategoryMinimumLevels` as a category minimum-level alias, keeping the new gateway runtime parameter naming compatible.</para>
        /// </summary>
        [Fact]
        public void AppLogFilterOptions_should_merge_minimum_level_overrides_alias()
        {
            var options = new AppLogFilterOptions
            {
                MinimumLevelOverrides = new Dictionary<string, LogLevel>
                {
                    ["Ocelot"] = LogLevel.Warning
                }
            };

            options.PostConfigure(options, new ConfigurationBuilder().Build());
            var logger = new CustomConsoleLogger(
                "Ocelot.Requester.Middleware.HttpRequesterMiddleware",
                options: Options.Create(options));

            Assert.True(options.CategoryMinimumLevels.ContainsKey("Ocelot"));
            Assert.False(logger.IsEnabled(LogLevel.Information));
            Assert.True(logger.IsEnabled(LogLevel.Warning));
        }

        /// <summary>
        /// <para>zh-cn:验证 `CategoryMinimumLevels` 与 `MinimumLevelOverrides` 存在相同前缀时，显式分类配置优先生效。</para>
        /// <para>en-us:Verifies explicit `CategoryMinimumLevels` entries win when the same prefix also exists in `MinimumLevelOverrides`.</para>
        /// </summary>
        [Fact]
        public void AppLogFilterOptions_should_keep_category_minimum_levels_when_alias_duplicates_prefix()
        {
            var options = new AppLogFilterOptions
            {
                CategoryMinimumLevels = new Dictionary<string, LogLevel>
                {
                    ["Ocelot"] = LogLevel.Error
                },
                MinimumLevelOverrides = new Dictionary<string, LogLevel>
                {
                    ["ocelot"] = LogLevel.Warning
                }
            };

            options.PostConfigure(options, new ConfigurationBuilder().Build());
            var logger = new CustomConsoleLogger(
                "Ocelot.Requester.Middleware.HttpRequesterMiddleware",
                options: Options.Create(options));

            Assert.False(logger.IsEnabled(LogLevel.Warning));
            Assert.True(logger.IsEnabled(LogLevel.Error));
        }
    }
}
