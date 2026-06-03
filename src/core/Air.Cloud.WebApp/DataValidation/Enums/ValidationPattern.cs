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
using System.ComponentModel;

namespace Air.Cloud.WebApp.DataValidation.Enums;

/// <summary>
/// 验证逻辑
/// </summary>
[IgnoreScanning]
public enum ValidationPattern
{
    /// <summary>
    /// 全部都要验证通过
    /// </summary>
    [Description("全部验证通过才为真")]
    AllOfThem,

    /// <summary>
    /// 至少一个验证通过
    /// </summary>
    [Description("有一个通过就为真")]
    AtLeastOne
}