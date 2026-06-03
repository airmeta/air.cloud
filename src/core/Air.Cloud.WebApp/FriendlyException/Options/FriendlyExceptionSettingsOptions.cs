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

using Microsoft.Extensions.Configuration;

namespace Air.Cloud.WebApp.FriendlyException.Options;

/// <summary>
/// 友好异常配置选项
/// </summary>
[ConfigurationInfo("FriendlyExceptionSettings")]
public sealed class FriendlyExceptionSettingsOptions : IConfigurableOptions<FriendlyExceptionSettingsOptions>
{
    /// <summary>
    /// 隐藏错误码
    /// </summary>
    public bool? HideErrorCode { get; set; }

    /// <summary>
    /// 默认错误码
    /// </summary>
    public string DefaultErrorCode { get; set; }

    /// <summary>
    /// 默认错误消息
    /// </summary>
    public string DefaultErrorMessage { get; set; }

    /// <summary>
    /// 标记 Oops.Oh 为业务异常
    /// </summary>
    /// <remarks>也就是不会进入异常处理</remarks>
    public bool? ThrowBah { get; set; }

    /// <summary>
    /// 选项后期配置
    /// </summary>
    /// <param name="options"></param>
    /// <param name="configuration"></param>
    public void PostConfigure(FriendlyExceptionSettingsOptions options, IConfiguration configuration)
    {
        options.HideErrorCode ??= false;
        options.DefaultErrorCode ??= string.Empty;
        options.DefaultErrorMessage ??= "Internal Server Error";
        options.ThrowBah ??= false;
    }
}
