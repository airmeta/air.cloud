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
    /// 应用程序执行切入
    /// </summary>
    public interface IAspectExecuteHandler
    {
        /// <summary>
        /// <para>zh-cn:方法执行之前</para>
        /// <para>en-us:Before method execute</para>
        /// </summary>
        /// <param name="methodInfo">
        /// <para>zh-cn:方法执行之前</para>
        /// <para>en-us:Method execute before</para>
        /// </param>
        /// <param name="args"></param>
        public void Execute_Before(MethodInfo methodInfo,object[] args);
        /// <summary>
        /// 执行之后
        /// </summary>
        public void Execute_After(MethodInfo methodInfo,object retValue);
    }
}
