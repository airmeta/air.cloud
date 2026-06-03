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
using Air.Cloud.Core.Extensions;
using Air.Cloud.Core.Standard.DynamicServer;
using Air.Cloud.WebApp.FriendlyException.Attributes;
using Air.Cloud.WebApp.FriendlyException.Exceptions;
using Air.Cloud.WebApp.FriendlyException.Extensions;
using Air.Cloud.WebApp.FriendlyException.Internal;
using Air.Cloud.WebApp.FriendlyException.Options;
using Air.Cloud.WebApp.FriendlyException.Providers;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;

using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Reflection;

namespace Air.Cloud.WebApp.FriendlyException;

/// <summary>
/// 友好异常工厂，负责创建业务异常和错误码异常。
/// </summary>
[IgnoreScanning]
public static class Oops
{
    /// <summary>
    /// 方法级异常元数据缓存。
    /// </summary>
    private static readonly ConcurrentDictionary<MethodBase, MethodIfException> ErrorMethods;

    /// <summary>
    /// 错误码定义类型集合。
    /// </summary>
    private static readonly IEnumerable<Type> ErrorCodeTypes;

    /// <summary>
    /// 错误码消息字典。
    /// </summary>
    private static readonly ConcurrentDictionary<string, string> ErrorCodeMessages;

    /// <summary>
    /// 友好异常配置。
    /// </summary>
    private static readonly FriendlyExceptionSettingsOptions FriendlyExceptionSettings;

    /// <summary>
    /// 初始化友好异常配置和错误码缓存。
    /// </summary>
    static Oops()
    {
        ErrorMethods = new ConcurrentDictionary<MethodBase, MethodIfException>();
        FriendlyExceptionSettings = AppConfiguration.GetConfig<FriendlyExceptionSettingsOptions>("FriendlyExceptionSettings", true);
        ErrorCodeTypes = ResolveErrorCodeTypes();
        ErrorCodeMessages = ResolveErrorCodeMessages();
    }

    /// <summary>
    /// 创建业务异常。
    /// </summary>
    /// <param name="errorMessage">异常消息。</param>
    /// <param name="args">格式化参数。</param>
    /// <returns>友好异常实例。</returns>
    public static AppFriendlyException Bah(string errorMessage, params object[] args)
    {
        return MarkAsBusinessException(Oh(errorMessage, typeof(ValidationException), args));
    }

    /// <summary>
    /// 通过错误码创建业务异常。
    /// </summary>
    /// <param name="errorCode">错误码。</param>
    /// <param name="args">格式化参数。</param>
    /// <returns>友好异常实例。</returns>
    public static AppFriendlyException Bah(object errorCode, params object[] args)
    {
        return MarkAsBusinessException(Oh(errorCode, typeof(ValidationException), args));
    }

    /// <summary>
    /// 通过字符串消息创建友好异常。
    /// </summary>
    /// <param name="errorMessage">异常消息。</param>
    /// <param name="args">格式化参数。</param>
    /// <returns>友好异常实例。</returns>
    public static AppFriendlyException Oh(string errorMessage, params object[] args)
    {
        var exception = CreateFriendlyException(MontageErrorMessage(errorMessage, default, args), default);
        return ApplyDefaultBusinessExceptionPolicy(exception);
    }

    /// <summary>
    /// 通过字符串消息和内部异常类型创建友好异常。
    /// </summary>
    /// <param name="errorMessage">异常消息。</param>
    /// <param name="exceptionType">内部异常类型。</param>
    /// <param name="args">格式化参数。</param>
    /// <returns>友好异常实例。</returns>
    public static AppFriendlyException Oh(string errorMessage, Type exceptionType, params object[] args)
    {
        var exceptionMessage = MontageErrorMessage(errorMessage, default, args);
        return CreateFriendlyException(exceptionMessage, default, CreateInnerException(exceptionType, exceptionMessage));
    }

    /// <summary>
    /// 通过字符串消息和内部异常类型创建友好异常。
    /// </summary>
    /// <typeparam name="TException">内部异常类型。</typeparam>
    /// <param name="errorMessage">异常消息。</param>
    /// <param name="args">格式化参数。</param>
    /// <returns>友好异常实例。</returns>
    public static AppFriendlyException Oh<TException>(string errorMessage, params object[] args)
        where TException : Exception
    {
        return Oh(errorMessage, typeof(TException), args);
    }

