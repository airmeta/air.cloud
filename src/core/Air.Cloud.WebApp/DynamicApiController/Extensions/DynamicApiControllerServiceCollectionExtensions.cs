// Copyright (c) 2020-2022 百小僧, Baiqian Co.,Ltd.
// Furion is licensed under Mulan PSL v2.
// You can use this software according to the terms and conditions of the Mulan PSL v2.
// You may obtain a copy of Mulan PSL v2 at:
//             https://gitee.com/dotnetchina/Furion/blob/master/LICENSE
// THIS SOFTWARE IS PROVIDED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO NON-INFRINGEMENT, MERCHANTABILITY OR FIT FOR A PARTICULAR PURPOSE.
// See the Mulan PSL v2 for more details.

using Air.Cloud.Core;
using Air.Cloud.Core.App;
using Air.Cloud.Core.Extensions;
using Air.Cloud.Core.Modules;
using Air.Cloud.Core.Plugins.Security.MD5;
using Air.Cloud.Core.Standard.DynamicServer;
using Air.Cloud.WebApp.DynamicApiController.Conventions;
using Air.Cloud.WebApp.DynamicApiController.Options;
using Air.Cloud.WebApp.DynamicApiController.Providers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;

using System.Reflection;

namespace Air.Cloud.WebApp.DynamicApiController.Extensions;

/// <summary>
/// 动态接口控制器拓展类
/// </summary>
[IgnoreScanning]
public static class DynamicApiControllerServiceCollectionExtensions
{
    /// <summary>
    /// 添加动态接口控制器服务
    /// </summary>
    /// <param name="mvcBuilder">Mvc构建器</param>
    /// <returns>Mvc构建器</returns>
    public static IMvcBuilder AddDynamicApiControllers(this IMvcBuilder mvcBuilder)
    {
        mvcBuilder.Services.AddDynamicApiControllers();

        return mvcBuilder;
    }

    /// <summary>
    /// 添加动态接口控制器服务
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddDynamicApiControllers(this IServiceCollection services)
    {
        services.AddDynamicApp();
        var partManager = services.FirstOrDefault(s => s.ServiceType == typeof(ApplicationPartManager))?.ImplementationInstance as ApplicationPartManager
            ?? throw new InvalidOperationException($"`{nameof(AddDynamicApiControllers)}` must be invoked after `{nameof(MvcServiceCollectionExtensions.AddControllers)}`.");
        // 载入模块化部件
        if (AppCore.AppExternal.ExternalModuleAssemblies != null && AppCore.AppExternal.ExternalModuleAssemblies.Any())
        {
            var Services= AppCore.AppExternal.ExternalModuleCrucialTypes.Where(s => s.GetInterfaces().Contains(typeof(IDynamicService))).ToList();

            IDictionary<string,Assembly> assemblies = new Dictionary<string, Assembly>();

            foreach (var item in Services)
            {
                var AssemblyKey = MD5Encryption.GetMd5By32($"{item.Assembly.FullName} {item.Assembly.Location}");
                
                assemblies.TryAdd(AssemblyKey, item.Assembly);
            }
            foreach (var assembly in assemblies.Values)
            {
                if (partManager.ApplicationParts.Any(u => u.Name != assembly.GetName().Name))
                {
                    partManager.ApplicationParts.Add(new AssemblyPart(assembly));
                }
            }
        }
        // 添加控制器特性提供器
        partManager.FeatureProviders.Add(new DynamicApiControllerFeatureProvider());

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