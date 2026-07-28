using Air.Cloud.DynamicApp.Contracts.Identifiers;

namespace Air.Cloud.DynamicApp.Manager;

/// <summary>
/// <para>zh-cn:DynamicApp 回滚请求模型，定位要回滚的 DynamicApp/Mod。</para>
/// <para>en-us:DynamicApp rollback request model that identifies the DynamicApp/Mod to roll back.</para>
/// </summary>
public sealed class DynamicAppRollbackRequest
{
    /// <summary>
    /// <para>zh-cn:DynamicApp 标识。</para>
    /// <para>en-us:DynamicApp identifier.</para>
    /// </summary>
    public string DynamicAppId { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:Mod 标识。</para>
    /// <para>en-us:Mod identifier.</para>
    /// </summary>
    public string ModId { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:转换为强类型 DynamicApp 标识。</para>
    /// <para>en-us:Converts to a strongly typed DynamicApp identifier.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:DynamicApp 标识值对象。</para>
    /// <para>en-us:DynamicApp identifier value object.</para>
    /// </returns>
    public DynamicAppId ToDynamicAppId() => new(DynamicAppId);

    /// <summary>
    /// <para>zh-cn:转换为强类型 Mod 标识。</para>
    /// <para>en-us:Converts to a strongly typed Mod identifier.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:Mod 标识值对象。</para>
    /// <para>en-us:Mod identifier value object.</para>
    /// </returns>
    public DynamicModId ToModId() => new(ModId);
}
