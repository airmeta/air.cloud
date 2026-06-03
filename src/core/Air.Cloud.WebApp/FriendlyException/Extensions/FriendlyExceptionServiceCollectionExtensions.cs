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

using Air.Cloud.Core.Extensions;
using Air.Cloud.WebApp.Extensions;
using Air.Cloud.WebApp.FriendlyException.Extensions.Options;
using Air.Cloud.WebApp.FriendlyException.Filters;
using Air.Cloud.WebApp.FriendlyException.Options;
using Air.Cloud.WebApp.FriendlyException.Providers;

using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.WebApp.FriendlyException.Extensions;

/// <summary>
/// 友好异常服务拓展类
/// </summary>
[IgnoreScanning]
public static class FriendlyExceptionServiceCollectionExtensions
{
    /// <summary>
    /// 添加友好异常服务拓展服务
    /// </summary>
    /// <typeparam name="TErrorCodeTypeProvider">异常错误码提供器</typeparam>
    /// <param name="mvcBuilder">Mvc构建器</param>
    /// <param name="configure">是否启用全局异常过滤器</param>
    /// <returns></returns>
    public static IMvcBuilder AddFriendlyException<TErrorCodeTypeProvider>(this IMvcBuilder mvcBuilder, Action<FriendlyExceptionServiceOptions> configure = null)
        where TErrorCodeTypeProvider : class, IErrorCodeTypeProvider
    {
        mvcBuilder.Services.AddFriendlyException<TErrorCodeTypeProvider>(configure);

        return mvcBuilder;
    }

    /// <summary>
    /// 添加友好异常服务拓展服务
    /// </summary>
    /// <typeparam name="TErrorCodeTypeProvider">异常错误码提供器</typeparam>
    /// <param name="services"></param>
    /// <param name="configure"></param>
    /// <returns></returns>
    public static IServiceCollection AddFriendlyException<TErrorCodeTypeProvider>(this IServiceCollection services, Action<FriendlyExceptionServiceOptions> configure = null)
        where TErrorCodeTypeProvider : class, IErrorCodeTypeProvider
    {
        // 添加全局异常过滤器
        services.AddFriendlyException(configure);

        // 单例注册异常状态码提供器
        services.AddSingleton<IErrorCodeTypeProvider, TErrorCodeTypeProvider>();

        return services;
    }

    /// <summary>
    /// 添加友好异常服务拓展服务
    /// </summary>
    /// <param name="mvcBuilder">Mvc构建器</param>
    /// <param name="configure"></param>
    /// <returns></returns>
    public static IMvcBuilder AddFriendlyException(this IMvcBuilder mvcBuilder, Action<FriendlyExceptionServiceOptions> configure = null)
    {
        mvcBuilder.Services.AddFriendlyException(configure);

        return mvcBuilder;
    }

    /// <summary>
    /// 添加友好异常服务拓展服务
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configure"></param>
    /// <returns></returns>
    public static IServiceCollection AddFriendlyException(this IServiceCollection services, Action<FriendlyExceptionServiceOptions> configure = null)
    {
        // 添加友好异常配置文件支持
        services.AddConfigurableOptions<FriendlyExceptionSettingsOptions>();

        // 添加异常配置文件支持
        services.AddConfigurableOptions<ErrorCodeMessageSettingsOptions>();

        // 载入服务配置选项
        var configureOptions = new FriendlyExceptionServiceOptions();
        configure?.Invoke(configureOptions);

        // 添加全局异常过滤器
        if (configureOptions.EnabledGlobalFriendlyException)
            services.AddMvcFilter<FriendlyExceptionFilter>();

        return services;
    }
}
