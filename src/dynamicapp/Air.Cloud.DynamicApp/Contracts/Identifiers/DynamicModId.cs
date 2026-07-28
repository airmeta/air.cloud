using System.Text.Json.Serialization;

using Air.Cloud.DynamicApp.Contracts.Serialization;

namespace Air.Cloud.DynamicApp.Contracts.Identifiers;

/// <summary>
/// <para>zh-cn:Mod 标识值对象，用于在单个 DynamicApp 内定位业务能力。</para>
/// <para>en-us:Mod identifier value object used to locate a business capability within one DynamicApp.</para>
/// </summary>
[JsonConverter(typeof(DynamicModIdJsonConverter))]
public readonly record struct DynamicModId
{
    /// <summary>
    /// <para>zh-cn:创建 Mod 标识，并复用 DynamicApp 标识规则。</para>
    /// <para>en-us:Creates a Mod identifier and reuses DynamicApp identifier rules.</para>
    /// </summary>
    /// <param name="value">
    /// <para>zh-cn:Mod 原始标识。</para>
    /// <para>en-us:The raw Mod identifier.</para>
    /// </param>
    public DynamicModId(string value) => Value = DynamicAppId.Normalize(value, nameof(value));

    /// <summary>
    /// <para>zh-cn:规范化后的 Mod 标识。</para>
    /// <para>en-us:Normalized Mod identifier.</para>
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// <para>zh-cn:返回规范化后的 Mod 标识字符串。</para>
    /// <para>en-us:Returns the normalized Mod identifier string.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:规范化 Mod 标识。</para>
    /// <para>en-us:The normalized Mod identifier.</para>
    /// </returns>
    public override string ToString() => Value;

    /// <summary>
    /// <para>zh-cn:隐式返回规范化 Mod 标识。</para>
    /// <para>en-us:Implicitly returns the normalized Mod identifier.</para>
    /// </summary>
    /// <param name="id">
    /// <para>zh-cn:要转换的 Mod 标识。</para>
    /// <para>en-us:The Mod identifier to convert.</para>
    /// </param>
    public static implicit operator string(DynamicModId id) => id.Value;
}
