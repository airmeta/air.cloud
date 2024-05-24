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
using System.Net.Http.Headers;
using System.Text;

namespace Air.Cloud.Core.Plugins.Http.Extensions
{
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
