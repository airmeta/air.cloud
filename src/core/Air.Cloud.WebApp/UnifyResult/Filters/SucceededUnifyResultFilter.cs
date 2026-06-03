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
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Air.Cloud.WebApp.DataValidation.Internal;
using Air.Cloud.Core;
using Air.Cloud.Core.Modules.AppPrint;

namespace Air.Cloud.WebApp.UnifyResult.Filters;

/// <summary>
/// 规范化结构（请求成功）过滤器
/// </summary>
[IgnoreScanning]
public class SucceededUnifyResultFilter : IAsyncActionFilter, IOrderedFilter
{
    /// <summary>
    /// 过滤器排序
    /// </summary>
    internal const int FilterOrder = 8888;

    /// <summary>
    /// 排序属性
    /// </summary>
    public int Order => FilterOrder;

    /// <summary>
    /// 处理规范化结果
    /// </summary>
    /// <param name="context"></param>
    /// <param name="next"></param>
    /// <returns></returns>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // 执行 Action 并获取结果
        var actionExecutedContext = await next();

        // 如果出现异常，则不会进入该过滤器
        if (actionExecutedContext.Exception != null) return;

        // 获取控制器信息
        var actionDescriptor = context.ActionDescriptor as ControllerActionDescriptor;
        if (actionDescriptor?.MethodInfo == null) return;

        // 判断是否支持 MVC 规范化处理
        if (!UnifyContext.CheckSupportMvcController(context.HttpContext, actionDescriptor, out _)) return;

        // 判断是否跳过规范化处理
        if (UnifyContext.CheckSucceededNonUnify(actionDescriptor.MethodInfo, context.HttpContext.RequestServices, out var unifyResult)) return;

        // 处理 BadRequestObjectResult 类型规范化处理
        if (actionExecutedContext.Result is BadRequestObjectResult badRequestObjectResult)
        {
            // 解析验证消息
            var validationMetadata = ValidatorContext.GetValidationMetadata(badRequestObjectResult.Value);

            var result = unifyResult.OnValidateFailed(context, validationMetadata);
            if (result != null) actionExecutedContext.Result = result;
            // 打印完整的堆栈信息
            AppRealization.Output.Print(new AppPrintInformation
            {
                Title = "validation",
                Level = AppPrintLevel.Error,
                Content = $"Validation Failed:\r\n{validationMetadata.DetailMessage}",
                State = true
            });
        }
        else
        {
            IActionResult result = default;

            // 检查是否是有效的结果（可进行规范化的结果）
            if (UnifyContext.CheckValidResult(actionExecutedContext.Result, out var data))
            {
                result = unifyResult.OnSucceeded(actionExecutedContext, data);
            }

            // 如果是不能规范化的结果类型，则跳过
            if (result == null) return;

            actionExecutedContext.Result = result;
        }
    }
}
