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
using Air.Cloud.Core;
using Air.Cloud.Core.App;
using Air.Cloud.Core.Modules.AppPrint;
using Air.Cloud.Core.Standard.TraceLog;

using SkyApm.Tracing;
using SkyApm.Tracing.Segments;

using System.ComponentModel;

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
        public void Write(string logContent, IDictionary<string, string> Tag =null)
        {
            _segContext.Context.Span.AddLog(LogEvent.Message(logContent));
            if (Tag.Count>0)
            {
                foreach (var item in Tag)
                {
                    _segContext.Context.Span.AddTag(item.Key, item.Value);
                }
            }
            else
            {
                _segContext.Context.Span.AddTag("event", "event");
            }
        }
        /// <inheritdoc/>
        public void Write<TLog>(TLog logContent, IDictionary<string, string> Tag = null) where TLog : ITraceLogContent, new()
        {
            var logContentString = AppRealization.JSON.Serialize(logContent);
            _segContext.Context.Span.AddLog(LogEvent.Message(logContentString));
            if (Tag.Count > 0)
            {
                foreach (var item in Tag)
                {
                    _segContext.Context.Span.AddTag(item.Key, item.Value);
                }
            }
            else
            {
                _segContext.Context.Span.AddTag("event", "event");
            }
        }

        public void Write(AppPrintInformation logContent, IDictionary<string, string> Tag = null)
        {
            Write(AppRealization.JSON.Serialize(logContent),Tag);
        }
    }
}
