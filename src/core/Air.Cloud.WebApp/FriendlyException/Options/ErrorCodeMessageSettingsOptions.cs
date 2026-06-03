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

namespace Air.Cloud.WebApp.FriendlyException.Options;

/// <summary>
/// 异常配置选项，最优的方式是采用后期配置，也就是所有异常状态码先不设置（推荐）
/// </summary>
[ConfigurationInfo("ErrorCodeMessageSettings")]
public sealed class ErrorCodeMessageSettingsOptions : IConfigurableOptions
{
    /// <summary>
    /// 异常状态码配置列表
    /// </summary>
    public object[][] Definitions { get; set; }
}
