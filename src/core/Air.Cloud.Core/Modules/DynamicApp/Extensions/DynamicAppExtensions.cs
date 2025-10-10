using Air.Cloud.Core;
using Air.Cloud.Core.Enhance;
using Air.Cloud.Core.Modules;
using Air.Cloud.Core.Modules.DynamicApp;
using Air.Cloud.Core.Modules.DynamicApp.Attributes;
using Air.Cloud.Core.Modules.DynamicApp.Model;
using Air.Cloud.Core.Plugins;
using Air.Cloud.Core.Standard;
using Air.Cloud.Core.Standard.DataBase.Model;
using Air.Cloud.Core.Standard.DynamicServer;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

using System.Reflection;

public static  class DynamicAppExtensions
{
    //需要解决接口 控制器 中间件 拦截器 的 动态加载问题 将其挂载到主程序中
    //  模组将会忽略启动程序集中的program.cs类,继承了AppStartup的startup.cs类(因为如果加载可能会导致主程序崩溃)
    //  每个模组都增加basepath 用来区分各个模组的接口路由地址 这样可以根据不通的basepath来应用不同的中间件和拦截器  如果只有一个模组 并且basepath为空或者/ 则表示应用到全局

    //调整目录接口 增加lib文件夹 和 ModInfo.xml 配置文件
    // ModInfo.xml 暂定以下配置:
    //    1. 模组名称
    //    2. 模组版本
    //    3. 模组描述   
    //    4. 模组运行端口
    //    5. 模组BasePath
    //    6. 模组接口Dll名称
    //    7. 模组启动Dll名称
    // lib文件夹存放动态加载的dll ModInfo.xml 配置文件存放动态应用的配置信息
    // 需要单独为主程序提供一个启动方法 用来扫描 并加载 多个动态应用的配置文件

    //需要增加插件的挂载 现在扫描的是模组 插件类型与模组不一样 扫描到的参数 直接应用到全局即可 插件只关注拦截器 中间件

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


    static Func<Type, bool> PluginScanningType = (t =>
    {
        if (t.IsDefined(typeof(PluginInjectAttribute)))
        {
            return true;
        }
        var instances = t.GetInterfaces();
        if (instances.Contains(typeof(IAsyncActionFilter))|| t.Name.EndsWith("MiddleWare"))
        {
            return true;
        }
        return false;
    });

    private  static IServiceCollection UseDynamicModules(IServiceCollection services)
    {
        //扫描 加载模组
        var Modules = AppRealization.DynamicAppLoader.TryLoadModules();
        IEnumerable<Type> AllModuleCrucialTypes = new List<Type>();
        foreach (var module in Modules)
        {
            //扫描出所有的控制器 中间件 拦截器
            var loadContext = AppRealization.DynamicAppLoader.GetAssemblyLoadContext(module);
            var ModuleAssemblies = loadContext.Assemblies.ToList();
            ModuleAssemblies.ForEach(a =>
            {
                var types = a.LoadTypes(ModuleScanningType).ToList();
                //将扫描到的类型插入到核心类型集合中
                AppCore.CrucialTypes = AppCore.CrucialTypes.Union(types);
                AllModuleCrucialTypes = AllModuleCrucialTypes.Union(types);
                //还需要加载filter和middleware
                var FilterMiddlewareAndPluginUseAttr = a.LoadTypes(ModuleScanningType).ToList();
                if (FilterMiddlewareAndPluginUseAttr.Count>0)
                {
                    foreach (var type in FilterMiddlewareAndPluginUseAttr)
                    {
                        if (type.IsDefined(typeof(PluginInjectAttribute)))
                        {
                            var attr = type.GetCustomAttribute<PluginInjectAttribute>();
                            IDynamicAppLoaderStandard.DynamicAppScanningResult.Add(new DynamicAppInformation
                            {
                                OrderType = attr.OrderType,
                                OrderNumber = attr.Order,
                                Usage = attr.Usage,
                                Type = type,
                                Assembly = a.GetName()
                            });
                        }
                        var instances = type.GetInterfaces();
                        if (instances.Contains(typeof(IAsyncActionFilter)))
                        {
                            //过滤器
                            IDynamicAppLoaderStandard.DynamicAppScanningResult.Add(new DynamicAppInformation
                            {
                                OrderType = Air.Cloud.Core.Modules.DynamicApp.Enums.PluginOrderEnum.Earliest,
                                OrderNumber = 0,
                                Usage = Air.Cloud.Core.Modules.DynamicApp.Enums.DynamicAppUsageEnum.Filter,
                                Type = type,
                                Assembly = a.GetName()
                            });
                        }
                        if (type.Name.EndsWith("MiddleWare"))
                        {
                            //中间件
                            IDynamicAppLoaderStandard.DynamicAppScanningResult.Add(new DynamicAppInformation
                            {
                                OrderType = Air.Cloud.Core.Modules.DynamicApp.Enums.PluginOrderEnum.Earliest,
                                OrderNumber = 0,
                                Usage = Air.Cloud.Core.Modules.DynamicApp.Enums.DynamicAppUsageEnum.Middleware,
                                Type = type,
                                Assembly = a.GetName()
                            });
                        }
                    }

                    AllModuleCrucialTypes = AllModuleCrucialTypes.Union(FilterMiddlewareAndPluginUseAttr).ToList();
                }
           
            });
        }
        AppCore.AppExternal.ExternalModuleCrucialTypes = AllModuleCrucialTypes;
        return services;
    }

