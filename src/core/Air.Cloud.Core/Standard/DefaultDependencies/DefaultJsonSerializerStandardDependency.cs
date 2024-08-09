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

using Newtonsoft.Json;

namespace Air.Cloud.Core.Standard.DefaultDependencies
{
    /// <summary>
    /// <para>zh-cn:默认JSON序列化实现 引用了Newtonsoft.Json</para>
    /// <para>en-us:Default json serializer dependency</para>
    /// </summary>
    public class DefaultJsonSerializerStandardDependency : IJsonSerializerStandard
    {
        /// <summary>
        /// <para>zh-cn:配置项</para>
        /// <para>en-us:JsonSerializerSettings</para>
        /// </summary>
        private static JsonSerializerSettings JsonSerializerSettings;

        /// <summary>
        /// 初始化配置
        /// </summary>
        public DefaultJsonSerializerStandardDependency()
        {
            var s = AppCore.GetOptions<MvcNewtonsoftJsonOptions>();
            JsonSerializerSettings = (s?.SerializerSettings)??new JsonSerializerSettings();
        }
        /// <summary>
        /// 序列化对象
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public string Serialize(object value)
        {
            return JsonConvert.SerializeObject(value,JsonSerializerSettings);
        }

        /// <summary>
        /// 反序列化字符串
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json"></param>
        /// <returns></returns>
        public T Deserialize<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, JsonSerializerSettings);
        }
    }
}
