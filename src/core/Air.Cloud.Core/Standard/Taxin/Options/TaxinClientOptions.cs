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
using Air.Cloud.Core.App;

namespace Air.Cloud.Modules.Taxin.Client
{
    /// <summary>
    /// <para>zh-cn:Taxin客户端配置项</para>
    /// <para>en-us:Taxin client options</para>
    /// </summary>
    public class TaxinClientOptions
    {
        /// <summary>
        /// <para>zh-cn:网关地址</para>
        /// <para>en-us:GateWay address</para>
        /// </summary>
        public string GateWayAddress => AppCore.Configuration["AppSettings:GateWayAddress"];
        /// <summary>
        /// <para>zh-cn:服务端地址</para>
        /// <para>en-us:Server address</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:如果配置文件中没有配置服务端地址，则使用网关地址</para>
        /// <para>en-us:If the server address is not configured in the configuration file, the gateway address is used</para>
        /// </remarks>
        public string ServerAddress => (AppCore.Configuration["TaxinSettings:ServerAddress"] ?? GateWayAddress);
    }
}
