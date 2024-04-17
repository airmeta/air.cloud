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
using Newtonsoft.Json;

using System;
using System.Diagnostics;
namespace Air.Cloud.Core.Standard.Print
{
    /// <summary>
    /// 程序输出标准
    /// </summary>
    public interface IAppOutputStandard : IStandard
    {
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
    public class AppPrintInformation
    {
        public enum AppPrintLevel
        {
            Information,
            Warning,
            Error,
            Debug,
            Trace
        }
        /// <summary>
        /// 标题
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// 类型信息: Information,Warning,Error 
        /// </summary>
        public AppPrintLevel Level { get; set; }
        /// <summary>
        /// 内容
        /// </summary>
        public string Content { get; set; }
        /// <summary>
        /// 状态
        /// </summary>
        public bool State { get; set; }

        /// <summary>
        /// zh-cn: 附加参数
        /// en-us: Additional parameters
        /// </summary>
        public Dictionary<string,object> AdditionalParams { get; set; }
    }
    /// <summary>
    /// 默认的输出标准实现
    /// </summary>
    [IgnoreScanning]
    public class DefaultAppOutputDependency : IAppOutputStandard
    {

        public DefaultAppOutputDependency() { }

        public void Error(Exception exception,Dictionary<string, object> pairs=default)
        {
            Console.WriteLine(JsonConvert.SerializeObject(new AppPrintInformation()
            {
                Title = "Error",
                State = true,
                AdditionalParams = pairs,
                Content = exception.Message,
                Level = AppPrintInformation.AppPrintLevel.Error
            }));
            throw exception;
        }
        
        public void Print(AppPrintInformation Content)
        {
            Console.WriteLine(JsonConvert.SerializeObject(Content));
        }

        public void Print<T>(T Content) where T : AppPrintInformation, new()
        {
            Console.WriteLine(JsonConvert.SerializeObject(Content));
        }
    }
}
