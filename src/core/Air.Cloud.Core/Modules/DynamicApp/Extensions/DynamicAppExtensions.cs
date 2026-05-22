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
using Air.Cloud.Core;
using Air.Cloud.Core.Enhances;
using Air.Cloud.Core.Modules;
using Air.Cloud.Core.Modules.DynamicApp;
using Air.Cloud.Core.Modules.DynamicApp.Attributes;
using Air.Cloud.Core.Modules.DynamicApp.Enums;
using Air.Cloud.Core.Modules.DynamicApp.Model;
using Air.Cloud.Core.Modules.DynamicApp.Provider;
using Air.Cloud.Core.Plugins;
using Air.Cloud.Core.Standard;
using Air.Cloud.Core.Standard.DataBase.Model;
using Air.Cloud.Core.Standard.DynamicServer;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

using System.Reflection;
/// <summary>
/// <para>zh-cn:动态应用扩展类</para>
/// <para>en-us: Dynamic Application Extension Class</para>
/// </summary>
public static class DynamicAppExtensions
{
    /// <summary>
    /// <para>zh-cn:模组扫描类型</para>
    /// <para>en-us: Module Execute Type</para>
    /// </summary>
    static Func<Type, bool> ModuleScanningType = (t =>
    {
        var instances = t.GetInterfaces();
        if (
            (instances.Contains(typeof(IPlugin))
            || instances.Contains(typeof(IModule))
            || instances.Contains(typeof(IEnhance))
            || instances.Contains(typeof(IStandard))
            || instances.Contains(typeof(IDynamicService))
            || instances.Contains(typeof(IPrivateEntity))
        ) && t.IsPublic && !t.IsDefined(typeof(IgnoreScanningAttribute), false))
            return true;
        if (t.IsDefined(typeof(NeedScanningAttribute)))
            return true;
        return false;
    });

    /// <summary>
    /// <para>zh-cn:插件扫描类型</para>
    /// <para>en-us: Plugin Execute Type</para>
    /// </summary>
    static Func<Type, bool> PluginScanningType = (t =>
    {
        if (t.IsDefined(typeof(PluginInjectAttribute)))
        {
            return true;
        }
        var instances = t.GetInterfaces();
        if (instances.Contains(typeof(IAsyncActionFilter)) || t.Name.EndsWith("MiddleWare"))
        {
            return true;
        }
        return false;
    });

    private static DynamicAppInformation CreateDynamicAppInformation(Type type, AssemblyName assemblyName)
    {
        if (type.IsDefined(typeof(PluginInjectAttribute)))
        {
            var attr = type.GetCustomAttribute<PluginInjectAttribute>();
            return new DynamicAppInformation
            {
                OrderType = attr.OrderType,
                OrderNumber = attr.Order,
                Usage = attr.Usage,
                Type = type,
                Assembly = assemblyName
            };
        }

        var instances = type.GetInterfaces();
        if (instances.Contains(typeof(IAsyncActionFilter)))
        {
            return new DynamicAppInformation
            {
                OrderType = DynamicPluginOrder.Earliest,
                OrderNumber = 0,
                Usage = DynamicAppUsage.Filter,
                Type = type,
                Assembly = assemblyName
            };
        }

        if (type.Name.EndsWith("MiddleWare"))
        {
            return new DynamicAppInformation
            {
                OrderType = DynamicPluginOrder.Earliest,
                OrderNumber = 0,
                Usage = DynamicAppUsage.Middleware,
                Type = type,
                Assembly = assemblyName
            };
        }

        return null;
    }

