﻿/*
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
using Air.Cloud.Core.Standard.TraceLog;

namespace Air.Cloud.Core.Standard.DefaultDependencies
{
    /// <summary>
    /// <para>zh-cn:默认的日志追踪实现(好吧,其实就是没有实现)</para>
    ///  <para>en-us:default trace log dependency</para>
    /// </summary>
    public class DefaultTraceLogDependency : ITraceLogStandard
    {
        /// <inheritdoc/>
        public void Write(string logContent, KeyValuePair<string, string>? Tag = null)
        {
            AppRealization.Output.Print(new AppPrintInformation()
            {
                Content = logContent,
                Title = "default trace log output events"
            });
        }
        /// <inheritdoc/>
        public void Write(object logContent, KeyValuePair<string, string>? Tag = null)
        {
            AppRealization.Output.Print(new AppPrintInformation()
            {
                Content = AppRealization.JSON.Serialize(logContent),
                Title = "default trace log output events"
            });
        }
    }
}
