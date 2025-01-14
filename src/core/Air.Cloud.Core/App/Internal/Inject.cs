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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Air.Cloud.Core.App.Internal;

/// <summary>
/// <para>zh-cn:注入</para>
/// <para>en-us:Injection</para>
/// </summary>
public static class Inject
{
    /// <summary>
    /// <para>zh-cn:创建初始服务集合</para>
    /// <para>en-us:Create initial service collection</para>
    /// </summary>
    /// <param name="configureLogging">
    /// <para>zh-cn:日志构建器</para>
    /// <para>en-us:Logging builder</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:返回服务集合</para>
    /// <para>en-us:Return service collection</para>
    /// </returns>
    public static IServiceCollection Create(Action<ILoggingBuilder> configureLogging = default)
    {
        // 监听全局异常
        AppDomain.CurrentDomain.UnhandledException += AppRealization.DomainExceptionHandler.OnException;

        // 创建配置构建器
        var configurationBuilder = new ConfigurationBuilder();

        var configuration = AppConfigurationLoader.Configurations;

        // 创建服务对象和存储服务提供器
        var services =AppCore.InternalServices = new ServiceCollection();

        // 添加默认控制台日志处理程序
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.AddConfiguration(configuration.GetSection("Logging"));
            loggingBuilder.AddConsole(); // 将日志输出到控制台
            configureLogging?.Invoke(loggingBuilder);
        });

        // 初始化应用服务
        services.AddApplication();

        return services;
    }
}