    /// <summary>
    /// 通过错误码创建友好异常。
    /// </summary>
    /// <param name="errorCode">错误码。</param>
    /// <param name="args">格式化参数。</param>
    /// <returns>友好异常实例。</returns>
    public static AppFriendlyException Oh(object errorCode, params object[] args)
    {
        var exception = CreateFriendlyException(ResolveErrorCodeMessage(errorCode, args), errorCode);
        return ApplyDefaultBusinessExceptionPolicy(exception);
    }

    /// <summary>
    /// 通过错误码和内部异常类型创建友好异常。
    /// </summary>
    /// <param name="errorCode">错误码。</param>
    /// <param name="exceptionType">内部异常类型。</param>
    /// <param name="args">格式化参数。</param>
    /// <returns>友好异常实例。</returns>
    public static AppFriendlyException Oh(object errorCode, Type exceptionType, params object[] args)
    {
        var exceptionMessage = ResolveErrorCodeMessage(errorCode, args);
        return CreateFriendlyException(exceptionMessage, errorCode, CreateInnerException(exceptionType, exceptionMessage));
    }

    /// <summary>
    /// 通过错误码和内部异常类型创建友好异常。
    /// </summary>
    /// <typeparam name="TException">内部异常类型。</typeparam>
    /// <param name="errorCode">错误码。</param>
    /// <param name="args">格式化参数。</param>
    /// <returns>友好异常实例。</returns>
    public static AppFriendlyException Oh<TException>(object errorCode, params object[] args)
        where TException : Exception
    {
        return Oh(errorCode, typeof(TException), args);
    }

    /// <summary>
    /// 创建友好异常实例。
    /// </summary>
    /// <param name="message">异常消息。</param>
    /// <param name="errorCode">错误码。</param>
    /// <param name="innerException">内部异常。</param>
    /// <returns>友好异常实例。</returns>
    private static AppFriendlyException CreateFriendlyException(string message, object errorCode, Exception innerException = null)
    {
        return innerException == null
            ? new AppFriendlyException(message, errorCode)
            : new AppFriendlyException(message, errorCode, innerException);
    }

    /// <summary>
    /// 按配置把普通友好异常转换为业务异常。
    /// </summary>
    /// <param name="exception">友好异常实例。</param>
    /// <returns>友好异常实例。</returns>
    private static AppFriendlyException ApplyDefaultBusinessExceptionPolicy(AppFriendlyException exception)
    {
        return FriendlyExceptionSettings.ThrowBah == true
            ? MarkAsBusinessException(exception)
            : exception;
    }

    /// <summary>
    /// 标记为业务验证异常。
    /// </summary>
    /// <param name="exception">友好异常实例。</param>
    /// <returns>友好异常实例。</returns>
    private static AppFriendlyException MarkAsBusinessException(AppFriendlyException exception)
    {
        exception.StatusCode(StatusCodes.Status400BadRequest);
        exception.ValidationException = true;
        return exception;
    }

    /// <summary>
    /// 创建内部异常实例。
    /// </summary>
    /// <param name="exceptionType">内部异常类型。</param>
    /// <param name="message">异常消息。</param>
    /// <returns>内部异常实例。</returns>
    private static Exception CreateInnerException(Type exceptionType, string message)
    {
        if (exceptionType == null)
        {
            throw new ArgumentNullException(nameof(exceptionType));
        }

        if (!typeof(Exception).IsAssignableFrom(exceptionType))
        {
            throw new ArgumentException($"{exceptionType.FullName} must inherit from {nameof(Exception)}.", nameof(exceptionType));
        }

        var stringConstructor = exceptionType.GetConstructor(new[] { typeof(string) });
        if (stringConstructor != null)
        {
            return (Exception)stringConstructor.Invoke(new object[] { message });
        }

        var defaultConstructor = exceptionType.GetConstructor(Type.EmptyTypes);
        if (defaultConstructor != null)
        {
            return (Exception)defaultConstructor.Invoke(null);
        }

        throw new ArgumentException($"{exceptionType.FullName} must declare a public parameterless constructor or a public constructor with a single string parameter.", nameof(exceptionType));
    }

