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
using Air.Cloud.Modules.Taxin.Model;

namespace Air.Cloud.Core.Standard.Taxin.Client
{
    /// <summary>
    /// <para>zh-cn:Taxin客户端标准</para>
    /// <para>en-us:Taxin client standard</para>
    /// </summary>
    public interface ITaxinClientStandard
    {
        /// <summary>
        /// <para>zh-cn:推送数据包</para>
        /// <para>en-us:Push data package</para>
        /// </summary>
        /// <param name="package">
        /// <para>zh-cn:数据包</para>
        /// <para>en-us:Data package</para>
        /// </param>
        public void Push(TaxinRouteDataPackage package);
        /// <summary>
        /// <para>zh-cn:拉取数据包</para>
        /// <para>en-us:Pull data package</para>
        /// </summary>
        /// <returns>
        /// <para>zh-cn:数据包</para>
        /// <para>en-us:Data package</para>
        /// </returns>
        public TaxinRouteDataPackage Pull();
        /// <summary>
        /// <para>zh-cn:初始化</para>
        /// <para>en-us:init</para>
        /// </summary>
        public void Init();
    }
}
