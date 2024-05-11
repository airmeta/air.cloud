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

namespace Air.Cloud.Modules.Taxin.Server
{
    /// <summary>
    /// <para>zh-cn:Taxin服务端配置项</para>
    /// <para>en-us:Taxin server options</para>
    /// </summary>
    public class TaxinServerOptions
    {
        /// <summary>
        /// <para>zh-cn:是否持久化</para>
        /// <para>en-us:Persistence</para>
        /// </summary>
        public bool Persistence { get; set; } = true;
        /// <summary>
        /// <para>zh-cn:持久化方式</para>
        /// <para>en-us:Persistence method</para>
        /// </summary>
        public PersistenceMethodEnum PersistenceMethod { get; set; } = PersistenceMethodEnum.Folder;
        /// <summary>
        /// <para>zh-cn:持久化路径</para>
        /// <para>en-us:Persistence path</para>
        /// </summary>
        public string PersistencePath { get; set; } = "Taxin";
    }
}
