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
using Air.Cloud.Core.Standard.DynamicServer.Internal;

using Microsoft.Extensions.Configuration;

namespace Air.Cloud.Core.Standard.DynamicServer.Options;

/// <summary>
/// 依赖注入配置选项
/// </summary>
[ConfigurationInfo("DependencyInjectionSettings")]
public sealed class DependencyInjectionSettingsOptions : IConfigurableOptions<DependencyInjectionSettingsOptions>
{
    /// <summary>
    /// 外部注册定义
    /// </summary>
    public ExternalService[] Definitions { get; set; }

    /// <summary>
    /// 后期配置
    /// </summary>
    /// <param name="options"></param>
    /// <param name="configuration"></param>
    public void PostConfigure(DependencyInjectionSettingsOptions options, IConfiguration configuration)
    {
        options.Definitions ??= Array.Empty<ExternalService>();
    }
}