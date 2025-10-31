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
using Air.Cloud.Core.Standard.Taxin.Enums;

namespace Air.Cloud.Core.Standard.Taxin.Options
{
    /// <summary>
    /// <para>zh-cn:Taxin客户端配置项</para>
    /// <para>en-us:Taxin client options</para>
    /// </summary>
    [ConfigurationInfo("TaxinSettings")]
    public class TaxinOptions
    {
        /// <summary>
        /// <para>zh-cn:服务端地址</para>
        /// <para>en-us:Server address</para>
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
        /// <para>zh-cn:检查频率</para>
        /// <para>en-us:CheckAsync Rate</para>
        /// </summary>
        public int CheckRate { get; set; } = 5;
        /// <summary>
        /// <para>zh-cn:检查路由地址</para>
        /// <para>en-us:CheckAsync route path </para>
        /// </summary>
        public string CheckRoute { get; set; } = "/air_cloud_micro/check";
        /// <summary>
        /// <para>zh-cn:拉取路由地址</para>
        /// <para>en-us:PullAsync route path </para>
        /// </summary>
        public string PullRoute { get; set; } = "/air_cloud_micro/pull";
        /// <summary>
        /// <para>zh-cn:推送路由地址</para>
        /// <para>en-us:PushAsync route path</para>
        /// </summary>
        public string PushRoute { get; set; } = "/air_cloud_micro/push";
        /// <summary>
        /// <para>zh-cn:下线路由地址</para>
        /// <para>en-us:offline route path</para>
        /// </summary>
        public string OffLineRoute { get; set; } = "/air_cloud_micro/off";

        /// <summary>
        /// <para>zh-cn:是否持久化</para>
        /// <para>en-us:Persistence</para>
        /// </summary>
        public bool Persistence { get; set; } = true;
        /// <summary>
        /// <para>zh-cn:持久化间隔(秒)</para>
        /// <para>en-us:Persistence rate(seconds)</para>
        /// </summary>
        public int PersistenceRate { get; set; } = 30;
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
        /// <summary>
        /// <para>zh-cn:持久化输出</para>
        /// <para>en-us:Persistence output</para>
        /// </summary>
        public bool PersistenceOutput { get; set; } = false;
        /// <summary>
        /// <para>zh-cn:负载均衡模式</para>
        /// <para>en-us:Balance type</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:0 第一个版本 1 随机版本 2低版本优先 3高版本优先</para>
        /// <para>en-us:0 First Version 1 Random Version 2 Low Version Priority 3 High Version Priority</para>
        /// </remarks>
        public BalanceTypeEnum BalanceType { get; set; } = BalanceTypeEnum.Low;
    }
}
