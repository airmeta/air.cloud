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

using Air.Cloud.Core.Standard.TraceLog.Enums;

namespace Air.Cloud.Core.Standard.TraceLog
{
    /// <summary>
    ///  .<para>zh-cn:跟踪日志选项</para>
    ///  .<para>en-us:Trace log options</para>
    /// </summary>
    public interface ITraceLogOptions
    {
        /// <summary>
        /// <para>zh-cn:是否启用滚动记录</para>
        /// <para>en-us:Whether to enable rolling log</para>
        /// </summary>
        public bool EnableRollingLog { get; set; }
        /// <summary>
        /// <para>zh-cn:滚动记录等级</para>
        /// <para>en-us:</para>
        /// </summary>
        public RollingLevelEnum RollingLevel { get; set; }
    }
}
