// Copyright (c) 2020-2022 百小僧, Baiqian Co.,Ltd.
// Furion is licensed under Mulan PSL v2.
// You can use this software according to the terms and conditions of the Mulan PSL v2.
// You may obtain a copy of Mulan PSL v2 at:
//             https://gitee.com/dotnetchina/Furion/blob/master/LICENSE
// THIS SOFTWARE IS PROVIDED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO NON-INFRINGEMENT, MERCHANTABILITY OR FIT FOR A PARTICULAR PURPOSE.
// See the Mulan PSL v2 for more details.
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Air.Cloud.Core.Plugins.JsonSerialization.Providers;
using Air.Cloud.Core.App;

namespace Air.Cloud.Core.Plugins.JsonSerialization;

/// <summary>
/// JSON 静态帮助类
/// </summary>
[IgnoreScanning]
public static class JSON
{
    /// <summary>
    /// 获取 JSON 序列化提供器
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <returns></returns>
    public static IJsonSerializerProvider GetJsonSerializer(IServiceProvider serviceProvider = default)
    {
        return AppCore.GetService<IJsonSerializerProvider>(serviceProvider ?? AppCore.RootServices);
    }

    /// <summary>
    /// 序列化对象
    /// </summary>
    /// <param name="value"></param>
    /// <param name="jsonSerializerOptions"></param>
    /// <param name="serviceProvider"></param>
    /// <returns></returns>
    public static string Serialize(object value, object jsonSerializerOptions = default, IServiceProvider serviceProvider = default)
    {
        return GetJsonSerializer(serviceProvider).Serialize(value, jsonSerializerOptions);
    }

    /// <summary>
    /// 反序列化字符串
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="json"></param>
    /// <param name="jsonSerializerOptions"></param>
    /// <param name="serviceProvider"></param>
    /// <returns></returns>
    public static T Deserialize<T>(string json, object jsonSerializerOptions = default, IServiceProvider serviceProvider = default)
    {
        return GetJsonSerializer(serviceProvider).Deserialize<T>(json, jsonSerializerOptions);
    }

    /// <summary>
    /// 获取 JSON 配置选项
    /// </summary>
    /// <typeparam name="TOptions"></typeparam>
    /// <param name="serviceProvider"></param>
    /// <returns></returns>
    public static TOptions GetSerializerOptions<TOptions>(IServiceProvider serviceProvider = default)
        where TOptions : class
    {
        return GetJsonSerializer(serviceProvider).GetSerializerOptions() as TOptions;
    }


    /// <summary>
    /// 将JSON文本写入JSON文件
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="obj"></param>
    public static void SaveToJsonFile(string filePath, object obj)
    {
        // 将JSON序列化为字符串
        string json = JsonConvert.SerializeObject(obj);
        string folderPath = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }
        // 将字符串写入文件
        File.WriteAllText(filePath, json);
    }
    /// <summary>
    /// 从JSON文件读取JSON文本
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    public static string LoadFromJsonFile(string filePath)
    {
        // 从文件中读取字符串
        string json = File.ReadAllText(filePath);
        return json;
    }
    /// <summary>
    /// 从JSON文件读取JSON文本并反序列化为对象
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="filePath"></param>
    /// <returns></returns>
    public static T LoadFromJsonFile<T>(string filePath)
    {
        // 从文件中读取字符串
        string json = File.ReadAllText(filePath);
        // 将JSON反序列化为对象
        T obj = JsonConvert.DeserializeObject<T>(json);
        return obj;
    }
    /// <summary>
    /// 修改JSON文件中的某个字段
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="fieldName"></param>
    /// <param name="newValue"></param>
    public static void UpdateJsonFile(string filePath, string fieldName, object newValue)
    {
        // 从文件中读取JSON文本并反序列化为对象
        JObject jsonObj = JObject.Parse(File.ReadAllText(filePath));
        // 修改对象中的字段值
        jsonObj[fieldName] = JToken.FromObject(newValue);
        // 将对象序列化为JSON字符串并写回文件
        string json = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
        File.WriteAllText(filePath, json);
    }
}