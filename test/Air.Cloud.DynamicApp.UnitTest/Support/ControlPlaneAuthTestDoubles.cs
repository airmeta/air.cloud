using Air.Cloud.Capabilities.ControlPlaneAuth.Models;
using Air.Cloud.Capabilities.ControlPlaneAuth.Services;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Air.Cloud.DynamicApp.UnitTest.Support;

/// <summary>
/// <para>zh-cn:面板单元测试使用的静态控制面认证服务和选项监视器。</para>
/// <para>en-us:Static control-plane authentication service and options monitor used by panel unit tests.</para>
/// </summary>
internal sealed class ControlPlaneAuthTestDoubles
{
    /// <summary>
    /// <para>zh-cn:创建未授权的认证服务替身。</para>
    /// <para>en-us:Creates an unauthorized authentication-service double.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:未授权认证服务。</para>
    /// <para>en-us:Unauthorized authentication service.</para>
    /// </returns>
    public static IControlPlaneAuthService CreateUnauthorizedService() => new StaticControlPlaneAuthService(false);

    /// <summary>
    /// <para>zh-cn:创建固定返回值的选项监视器。</para>
    /// <para>en-us:Creates an options monitor with a fixed value.</para>
    /// </summary>
    /// <typeparam name="T">
    /// <para>zh-cn:选项类型。</para>
    /// <para>en-us:Options type.</para>
    /// </typeparam>
    /// <param name="value">
    /// <para>zh-cn:固定选项值。</para>
    /// <para>en-us:Fixed options value.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:选项监视器。</para>
    /// <para>en-us:Options monitor.</para>
    /// </returns>
    public static IOptionsMonitor<T> CreateOptionsMonitor<T>(T value) => new StaticOptionsMonitor<T>(value);

    private sealed class StaticControlPlaneAuthService(bool authorized) : IControlPlaneAuthService
    {
        public Task EnsureBootstrapTicketAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<object> ReadStatusAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<object>(new { initialized = true });

        public Task<ControlPlaneOperationResult> InitializeAsync(string bootstrapTicket, string userName, string password, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ControlPlaneOperationResult(false));

        public Task<ControlPlaneLoginResult> LoginAsync(string userName, string password, string? mfaCode = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ControlPlaneLoginResult(false));

        public Task<ControlPlaneMfaSetupResult> BeginMfaSetupAsync(string userName, string password, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ControlPlaneMfaSetupResult(false));

        public Task<ControlPlaneOperationResult> CompleteMfaSetupAsync(string setupToken, string code, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ControlPlaneOperationResult(false));

        public Task<ControlPlaneOperationResult> ResetMfaAsync(string userName, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ControlPlaneOperationResult(false));

        public Task<ControlPlaneAuthorizationResult> AuthorizeAsync(HttpContext context, CancellationToken cancellationToken = default) =>
            Task.FromResult(authorized
                ? new ControlPlaneAuthorizationResult(true)
                : new ControlPlaneAuthorizationResult(false));

        public Task LogoutAsync(HttpContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<IReadOnlyList<ControlPlaneServiceTicket>> ReadServiceTicketsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ControlPlaneServiceTicket>>(Array.Empty<ControlPlaneServiceTicket>());

        public Task<ControlPlaneServiceTicketCreatedResult> CreateServiceTicketAsync(string type, string name, DateTimeOffset? expiresAtUtc = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<ControlPlaneServiceTicketCreatedResult?> RotateServiceTicketAsync(string id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> SetServiceTicketEnabledAsync(string id, bool enabled, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }

    private sealed class StaticOptionsMonitor<T>(T currentValue) : IOptionsMonitor<T>
    {
        public T CurrentValue { get; } = currentValue;

        public T Get(string? name) => CurrentValue;

        public IDisposable OnChange(Action<T, string?> listener) => NoopDisposable.Instance;

        private sealed class NoopDisposable : IDisposable
        {
            public static NoopDisposable Instance { get; } = new();

            public void Dispose()
            {
            }
        }
    }
}
