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
using Air.Cloud.Core.Modules.AppPrint;
using Air.Cloud.Core.Modules.DynamicApp.Internal;
using Air.Cloud.WebApp.DataValidation.Filters;
using Air.Cloud.WebApp.DataValidation.Internal;
using Air.Cloud.WebApp.FriendlyException.Exceptions;
using Air.Cloud.WebApp.FriendlyException.Handlers;
using Air.Cloud.WebApp.FriendlyException.Internal;
using Air.Cloud.WebApp.FriendlyException.Results;
using Air.Cloud.WebApp.UnifyResult;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

using System.Diagnostics;

namespace Air.Cloud.WebApp.FriendlyException.Filters;

/// <summary>
/// 友好异常过滤器，负责把 MVC 异常转换为普通错误页或统一返回结果。
/// </summary>
[IgnoreScanning]
public sealed class FriendlyExceptionFilter : IAsyncExceptionFilter
{
    /// <summary>
    /// 处理 MVC 管道中的异常。
    /// </summary>
    /// <param name="context">异常上下文。</param>
    /// <returns>异步任务。</returns>
    public async Task OnExceptionAsync(ExceptionContext context)
    {
        var isValidationException = IsValidationException(context.Exception);

        await InvokeGlobalExceptionHandlerAsync(context, isValidationException);
        if (context.ExceptionHandled) return;

        if (TryHandleValidationException(context)) return;

        var actionDescriptor = context.ActionDescriptor as ControllerActionDescriptor;
        var exceptionMetadata = UnifyContext.GetExceptionMetadata(context);

        if (!TryHandleNonUnifiedException(context, actionDescriptor, exceptionMetadata))
        {
            TryHandleUnifiedException(context, actionDescriptor, exceptionMetadata);
        }

        PrintError(context.Exception);
    }

    /// <summary>
    /// 判断异常是否由数据验证流程抛出。
    /// </summary>
    /// <param name="exception">异常实例。</param>
    /// <returns>验证异常返回 true。</returns>
    private static bool IsValidationException(Exception exception)
    {
        return exception is AppFriendlyException { ValidationException: true };
    }

    /// <summary>
    /// 调用可选的全局异常处理器；验证异常不触发该处理器。
    /// </summary>
    /// <param name="context">异常上下文。</param>
    /// <param name="isValidationException">是否验证异常。</param>
    /// <returns>异步任务。</returns>
    private static async Task InvokeGlobalExceptionHandlerAsync(ExceptionContext context, bool isValidationException)
    {
        if (isValidationException) return;

        var globalExceptionHandler = context.HttpContext.RequestServices.GetService<IGlobalExceptionHandler>();
        if (globalExceptionHandler == null) return;

        await globalExceptionHandler.OnExceptionAsync(context);
    }

    /// <summary>
    /// 处理由数据验证过滤器转交的验证异常。
    /// </summary>
    /// <param name="context">异常上下文。</param>
    /// <returns>已处理返回 true。</returns>
    private static bool TryHandleValidationException(ExceptionContext context)
    {
        if (!IsValidationException(context.Exception)) return false;
        if (context.HttpContext.Items[nameof(DataValidationFilter)] is not ActionExecutedContext actionResultContext) return false;

        context.Result = actionResultContext.Result ?? CreateValidationBadPageResult(context.Exception);
        context.ExceptionHandled = true;
        return true;
    }

    /// <summary>
    /// 处理启用统一返回时的异常响应。
    /// </summary>
    /// <param name="context">异常上下文。</param>
    /// <param name="actionDescriptor">控制器动作描述。</param>
    /// <param name="metadata">异常元数据。</param>
    /// <returns>已处理返回 true。</returns>
    private static bool TryHandleUnifiedException(
        ExceptionContext context,
        ControllerActionDescriptor actionDescriptor,
        ExceptionMetadata metadata)
    {
        if (actionDescriptor?.MethodInfo == null) return false;
        if (UnifyContext.CheckFailedNonUnify(actionDescriptor.MethodInfo, context.HttpContext.RequestServices, out var unifyResult)) return false;
        if (!UnifyContext.CheckSupportMvcController(context.HttpContext, actionDescriptor, out _)) return false;

        context.Result = unifyResult.OnException(context, metadata);
        context.ExceptionHandled = true;
        return true;
    }

