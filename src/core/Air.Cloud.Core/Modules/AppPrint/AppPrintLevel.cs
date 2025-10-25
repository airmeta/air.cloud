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

namespace Air.Cloud.Core.Modules.AppPrint
{
    /// <summary>
    /// <para>zh-cn:输出打印等级</para>
    /// <para>en-us:Output message level</para>
    /// </summary>
    public enum AppPrintLevel
    {
        /// <summary>
        /// <para>zh-cn:普通消息</para>
        /// <para>en-us:Information</para>
        /// </summary>
        Information,
        /// <summary>
        /// <para>zh-cn:警告消息</para>
        /// <para>en-us:Warning</para>
        /// </summary>
        Warn,
        /// <summary>
        /// <para>zh-cn:错误消息</para>
        /// <para>en-us:Error</para>
        /// </summary>
        Error,
        /// <summary>
        /// <para>zh-cn:调试消息</para>
        /// <para>en-us:Debug</para>
        /// </summary>
        Debug,
        /// <summary>
        /// <para>zh-cn:追踪消息</para>
        /// <para>en-us:Trace</para>
        /// </summary>
        Trace
    }
}
