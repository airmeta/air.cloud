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
using Air.Cloud.WebApp.DataValidation.Attributes;
using Air.Cloud.WebApp.DataValidation.Internal;
using Air.Cloud.WebApp.FriendlyException.Exceptions;
using Air.Cloud.WebApp.UnifyResult;
using Air.Cloud.WebApp.FriendlyException.Results;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

using System.Reflection;
using Air.Cloud.Core;
using Air.Cloud.Core.Standard.Print;
using Air.Cloud.Core.Modules.AppPrint;
using Air.Cloud.Core.Modules.DynamicApp.Internal;


namespace Air.Cloud.WebApp.DataValidation.Filters;

/// <summary>
/// 数据验证控制器
/// </summary>
[IgnoreScanning]
public sealed class DataValidationFilter : IAsyncActionFilter, IOrderedFilter
{
    /// <summary>
    /// Api 行为配置选项
    /// </summary>
    private readonly ApiBehaviorOptions _apiBehaviorOptions;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="options"></param>
    public DataValidationFilter(IOptions<ApiBehaviorOptions> options)
    {
        _apiBehaviorOptions = options.Value;
    }

    /// <summary>
    /// 过滤器排序
    /// </summary>
    internal const int FilterOrder = -1000;

    /// <summary>
    /// 排序属性
    /// </summary>
    public int Order => FilterOrder;

    /// <summary>
    /// 是否是可重复使用的
    /// </summary>
    public static bool IsReusable => true;

    /// <summary>
    /// 拦截请求
    /// </summary>
    /// <param name="context">动作方法上下文</param>
    /// <param name="next">中间件委托</param>
    /// <returns></returns>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // 获取控制器/方法信息
        var actionDescriptor = context.ActionDescriptor as ControllerActionDescriptor;
        if (actionDescriptor?.MethodInfo == null)
        {
            await next();
            return;
        }

        // 跳过验证类型
        var nonValidationAttributeType = typeof(NonValidationAttribute);
        var method = actionDescriptor.MethodInfo;

        // 获取验证状态
        var modelState = context.ModelState;

        // 如果参数为 0或贴了 [NonValidation] 特性 或所在类型贴了 [NonValidation] 特性或验证成功或已经设置了结果，则跳过验证
        if (actionDescriptor.Parameters.Count == 0 ||
            method.IsDefined(nonValidationAttributeType, true) ||
            method.DeclaringType.IsDefined(nonValidationAttributeType, true) ||
            modelState.IsValid ||
            context.Result != null)
        {
            // 处理执行后验证信息
            var resultContext = await next();

            // 如果异常不为空且属于友好验证异常
            if (resultContext.Exception != null && resultContext.Exception is AppFriendlyException friendlyException && friendlyException.ValidationException)
            {
                // 存储执行结果
                context.HttpContext.Items[nameof(DataValidationFilter)] = resultContext;

                // 处理验证信息
                HandleValidation(context, method, actionDescriptor, friendlyException.ErrorMessage, resultContext, friendlyException);
            }
            return;
        }

        // 处理执行前验证信息
        HandleValidation(context, method, actionDescriptor, modelState);
    }

    /// <summary>
    /// 内部处理异常
    /// </summary>
    /// <param name="context"></param>
    /// <param name="method"></param>
    /// <param name="actionDescriptor"></param>
    /// <param name="errors"></param>
    /// <param name="resultContext"></param>
    /// <param name="friendlyException"></param>
    private void HandleValidation(ActionExecutingContext context, MethodInfo method, ControllerActionDescriptor actionDescriptor, object errors, ActionExecutedContext resultContext = default, AppFriendlyException friendlyException = default)
    {
        var validationMetadata = CreateValidationMetadata(errors, friendlyException);
        var result = CreateValidationResult(context, method, actionDescriptor, validationMetadata);
        if (result == null) return;

        if (resultContext != null) resultContext.Result = result;
        else context.Result = result;

        PrintValidation(validationMetadata);
    }

    /// <summary>
    /// 创建验证失败元数据，并补齐友好异常中的错误码和状态码。
    /// </summary>
    /// <param name="errors">验证错误。</param>
    /// <param name="friendlyException">友好异常。</param>
    /// <returns>验证元数据。</returns>
    private static ValidationMetadata CreateValidationMetadata(object errors, AppFriendlyException friendlyException)
    {
        var validationMetadata = ValidatorContext.GetValidationMetadata(errors);
        validationMetadata.ErrorCode = friendlyException?.ErrorCode;
        validationMetadata.StatusCode = friendlyException?.StatusCode;

        return validationMetadata;
    }

    /// <summary>
    /// 根据统一返回配置创建最终 MVC 结果。
    /// </summary>
    /// <param name="context">动作执行上下文。</param>
    /// <param name="method">动作方法。</param>
    /// <param name="actionDescriptor">动作描述。</param>
    /// <param name="validationMetadata">验证元数据。</param>
    /// <returns>验证失败结果；返回 null 表示当前场景不处理。</returns>
    private IActionResult CreateValidationResult(
        ActionExecutingContext context,
        MethodInfo method,
        ControllerActionDescriptor actionDescriptor,
        ValidationMetadata validationMetadata)
    {
        if (UnifyContext.CheckFailedNonUnify(actionDescriptor.MethodInfo, context.HttpContext.RequestServices, out var unifyResult))
        {
            return CreateNonUnifiedValidationResult(context, method, validationMetadata);
        }

        if (!UnifyContext.CheckSupportMvcController(context.HttpContext, actionDescriptor, out _))
        {
            return null;
        }

        return unifyResult.OnValidateFailed(context, validationMetadata);
    }

    /// <summary>
    /// 创建非统一返回场景下的验证失败结果。
    /// </summary>
    /// <param name="context">动作执行上下文。</param>
    /// <param name="method">动作方法。</param>
    /// <param name="validationMetadata">验证元数据。</param>
    /// <returns>验证失败结果。</returns>
    private IActionResult CreateNonUnifiedValidationResult(
        ActionExecutingContext context,
        MethodInfo method,
        ValidationMetadata validationMetadata)
    {
        if (!Penetrates.IsApiController(method.DeclaringType))
        {
            return new BadPageResult(StatusCodes.Status400BadRequest)
            {
                Code = validationMetadata.DetailMessage
            };
        }

        if (!_apiBehaviorOptions.SuppressModelStateInvalidFilter)
        {
            return _apiBehaviorOptions.InvalidModelStateResponseFactory(context);
        }

        return new JsonResult(validationMetadata.ValidationResult)
        {
            StatusCode = StatusCodes.Status400BadRequest
        };
    }

    /// <summary>
    /// 输出验证失败明细。
    /// </summary>
    /// <param name="validationMetadata">验证元数据。</param>
    private static void PrintValidation(ValidationMetadata validationMetadata)
    {
        AppRealization.Output.Print(new AppPrintInformation
        {
            Title = "validation",
            Level=AppPrintLevel.Error,
            Content = $"Validation Failed:\r\n{validationMetadata.DetailMessage}",
            State = true
        });
    }
}
