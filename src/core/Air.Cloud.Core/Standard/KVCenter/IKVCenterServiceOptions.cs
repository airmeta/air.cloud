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
namespace Air.Cloud.Core.Standard.KVCenter
{
    /// <summary>
    /// <para>zh-cn:键值存储中心选项</para>
    /// <para>en-us:KVCenterServiceOptions</para>
    /// </summary>
    public interface IKVCenterServiceOptions
    {
        /// <summary>
        /// <para>zh-cn:键</para>
        /// <para>en-us:Key</para>
        /// </summary>
        public string Key { get; set; }
        /// <summary>
        /// <para>zh-cn:值</para>
        /// <para>en-us:Value</para>
        /// </summary>
        public string Value { get; set; }
    }

    /// <summary>
    /// <para>zh-cn:默认键值对参数</para>
    /// <para>en-us:Default key-value pair parameters</para>
    /// </summary>
    public class DefaultKVCenterServiceOptions: IKVCenterServiceOptions
    {
        /// <summary>
        /// <para>zh-cn:键</para>
        /// <para>en-us:Key</para>
        /// </summary>
        public string Key { get; set; }
        /// <summary>
        /// <para>zh-cn:值</para>
        /// <para>en-us:Value</para>
        /// </summary>
        public string Value { get; set; }
    }
}
