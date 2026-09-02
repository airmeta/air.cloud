using Air.Cloud.DynamicApp.Contracts.Runtime;

namespace Air.Cloud.DynamicApp.Manager;

/// <summary>
/// <para>zh-cn:动态入口路由新增或更新请求；目标版本不可用时必须显式确认后才会保存。</para>
/// <para>en-us:Dynamic entry-route create or update request; an unavailable target version requires explicit confirmation before saving.</para>
/// </summary>
public sealed class DynamicAppGatewayRouteSaveRequest
{
    /// <summary>
    /// <para>zh-cn:待保存的独立动态路由。</para>
    /// <para>en-us:The independent dynamic route to save.</para>
    /// </summary>
    public DynamicAppGatewayRoute Route { get; set; } = new();

    /// <summary>
    /// <para>zh-cn:是否确认在目标不存在或不是 Active + Healthy 时仍保存路由。</para>
    /// <para>en-us:Whether the operator confirms saving when the target is missing or is not Active and Healthy.</para>
    /// </summary>
    public bool ConfirmUnavailable { get; set; }

    /// <summary>
    /// <para>zh-cn:更新时可选的乐观并发时间戳。</para>
    /// <para>en-us:Optional optimistic-concurrency timestamp for updates.</para>
    /// </summary>
    public DateTimeOffset? ExpectedUpdatedAtUtc { get; set; }
}

/// <summary>
/// <para>zh-cn:动态路由目标可用性提示，供管理端展示二次确认信息。</para>
/// <para>en-us:Dynamic route target availability notice used by management clients for confirmation.</para>
/// </summary>
public sealed class DynamicAppGatewayRouteTargetNotice
{
    /// <summary>
    /// <para>zh-cn:是否存在至少一个 Active + Healthy 目标。</para>
    /// <para>en-us:Whether at least one Active and Healthy target exists.</para>
    /// </summary>
    public bool Available { get; init; }

    /// <summary>
    /// <para>zh-cn:是否需要管理端二次确认。</para>
    /// <para>en-us:Whether management must request a second confirmation.</para>
    /// </summary>
    public bool RequiresConfirmation { get; init; }

    /// <summary>
    /// <para>zh-cn:当前目标匹配数量。</para>
    /// <para>en-us:Number of matching runtime targets.</para>
    /// </summary>
    public int MatchingTargetCount { get; init; }

    /// <summary>
    /// <para>zh-cn:面向操作员的提示信息。</para>
    /// <para>en-us:Operator-facing availability message.</para>
    /// </summary>
    public string Message { get; init; } = string.Empty;
}
