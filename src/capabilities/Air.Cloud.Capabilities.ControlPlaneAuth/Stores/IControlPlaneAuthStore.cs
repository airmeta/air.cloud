using Air.Cloud.Capabilities.ControlPlaneAuth.Models;

namespace Air.Cloud.Capabilities.ControlPlaneAuth.Stores;

/// <summary>
/// <para>zh-cn:控制面认证运行数据存储接口。</para>
/// <para>en-us:Runtime data store contract for control-plane authentication.</para>
/// </summary>
public interface IControlPlaneAuthStore
{
    /// <summary>
    /// <para>zh-cn:读取控制面初始化状态。</para>
    /// <para>en-us:Reads the control-plane initialization state.</para>
    /// </summary>
    /// <param name="cancellationToken"><para>zh-cn:取消令牌。</para><para>en-us:Cancellation token.</para></param>
    /// <returns><para>zh-cn:初始化状态。</para><para>en-us:Initialization state.</para></returns>
    Task<ControlPlaneAuthState> ReadStateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:保存控制面初始化状态。</para>
    /// <para>en-us:Saves the control-plane initialization state.</para>
    /// </summary>
    /// <param name="state"><para>zh-cn:初始化状态。</para><para>en-us:Initialization state.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消令牌。</para><para>en-us:Cancellation token.</para></param>
    Task WriteStateAsync(ControlPlaneAuthState state, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:读取全部管理员用户。</para>
    /// <para>en-us:Reads all administrator users.</para>
    /// </summary>
    /// <param name="cancellationToken"><para>zh-cn:取消令牌。</para><para>en-us:Cancellation token.</para></param>
    /// <returns><para>zh-cn:用户列表。</para><para>en-us:User list.</para></returns>
    Task<IReadOnlyList<ControlPlaneUser>> ReadUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:保存全部管理员用户。</para>
    /// <para>en-us:Saves all administrator users.</para>
    /// </summary>
    /// <param name="users"><para>zh-cn:用户列表。</para><para>en-us:User list.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消令牌。</para><para>en-us:Cancellation token.</para></param>
    Task WriteUsersAsync(IReadOnlyList<ControlPlaneUser> users, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:读取全部管理会话。</para>
    /// <para>en-us:Reads all management sessions.</para>
    /// </summary>
    /// <param name="cancellationToken"><para>zh-cn:取消令牌。</para><para>en-us:Cancellation token.</para></param>
    /// <returns><para>zh-cn:会话列表。</para><para>en-us:Session list.</para></returns>
    Task<IReadOnlyList<ControlPlaneSession>> ReadSessionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:保存全部管理会话。</para>
    /// <para>en-us:Saves all management sessions.</para>
    /// </summary>
    /// <param name="sessions"><para>zh-cn:会话列表。</para><para>en-us:Session list.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消令牌。</para><para>en-us:Cancellation token.</para></param>
    Task WriteSessionsAsync(IReadOnlyList<ControlPlaneSession> sessions, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:读取全部服务间票据。</para>
    /// <para>en-us:Reads all service-to-service tickets.</para>
    /// </summary>
    /// <param name="cancellationToken"><para>zh-cn:取消令牌。</para><para>en-us:Cancellation token.</para></param>
    /// <returns><para>zh-cn:服务票据列表。</para><para>en-us:Service-ticket list.</para></returns>
    Task<IReadOnlyList<ControlPlaneServiceTicket>> ReadServiceTicketsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:保存全部服务间票据。</para>
    /// <para>en-us:Saves all service-to-service tickets.</para>
    /// </summary>
    /// <param name="tickets"><para>zh-cn:服务票据列表。</para><para>en-us:Service-ticket list.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消令牌。</para><para>en-us:Cancellation token.</para></param>
    Task WriteServiceTicketsAsync(IReadOnlyList<ControlPlaneServiceTicket> tickets, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:写入一次性初始化 Ticket 明文文件。该文件只用于首次初始化，初始化完成后应删除。</para>
    /// <para>en-us:Writes the one-time bootstrap-ticket clear-text file. The file is used only for first initialization and should be deleted after initialization completes.</para>
    /// </summary>
    /// <param name="plainTextTicket"><para>zh-cn:初始化 Ticket 明文。</para><para>en-us:Bootstrap ticket in clear text.</para></param>
    /// <param name="expiresAtUtc"><para>zh-cn:过期时间。</para><para>en-us:Expiration time.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消令牌。</para><para>en-us:Cancellation token.</para></param>
    Task WriteBootstrapTicketFileAsync(string plainTextTicket, DateTimeOffset expiresAtUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:删除一次性初始化 Ticket 明文文件。</para>
    /// <para>en-us:Deletes the one-time bootstrap-ticket clear-text file.</para>
    /// </summary>
    /// <param name="cancellationToken"><para>zh-cn:取消令牌。</para><para>en-us:Cancellation token.</para></param>
    Task DeleteBootstrapTicketFileAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>zh-cn:追加一条审计记录。</para>
    /// <para>en-us:Appends one audit record.</para>
    /// </summary>
    /// <param name="action"><para>zh-cn:审计动作。</para><para>en-us:Audit action.</para></param>
    /// <param name="userName"><para>zh-cn:用户名，可为空。</para><para>en-us:User name, if available.</para></param>
    /// <param name="message"><para>zh-cn:审计消息。</para><para>en-us:Audit message.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消令牌。</para><para>en-us:Cancellation token.</para></param>
    Task AppendAuditAsync(string action, string? userName, string message, CancellationToken cancellationToken = default);
}
