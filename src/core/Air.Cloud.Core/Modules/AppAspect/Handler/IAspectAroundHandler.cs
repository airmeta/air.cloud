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
    /// 应用程序环绕
    /// </summary>
    public interface IAspectAroundHandler
    {
        /// <summary>
        /// 执行环绕之前(可修改方法入参)
        /// </summary>
        public object[] Around_Before(MethodInfo methodInfo, object[] args);

        /// <summary>
        /// 执行环绕之后(可修改方法返回值)
        /// </summary>
        public object Around_After(MethodInfo methodInfo, object[] args, object result);

    }
}
