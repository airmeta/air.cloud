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

using Air.Cloud.WebApp.UnifyResult.Middlewares;

namespace Air.Cloud.WebApp.UnifyResult.Extensions;

/// <summary>
/// 状态码中间件拓展
/// </summary>
[IgnoreScanning]
public static class UnifyResultMiddlewareExtensions
{
    /// <summary>
    /// 添加状态码拦截中间件
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseUnifyResultStatusCodes(this IApplicationBuilder builder)
    {
        // 注册中间件
        builder.UseMiddleware<UnifyResultStatusCodesMiddleware>();

        return builder;
    }
}