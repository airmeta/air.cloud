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
using System.Text.RegularExpressions;

namespace Air.Cloud.WebApp.DataValidation.Attributes;

/// <summary>
/// 验证项元数据
/// </summary>
[IgnoreScanning, AttributeUsage(AttributeTargets.Field)]
public class ValidationItemMetadataAttribute : Attribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="regularExpression">正则表达式</param>
    /// <param name="defaultErrorMessage">失败提示默认消息</param>
    /// <param name="regexOptions">正则表达式匹配选项</param>
    public ValidationItemMetadataAttribute(string regularExpression, string defaultErrorMessage, RegexOptions regexOptions = RegexOptions.None)
    {
        RegularExpression = regularExpression;
        DefaultErrorMessage = defaultErrorMessage;
        RegexOptions = regexOptions;
    }

    /// <summary>
    /// 正则表达式
    /// </summary>
    public string RegularExpression { get; set; }

    /// <summary>
    /// 默认验证失败类型
    /// </summary>
    public string DefaultErrorMessage { get; set; }

    /// <summary>
    /// 正则表达式选项
    /// </summary>
    public RegexOptions RegexOptions { get; set; }
}