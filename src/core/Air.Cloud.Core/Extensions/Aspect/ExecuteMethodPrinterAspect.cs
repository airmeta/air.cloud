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
using Air.Cloud.Core.Modules.AppPrint;
using System.Reflection;

namespace Air.Cloud.Core.Extensions.Aspect
{
    /// <summary>
    /// <para>zh-cn:方法执行打印切面</para>
    /// </summary>
    public class ExecuteMethodPrinterAspect : IAspectExecuteHandler
    {
        /// <inheritdoc/>
        public void Execute_After(MethodInfo methodInfo,object retValue)
        {
            AppRealization.Output.Print(new AppPrintInformation()
            {
                State = true,
                AdditionalParams = new Dictionary<string, object>()
                {
                    {"return_value", retValue}
                },
                Content = $"Execute method {methodInfo.DeclaringType.FullName}.{methodInfo.Name} Finish",
                Level = AppPrintLevel.Information,
                Title = "After Execute Method",
                Type = "method_log"
            });
        }
        /// <inheritdoc/>
        public void Execute_Before(MethodInfo methodInfo,object[] args)
        {
            AppRealization.Output.Print(new AppPrintInformation()
            {
                State=true,
                AdditionalParams = args.Length>0? new Dictionary<string, object>()
                {
                    {"args", args}
                }:null,
                Content=$"Execute method {methodInfo.DeclaringType.FullName}.{methodInfo.Name} Start",
                Level=AppPrintLevel.Information,
                Title= "Before Execute Method",
                Type="method_log"
            });
        }
    }
}
