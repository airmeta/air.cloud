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


using Air.Cloud.WebApp.FriendlyException.Exceptions;

using Microsoft.AspNetCore.Http;

namespace Air.Cloud.WebApp.FriendlyException.Extensions;

/// <summary>
/// 异常拓展
/// </summary>
[IgnoreScanning]
public static class AppFriendlyExceptionExtensions
{
    /// <summary>
    /// 设置异常状态码
    /// </summary>
    /// <param name="exception"></param>
    /// <param name="statusCode"></param>
    /// <returns></returns>
    public static AppFriendlyException StatusCode(this AppFriendlyException exception, int statusCode = StatusCodes.Status500InternalServerError)
    {
        exception.StatusCode = statusCode;
        return exception;
    }
}
