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
using Air.Cloud.WebApp.DataValidation.Validators;

using System.ComponentModel.DataAnnotations;

namespace Air.Cloud.WebApp.DataValidation.Attributes;

/// <summary>
/// 数据类型验证特性。
/// </summary>
[IgnoreScanning]
public class DataValidationAttribute : ValidationAttribute
{
    /// <summary>
    /// 构造数据类型验证特性。
    /// </summary>
    /// <param name="validationPattern">多规则组合方式。</param>
    /// <param name="validationTypes">验证规则枚举值。</param>
    public DataValidationAttribute(ValidationPattern validationPattern, params object[] validationTypes)
    {
        ValidationPattern = validationPattern;
        ValidationTypes = validationTypes;
    }

    /// <summary>
    /// 构造数据类型验证特性，默认要求全部规则通过。
    /// </summary>
    /// <param name="validationTypes">验证规则枚举值。</param>
    public DataValidationAttribute(params object[] validationTypes)
    {
        ValidationPattern = ValidationPattern.AllOfThem;
        ValidationTypes = validationTypes;
    }

    /// <summary>
    /// 验证当前属性值。
    /// </summary>
    /// <param name="value">属性值。</param>
    /// <param name="validationContext">验证上下文。</param>
    /// <returns>验证结果。</returns>
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        if (AllowNullValue && value == null)
        {
            return ValidationResult.Success;
        }

        if (AllowEmptyStrings && value is string && string.IsNullOrEmpty(value?.ToString()))
        {
            return ValidationResult.Success;
        }

        var result = DataValidator.TryValidateByTypes(value, ValidationPattern, ValidationTypes);
        result.MemberOrValue = validationContext.MemberName;

        if (result.Passed)
        {
            return ValidationResult.Success;
        }

        var resultMessage = result.ValidationResults.FirstOrDefault()?.ErrorMessage;
        var errorMessage = !string.IsNullOrWhiteSpace(ErrorMessage) ? ErrorMessage : resultMessage;

        return new ValidationResult(string.Format(errorMessage, validationContext.MemberName));
    }

    /// <summary>
    /// 验证规则枚举值。
    /// </summary>
    public object[] ValidationTypes { get; set; }

    /// <summary>
    /// 多规则组合方式。
    /// </summary>
    public ValidationPattern ValidationPattern { get; set; }

    /// <summary>
    /// 是否允许空字符串。
    /// </summary>
    public bool AllowEmptyStrings { get; set; } = false;

    /// <summary>
    /// 是否允许空值；允许时仅在有值时执行规则验证。
    /// </summary>
    public bool AllowNullValue { get; set; } = false;
}
