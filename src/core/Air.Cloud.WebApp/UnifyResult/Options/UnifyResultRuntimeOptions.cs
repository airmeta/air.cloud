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

using Air.Cloud.WebApp.UnifyResult.Internal;

namespace Air.Cloud.WebApp.UnifyResult.Options;

/// <summary>
/// 统一返回运行时配置。
/// </summary>
public sealed class UnifyResultRuntimeOptions
{
    /// <summary>
    /// 是否启用统一返回。
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// 统一返回模型泛型类型。
    /// </summary>
    public Type ResultModelType { get; set; } = typeof(RESTfulResult<>);
}
