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
namespace Air.Cloud.Core.Standard.Taxin.Enums
{
    /// <summary>
    /// <para>zh-cn:Taxin 客户端请求其他多实例客户端时版本类型选择枚举</para>
    /// <para>en-us:When a Taxin client requests a different multi-instance client, the version type is selected as an enumeration</para>
    /// </summary>
    public enum BalanceTypeEnum
    {
        /// <summary>
        /// <para>zh-cn:在可用版本中使用随机版本</para>
        /// <para>en-us:Use a random version in the available version</para>
        /// </summary>
        Random,
        /// <summary>
        /// <para>zh-cn:低版本优先</para>
        /// <para>en-us:Low version priority</para>
        /// </summary>
        Low,
        /// <summary>
        /// <para>zh-cn:高版本优先</para>
        /// <para>en-us:High version priority</para>
        /// </summary>
        High,
    }
}