    /// <summary>
    /// 解析错误码对应的异常消息。
    /// </summary>
    /// <param name="errorCode">错误码。</param>
    /// <param name="args">格式化参数。</param>
    /// <returns>异常消息。</returns>
    private static string ResolveErrorCodeMessage(object errorCode, params object[] args)
    {
        var normalizedErrorCode = HandleEnumErrorCode(errorCode);
        var methodIfException = ResolveCurrentEndpointExceptionMetadata();
        var ifExceptionAttribute = ResolveIfExceptionAttribute(methodIfException, normalizedErrorCode);
        var errorCodeMessage = ResolveConfiguredErrorCodeMessage(methodIfException, normalizedErrorCode, ifExceptionAttribute);

        return MontageErrorMessage(
            errorCodeMessage,
            normalizedErrorCode?.ToString(),
            args != null && args.Length > 0 ? args : ifExceptionAttribute?.Args);
    }

    /// <summary>
    /// 解析匹配当前错误码的 IfExceptionAttribute。
    /// </summary>
    /// <param name="methodIfException">方法异常元数据。</param>
    /// <param name="errorCode">错误码。</param>
    /// <returns>匹配的异常特性。</returns>
    private static IfExceptionAttribute ResolveIfExceptionAttribute(MethodIfException methodIfException, object errorCode)
    {
        return methodIfException.IfExceptionAttributes
                                .FirstOrDefault(attribute =>
                                    attribute.ErrorCode != null
                                    && HandleEnumErrorCode(attribute.ErrorCode)?.ToString() == errorCode?.ToString());
    }

    /// <summary>
    /// 从特性、错误码字典或默认配置中解析错误消息。
    /// </summary>
    /// <param name="methodIfException">方法异常元数据。</param>
    /// <param name="errorCode">错误码。</param>
    /// <param name="ifExceptionAttribute">匹配的异常特性。</param>
    /// <returns>错误消息。</returns>
    private static string ResolveConfiguredErrorCodeMessage(
        MethodIfException methodIfException,
        object errorCode,
        IfExceptionAttribute ifExceptionAttribute)
    {
        var errorCodeMessage = ifExceptionAttribute == null || string.IsNullOrWhiteSpace(ifExceptionAttribute.ErrorMessage)
            ? ErrorCodeMessages.GetValueOrDefault(errorCode?.ToString() ?? string.Empty) ?? FriendlyExceptionSettings.DefaultErrorMessage
            : ifExceptionAttribute.ErrorMessage;

        if (!string.IsNullOrWhiteSpace(errorCodeMessage)) return errorCodeMessage;

        return methodIfException.IfExceptionAttributes
                                .FirstOrDefault(attribute => attribute.ErrorCode == null && !string.IsNullOrWhiteSpace(attribute.ErrorMessage))
                                ?.ErrorMessage;
    }

    /// <summary>
    /// 将带元数据的枚举错误码转换为最终错误码。
    /// </summary>
    /// <param name="errorCode">错误码。</param>
    /// <returns>规范化后的错误码。</returns>
    private static object HandleEnumErrorCode(object errorCode)
    {
        if (errorCode == null) return null;

        var errorType = errorCode.GetType();
        if (!ErrorCodeTypes.Any(type => type == errorType)) return errorCode;

        var fieldInfo = errorType.GetField(Enum.GetName(errorType, errorCode));
        if (fieldInfo?.IsDefined(typeof(ErrorCodeItemMetadataAttribute), true) != true) return errorCode;

        return GetErrorCodeItemMessage(fieldInfo).Key;
    }

    /// <summary>
    /// 获取所有错误码定义类型。
    /// </summary>
    /// <returns>错误码定义类型集合。</returns>
    private static IEnumerable<Type> ResolveErrorCodeTypes()
    {
        var errorCodeTypes = AppCore.EffectiveTypes
                                    .Where(type => type.IsDefined(typeof(ErrorCodeTypeAttribute), true) && type.IsEnum);

        var errorCodeTypeProvider = AppCore.GetService<IErrorCodeTypeProvider>(AppCore.RootServices);
        if (errorCodeTypeProvider is { Definitions: not null })
            errorCodeTypes = errorCodeTypes.Concat(errorCodeTypeProvider.Definitions);

        return errorCodeTypes.Distinct();
    }

