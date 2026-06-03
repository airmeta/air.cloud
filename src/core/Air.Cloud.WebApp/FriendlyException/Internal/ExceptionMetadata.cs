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
namespace Air.Cloud.WebApp.FriendlyException.Internal;

/// <summary>
/// 统一异常返回元数据。
/// </summary>
[IgnoreScanning]
public sealed class ExceptionMetadata
{
    /// <summary>
    /// HTTP 状态码。
    /// </summary>
    public int StatusCode { get; internal set; }

    /// <summary>
    /// 业务错误码。
    /// </summary>
    public object ErrorCode { get; internal set; }

    /// <summary>
    /// 错误消息或错误对象。
    /// </summary>
    public object Errors { get; internal set; }
}