    private static IServiceCollection UseDynamicPlugins(IServiceCollection services)
    {
        //扫描 加载插件
        var Plugins = AppRealization.DynamicAppLoader.TryLoadPlugins();
        IList<Type> AllPluginCrucialTypes = new List<Type>();
        foreach (var plugin in Plugins)
        {
            //扫描出所有的控制器 中间件 拦截器
            var loadContext = AppRealization.DynamicAppLoader.GetAssemblyLoadContext(plugin);
            loadContext.Assemblies.ToList().ForEach(a =>
            {
                var types = a.LoadTypes(PluginScanningType).ToList();
                foreach (var type in types)
                {
                    if (type.IsDefined(typeof(PluginInjectAttribute)))
                    {
                        var attr = type.GetCustomAttribute<PluginInjectAttribute>();
                        IDynamicAppLoaderStandard.DynamicAppScanningResult.Add(new DynamicAppInformation
                        {
                            OrderType = attr.OrderType,
                            OrderNumber = attr.Order,
                            Usage = attr.Usage,
                            Type = type,
                            Assembly = a.GetName()
                        });
                    }
                    else
                    {
                        var instances = type.GetInterfaces();
                        if (instances.Contains(typeof(IAsyncActionFilter)))
                        {
                            //过滤器
                            IDynamicAppLoaderStandard.DynamicAppScanningResult.Add(new DynamicAppInformation
                            {
                                OrderType = Air.Cloud.Core.Modules.DynamicApp.Enums.PluginOrderEnum.Earliest,
                                OrderNumber = 0,
                                Usage = Air.Cloud.Core.Modules.DynamicApp.Enums.DynamicAppUsageEnum.Filter,
                                Type = type,
                                Assembly = a.GetName()
                            });
                        }
                        else
                        {
                            //中间件
                            IDynamicAppLoaderStandard.DynamicAppScanningResult.Add(new DynamicAppInformation
                            {
                                OrderType = Air.Cloud.Core.Modules.DynamicApp.Enums.PluginOrderEnum.Earliest,
                                OrderNumber = 0,
                                Usage = Air.Cloud.Core.Modules.DynamicApp.Enums.DynamicAppUsageEnum.Middleware,
                                Type = type,
                                Assembly = a.GetName()
                            });
                        }
                    }
                }
                AllPluginCrucialTypes = AllPluginCrucialTypes.Union(types).ToList();
            });
        }
        AppCore.AppExternal.ExternalPluginCrucialTypes = AllPluginCrucialTypes;
        return services;
    }


    public static IServiceCollection AddDynamicApp(this IServiceCollection services)
    {

        UseDynamicModules(services);

        UseDynamicPlugins(services);

        return services;

    }

    public static IApplicationBuilder UseDynamicApp(this IApplicationBuilder app)
    {
        IDynamicAppLoaderStandard.DynamicAppScanningResult = IDynamicAppLoaderStandard.DynamicAppScanningResult.OrderBy(s => s.OrderNumber).ThenBy(s => s.OrderType).ToList();

        var Middlewares = IDynamicAppLoaderStandard.DynamicAppScanningResult.Where(s => s.Usage == Air.Cloud.Core.Modules.DynamicApp.Enums.DynamicAppUsageEnum.Middleware).ToList();

        foreach (var item in Middlewares)
        {
            var UseMiddlewareMethod = typeof(IApplicationBuilder).GetMethod("UseMiddleware").MakeGenericMethod(item.Type);

            UseMiddlewareMethod.Invoke(app, null);
        }
        return app;
    }

    public static IMvcBuilder AddAppControllers(this IServiceCollection services, Action<MvcOptions> configure = null)
    {
        return services.AddControllers(s =>
        {
            var EarliestFilters = IDynamicAppLoaderStandard.DynamicAppScanningResult
                         .Where(s => s.Usage == Air.Cloud.Core.Modules.DynamicApp.Enums.DynamicAppUsageEnum.Filter
                                     && s.OrderType == Air.Cloud.Core.Modules.DynamicApp.Enums.PluginOrderEnum.Earliest)
                         .OrderBy(s => s.OrderNumber)
                         .ThenBy(s => s.OrderType)
                         .ToList();
            foreach (var item in EarliestFilters)
            {
                var AddFilterMethod = typeof(FilterCollection).GetMethod("Add").MakeGenericMethod(item.Type);
                AddFilterMethod.Invoke(s.Filters, null);
            }
            configure.Invoke(s);
            var LatestFilters = IDynamicAppLoaderStandard.DynamicAppScanningResult
               .Where(s => s.Usage == Air.Cloud.Core.Modules.DynamicApp.Enums.DynamicAppUsageEnum.Filter
                           && s.OrderType == Air.Cloud.Core.Modules.DynamicApp.Enums.PluginOrderEnum.Latest)
               .OrderBy(s => s.OrderNumber)
               .ThenBy(s => s.OrderType)
               .ToList();
            foreach (var item in LatestFilters)
            {
                var AddFilterMethod = typeof(FilterCollection).GetMethod("Add").MakeGenericMethod(item.Type);
                AddFilterMethod.Invoke(s.Filters, null);
            }
        });
    }
}