    /// <summary>
    /// 处理未启用统一返回或被标记为不统一返回的异常响应。
    /// </summary>
    /// <param name="context">异常上下文。</param>
    /// <param name="actionDescriptor">控制器动作描述。</param>
    /// <param name="metadata">异常元数据。</param>
    /// <returns>已处理返回 true。</returns>
    private static bool TryHandleNonUnifiedException(
        ExceptionContext context,
        ControllerActionDescriptor actionDescriptor,
        ExceptionMetadata metadata)
    {
        if (actionDescriptor?.MethodInfo == null) return false;
        if (!UnifyContext.CheckFailedNonUnify(actionDescriptor.MethodInfo, context.HttpContext.RequestServices, out _)) return false;

        context.Result = Penetrates.IsApiController(actionDescriptor.MethodInfo.DeclaringType)
            ? CreateJsonErrorResult(metadata)
            : CreateBadPageResult(metadata);
        context.ExceptionHandled = true;
        return true;
    }

    /// <summary>
    /// 创建验证异常的错误页结果。
    /// </summary>
    /// <param name="exception">异常实例。</param>
    /// <returns>错误页结果。</returns>
    private static BadPageResult CreateValidationBadPageResult(Exception exception)
    {
        var errorMessage = exception is AppFriendlyException friendlyException
            ? friendlyException.ErrorMessage
            : exception.Message;

        return new BadPageResult(StatusCodes.Status400BadRequest)
        {
            Code = ValidatorContext.GetValidationMetadata(errorMessage).DetailMessage
        };
    }

    /// <summary>
    /// 创建 API JSON 异常结果。
    /// </summary>
    /// <param name="metadata">异常元数据。</param>
    /// <returns>JSON 结果。</returns>
    private static JsonResult CreateJsonErrorResult(ExceptionMetadata metadata)
    {
        return new JsonResult(metadata.Errors)
        {
            StatusCode = metadata.StatusCode
        };
    }

    /// <summary>
    /// 创建非 API 场景的错误页结果。
    /// </summary>
    /// <param name="metadata">异常元数据。</param>
    /// <returns>错误页结果。</returns>
    private static BadPageResult CreateBadPageResult(ExceptionMetadata metadata)
    {
        return new BadPageResult(metadata.StatusCode)
        {
            Title = "Internal Server",
            Code = metadata.Errors?.ToString()
        };
    }

    /// <summary>
    /// 打印异常文件位置和完整堆栈。
    /// </summary>
    /// <param name="exception">异常实例。</param>
    internal static void PrintError(Exception exception)
    {
        var stackTrace = new StackTrace(exception, true);
        if (stackTrace.FrameCount == 0) return;

        PrintFirstStackFrame(stackTrace);
        PrintExceptionStack(exception);
    }

    /// <summary>
    /// 打印首个堆栈帧对应的源码位置。
    /// </summary>
    /// <param name="stackTrace">异常堆栈。</param>
    private static void PrintFirstStackFrame(StackTrace stackTrace)
    {
        var traceFrame = stackTrace.GetFrame(0);
        var exceptionFileName = traceFrame?.GetFileName();
        var exceptionFileLineNumber = traceFrame?.GetFileLineNumber() ?? 0;
        if (string.IsNullOrWhiteSpace(exceptionFileName) || exceptionFileLineNumber <= 0) return;

        AppRealization.Output.Print(new AppPrintInformation
        {
            Title = "errors",
            Level = AppPrintLevel.Error,
            Content = $"{exceptionFileName}:line {exceptionFileLineNumber}",
            State = true
        });
    }

    /// <summary>
    /// 打印完整异常堆栈。
    /// </summary>
    /// <param name="exception">异常实例。</param>
    private static void PrintExceptionStack(Exception exception)
    {
        AppRealization.Output.Print(new AppPrintInformation
        {
            Title = "errors",
            Level = AppPrintLevel.Error,
            Content = exception.ToString(),
            State = true
        });
    }
}
