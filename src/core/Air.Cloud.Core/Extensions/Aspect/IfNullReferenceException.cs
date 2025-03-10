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

using System.Reflection;

namespace Air.Cloud.Core.Extensions.Aspect
{
    /// <summary>
    /// <para>zh-cn:空指针异常捕捉环绕</para>
    /// <para>en-us:Null pointer exception capture around</para>    
    /// </summary>
    public  class IfNullReferenceException : IAspectAroundHandler
    {
        public object Around_After(MethodInfo methodInfo, object[] args, object result)
        {
            return result;
        }

        public object[] Around_Before(MethodInfo methodInfo, object[] args)
        {
            return args;
        }

        public void Around_Error<TException>(MethodInfo methodInfo, object[] args, TException exception) where TException : Exception, new()
        {
            if (exception is NullReferenceException)
            {
                AppPrintInformation appPrintInformation = new AppPrintInformation(
                    "空指针异常", 
                    $"在执行[{methodInfo.DeclaringType}]的方法[{methodInfo.Name}]时出现空指针异常",
                   AppPrintInformation.AppPrintLevel.Error, 
                   true,
                   new Dictionary<string, object>()
                    {
                        {"source",exception.Source },
                        {"stace",exception.StackTrace }
                    }
                );
                AppRealization.Output.Print(appPrintInformation);
            }
        }
    }
}
