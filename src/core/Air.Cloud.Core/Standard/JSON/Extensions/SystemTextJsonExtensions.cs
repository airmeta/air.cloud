/*
 * Copyright (c) 2024 星曳数据
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * This file is provided under the Mozilla Public License Version 2.0,
 * and the "NO WARRANTY" clause of the MPL is hereby expressly
 * acknowledged.
 */
using Air.Cloud.Core.Standard.JSON.Converters;

using System.Text.Json.Serialization;

namespace Air.Cloud.Core.Standard.JSON.Extensions;

/// <summary>
/// System.Text.Json 拓展
/// </summary>
[IgnoreScanning]
public static class SystemTextJsonExtensions
{
    /// <summary>
    /// 添加时间格式化
    /// </summary>
    /// <param name="converters"></param>
    /// <param name="formatString"></param>
    /// <param name="outputToLocalDateTime">自动转换 DateTimeOffset 为当地时间</param>
    public static void AddDateFormatString(this IList<JsonConverter> converters, string formatString, bool outputToLocalDateTime = false)
    {
        converters.Add(new DateTimeJsonConverter(formatString));
        converters.Add(new DateTimeOffsetJsonConverter(formatString, outputToLocalDateTime));
    }
}