/*
 * Copyright (c) 2024-2030 星曳数据
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * This file is provided under the Mozilla Public License Version 2.0,
 * and the "NO WARRANTY" clause of the MPL is hereby expressly
 * acknowledged.
 */
using Air.Cloud.Core.App;
using Air.Cloud.Core.Plugins.LogFiltering;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
namespace Air.Cloud.Core.Extensions
{
        /// <summary>
        /// <para>zh-cn:提供 Air.Cloud 自定义控制台日志实例，将 Microsoft.Extensions.Logging 日志接入框架统一输出管道。</para>
        /// <para>en-us:Provides Air.Cloud custom console logger instances, connecting Microsoft.Extensions.Logging logs to the framework unified output pipeline.</para>
        /// </summary>
        public class AppLogProvider : ILoggerProvider
        {
            private readonly IAppLogFilterPlugin _logFilter;

            /// <summary>
            /// <para>zh-cn:初始化 Air.Cloud 日志提供器，并使用可替换的日志过滤插件处理输出前过滤。</para>
            /// <para>en-us:Initializes the Air.Cloud log provider and uses a replaceable log filtering plugin for pre-output filtering.</para>
            /// </summary>
            /// <param name="logFilter">
            /// <para>zh-cn:日志过滤插件；为空时会使用框架默认插件。</para>
            /// <para>en-us:The log filtering plugin; the framework default plugin is used when null.</para>
            /// </param>
            public AppLogProvider(IAppLogFilterPlugin logFilter = null)
            {
                _logFilter = logFilter ?? new DefaultAppLogFilterPlugin();
            }

            /// <summary>
            /// <para>zh-cn:根据日志分类名称创建自定义控制台日志实例。</para>
            /// <para>en-us:Creates a custom console logger instance by logger category name.</para>
            /// </summary>
            /// <param name="categoryName">
            /// <para>zh-cn:日志分类名称，通常为框架组件或业务类型名称。</para>
            /// <para>en-us:The logger category name, usually a framework component or business type name.</para>
            /// </param>
            /// <returns>
            /// <para>zh-cn:用于输出指定分类日志的日志实例。</para>
            /// <para>en-us:A logger instance used to write logs for the specified category.</para>
            /// </returns>
            public ILogger CreateLogger(string categoryName)
            {
                // categoryName 是日志分类（如 Microsoft.AspNetCore.Server.Kestrel），作为 Title 一部分
                return new CustomConsoleLogger(categoryName, _logFilter);
            }

            /// <summary>
            /// <para>zh-cn:释放日志提供器资源。当前实现没有需要释放的托管资源。</para>
            /// <para>en-us:Releases logger provider resources. The current implementation has no managed resources to release.</para>
            /// </summary>
            public void Dispose()
            {
                // 无需释放资源，空实现
            }
        }

        /// <summary>
        /// <para>zh-cn:实现 Air.Cloud 自定义控制台日志格式化逻辑，并将日志内容转换为 `AppPrintInformation` 输出。</para>
        /// <para>en-us:Implements Air.Cloud custom console log formatting and converts log entries to `AppPrintInformation` output.</para>
        /// </summary>
        public class CustomConsoleLogger : ILogger
        {
            private readonly string _categoryName;
            private readonly IAppLogFilterPlugin _logFilter;

            /// <summary>
            /// <para>zh-cn:初始化指定分类名称的自定义控制台日志实例。</para>
            /// <para>en-us:Initializes a custom console logger instance for the specified category name.</para>
            /// </summary>
            /// <param name="categoryName">
            /// <para>zh-cn:日志分类名称。</para>
            /// <para>en-us:The logger category name.</para>
            /// </param>
            /// <param name="logFilter">
            /// <para>zh-cn:日志过滤插件；为空时使用默认过滤插件。</para>
            /// <para>en-us:The log filtering plugin; the default filtering plugin is used when null.</para>
            /// </param>
            public CustomConsoleLogger(string categoryName, IAppLogFilterPlugin logFilter = null)
            {
                _categoryName = categoryName;
                _logFilter = logFilter ?? new DefaultAppLogFilterPlugin();
            }

