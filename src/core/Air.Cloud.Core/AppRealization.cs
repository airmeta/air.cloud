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
using Air.Cloud.Core.Plugins.DefaultDependencies;
using Air.Cloud.Core.Plugins.PID;
using Air.Cloud.Core.Standard;
using Air.Cloud.Core.Standard.AppInject;
using Air.Cloud.Core.Standard.Assemblies;
using Air.Cloud.Core.Standard.Authentication.Jwt;
using Air.Cloud.Core.Standard.Cache;
using Air.Cloud.Core.Standard.Cache.Redis;
using Air.Cloud.Core.Standard.Configuration;
using Air.Cloud.Core.Standard.Container;
using Air.Cloud.Core.Standard.DefaultDependencies;
using Air.Cloud.Core.Standard.Exceptions;
using Air.Cloud.Core.Standard.JinYiWei;
using Air.Cloud.Core.Standard.JSON;
using Air.Cloud.Core.Standard.KVCenter;
using Air.Cloud.Core.Standard.MessageQueue;
using Air.Cloud.Core.Standard.UtilStandard;

using Microsoft.AspNetCore.Mvc;

using System.Reflection;

namespace Air.Cloud.Core
{
    /// <summary>
    /// 所有标准实现
    /// </summary>
    public class AppRealization
    {
        /// <summary>
        /// 内容输出标准实现
        /// </summary>
        public static IAppOutputStandard Output => InternalRealization.Output ?? DefaultRealization.Output;
        /// <summary>
        /// 容器标准实现
        /// </summary>
        public static IContainerStandard Container => InternalRealization.Container ?? DefaultRealization.Container;
        /// <summary>
        /// 系统注入标准实现
        /// </summary>
        public static IAppConfigurationStandard Configuration => InternalRealization.Configuration ?? DefaultRealization.Configuration;
        /// <summary>
        /// 全局异常标准实现
        /// </summary>
        public static IAppDomainExceptionHandlerStandard DomainExceptionHandler => InternalRealization.DomainExceptionHandler ?? DefaultRealization.DomainExceptionHandler;
        /// <summary>
        /// 类库扫描标准实现
        /// </summary>
        public static IAssemblyScanningStandard AssemblyScanning => InternalRealization.AssemblyScanning ?? DefaultRealization.AssemblyScanning;
        /// <summary>
        /// JSON Web Token 标准实现
        /// </summary>
        public static IJwtHandlerStandard Jwt => InternalRealization.Jwt ?? DefaultRealization.Jwt;
        /// <summary>
        /// 缓存标准实现
        /// </summary>
        /// <remarks>
        /// 如果无实现,则使用Redis缓存标准实现中的String模块,如果无Redis缓存标准实现,则抛出异常信息
        /// </remarks>
        public static IAppCacheStandard Cache => InternalRealization.Cache ?? DefaultRealization.Cache;
        /// <summary>
        /// Redis缓存标准实现
        /// </summary>
        public static IRedisCacheStandard RedisCache => InternalRealization.RedisCache ?? DefaultRealization.RedisCache;
        /// <summary>
        /// 压缩与解压缩标准实现
        /// </summary>
        public static ICompressStandard Compress => InternalRealization.Compress ?? DefaultRealization.Compress;
        /// <summary>
        /// JSON序列化标准实现
        /// </summary>
        public static IJsonSerializerStandard JSON => InternalRealization.JSON ?? DefaultRealization.JSON;
        /// <summary>
        /// 应用程序PID信息 
        /// </summary>
        /// <remarks>
        ///  与linux 系统的不同的是,这个PID是为了在微服务架构下,多节点的统一注册时,每个实例的名称
        ///  (暂时只在Windows 环境下进行测试)
        /// </remarks>
        public static IPIDPlugin PID => InternalRealization.PID ?? DefaultRealization.PID;
        /// <summary>
        /// 系统注入标准
        /// </summary>
        public static IAppInjectStandard Injection=> InternalRealization.Injection ?? DefaultRealization.Injection;
        /// <summary>
        /// <para>zh-cn:键值对存储中心标准</para>
        /// <para>en-us:Key-Value store center</para>
        /// </summary>
        public static IKVCenterStandard KVCenter => InternalRealization.KVCenter ?? DefaultRealization.KVCenter;
        /// <summary>
        /// <para>zh-cn:日志追踪实现</para>
        /// <para>en-us:Log tracking  dependency</para>
        /// </summary>
        public static ITraceLogStandard Log=>InternalRealization.TraceLog ?? DefaultRealization.TraceLog;

