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

namespace Air.Cloud.Core.Modules.DynamicApp.Enums
{
    /// <summary>
    /// <para>zh-cn:动态应用使用场景枚举</para>
    /// <para>en-us: Dynamic Application Usage Scenario Enumeration</para>
    /// </summary>
    public enum DynamicAppUsageEnum
    {
        /// <summary>
        /// <para>zh-cn:过滤器</para>
        /// <para>en-us:Filter</para>
        /// </summary>
        /// <remarks>
        ///  <para>zh-cn:自动应用到全局应用程序中,需要使用此枚举的类实现自 IAsyncActionFilter 接口</para>
        /// </remarks>
        Filter,
        /// <summary>
        /// <para>zh-cn:中间件</para>
        /// <para>en-us:Middleware</para>
        /// </summary>
        Middleware
    }
}
