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
using Air.Cloud.WebApp.UnifyResult.Options;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Air.Cloud.WebApp.UnifyResult.Middlewares;

/// <summary>
/// 状态码中间件
/// </summary>
[IgnoreScanning]
public class UnifyResultStatusCodesMiddleware
{
    /// <summary>
    /// 请求委托
    /// </summary>
    private readonly RequestDelegate _next;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="next"></param>
    public UnifyResultStatusCodesMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// 中间件执行方法
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        // 只有请求错误（短路状态码）才支持规范化处理。
        if (context.Response.StatusCode < 400 || context.Response.HasStarted) return;

        // 处理规范化结果。
        if (!UnifyContext.CheckStatusCodeNonUnify(context, out var unifyResult))
        {
            await unifyResult.OnResponseStatusCodes(context, context.Response.StatusCode, context.RequestServices.GetService<IOptions<UnifyResultSettingsOptions>>()?.Value);
        }
    }
}
