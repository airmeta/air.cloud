using Air.Cloud.Modules.ReverseTunnel.Common.Options;

using Microsoft.AspNetCore.Http;

namespace Air.Cloud.Modules.ReverseTunnel.Management;

/// <summary>
/// <para>zh-cn:AirTunnel 管理端授权接口。宿主可以替换该接口，把管理 API 从默认 Token 校验切换到统一控制面 Ticket、账号体系或其他认证能力。</para>
/// <para>en-us:AirTunnel management authorizer contract. Hosts can replace this interface to switch management APIs from default token validation to a unified control-plane ticket, account system, or another authentication capability.</para>
/// </summary>
public interface IReverseTunnelManagementAuthorizer
{
    /// <summary>
    /// <para>zh-cn:校验当前 HTTP 请求是否允许访问 AirTunnel 管理端。返回 null 表示授权通过；返回 IResult 表示应立即结束请求并写出该响应。</para>
    /// <para>en-us:Checks whether the current HTTP request may access the AirTunnel management endpoint. Returning null means authorization succeeded; returning an IResult means the request should be ended with that response.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:当前 HTTP 请求上下文。</para>
    /// <para>en-us:The current HTTP request context.</para>
    /// </param>
    /// <param name="management">
    /// <para>zh-cn:当前运行期管理端配置。</para>
    /// <para>en-us:The current runtime management options.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:请求取消令牌。</para>
    /// <para>en-us:The request cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:null 表示授权通过；否则返回未授权、未找到或服务不可用等响应。</para>
    /// <para>en-us:Null when authorization succeeds; otherwise an unauthorized, not-found, service-unavailable, or equivalent response.</para>
    /// </returns>
    Task<IResult?> AuthorizeAsync(HttpContext context, ReverseTunnelManagementOptions management, CancellationToken cancellationToken = default);
}
