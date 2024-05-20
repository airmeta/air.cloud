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
using Air.Cloud.Core.Plugins.Http.Result;

namespace Air.Cloud.Core.Standard.Taxin.Result
{
    /// <summary>
    /// <para>zh-cn:Taxin 动作结果</para>
    /// <para>en-us:Taxin action result</para>
    /// </summary>
    public class TaxinActionResult: IHttpResultBase
    {
        /// <summary>
        /// <para>zh-cn:是否请求成功</para>
        /// <para>en-us:Is success</para>
        /// </summary>
        public bool IsSuccess { get; set; }
        /// <summary>
        /// <para>zh-cn:标记是否修改</para>
        /// <para>en-us:Is change</para>
        /// </summary>
        public bool IsChange { get; set; }
        /// <summary>
        /// <para>zh-cn:旧标记</para>
        /// <para>en-us:Old check tag</para>
        /// </summary>
        public string OldTag { get; set; }
        /// <summary>
        /// <para>zh-cn:新标记</para>
        /// <para>en-us:New check tag</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:如果IsChange为false,则OldTag和NewTag结果是相同的</para>
        /// <para>en-us: If the value of the ischange property is false, the oldtag is the same as the newtag</para>
        /// </remarks>
        public string NewTag { get; set; }

        /// <summary>
        /// <para>zh-cn: 消息提示</para>
        /// <para>en-us: message </para>
        /// </summary>
        public string Message { get; set; }

    }
}