        /// <summary>
        /// <para>zh-cn:队列实现</para>
        /// <para>en-us:queue dependency</para>
        /// </summary>
        public static IMessageQueueStandard Queue=> InternalRealization.Queue ?? DefaultRealization.Queue;

        /// <summary>
        /// 设置约定实现
        /// </summary>
        /// <typeparam name="TDependency">约定类型</typeparam>
        /// <param name="standard">约定实现</param>
        public static void SetDependency<TStandard>(TStandard standard) 
                where TStandard :IStandard
        {
            FieldInfo Field = typeof(InternalRealization).GetFields().FirstOrDefault(s => s.FieldType == typeof(TStandard));
            Field.SetValue(null, standard);
        }

        static AppRealization()
        {
            FieldInfo[] FieldInfos = typeof(InternalRealization).GetFields().ToArray();
            foreach (var item in FieldInfos)
            {
                //通过反射寻找IAppInjectStandard的实现
                var Types = AppCore.StandardTypes.Where(s => item.FieldType.IsAssignableFrom(s) && s != item.FieldType).ToList();
                if (Types.Count > 1)
                {
                    var ModulesInformation = new Dictionary<string, object>();
                    for (int a = 1;  a<=Types.Count; a++)
                    {
                        ModulesInformation.Add($"模块{a}", Types[a-1]);
                    }
                    Output.Error(new Exception(item.FieldType.Name + "标准具有多个实现模块"));
                }
                if (Types.Count == 0) continue;
                var Instance = Activator.CreateInstance(Types[0]);
                item.SetValue(null, Instance);
            }
        }
        /// <summary>
        /// 默认标准实现
        /// </summary>
        protected static class DefaultRealization
        {
            /// <summary>
            /// 压缩与解压缩标准实现
            /// </summary>
            public static ICompressStandard Compress => new DefaultCompressStandardDependency();

            /// <summary>
            /// 内容输出标准实现
            /// </summary>
            public static IAppOutputStandard Output => new DefaultAppOutputDependency();
            /// <summary>
            /// 容器标准实现
            /// </summary>
            public static IContainerStandard Container => throw new NotImplementedException("未查询到容器标准的实现");
            /// <summary>
            /// 系统配置标准实现
            /// </summary>
            public static IAppConfigurationStandard Configuration => new DefaultAppConfigurationDependency();
            /// <summary>
            /// 全局异常标准实现
            /// </summary>
            public static IAppDomainExceptionHandlerStandard DomainExceptionHandler => new DefaultAppDomainExcepetionHandlerDependency();
            /// <summary>
            /// 类库扫描标准实现
            /// </summary>
            public static IAssemblyScanningStandard AssemblyScanning => new DefaultAssemblyScanningDependency();
            /// <summary>
            /// JSON Web Token 标准实现
            /// </summary>
            public static IJwtHandlerStandard Jwt => new DefaultJwtHandlerDependency();
            /// <summary>
            /// 缓存标准实现
            /// </summary>
            public static IAppCacheStandard Cache => throw new NotImplementedException("未引入任何关于Cache的模组或插件,如果引入,则检查你的代码是否注入该实现");
            /// <summary>
            /// Redis缓存标准实现
            /// </summary>
            public static IRedisCacheStandard RedisCache => throw new NotImplementedException("未引入任何关于Redis的模组或插件,如果引入,则检查你的代码是否注入该实现");
            /// <summary>
            /// JSON序列化标准实现
            /// </summary>
            public static IJsonSerializerStandard JSON => new DefaultJsonSerializerStandardDependency();

