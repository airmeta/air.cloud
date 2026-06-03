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
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Air.Cloud.WebApp.DataValidation.Internal;

/// <summary>
/// <para>zh-cn:验证信息元数据，承载统一验证失败结果和日志明细。</para>
/// <para>en-us:Validation metadata carrying the unified validation failure result and log details.</para>
/// </summary>
public sealed class ValidationMetadata
{
    private object _errorCode;
    private int? _statusCode;

    /// <summary>
    /// <para>zh-cn:验证结果；当前默认返回 ValidationFailureResult，便于前端稳定解析。</para>
    /// <para>en-us:Validation result; defaults to ValidationFailureResult for stable frontend parsing.</para>
    /// </summary>
    /// <remarks>
    /// <para>zh-cn:保留 object 类型是为了兼容自定义统一返回提供器。</para>
    /// <para>en-us:The object type is kept for compatibility with custom unified result providers.</para>
    /// </remarks>
    public object ValidationResult { get; internal set; }

    /// <summary>
    /// <para>zh-cn:可展示的摘要消息。</para>
    /// <para>en-us:Displayable summary message.</para>
    /// </summary>
    public string Message { get; internal set; }

    /// <summary>
    /// <para>zh-cn:完整验证明细消息，主要用于日志输出和排查问题。</para>
    /// <para>en-us:Detailed validation message mainly used for logging and diagnostics.</para>
    /// </summary>
    public string DetailMessage { get; internal set; }

    /// <summary>
    /// <para>zh-cn:验证状态。</para>
    /// <para>en-us:Validation state.</para>
    /// </summary>
    public ModelStateDictionary ModelState { get; internal set; }

    /// <summary>
    /// <para>zh-cn:业务错误码；设置后会同步写入 ValidationFailureResult。</para>
    /// <para>en-us:Business error code; when set, it is synchronized into ValidationFailureResult.</para>
    /// </summary>
    public object ErrorCode
    {
        get => _errorCode;
        internal set
        {
            _errorCode = value;
            if (ValidationResult is ValidationFailureResult validationFailureResult)
            {
                validationFailureResult.ErrorCode = value;
            }
        }
    }

    /// <summary>
    /// <para>zh-cn:HTTP 状态码；设置后会同步写入 ValidationFailureResult。</para>
    /// <para>en-us:HTTP status code; when set, it is synchronized into ValidationFailureResult.</para>
    /// </summary>
    public int? StatusCode
    {
        get => _statusCode;
        internal set
        {
            _statusCode = value;
            if (value.HasValue && ValidationResult is ValidationFailureResult validationFailureResult)
            {
                validationFailureResult.StatusCode = value.Value;
            }
        }
    }
}
