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


using Air.Cloud.Core.Standard;

namespace Air.Cloud.WebApp.FriendlyException.Attributes;

/// <summary>
/// 异常复写特性
/// </summary>
[IgnoreScanning, AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public sealed class IfExceptionAttribute : Attribute
{
    /// <summary>
    /// 错误编码。
    /// </summary>
    public object ErrorCode { get; set; }

    /// <summary>
    /// 异常类型。
    /// </summary>
    public Type ExceptionType { get; set; }

    /// <summary>
    /// 错误消息。
    /// </summary>
    public string ErrorMessage { get; set; }

    /// <summary>
    /// 格式化参数。
    /// </summary>
    public object[] Args { get; set; }

    /// <summary>
    /// 默认构造函数
    /// </summary>
    public IfExceptionAttribute()
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="errorCode">错误编码</param>
    /// <param name="args">格式化参数</param>
    public IfExceptionAttribute(object errorCode, params object[] args)
    {
        ErrorCode = errorCode;
        Args = args;
    }

    /// <summary>
    /// 捕获特定异常类型异常（用于全局异常捕获）
    /// </summary>
    /// <param name="exceptionType"></param>
    public IfExceptionAttribute(Type exceptionType)
    {
        ExceptionType = exceptionType;
    }
}