            /// <summary>
            /// 应用程序PID信息 
            /// </summary>
            /// <remarks>
            ///  这个PID是为了在微服务架构下,多节点的统一注册时,每个实例的名称 每个不同路径运行的实例唯一
            /// </remarks>
            public static IPIDPlugin PID => new DefaultPIDPluginDependency();

            /// <summary>
            /// 系统注入标准默认实现
            /// </summary>
            public static IAppInjectStandard Injection => throw new NotImplementedException("系统未实现注入标准");
            /// <summary>
            /// <para>zh-cn:键值对存储中心标准</para>
            /// <para>en-us:Key-Value store center</para>
            /// </summary>
            public static IKVCenterStandard KVCenter => throw new NotImplementedException("系统未实现KV中心标准");
            /// <summary>
            /// <para>zh-cn:默认日志追踪</para>
            /// <para>en-us:Default log tracking </para>
            /// </summary>
            public static ITraceLogStandard TraceLog => new DefaultTraceLogDependency();
            /// <summary>
            /// <para>zh-cn:默认队列实现</para>
            /// <para>en-us:Default queue dependency</para>
            /// </summary>
            public static IMessageQueueStandard Queue => throw new NotImplementedException("系统未实现队列标准");
        }
        /// <summary>
        /// 自定义标准实现
        /// </summary>
        protected static class InternalRealization
        {
            /// <summary>
            /// 压缩与解压缩标准实现
            /// </summary>
            public static ICompressStandard Compress => AppCore.GetService<ICompressStandard>();
            /// <summary>
            /// 内容输出标准实现
            /// </summary>
            public static IAppOutputStandard Output = null;
            /// <summary>
            /// 容器标准实现
            /// </summary>
            public static IContainerStandard Container => AppCore.GetService<IContainerStandard>();
            /// <summary>
            /// 系统配置标准实现
            /// </summary>
            public static IAppConfigurationStandard Configuration = null;
            /// <summary>
            /// 全局异常标准实现
            /// </summary>
            public static IAppDomainExceptionHandlerStandard DomainExceptionHandler = null;
            /// <summary>
            /// 类库扫描标准实现
            /// </summary>
            public static IAssemblyScanningStandard AssemblyScanning = null;
            /// <summary>
            /// JSON Web Token 标准实现
            /// </summary>
            public static IJwtHandlerStandard Jwt = null;
            /// <summary>
            /// 缓存缓存标准实现
            /// </summary>
            public static IAppCacheStandard Cache => AppCore.GetService<IAppCacheStandard>();
            /// <summary>
            /// Redis缓存缓存标准实现
            /// </summary>
            public static IRedisCacheStandard RedisCache => AppCore.GetService<IRedisCacheStandard>();
            /// <summary>
            /// JSON序列化标准实现
            /// </summary>
            public static IJsonSerializerStandard JSON => AppCore.GetService<IJsonSerializerStandard>()??null;

            /// <summary>
            /// 应用程序PID信息 
            /// </summary>
            /// <remarks>
            ///  与linux 系统的不同的是,这个PID是为了在微服务架构下,多节点的统一注册时,每个实例的名称
            ///  (暂时只在Windows 环境下进行测试)
            /// </remarks>
            public static IPIDPlugin PID=null;

            /// <summary>
            /// 系统注入标准实现
            /// </summary>
            public static IAppInjectStandard Injection = null;

            /// <summary>
            /// <para>zh-cn:键值对存储中心标准</para>
            /// <para>en-us:Key-Value store center</para>
            /// </summary>
            public static IKVCenterStandard KVCenter = null;
            /// <summary>
            /// <para>zh-cn:日志追踪</para>
            /// <para>en-us:Log tracking </para>
            /// </summary>
            public static ITraceLogStandard TraceLog => AppCore.GetService<ITraceLogStandard>();

            /// <summary>
            /// <para>zh-cn:队列实现</para>
            /// <para>en-us:queue dependency</para>
            /// </summary>
            public static IMessageQueueStandard Queue => AppCore.GetService<IMessageQueueStandard>();
        }
    }
}
