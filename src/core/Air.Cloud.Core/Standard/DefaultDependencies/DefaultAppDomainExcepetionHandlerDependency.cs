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
using Air.Cloud.Core.Standard;
using Air.Cloud.Core.Standard.Exceptions;

namespace Air.Cloud.Core.Standard.DefaultDependencies
{
    [IgnoreScanning]
    public class DefaultAppDomainExcepetionHandlerDependency : IAppDomainExceptionHandlerStandard
    {
        /// <summary>
        /// 监听全局异常
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = (Exception)e.ExceptionObject;
            if (exception is IFriendlyExceptionStandard) return;
            AppRealization.Print.Print(new
            {
                Title = "domain-errors",
                Type = "Information",
                Content = exception.Message,
                State = true
            });
        }
    }
}
