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

using Air.Cloud.WebApp.DataValidation.Enums;
using Air.Cloud.WebApp.DataValidation.Results;
using Air.Cloud.WebApp.DataValidation.Validators;
using Air.Cloud.WebApp.FriendlyException.Exceptions;

using Microsoft.AspNetCore.Http;

using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Air.Cloud.WebApp.DataValidation.Extensions;

/// <summary>
/// 数据验证扩展入口。
/// </summary>
[IgnoreScanning]
public static class DataValidationExtensions
{
    /// <summary>
    /// 为 IValidatableObject 创建实例级验证访问器。
    /// </summary>
    /// <param name="instance">当前验证对象。</param>
    /// <returns>实例级验证访问器。</returns>
    public static DataValidationAccessor Validator(this IValidatableObject instance)
    {
        return new DataValidationAccessor(instance);
    }

    /// <summary>
    /// 使用 DataAnnotations 验证当前对象模型。
    /// </summary>
    /// <param name="obj">对象实例。</param>
    /// <param name="validateAllProperties">是否验证所有属性。</param>
    /// <returns>验证结果。</returns>
    public static DataValidationResult TryValidateObjectModel(this object obj, bool validateAllProperties = true)
    {
        return DataValidator.TryValidateObjectModel(obj, validateAllProperties);
    }

    /// <summary>
    /// 使用 DataAnnotations 特性验证单个值。
    /// </summary>
    /// <param name="value">待验证值。</param>
    /// <param name="validationAttributes">验证特性。</param>
    /// <returns>验证结果。</returns>
    public static DataValidationResult TryValidateByAttributes(this object value, params ValidationAttribute[] validationAttributes)
    {
        return DataValidator.TryValidateByAttributes(value, validationAttributes);
    }

    /// <summary>
    /// 使用内置或自定义验证类型验证单个值，所有规则通过才算成功。
    /// </summary>
    /// <param name="value">待验证值。</param>
    /// <param name="validationTypes">验证规则枚举值。</param>
    /// <returns>验证结果。</returns>
    public static DataValidationResult TryValidateByTypes(this object value, params object[] validationTypes)
    {
        return DataValidator.TryValidateByTypes(value, validationTypes);
    }

    /// <summary>
    /// 使用内置或自定义验证类型验证单个值。
    /// </summary>
    /// <param name="value">待验证值。</param>
    /// <param name="validationPattern">多规则组合方式。</param>
    /// <param name="validationTypes">验证规则枚举值。</param>
    /// <returns>验证结果。</returns>
    public static DataValidationResult TryValidateByTypes(
        this object value,
        ValidationPattern validationPattern,
        params object[] validationTypes)
    {
        return DataValidator.TryValidateByTypes(value, validationPattern, validationTypes);
    }

    /// <summary>
    /// 使用正则表达式验证单个值，并返回完整验证结果。
    /// </summary>
    /// <param name="value">待验证值。</param>
    /// <param name="regexPattern">正则表达式。</param>
    /// <param name="regexOptions">正则表达式选项。</param>
    /// <returns>验证结果。</returns>
    public static DataValidationResult TryMatchPattern(
        this object value,
        string regexPattern,
        RegexOptions regexOptions = RegexOptions.None)
    {
        return DataValidator.TryMatchPattern(value, regexPattern, regexOptions);
    }

    /// <summary>
    /// 使用正则表达式验证单个值，并返回布尔结果。
    /// </summary>
    /// <param name="value">待验证值。</param>
    /// <param name="regexPattern">正则表达式。</param>
    /// <param name="regexOptions">正则表达式选项。</param>
    /// <returns>是否匹配。</returns>
    public static bool IsMatchPattern(this object value, string regexPattern, RegexOptions regexOptions = RegexOptions.None)
    {
        return DataValidator.IsMatchPattern(value, regexPattern, regexOptions);
    }

    /// <summary>
    /// 验证当前对象模型，失败时抛出统一验证异常。
    /// </summary>
    /// <param name="obj">对象实例。</param>
    /// <param name="validateAllProperties">是否验证所有属性。</param>
    public static void EnsureValidObjectModel(this object obj, bool validateAllProperties = true)
    {
        DataValidator.TryValidateObjectModel(obj, validateAllProperties).ThrowValidateFailedModel();
    }

    /// <summary>
    /// 使用 DataAnnotations 特性验证单个值，失败时抛出统一验证异常。
    /// </summary>
    /// <param name="value">待验证值。</param>
    /// <param name="validationAttributes">验证特性。</param>
    public static void EnsureValidByAttributes(this object value, params ValidationAttribute[] validationAttributes)
    {
        DataValidator.TryValidateByAttributes(value, validationAttributes).ThrowValidateFailedModel();
    }

    /// <summary>
    /// 使用内置或自定义验证类型验证单个值，失败时抛出统一验证异常。
    /// </summary>
    /// <param name="value">待验证值。</param>
    /// <param name="validationTypes">验证规则枚举值。</param>
    public static void EnsureValidByTypes(this object value, params object[] validationTypes)
    {
        DataValidator.TryValidateByTypes(value, validationTypes).ThrowValidateFailedModel();
    }

    /// <summary>
    /// 使用内置或自定义验证类型验证单个值，失败时抛出统一验证异常。
    /// </summary>
    /// <param name="value">待验证值。</param>
    /// <param name="validationPattern">多规则组合方式。</param>
    /// <param name="validationTypes">验证规则枚举值。</param>
    public static void EnsureValidByTypes(this object value, ValidationPattern validationPattern, params object[] validationTypes)
    {
        DataValidator.TryValidateByTypes(value, validationPattern, validationTypes).ThrowValidateFailedModel();
    }

    /// <summary>
    /// 验证失败时抛出统一验证异常。
    /// </summary>
    /// <param name="dataValidationResult">验证结果。</param>
    public static void ThrowValidateFailedModel(this DataValidationResult dataValidationResult)
    {
        ArgumentNullException.ThrowIfNull(dataValidationResult);

        if (!dataValidationResult.Failed)
        {
            return;
        }

        var validationFailMessage = dataValidationResult.ValidationResults
            .Select(u => new
            {
                MemberNames = u.MemberNames.Any() ? u.MemberNames : new[] { $"{dataValidationResult.MemberOrValue}" },
                u.ErrorMessage
            })
            .OrderBy(u => u.MemberNames.First())
            .GroupBy(u => u.MemberNames.First())
            .ToDictionary(x => x.Key, u => u.Select(c => c.ErrorMessage).ToArray());

        throw new AppFriendlyException(default, default, new ValidationException())
        {
            StatusCode = StatusCodes.Status400BadRequest,
            ValidationException = true,
            ErrorMessage = validationFailMessage,
        };
    }
}
