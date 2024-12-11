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
using Air.Cloud.Core;
using Air.Cloud.Core.App;
using Air.Cloud.Core.Dependencies;
using Air.Cloud.Core.Standard.TraceLog;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using SkyApm.Tracing;
using SkyApm.Tracing.Segments;

namespace Air.Cloud.Modules.SkyWalking.Dependency
{
    /// <summary>
    /// <para>zh-cn:追踪日志标准写入实现</para>
    /// <para>en-us:Trace log standard dependency</para>
    /// </summary>
    public class TraceLogDependency : ITraceLogStandard
    {
        private IEntrySegmentContextAccessor _segContext => AppCore.GetService<IEntrySegmentContextAccessor>();
        
        /// <inheritdoc/>
        public void Write(string logContent,KeyValuePair<string,string>? Tag=null)
        {
            _segContext.Context.Span.AddLog(LogEvent.Message(logContent));
            if (Tag != null&&Tag.HasValue)
            {
                _segContext.Context.Span.AddTag(Tag.Value.Key, Tag.Value.Value);
            }
            else
            {
                _segContext.Context.Span.AddTag("event", "event");
            }
        }
        /// <inheritdoc/>
        public void Write(object logContent, KeyValuePair<string, string>? Tag = null)
        {
            var logContentString = AppRealization.JSON.Serialize(logContent);
            _segContext.Context.Span.AddLog(LogEvent.Message(logContentString));
            if (Tag != null && Tag.HasValue)
            {
                _segContext.Context.Span.AddTag(Tag.Value.Key, Tag.Value.Value);
            }
            else
            {
                _segContext.Context.Span.AddTag("event", "event");
            }
        }

    }
}
