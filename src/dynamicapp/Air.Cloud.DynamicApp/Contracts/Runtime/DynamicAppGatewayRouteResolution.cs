using Air.Cloud.DynamicApp.Gateway;

namespace Air.Cloud.DynamicApp.Contracts.Runtime;

/// <summary>
/// <para>zh-cn:动态路由解析结果，区分未匹配、目标不存在和目标不可用。</para>
/// <para>en-us:Dynamic route resolution result distinguishing no match, missing targets, and unavailable targets.</para>
/// </summary>
public sealed class DynamicAppGatewayRouteResolution
{
    private DynamicAppGatewayRouteResolution(
        bool succeeded,
        DynamicAppGatewayRouteMatch? match,
        DynamicAppRouteTarget? target,
        string errorCode,
        string message)
    {
        Succeeded = succeeded;
        Match = match;
        Target = target;
        ErrorCode = errorCode;
        Message = message;
    }

    /// <summary>
    /// <para>zh-cn:是否成功解析并选出可转发目标。</para>
    /// <para>en-us:Whether a matching route and forwardable target were resolved.</para>
    /// </summary>
    public bool Succeeded { get; }

    /// <summary>
    /// <para>zh-cn:命中的动态路由结果。</para>
    /// <para>en-us:Matched dynamic route result.</para>
    /// </summary>
    public DynamicAppGatewayRouteMatch? Match { get; }

    /// <summary>
    /// <para>zh-cn:选中的运行目标。</para>
    /// <para>en-us:Selected runtime target.</para>
    /// </summary>
    public DynamicAppRouteTarget? Target { get; }

    /// <summary>
    /// <para>zh-cn:稳定错误码；成功时为空。</para>
    /// <para>en-us:Stable error code; empty on success.</para>
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// <para>zh-cn:面向管理端和日志的结果说明。</para>
    /// <para>en-us:Result message for management clients and logs.</para>
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// <para>zh-cn:创建成功解析结果。</para>
    /// <para>en-us:Creates a successful resolution result.</para>
    /// </summary>
    public static DynamicAppGatewayRouteResolution Success(DynamicAppGatewayRouteMatch match, DynamicAppRouteTarget target)
        => new(true, match, target, string.Empty, "Dynamic route resolved.");

    /// <summary>
    /// <para>zh-cn:创建失败解析结果。</para>
    /// <para>en-us:Creates a failed resolution result.</para>
    /// </summary>
    public static DynamicAppGatewayRouteResolution Failure(string errorCode, string message, DynamicAppGatewayRouteMatch? match = null)
        => new(false, match, null, errorCode, message);
}
