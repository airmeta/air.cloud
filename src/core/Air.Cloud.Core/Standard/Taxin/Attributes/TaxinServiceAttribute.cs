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
namespace Air.Cloud.Core.Standard.Taxin.Attributes
{
    /// <summary>
    /// <para>zh-cn:Taxin服务特性</para>
    /// <para>en-us:Taxin service attribute</para>
    /// </summary>
    public  class TaxinServiceAttribute:Attribute
    {
        /// <summary>
        /// <para>zh-cn:服务名称</para>
        /// <para>en-us:Service name</para>
        /// </summary>
        public string ServiceName { get; set; }
        /// <summary>
        /// <para>zh-cn:版本调用</para>
        /// <para>en-us: Version calls </para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:该参数指明允许在哪个版本中发起调用,客户端将此信息传入服务端时,服务端将会为该接口做版本控制</para>
        /// <para>en-us:This parameter indicates which version is allowed to initiate the call, and when the client passes this information to the server, the server will version the interface</para>
        /// </remarks>
        public Version VersionCalls { get; set; } = AppCore.Settings.VersionSerialize;
    }
}
