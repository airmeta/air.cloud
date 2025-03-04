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

using System.Reflection;

namespace Air.Cloud.Core.Modules.AppAspect.Handler
{
    /// <summary>
    /// <para>zh-cn:应用程序环绕</para>
    /// <para>en-us:Application Aspect arround handler</para>
    /// </summary>
    public interface IAspectAroundHandler
    {
        /// <summary>
        ///     <para>zh-cn:环绕执行之前(可修改参数)</para>
        ///     <para>en-us:Before method around(can change args data)</para>
        /// </summary>
        /// <param name="methodInfo">
        ///     <para>zh-cn:方法</para>
        ///     <para>en-us:Method information</para>
        /// </param>
        /// <param name="args">
        ///     <para>zh-cn:方法参数</para>
        ///     <para>en-us:method args</para>  
        /// </param>
        public object[] Around_Before(MethodInfo methodInfo, object[] args);

        /// <summary>
        ///     <para>zh-cn:环绕执行之后(可修改返回值)</para>
        ///     <para>en-us:Before method around</para>
        /// </summary>
        /// <param name="methodInfo">
        ///     <para>zh-cn:方法</para>
        ///     <para>en-us:Method information</para>
        /// </param>
        /// <param name="args">
        ///     <para>zh-cn:方法参数</para>
        ///     <para>en-us:method args</para>  
        /// </param>
        /// <param name="result">
        ///     <para>zh-cn:方法返回值</para>
        ///     <para>en-us:return value</para>
        /// </param>
        public object Around_After(MethodInfo methodInfo, object[] args, object result);

        /// <summary>
        ///     <para>zh-cn:方法环绕时异常</para>
        ///     <para>en-us:Method arround error</para>
        /// </summary>
        /// <typeparam name="TException">
        ///     <para>zh-cn:异常信息</para>
        ///     <para>en-us:Exception</para>
        /// </typeparam>
        /// <param name="methodInfo">
        ///     <para>zh-cn:方法信息</para>
        ///     <para>en-us:Method information</para>
        /// </param>
        /// <param name="args">
        ///     <para>zh-cn:方法参数</para>
        ///     <para>en-us:method args</para>  
        /// </param>
        /// <param name="exception">
        ///     <para>zh-cn:异常信息</para>
        ///     <para>en-us:exception information</para>  
        /// </param>
        public void Around_Error<TException>(MethodInfo methodInfo, object[] args, TException exception) where TException: Exception,new();


    }
}
