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
using Air.Cloud.Core.App.Options;
using Air.Cloud.Core.App.Startups;
using Air.Cloud.Core.Standard.DynamicServer.Extensions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Air.Cloud.Core.Extensions;

/// <summary>
/// 应用服务集合拓展类（由框架内部调用）
/// </summary>
[IgnoreScanning]
public static class AppServiceCollectionExtensions
{
    /// <summary>
    /// 自动添加主机服务
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddAppHostedService(this IServiceCollection services)
    {
        // 获取所有 BackgroundService 类型，排除泛型主机
        var backgroundServiceTypes = AppCore.EffectiveTypes
            .Where(u => typeof(IHostedService).IsAssignableFrom(u) && u.Name != "GenericWebHostService")
            .ToList();
        var addHostServiceMethod = typeof(ServiceCollectionHostedServiceExtensions).GetMethods(BindingFlags.Static | BindingFlags.Public)
                            .Where(u => u.Name.Equals("AddHostedService") && u.IsGenericMethod && u.GetParameters().Length == 1)
                            .FirstOrDefault();

        foreach (var type in backgroundServiceTypes)
        {
            addHostServiceMethod.MakeGenericMethod(type).Invoke(null, new object[] { services });
        }

        return services;
    }

    /// <summary>
    /// 供控制台构建根服务
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static void Build(this IServiceCollection services)
    {
        var serviceProvider = services.BuildServiceProvider(false);
        // 存储根服务
         //AppCore.RootServices = serviceProvider;
    }

    /// <summary>
    /// 添加应用配置
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configure">服务配置</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services, Action<IServiceCollection> configure = null)
    {
        // 注册全局配置选项
        services.AddConfigurableOptions<AppSettingsOptions>();

        // 注册内存和分布式内存
        services.AddMemoryCache();
        services.AddDistributedMemoryCache();

        // 注册全局依赖注入
        services.AddDependencyInjection();

        // 注册全局 Startup 扫描
        services.AddStartups();

        // 默认内置 GBK，Windows-1252, Shift-JIS, GB2312 编码支持
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        // 自定义服务
        configure?.Invoke(services);
        //进行最终的扫描
        AppRealization.AssemblyScanning.Execute();
        return services;
    }

    /// <summary>
    /// 添加 Startup 自动扫描
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    internal static IServiceCollection AddStartups(this IServiceCollection services)
    {
        // 扫描所有继承 AppStartup 的类
        var startups = SortStartupsByDependency(
            AppCore.StartTypes
                .Where(u => typeof(AppStartup).IsAssignableFrom(u) && u.IsClass && !u.IsAbstract && !u.IsGenericType));

        // 注册自定义 startup
        foreach (var type in startups)
        {
            var startup = Activator.CreateInstance(type) as AppStartup;
            AppCore.AppStartups.Add(startup);

            // 获取所有符合依赖注入格式的方法，如返回值void，且第一个参数是 IServiceCollection 类型
            var serviceMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(u => u.ReturnType == typeof(void)
                    && u.GetParameters().Length == 1
                    && u.GetParameters().First().ParameterType == typeof(IServiceCollection))
                .ToList();

            if (serviceMethods.Count == 0) continue;

            // 自动安装属性调用
            foreach (var method in serviceMethods)
            {
                method.Invoke(startup, new[] { services });
            }
        }

        return services;
    }

    /// <summary>
    /// <para>zh-cn:按依赖关系对 Startup 类型进行拓扑排序，并在同层按 Order 与类型全名稳定排序。</para>
    /// <para>en-us:Topologically sorts Startup types by dependency, then applies stable ordering by Order and full type name within the same layer.</para>
    /// </summary>
    /// <param name="startupTypes">
    /// <para>zh-cn:待排序的 Startup 类型集合。</para>
    /// <para>en-us:The Startup type collection to sort.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:返回依赖有序的 Startup 类型列表。</para>
    /// <para>en-us:Returns a dependency-ordered Startup type list.</para>
    /// </returns>
    private static IReadOnlyList<Type> SortStartupsByDependency(IEnumerable<Type> startupTypes)
    {
        var allTypes = startupTypes
            .Distinct()
            .OrderBy(GetStartupOrder)
            .ThenBy(t => t.FullName, StringComparer.Ordinal)
            .ToList();

        var typeSet = allTypes.ToHashSet();
        var inDegree = allTypes.ToDictionary(t => t, _ => 0);
        var dependents = allTypes.ToDictionary(t => t, _ => new List<Type>());

        foreach (var startupType in allTypes)
        {
            var dependType = GetStartupDependType(startupType);
            if (dependType == null || !typeSet.Contains(dependType))
            {
                continue;
            }

            inDegree[startupType]++;
            dependents[dependType].Add(startupType);
        }

        var queue = new Queue<Type>(allTypes.Where(t => inDegree[t] == 0));
        var ordered = new List<Type>(allTypes.Count);

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            ordered.Add(node);

            foreach (var dependent in dependents[node]
                         .OrderBy(GetStartupOrder)
                         .ThenBy(t => t.FullName, StringComparer.Ordinal))
            {
                inDegree[dependent]--;
                if (inDegree[dependent] == 0)
                {
                    queue.Enqueue(dependent);
                }
            }
        }

        // 如果存在环路或无效依赖，回退到原始稳定顺序，避免中断当前行为。
        if (ordered.Count != allTypes.Count)
        {
            return allTypes;
        }

        return ordered;
    }

    /// <summary>
    /// <para>zh-cn:获取 Startup 的依赖 Startup 类型。</para>
    /// <para>en-us:Gets the dependent Startup type of the Startup.</para>
    /// </summary>
    /// <param name="type">
    /// <para>zh-cn:Startup 类型。</para>
    /// <para>en-us:The Startup type.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:返回依赖类型，未配置则返回 null。</para>
    /// <para>en-us:Returns the dependency type, or null when not configured.</para>
    /// </returns>
    private static Type? GetStartupDependType(Type type)
    {
        return !type.IsDefined(typeof(AppStartupAttribute), true)
            ? null
            : type.GetCustomAttribute<AppStartupAttribute>(true).DependType;
    }

    /// <summary>
    /// 获取 Startup 排序
    /// </summary>
    /// <param name="type">排序类型</param>
    /// <returns>int</returns>
    private static int GetStartupOrder(Type type)
    {
        return !type.IsDefined(typeof(AppStartupAttribute), true) ? 0 : type.GetCustomAttribute<AppStartupAttribute>(true).Order;
    }
}
