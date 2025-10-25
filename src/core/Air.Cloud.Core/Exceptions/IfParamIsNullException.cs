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

namespace Air.Cloud.Core.Exceptions
{
    /// <summary>
    /// <para>zh-cn:参数为空异常</para>
    /// <para>en-us:Parameter is null exception</para>
    /// </summary>
    public class IfParamIsNullOrEmptyException: Exception
    {
        /// <summary>
        /// <para>zh-cn:参数名</para>
        /// <para>en-us:Parameter name</para>
        /// </summary>
        public string  ParamName { get; set; }
        /// <summary>
        /// <para>zh-cn:构造一个参数为空异常</para>
        /// <para>en-us:Construct a parameter is null exception</para>
        /// </summary>
        /// <param name="ParamName"></param>
        public IfParamIsNullOrEmptyException(string ParamName):base($"参数[{ParamName}]不能为空或空字符串")
        {
            this.ParamName = ParamName;
        }
    }
    /// <summary>
    /// <para>zh-cn:参数为空或者空字符串拦截</para>
    /// <para>en-us:Parameter is null or empty interception</para>
    /// </summary>
    public class IfParamIsNullOrEmptyExceptionHandler : IAspectAroundHandler
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
            if (exception is IfParamIsNullOrEmptyException)
            {
                DefaultTraceLogContent appPrintInformation = new DefaultTraceLogContent(
                "检查参数为Null或空值",
                 $"在执行[{methodInfo.DeclaringType}]的方法[{methodInfo.Name}]时捕捉到参数为Null或空值,"+exception.Message,
                new Dictionary<string, object>()
                {
                     {"message",exception.Message},
                     {"source",exception.Source },
                     {"stace",exception.StackTrace }
                }, DefaultTraceLogContent.EVENT_TAG, DefaultTraceLogContent.ERROR_TAG);
                AppRealization.TraceLog.Write(appPrintInformation);
                AppRealization.Output.Print("检查参数为Null或空值", exception.Message,AppPrintLevel.Error);
            }
        }
    }

}
