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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Air.Cloud.WebApp.UnifyResult.Options;
using Air.Cloud.WebApp.FriendlyException.Internal;
using Air.Cloud.WebApp.DataValidation.Internal;

namespace Air.Cloud.WebApp.UnifyResult.Providers;

/// <summary>
/// 瑙勮寖鍖栫粨鏋滄彁渚涘櫒
/// </summary>
public interface IUnifyResultProvider
{
    /// <summary>
    /// 寮傚父杩斿洖鍊?
    /// </summary>
    /// <param name="context"></param>
    /// <param name="metadata"></param>
    /// <returns></returns>
    IActionResult OnException(ExceptionContext context, ExceptionMetadata metadata);

    /// <summary>
    /// 鎴愬姛杩斿洖鍊?
    /// </summary>
    /// <param name="context"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    IActionResult OnSucceeded(ActionExecutedContext context, object data);

    /// <summary>
    /// 楠岃瘉澶辫触杩斿洖鍊?
    /// </summary>
    /// <param name="context"></param>
    /// <param name="metadata"></param>
    /// <returns></returns>
    IActionResult OnValidateFailed(ActionExecutingContext context, ValidationMetadata metadata);

    /// <summary>
    /// 鎷︽埅杩斿洖鐘舵€佺爜
    /// </summary>
    /// <param name="context"></param>
    /// <param name="statusCode"></param>
    /// <param name="unifyResultSettings"></param>
    /// <returns></returns>
    Task OnResponseStatusCodes(HttpContext context, int statusCode, UnifyResultSettingsOptions unifyResultSettings = default);
}