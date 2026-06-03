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
using Microsoft.AspNetCore.Mvc.ModelBinding;

using System.Text.Encodings.Web;
using System.Text.Json;

namespace Air.Cloud.WebApp.DataValidation.Internal;

/// <summary>
/// <para>zh-cn:验证上下文，用于将不同来源的验证错误转换为统一元数据。</para>
/// <para>en-us:Validation context used to convert validation errors from different sources into unified metadata.</para>
/// </summary>
internal static class ValidatorContext
{
    /// <summary>
    /// <para>zh-cn:获取验证错误信息，并统一转换为前端稳定解析的 ValidationFailureResult。</para>
    /// <para>en-us:Gets validation error information and converts it into a frontend-stable ValidationFailureResult.</para>
    /// </summary>
    /// <param name="errors">
    /// <para>zh-cn:验证错误来源，支持 ModelStateDictionary、ValidationProblemDetails、Dictionary&lt;string,string[]&gt; 和普通字符串。</para>
    /// <para>en-us:Validation error source; supports ModelStateDictionary, ValidationProblemDetails, Dictionary&lt;string,string[]&gt;, and plain strings.</para>
    /// </param>
    /// <returns></returns>
    internal static ValidationMetadata GetValidationMetadata(object errors)
    {
        ModelStateDictionary _modelState = null;
        Dictionary<string, string[]> fieldErrors = new();
        string[] plainErrors = Array.Empty<string>();

        // 如果是模型验证字典类型
        // Convert MVC model state errors into field-grouped errors.
        if (errors is ModelStateDictionary modelState)
        {
            _modelState = modelState;
            fieldErrors = modelState.Where(u => modelState[u.Key].ValidationState == ModelValidationState.Invalid)
                    .ToDictionary(u => u.Key, u => NormalizeMessages(modelState[u.Key].Errors.Select(c => c.ErrorMessage)));
        }
        // 如果是 ValidationProblemDetails 特殊类型
        // Convert ASP.NET Core ValidationProblemDetails into field-grouped errors.
        else if (errors is ValidationProblemDetails validation)
        {
            fieldErrors = validation.Errors
                .ToDictionary(u => u.Key, u => NormalizeMessages(u.Value));
        }
        // 如果是字典类型
        // Keep existing dictionary-shaped validation errors but normalize empty messages.
        else if (errors is Dictionary<string, string[]> dicResults)
        {
            fieldErrors = dicResults.ToDictionary(u => u.Key, u => NormalizeMessages(u.Value));
        }
        // 其他类型
        // Treat all other values as global validation messages.
        else
        {
            plainErrors = NormalizeMessages(new[] { errors?.ToString() });
        }

        var flattenedErrors = fieldErrors.Any()
            ? fieldErrors.SelectMany(u => u.Value).Where(u => !string.IsNullOrWhiteSpace(u)).Distinct().ToArray()
            : plainErrors;

        var validationFailureResult = new ValidationFailureResult
        {
            Message = fieldErrors.Any() ? "请求参数验证失败" : flattenedErrors.FirstOrDefault() ?? "请求参数验证失败",
            Fields = fieldErrors,
            Errors = flattenedErrors
        };

        return new ValidationMetadata
        {
            ValidationResult = validationFailureResult,
            Message = validationFailureResult.Message,
            DetailMessage = JsonSerializer.Serialize(validationFailureResult, new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true
            }),
            ModelState = _modelState
        };
    }

    /// <summary>
    /// <para>zh-cn:标准化错误消息集合，过滤空白内容并保留可展示的文本。</para>
    /// <para>en-us:Normalizes error message collections by filtering blank values and preserving displayable text.</para>
    /// </summary>
    /// <param name="messages">
    /// <para>zh-cn:原始错误消息集合。</para>
    /// <para>en-us:Raw error message collection.</para>
    /// </param>
    /// <returns></returns>
    private static string[] NormalizeMessages(IEnumerable<string> messages)
    {
        return messages?
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .Select(u => u.Trim())
            .ToArray() ?? Array.Empty<string>();
    }
}
