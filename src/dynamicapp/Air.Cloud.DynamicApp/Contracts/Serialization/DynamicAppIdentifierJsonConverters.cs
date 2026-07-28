using System.Text.Json;
using System.Text.Json.Serialization;

using Air.Cloud.DynamicApp.Contracts.Identifiers;

namespace Air.Cloud.DynamicApp.Contracts.Serialization;

/// <summary>
/// <para>zh-cn:DynamicAppId JSON 转换器，允许外部 API 使用字符串或包含 value 字段的对象表示标识。</para>
/// <para>en-us:JSON converter for DynamicAppId that allows external APIs to represent identifiers as strings or objects containing a value field.</para>
/// </summary>
public sealed class DynamicAppIdJsonConverter : JsonConverter<DynamicAppId>
{
    /// <summary>
    /// <para>zh-cn:从 JSON 字符串或对象读取 DynamicAppId；空值和非法字符由值对象构造函数继续校验。</para>
    /// <para>en-us:Reads DynamicAppId from a JSON string or object; empty values and invalid characters remain validated by the value object constructor.</para>
    /// </summary>
    /// <param name="reader">
    /// <para>zh-cn:JSON 读取器。</para>
    /// <para>en-us:JSON reader.</para>
    /// </param>
    /// <param name="typeToConvert">
    /// <para>zh-cn:目标转换类型。</para>
    /// <para>en-us:Target conversion type.</para>
    /// </param>
    /// <param name="options">
    /// <para>zh-cn:序列化选项。</para>
    /// <para>en-us:Serialization options.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:DynamicAppId 值对象。</para>
    /// <para>en-us:DynamicAppId value object.</para>
    /// </returns>
    public override DynamicAppId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new DynamicAppId(ReadValue(ref reader, options));
    }

    /// <summary>
    /// <para>zh-cn:将 DynamicAppId 写为字符串，保持 API JSON 简洁。</para>
    /// <para>en-us:Writes DynamicAppId as a string to keep API JSON compact.</para>
    /// </summary>
    /// <param name="writer">
    /// <para>zh-cn:JSON 写入器。</para>
    /// <para>en-us:JSON writer.</para>
    /// </param>
    /// <param name="value">
    /// <para>zh-cn:待写入的 DynamicAppId。</para>
    /// <para>en-us:DynamicAppId to write.</para>
    /// </param>
    /// <param name="options">
    /// <para>zh-cn:序列化选项。</para>
    /// <para>en-us:Serialization options.</para>
    /// </param>
    public override void Write(Utf8JsonWriter writer, DynamicAppId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }

    internal static string ReadValue(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return reader.GetString() ?? string.Empty;
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Identifier JSON must be a string or an object with a value field.");
        }

        using var document = JsonDocument.ParseValue(ref reader);
        return document.RootElement.TryGetProperty("value", out var camelValue)
            ? camelValue.GetString() ?? string.Empty
            : document.RootElement.TryGetProperty("Value", out var pascalValue)
                ? pascalValue.GetString() ?? string.Empty
                : throw new JsonException("Identifier JSON object must contain a value field.");
    }
}

/// <summary>
/// <para>zh-cn:DynamicModId JSON 转换器，复用 DynamicAppId 的字符串和 value 对象读取规则。</para>
/// <para>en-us:JSON converter for DynamicModId that reuses DynamicAppId string and value-object reading rules.</para>
/// </summary>
public sealed class DynamicModIdJsonConverter : JsonConverter<DynamicModId>
{
    /// <summary>
    /// <para>zh-cn:从 JSON 字符串或 value 对象读取 DynamicModId。</para>
    /// <para>en-us:Reads DynamicModId from a JSON string or value object.</para>
    /// </summary>
    /// <param name="reader">
    /// <para>zh-cn:JSON 读取器。</para>
    /// <para>en-us:JSON reader.</para>
    /// </param>
    /// <param name="typeToConvert">
    /// <para>zh-cn:目标转换类型。</para>
    /// <para>en-us:Target conversion type.</para>
    /// </param>
    /// <param name="options">
    /// <para>zh-cn:序列化选项。</para>
    /// <para>en-us:Serialization options.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:DynamicModId 值对象。</para>
    /// <para>en-us:DynamicModId value object.</para>
    /// </returns>
    public override DynamicModId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new DynamicModId(DynamicAppIdJsonConverter.ReadValue(ref reader, options));
    }

    /// <summary>
    /// <para>zh-cn:将 DynamicModId 写为字符串。</para>
    /// <para>en-us:Writes DynamicModId as a string.</para>
    /// </summary>
    /// <param name="writer">
    /// <para>zh-cn:JSON 写入器。</para>
    /// <para>en-us:JSON writer.</para>
    /// </param>
    /// <param name="value">
    /// <para>zh-cn:待写入的 DynamicModId。</para>
    /// <para>en-us:DynamicModId to write.</para>
    /// </param>
    /// <param name="options">
    /// <para>zh-cn:序列化选项。</para>
    /// <para>en-us:Serialization options.</para>
    /// </param>
    public override void Write(Utf8JsonWriter writer, DynamicModId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}

/// <summary>
/// <para>zh-cn:DynamicAppVersion JSON 转换器，允许版本使用字符串或包含 value 字段的对象表示。</para>
/// <para>en-us:JSON converter for DynamicAppVersion that allows versions to be represented as strings or objects containing a value field.</para>
/// </summary>
public sealed class DynamicAppVersionJsonConverter : JsonConverter<DynamicAppVersion>
{
    /// <summary>
    /// <para>zh-cn:从 JSON 字符串或 value 对象读取版本值。</para>
    /// <para>en-us:Reads the version value from a JSON string or value object.</para>
    /// </summary>
    /// <param name="reader">
    /// <para>zh-cn:JSON 读取器。</para>
    /// <para>en-us:JSON reader.</para>
    /// </param>
    /// <param name="typeToConvert">
    /// <para>zh-cn:目标转换类型。</para>
    /// <para>en-us:Target conversion type.</para>
    /// </param>
    /// <param name="options">
    /// <para>zh-cn:序列化选项。</para>
    /// <para>en-us:Serialization options.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:DynamicAppVersion 值对象。</para>
    /// <para>en-us:DynamicAppVersion value object.</para>
    /// </returns>
    public override DynamicAppVersion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new DynamicAppVersion(DynamicAppIdJsonConverter.ReadValue(ref reader, options));
    }

    /// <summary>
    /// <para>zh-cn:将 DynamicAppVersion 写为字符串。</para>
    /// <para>en-us:Writes DynamicAppVersion as a string.</para>
    /// </summary>
    /// <param name="writer">
    /// <para>zh-cn:JSON 写入器。</para>
    /// <para>en-us:JSON writer.</para>
    /// </param>
    /// <param name="value">
    /// <para>zh-cn:待写入的 DynamicAppVersion。</para>
    /// <para>en-us:DynamicAppVersion to write.</para>
    /// </param>
    /// <param name="options">
    /// <para>zh-cn:序列化选项。</para>
    /// <para>en-us:Serialization options.</para>
    /// </param>
    public override void Write(Utf8JsonWriter writer, DynamicAppVersion value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}