    /// <summary>
    /// 获取错误码消息字典。
    /// </summary>
    /// <returns>错误码消息字典。</returns>
    private static ConcurrentDictionary<string, string> ResolveErrorCodeMessages()
    {
        var errorCodeMessages = new ConcurrentDictionary<string, string>();

        AddEnumErrorCodeMessages(errorCodeMessages);
        AddConfiguredErrorCodeMessages(errorCodeMessages);

        return errorCodeMessages;
    }

    /// <summary>
    /// 加载枚举特性定义的错误码消息。
    /// </summary>
    /// <param name="errorCodeMessages">错误码消息字典。</param>
    private static void AddEnumErrorCodeMessages(ConcurrentDictionary<string, string> errorCodeMessages)
    {
        var enumErrorCodeMessages = ErrorCodeTypes.SelectMany(type =>
            type.GetFields().Where(field => field.IsDefined(typeof(ErrorCodeItemMetadataAttribute))));

        foreach (var fieldInfo in enumErrorCodeMessages)
        {
            var (key, value) = GetErrorCodeItemMessage(fieldInfo);
            errorCodeMessages[key.ToString()] = value;
        }
    }

    /// <summary>
    /// 加载配置文件定义的错误码消息。
    /// </summary>
    /// <param name="errorCodeMessages">错误码消息字典。</param>
    private static void AddConfiguredErrorCodeMessages(ConcurrentDictionary<string, string> errorCodeMessages)
    {
        var errorCodeMessageSettings = AppConfiguration.GetConfig<ErrorCodeMessageSettingsOptions>("ErrorCodeMessageSettings", true);
        if (errorCodeMessageSettings is not { Definitions: not null }) return;

        var configuredMessages = errorCodeMessageSettings.Definitions
                                                         .Where(definition => definition.Length > 1)
                                                         .ToDictionary(definition => definition[0].ToString(), FixErrorCodeSettingMessage);

        foreach (var (key, value) in configuredMessages)
        {
            errorCodeMessages[key] = value;
        }
    }

    /// <summary>
    /// 处理配置文件中的错误码消息。
    /// </summary>
    /// <param name="errorCodes">错误码配置。</param>
    /// <returns>格式化后的错误消息。</returns>
    private static string FixErrorCodeSettingMessage(object[] errorCodes)
    {
        var args = errorCodes.Skip(2).ToArray();
        var errorMessage = errorCodes[1].ToString();
        return string.Format(errorMessage, args);
    }

    /// <summary>
    /// 获取当前调用点的 IfExceptionAttribute 元数据。
    /// </summary>
    /// <returns>方法异常元数据。</returns>
    private static MethodIfException ResolveCurrentEndpointExceptionMetadata()
    {
        var endpointMethod = ResolveCurrentEndpointMethod();
        if (endpointMethod != null)
        {
            return ErrorMethods.GetOrAdd(endpointMethod, CreateEndpointMethodIfException);
        }

        var stackTrace = new StackTrace();
        var stackFrame = ResolveEndpointStackFrame(stackTrace);
        var errorMethod = stackFrame?.GetMethod();
        return errorMethod == null
            ? MethodIfException.Empty
            : ErrorMethods.GetOrAdd(errorMethod, _ => CreateMethodIfException(errorMethod, stackTrace));
    }

    /// <summary>
    /// 从当前 HTTP Endpoint 中解析动作方法，避免 Web 请求中依赖 StackTrace。
    /// </summary>
    /// <returns>当前 Endpoint 对应的方法。</returns>
    private static MethodBase ResolveCurrentEndpointMethod()
    {
        var endpoint = AppCore.HttpContext?.GetEndpoint();
        if (endpoint == null) return null;

        var actionDescriptor = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();
        if (actionDescriptor?.MethodInfo != null)
        {
            return actionDescriptor.MethodInfo;
        }

        return endpoint.Metadata.OfType<MethodInfo>().FirstOrDefault();
    }

