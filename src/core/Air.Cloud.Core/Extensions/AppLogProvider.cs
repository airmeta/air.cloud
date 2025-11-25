<<<<<<< HEAD
﻿/*
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
using Microsoft.Extensions.Logging;
=======
﻿using Microsoft.Extensions.Logging;
>>>>>>> aeba4aab7dcf969688fd35ab1ea3ac980b15307d
namespace Air.Cloud.Core.Extensions
{
        /// <summary>
        /// 自定义控制台日志提供器（负责创建日志实例）
        /// </summary>
        public class AppLogProvider : ILoggerProvider
        {
            public ILogger CreateLogger(string categoryName)
            {
                // categoryName 是日志分类（如 Microsoft.AspNetCore.Server.Kestrel），作为 Title 一部分
                return new CustomConsoleLogger(categoryName);
            }

            public void Dispose()
            {
                // 无需释放资源，空实现
            }
        }

        /// <summary>
        /// 自定义控制台日志（实现具体格式化逻辑）
        /// </summary>
        public class CustomConsoleLogger : ILogger
        {
            private readonly string _categoryName;

            public CustomConsoleLogger(string categoryName)
            {
                _categoryName = categoryName;
            }

            /// <summary>
            /// 核心格式化方法：所有日志都会经过这里输出
            /// </summary>
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                    // 1. 过滤掉不需要的日志级别（可选，如只输出 Information 及以上）
                    if (!IsEnabled(logLevel))
                        return;
                    var title = GetTitle(_categoryName, state); // 提取/生成 Title
                    var content = formatter(state, exception); // 日志内容（包含异常信息）
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
            public bool IsEnabled(LogLevel logLevel)
            {
                // 启用所有日志级别（可根据需求调整，如只启用 Information 及以上）
                return logLevel >= LogLevel.Debug;
            }

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
        /// 扩展方法：简化日志配置
        /// </summary>
        public static class CustomLoggingExtensions
        {
            public static ILoggingBuilder AddCustomConsole(this ILoggingBuilder builder)
            {
                // 清除默认控制台日志，避免格式冲突
                builder.ClearProviders();
                // 添加自定义日志提供器
                builder.AddProvider(new AppLogProvider());
                return builder;
            }
        }
}