            /// <summary>
            /// <para>zh-cn:写入日志事件，并将日志级别、标题和内容映射到框架统一输出模型。</para>
            /// <para>en-us:Writes a log event and maps its level, title, and content to the framework unified output model.</para>
            /// </summary>
            /// <typeparam name="TState">
            /// <para>zh-cn:日志状态对象类型。</para>
            /// <para>en-us:The log state object type.</para>
            /// </typeparam>
            /// <param name="logLevel">
            /// <para>zh-cn:日志级别。</para>
            /// <para>en-us:The log level.</para>
            /// </param>
            /// <param name="eventId">
            /// <para>zh-cn:日志事件标识。</para>
            /// <para>en-us:The log event identifier.</para>
            /// </param>
            /// <param name="state">
            /// <para>zh-cn:日志状态对象。</para>
            /// <para>en-us:The log state object.</para>
            /// </param>
            /// <param name="exception">
            /// <para>zh-cn:日志关联异常。</para>
            /// <para>en-us:The exception associated with the log entry.</para>
            /// </param>
            /// <param name="formatter">
            /// <para>zh-cn:用于格式化日志状态和异常的委托。</para>
            /// <para>en-us:The delegate used to format the log state and exception.</para>
            /// </param>
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                    // 1. 过滤掉不需要的日志级别（可选，如只输出 Information 及以上）
                    if (!IsEnabled(logLevel))
                        return;
                    var title = GetTitle(_categoryName, state); // 提取/生成 Title
                    var content = formatter(state, exception); // 日志内容（包含异常信息）
                    if (_logFilter.ShouldIgnore(CreateFilterContext(logLevel, eventId, state, content)))
                        return;
                    AppRealization.Output.Print(new AppPrintInformation()
                    {
                        State=true,
                        AdditionalParams=new Dictionary<string, object>() { },
                        Content= content,
                        Level= logLevel switch
                        {
                            LogLevel.Trace => AppPrintLevel.Debug,
                            LogLevel.Debug => AppPrintLevel.Debug,
                            LogLevel.Information => AppPrintLevel.Information,
                            LogLevel.Warning => AppPrintLevel.Warn,
                            LogLevel.Error => AppPrintLevel.Error,
                            LogLevel.Critical => AppPrintLevel.Critical,
                            _ => AppPrintLevel.Information
                        },
                        Title= title,
                        Type= AppPrintConstType.SYSTEM_TYPE
                    });
            }

            private AppLogFilterContext CreateFilterContext<TState>(LogLevel logLevel, EventId eventId, TState state, string content)
            {
                var httpContext = AppCore.HttpContext;
                return new AppLogFilterContext
                {
                    CategoryName = _categoryName,
                    LogLevel = logLevel,
                    EventId = eventId,
                    State = state,
                    Content = content,
                    RequestPath = httpContext?.Request?.Path.Value ?? string.Empty,
                    RequestMethod = httpContext?.Request?.Method ?? string.Empty,
                    StatusCode = httpContext?.Response?.StatusCode
                };
            }

            /// <summary>
            /// 提取 Title（优先用自定义Title，无则用日志分类）
            /// </summary>
            private string GetTitle(string categoryName, object state)
            {
                // 场景1：如果是你自己代码里的日志（带 Title），这里需要你按自己的日志结构提取
                // 示例：如果你用的是 `logger.LogInformation("Title:自定义标题;Content:日志内容")`，可解析这里
                // 以下是通用处理：如果是系统组件日志，用分类名简化作为 Title；如果是自定义日志，提取 Title
                if (state is IEnumerable<KeyValuePair<string, object>> properties)
                {
                    foreach (var prop in properties)
                    {
                        // 如果你日志中包含 "Title" 键，直接提取（根据你实际的日志结构调整）
                        if (prop.Key.Equals("Title", StringComparison.OrdinalIgnoreCase))
                        {
                            return prop.Value?.ToString() ?? categoryName;
                        }
                    }
                }

                // 场景2：系统组件日志（如 Kestrel、Hosting.Lifetime），简化分类名作为 Title
                return SimplifyCategoryName(categoryName);
            }

