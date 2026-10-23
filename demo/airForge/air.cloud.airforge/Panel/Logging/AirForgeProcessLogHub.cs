using System.Security.Cryptography;
using System.Text;

using Air.Cloud.Capabilities.ControlPlaneAuth.Services;
using Air.Cloud.DynamicApp.Contracts.Identifiers;
using Air.Cloud.DynamicApp.Contracts.Runtime;
using Air.Cloud.DynamicApp.Host;
using Air.Cloud.DynamicApp.Manager;

using Microsoft.AspNetCore.SignalR;

namespace Air.Cloud.AirForge.Panel.Logging;

/// <summary>
/// <para>zh-cn:AirForge Process 日志 SignalR Hub，将子进程 stdout、stderr 和退出事件实时推送到浏览器。</para>
/// <para>en-us:AirForge SignalR Hub that streams Process child-process stdout, stderr, and exit events to the browser in real time.</para>
/// </summary>
public sealed class AirForgeProcessLogHub : Hub
{
    private readonly IControlPlaneAuthService _controlPlaneAuthService;
    private readonly DynamicAppManagerApiOptions _managerOptions;
    private readonly DynamicAppProcessLogBroadcaster _broadcaster;

    /// <summary>
    /// <para>zh-cn:创建 Process 日志 Hub。</para>
    /// <para>en-us:Creates the Process log Hub.</para>
    /// </summary>
    /// <param name="controlPlaneAuthService">
    /// <para>zh-cn:控制面认证服务。</para>
    /// <para>en-us:Control-plane authentication service.</para>
    /// </param>
    /// <param name="managerOptions">
    /// <para>zh-cn:Manager Ticket 配置，用于兼容面板的 Manager Ticket 授权。</para>
    /// <para>en-us:Manager Ticket options used to support the panel Manager Ticket authorization path.</para>
    /// </param>
    /// <param name="broadcaster">
    /// <para>zh-cn:进程日志广播器。</para>
    /// <para>en-us:Process log broadcaster.</para>
    /// </param>
    public AirForgeProcessLogHub(
        IControlPlaneAuthService controlPlaneAuthService,
        DynamicAppManagerApiOptions managerOptions,
        DynamicAppProcessLogBroadcaster broadcaster)
    {
        _controlPlaneAuthService = controlPlaneAuthService ?? throw new ArgumentNullException(nameof(controlPlaneAuthService));
        _managerOptions = managerOptions ?? throw new ArgumentNullException(nameof(managerOptions));
        _broadcaster = broadcaster ?? throw new ArgumentNullException(nameof(broadcaster));
    }

    /// <summary>
    /// <para>zh-cn:在 WebSocket 连接建立时校验当前控制面会话；未授权连接立即中止，避免匿名客户端保持日志通道。</para>
    /// <para>en-us:Validates the current control-plane session when the WebSocket connection is established; unauthorized connections are aborted immediately so anonymous clients cannot keep a log channel open.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:连接授权完成后的异步任务。</para>
    /// <para>en-us:Asynchronous task completed after connection authorization.</para>
    /// </returns>
    public override async Task OnConnectedAsync()
    {
        if (!await IsAuthorizedAsync().ConfigureAwait(false))
        {
            Context.Abort();
            return;
        }

        await base.OnConnectedAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// <para>zh-cn:持续订阅一个 Process Mod 的实时日志；连接断开后服务端自动结束订阅。</para>
    /// <para>en-us:Continuously subscribes to live logs for one Process Mod; the server ends the subscription automatically when the connection closes.</para>
    /// </summary>
    /// <param name="dynamicAppId">
    /// <para>zh-cn:DynamicApp 应用标识。</para>
    /// <para>en-us:DynamicApp application identifier.</para>
    /// </param>
    /// <param name="modId">
    /// <para>zh-cn:Mod 标识。</para>
    /// <para>en-us:Mod identifier.</para>
    /// </param>
    /// <param name="version">
    /// <para>zh-cn:Mod 版本。</para>
    /// <para>en-us:Mod version.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:持续推送日志的异步任务。</para>
    /// <para>en-us:Asynchronous task that continuously pushes log entries.</para>
    /// </returns>
    public async Task Stream(string dynamicAppId, string modId, string version, string? deploymentId = null)
    {
        if (!await IsAuthorizedAsync().ConfigureAwait(false))
        {
            throw new HubException("Unauthorized AirForge process-log connection.");
        }

        var key = new DynamicModVersionKey(
            new DynamicAppId(dynamicAppId),
            new DynamicModId(modId),
            new DynamicAppVersion(version));
        var runtimeKey = new DynamicAppRuntimeKey(key, string.IsNullOrWhiteSpace(deploymentId) ? "default" : deploymentId.Trim());
        await Clients.Caller.SendAsync("processLogReady", runtimeKey.ToString(), Context.ConnectionAborted).ConfigureAwait(false);

        await foreach (var entry in _broadcaster.Subscribe(runtimeKey, Context.ConnectionAborted).ConfigureAwait(false))
        {
            await Clients.Caller.SendAsync("processLog", entry, Context.ConnectionAborted).ConfigureAwait(false);
        }
    }

    private async Task<bool> IsAuthorizedAsync()
    {
        var httpContext = Context.GetHttpContext();
        if (httpContext == null)
        {
            return false;
        }

        if ((await _controlPlaneAuthService.AuthorizeAsync(httpContext, Context.ConnectionAborted).ConfigureAwait(false)).Succeeded)
        {
            return true;
        }

        if (!_managerOptions.RequireTicket
            || string.IsNullOrWhiteSpace(_managerOptions.HeaderName)
            || _managerOptions.Tickets.Length == 0)
        {
            return false;
        }

        var requestTickets = httpContext.Request.Headers[_managerOptions.HeaderName]
            .Where(ticket => !string.IsNullOrWhiteSpace(ticket))
            .ToArray();
        return requestTickets.Any(requestTicket =>
            _managerOptions.Tickets.Any(allowedTicket => FixedTimeEquals(requestTicket!, allowedTicket)));
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return leftBytes.Length == rightBytes.Length && CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}
