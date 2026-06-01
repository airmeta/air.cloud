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
namespace Air.Cloud.Core.Standard.AppResult
{
    /// <summary>
    /// <para>zh-cn:定义带数据负载的 REST 风格统一结果标准。</para>
    /// <para>en-us:Defines the REST-style unified result contract with a data payload.</para>
    /// </summary>
    /// <typeparam name="T">
    /// <para>zh-cn:结果数据类型。</para>
    /// <para>en-us:The result data type.</para>
    /// </typeparam>
    public interface IRESTfulResultStandard<T> : IStandard
    {
        /// <summary>
        /// <para>zh-cn:获取或设置业务状态码。</para>
        /// <para>en-us:Gets or sets the business status code.</para>
        /// </summary>
        public int? Code { get; set; }

        /// <summary>
        /// <para>zh-cn:获取或设置结果数据。</para>
        /// <para>en-us:Gets or sets the result data.</para>
        /// </summary>
        public T Data { get; set; }

        /// <summary>
        /// <para>zh-cn:获取或设置请求是否执行成功。</para>
        /// <para>en-us:Gets or sets whether the request executed successfully.</para>
        /// </summary>
        public bool Succeeded { get; set; }

        /// <summary>
        /// <para>zh-cn:获取或设置错误信息。</para>
        /// <para>en-us:Gets or sets error information.</para>
        /// </summary>
        public object Errors { get; set; }

        /// <summary>
        /// <para>zh-cn:获取或设置附加数据。</para>
        /// <para>en-us:Gets or sets additional data.</para>
        /// </summary>
        public object Extras { get; set; }

        /// <summary>
        /// <para>zh-cn:获取或设置结果生成时间戳。</para>
        /// <para>en-us:Gets or sets the result generation timestamp.</para>
        /// </summary>
        public long Timestamp { get; set; }

        /// <summary>
        /// <para>zh-cn:获取或设置提示消息。</para>
        /// <para>en-us:Gets or sets the prompt message.</para>
        /// </summary>
        public string Message { get; set; }

    }
    /// <summary>
    /// <para>zh-cn:定义不带数据负载的 REST 风格统一结果标准。</para>
    /// <para>en-us:Defines the REST-style unified result contract without a data payload.</para>
    /// </summary>
    public interface IRESTfulResultStandard : IStandard
    {
        /// <summary>
        /// 状态码
        /// </summary>
        public int? Code { get; set; }

        /// <summary>
        /// 执行成功
        /// </summary>
        public bool Succeeded { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public object Errors { get; set; }

        /// <summary>
        /// 附加数据
        /// </summary>
        public object Extras { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        public long Timestamp { get; set; }

    }
}