    /// <summary>
    /// 从 Endpoint 方法和控制器类型上创建 IfException 元数据。
    /// </summary>
    /// <param name="errorMethod">当前 Endpoint 方法。</param>
    /// <returns>方法级异常元数据。</returns>
    private static MethodIfException CreateEndpointMethodIfException(MethodBase errorMethod)
    {
        return new MethodIfException
        {
            ErrorMethod = errorMethod,
            IfExceptionAttributes = GetIfExceptionAttributes(errorMethod).ToArray()
        };
    }

    /// <summary>
    /// 解析最能代表当前业务调用点的堆栈帧。
    /// </summary>
    /// <param name="stackTrace">增强堆栈。</param>
    /// <returns>堆栈帧。</returns>
    private static StackFrame ResolveEndpointStackFrame(StackTrace stackTrace)
    {
        var frames = stackTrace.GetFrames() ?? Array.Empty<StackFrame>();

        return frames.FirstOrDefault(frame =>
                   typeof(ControllerBase).IsAssignableFrom(frame.GetMethod()?.DeclaringType)
                   || typeof(IDynamicService).IsAssignableFrom(frame.GetMethod()?.DeclaringType))
               ?? frames.FirstOrDefault(frame => frame.GetMethod()?.DeclaringType?.Namespace != typeof(Oops).Namespace);
    }

    /// <summary>
    /// 创建方法异常元数据。
    /// </summary>
    /// <param name="errorMethod">错误方法。</param>
    /// <param name="stackTrace">增强堆栈。</param>
    /// <returns>方法异常元数据。</returns>
    private static MethodIfException CreateMethodIfException(MethodBase errorMethod, StackTrace stackTrace)
    {
        var frames = stackTrace.GetFrames() ?? Array.Empty<StackFrame>();

        return new MethodIfException
        {
            ErrorMethod = errorMethod,
            IfExceptionAttributes = frames
                .Select(frame => frame.GetMethod())
                .Where(method => method != null)
                .SelectMany(GetIfExceptionAttributes)
                .ToArray()
        };
    }

    /// <summary>
    /// 获取方法和声明类型上的 IfException 特性，方法级优先于类型级。
    /// </summary>
    /// <param name="method">方法元数据。</param>
    /// <returns>IfException 特性集合。</returns>
    private static IEnumerable<IfExceptionAttribute> GetIfExceptionAttributes(MethodBase method)
    {
        var methodAttributes = method.GetCustomAttributes<IfExceptionAttribute>(true);
        var typeAttributes = method.DeclaringType?.GetCustomAttributes<IfExceptionAttribute>(true)
                            ?? Enumerable.Empty<IfExceptionAttribute>();

        return methodAttributes.Concat(typeAttributes);
    }

    /// <summary>
    /// 获取错误码字段上的错误码和错误消息。
    /// </summary>
    /// <param name="fieldInfo">字段对象。</param>
    /// <returns>错误码和错误消息。</returns>
    private static (object Key, string Value) GetErrorCodeItemMessage(FieldInfo fieldInfo)
    {
        var errorCodeItemMetadata = fieldInfo.GetCustomAttribute<ErrorCodeItemMetadataAttribute>();
        return (
            errorCodeItemMetadata.ErrorCode ?? fieldInfo.Name,
            string.Format(errorCodeItemMetadata.ErrorMessage, errorCodeItemMetadata.Args));
    }

    /// <summary>
    /// 拼装最终错误消息。
    /// </summary>
    /// <param name="errorMessage">错误消息。</param>
    /// <param name="errorCode">错误码。</param>
    /// <param name="args">格式化参数。</param>
    /// <returns>最终错误消息。</returns>
    private static string MontageErrorMessage(string errorMessage, string errorCode, params object[] args)
    {
        var message = string.IsNullOrWhiteSpace(errorMessage)
            ? FriendlyExceptionSettings.DefaultErrorMessage
            : errorMessage;

        var prefix = FriendlyExceptionSettings.HideErrorCode == true || string.IsNullOrWhiteSpace(errorCode)
            ? string.Empty
            : $"[{errorCode}] ";

        return string.Format(prefix + message, args ?? Array.Empty<object>());
    }
}
