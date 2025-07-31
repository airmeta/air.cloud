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
using Air.Cloud.Core.Enums;
using Air.Cloud.Core.Plugins.DefaultDependencies.IdGenerator;
using Air.Cloud.Core.Plugins.IdGenerator;

using Mapster;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using System.Reflection;
using System.Runtime.Loader;

namespace Air.Cloud.Core.App
{
    /// <summary>
    /// <para>zh-cn:应用程序核心</para>
    /// <para>en-us:Application core</para>
    /// </summary>
    public static partial class AppCore
    {
        /// <summary>
        /// <para>zh-cn:泛型实现连续生成GUID</para>
        /// <para>en-us:Generic implementation of continuous generation GUID</para>
        /// </summary>
        /// <remarks>
        ///  <para>zh-cn:你可以自行实现,并使用该方法调用你的实现</para>
        ///  <para>en-us:You can implement it yourself and use this method to call your implementation </para>
        /// </remarks>
        /// <returns></returns>
        public static string Guid<T, K>(T t, K k, bool Format = true) where T : class, IUniqueGuidGenerator, new() where K : IUniqueGuidCreatOptions, new()
        {
            k = k == null ? new UniqueGuidCreatOptions { LittleEndianBinary16Format = true }.Adapt<K>() : k;
            var guid = t.Create(k);
            if (Format) return guid.ToString().Replace("-", "");
            return guid.ToString();
        }

        /// <summary>
        /// <para>zh-cn:默认方式生成Guid</para>
        /// <para>en-us:Default way to generate Guid</para>
        /// </summary>
        /// <returns></returns>
        public static string Guid(bool Format = true)
        {
            return Guid(new UniqueGuidGenerator(), new UniqueGuidCreatOptions { LittleEndianBinary16Format = true }, Format);
        }


        #region  Get Service
        /// <summary>
        /// <para>zh-cn:获取服务提供器</para>
        /// <para>en-us:GetAsync service provider</para>
        /// </summary>
        /// <param name="serviceType">
        /// <para>zh-cn:服务类型</para>
        /// <para>en-us:Service type</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:返回服务提供器</para>
        /// <para>en-us:Return service provider</para>
        /// </returns>
        public static IServiceProvider GetServiceProvider(Type serviceType)
        {
            // 处理控制台应用程序
            if (AppStartType == AppStartupTypeEnum.WEB && RootServices != null) return RootServices;

            // 第一选择，判断是否是单例注册且单例服务不为空，如果是直接返回根服务提供器
            if (RootServices != null && InternalServices.Where(u => u.ServiceType == (serviceType.IsGenericType ? serviceType.GetGenericTypeDefinition() : serviceType))
                                                                    .Any(u => u.Lifetime == ServiceLifetime.Singleton)) return RootServices;

            // 第二选择是获取 HttpContext 对象的 RequestServices
            var httpContext = HttpContext;
            if (httpContext?.RequestServices != null) return httpContext.RequestServices;
            // 第三选择，创建新的作用域并返回服务提供器
            else if (RootServices != null)
            {
                var scoped = RootServices.CreateScope();
                UnmanagedObjects.Add(scoped);
                return scoped.ServiceProvider;
            }
            // 第四选择，构建新的服务对象（性能最差）
            else
            {
                if (InternalServices == null)
                {
                    return null;
                }
                var serviceProvider = InternalServices.BuildServiceProvider();
                UnmanagedObjects.Add(serviceProvider);
                return serviceProvider;
            }
        }

        /// <summary>
        /// <para>zh-cn:获取请求生存周期的服务</para>
        /// <para>en-us:GetAsync request survival cycle service</para>
        /// </summary>
        /// <typeparam name="TService">
        /// <para>zh-cn:服务类型</para>
        /// <para>en-us:Service type</para>
        /// </typeparam>
        /// <param name="serviceProvider">
        /// <para>zh-cn:服务提供器</para>
        /// <para>en-us:Service provider</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:返回服务</para>
        /// <para>en-us:Return service</para>
        /// </returns>
        public static TService GetService<TService>(IServiceProvider serviceProvider = default)
            where TService : class
        {
            return GetService(typeof(TService), serviceProvider) as TService;
        }

        /// <summary>
        /// <para>zh-cn:获取请求生存周期的服务</para>
        /// <para>en-us:GetAsync request survival cycle service</para>
        /// </summary>
        /// <param name="serviceProvider">
        /// <para>zh-cn:服务提供器</para>
        /// <para>en-us:Service provider</para>
        /// </param>
        /// <param name="type">
        /// <para>zh-cn:服务类型</para>
        /// <para>en-us:Service type</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:返回服务</para>
        /// <para>en-us:Return service</para>
        /// </returns>
        public static object GetService(Type type, IServiceProvider serviceProvider = default)
        {
            return (serviceProvider ?? GetServiceProvider(type))?.GetService(type);
        }

        /// <summary>
        /// <para>zh-cn:获取请求生存周期的服务</para>
        /// <para>en-us:GetAsync request survival cycle service</para>
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="serviceProvider"></param>
        /// <returns></returns>
        public static TService GetRequiredService<TService>(IServiceProvider serviceProvider = default)
            where TService : class
        {
            return GetRequiredService(typeof(TService), serviceProvider) as TService;
        }

        /// <summary>
        /// 获取请求生存周期的服务
        /// </summary>
        /// <param name="type"></param>
        /// <param name="serviceProvider"></param>
        /// <returns></returns>
        public static object GetRequiredService(Type type, IServiceProvider serviceProvider = default)
        {
            return (serviceProvider ?? GetServiceProvider(type))?.GetRequiredService(type);
        }