    /// <summary>
    /// <para>zh-cn:使用动态模组</para>
    /// <para>en-us: Use Dynamic Modules</para>
    /// </summary>
    /// <param name="services">
    ///  <para>zh-cn:服务集合</para>
    ///  <para>en-us: Service Collection</para>
    /// </param>
    /// <returns>
    ///  <para>zh-cn:返回服务集合</para>
    ///  <para>en-us: Returns the Service Collection</para>
    /// </returns>
    private static IServiceCollection UseDynamicModules(IServiceCollection services)
    {
        //扫描 加载模组
        var modules = AppRealization.DynamicAppStore.TryLoadApplication(DynamicAppType.Mod);
        var moduleCrucialTypes = new List<Type>();
        var moduleDynamicAppInfos = new List<DynamicAppInformation>();

        foreach (var module in modules)
        {
            //扫描出所有的控制器 中间件 拦截器
            var loadContext = AppRealization.DynamicAppStore.GetAssemblyLoadContext(module);
            var moduleAssemblies = loadContext.Assemblies.ToList();
            foreach (var assembly in moduleAssemblies)
            {
                var types = assembly.LoadTypes(ModuleScanningType).ToList();
                if (types.Count == 0)
                {
                    continue;
                }

                moduleCrucialTypes.AddRange(types);
                foreach (var type in types)
                {
                    var dynamicAppInformation = CreateDynamicAppInformation(type, assembly.GetName());
                    if (dynamicAppInformation != null)
                    {
                        moduleDynamicAppInfos.Add(dynamicAppInformation);
                    }
                }
            }
        }

        var publishedModuleCrucialTypes = moduleCrucialTypes.ToList();
        AppCore.CrucialTypes = AppCore.CrucialTypes.Union(publishedModuleCrucialTypes).ToList();
        AppCore.AppExternal.ExternalModuleCrucialTypes = publishedModuleCrucialTypes;
        IDynamicAppStoreStandard.DynamicAppScanningResult = IDynamicAppStoreStandard.DynamicAppScanningResult
            .Concat(moduleDynamicAppInfos)
            .ToList();

        return services;
    }

    /// <summary>
    /// <para>zh-cn:使用动态插件</para>
    /// <para>en-us: Use Dynamic Plugins</para>
    /// </summary>
    /// <param name="services">
    ///  <para>zh-cn:服务集合</para>
    ///  <para>en-us: Service Collection</para>
    /// </param>
    /// <returns>
    ///  <para>zh-cn:返回服务集合</para>
    ///  <para>en-us: Returns the Service Collection</para>
    /// </returns>
    private static IServiceCollection UseDynamicPlugins(IServiceCollection services)
    {
        //扫描 加载插件
        var plugins = AppRealization.DynamicAppStore.TryLoadApplication(DynamicAppType.Plugin);
        var pluginCrucialTypes = new List<Type>();
        var pluginDynamicAppInfos = new List<DynamicAppInformation>();

        foreach (var plugin in plugins)
        {
            //扫描出所有的控制器 中间件 拦截器
            var loadContext = AppRealization.DynamicAppStore.GetAssemblyLoadContext(plugin);
            foreach (var assembly in loadContext.Assemblies.ToList())
            {
                var types = assembly.LoadTypes(PluginScanningType).ToList();
                if (types.Count == 0)
                {
                    continue;
                }

                pluginCrucialTypes.AddRange(types);
                foreach (var type in types)
                {
                    var dynamicAppInformation = CreateDynamicAppInformation(type, assembly.GetName());
                    if (dynamicAppInformation != null)
                    {
                        pluginDynamicAppInfos.Add(dynamicAppInformation);
                    }
                }
            }
        }

        AppCore.AppExternal.ExternalPluginCrucialTypes = pluginCrucialTypes.ToList();
        IDynamicAppStoreStandard.DynamicAppScanningResult = IDynamicAppStoreStandard.DynamicAppScanningResult
            .Concat(pluginDynamicAppInfos)
            .ToList();

        return services;
    }


    /// <summary>
    /// <para>zh-cn:添加动态应用支持</para>
    /// <para>en-us: Add dynamic application support</para>
    /// </summary>
    /// <param name="services">
    ///  <para>zh-cn:服务集合</para>
    ///  <para>en-us: Service Collection</para>
    /// </param>
    /// <returns>
    ///  <para>zh-cn:返回服务集合</para>
    ///  <para>en-us: Returns the Service Collection</para>
    /// </returns>
    public static IServiceCollection AddDynamicApp(this IServiceCollection services)
    {

        UseDynamicModules(services);

        UseDynamicPlugins(services);

        services.AddDynamicAppPart();

        return services;

    }

