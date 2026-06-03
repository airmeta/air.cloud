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


namespace Air.Cloud.WebApp.FriendlyException.Attributes;

/// <summary>
/// 异常元数据特性
/// </summary>
[IgnoreScanning, AttributeUsage(AttributeTargets.Field)]
public sealed class ErrorCodeItemMetadataAttribute : Attribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="errorMessage">错误消息</param>
    /// <param name="args">格式化参数</param>
    public ErrorCodeItemMetadataAttribute(string errorMessage, params object[] args)
    {
        ErrorMessage = errorMessage;
        Args = args;
    }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string ErrorMessage { get; set; }

    /// <summary>
    /// 错误码
    /// </summary>
    public object ErrorCode { get; set; }

    /// <summary>
    /// 格式化参数
    /// </summary>
    public object[] Args { get; set; }
}
