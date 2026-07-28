using Air.Cloud.DynamicApp.Contracts.Identifiers;

namespace Air.Cloud.DynamicApp.Contracts.Runtime;

/// <summary>
/// <para>zh-cn:DynamicApp、Mod 与版本组成的运行目标唯一键。</para>
/// <para>en-us:Unique runtime target key composed from DynamicApp, Mod, and version.</para>
/// </summary>
/// <param name="DynamicAppId">
/// <para>zh-cn:DynamicApp 标识。</para>
/// <para>en-us:DynamicApp identifier.</para>
/// </param>
/// <param name="ModId">
/// <para>zh-cn:Mod 标识。</para>
/// <para>en-us:Mod identifier.</para>
/// </param>
/// <param name="Version">
/// <para>zh-cn:Mod 版本。</para>
/// <para>en-us:Mod version.</para>
/// </param>
public readonly record struct DynamicModVersionKey(DynamicAppId DynamicAppId, DynamicModId ModId, DynamicAppVersion Version)
{
    /// <summary>
    /// <para>zh-cn:返回面向日志和字典键的复合键字符串。</para>
    /// <para>en-us:Returns the composite key string for logs and dictionary keys.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:格式为 DynamicApp/Mod/Version 的复合键。</para>
    /// <para>en-us:A composite key in DynamicApp/Mod/Version format.</para>
    /// </returns>
    public override string ToString() => $"{DynamicAppId.Value}/{ModId.Value}/{Version.Value}";
}
