
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
using Air.Cloud.Core.Modules.AppAspect.Attributes;
using Air.Cloud.Core.Modules.AppAspect.Handler;
using Air.Cloud.Core.Modules.AppAspect.Model;
using Air.Cloud.Core.Plugins.Security.MD5;
using Air.Cloud.Core.Standard.DynamicServer.Extensions;

using Microsoft.Extensions.DependencyInjection;

using System.Reflection;
using System.Runtime.Loader;

namespace Air.Cloud.Core.Modules.AppAspect
{
    /// <summary>
    ///  <para>zh-cn:Aspect 服务构建器</para>
    ///  <para>en-us:Aspect service builder</para>
    /// </summary>
    public class AppAspectOptionsBuilder
    {
        /// <summary>
        /// <para>zh-cn:Aspect元数据</para>
        /// <para>en-us:</para>
        /// </summary>
        public static IReadOnlyDictionary<string, AspectMetadata> AspectMetadatas = new Dictionary<string, AspectMetadata>();

        /// <summary>
        /// 
        /// </summary>
        public static IList<Type> UseAspectTypes = new List<Type>();

        internal void Build(IServiceCollection services)
        {
            IList<AspectMetadata> aspectMetadata = new List<AspectMetadata>();
            UseAspectTypes = AppCore.Assemblies.SelectMany(ass =>
            {
                return AssemblyLoadContext.Default.LoadFromAssemblyName(ass).GetTypes().Where(t =>
                {
                    var UseAspectType = t.GetCustomAttribute<AppAspectAttribute>();

                    if (UseAspectType != null)
                        return true;
                    return false;
                }).ToList();
            }).ToList();
            foreach (var type in UseAspectTypes)
            {
                foreach (var method in type.GetMethods())
                {
                    //attributes 为方法上的所有特性 
                    var attributes = method.GetCustomAttributes<UseAspectAttribute>().DistinctBy(s => MD5Encryption.GetMd5By8(s.AppAspectDependencies.FullName)).OrderByDescending(s => s.Order).ToArray();
                    if (attributes.Count() == 0) continue;
                    //这个时候遍历 attributes 中的信息 并获取到对象中的AppAspectDependencies属性,动态创建对应的AppAspectDependencies 的实例
                    foreach (var item in attributes)
                    {
                        if (item.AppAspectDependencies.GetInterfaces().Contains(typeof(IAspectAroundHandler)) || item.AppAspectDependencies.GetInterfaces().Contains(typeof(IAspectExecuteHandler)))
                        {
                            var instance = AppCore.GetService(item.AppAspectDependencies);
                            if (instance==null)
                            {
                                //这里可以使用反射创建对象
                                var aspectInstance = Activator.CreateInstance(item.AppAspectDependencies);
                                //动态注入到容器中
                                services.Add(ServiceDescriptor.Describe(item.AppAspectDependencies, (s) =>
                                {
                                    return aspectInstance;
                                }, DependencyInjectionServiceCollectionExtensions.TryGetServiceLifetime(typeof(ISingleton))));
                            }
                        }
                        else
                        {
                            AppRealization.Output.Print(new AppPrintInformation("错误的Aspect切入设置", $"切入类:{item.AppAspectDependencies.FullName} 未实现IAspectAroundHandler 或 IAspectExecuteHandler,已为你排除该项"));
                        }
                    }
                    aspectMetadata.Add(new AspectMetadata
                    {
                        AspectTargetType = type,
                        MethodName = MD5Encryption.GetMd5By32($"{type.FullName}.{method.Name}"),
                        Aspects = attributes
                    });
                }
            }
            AspectMetadatas = aspectMetadata.ToArray().ToDictionary(s => s.MethodName, s => s);
            aspectMetadata = null;
        }
    }
}
