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
namespace Air.Cloud.WebApp.DataValidation.Providers;

/// <summary>
/// 验证消息类型提供器
/// </summary>
public interface IValidationMessageTypeProvider
{
    /// <summary>
    /// 验证消息类型定义
    /// </summary>
    Type[] Definitions { get; }
}