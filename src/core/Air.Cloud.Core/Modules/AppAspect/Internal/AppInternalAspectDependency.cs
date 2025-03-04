
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
using Air.Cloud.Core.Modules.AppAspect.Attributes;
using Air.Cloud.Core.Modules.AppAspect.Model;
using Air.Cloud.Core.Plugins.Security.MD5;

using AspectInjector.Broker;

using System.Reflection;

using AspectInjector_Aspect = AspectInjector.Broker.Aspect;
namespace Air.Cloud.Core.Modules.AppAspect.Internal
{
    /// <summary>
    /// <para>zh-cn:内部Aspect依赖,减少外部配置Aspect错误问题</para>
    /// <para>en-us:Internal Aspect dependencies to reduce external configuration Aspect errors</para>
    /// </summary>
    /// <remarks>
    /// <para>zh-cn:如果你想要配置Aspect,你需要使用AspectInjector库,框架内已经默认引入</para>
    /// <para>en-us:If you want to configure Aspect, you need to use the AspectInjector library, which is introduced by default in the framework</para>
    /// </remarks>
    [AspectInjector_Aspect(Scope.Global)]
    public class AppInternalAspectDependency
    {
        private string AspestKey(Type hostType, string MethodName) => MD5Encryption.GetMd5By32($"{hostType.FullName}.{MethodName}");
        private AspectMetadata? Metadata(Type hostType, string MethodName)
        {
            var Key = AspestKey(hostType, MethodName);
            if (!AppAspectOptionsBuilder.AspectMetadatas.ContainsKey(Key)) return null;
            return AppAspectOptionsBuilder.AspectMetadatas[Key];
        }
        private UseAspectAttribute[] EmptyAspects = new UseAspectAttribute[0];

        /// <summary>
        ///  <para>zh-cn:方法执行之前</para>
        ///  <para>en-us:Befor method execute</para>
        /// </summary>
        /// <param name="name">
        /// <para>zh-cn:方法名</para>
        /// <para>en-us:MethodName</para>
        /// </param>
        /// <param name="args">
        ///  <para>zh-cn:方法参数</para>
        ///  <para>en-us:Method parameters</para>
        /// </param>
        /// <param name="hostType">
        ///  <para>zh-cn:方法所在类类型</para>
        ///  <para>en-us: Class type</para>
        /// </param>
        [Advice(Kind.Before, Targets = Target.Method)]
        public void Before([Argument(Source.Name)] string name, [Argument(Source.Arguments)] object[] args, [Argument(Source.Type)] Type hostType)
        {
            //执行切面
            foreach (var aspect in (Metadata(hostType, name)?.Aspects) ?? EmptyAspects)
            {
                var aspectInstance = AppCore.GetService(aspect.AppAspectDependencies) ?? Activator.CreateInstance(aspect.AppAspectDependencies);
                MethodInfo method = aspectInstance.GetType().GetMethod("Execute_Before");
                if (method != null)
                {
                    method.Invoke(aspectInstance, new object[] { hostType.GetMethod(name), args });
                }
            }
        }
        /// <summary>
        ///  <para>zh-cn:方法执行之后</para>
        ///  <para>en-us:Befor method execute</para>
        /// </summary>
        /// <param name="name">
        /// <para>zh-cn:方法名</para>
        /// <para>en-us:MethodName</para>
        /// </param>
        /// <param name="retValue">
        ///  <para>zh-cn:方法返回值</para>
        ///  <para>en-us:Method return value</para>
        /// </param>
        /// <param name="hostType">
        ///  <para>zh-cn:方法所在类类型</para>
        ///  <para>en-us: Class type</para>
        /// </param>
        [Advice(Kind.After, Targets = Target.Method)]
        public void After([Argument(Source.Name)] string name, [Argument(Source.ReturnValue)] object retValue, [Argument(Source.Type)] Type hostType)
        {
            //执行切面
            foreach (var aspect in (Metadata(hostType, name)?.Aspects) ?? EmptyAspects)
            {
                var aspectInstance = AppCore.GetService(aspect.AppAspectDependencies) ?? Activator.CreateInstance(aspect.AppAspectDependencies);
                MethodInfo method = aspectInstance.GetType().GetMethod("Execute_After");
                if (method != null)
                {
                    method.Invoke(aspectInstance, new object[] { hostType.GetMethod(name), retValue });
                }
            }
        }
        /// <summary>
        ///  <para>zh-cn:方法执行之后</para>
        ///  <para>en-us:Befor method execute</para>
        /// </summary>
        /// <param name="name">
        /// <para>zh-cn:方法名</para>
        /// <para>en-us:MethodName</para>
        /// </param>
        /// <param name="args">
        ///  <para>zh-cn:方法参数</para>
        ///  <para>en-us:Method parameters</para>
        /// </param>
        /// <param name="hostType">
        ///  <para>zh-cn:方法所在类类型</para>
        ///  <para>en-us: Class type</para>
        /// </param>
        /// <param name="target">
        ///  <para>zh-cn:方法本身</para>
        ///  <para>en-us:Method delegate</para>
        /// </param>
        [Advice(Kind.Around, Targets = Target.Method)]
        public object Around([Argument(Source.Name)] string name, [Argument(Source.Arguments)] object[] args, [Argument(Source.Type)] Type hostType, [Argument(Source.Target)] Func<object[], object> target)
        {
            var MethodAspects = (Metadata(hostType, name)?.Aspects) ?? EmptyAspects;
            if (MethodAspects == null || MethodAspects.Length == 0) return target.Invoke(args);
            //如果设置按照123456 顺序的话 那么环绕After 就按照654321执行 
            foreach (var aspect in MethodAspects.OrderBy(s => s.Order))
            {
                var aspectInstance = AppCore.GetService(aspect.AppAspectDependencies) ?? Activator.CreateInstance(aspect.AppAspectDependencies);
                MethodInfo method = aspectInstance.GetType().GetMethod("Around_Before");
                if (method != null)
                {
                    args = method.Invoke(aspectInstance, new object[] { hostType.GetMethod(name), args }) as object[];
                   
                }
            }
            object result=null;
            try
            {
                result = target.Invoke(args);
            }
            catch (Exception ex)
            {
                foreach (var aspect in MethodAspects.OrderBy(s => s.Order))
                {
                    var aspectInstance = AppCore.GetService(aspect.AppAspectDependencies) ?? Activator.CreateInstance(aspect.AppAspectDependencies);
                    try
                    {
                        MethodInfo method = aspectInstance.GetType().GetMethod("Around_Error");
                        if (method != null)
                        {
                            method.MakeGenericMethod(ex.GetType()).Invoke(aspectInstance, new object[] { hostType.GetMethod(name), args, ex });
                        }
                    }
                    catch (Exception ex1)
                    {
                        AppRealization.Output.Error(ex1);
                    }
                    throw;
                }
            }
            foreach (var aspect in MethodAspects.OrderByDescending(s => s.Order))
            {
                var aspectInstance = AppCore.GetService(aspect.AppAspectDependencies) ?? Activator.CreateInstance(aspect.AppAspectDependencies);
                MethodInfo method = aspectInstance.GetType().GetMethod("Around_After");
                if (method != null)
                {
                    result = method.Invoke(aspectInstance, new object[] { hostType.GetMethod(name), args, result });
                }
            }
            return result;
        }
    }
}
