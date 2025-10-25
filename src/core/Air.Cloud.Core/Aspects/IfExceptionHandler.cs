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

using Air.Cloud.Core.Modules.AppAspect.Handler;
using Air.Cloud.Core.Standard.TraceLog.Defaults;

using System.Reflection;

namespace Air.Cloud.Core.Aspects
{
    /// <summary>
    /// <para>zh-cn:如果出现异常则进行日志记录</para>
    /// <para>en-us:If an exception occurs, log it</para>
    /// </summary>
    public class IfExceptionHandler : IAspectAroundHandler
    {
        /// <inheritdoc/>
        public object Around_After(MethodInfo methodInfo, object[] args, object result)
        {
            return result;
        }
        /// <inheritdoc/>
        public object[] Around_Before(MethodInfo methodInfo, object[] args)
        {
            return args;
        }
        /// <inheritdoc/>
        public void Around_Error<TException>(MethodInfo methodInfo, object[] args, TException exception) where TException : Exception, new()
        {
            DefaultTraceLogContent appPrintInformation = new DefaultTraceLogContent(
               "Exception Aspect",
                $"在执行[{methodInfo.DeclaringType}]的方法[{methodInfo.Name}]时出现{exception.GetType().Name}异常",
               new Dictionary<string, object>()
               {
                     {"source",exception.Source },
                     {"stace",exception.StackTrace }
               }, DefaultTraceLogContent.EVENT_TAG, DefaultTraceLogContent.ERROR_TAG);
            AppRealization.TraceLog.Write(appPrintInformation);
        }
    }
}
