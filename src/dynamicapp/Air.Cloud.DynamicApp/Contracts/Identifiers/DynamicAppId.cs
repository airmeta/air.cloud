using System.Text.Json.Serialization;

using Air.Cloud.DynamicApp.Contracts.Serialization;

namespace Air.Cloud.DynamicApp.Contracts.Identifiers;

/// <summary>
/// <para>zh-cn:DynamicApp 标识值对象，用于隔离不同应用域的清单、状态、路由和资源。</para>
/// <para>en-us:DynamicApp identifier value object used to isolate manifests, state, routes, and resources across application domains.</para>
/// </summary>
[JsonConverter(typeof(DynamicAppIdJsonConverter))]
public readonly record struct DynamicAppId
{
    /// <summary>
    /// <para>zh-cn:创建 DynamicApp 标识，并校验空值、长度和字符范围。</para>
    /// <para>en-us:Creates a DynamicApp identifier and validates emptiness, length, and allowed characters.</para>
    /// </summary>
    /// <param name="value">
    /// <para>zh-cn:原始标识；允许小写字母、数字、短横线和下划线，长度为 1 到 64。</para>
    /// <para>en-us:Raw identifier; lowercase letters, digits, hyphens, and underscores are allowed, with length from 1 to 64.</para>
    /// </param>
    public DynamicAppId(string value) => Value = Normalize(value, nameof(value));

    /// <summary>
    /// <para>zh-cn:规范化后的标识值。</para>
    /// <para>en-us:Normalized identifier value.</para>
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// <para>zh-cn:返回规范化后的标识字符串。</para>
    /// <para>en-us:Returns the normalized identifier string.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:规范化标识。</para>
    /// <para>en-us:The normalized identifier.</para>
    /// </returns>
    public override string ToString() => Value;

    /// <summary>
    /// <para>zh-cn:隐式返回规范化标识。</para>
    /// <para>en-us:Implicitly returns the normalized identifier.</para>
    /// </summary>
    /// <param name="id">
    /// <para>zh-cn:要转换的标识。</para>
    /// <para>en-us:The identifier to convert.</para>
    /// </param>
    public static implicit operator string(DynamicAppId id) => id.Value;

    internal static string Normalize(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Identifier cannot be empty.", parameterName);
        }

        var normalized = value.Trim().ToLowerInvariant();
        if (normalized.Length > 64)
        {
            throw new ArgumentException("Identifier cannot be longer than 64 characters.", parameterName);
        }

        foreach (var character in normalized)
        {
            var allowed = character is >= 'a' and <= 'z'
                || character is >= '0' and <= '9'
                || character is '-' or '_';
            if (!allowed)
            {
                throw new ArgumentException("Identifier can only contain lowercase letters, digits, hyphens, and underscores.", parameterName);
            }
        }

        return normalized;
    }
}
