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
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Air.Cloud.Core.Standard.JSON.Converters
{
    /// <summary>
    /// <para>zh-cn:提供异常类型的 JSON 序列化转换器，仅支持将异常对象写出为属性对象，不支持反序列化异常。</para>
    /// <para>en-us:Provides a JSON converter for exception types, supporting only writing exception objects as property objects and not supporting exception deserialization.</para>
    /// </summary>
    /// <typeparam name="TExceptionType">
    /// <para>zh-cn:需要序列化的异常类型。</para>
    /// <para>en-us:The exception type to serialize.</para>
    /// </typeparam>
    public class ExceptionJsonConverter<TExceptionType> : JsonConverter<TExceptionType> where TExceptionType : Exception
    {
        /// <summary>
        /// <para>zh-cn:判断指定类型是否可以由当前转换器处理。</para>
        /// <para>en-us:Determines whether the specified type can be handled by the current converter.</para>
        /// </summary>
        /// <param name="typeToConvert">
        /// <para>zh-cn:需要判断的类型。</para>
        /// <para>en-us:The type to check.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:如果类型可赋值给 `Exception` 则返回 `true`；否则返回 `false`。</para>
        /// <para>en-us:Returns `true` when the type is assignable to `Exception`; otherwise returns `false`.</para>
        /// </returns>
        public override bool CanConvert(Type typeToConvert)
        {
            return typeof(Exception).IsAssignableFrom(typeToConvert);
        }

        /// <summary>
        /// <para>zh-cn:读取异常 JSON 值。出于安全和兼容性考虑，当前转换器不允许反序列化异常。</para>
        /// <para>en-us:Reads an exception JSON value. For security and compatibility reasons, this converter does not allow exception deserialization.</para>
        /// </summary>
        /// <param name="reader">
        /// <para>zh-cn:JSON 读取器。</para>
        /// <para>en-us:The JSON reader.</para>
        /// </param>
        /// <param name="typeToConvert">
        /// <para>zh-cn:目标异常类型。</para>
        /// <para>en-us:The target exception type.</para>
        /// </param>
        /// <param name="options">
        /// <para>zh-cn:JSON 序列化选项。</para>
        /// <para>en-us:The JSON serializer options.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:该方法不会返回值，始终抛出不支持异常。</para>
        /// <para>en-us:This method does not return a value and always throws a not-supported exception.</para>
        /// </returns>
        public override TExceptionType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotSupportedException("Deserializing exceptions is not allowed");
        }

        /// <summary>
        /// <para>zh-cn:将异常对象的可序列化属性写入 JSON 对象，并跳过 `TargetSite` 属性。</para>
        /// <para>en-us:Writes serializable properties of an exception object to a JSON object and skips the `TargetSite` property.</para>
        /// </summary>
        /// <param name="writer">
        /// <para>zh-cn:JSON 写入器。</para>
        /// <para>en-us:The JSON writer.</para>
        /// </param>
        /// <param name="value">
        /// <para>zh-cn:需要写出的异常对象。</para>
        /// <para>en-us:The exception object to write.</para>
        /// </param>
        /// <param name="options">
        /// <para>zh-cn:JSON 序列化选项；当配置为忽略空值时会跳过空属性。</para>
        /// <para>en-us:The JSON serializer options; when configured to ignore null values, null properties are skipped.</para>
        /// </param>
        public override void Write(Utf8JsonWriter writer, TExceptionType value, JsonSerializerOptions options)
        {
            var serializableProperties = value.GetType()
                .GetProperties()
                .Select(uu => new { uu.Name, Value = uu.GetValue(value) })
                .Where(uu => uu.Name != nameof(Exception.TargetSite));

            if (options?.DefaultIgnoreCondition == System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)
            {
                serializableProperties = serializableProperties.Where(uu => uu.Value != null);
            }

            var propList = serializableProperties.ToList();

            if (propList.Count == 0)
            {
                // Nothing to write
                return;
            }

            writer.WriteStartObject();

            foreach (var prop in propList)
            {
                writer.WritePropertyName(prop.Name);
                JsonSerializer.Serialize(writer, prop.Value, options);
            }

            writer.WriteEndObject();
        }
    }
}
