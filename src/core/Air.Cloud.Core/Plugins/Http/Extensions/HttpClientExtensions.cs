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
using System.Net.Http.Headers;
using System.Text;

namespace Air.Cloud.Core.Plugins.Http.Extensions
{
    /// <summary>
    /// <para>zh-cn:HttpClient 请求头与请求体构造扩展方法。</para>
    /// <para>en-us:HttpClient extension methods for constructing request headers and request bodies.</para>
    /// </summary>
    public static class HttpClientExtensions
    {
        /// <summary>
        /// <para>zh-cn:设置请求头信息</para>
        /// <para>en-us:Set request header</para>
        /// </summary>
        /// <param name="client"></param>
        /// <param name="Headers"></param>
        /// <returns></returns>
        public static HttpClient SetHeaders(this HttpClient client,IDictionary<string,string> Headers)
        {
            if (Headers != null)
            {
                foreach (var item in Headers) client.DefaultRequestHeaders.Add(item.Key, item.Value);
            }
            return client;
        }
        /// <summary>
        /// <para>zh-cn:根据对象内容创建 HTTP 请求体。</para>
        /// <para>en-us:Creates HTTP content from the supplied body object.</para>
        /// </summary>
        /// <param name="client">
        /// <para>zh-cn:当前 HttpClient 实例。</para>
        /// <para>en-us:Current HttpClient instance.</para>
        /// </param>
        /// <param name="Body">
        /// <para>zh-cn:请求体对象；字符串将原样写入，其他对象将序列化为 JSON。</para>
        /// <para>en-us:Request body object; strings are written directly and other objects are serialized as JSON.</para>
        /// </param>
        /// <param name="ContentType">
        /// <para>zh-cn:请求体媒体类型。</para>
        /// <para>en-us:Request body media type.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:可用于请求发送的 HttpContent。</para>
        /// <para>en-us:HttpContent that can be used for sending the request.</para>
        /// </returns>
        public static HttpContent SetBody(this HttpClient client,object Body,string ContentType= "application/json")
        {
            string Data;
            if (Body.GetType() == typeof(string) || Body.GetType() == typeof(string))
            {
                Data = Body.ToString();
            }
            else
            {
                Data = AppRealization.JSON.Serialize(Body);
            }
            HttpContent Content = new StringContent(Data ?? string.Empty, Encoding.UTF8);
            Content.Headers.ContentType = new MediaTypeHeaderValue(ContentType);
            return Content;
        }
        

    }
}
