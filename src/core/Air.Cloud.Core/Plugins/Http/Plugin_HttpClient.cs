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
using Air.Cloud.Core.Plugins.Http.Enums;
using Air.Cloud.Core.Plugins.Http.Extensions;
using Air.Cloud.Core.Plugins.Http.Provider;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.WebUtilities;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Air.Cloud.Core.Plugins.Http
{
    /// <summary>
    /// Http请求
    /// </summary>
    public static class Plugin_HttpClient
    {

        #region 常量
        /// <summary>
        /// GET 请求
        /// </summary>
        public const string GET = "GET";
        /// <summary>
        /// POST请求
        /// </summary>
        public const string POST = "POST";
        /// <summary>
        /// PUT请求
        /// </summary>
        public const string PUT = "PUT";
        /// <summary>
        /// DELETE请求
        /// </summary>
        public const string DELETE = "DELETE";

        #endregion

        /// <summary>
        /// 发起网络请求
        /// </summary>
        /// <typeparam name="T">返回数据类型</typeparam>
        /// <param name="HttpClientMethodConst">常量</param>
        /// <param name="Url">请求地址</param>
        /// <param name="ObjectData">请求数据</param>
        /// <param name="Headers">请求头</param>
        /// <param name="ContentType">数据类型</param>
        /// <returns></returns>
        public static T Do<T>(string HttpClientMethodConst, string Url, string ObjectData, IDictionary<string, string> Headers = null, HttpContentTypeEnum ContentType = HttpContentTypeEnum.JSON, TimeSpan? Timeout = null) where T : class, new()
        {
            switch (HttpClientMethodConst)
            {
                case GET:
                    var GetData = string.IsNullOrEmpty(ObjectData) ? null : JsonConvert.DeserializeObject<IDictionary<string, string>>(ObjectData);
                    return Get.Do<T>(Url, GetData, Headers, Timeout);
                case POST:
                    JObject objects = JObject.Parse(ObjectData);
                    return Post.Do<T>(Url, objects, Headers, ContentType, Timeout);
                case PUT:
                    JObject PutObject = JObject.Parse(ObjectData);
                    return Put.Do<T>(Url, PutObject, Headers, ContentType, Timeout);
                case DELETE:
                    var DeleteData = string.IsNullOrEmpty(ObjectData) ? null : JsonConvert.DeserializeObject<IDictionary<string, string>>(ObjectData);
                    return Delete.Do<T>(Url, DeleteData, Headers, Timeout);
            }
            return default;
        }
        /// <summary>
        /// POST 请求
        /// </summary>
        public static class Post
        {
            /// <summary>
            /// post 提交json格式参数
            /// </summary>
            /// <param name="Url">请求地址</param>
            /// <param name="ObjectData">请求数据</param>
            /// <param name="Headers">请求头数据</param>
            /// <param name="ContentType">请求数据类型</param>
            /// <returns></returns>
            public static T Do<T>(string Url, object ObjectData, IDictionary<string, string> Headers = null, HttpContentTypeEnum ContentType = HttpContentTypeEnum.JSON, TimeSpan? Timeout = null) where T : class, new()
            {
                var ResultJson = Do(Url, ObjectData, Headers, ContentType, Timeout);
                var ResultObject = JsonConvert.DeserializeObject<RestfulResult<T>>(ResultJson);
                HttpCheck.CheckResult(ResultObject);
                try
                {
                    var ResultData = JsonConvert.DeserializeObject<RestfulResult<T>>(ResultJson);
                    return ResultData?.Data;
                }
                catch (Exception ex)
                {
                    throw new Exception($"调用远程接口之后,解析返回数据出现异常[{ex.Message}]", new Exception("响应数据:" + ResultJson));
                }
            }

            /// <summary>
            /// post 提交json格式参数
            /// </summary>
            /// <param name="Url">请求地址</param>
            /// <param name="ObjectData">请求数据</param>
            /// <param name="Headers">请求头数据</param>
            /// <param name="ContentType">请求数据类型</param>
            /// <returns></returns>
            public static string Do(string Url, object ObjectData, IDictionary<string, string> Headers = null, HttpContentTypeEnum ContentType = HttpContentTypeEnum.JSON, TimeSpan? Timeout = null)
            {
                string Data = string.Empty;
                if (ObjectData.GetType() == typeof(string) || ObjectData.GetType() == typeof(string))
                {
                    Data = ObjectData.ToString();
                }
                else
                {
                    Data = JsonConvert.SerializeObject(ObjectData);
                }
                HttpContent Content = new StringContent(Data ?? string.Empty, Encoding.UTF8);
                Content.Headers.ContentType = new MediaTypeHeaderValue(HttpContentTypes.GetContentType(ContentType));
                var handler = new HttpClientHandler() { AutomaticDecompression = DecompressionMethods.GZip };
                using (var http = new System.Net.Http.HttpClient(handler))
                {
                    if (Headers != null)
                    {
                        foreach (var item in Headers)
                            http.DefaultRequestHeaders.Add(item.Key, item.Value);
                    }
                    if (Url.StartsWith("https", StringComparison.OrdinalIgnoreCase))
                        ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
                    http.Timeout = Timeout == null || !Timeout.HasValue ? new TimeSpan(0, 0, 15) : Timeout.Value;
                    var response = http.PostAsync(Url, Content).Result;
                    //确保HTTP成功状态值
                    response.EnsureSuccessStatusCode();
                    //await异步读取最后的JSON（注意此时gzip已经被自动解压缩了，因为上面的AutomaticDecompression = DecompressionMethods.GZip）
                    var ResultJson = response.Content.ReadAsStringAsync().Result;
                    return ResultJson;
                }
            }
        }

        /// <summary>
        /// POST 请求
        /// </summary>
        public static class Put
        {
            /// <summary>
            /// post 提交json格式参数
            /// </summary>
            /// <param name="Url">请求地址</param>
            /// <param name="ObjectData">请求数据</param>
            /// <param name="Headers">请求头数据</param>
            /// <param name="ContentType">请求数据类型</param>
            /// <returns></returns>
            public static T Do<T>(string Url, object ObjectData, IDictionary<string, string> Headers = null, HttpContentTypeEnum ContentType = HttpContentTypeEnum.JSON, TimeSpan? Timeout = null) where T : class, new()
            {
                var ResultJson = Do(Url, ObjectData, Headers, ContentType, Timeout);
                var ResultObject = JsonConvert.DeserializeObject<RestfulResult<T>>(ResultJson);
                HttpCheck.CheckResult(ResultObject);
                try
                {
                    var ResultData = JsonConvert.DeserializeObject<RestfulResult<T>>(ResultJson);
                    return ResultData.Data;
                }
                catch (Exception ex)
                {
                    throw new Exception($"调用远程接口之后,解析返回数据出现异常[{ex.Message}]", new Exception("响应数据:" + ResultJson));
                }
            }

            /// <summary>
            /// post 提交json格式参数
            /// </summary>
            /// <param name="Url">请求地址</param>
            /// <param name="ObjectData">请求数据</param>
            /// <param name="Headers">请求头数据</param>
            /// <param name="ContentType">请求数据类型</param>
            /// <returns></returns>
            public static string Do(string Url, object ObjectData, IDictionary<string, string> Headers = null, HttpContentTypeEnum ContentType = HttpContentTypeEnum.JSON, TimeSpan? Timeout = null)
            {
                string Data = string.Empty;
                if (ObjectData.GetType() == typeof(string) || ObjectData.GetType() == typeof(string))
                {
                    Data = ObjectData.ToString();
                }
                else
                {
                    Data = JsonConvert.SerializeObject(ObjectData);
                }

                HttpContent Content = new StringContent(Data ?? string.Empty, Encoding.UTF8);
                Content.Headers.ContentType = new MediaTypeHeaderValue(HttpContentTypes.GetContentType(ContentType));

                var handler = new HttpClientHandler() { AutomaticDecompression = DecompressionMethods.GZip };
                using (var http = new System.Net.Http.HttpClient(handler))
                {
                    if (Headers != null)
                    {
                        foreach (var item in Headers)
                            http.DefaultRequestHeaders.Add(item.Key, item.Value);
                    }
                    if (Url.StartsWith("https", StringComparison.OrdinalIgnoreCase))
                        ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
                    http.Timeout = Timeout == null || !Timeout.HasValue ? new TimeSpan(0, 0, 15) : Timeout.Value;
                    var response = http.PutAsync(Url, Content).Result;
                    //确保HTTP成功状态值
                    response.EnsureSuccessStatusCode();
                    //await异步读取最后的JSON（注意此时gzip已经被自动解压缩了，因为上面的AutomaticDecompression = DecompressionMethods.GZip）
                    var ResultJson = response.Content.ReadAsStringAsync().Result;
                    return ResultJson;
                }
            }
        }

        /// <summary>
        /// GET 请求
        /// </summary>
        public static class Get
        {
            /// <summary>
            /// Plugin_HttpClient Configuration 提交
            /// </summary>
            /// <param name="Url">请求地址</param>
            /// <param name="QueryData">请求数据</param>
            /// <param name="Headers">请求头</param>
            /// <returns></returns>
            public static string Do(string Url, IDictionary<string, string> QueryData = null, IDictionary<string, string> Headers = null, TimeSpan? Timeout = null)
            {
                var handler = new HttpClientHandler() { AutomaticDecompression = DecompressionMethods.GZip };
                if (QueryData != null && QueryData.Count > 0)
                {
                    var argStr = QueryData.Aggregate("?", (current, item) => current + item.Key + "=" + item.Value + "&");
                    argStr = argStr.TrimEnd('&');
                    Url = Url + argStr;
                }
                using (var http = new System.Net.Http.HttpClient(handler))
                {
                    if (Headers != null)
                        foreach (var item in Headers)
                            http.DefaultRequestHeaders.Add(item.Key, item.Value);
                    if (Url.StartsWith("https", StringComparison.OrdinalIgnoreCase))
                        ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
                    //await异步等待回应
                    http.Timeout = Timeout == null || !Timeout.HasValue ? new TimeSpan(0, 0, 15) : Timeout.Value;
                    var response = http.GetStringAsync(Url).Result;
                    return response;
                }
            }
            /// <summary>
            /// Plugin_HttpClient Configuration 提交
            /// </summary>
            /// <param name="Url">请求地址</param>
            /// <param name="QueryData">请求数据</param>
            /// <param name="Headers">请求头</param>
            /// <returns></returns>
            public static T Do<T>(string Url, IDictionary<string, string> QueryData = null, IDictionary<string, string> Headers = null, TimeSpan? Timeout = null) where T : class, new()
            {
                var ResultJson = Do(Url, QueryData, Headers, Timeout);
                var ResultObject = JsonConvert.DeserializeObject<RestfulResult<T>>(ResultJson);
                HttpCheck.CheckResult(ResultObject);
                try
                {
                    var ResultData = JsonConvert.DeserializeObject<RestfulResult<T>>(ResultJson);
                    return ResultData?.Data;
                }
                catch (Exception)
                {
                    throw new Exception("调用远程接口之后,解析返回数据出现异常", new Exception("响应数据:" + ResultJson));
                }
            }
        }

        /// <summary>
        /// http delete static class 
        /// </summary>
        public static class Delete
        {
            /// <summary>
            /// Plugin_HttpClient delete  提交
            /// </summary>
            /// <param name="Url">请求地址</param>
            /// <param name="QueryData">请求数据</param>
            /// <param name="Headers">请求头</param>
            /// <returns></returns>
            public static string Do(string Url, IDictionary<string, string> QueryData = null, IDictionary<string, string> Headers = null, TimeSpan? Timeout = null)
            {
                var handler = new HttpClientHandler() { AutomaticDecompression = DecompressionMethods.GZip };
                if (QueryData != null && QueryData.Count > 0)
                {
                    var argStr = QueryData.Aggregate("?", (current, item) => current + item.Key + "=" + item.Value + "&");
                    argStr = argStr.TrimEnd('&');
                    Url = Url + argStr;
                }
                using (var http = new System.Net.Http.HttpClient(handler))
                {
                    if (Headers != null)
                        foreach (var item in Headers)
                            http.DefaultRequestHeaders.Add(item.Key, item.Value);
                    if (Url.StartsWith("https", StringComparison.OrdinalIgnoreCase))
                        ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
                    //await异步等待回应
                    http.Timeout = Timeout == null || !Timeout.HasValue ? new TimeSpan(0, 0, 15) : Timeout.Value;

                    var response = http.DeleteAsync(Url).Result;

                    //确保HTTP成功状态值
                    response.EnsureSuccessStatusCode();
                    //await异步读取最后的JSON（注意此时gzip已经被自动解压缩了，因为上面的AutomaticDecompression = DecompressionMethods.GZip）
                    var ResultJson = response.Content.ReadAsStringAsync().Result;
                    return ResultJson;
                }
            }

            /// <summary>
            /// Plugin_HttpClient delete 提交
            /// </summary>
            /// <param name="Url">请求地址</param>
            /// <param name="QueryData">请求数据</param>
            /// <param name="Headers">请求头</param>
            /// <returns></returns>
            public static T Do<T>(string Url, IDictionary<string, string> QueryData = null, IDictionary<string, string> Headers = null, TimeSpan? Timeout = null) where T : class, new()
            {
                var ResultJson = Do(Url, QueryData, Headers, Timeout);
                var ResultObject = JsonConvert.DeserializeObject<RestfulResult<T>>(ResultJson);
                HttpCheck.CheckResult(ResultObject);
                try
                {
                    var ResultData = JsonConvert.DeserializeObject<RestfulResult<T>>(ResultJson);
                    return ResultData?.Data;
                }
                catch (Exception)
                {
                    throw new Exception("调用远程接口之后,解析返回数据出现异常", new Exception("响应数据:" + ResultJson));
                }
            }

        }
        public static class Download
        {
            public static async Task<byte[]> Do(string Url)
            {
                var handler = new HttpClientHandler() { AutomaticDecompression = DecompressionMethods.GZip };
                using (var http = new System.Net.Http.HttpClient(handler))
                {
                    byte[] fileBytes = await http.GetByteArrayProgressAsync(Url);
                    if (fileBytes.Length == 0)
                    {
                        return new byte[0];
                    }
                    return fileBytes;
                }
            }
        }
        public static class HttpCheck
        {
            public static void CheckResult<T>(RestfulResult<T> ResultObject) where T : class, new()
            {
                string exc = string.Empty;
                if (ResultObject != null && (!ResultObject.Succeeded || ResultObject.Code != 200))
                {
                    switch (ResultObject.Code)
                    {
                        case 401:
                            exc = "当前请求未授权";
                            break;
                        default:
                            exc = ResultObject.Msg == null ? "当前请求出现异常" : ResultObject.Msg.ToString();
                            break;
                    }
                }
                if (!string.IsNullOrEmpty(exc))
                {
                    throw new Exception(exc);
                }
            }
        }
        /// <summary>
        /// 结果检查
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="certificate"></param>
        /// <param name="chain"></param>
        /// <param name="errors"></param>
        /// <returns></returns>
        private static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            return true; //总是接受  
        }

    }


    /// <summary>
    /// 请求处理
    /// </summary>
    public static class HttpClientExtensions
    {
        public static async Task<string> ReadBodyAsync(this HttpRequest request)
        {
            if (request.ContentLength > 0)
            {
                await EnableRewindAsync(request).ConfigureAwait(false);
                var encoding = GetRequestEncoding(request);
                return await ReadStreamAsync(request.Body, encoding).ConfigureAwait(false);
            }
            return null;
        }

        public static Encoding GetRequestEncoding(HttpRequest request)
        {
            var requestContentType = request.ContentType;
            var requestMediaType = new MediaType(requestContentType);
            var requestEncoding = requestMediaType.Encoding ?? Encoding.UTF8;
            return requestEncoding;
        }

        public static async Task EnableRewindAsync(HttpRequest request)
        {
            if (!request.Body.CanSeek)
            {
                request.EnableBuffering();
                await request.Body.DrainAsync(CancellationToken.None);
                request.Body.Seek(0L, SeekOrigin.Begin);
            }
        }

        public static async Task<string> ReadStreamAsync(Stream stream, Encoding encoding)
        {
            using
                StreamReader sr = new StreamReader(stream, encoding, true, 1024, true);
            stream.Seek(0, SeekOrigin.Begin);//内容读取完成后需要将当前位置初始化，否则后面的InputFormatter会无法读取
            var str = await sr.ReadToEndAsync();
            return str;
        }

    }
}
