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

using Air.Cloud.WebApp.FriendlyException.Attributes;
using Air.Cloud.WebApp.FriendlyException.Exceptions;
using Air.Cloud.WebApp.FriendlyException.Internal;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

using System.Reflection;

namespace Air.Cloud.WebApp.UnifyResult.Internal;

/// <summary>
/// 统一返回异常元数据解析器。
/// </summary>
[IgnoreScanning]
internal static class UnifyExceptionMetadataResolver
{
    /// <summary>
    /// 获取统一返回需要的异常元数据。
    /// </summary>
    /// <param name="context">动作上下文。</param>
    /// <returns>异常元数据。</returns>
    public static ExceptionMetadata GetExceptionMetadata(ActionContext context)
    {
        var exception = ResolveException(context);
        if (exception is AppFriendlyException friendlyException)
            return CreateFriendlyExceptionMetadata(context, friendlyException);

        return new ExceptionMetadata
        {
            StatusCode = StatusCodes.Status500InternalServerError,
            ErrorCode = default,
            Errors = ResolveExceptionErrors(context, exception, false)
        };
    }

    private static Exception ResolveException(ActionContext context)
    {
        return context switch
        {
            ExceptionContext exceptionContext => exceptionContext.Exception,
            ActionExecutedContext actionExecutedContext => actionExecutedContext.Exception,
            _ => default
        };
    }

    private static ExceptionMetadata CreateFriendlyExceptionMetadata(ActionContext context, AppFriendlyException exception)
    {
        return new ExceptionMetadata
        {
            StatusCode = exception.StatusCode,
            ErrorCode = exception.ErrorCode,
            Errors = exception.ValidationException
                ? exception.ErrorMessage
                : ResolveExceptionErrors(context, exception, true)
        };
    }

    private static object ResolveExceptionErrors(ActionContext context, Exception exception, bool isFriendlyException)
    {
        var ifExceptionMessage = ResolveIfExceptionMessage(context, exception, isFriendlyException);
        if (!string.IsNullOrWhiteSpace(ifExceptionMessage)) return ifExceptionMessage;

        return exception?.InnerException?.Message ?? exception?.Message;
    }

    private static string ResolveIfExceptionMessage(ActionContext context, Exception exception, bool isFriendlyException)
    {
        var ifExceptionAttributes = GetIfExceptionAttributes(context).ToArray();
        if (ifExceptionAttributes.Length == 0) return default;

        var exceptionType = ResolveComparableExceptionType(exception, isFriendlyException);
        var matchedAttribute = ifExceptionAttributes.FirstOrDefault(attribute => attribute.ExceptionType == exceptionType)
                               ?? ifExceptionAttributes.FirstOrDefault(attribute => attribute.ExceptionType == null);

        return matchedAttribute?.ErrorMessage;
    }

    private static IEnumerable<IfExceptionAttribute> GetIfExceptionAttributes(ActionContext context)
    {
        if (context.ActionDescriptor is not ControllerActionDescriptor actionDescriptor)
            return Enumerable.Empty<IfExceptionAttribute>();

        return actionDescriptor.MethodInfo
                               .GetCustomAttributes<IfExceptionAttribute>(true)
                               .Where(attribute => attribute.ErrorCode == null);
    }

    private static Type ResolveComparableExceptionType(Exception exception, bool isFriendlyException)
    {
        return isFriendlyException && exception?.InnerException != null
            ? exception.InnerException.GetType()
            : exception?.GetType();
    }
}
