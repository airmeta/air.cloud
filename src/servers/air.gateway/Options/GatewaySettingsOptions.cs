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
namespace air.gateway.Options;

/// <summary>
/// <para>zh-cn:Gateway 加载 Ocelot 路由配置时使用的配置源。</para>
/// <para>en-us:Configuration source used by Gateway when loading Ocelot routing settings.</para>
/// </summary>
public enum GatewayOcelotConfigurationSource
{
    /// <summary>
    /// <para>zh-cn:从 Consul 中读取服务级配置和公共配置。这是默认值，用于保持已有部署行为。</para>
    /// <para>en-us:Loads service-level and common configuration from Consul. This is the default value that preserves the existing deployment behavior.</para>
    /// </summary>
    Consul = 0,

    /// <summary>
    /// <para>zh-cn:只从本地配置文件读取 Ocelot `Routes` 和 `GlobalConfiguration`，适用于独立迁移部署或离线部署。</para>
    /// <para>en-us:Loads Ocelot `Routes` and `GlobalConfiguration` only from local configuration files, suitable for standalone relocation or offline deployment.</para>
    /// </summary>
    File = 1,

    /// <summary>
    /// <para>zh-cn:先从 Consul 读取配置，再叠加本地配置文件。本地配置拥有更高优先级，适用于临时覆盖远程路由。</para>
    /// <para>en-us:Loads configuration from Consul first and overlays local configuration files afterwards. Local configuration has higher priority and is intended for temporary route overrides.</para>
    /// </summary>
    ConsulThenFile = 2
}

/// <summary>
/// <para>zh-cn:Gateway 自身运行配置。该配置只控制 Gateway 的部署和配置加载方式，不改变 Ocelot 路由模型。</para>
/// <para>en-us:Gateway runtime settings. These settings control Gateway deployment and configuration loading only; they do not change the Ocelot routing model.</para>
/// </summary>
public sealed class GatewaySettingsOptions
{
    /// <summary>
    /// <para>zh-cn:Ocelot 配置源。未配置时默认使用 `Consul`，以兼容历史部署；设置为 `File` 时完全使用本地配置文件。</para>
    /// <para>en-us:Ocelot configuration source. When omitted it defaults to `Consul` for backward compatibility; when set to `File`, local configuration files are used exclusively.</para>
    /// </summary>
    public GatewayOcelotConfigurationSource OcelotConfigurationSource { get; set; } = GatewayOcelotConfigurationSource.Consul;
}
