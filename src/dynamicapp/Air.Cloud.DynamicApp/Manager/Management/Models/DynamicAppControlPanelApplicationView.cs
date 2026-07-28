using Air.Cloud.DynamicApp.Contracts.Identifiers;

namespace Air.Cloud.DynamicApp.Manager;

/// <summary>
/// <para>zh-cn:DynamicApp 控制面板应用视图，聚合应用级 Manifest 历史、当前运行目标和 Mod 摘要。</para>
/// <para>en-us:DynamicApp control-panel application view aggregating application-level manifest history, current runtime targets, and Mod summaries.</para>
/// </summary>
public sealed class DynamicAppControlPanelApplicationView
{
    /// <summary>
    /// <para>zh-cn:DynamicApp 应用标识。</para>
    /// <para>en-us:DynamicApp application identifier.</para>
    /// </summary>
    public DynamicAppId DynamicAppId { get; set; }

    /// <summary>
    /// <para>zh-cn:最近一次发布的应用版本；没有 Manifest 历史时为空字符串。</para>
    /// <para>en-us:Latest published application version; empty when no manifest history exists.</para>
    /// </summary>
    public string LatestVersion { get; set; } = string.Empty;

    /// <summary>
    /// <para>zh-cn:最近一次 Manifest 保存时间；仅存在运行状态但没有历史记录时为 null。</para>
    /// <para>en-us:Latest manifest save time; null when runtime state exists without history records.</para>
    /// </summary>
    public DateTimeOffset? LatestPublishedAtUtc { get; set; }

    /// <summary>
    /// <para>zh-cn:当前应用下的 Mod 数量，按 Mod 标识聚合。</para>
    /// <para>en-us:Number of Mods under the application, grouped by Mod identifier.</para>
    /// </summary>
    public int ModCount { get; set; }

    /// <summary>
    /// <para>zh-cn:当前处于 Active 且 Healthy 状态的 Mod 数量。</para>
    /// <para>en-us:Number of Mods currently in Active and Healthy state.</para>
    /// </summary>
    public int ActiveModCount { get; set; }

    /// <summary>
    /// <para>zh-cn:控制面板展示用 Mod 视图集合。</para>
    /// <para>en-us:Mod view collection used by the control panel.</para>
    /// </summary>
    public IReadOnlyCollection<DynamicAppControlPanelModView> Mods { get; set; } = Array.Empty<DynamicAppControlPanelModView>();
}
