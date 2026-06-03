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

namespace Air.Cloud.WebApp.DynamicApiController.Enums;

/// <summary>
/// 接口参数位置
/// </summary>
[IgnoreScanning]
public enum ApiSeats
{
    /// <summary>
    /// 控制器之前
    /// </summary>
    [Description("控制器之前")]
    ControllerStart,

    /// <summary>
    /// 控制器之后
    /// </summary>
    [Description("控制器之后")]
    ControllerEnd,

    /// <summary>
    /// 行为之前
    /// </summary>
    [Description("行为之前")]
    ActionStart,

    /// <summary>
    /// 行为之后
    /// </summary>
    [Description("行为之后")]
    ActionEnd
}