            /// <summary>
            /// 简化系统组件的分类名（避免 Title 过长）
            /// </summary>
            private string SimplifyCategoryName(string categoryName)
            {
                return categoryName switch
                {
                    "Microsoft.AspNetCore.Server.Kestrel" => "Kestrel服务",
                    "Microsoft.Hosting.Lifetime" => "宿主生命周期",
                    "Microsoft.AspNetCore.Watch.BrowserRefresh.BlazorWasmHotReloadMiddleware" => "Blazor热重载中间件",
                    "Microsoft.AspNetCore.Watch.BrowserRefresh.BrowserScriptMiddleware" => "浏览器脚本中间件",
                    "Microsoft.AspNetCore.Watch.BrowserRefresh.BrowserRefreshMiddleware" => "浏览器刷新中间件",
                    "System.Net.Http.HttpClient.Default.LogicalHandler" => "HttpClient-逻辑处理",
                    "System.Net.Http.HttpClient.Default.ClientHandler" => "HttpClient-客户端处理",
                    "System.Net.Http.HttpClient.LogicalHandler" => "HttpClient-逻辑处理",
                    "System.Net.Http.HttpClient.ClientHandler" => "HttpClient-客户端处理",
                    _ => categoryName // 其他分类保留原名
                };
            }
            /// <summary>
            /// <para>zh-cn:判断指定日志级别是否启用。</para>
            /// <para>en-us:Determines whether the specified log level is enabled.</para>
            /// </summary>
            /// <param name="logLevel">
            /// <para>zh-cn:需要判断的日志级别。</para>
            /// <para>en-us:The log level to check.</para>
            /// </param>
            /// <returns>
            /// <para>zh-cn:如果该级别日志启用则返回 `true`；否则返回 `false`。</para>
            /// <para>en-us:Returns `true` when the log level is enabled; otherwise returns `false`.</para>
            /// </returns>
            public bool IsEnabled(LogLevel logLevel)
            {
                // 启用所有日志级别（可根据需求调整，如只启用 Information 及以上）
                return logLevel != LogLevel.None;
            }

            /// <summary>
            /// <para>zh-cn:开始一个日志作用域。当前实现不记录作用域信息，并返回空释放对象。</para>
            /// <para>en-us:Begins a log scope. The current implementation does not record scope information and returns a no-op disposable object.</para>
            /// </summary>
            /// <typeparam name="TState">
            /// <para>zh-cn:日志作用域状态类型。</para>
            /// <para>en-us:The log scope state type.</para>
            /// </typeparam>
            /// <param name="state">
            /// <para>zh-cn:日志作用域状态。</para>
            /// <para>en-us:The log scope state.</para>
            /// </param>
            /// <returns>
            /// <para>zh-cn:用于结束作用域的释放对象。</para>
            /// <para>en-us:A disposable object used to end the scope.</para>
            /// </returns>
            public IDisposable BeginScope<TState>(TState state)
            {
                // 不需要日志范围，返回空 disposable
                return NullDisposable.Instance;
            }

            private class NullDisposable : IDisposable
            {
                public static NullDisposable Instance = new NullDisposable();
                public void Dispose() { }
            }
        }

        /// <summary>
        /// <para>zh-cn:提供自定义日志配置扩展方法，用于将 Air.Cloud 控制台日志提供器注册到日志构建器。</para>
        /// <para>en-us:Provides custom logging configuration extension methods used to register the Air.Cloud console logger provider on a logging builder.</para>
        /// </summary>
        public static class CustomLoggingExtensions
        {
            /// <summary>
            /// <para>zh-cn:清除默认日志提供器并添加 Air.Cloud 自定义控制台日志提供器。</para>
            /// <para>en-us:Clears default logging providers and adds the Air.Cloud custom console logger provider.</para>
            /// </summary>
            /// <param name="builder">
            /// <para>zh-cn:日志构建器。</para>
            /// <para>en-us:The logging builder.</para>
            /// </param>
            /// <returns>
            /// <para>zh-cn:完成配置后的日志构建器。</para>
            /// <para>en-us:The logging builder after configuration.</para>
            /// </returns>
            public static ILoggingBuilder AddCustomConsole(this ILoggingBuilder builder)
            {
                // 清除默认控制台日志，避免格式冲突
                builder.ClearProviders();
                // 添加自定义日志提供器
                builder.Services.AddOptions<AppLogFilterOptions>();
                builder.Services.TryAddSingleton<IAppLogFilterPlugin, DefaultAppLogFilterPlugin>();
                builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, AppLogProvider>());
                return builder;
            }
        }
}
