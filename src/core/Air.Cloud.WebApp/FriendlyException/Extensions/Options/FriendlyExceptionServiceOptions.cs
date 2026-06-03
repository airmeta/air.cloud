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


namespace Air.Cloud.WebApp.FriendlyException.Extensions.Options;

/// <summary>
/// 友好异常服务配置选项
/// </summary>
public sealed class FriendlyExceptionServiceOptions
{
    /// <summary>
    /// 是否启用全局友好异常
    /// </summary>
    public bool EnabledGlobalFriendlyException { get; set; } = true;
}
