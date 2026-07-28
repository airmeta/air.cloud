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

using Air.Cloud.Core.Modules.AppAspect.Internal;
using InjectionAttribute = AspectInjector.Broker.Injection;
namespace Air.Cloud.Core.Modules.AppAspect.Attributes
{
    /// <summary>
    /// <para>zh-cn:Aspect切入挂载</para>
    /// <para>Mount Aspect</para>
    /// </summary>
    [Injection(typeof(AppInternalAspectDependency))]
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class AspectAttribute : Attribute
    {
        /// <summary>
        /// <para>zh-cn:执行顺序</para>
        /// <para>en-us:Execute order</para>
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// <para>zh-cn:Aspect 实现类</para>
        /// <para>en-us:Aspect impl type</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn: 该类必须要实现IAspectAroundHandler</para>
        /// </remarks>
        public Type AppAspectDependencies { get; set; }

        /// <summary>
        /// <para>zh-cn:初始化方法级 Aspect 切入点，并保留传入的执行顺序。具体执行方向由处理器类型和切面调度逻辑决定。</para>
        /// <para>en-us:Initializes a method-level Aspect interception point and preserves the supplied execution order. The effective direction is determined by the handler type and aspect dispatch logic.</para>
        /// </summary>
        /// <param name="appAspectDependencies">
        /// <para>zh-cn:实现 <c>IAspectAroundHandler</c> 或 <c>IAspectExecuteHandler</c> 的切面处理器类型。</para>
        /// <para>en-us:The aspect handler type implementing <c>IAspectAroundHandler</c> or <c>IAspectExecuteHandler</c>.</para>
        /// </param>
        /// <param name="Order">
        /// <para>zh-cn:切面执行顺序，默认值为 1。</para>
        /// <para>en-us:The aspect execution order. The default is 1.</para>
        /// </param>
        public AspectAttribute(Type appAspectDependencies, int Order = 1)
        {
            AppAspectDependencies = appAspectDependencies;
            this.Order = Order;
        }
    }



}
