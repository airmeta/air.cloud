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
using Air.Cloud.Core.Dependencies;

namespace Air.Cloud.Core.Standard.JinYiWei
{
    /// <summary>
    /// <para>zh-cn:日志追踪</para> 
    /// <para>en-us: Trace log standard</para>
    /// </summary>
    public  interface ITraceLogStandard:IStandard,ITransient
    {
        /// <summary>
        /// <para>zh-cn:写入日志信息</para>
        /// <para>en-us:Write log data</para>
        /// </summary>
        /// <param name="logContent">
        /// <para>zh-cn:日志信息</para>
        /// <para>en-us:Log content</para>
        /// </param>
        /// <param name="Tag">
        /// <para>zh-cn:标签信息</para>
        /// <para>en-us:Tag information</para>
        /// </param>
        public void Write(string logContent, KeyValuePair<string, string>? Tag = null);
        /// <summary>
        /// <para>zh-cn:写入日志信息</para>
        /// <para>en-us:Write log data</para>
        /// </summary>
        /// <param name="logContent">
        /// <para>zh-cn:日志信息</para>
        /// <para>en-us:Log content</para>
        /// </param>
        /// <param name="Tag">
        /// <para>zh-cn:标签信息</para>
        /// <para>en-us:Tag information</para>
        /// </param>
        public void Write(object logContent, KeyValuePair<string, string>? Tag = null);
    }
}
