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
namespace Air.Cloud.Core.Standard.TraceLog
{
    /// <summary>
    /// <para>zh-cn:跟踪日志内容</para>
    /// <para>en-us:Trace log content</para>
    /// </summary>
    public  interface ITraceLogContent
    {
        /// <summary>
        /// <para>zh-cn:编号</para>
        /// <para>en-us:id</para>
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// <para>zh-cn:标签</para>
        /// <para>en-us:Tags</para>
        /// </summary>
        public string Tags { get; set; }

    }
}
