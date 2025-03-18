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
namespace Air.Cloud.Core.Standard.TraceLog.Defaults
{
    /// <summary>
    /// <para>zh-cn:默认跟踪日志内容</para>
    /// <para>en-us:Default trace log content</para>
    /// </summary>
    public class DefaultTraceLogContent : ITraceLogContent
    {
        /// <summary>
        /// <para>zh-cn:异常标签</para>
        /// <para>en-us:Error tag</para>
        /// </summary>
        public const string ERROR_TAG = "error";
        /// <summary>
        /// <para>zh-cn:日志标签</para>
        /// <para>en-us:Log tag</para>
        /// </summary>
        public const string LOG_TAG= "default";
        /// <summary>
        /// <para>zh-cn:事件标签</para>
        /// <para>en-us:Event tag</para>
        /// </summary>
        public const string EVENT_TAG= "event";
        /// <inheritdoc/>
        public string Id { get; set; } = AppCore.Guid();
        /// <summary>
        ///  标签
        /// </summary>
        public string Tags { get; set; } = LOG_TAG;
        /// <summary>
        /// 标题
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// 内容
        /// </summary>
        public string Content { get; set; }
        /// <summary>
        /// <para>zh-cn: 附加参数</para>
        /// <para>en-us: Additional parameters</para>
        /// </summary>
        public Dictionary<string, object> AdditionalParams { get; set; } = null;

        /// <summary>
        ///     <para>zh-cn:默认跟踪日志内容</para>
        ///     <para>en-us:Default trace log content</para>
        /// </summary>
        /// <param name="title">
        ///     <para>zh-cn:标题</para>
        ///     <para>en-us:Title</para>
        /// </param>
        /// <param name="content">
        ///     <para>zh-cn:内容</para>
        ///     <para>en-us:Content</para>
        /// </param>
        /// <param name="additionalParams">
        ///     <para>zh-cn:附加参数</para>
        ///     <para>en-us:Additional parameters</para>
        /// </param>
        /// <param name="tags">
        ///     <para>zh-cn:标签</para>
        ///     <para>en-us:Tags</para>
        /// </param>
        public DefaultTraceLogContent(string title, string content, Dictionary<string, object> additionalParams = null,params string[] tags)
        {
            Title = title;
            Content = content;
            AdditionalParams = additionalParams;
            Tags = string.Join(",",tags);
        }
        /// <summary>
        /// <para>zh-cn:默认跟踪日志内容</para>
        /// <para>en-us:Trace log constractor</para>
        /// </summary>
        public DefaultTraceLogContent() { }
    }
}
