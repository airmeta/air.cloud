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
namespace Air.Cloud.WebApp.DataValidation.Extensions.Options;

/// <summary>
/// 数据验证服务配置选项
/// </summary>
public sealed class DataValidationServiceOptions
{
    /// <summary>
    /// 启用全局数据验证
    /// </summary>
    public bool EnableGlobalDataValidation { get; set; } = true;

    /// <summary>
    /// 禁止C# 8.0 验证非可空引用类型
    /// </summary>
    public bool SuppressImplicitRequiredAttributeForNonNullableReferenceTypes { get; set; } = true;

    /// <summary>
    /// 是否禁用 MVC 模型验证过滤器
    /// </summary>
    /// <remarks>只会改变启用全局验证的情况，也就是 <see cref="EnableGlobalDataValidation"/> 为 true 的情况</remarks>
    public bool SuppressModelStateInvalidFilter { get; set; } = true;
}