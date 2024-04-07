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
namespace Air.Cloud.Core.Standard
{
    /// <summary>
    /// 程序内打印约定
    /// </summary>
    public interface IAppPrintStandard: IStandard
    {
        /// <summary>
        /// 打印对象
        /// </summary>
        /// <remarks>
        ///  你可以自己实现一个对象转字符串的方法，也可以使用默认的实现
        /// </remarks>
        public void Print();

        /// <summary>
        /// 打印对象
        /// </summary>
        /// <param name="Content">打印内容</param>
        public void Print(string Content);

        /// <summary>
        /// 打印对象
        /// </summary>
        /// <param name="Content">打印内容</param>
        public void Print(object Content);

        /// <summary>
        /// 需要打印的内容
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        ///设置打印内容
        /// </summary>
        /// <param name="Content"></param>
        /// <returns></returns>
        public IAppPrintStandard SetContent(string Content);

        /// <summary>
        ///设置打印内容
        /// </summary>
        /// <param name="Content"></param>
        /// <returns></returns>
        public IAppPrintStandard SetContent(object Content);
    }
    /// <summary>
    /// 默认的打印内容实现
    /// </summary>
    [IgnoreScanning]
    public class DefaultAppPrintDependency : IAppPrintStandard
    {
        /// <summary>
        /// 打印内容
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// 构造函数 
        /// </summary>
        /// <remarks>
        /// 允许传入任意对象 会自动序列化为字符串
        /// </remarks>
        /// <param name="Content">任意需要打印的对象</param>
        public DefaultAppPrintDependency(object Content)
        {

            this.Content = JsonConvert.SerializeObject(Content);
        }
        public DefaultAppPrintDependency() { }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="content">打印内容</param>
        public DefaultAppPrintDependency(string content)
        {
            Content = content;
        }

        public void Print()
        {
            Console.WriteLine(Content);
        }

        public IAppPrintStandard SetContent(string Content)
        {
            this.Content = Content;
            return this;
        }

        public IAppPrintStandard SetContent(object Content)
        {
            this.Content = JsonConvert.SerializeObject(Content);
            return this;
        }

        public void Print(string Content)
        {
            this.Content = Content;
            Console.WriteLine(Content);
        }

        public void Print(object Content)
        {
            this.Content = JsonConvert.SerializeObject(Content);
            Console.WriteLine(Content);
        }
    }
}
