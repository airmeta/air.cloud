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
using Air.Cloud.Core.Attributes;

namespace Air.Cloud.Modules.Consul.Model
{
    [ConfigurationInfo("ConsulServiceOptions")]
    public class ConsulServiceOptions
    {
        // 服务注册地址（Consul的地址）
        public string ConsulAddress { get; set; }
        // 服务ID
        public string ServiceId { get; set; }
        // 服务名称
        public string ServiceName { get; set; } = null;

        // 健康检查地址
        public string HealthCheck { get; set; } = "/Health";
        /// <summary>
        /// 服务地址
        /// </summary>
        public string ServiceAddress { get; set; } = null;
        /// <summary>
        /// 是否加载公共配置文件
        /// </summary>
        public bool EnableCommonConfig { get; set; } = true;
        /// <summary>
        /// 公共配置文件路由地址
        /// </summary>
        public string CommonConfigFileRoute { get; set; } = "Common";

        /// <summary>
        /// 获取ConsulService 配置信息  配置文件优先级>扫描结果
        /// </summary>
        /// <param name="info"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static ConsulServiceOptions GetConsulServiceOptions(AppRuntimeOptions info, ConsulServiceOptions options)
        {
            options.ConsulAddress = options.ConsulAddress;
            options.ServiceId = info.PID;
            options.ServiceAddress = options.ServiceAddress ?? "http://" + info.IPAddress + ":" + info.Port + "/";
            return options;
        }

        /// <summary>
        /// 验证当前配置项有效性
        /// </summary>
        /// <returns></returns>
        public bool ValidateConfig()
        {
            if (string.IsNullOrEmpty(ServiceAddress) || string.IsNullOrEmpty(ConsulAddress)) return false;
            return true;
        }
    }
}
