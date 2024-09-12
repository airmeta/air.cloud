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

namespace Air.Cloud.Plugins.SpecificationDocument.Internal;

/// <summary>
/// 规范化文档授权登录配置信息
/// </summary>
[IgnoreScanning]
public sealed class SpecificationLoginInfo
{
    /// <summary>
    /// 是否启用授权控制
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// 检查登录地址
    /// </summary>
    public string CheckUrl { get; set; }

    /// <summary>
    /// 提交登录地址
    /// </summary>
    public string SubmitUrl { get; set; }
}