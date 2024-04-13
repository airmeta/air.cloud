using Air.Cloud.Core.Dependencies;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Air.Cloud.Core.Standard.JSON
{
    /// <summary>
    /// JSON 序列化器标准接口
    /// </summary>
    public  interface IJsonSerializerStandard: ISingleton
    {
        /// <summary>
        /// 序列化对象
        /// </summary>
        /// <param name="value"></param>
        /// <param name="jsonSerializerOptions"></param>
        /// <returns></returns>
        string Serialize(object value, JsonSerializerOptions jsonSerializerOptions = default);

        /// <summary>
        /// 反序列化字符串
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json"></param>
        /// <param name="jsonSerializerOptions"></param>
        /// <returns></returns>
        T Deserialize<T>(string json, JsonSerializerOptions jsonSerializerOptions = default);

        /// <summary>
        /// 返回读取全局配置的 JSON 选项
        /// </summary>
        /// <returns></returns>
        object GetSerializerOptions();
    }
}
