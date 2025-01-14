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
using Air.Cloud.Core.Standard.Taxin.Model;

namespace Air.Cloud.Core.Standard.Taxin.Events
{
    /// <summary>
    /// <para>zh-cn:Taxin存储事件参数</para>
    /// <para>en-us:Taxin store event args</para>
    /// </summary>
    public class TaxinStoreEventArgs : EventArgs
    {
        /// <summary>
        /// <para>zh-cn:数据包</para>
        /// <para>en-us:Taxin data packages</para>
        /// </summary>
        public IDictionary<string, IEnumerable<TaxinRouteDataPackage>> Packages { get; set; }
    }
}
