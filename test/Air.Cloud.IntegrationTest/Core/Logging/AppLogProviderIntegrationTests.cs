using Air.Cloud.Core.Extensions;
using Air.Cloud.Core.Plugins.LogFiltering;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Air.Cloud.IntegrationTest.Core.Logging;

/// <summary>
/// <para>zh-cn:验证 Air.Cloud 自定义日志提供器在依赖注入注册后的分类级别过滤行为。</para>
/// <para>en-us:Verifies category-level filtering behavior after the Air.Cloud custom log provider is registered in dependency injection.</para>
/// </summary>
public sealed class AppLogProviderIntegrationTests
{
    /// <summary>
    /// <para>zh-cn:验证通过 AddCustomConsole 注册日志组件后，ILoggerFactory 创建的 Ocelot 分类日志会遵循 AppLogFilter 的分类最低级别配置。</para>
    /// <para>en-us:Verifies that after logging is registered with AddCustomConsole, Ocelot category loggers created by ILoggerFactory honor AppLogFilter category minimum-level configuration.</para>
    /// </summary>
    [Fact]
    public void AddCustomConsole_should_apply_category_minimum_level_to_logger_factory()
    {
        using var provider = new ServiceCollection()
            .AddLogging(builder => builder.AddCustomConsole())
            .Configure<AppLogFilterOptions>(options =>
            {
                options.CategoryMinimumLevels["Ocelot"] = LogLevel.Warning;
            })
            .BuildServiceProvider();

        var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("Ocelot.Requester.Middleware.HttpRequesterMiddleware");

        Assert.False(logger.IsEnabled(LogLevel.Information));
        Assert.True(logger.IsEnabled(LogLevel.Warning));
    }

    /// <summary>
    /// <para>zh-cn:验证通过依赖注入绑定 `MinimumLevelOverrides` 时，Air.Cloud 自定义日志 Provider 会把该别名作为分类最低级别配置应用到 ILoggerFactory。</para>
    /// <para>en-us:Verifies that when `MinimumLevelOverrides` is bound through dependency injection, the Air.Cloud custom log provider applies the alias as category minimum-level configuration to ILoggerFactory.</para>
    /// </summary>
    [Fact]
    public void AddCustomConsole_should_apply_minimum_level_overrides_alias_to_logger_factory()
    {
        using var provider = new ServiceCollection()
            .AddLogging(builder => builder.AddCustomConsole())
            .Configure<AppLogFilterOptions>(options =>
            {
                options.MinimumLevelOverrides["Ocelot"] = LogLevel.Warning;
            })
            .BuildServiceProvider();

        var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("Ocelot.Requester.Middleware.HttpRequesterMiddleware");

        Assert.False(logger.IsEnabled(LogLevel.Information));
        Assert.True(logger.IsEnabled(LogLevel.Warning));
    }
}
