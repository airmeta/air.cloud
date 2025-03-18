
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
using Air.Cloud.Core.Standard.TraceLog;

using static Air.Cloud.Core.Standard.Print.AppPrintInformation;

namespace Air.Cloud.Core.Standard.TraceLog.Defaults
{
    /// <summary>
    /// <para>zh-cn:默认的日志追踪实现(好吧,其实就是没有实现)</para>
    ///  <para>en-us:default trace log dependency</para>
    /// </summary>
    public class DefaultTraceLogDependency : ITraceLogStandard
    {
        /// <inheritdoc/>
        public void Write(string logContent, IDictionary<string, string> Tag = null)
        {
            AppRealization.Output.Print(new AppPrintInformation()
            {
                Content = logContent,
                Title = "default trace log output events"
            });
        }
        /// <inheritdoc/>
        public void Write<TLog>(TLog logContent, IDictionary<string, string> Tag = null) where TLog : ITraceLogContent, new()
        {
            if (logContent is AppPrintInformation)
            {
                AppRealization.Output.Print(logContent as AppPrintInformation);
                return;
            }
            AppRealization.Output.Print(new AppPrintInformation()
            {
                Content = AppRealization.JSON.Serialize(logContent),
                Title = "default trace log output events"
            });
        }

       
    }
}
