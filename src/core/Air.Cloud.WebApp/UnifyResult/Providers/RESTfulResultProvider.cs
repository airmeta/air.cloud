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
using Air.Cloud.Core.App;
using Air.Cloud.WebApp.DataValidation.Internal;
using Air.Cloud.WebApp.FriendlyException.Internal;
using Air.Cloud.WebApp.UnifyResult;
using Air.Cloud.WebApp.UnifyResult.Internal;
using Air.Cloud.WebApp.UnifyResult.Options;
using Air.Cloud.WebApp.UnifyResult.Attributes;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.WebUtilities;

namespace Air.Cloud.WebApp.UnifyResult.Providers;

/// <summary>
/// RESTful 椋庢牸杩斿洖鍊?
/// </summary>
[IgnoreScanning, UnifyModel(typeof(RESTfulResult<>))]
public class RESTfulResultProvider : IUnifyResultProvider
{
    /// <summary>
    /// 寮傚父杩斿洖鍊?
    /// </summary>
    /// <param name="context"></param>
    /// <param name="metadata"></param>
    /// <returns></returns>
    public IActionResult OnException(ExceptionContext context, ExceptionMetadata metadata)
    {
        return new JsonResult(RESTfulResult(metadata.StatusCode, errors: metadata.Errors));
    }

    /// <summary>
    /// 鎴愬姛杩斿洖鍊?
    /// </summary>
    /// <param name="context"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    public IActionResult OnSucceeded(ActionExecutedContext context, object data)
    {
        return new JsonResult(RESTfulResult(StatusCodes.Status200OK, true, data));
    }

    /// <summary>
    /// 楠岃瘉澶辫触/涓氬姟寮傚父杩斿洖鍊?
    /// </summary>
    /// <param name="context"></param>
    /// <param name="metadata"></param>
    /// <returns></returns>
    public IActionResult OnValidateFailed(ActionExecutingContext context, ValidationMetadata metadata)
    {
        // 楠岃瘉澶辫触鏃讹紝缁熶竴杩斿洖鎽樿娑堟伅鍜岀ǔ瀹氱殑楠岃瘉澶辫触妯″瀷锛屽墠绔笉鍐嶉渶瑕佸垽鏂?Errors 鏄瓧绗︿覆杩樻槸瀛楀吀銆?        // When validation fails, return a summary message and a stable validation failure model so the frontend no longer needs to detect whether Errors is a string or a dictionary.
        var statusCode = metadata.StatusCode ?? StatusCodes.Status400BadRequest;
        return new JsonResult(RESTfulResult(statusCode, errors: metadata.ValidationResult, message: metadata.Message))
        {
            StatusCode = statusCode
        };
    }

    /// <summary>
    /// 鐗瑰畾鐘舵€佺爜杩斿洖鍊?
    /// </summary>
    /// <param name="context"></param>
    /// <param name="statusCode"></param>
    /// <param name="unifyResultSettings"></param>
    /// <returns></returns>
    public async Task OnResponseStatusCodes(HttpContext context, int statusCode, UnifyResultSettingsOptions unifyResultSettings)
    {
        // 璁剧疆鍝嶅簲鐘舵€佺爜
        UnifyContext.SetResponseStatusCodes(context, statusCode, unifyResultSettings);

        switch (statusCode)
        {
            // 澶勭悊 401 鐘舵€佺爜
            case StatusCodes.Status401Unauthorized:
                await context.Response.WriteAsJsonAsync(RESTfulResult(statusCode, errors: "401 Unauthorized")
                    , AppCore.GetOptions<JsonOptions>()?.JsonSerializerOptions);
                break;
            // 澶勭悊 403 鐘舵€佺爜
            case StatusCodes.Status403Forbidden:
                await context.Response.WriteAsJsonAsync(RESTfulResult(statusCode, errors: "403 Forbidden")
                    , AppCore.GetOptions<JsonOptions>()?.JsonSerializerOptions);
                break;

            default:
                await context.Response.WriteAsJsonAsync(RESTfulResult(statusCode, errors: ReasonPhrases.GetReasonPhrase(statusCode))
                    , AppCore.GetOptions<JsonOptions>()?.JsonSerializerOptions);
                break;
        }
    }

    /// <summary>
    /// 杩斿洖 RESTful 椋庢牸缁撴灉闆?
    /// </summary>
    /// <param name="statusCode"></param>
    /// <param name="succeeded"></param>
    /// <param name="data"></param>
    /// <param name="errors"></param>
    /// <returns></returns>
    private static RESTfulResult<object> RESTfulResult(int statusCode, bool succeeded = default, object data = default, object errors = default, string message = default)
    {
        return new RESTfulResult<object>
        {
            Code = statusCode,
            Succeeded = succeeded,
            Data = data,
            Errors = errors,
            Extras = UnifyContext.Take(),
            Message = message,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
    }
}
