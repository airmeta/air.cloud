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
using Air.Cloud.WebApp.DataValidation.Extensions.Options;
using Air.Cloud.WebApp.FriendlyException.Extensions.Options;

namespace Air.Cloud.WebApp.Extensions.Options;

/// <summary>
/// Configuration 服务配置选项
/// </summary>
public sealed class InjectServiceOptions
{
    /// <summary>
    /// 数据校验配置
    /// </summary>
    public Action<DataValidationServiceOptions> DataValidationConfigure { get; set; }

    /// <summary>
    /// 友好异常配置
    /// </summary>
    public Action<FriendlyExceptionServiceOptions> FriendlyExceptionConfigure { get; set; }
}