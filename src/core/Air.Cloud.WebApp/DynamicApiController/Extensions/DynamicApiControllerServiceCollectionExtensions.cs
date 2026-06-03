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
using Air.Cloud.WebApp.DynamicApiController.Conventions;
using Air.Cloud.WebApp.DynamicApiController.Internal;
using Air.Cloud.WebApp.DynamicApiController.Options;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Air.Cloud.WebApp.DynamicApiController.Extensions;


/// <summary>
/// <para>zh-cn:动态接口控制器服务拓展类</para>
/// <para>en-us: Dynamic API Controller Service Extension Class</para>
/// </summary>
[IgnoreScanning]
public static class DynamicApiControllerServiceCollectionExtensions
{
    /// <summary>
    /// <para>zh-cn:添加动态接口控制器服务</para>
    /// <para>en-us: Add Dynamic API Controller Services</para>
    /// </summary>
    /// <param name="mvcBuilder">
    ///  <para>zh-cn:MVC 构建器</para>
    ///  <para>en-us: MVC Builder</para>
    /// </param>
    /// <returns>
    ///  <para>zh-cn:返回 MVC 构建器</para>
    ///  <para>en-us: Returns the MVC Builder</para>
    /// </returns>
    public static IMvcBuilder AddDynamicApiControllers(this IMvcBuilder mvcBuilder)
    {
        mvcBuilder.Services.AddDynamicApiControllers();

        return mvcBuilder;
    }

    /// <summary>
    /// <para>zh-cn:添加动态接口控制器服务</para>
    /// <para>en-us: Add Dynamic API Controller Services</para>
    /// </summary>
    /// <param name="services">
    ///  <para>zh-cn:服务集合</para>
    ///  <para>en-us: Service Collection</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:返回服务集合</para>
    /// <para>en-us: Returns the Service Collection</para>
    /// </returns>
    public static IServiceCollection AddDynamicApiControllers(this IServiceCollection services)
    {
        //装配动态应用服务
        services.AddDynamicApp();

        // 添加配置
        services.AddConfigurableOptions<DynamicApiControllerSettingsOptions>();

        services.TryAddTransient<DynamicApiVerbMap>();
        services.TryAddTransient<DynamicApiNameResolver>();
        services.TryAddTransient<DynamicApiHttpMethodResolver>();
        services.TryAddTransient<DynamicApiParameterBinder>();
        services.TryAddTransient<DynamicApiRouteBuilder>();
        services.TryAddTransient<DynamicApiUnifyMetadataContributor>();
        services.TryAddTransient<DynamicApiProbeMetadataContributor>();
        services.TryAddTransient<DynamicApiControllerApplicationModelConvention>();

        services.AddTransient<IConfigureOptions<MvcOptions>, DynamicApiControllerMvcOptionsSetup>();

        return services;
    }
}
