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
namespace Air.Cloud.Core.Modules.AppPrint
{
    /// <summary>
    /// <para>zh-cn:输出打印信息</para>
    /// <para>en-us:Output message content</para>
    /// </summary>
    public class AppPrintInformation
    {


        /// <summary>
        /// 标题
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// 类型信息: Information,Warning,Error 
        /// </summary>
        public AppPrintLevel Level { get; set; } = AppPrintLevel.Information;
        /// <summary>
        /// 内容
        /// </summary>
        public string Content { get; set; }
        /// <summary>
        /// 状态
        /// </summary>
        public bool State { get; set; } = true;
        /// <summary>
        /// <para>zh-cn: 附加参数</para>
        /// <para>en-us: Additional parameters</para>
        /// </summary>
        public Dictionary<string, object> AdditionalParams { get; set; } = null;
        /// <summary>
        /// <para>zh-cn: 类型</para>
        /// <para>en-us: Output content type</para>
        /// </summary>
        public string Type { get; set; } = "default";

        public AppPrintInformation() { }

        /// <summary>
        /// <para>zh-cn:应用程序打印信息构造函数</para>
        /// <para>en-us:Application print information constractor</para>
        /// </summary>
        /// <param name="title"></param>
        /// <param name="content"></param>
        /// <param name="level"></param>
        /// <param name="state"></param>
        /// <param name="additionalParams"></param>
        /// <param name="type"></param>
        public AppPrintInformation(string title, string content, AppPrintLevel level = AppPrintLevel.Information, bool state = true, Dictionary<string, object> additionalParams = null, string type = "default")
        {
            Title = title;
            Level = level;
            Content = content;
            State = state;
            AdditionalParams = additionalParams;
            Type = type;
        }
    }


}
