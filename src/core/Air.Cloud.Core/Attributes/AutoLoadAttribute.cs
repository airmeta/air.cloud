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
namespace Air.Cloud.Core.Attributes
{
    /// <summary>
    /// <para>
    /// zh-cn:自动加载特性
    /// </para>
    /// <para>
    /// en-us:Auto load attribute
    /// </para>
    /// </summary>
    public class AutoLoadAttribute:Attribute
    {
        /// <summary>
        /// <para>
        /// zh-cn:是否自动加载
        /// </para>
        /// <para>
        /// en-us:Whether to load automatically
        /// </para>
        /// </summary>
        public bool Load { get; set; } = true;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="Load">是否加载</param>
        public  AutoLoadAttribute(bool Load = true)
        {
            this.Load= Load;
        }
    }
}
