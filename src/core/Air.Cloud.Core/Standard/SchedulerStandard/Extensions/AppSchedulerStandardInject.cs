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
using Air.Cloud.Core.Dependencies;
using Air.Cloud.Core.Modules.AppPrint;
using Air.Cloud.Core.Standard.DynamicServer.Extensions;

using Microsoft.Extensions.DependencyInjection;

using System.Reflection;
using System.Runtime.Loader;

namespace Air.Cloud.Core.Standard.SchedulerStandard.Extensions
{
    /// <summary>
    /// 
    /// </summary>
    public static class AppSchedulerStandardInject
    {

        /// <summary>
        /// <para>zh-cn:注册调度任务</para>
        /// <para>en-us: Inject app scheduler</para>
        /// </summary>
        /// <param name="services">
        /// 服务集合
        /// </param>
        public static void AddSchedulerStandard<TSchedulerOptions>(this IServiceCollection services) where TSchedulerOptions : class, ISchedulerStandardOptions
        {
            foreach (var item in AppCore.Assemblies.ToList())
            {
                try
                {
                    var AllTypes = AssemblyLoadContext.Default.LoadFromAssemblyName(item).GetTypes().Where(s => s.IsClass && s.GetInterfaces().Contains(typeof(ISchedulerStandard<TSchedulerOptions>))).ToList();
                    foreach (var t in AllTypes)
                    {
                        AutoLoadAttribute? NeedLoad = t.GetCustomAttribute<AutoLoadAttribute>();
                        if (NeedLoad != null&&NeedLoad.Load==false)
                        {
                            continue;
                        }
                        var instances = t.GetInterfaces();
                        string[] ints= instances.Select(i => i.Name).ToArray();
                        if (instances.Contains(typeof(ISchedulerStandard<TSchedulerOptions>)) && t.IsPublic)
                        {
                            services.Add(ServiceDescriptor.Describe(typeof(ISchedulerStandard<TSchedulerOptions>), t, DependencyInjectionServiceCollectionExtensions.TryGetServiceLifetime(typeof(ISingleton))));
                        }
                    }
                }
                catch (Exception ex)
                {
                    AppRealization.Output.Print(new AppPrintInformation
                    {
                        Title = "domain-errors",
                        Level = AppPrintLevel.Error,
                        Content = $"注册调度任务失败,异常信息:{ex.Message}",
                        State = true
                    });
                }
            }
        }

    }
}
