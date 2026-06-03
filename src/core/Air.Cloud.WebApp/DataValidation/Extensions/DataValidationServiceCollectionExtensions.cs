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
using Air.Cloud.WebApp.DataValidation.Extensions.Options;
using Air.Cloud.WebApp.DataValidation.Filters;
using Air.Cloud.WebApp.DataValidation.Options;
using Air.Cloud.WebApp.DataValidation.Providers;
using Air.Cloud.WebApp.Extensions;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;


namespace Air.Cloud.WebApp.DataValidation.Extensions;

/// <summary>
/// 友好异常服务拓展类
/// </summary>
[IgnoreScanning]
public static class DataValidationServiceCollectionExtensions
{
    /// <summary>
    /// 添加全局数据验证
    /// </summary>
    /// <typeparam name="TValidationMessageTypeProvider">验证类型消息提供器</typeparam>
    /// <param name="mvcBuilder"></param>
    /// <param name="configure"></param>
    /// <returns></returns>
    public static IMvcBuilder AddDataValidation<TValidationMessageTypeProvider>(this IMvcBuilder mvcBuilder, Action<DataValidationServiceOptions> configure = null)
        where TValidationMessageTypeProvider : class, IValidationMessageTypeProvider
    {
        // 添加全局数据验证
        mvcBuilder.Services.AddDataValidation<TValidationMessageTypeProvider>(configure);

        return mvcBuilder;
    }

    /// <summary>
    /// 添加全局数据验证
    /// </summary>
    /// <typeparam name="TValidationMessageTypeProvider">验证类型消息提供器</typeparam>
    /// <param name="services"></param>
    /// <param name="configure"></param>
    /// <returns></returns>
    public static IServiceCollection AddDataValidation<TValidationMessageTypeProvider>(this IServiceCollection services, Action<DataValidationServiceOptions> configure = null)
        where TValidationMessageTypeProvider : class, IValidationMessageTypeProvider
    {
        // 添加全局数据验证
        services.AddDataValidation(configure);

        // 单例注册验证消息提供器
        services.AddSingleton<IValidationMessageTypeProvider, TValidationMessageTypeProvider>();

        return services;
    }

    /// <summary>
    /// 添加全局数据验证
    /// </summary>
    /// <param name="mvcBuilder"></param>
    /// <param name="configure"></param>
    /// <returns></returns>
    public static IMvcBuilder AddDataValidation(this IMvcBuilder mvcBuilder, Action<DataValidationServiceOptions> configure = null)
    {
        mvcBuilder.Services.AddDataValidation(configure);

        return mvcBuilder;
    }

    /// <summary>
    /// 添加全局数据验证
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configure"></param>
    /// <returns></returns>
    public static IServiceCollection AddDataValidation(this IServiceCollection services, Action<DataValidationServiceOptions> configure = null)
    {
        // 添加验证配置文件支持
        services.AddConfigurableOptions<ValidationTypeMessageSettingsOptions>();

        // 载入服务配置选项
        var configureOptions = new DataValidationServiceOptions();
        configure?.Invoke(configureOptions);

        // 判断是否启用全局
        if (configureOptions.EnableGlobalDataValidation)
        {
            // 启用了全局验证，则默认关闭原生 ModelStateInvalidFilter 验证
            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.SuppressModelStateInvalidFilter = configureOptions.SuppressModelStateInvalidFilter;
            });

            // 添加全局数据验证
            services.AddMvcFilter<DataValidationFilter>(options =>
            {
                // 关闭空引用对象验证
                options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = configureOptions.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes;
            });
        }

        return services;
    }
}