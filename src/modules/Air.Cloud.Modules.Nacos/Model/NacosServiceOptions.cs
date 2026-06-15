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
using Air.Cloud.Core;
using Air.Cloud.Core.App;
using Air.Cloud.Core.Attributes;

namespace Air.Cloud.Modules.Nacos.Model
{
    /// <summary>
    /// <para>zh-cn:Nacos 服务配置，统一描述服务发现、配置中心和远程配置加载所需的连接参数。</para>
    /// <para>en-us:Nacos service options that describe connection settings for service discovery, config center, and remote configuration loading.</para>
    /// </summary>
    [ConfigurationInfo("NacosServiceOptions")]
    public class NacosServiceOptions
    {
        /// <summary>
        /// <para>zh-cn:当前服务注册到 Nacos 时使用的服务实例标识，默认来自 Air.Cloud PID 插件。</para>
        /// <para>en-us:The service instance identifier used when registering the current service to Nacos; defaults to the Air.Cloud PID plugin.</para>
        /// </summary>
        public static string ServiceId = AppRealization.PID.Get();

        /// <summary>
        /// <para>zh-cn:Nacos 服务端地址列表，例如 http://127.0.0.1:8848。为空时可回退读取 ServerAddress。</para>
        /// <para>en-us>Nacos server address list, for example http://127.0.0.1:8848. When empty, ServerAddress can be used as a fallback.</para>
        /// </summary>
        public IList<string> ServerAddresses { get; set; } = new List<string>();

        /// <summary>
        /// <para>zh-cn:Nacos 单节点地址，便于与 ConsulAddress 形式保持接近；会并入 ServerAddresses。</para>
        /// <para>en-us>A single Nacos server address, close to the ConsulAddress style; it is merged into ServerAddresses.</para>
        /// </summary>
        public string ServerAddress { get; set; }

        /// <summary>
        /// <para>zh-cn:Nacos 命名空间标识，留空时使用 public 命名空间。</para>
        /// <para>en-us>Nacos namespace id; when empty, the public namespace is used.</para>
        /// </summary>
        public string Namespace { get; set; }

        /// <summary>
        /// <para>zh-cn:Nacos 上下文路径，默认 nacos。</para>
        /// <para>en-us>Nacos context path, defaulting to nacos.</para>
        /// </summary>
        public string ContextPath { get; set; } = "nacos";

        /// <summary>
        /// <para>zh-cn:Nacos 用户名；启用鉴权的 Nacos 服务端需要配置。</para>
        /// <para>en-us>Nacos user name, required when server authentication is enabled.</para>
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// <para>zh-cn:Nacos 密码；启用鉴权的 Nacos 服务端需要配置。</para>
        /// <para>en-us>Nacos password, required when server authentication is enabled.</para>
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// <para>zh-cn:配置中心默认分组。</para>
        /// <para>en-us>The default config center group.</para>
        /// </summary>
        public string ConfigGroup { get; set; } = "DEFAULT_GROUP";

        /// <summary>
        /// <para>zh-cn:服务发现默认分组。</para>
        /// <para>en-us>The default naming group.</para>
        /// </summary>
        public string ServiceGroup { get; set; } = "DEFAULT_GROUP";

        /// <summary>
        /// <para>zh-cn:配置拉取超时时间，单位毫秒。</para>
        /// <para>en-us>Config pull timeout in milliseconds.</para>
        /// </summary>
        public long ConfigTimeoutMs { get; set; } = 5000;

        /// <summary>
        /// <para>zh-cn:Nacos SDK 默认超时时间，单位毫秒。</para>
        /// <para>en-us>The default timeout passed to the Nacos SDK, in milliseconds.</para>
        /// </summary>
        public int DefaultTimeOut { get; set; } = 15000;

        /// <summary>
        /// <para>zh-cn:当前服务名称。</para>
        /// <para>en-us>The current service name.</para>
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// <para>zh-cn:当前服务地址，例如 http://127.0.0.1:5000。</para>
        /// <para>en-us>The current service address, for example http://127.0.0.1:5000.</para>
        /// </summary>
        public string ServiceAddress { get; set; }

        /// <summary>
        /// <para>zh-cn:注册实例所属集群名称。</para>
        /// <para>en-us>The cluster name of the registered instance.</para>
        /// </summary>
        public string ClusterName { get; set; } = "DEFAULT";

        /// <summary>
        /// <para>zh-cn:注册实例权重。</para>
        /// <para>en-us>The weight of the registered instance.</para>
        /// </summary>
        public double Weight { get; set; } = 1D;

        /// <summary>
        /// <para>zh-cn:是否注册为临时实例。</para>
        /// <para>en-us>Whether to register as an ephemeral instance.</para>
        /// </summary>
        public bool Ephemeral { get; set; } = true;

        /// <summary>
        /// <para>zh-cn:是否启用服务注册。</para>
        /// <para>en-us>Whether service registration is enabled.</para>
        /// </summary>
        public bool EnableServiceRegister { get; set; } = true;

        /// <summary>
        /// <para>zh-cn:是否加载公共配置文件。</para>
        /// <para>en-us>Whether common configuration should be loaded.</para>
        /// </summary>
        public bool EnableCommonConfig { get; set; } = true;

        /// <summary>
        /// <para>zh-cn:公共配置 DataId。</para>
        /// <para>en-us>The common configuration data id.</para>
        /// </summary>
        public string CommonConfigDataId { get; set; } = AppConst.CommonEnvironmentConfigFileFullName;

        /// <summary>
        /// <para>zh-cn:服务配置 DataId；为空时使用 appsettings.{环境}.json。</para>
        /// <para>en-us>The service configuration data id; when empty, appsettings.{environment}.json is used.</para>
        /// </summary>
        public string ConfigDataId { get; set; } = AppConst.SystemEnvironmentConfigFileFullName;
    }
}
