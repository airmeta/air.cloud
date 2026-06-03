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
using Air.Cloud.WebApp.DynamicApiController.Enums;

namespace Air.Cloud.WebApp.DynamicApiController.Attributes;

/// <summary>
/// 接口参数位置设置
/// </summary>
[IgnoreScanning, AttributeUsage(AttributeTargets.Parameter)]
public class ApiSeatAttribute : Attribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="seat"></param>
    public ApiSeatAttribute(ApiSeats seat = ApiSeats.ActionEnd)
    {
        Seat = seat;
    }

    /// <summary>
    /// 参数位置
    /// </summary>
    public ApiSeats Seat { get; set; }
}