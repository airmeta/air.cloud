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
namespace Air.Cloud.WebApp.UnifyResult.Attributes;

/// <summary>
/// 规范化模型特性
/// </summary>
[IgnoreScanning, AttributeUsage(AttributeTargets.Class)]
public class UnifyModelAttribute : Attribute
{
    /// <summary>
    /// 规范化模型
    /// </summary>
    /// <param name="modelType"></param>
    public UnifyModelAttribute(Type modelType)
    {
        ModelType = modelType;
    }

    /// <summary>
    /// 模型类型（泛型）
    /// </summary>
    public Type ModelType { get; set; }
}