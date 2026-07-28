using System.Text.Json.Serialization;

using Air.Cloud.DynamicApp.Contracts.Serialization;

namespace Air.Cloud.DynamicApp.Contracts.Identifiers;

/// <summary>
/// <para>zh-cn:版本值对象，保存发布和路由状态使用的版本字符串。</para>
/// <para>en-us:Version value object storing the version string used by release and routing state.</para>
/// </summary>
[JsonConverter(typeof(DynamicAppVersionJsonConverter))]
public readonly record struct DynamicAppVersion
{
    /// <summary>
    /// <para>zh-cn:创建版本值对象，并校验版本不能为空。</para>
    /// <para>en-us:Creates a version value object and validates that the version is not empty.</para>
    /// </summary>
    /// <param name="value">
    /// <para>zh-cn:版本原始值。</para>
    /// <para>en-us:The raw version value.</para>
    /// </param>
    public DynamicAppVersion(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Version cannot be empty.", nameof(value));
        }

        Value = value.Trim();
    }

    /// <summary>
    /// <para>zh-cn:规范化后的版本。</para>
    /// <para>en-us:Normalized version.</para>
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// <para>zh-cn:返回版本字符串。</para>
    /// <para>en-us:Returns the version string.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:版本字符串。</para>
    /// <para>en-us:The version string.</para>
    /// </returns>
    public override string ToString() => Value;

    /// <summary>
    /// <para>zh-cn:隐式返回版本字符串。</para>
    /// <para>en-us:Implicitly returns the version string.</para>
    /// </summary>
    /// <param name="version">
    /// <para>zh-cn:要转换的版本值对象。</para>
    /// <para>en-us:The version value object to convert.</para>
    /// </param>
    public static implicit operator string(DynamicAppVersion version) => version.Value;
}
