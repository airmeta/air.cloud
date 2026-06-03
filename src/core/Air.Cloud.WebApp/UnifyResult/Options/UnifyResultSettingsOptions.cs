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

namespace Air.Cloud.WebApp.UnifyResult.Options;

/// <summary>
/// 规范化配置选项
/// </summary>
[ConfigurationInfo("UnifyResultSettings")]
public sealed class UnifyResultSettingsOptions : IConfigurableOptions<UnifyResultSettingsOptions>
{
    /// <summary>
    /// 设置返回 200 状态码列表
    /// <para>默认：401，403，如果设置为 null，则标识所有状态码都返回 200 </para>
    /// </summary>
    public int[] Return200StatusCodes { get; set; }

    /// <summary>
    /// 适配（篡改）Http 状态码（只支持短路状态码，比如 401，403，500 等）
    /// </summary>
    public int[][] AdaptStatusCodes { get; set; }

    /// <summary>
    /// 是否支持 MVC 控制台规范化处理
    /// </summary>
    public bool? SupportMvcController { get; set; }

    /// <summary>
    /// 选项后期配置
    /// </summary>
    /// <param name="options"></param>
    /// <param name="configuration"></param>
    public void PostConfigure(UnifyResultSettingsOptions options, IConfiguration configuration)
    {
        options.Return200StatusCodes ??= new[] { 401, 403 };
        options.SupportMvcController ??= false;
    }
}