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
using Air.Cloud.WebApp.DataValidation.Extensions;
using Air.Cloud.WebApp.DataValidation.Results;

using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Air.Cloud.WebApp.DataValidation.Validators;

/// <summary>
/// 实例级数据验证访问器，主要用于 IValidatableObject.Validate 内部的手动字段验证。
/// </summary>
[IgnoreScanning]
public sealed class DataValidationAccessor
{
    /// <summary>
    /// 当前对象实例。
    /// </summary>
    private readonly object _owner;

    /// <summary>
    /// 构造实例级验证访问器。
    /// </summary>
    /// <param name="owner">当前对象实例。</param>
    internal DataValidationAccessor(object owner)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    /// <summary>
    /// 验证当前对象模型，包含 DataAnnotations 和 IValidatableObject。
    /// </summary>
    /// <param name="validateAllProperties">是否验证所有属性。</param>
    /// <returns>验证结果。</returns>
    public DataValidationResult ValidateSelf(bool validateAllProperties = true)
    {
        return DataValidator.TryValidateObjectModel(_owner, validateAllProperties);
    }

    /// <summary>
    /// 使用 DataAnnotations 特性验证单个值。
    /// </summary>
    /// <param name="value">待验证值。</param>
    /// <param name="validationAttributes">验证特性。</param>
    /// <returns>验证结果。</returns>
    public DataValidationResult ValidateValue(object value, params ValidationAttribute[] validationAttributes)
    {
        return DataValidator.TryValidateByAttributes(value, validationAttributes);
    }

    /// <summary>
    /// 使用内置或自定义验证类型验证单个值，所有规则通过才算成功。
    /// </summary>
    /// <param name="value">待验证值。</param>
    /// <param name="validationTypes">验证规则枚举值。</param>
    /// <returns>验证结果。</returns>
    public DataValidationResult ValidateValue(object value, params object[] validationTypes)
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
    public DataValidationResult ValidateValue(
        object value,
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
    public DataValidationResult MatchPattern(
        object value,
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
    public bool IsMatch(object value, string regexPattern, RegexOptions regexOptions = RegexOptions.None)
    {
        return DataValidator.IsMatchPattern(value, regexPattern, regexOptions);
    }

    /// <summary>
    /// 验证当前对象模型，失败时抛出统一验证异常。
    /// </summary>
    /// <param name="validateAllProperties">是否验证所有属性。</param>
    public void EnsureSelf(bool validateAllProperties = true)
    {
        ValidateSelf(validateAllProperties).ThrowValidateFailedModel();
    }

    /// <summary>
    /// 使用 DataAnnotations 特性验证单个值，失败时抛出统一验证异常。
    /// </summary>
    /// <param name="value">待验证值。</param>
    /// <param name="validationAttributes">验证特性。</param>
    public void EnsureValue(object value, params ValidationAttribute[] validationAttributes)
    {
        ValidateValue(value, validationAttributes).ThrowValidateFailedModel();
    }

    /// <summary>
    /// 使用内置或自定义验证类型验证单个值，失败时抛出统一验证异常。
    /// </summary>
    /// <param name="value">待验证值。</param>
    /// <param name="validationTypes">验证规则枚举值。</param>
    public void EnsureValue(object value, params object[] validationTypes)
    {
        ValidateValue(value, validationTypes).ThrowValidateFailedModel();
    }

    /// <summary>
    /// 使用内置或自定义验证类型验证单个值，失败时抛出统一验证异常。
    /// </summary>
    /// <param name="value">待验证值。</param>
    /// <param name="validationPattern">多规则组合方式。</param>
    /// <param name="validationTypes">验证规则枚举值。</param>
    public void EnsureValue(object value, ValidationPattern validationPattern, params object[] validationTypes)
    {
        ValidateValue(value, validationPattern, validationTypes).ThrowValidateFailedModel();
    }
}
