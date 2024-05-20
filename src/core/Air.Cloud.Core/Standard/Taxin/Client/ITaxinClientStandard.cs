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
using Air.Cloud.Core.Standard.Taxin.Store;
using Air.Cloud.Modules.Taxin.Model;

namespace Air.Cloud.Core.Standard.Taxin.Client
{
    /// <summary>
    /// <para>zh-cn:Taxin客户端标准</para>
    /// <para>en-us:Taxin client standard</para>
    /// </summary>
    public interface ITaxinClientStandard: ITaxinStandard
    {
        /// <summary>
        /// <para>zh-cn:推送数据包</para>
        /// <para>en-us:Push data package</para>
        /// </summary>
        public Task PushAsync();
        /// <summary>
        /// <para>zh-cn:拉取数据包</para>
        /// <para>en-us:Pull data package</para>
        /// </summary>
        /// <returns>
        /// <para>zh-cn:数据包</para>
        /// <para>en-us:Data package</para>
        /// </returns>
        public Task PullAsync();
        /// <summary>
        /// <para>zh-cn:远程检查是否最新</para>
        /// <para>en-us:Remotely check if it's up to date</para>
        /// </summary>
        /// <returns></returns>
        public Task CheckAsync();
    }
}