    /// <summary>
    /// <para>zh-cn:添加动态应用部件</para>
    /// <para>en-us: Add dynamic application parts</para>
    /// </summary>
    /// <param name="services">
    ///  <para>zh-cn:服务集合</para>
    ///  <para>en-us: Service Collection</para>
    /// </param>
    /// <returns>
    ///  <para>zh-cn:返回服务集合</para>
    ///  <para>en-us: Returns the Service Collection</para>
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///  <para>zh-cn:如果未能找到 ApplicationPartManager 实例，则抛出此异常，提示必须在调用 AddControllers 之后调用此方法。</para>
    ///  <para>en-us: If the ApplicationPartManager instance cannot be found, this exception is thrown, indicating that this method must be called after AddControllers.</para>
    /// </exception>
    private static IServiceCollection AddDynamicAppPart(this IServiceCollection services)
    {
        var partManager = services.FirstOrDefault(s => s.ServiceType == typeof(ApplicationPartManager))?.ImplementationInstance as ApplicationPartManager
            ?? throw new InvalidOperationException($"`{nameof(AddDynamicAppPart)}` must be invoked after `{nameof(MvcServiceCollectionExtensions.AddControllers)}`.");

        AppCore.ApplicationPart = partManager;

        AppRealization.DynamicAppStore.InjectDynamicAppPartManager().FeatureProviders.Add(new DynamicApiControllerFeatureProvider());

        return services;
    }


    /// <summary>
    /// <para>zh-cn:使用动态应用中间件</para>
    /// <para>en-us: Use Dynamic Application Middleware</para>
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseDynamicAppMiddleware(this IApplicationBuilder app)
    {
        var dynamicAppScanningResults = IDynamicAppStoreStandard.DynamicAppScanningResult
            .OrderBy(s => s.OrderNumber)
            .ThenBy(s => s.OrderType)
            .ToList();

        var middlewares = dynamicAppScanningResults
            .Where(s => s.Usage == DynamicAppUsage.Middleware)
            .ToList();

        foreach (var item in middlewares)
        {
            var UseMiddlewareMethod = typeof(IApplicationBuilder).GetMethod("UseMiddleware").MakeGenericMethod(item.Type);

            UseMiddlewareMethod.Invoke(app, null);
        }
        return app;
    }
    /// <summary>
    /// <para>zh-cn:添加应用控制器</para>
    /// <para>en-us: Add Application Controllers</para>
    /// </summary>
    /// <remarks>
    ///  <para>zh-cn:此方法是适配动态应用程序的控制器方法</para>
    ///  <para>en-us: This method is an adaptation of the controller method for dynamic applications</para>
    /// </remarks>
    /// <param name="services">
    /// <para>zh-cn:服务集合</para>
    /// <para>en-us: Service Collection</para>
    /// </param>
    /// <param name="configure">
    ///  <para>zh-cn:配置选项</para>
    ///  <para>en-us: Configuration options</para>
    /// </param>
    /// <returns>
    ///  <para>zh-cn:返回 MVC 构建器</para>
    ///  <para>en-us: Returns the MVC Builder</para>
    /// </returns>
    public static IMvcBuilder AddAppControllers(this IServiceCollection services, Action<MvcOptions> configure = null)
    {
        return services.AddControllers(s =>
        {
            var dynamicAppScanningResults = IDynamicAppStoreStandard.DynamicAppScanningResult.ToList();

            var earliestFilters = dynamicAppScanningResults
                         .Where(s => s.Usage == DynamicAppUsage.Filter
                                     && s.OrderType == DynamicPluginOrder.Earliest)
                         .OrderBy(s => s.OrderNumber)
                         .ThenBy(s => s.OrderType)
                         .ToList();
            foreach (var item in earliestFilters)
            {
                var AddFilterMethod = typeof(FilterCollection).GetMethod("Add").MakeGenericMethod(item.Type);
                AddFilterMethod.Invoke(s.Filters, null);
            }
            configure?.Invoke(s);
            var latestFilters = dynamicAppScanningResults
               .Where(s => s.Usage == DynamicAppUsage.Filter
                           && s.OrderType == DynamicPluginOrder.Latest)
               .OrderBy(s => s.OrderNumber)
               .ThenBy(s => s.OrderType)
               .ToList();
            foreach (var item in latestFilters)
            {
                var AddFilterMethod = typeof(FilterCollection).GetMethod("Add").MakeGenericMethod(item.Type);
                AddFilterMethod.Invoke(s.Filters, null);
            }
        });
    }
}
