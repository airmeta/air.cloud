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
using Air.Cloud.WebApp.DataValidation.Attributes;
using Air.Cloud.WebApp.DataValidation.Enums;
using Air.Cloud.WebApp.DataValidation.Options;
using Air.Cloud.WebApp.DataValidation.Providers;
using Air.Cloud.WebApp.DataValidation.Results;

using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Air.Cloud.WebApp.DataValidation.Validators;

/// <summary>
/// 数据验证器。
/// </summary>
[IgnoreScanning]
public static class DataValidator
{
    /// <summary>
    /// 所有可用的验证类型定义。
    /// </summary>
    private static readonly IEnumerable<Type> ValidationTypeDefinitions;

    /// <summary>
    /// 所有可用的验证消息类型定义。
    /// </summary>
    private static readonly IEnumerable<Type> ValidationMessageTypeDefinitions;

    /// <summary>
    /// 验证项名称与正则元数据映射。
    /// </summary>
    private static readonly ConcurrentDictionary<string, ValidationItemMetadataAttribute> ValidationItemMetadatas;

    /// <summary>
    /// 验证规则元数据缓存。
    /// </summary>
    private static readonly ConcurrentDictionary<object, (string, ValidationItemMetadataAttribute)> ValidationItemMetadataCache;

    static DataValidator()
    {
        ValidationTypeDefinitions = GetValidationTypeDefinitions().ToArray();
        ValidationMessageTypeDefinitions = GetValidationMessageTypeDefinitions().ToArray();
        ValidationItemMetadatas = BuildValidationItemMetadatas();
        ValidationItemMetadataCache = new ConcurrentDictionary<object, (string, ValidationItemMetadataAttribute)>();
    }

    /// <summary>
    /// 验证对象模型，包含 DataAnnotations 和 IValidatableObject。
    /// </summary>
    /// <param name="obj">对象实例。</param>
    /// <param name="validateAllProperties">是否验证所有属性。</param>
    /// <returns>验证结果。</returns>
    public static DataValidationResult TryValidateObjectModel(object obj, bool validateAllProperties = true)
    {
        ArgumentNullException.ThrowIfNull(obj);

        if (ShouldSkipObjectValidation(obj))
        {
            return CreateResult(true, obj);
        }

        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(obj, new ValidationContext(obj), results, validateAllProperties);

        return CreateResult(isValid, obj, results);
    }

    /// <summary>
    /// 使用 DataAnnotations 特性验证单个值。
    /// </summary>
    /// <param name="value">待验证值。</param>
    /// <param name="validationAttributes">验证特性。</param>
    /// <returns>验证结果。</returns>
    public static DataValidationResult TryValidateByAttributes(object value, params ValidationAttribute[] validationAttributes)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(value ?? new object());
        var attributes = validationAttributes ?? Array.Empty<ValidationAttribute>();
        var isValid = Validator.TryValidateValue(value, context, results, attributes);

