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
namespace Air.Cloud.Core.Standard.Taxin.Store
{
    /// <summary>
    /// <para>zh-cn:Taxin 基础标准</para>
    /// <para>en-us:Taxin base standard</para>
    /// </summary>
    public interface ITaxinStandard : IStandard
    {
        /// <summary>
        /// <para>zh-cn:实例上线</para>
        /// <para>en-us:The client goes online</para>
        /// </summary>
        public Task OnLineAsync();
        /// <summary>
        /// <para>zh-cn:实例下线</para>
        /// <para>en-us:The client goes offline</para>
        /// </summary>
        /// <returns></returns>
        public Task OffLineAsync();
    }
}
