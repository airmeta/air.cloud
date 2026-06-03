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
using Air.Cloud.WebApp.FriendlyException.Internal;
using Air.Cloud.WebApp.UnifyResult.Internal;
using Air.Cloud.WebApp.UnifyResult.Options;
using Air.Cloud.WebApp.UnifyResult.Providers;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using System.Reflection;

namespace Air.Cloud.WebApp.UnifyResult;

/// <summary>
/// 规范化结果上下文兼容门面。
/// </summary>
[IgnoreScanning]
public static class UnifyContext
{
    /// <summary>
    /// 获取统一返回需要的异常元数据。
    /// </summary>
    /// <param name="context">动作上下文。</param>
    /// <returns>异常元数据。</returns>
    public static ExceptionMetadata GetExceptionMetadata(ActionContext context)
    {
        return UnifyExceptionMetadataResolver.GetExceptionMetadata(context);
    }

    /// <summary>
    /// 填充附加信息。
    /// </summary>
    /// <param name="extras">附加信息。</param>
    public static void Fill(object extras)
    {
        UnifyResultExtrasAccessor.Fill(extras);
    }

    /// <summary>
    /// 读取附加信息。
    /// </summary>
    /// <returns>附加信息。</returns>
    public static object Take()
    {
        return UnifyResultExtrasAccessor.Take();
    }

    /// <summary>
    /// 设置响应状态码。
    /// </summary>
    /// <param name="context">HTTP 上下文。</param>
    /// <param name="statusCode">原始状态码。</param>
    /// <param name="unifyResultSettings">统一返回配置。</param>
    public static void SetResponseStatusCodes(HttpContext context, int statusCode, UnifyResultSettingsOptions unifyResultSettings)
    {
        UnifyResultStatusCodeResolver.SetResponseStatusCodes(context, statusCode, unifyResultSettings);
    }

    /// <summary>
    /// 检查请求成功是否进行规范化处理。
    /// </summary>
    /// <param name="method">动作方法。</param>
    /// <param name="unifyResult">统一返回提供器。</param>
    /// <param name="isWebRequest">是否 Web 请求。</param>
    /// <returns>返回 true 跳过处理，否则进行规范化处理。</returns>
    internal static bool CheckSucceededNonUnify(MethodInfo method, out IUnifyResultProvider unifyResult, bool isWebRequest = true)
    {
        return CheckSucceededNonUnify(method, AppCore.RootServices, out unifyResult, isWebRequest);
    }

    /// <summary>
    /// 检查请求成功是否进行规范化处理。
    /// </summary>
    /// <param name="method">动作方法。</param>
    /// <param name="requestServices">当前请求服务提供器。</param>
    /// <param name="unifyResult">统一返回提供器。</param>
    /// <param name="isWebRequest">是否 Web 请求。</param>
    /// <returns>返回 true 跳过处理，否则进行规范化处理。</returns>
    internal static bool CheckSucceededNonUnify(
        MethodInfo method,
        IServiceProvider requestServices,
        out IUnifyResultProvider unifyResult,
        bool isWebRequest = true)
    {
        return UnifyResultPolicy.CheckSucceededNonUnify(
            method,
            requestServices,
            GetRuntimeOptions(requestServices),
            out unifyResult,
            isWebRequest);
    }

    /// <summary>
    /// 检查请求失败（验证失败、抛异常）是否进行规范化处理。
    /// </summary>
    /// <param name="method">动作方法。</param>
    /// <param name="unifyResult">统一返回提供器。</param>
    /// <returns>返回 true 跳过处理，否则进行规范化处理。</returns>
    internal static bool CheckFailedNonUnify(MethodInfo method, out IUnifyResultProvider unifyResult)
    {
        return CheckFailedNonUnify(method, AppCore.RootServices, out unifyResult);
    }

    /// <summary>
    /// 检查请求失败（验证失败、抛异常）是否进行规范化处理。
    /// </summary>
    /// <param name="method">动作方法。</param>
    /// <param name="requestServices">当前请求服务提供器。</param>
    /// <param name="unifyResult">统一返回提供器。</param>
    /// <returns>返回 true 跳过处理，否则进行规范化处理。</returns>
    internal static bool CheckFailedNonUnify(MethodInfo method, IServiceProvider requestServices, out IUnifyResultProvider unifyResult)
    {
        return UnifyResultPolicy.CheckFailedNonUnify(method, requestServices, GetRuntimeOptions(requestServices), out unifyResult);
    }

    /// <summary>
    /// 检查短路状态码（>=400）是否进行规范化处理。
    /// </summary>
    /// <param name="context">HTTP 上下文。</param>
    /// <param name="unifyResult">统一返回提供器。</param>
    /// <returns>返回 true 跳过处理，否则进行规范化处理。</returns>
    internal static bool CheckStatusCodeNonUnify(HttpContext context, out IUnifyResultProvider unifyResult)
    {
        return UnifyResultPolicy.CheckStatusCodeNonUnify(
            context,
            GetRuntimeOptions(context.RequestServices),
            out unifyResult);
    }

    /// <summary>
    /// 判断是否支持 MVC 控制器规范化处理。
    /// </summary>
    /// <param name="httpContext">HTTP 上下文。</param>
    /// <param name="actionDescriptor">动作描述。</param>
    /// <param name="unifyResultSettings">统一返回配置。</param>
    /// <returns>支持返回 true。</returns>
    internal static bool CheckSupportMvcController(
        HttpContext httpContext,
        ControllerActionDescriptor actionDescriptor,
        out UnifyResultSettingsOptions unifyResultSettings)
    {
        return UnifyResultPolicy.CheckSupportMvcController(httpContext, actionDescriptor, out unifyResultSettings);
    }

    /// <summary>
    /// 检查是否是有效的结果（可进行规范化的结果）。
    /// </summary>
    /// <param name="result">动作结果。</param>
    /// <param name="data">结果数据。</param>
    /// <returns>可规范化返回 true。</returns>
    internal static bool CheckValidResult(IActionResult result, out object data)
    {
        return UnifyResultDataResolver.CheckValidResult(result, out data);
    }

    private static UnifyResultRuntimeOptions GetRuntimeOptions(IServiceProvider services)
    {
        return services?.GetService<IOptions<UnifyResultRuntimeOptions>>()?.Value
               ?? new UnifyResultRuntimeOptions();
    }
}
