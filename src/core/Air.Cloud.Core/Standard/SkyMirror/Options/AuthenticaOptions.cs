
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
using Air.Cloud.Core.App.Options;

namespace Air.Cloud.Core.Standard.SkyMirror.Options
{
    /// <summary>
    /// <para>zh-cn:表示 SkyMirror 认证同步配置项，包含服务端地址、推送路由、存储间隔和重试间隔。</para>
    /// <para>en-us:Represents SkyMirror authentication synchronization options, including server address, push route, store interval, and retry interval.</para>
    /// </summary>
    [ConfigurationInfo("AuthenticaSettings")]
    public  class AuthenticaOptions
    {
        /// <summary>
        /// <para>zh-cn:获取有效服务端地址；未显式配置服务端地址时回退到应用网关地址。</para>
        /// <para>en-us:Gets the effective server address; when no server address is explicitly configured, falls back to the application gateway address.</para>
        /// </summary>
        public string ServerAddress { get; set; }

        /// <summary>
        /// <para>zh-cn:服务端地址</para>
        /// <para>en-us:Server address</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:如果配置文件中没有配置服务端地址，则使用网关地址</para>
        /// <para>en-us:If the server address is not configured in the configuration file, the gateway address is used</para>
        /// </remarks>
        public string GetServerAddress() => string.IsNullOrEmpty(ServerAddress) ? AppConfiguration.GetConfig<AppSettingsOptions>().GateWayAddress : ServerAddress;
        /// <summary>
        /// <para>zh-cn:获取或设置认证端点数据推送路由。</para>
        /// <para>en-us:Gets or sets the authentication endpoint data push route.</para>
        /// </summary>
        public string PushRoute { get; set; }="/authenticaton/endpoint/push";

        /// <summary>
        /// <para>zh-cn:获取或设置认证数据本地存储间隔，单位为毫秒。</para>
        /// <para>en-us:Gets or sets the local authentication data store interval, in milliseconds.</para>
        /// </summary>
        public int StoreIntervalMillis{get;set; }= 60000;

        /// <summary>
        /// <para>zh-cn:重试间隔时间，单位毫秒</para>
        /// <para>en-us:Retry interval time in milliseconds</para>
        /// </summary>
        public int RetryIntervalMillis{get;set; }= 10000;

    }
}
