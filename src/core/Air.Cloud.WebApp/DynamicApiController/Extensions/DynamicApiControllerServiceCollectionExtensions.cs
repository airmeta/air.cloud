// Copyright (c) 2020-2022 百小僧, Baiqian Co.,Ltd.
// Furion is licensed under Mulan PSL v2.
// You can use this software according to the terms and conditions of the Mulan PSL v2.
// You may obtain a copy of Mulan PSL v2 at:
//             https://gitee.com/dotnetchina/Furion/blob/master/LICENSE
// THIS SOFTWARE IS PROVIDED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO NON-INFRINGEMENT, MERCHANTABILITY OR FIT FOR A PARTICULAR PURPOSE.
// See the Mulan PSL v2 for more details.

using Air.Cloud.Core.Extensions;
using Air.Cloud.WebApp.DynamicApiController.Conventions;
using Air.Cloud.WebApp.DynamicApiController.Options;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

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

        // 配置 Mvc 选项
        services.Configure<MvcOptions>(options =>
        {
            // 添加应用模型转换器
            options.Conventions.Add(new DynamicApiControllerApplicationModelConvention());
        });

        return services;
    }
}