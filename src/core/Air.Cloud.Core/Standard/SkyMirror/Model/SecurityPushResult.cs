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
namespace Air.Cloud.Core.Standard.SkyMirror.Model
{
    /// <summary>
    /// <para>zh-cn:表示 SkyMirror 安全数据推送结果。</para>
    /// <para>en-us:Represents the SkyMirror security data push result.</para>
    /// </summary>
    public  class SecurityPushResult
    {
        /// <summary>
        /// <para>zh-cn:获取或设置推送是否成功。</para>
        /// <para>en-us:Gets or sets whether the push succeeded.</para>
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// <para>zh-cn:获取或设置推送结果消息。</para>
        /// <para>en-us:Gets or sets the push result message.</para>
        /// </summary>
        public string Message { get; set; }

    }
}
