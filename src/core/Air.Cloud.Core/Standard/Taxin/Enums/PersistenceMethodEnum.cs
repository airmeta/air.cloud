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
namespace Air.Cloud.Core.Standard.Taxin.Server
{
    /// <summary>
    /// <para>zh-cn:服务端数据持久化方式</para>
    /// <para>en-us:Server data persistence method</para>
    /// </summary>
    public enum PersistenceMethodEnum
    {
        /// <summary>
        /// <para>zh-cn:存储在文件夹中</para>
        /// <para>en-us:Stored in a folder</para>
        /// </summary>
        Folder = 0,
        /// <summary>
        /// <para>zh-cn:存储在缓存中</para>
        /// <para>en-us:Stored in cache</para>
        /// </summary>
        Cache = 1,
        /// <summary>
        /// <para>zh-cn:存储在键值对中心</para>
        /// <para>en-us:Stored in key-value center</para>
        /// </summary>
        KVCenter=3,
        /// <summary>
        /// <para>zh-cn:其他</para>
        /// <para>en-us:Other</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:使用其他方式则会调用存储钩子并将数据传入,由开发人员自定义存储</para>
        /// <para>en-us:Using other methods will call the storage hook and pass the data, allowing developers to customize the storage</para>
        /// </remarks>
        Other = 4,
    }
}
