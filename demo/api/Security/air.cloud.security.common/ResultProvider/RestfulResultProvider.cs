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
using Air.Cloud.Core;
using Air.Cloud.Core.App;
using Air.Cloud.Core.Attributes;
using Air.Cloud.WebApp.DataValidation.Internal;
using Air.Cloud.WebApp.FriendlyException.Exceptions;
using Air.Cloud.WebApp.FriendlyException.Internal;
using Air.Cloud.WebApp.UnifyResult;
using Air.Cloud.WebApp.UnifyResult.Attributes;
using Air.Cloud.WebApp.UnifyResult.Internal;
using Air.Cloud.WebApp.UnifyResult.Options;
using Air.Cloud.WebApp.UnifyResult.Providers;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace air.cloud.security.common.ResultProvider
{
    [IgnoreScanning, UnifyModel(typeof(RESTfulResult<>))]
    public class RestfulResultProvider : IUnifyResultProvider
    {
        /// <summary>
        /// 异常返回值
        /// </summary>
        /// <param name="context"></param>
        /// <param name="metadata"></param>
        /// <returns></returns>
        public IActionResult OnException(ExceptionContext context, ExceptionMetadata metadata)
        {
            // 解析异常信息
            var data = UnifyContext.GetExceptionMetadata(context);
            AppRealization.Output.Print("出现异常", context.Exception.Message, AdditionalParams: new Dictionary<string, object>()
            {
                { "ExceptionType", context.Exception.GetType().FullName },
                { "StackTrace", context.Exception.StackTrace }
            });
            string errorMsg = context.Exception.InnerException != null && !string.IsNullOrEmpty(context.Exception.InnerException.ToString())
                   ? $"[{context.Exception.InnerException?.Message}]{context.Exception.Message}"
                   : context.Exception.Message;
            //标记已处理
            context.ExceptionHandled = true;
            return new JsonResult(new RESTfulResult<object>
            {
                Code = data.StatusCode,
                Succeeded = false,
                Data = null,
                Errors=data.Errors,
                Message = context.Exception is AppFriendlyException ? data.Errors.ToString() : errorMsg,//Errors
                Extras = UnifyContext.Take(),
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });
        }

        /// <summary>
        /// 成功返回值
        /// </summary>
        /// <param name="context"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public IActionResult OnSucceeded(ActionExecutedContext context, object data)
        {
            // 处理内容结果
            if (context.Result is ContentResult contentResult) data = contentResult.Content;
            // 处理对象结果
            else if (context.Result is ObjectResult objectResult) data = objectResult.Value;
            else if (context.Result is EmptyResult) data = null;
            else return null;
            var result = new JsonResult(new RESTfulResult<object>
            {
                Code = context.Result is EmptyResult ? StatusCodes.Status204NoContent : StatusCodes.Status200OK,  // 处理没有返回值情况 204
                Succeeded = true,
                Data = data,
                Message = "操作成功",
                Extras = UnifyContext.Take(),
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });
            return result;
        }

        /// <summary>
        /// 验证失败返回值
        /// </summary>
        /// <param name="context"></param>
        /// <param name="metadata"></param>
        /// <returns></returns>
        public IActionResult OnValidateFailed(ActionExecutingContext context, ValidationMetadata metadata)
        {
            return new JsonResult(new RESTfulResult<object>
            {
                Code = StatusCodes.Status400BadRequest,
                Succeeded = false,
                Data = null,
                Message = metadata.Message,
                Extras = UnifyContext.Take(),
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });
        }

        /// <summary>
        /// 拦截返回状态码
        /// </summary>
        /// <param name="context"></param>
        /// <param name="statusCode"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public async Task OnResponseStatusCodes(HttpContext context, int statusCode, UnifyResultSettingsOptions options = null)
        {
            // 设置响应状态码
            UnifyContext.SetResponseStatusCodes(context, statusCode, options);

            switch (statusCode)
            {
                // 处理 401 状态码
                case StatusCodes.Status401Unauthorized:
                    await context.Response.WriteAsJsonAsync(new RESTfulResult<object>
                    {
                        Code = StatusCodes.Status401Unauthorized,
                        Succeeded = false,
                        Data = null,
                        Errors = "401 Unauthorized",
                        Extras = UnifyContext.Take(),
                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    }, AppCore.GetOptions<JsonOptions>()?.JsonSerializerOptions);
                    break;
                // 处理 403 状态码
                case StatusCodes.Status403Forbidden:
                    await context.Response.WriteAsJsonAsync(new RESTfulResult<object>
                    {
                        Code = StatusCodes.Status403Forbidden,
                        Succeeded = false,
                        Data = null,
                        Errors = "403 Forbidden",
                        Extras = UnifyContext.Take(),
                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    }, AppCore.GetOptions<JsonOptions>()?.JsonSerializerOptions);
                    break;
                default:
                    break;
            }
        }
    }
}
