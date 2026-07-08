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
using Microsoft.Extensions.Logging;

namespace Air.Cloud.Core.Plugins.LogFiltering;

/// <summary>
/// <para>zh-cn:描述 Air.Cloud 统一日志输出前的过滤上下文，包含日志分类、级别、结构化状态、格式化内容和当前 HTTP 请求信息。</para>
/// <para>en-us:Describes the filtering context before Air.Cloud unified log output, including category, level, structured state, formatted content, and current HTTP request information.</para>
/// </summary>
public sealed class AppLogFilterContext
{
    /// <summary>
    /// <para>zh-cn:Microsoft.Extensions.Logging 日志分类名称。</para>
    /// <para>en-us:The Microsoft.Extensions.Logging category name.</para>
    /// </summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:日志级别。</para>
    /// <para>en-us:The log level.</para>
    /// </summary>
    public LogLevel LogLevel { get; set; }

    /// <summary>
    /// <para>zh-cn:日志事件标识。</para>
    /// <para>en-us:The log event identifier.</para>
    /// </summary>
    public EventId EventId { get; set; }

    /// <summary>
    /// <para>zh-cn:日志结构化状态对象；框架日志通常包含 OriginalFormat 和命名参数。</para>
    /// <para>en-us:The structured log state object; framework logs usually contain OriginalFormat and named parameters.</para>
    /// </summary>
    public object State { get; set; }

    /// <summary>
    /// <para>zh-cn:格式化后的日志内容。</para>
    /// <para>en-us:The formatted log content.</para>
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:当前 HTTP 请求路径；没有请求上下文时为空。</para>
    /// <para>en-us:The current HTTP request path; empty when no request context exists.</para>
    /// </summary>
    public string RequestPath { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:当前 HTTP 请求方法；没有请求上下文时为空。</para>
    /// <para>en-us:The current HTTP request method; empty when no request context exists.</para>
    /// </summary>
    public string RequestMethod { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:当前 HTTP 响应状态码；没有请求上下文或状态码尚未确定时为空。</para>
    /// <para>en-us:The current HTTP response status code; null when no request context exists or the status code is not available yet.</para>
    /// </summary>
    public int? StatusCode { get; set; }
}
