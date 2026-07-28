using Air.Cloud.DynamicApp.Contracts.Identifiers;
using Air.Cloud.DynamicApp.Contracts.Runtime;

namespace Air.Cloud.DynamicApp.Manager;

/// <summary>
/// <para>zh-cn:DynamicApp 目标版本归档请求。</para>
/// <para>en-us:Request model for archiving a DynamicApp target version.</para>
/// </summary>
public sealed class DynamicAppArchiveTargetRequest : DynamicAppTargetKeyRequest
{
    /// <summary>
    /// <para>zh-cn:归档原因；用于记录坏包、错误 Manifest 或人工治理说明。</para>
    /// <para>en-us:Archive reason used to record a bad package, invalid manifest, or manual governance note.</para>
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:转换为运行时目标版本键；字段为空或格式非法时抛出参数异常。</para>
    /// <para>en-us:Converts to a runtime target version key; throws an argument exception when fields are empty or invalid.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:运行时目标版本键。</para>
    /// <para>en-us:The runtime target version key.</para>
    /// </returns>
    public new DynamicModVersionKey ToKey() => new(new DynamicAppId(DynamicAppId), new DynamicModId(ModId), new DynamicAppVersion(Version));
}