        return CreateResult(isValid, value, results);
    }

    /// <summary>
    /// 使用内置或自定义验证类型验证单个值，所有规则通过才算成功。
    /// </summary>
    /// <param name="value">待验证值。</param>
    /// <param name="validationTypes">验证规则枚举值。</param>
    /// <returns>验证结果。</returns>
    public static DataValidationResult TryValidateByTypes(object value, params object[] validationTypes)
    {
        return TryValidateByTypes(value, ValidationPattern.AllOfThem, validationTypes);
    }

    /// <summary>
    /// 使用内置或自定义验证类型验证单个值。
    /// </summary>
    /// <param name="value">待验证值。</param>
    /// <param name="validationPattern">多规则组合方式。</param>
    /// <param name="validationTypes">验证规则枚举值。</param>
    /// <returns>验证结果。</returns>
    public static DataValidationResult TryValidateByTypes(object value, ValidationPattern validationPattern, params object[] validationTypes)
    {
        if (value == null)
        {
            return CreateResult(false, value, new[] { new ValidationResult("The value is required") });
        }

        var results = new List<ValidationResult>();
        var rules = validationTypes ?? Array.Empty<object>();
        var isValid = ValidateByTypeRules(value, validationPattern, rules, results);

        return CreateResult(isValid, value, results);
    }

    /// <summary>
    /// 使用正则表达式验证单个值，并返回完整验证结果。
    /// </summary>
    /// <param name="value">待验证值。</param>
    /// <param name="regexPattern">正则表达式。</param>
    /// <param name="regexOptions">正则表达式选项。</param>
    /// <returns>验证结果。</returns>
    public static DataValidationResult TryMatchPattern(object value, string regexPattern, RegexOptions regexOptions = RegexOptions.None)
    {
        var isMatch = IsMatchPattern(value, regexPattern, regexOptions);

        return CreateResult(
            isMatch,
            value,
            isMatch ? Array.Empty<ValidationResult>() : new[] { new ValidationResult("The value does not match the specified pattern") });
    }

    /// <summary>
    /// 使用正则表达式验证单个值，并返回布尔结果。
    /// </summary>
    /// <param name="value">待验证值。</param>
    /// <param name="regexPattern">正则表达式。</param>
    /// <param name="regexOptions">正则表达式选项。</param>
    /// <returns>是否匹配。</returns>
    public static bool IsMatchPattern(object value, string regexPattern, RegexOptions regexOptions = RegexOptions.None)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(regexPattern);

        return value != null && Regex.IsMatch(value.ToString()!, regexPattern, regexOptions);
    }

    /// <summary>
    /// 判断对象是否应跳过模型验证。
    /// </summary>
    /// <param name="obj">对象实例。</param>
    /// <returns>是否跳过。</returns>
    private static bool ShouldSkipObjectValidation(object obj)
    {
        return obj.GetType().IsDefined(typeof(NonValidationAttribute), true);
    }

    /// <summary>
    /// 执行验证类型规则。
    /// </summary>
    /// <param name="value">待验证值。</param>
    /// <param name="validationPattern">多规则组合方式。</param>
    /// <param name="validationTypes">验证规则集合。</param>
    /// <param name="results">验证失败结果集合。</param>
    /// <returns>是否验证成功。</returns>
    private static bool ValidateByTypeRules(
        object value,
        ValidationPattern validationPattern,
        IEnumerable<object> validationTypes,
        ICollection<ValidationResult> results)
    {
        var isValid = true;

        foreach (var validationType in validationTypes)
        {
            var (validationName, validationItemMetadata) = GetValidationItemMetadata(validationType);
            var rulePassed = IsMatchPattern(value, validationItemMetadata.RegularExpression, validationItemMetadata.RegexOptions);

            if (validationPattern == ValidationPattern.AtLeastOne && rulePassed)
            {
                results.Clear();
                return true;
            }

            if (rulePassed)
            {
                continue;
            }

            isValid = false;
            results.Add(CreateValidationFailure(value, validationName, validationItemMetadata));
        }

        return isValid;
    }

    /// <summary>
    /// 创建单条验证失败消息。
    /// </summary>
    /// <param name="value">待验证值。</param>
    /// <param name="validationName">验证规则名称。</param>
    /// <param name="metadata">验证规则元数据。</param>
    /// <returns>验证失败结果。</returns>
    private static ValidationResult CreateValidationFailure(
        object value,
        string validationName,
        ValidationItemMetadataAttribute metadata)
    {
        return new ValidationResult(string.Format(metadata.DefaultErrorMessage, value, validationName));
    }

    /// <summary>
    /// 获取验证规则元数据。
    /// </summary>
    /// <param name="validationType">验证规则枚举值。</param>
    /// <returns>规则名称和元数据。</returns>
    private static (string ValidationName, ValidationItemMetadataAttribute ValidationItemMetadata) GetValidationItemMetadata(object validationType)
    {
        ArgumentNullException.ThrowIfNull(validationType);

        return ValidationItemMetadataCache.GetOrAdd(validationType, ResolveValidationItemMetadata);
    }

    /// <summary>
    /// 解析验证规则元数据。
    /// </summary>
    /// <param name="validationType">验证规则枚举值。</param>
    /// <returns>规则名称和元数据。</returns>
    private static (string, ValidationItemMetadataAttribute) ResolveValidationItemMetadata(object validationType)
    {
        var type = validationType.GetType();

        if (!ValidationTypeDefinitions.Any(u => u == type))
        {
            throw new InvalidOperationException($"{type.Name} is not a valid validation type.");
        }

        var validationName = Enum.GetName(type, validationType);
        if (string.IsNullOrWhiteSpace(validationName))
        {
            throw new InvalidOperationException($"{validationType} is not a valid validation enum value.");
        }

        if (!ValidationItemMetadatas.TryGetValue(validationName, out var validationItemMetadata))
        {
            throw new InvalidOperationException($"No {validationName} validation type metadata exists.");
        }

        return (validationName, validationItemMetadata);
    }

    /// <summary>
    /// 创建验证结果对象。
    /// </summary>
    /// <param name="isValid">是否验证成功。</param>
    /// <param name="memberOrValue">成员名或原始值。</param>
    /// <param name="validationResults">验证失败结果。</param>
    /// <returns>验证结果。</returns>
    private static DataValidationResult CreateResult(
        bool isValid,
        object memberOrValue,
        IEnumerable<ValidationResult> validationResults = null)
    {
        return new DataValidationResult
        {
            Passed = isValid,
            ValidationResults = validationResults?.ToArray() ?? Array.Empty<ValidationResult>(),
            MemberOrValue = memberOrValue
        };
    }

    /// <summary>
    /// 获取所有验证类型定义。
    /// </summary>
    /// <returns>验证类型集合。</returns>
    private static IEnumerable<Type> GetValidationTypeDefinitions()
    {
        return (AppCore.CrucialTypes ?? Array.Empty<Type>())
            .Append(typeof(ValidationTypes))
            .Where(u => u.IsDefined(typeof(ValidationTypeAttribute), true) && u.IsEnum)
            .Distinct();
    }

    /// <summary>
    /// 获取所有验证消息类型定义。
    /// </summary>
    /// <returns>验证消息类型集合。</returns>
    private static IEnumerable<Type> GetValidationMessageTypeDefinitions()
    {
        var validationMessageTypes = (AppCore.EffectiveTypes ?? Array.Empty<Type>())
            .Where(u => u.IsDefined(typeof(ValidationMessageTypeAttribute), true) && u.IsEnum);

        var provider = AppCore.GetService<IValidationMessageTypeProvider>(AppCore.RootServices);
        if (provider is { Definitions: not null })
        {
            validationMessageTypes = validationMessageTypes.Concat(provider.Definitions);
        }

        return validationMessageTypes.Distinct();
    }

    /// <summary>
    /// 构建验证项元数据映射。
    /// </summary>
    /// <returns>验证项元数据映射。</returns>
    private static ConcurrentDictionary<string, ValidationItemMetadataAttribute> BuildValidationItemMetadatas()
    {
        var validationItems = new ConcurrentDictionary<string, ValidationItemMetadataAttribute>();
        var customErrorMessages = GetCustomErrorMessages();

        foreach (var field in GetValidationMetadataFields())
        {
            var metadata = ReplaceValidateErrorMessage(field.Name, field, customErrorMessages);
            validationItems.TryAdd(field.Name, metadata);
        }

        return validationItems;
    }

    /// <summary>
    /// 获取验证项字段。
    /// </summary>
    /// <returns>验证项字段集合。</returns>
    private static IEnumerable<FieldInfo> GetValidationMetadataFields()
    {
        return ValidationTypeDefinitions
            .SelectMany(u => u.GetFields())
            .Where(u => u.IsDefined(typeof(ValidationItemMetadataAttribute)));
    }

    /// <summary>
    /// 获取自定义验证失败消息。
    /// </summary>
    /// <returns>验证项名称与失败消息映射。</returns>
    private static Dictionary<string, string> GetCustomErrorMessages()
    {
        var attributeMessages = GetAttributeErrorMessages();
        var settingsMessages = GetSettingsErrorMessages();

        return attributeMessages.Connect(settingsMessages);
    }

    /// <summary>
    /// 获取验证消息枚举上的失败消息。
    /// </summary>
    /// <returns>验证项名称与失败消息映射。</returns>
    private static Dictionary<string, string> GetAttributeErrorMessages()
    {
        return ValidationMessageTypeDefinitions
            .SelectMany(u => u.GetFields())
            .Where(u => u.IsDefined(typeof(ValidationMessageAttribute)))
            .GroupBy(u => u.Name)
            .ToDictionary(
                u => u.Key,
                u => u.First().GetCustomAttribute<ValidationMessageAttribute>()!.ErrorMessage);
    }

    /// <summary>
    /// 获取配置文件中的失败消息。
    /// </summary>
    /// <returns>验证项名称与失败消息映射。</returns>
    private static Dictionary<string, string> GetSettingsErrorMessages()
    {
        var settings = AppConfiguration.GetConfig<ValidationTypeMessageSettingsOptions>("ValidationTypeMessageSettings", true);
        if (settings is not { Definitions: not null })
        {
            return new Dictionary<string, string>();
        }

        return settings.Definitions
            .Where(u => u.Length > 1)
            .GroupBy(u => u[0].ToString())
            .ToDictionary(u => u.Key, u => u.First()[1].ToString());
    }

    /// <summary>
    /// 替换默认验证失败消息。
    /// </summary>
    /// <param name="name">验证项名称。</param>
    /// <param name="field">验证项字段。</param>
    /// <param name="customErrorMessages">自定义错误消息。</param>
    /// <returns>验证项元数据。</returns>
    private static ValidationItemMetadataAttribute ReplaceValidateErrorMessage(
        string name,
        FieldInfo field,
        IReadOnlyDictionary<string, string> customErrorMessages)
    {
        var metadata = field.GetCustomAttribute<ValidationItemMetadataAttribute>()!;
        if (customErrorMessages.TryGetValue(name, out var errorMessage))
        {
            metadata.DefaultErrorMessage = errorMessage;
        }

        return metadata;
    }
}
