/*
 * Copyright (c) 2024 星曳数据
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * This file is provided under the Mozilla Public License Version 2.0,
 * and the "NO WARRANTY" clause of the MPL is hereby expressly
 * acknowledged.
 */
using Air.Cloud.Core.App.Startups;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

using System.Reflection;

namespace Air.Cloud.Core.App.Filters;

/// <summary>
/// <para>zh-cn:应用启动时自动注册中间件</para>
/// <para>en-us:Automatically register middleware when the application starts</para>
/// </summary>
[IgnoreScanning]
public class StartupFilter : IStartupFilter
{
    /// <summary>
    /// <para>zh-cn:配置应用程序构建器</para>
    /// <para>en-us:Configure the application builder</para>
    /// </summary>
    /// <param name="next">
    /// <para>zh-cn:下一个中间件</para>
    /// <para>en-us:The next middleware</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:返回配置后的应用程序构建器</para>
    /// <para>en-us:Return the configured application builder</para>
    /// </returns>
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
          
            // 设置响应报文头信息
            app.Use(async (context, next) =>
            {
                // 执行下一个中间件
                await next.Invoke();
                // 释放所有未托管的服务提供器
                AppCore.DisposeUnmanagedObjects();
               
            });
            // 调用默认中间件
            app.UseApp();

            // 配置所有 Starup Configure
            UseStartups(app);
            // 存储根服务
            AppCore.RootServices = app.ApplicationServices;
            // 调用启动层的 Startup
            next(app);
        };
    }


    /// <summary>
    /// <para>zh-cn:配置 Startup 的 Configure</para>
    /// <para>en-us:Configure the Startup Configure</para>
    /// </summary>
    /// <param name="app">
    /// <para>zh-cn:应用程序构建器</para>
    /// <para>en-us:The application builder</para></para>
    /// </param>
    private static void UseStartups(IApplicationBuilder app)
    {
        UseStartups(AppCore.AppStartups, app);
    }

    /// <summary>
    /// <para>zh-cn:批量将自定义 AppStartup 添加到 Startup.cs 的 Configure 中</para>
    /// <para>en-us:Batch add custom AppStartup to Configure in Startup.cs</para>
    /// </summary>
    /// <param name="startups">
    /// <para>zh-cn:AppStartup 类型集合</para>
    /// <para>en-us>AppStartup type collection</para>
    /// </param>
    /// <param name="app">
    /// <para>zh-cn:应用程序构建器</para>
    /// <para>en-us:The application builder</para>
    /// </param>
    private static void UseStartups(IEnumerable<AppStartup> startups, IApplicationBuilder app)
    {
        // 遍历所有
        foreach (var startup in startups)
        {
            var type = startup.GetType();
            // 获取所有符合依赖注入格式的方法，如返回值 void，且第一个参数是 IApplicationBuilder 类型
            var configureMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(u => u.ReturnType == typeof(void)
                    && u.GetParameters().Length > 0
                    && u.GetParameters().First().ParameterType == typeof(IApplicationBuilder));
            if (!configureMethods.Any()) continue;
            foreach (var method in configureMethods)
            {
                method.Invoke(startup, ResolveMethodParameterInstances(app, method));
            }
        }
        AppCore.AppStartups.Clear();
    }

    /// <summary>
    /// <para>zh-cn:解析方法参数实例</para>
    /// <para>en-us:Resolve method parameter instances</para>
    /// </summary>
    /// <param name="app">
    /// <para>zh-cn:应用程序构建器</para>
    /// <para>en-us:The application builder</para>
    /// </param>
    /// <param name="method">
    /// <para>zh-cn:方法信息</para>
    /// <para>en-us:Method information</para>
    /// </param>
    /// <returns></returns>
    private static object[] ResolveMethodParameterInstances(IApplicationBuilder app, MethodInfo method)
    {
        var parameters = method.GetParameters();
        var parameterInstances = new object[parameters.Length];
        parameterInstances[0] = app;
        for (var i = 1; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            parameterInstances[i] = app.ApplicationServices.GetRequiredService(parameter.ParameterType);
        }
        return parameterInstances;
    }
}