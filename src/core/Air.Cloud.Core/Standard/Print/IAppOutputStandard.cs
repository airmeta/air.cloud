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
using System.Data;
using Air.Cloud.Core.Modules.AppPrint;

namespace Air.Cloud.Core.Standard.Print
{
    /// <summary>
    /// 应用程序输出标准
    /// </summary>
    public interface IAppOutputStandard : IStandard
    {
        /// <summary>
        /// 输出对象
        /// </summary>
        /// <param name="title">输出对象说明</param>
        /// <param name="content">对象内容</param>
        /// <param name="level">输出等级</param>
        /// <param name="type">输出类别</param>
        /// <param name="state">显式输出</param>
        /// <param name="AdditionalParams">附加参数</param>
        public void Print(string title, string content, AppPrintLevel level = AppPrintLevel.Information, string type = "default", bool state = true, Dictionary<string, object> AdditionalParams = null);
        /// <summary>
        /// 输出对象
        /// </summary>
        /// <param name="Content">打印内容</param>
        public void Print(AppPrintInformation Content);
        /// <summary>
        /// 输出对象
        /// </summary>
        /// <typeparam name="T">内容类型 继承自AppPrintInformation</typeparam>
        /// <param name="Content">打印内容</param>
        public void Print<T>(T Content) where T : AppPrintInformation,new();
        /// <summary>
        /// 输出异常
        /// </summary>
        /// <param name="exception">异常信息</param>
        /// <param name="pairs">附加参数</param>
        public void Error(Exception exception, Dictionary<string, object> pairs = default);
    }
    /// <summary>
    /// 默认的输出标准实现
    /// </summary>
    [IgnoreScanning]
    public class DefaultAppOutputDependency : IAppOutputStandard
    {
        /// <inheritdoc/>
        public void Error(Exception exception,Dictionary<string, object> pairs=default)
        {
            Print(new AppPrintInformation()
            {
                Title = "app-error",
                State = true,
                AdditionalParams = pairs,
                Content = exception.Message,
                Level = AppPrintLevel.Error
            });
            throw exception;
        }
        /// <inheritdoc/>
        public void Print(AppPrintInformation Content)
        {
            Console.WriteLine(AppRealization.JSON.Serialize(Content));
        }
        /// <inheritdoc/>
        public void Print<T>(T Content) where T : AppPrintInformation, new()
        {
            Console.WriteLine(AppRealization.JSON.Serialize(Content));
        }

        /// <inheritdoc/>
        public void Print(string title, string content, AppPrintLevel level =AppPrintLevel.Information, string type = "default", bool state = true, Dictionary<string, object> AdditionalParams = null)
        {
            Print(new AppPrintInformation()
            {
                State = state,
                Title = title,
                Content = content,
                Level = level,
                Type = type,
                AdditionalParams = AdditionalParams
            });
        }
    }
}
