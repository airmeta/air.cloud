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
using Air.Cloud.Core.Standard.JSON;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using System.Text.Json;

namespace Air.Cloud.Core.Standard.DefaultDependencies
{
    public class DefaultJsonSerializerStandardDependency : IJsonSerializerStandard
    {
        /// <summary>
        /// 获取 JSON 配置选项
        /// </summary>
        private readonly JsonOptions _jsonOptions;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="options"></param>
        public DefaultJsonSerializerStandardDependency(IOptions<JsonOptions> options)
        {
            _jsonOptions = options.Value;
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="options"></param>
        public DefaultJsonSerializerStandardDependency(JsonOptions options)
        {
            _jsonOptions = options;
        }
        /// <summary>
        /// 序列化对象
        /// </summary>
        /// <param name="value"></param>
        /// <param name="jsonSerializerOptions"></param>
        /// <returns></returns>
        public string Serialize(object value, JsonSerializerOptions jsonSerializerOptions = null)
        {
            return JsonSerializer.Serialize(value, (jsonSerializerOptions ?? GetSerializerOptions()) as JsonSerializerOptions);
        }

        /// <summary>
        /// 反序列化字符串
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json"></param>
        /// <param name="jsonSerializerOptions"></param>
        /// <returns></returns>
        public T Deserialize<T>(string json, JsonSerializerOptions jsonSerializerOptions = null)
        {
            return JsonSerializer.Deserialize<T>(json, (jsonSerializerOptions ?? GetSerializerOptions()) as JsonSerializerOptions);
        }

        /// <summary>
        /// 返回读取全局配置的 JSON 选项
        /// </summary>
        /// <returns></returns>
        public object GetSerializerOptions()
        {
            return _jsonOptions?.JsonSerializerOptions;
        }
    }
}