        #endregion

        #region Get Options

        /// <summary>
        /// 获取选项
        /// </summary>
        /// <typeparam name="TOptions">强类型选项类</typeparam>
        /// <param name="serviceProvider"></param>
        /// <returns>TOptions</returns>
        public static TOptions GetOptions<TOptions>(IServiceProvider serviceProvider = default)
            where TOptions : class, new()
        {
            return GetService<IOptions<TOptions>>(serviceProvider ?? RootServices)?.Value;
        }

        /// <summary>
        /// 获取选项
        /// </summary>
        /// <typeparam name="TOptions">强类型选项类</typeparam>
        /// <param name="serviceProvider"></param>
        /// <returns>TOptions</returns>
        public static TOptions GetOptionsMonitor<TOptions>(IServiceProvider serviceProvider = default)
            where TOptions : class, new()
        {
            return GetService<IOptionsMonitor<TOptions>>(serviceProvider ?? RootServices)?.CurrentValue;
        }

        /// <summary>
        /// <para>zh-cn:获取选项</para>
        /// <para>en-us:GetAsync options</para>
        /// </summary>
        /// <typeparam name="TOptions">强类型选项类</typeparam>
        /// <param name="serviceProvider"></param>
        /// <returns>TOptions</returns>
        public static TOptions GetOptionsSnapshot<TOptions>(IServiceProvider serviceProvider = default)
            where TOptions : class, new()
        {
            // 这里不能从根服务解析，因为是 Scoped 作用域
            return GetService<IOptionsSnapshot<TOptions>>(serviceProvider)?.Value;
        }

        #endregion

        #region  Set Service
        /// <summary>
        /// <para>zh-cn:注册服务</para>
        /// <para>en-us:Set Service</para>
        /// </summary>
        /// <typeparam name="TType">
        /// <para>zh-cn:服务类型</para>
        /// <para>en-us:Service type</para>
        /// </typeparam>
        /// <typeparam name="TImpl">
        /// <para>zh-cn:实现类型</para>
        /// <para>en-us:Implementation type</para>
        /// </typeparam>
        /// <param name="serviceLifetime">
        /// <para>zh-cn:服务生命周期</para>
        /// <para>en-us:Service lifetime</para>
        /// </param>
        public static void SetService<TType, TImpl>(ServiceLifetime serviceLifetime=ServiceLifetime.Transient)
        {
            InternalServices.Add(
                        ServiceDescriptor.Describe(typeof(TType),
                        typeof(TImpl),
                        serviceLifetime));
        }
        /// <summary>
        /// <para>zh-cn:注册服务</para>
        /// <para>en-us:Set Service</para>
        /// </summary>
        /// <typeparam name="TType">
        /// <para>zh-cn:服务类型</para>
        /// <para>en-us:Service type</para>
        /// </typeparam>
        /// <param name="impl">
        /// <para>zh-cn:实例</para>
        /// <para>en-us:Instance</para>
        /// </param>
        /// <param name="serviceLifetime">
        /// <para>zh-cn:服务生命周期</para>
        /// <para>en-us:Service lifetime</para>
        /// </param>
        public static void SetService<TType>(TType impl, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        {
            InternalServices.Add(
                       ServiceDescriptor.Describe(typeof(TType),
                       (s) =>
                       {
                           return impl;
                       },
                       serviceLifetime));
        }

        /// <summary>
        /// <para>zh-cn:注册服务</para>
        /// <para>en-us:Set Service</para>
        /// </summary>
        /// <param name = "baseType" >
        /// <para>zh-cn:服务类型</para>
        /// <para>en-us:Service type</para>
        /// </param>
        /// <param name="impl">
        /// <para>zh-cn:实例</para>
        /// <para>en-us:Instance</para>
        /// </param>
        /// <param name="serviceLifetime">
        /// <para>zh-cn:服务生命周期</para>
        /// <para>en-us:Service lifetime</para>
        /// </param>
        public static void SetService(Type baseType,Type impl, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
        {
            InternalServices.Add(ServiceDescriptor.Describe(baseType, impl,serviceLifetime));
        }
        #endregion

        /// <summary>
        /// <para>zh-cn:释放所有未托管的对象</para>
        /// <para>en-us:Release all unmanaged objects</para>
        /// </summary>
        public static void DisposeUnmanagedObjects()
        {
            UnmanagedObjects.Clear();
        }

        /// <summary>
        /// <para>zh-cn:从系统中的所有程序集中加载指定类型的子类型</para>
        /// <para>en-us:Load a subtype of a specified type from all assemblies in the system</para>
        /// </summary>
        /// <param name="specifyTypes">
        ///  <para>zh-cn:指定的类型</para>
        ///  <para>en-us:Specify type</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:所有实现了指定接口的类型信息</para>
        /// <para>en-us:All type information that implements the specified interface</para>
        /// </returns>
        public  static Type[] LoadSpecifyTypes(params Type[] specifyTypes)
        {
            return Assemblies.SelectMany(ass =>
            {
                return AssemblyLoadContext.Default.LoadFromAssemblyName(ass).GetTypes().Where(t =>
                {
                    var IsImpl = t.GetInterfaces().Any(x => specifyTypes.Any(x1 => x1 == (x.IsGenericType ? x.GetGenericTypeDefinition() : x)));
                    if (IsImpl && t.IsPublic && !t.IsDefined(typeof(IgnoreScanningAttribute), false))
                        return true;
                    return false;
                }).ToArray();
            }).ToArray();
        }

    }

}
