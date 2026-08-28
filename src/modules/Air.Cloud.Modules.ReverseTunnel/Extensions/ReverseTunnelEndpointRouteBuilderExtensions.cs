using Air.Cloud.Core.Attributes;
using Air.Cloud.Modules.ReverseTunnel.Common.Diagnostics;
using Air.Cloud.Modules.ReverseTunnel.Common.Http;
using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Protocol;
using Air.Cloud.Modules.ReverseTunnel.Common.Routing;
using Air.Cloud.Modules.ReverseTunnel.Common.Runtime;
using Air.Cloud.Modules.ReverseTunnel.Management;
using Air.Cloud.Modules.ReverseTunnel.Server;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Air.Cloud.Modules.ReverseTunnel.Extensions;

/// <summary>
/// <para>zh-cn:反向隧道 Server endpoint 映射扩展。调用这些方法会显式开放 Agent gRPC 接入面。</para>
/// <para>en-us:Reverse tunnel Server endpoint-mapping extensions. Calling these methods explicitly exposes the Agent gRPC ingress surface.</para>
/// </summary>
[IgnoreScanning]
public static class ReverseTunnelEndpointRouteBuilderExtensions
{
    private static readonly JsonSerializerOptions ClientConfigurationSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// <para>zh-cn:在已有 endpoint 路由表中映射反向隧道 Server gRPC 服务。该方法要求模块已启用、当前进程包含 Server 角色、CA 挂载配置有效且 CA PFX 可加载；普通 Ocelot 路由不会被修改。</para>
    /// <para>en-us:Maps the reverse tunnel Server gRPC service into an existing endpoint route table. The method requires the module to be enabled, the current process to use Server mode, valid CA mount configuration, and a loadable CA PFX; normal Ocelot routes are not modified.</para>
    /// </summary>
    /// <param name="endpoints">
    /// <para>zh-cn:宿主已有的 endpoint 路由构建器，通常位于 UseEndpoints 回调内。</para>
    /// <para>en-us:Existing endpoint route builder from the host, normally inside the UseEndpoints callback.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:原始 endpoint 路由构建器，便于链式调用。</para>
    /// <para>en-us:The original endpoint route builder for chaining.</para>
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// <para>zh-cn:模块未启用、当前进程不是 Server 角色、选项验证失败或挂载 CA 无法加载。</para>
    /// <para>en-us:The module is not enabled, the current process is not in Server mode, options validation fails, or the mounted CA cannot be loaded.</para>
    /// </exception>
    public static IEndpointRouteBuilder MapReverseTunnelServer(this IEndpointRouteBuilder endpoints)
    {
        if (endpoints is null)
        {
            throw new ArgumentNullException(nameof(endpoints));
        }

        var options = ValidateServerEndpoint(endpoints.ServiceProvider);
        endpoints.MapGrpcService<ReverseTunnelTransportService>();
        endpoints.MapGrpcService<ReverseTunnelConfigurationService>();
        endpoints.MapGrpcService<ReverseTunnelCertificateManagementService>();
        if (options.Enrollment.Enabled)
        {
            endpoints.MapGrpcService<ReverseTunnelEnrollmentService>();
        }

        endpoints.MapReverseTunnelGatewayInvoke();
        endpoints.MapReverseTunnelDiagnostics();
        endpoints.MapReverseTunnelManagement();
        return endpoints;
    }

    /// <summary>
    /// <para>zh-cn:映射 Gateway 到独立 AirTunnel Server 的 HTTP 调用入口。入口会按 `ServiceKey` 选择当前 Server 进程内已连接的 Agent 会话，并把请求流转发到 Agent；该入口仅在 Server 角色进程中使用。</para>
    /// <para>en-us:Maps the HTTP invocation entry from Gateway to a standalone AirTunnel Server. The endpoint selects a connected Agent session in the current Server process by `ServiceKey` and forwards the request stream to the Agent; it is intended only for Server-role processes.</para>
    /// </summary>
    /// <param name="endpoints">
    /// <para>zh-cn:endpoint 路由构建器。</para>
    /// <para>en-us:The endpoint route builder.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:原始 endpoint 路由构建器。</para>
    /// <para>en-us:The original endpoint route builder.</para>
    /// </returns>
    public static IEndpointRouteBuilder MapReverseTunnelGatewayInvoke(this IEndpointRouteBuilder endpoints)
    {
        if (endpoints is null)
        {
            throw new ArgumentNullException(nameof(endpoints));
        }

        var options = endpoints.ServiceProvider.GetRequiredService<IOptions<ReverseTunnelOptions>>().Value;
        var prefix = options.Gateway.InvokePathPrefix;
        endpoints.MapMethods($"{prefix}/{{serviceKey}}/{{**path}}", ["GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS"], InvokeFromGatewayAsync)
            .WithDisplayName("AirTunnel Gateway remote invoke");
        endpoints.MapMethods($"{prefix}/{{serviceKey}}", ["GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS"], InvokeFromGatewayAsync)
            .WithDisplayName("AirTunnel Gateway remote invoke root");
        return endpoints;
    }

    /// <summary>
    /// <para>zh-cn:映射 AirTunnel 诊断 HTTP API。endpoint 会被固定加入路由表；每次请求都会按运行期配置检查 `ReverseTunnelSettings:Diagnostics:Enabled`，关闭时返回 404。</para>
    /// <para>en-us:Maps AirTunnel diagnostic HTTP APIs. Endpoints are always added to the route table; each request checks the runtime `ReverseTunnelSettings:Diagnostics:Enabled` value and returns 404 when diagnostics are disabled.</para>
    /// </summary>
    /// <param name="endpoints">
    /// <para>zh-cn:endpoint 路由构建器。</para>
    /// <para>en-us:The endpoint route builder.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:原始 endpoint 路由构建器。诊断接口路径在映射时确定，开关在请求时判断。</para>
    /// <para>en-us:The original endpoint route builder. The diagnostic API path is determined at mapping time, while the switch is evaluated per request.</para>
    /// </returns>
    public static IEndpointRouteBuilder MapReverseTunnelDiagnostics(this IEndpointRouteBuilder endpoints)
    {
        if (endpoints is null)
        {
            throw new ArgumentNullException(nameof(endpoints));
        }

        var options = endpoints.ServiceProvider.GetRequiredService<IReverseTunnelRuntimeOptionsAccessor>().Current;
        var diagnostics = ReverseTunnelDiagnosticsConfigurationReader.Read(options);
        var prefix = diagnostics.PathPrefix;
        endpoints.MapGet($"{prefix}/snapshot", (IReverseTunnelDiagnosticsService diagnostics, IReverseTunnelRuntimeOptionsAccessor options) => CreateDiagnosticsResult(options.Current, () => diagnostics.GetSnapshot()))
            .WithDisplayName("AirTunnel diagnostics snapshot");
        endpoints.MapGet($"{prefix}/metrics", (IReverseTunnelDiagnosticsService diagnostics, IReverseTunnelRuntimeOptionsAccessor options) => CreateDiagnosticsResult(options.Current, () => diagnostics.GetMetrics()))
            .WithDisplayName("AirTunnel diagnostics metrics");
        endpoints.MapGet($"{prefix}/sessions", (IReverseTunnelDiagnosticsService diagnostics, IReverseTunnelRuntimeOptionsAccessor options) => CreateDiagnosticsResult(options.Current, () => diagnostics.GetSessions()))
            .WithDisplayName("AirTunnel diagnostics sessions");
        return endpoints;
    }

    /// <summary>
    /// <para>zh-cn:映射 AirTunnel Server 管理 API。管理 API 会按 `ReverseTunnelSettings:Management` 检查独立管理端口、访问 token 和写入开关；未启用管理端时不添加路由。</para>
    /// <para>en-us:Maps AirTunnel Server management APIs. The management APIs validate the dedicated management port, access token, and write switches from `ReverseTunnelSettings:Management`; no routes are added when management is disabled.</para>
    /// </summary>
    /// <param name="endpoints">
    /// <para>zh-cn:endpoint 路由构建器。</para>
    /// <para>en-us:The endpoint route builder.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:原始 endpoint 路由构建器。</para>
    /// <para>en-us:The original endpoint route builder.</para>
    /// </returns>
    public static IEndpointRouteBuilder MapReverseTunnelManagement(this IEndpointRouteBuilder endpoints)
    {
        if (endpoints is null)
        {
            throw new ArgumentNullException(nameof(endpoints));
        }

        var options = endpoints.ServiceProvider.GetRequiredService<IReverseTunnelRuntimeOptionsAccessor>().Current;
        if (!options.Management.Enabled)
        {
            return endpoints;
        }

        var prefix = options.Management.PathPrefix;
        endpoints.MapGet("/", (HttpContext context, IReverseTunnelRuntimeOptionsAccessor runtimeOptions) =>
            IsManagementPort(context, runtimeOptions.Current.Management) ? Results.Redirect(runtimeOptions.Current.Management.PathPrefix) : Results.NotFound())
            .WithDisplayName("AirTunnel management root redirect");
        endpoints.MapGet(prefix, async (
            HttpContext context,
            IReverseTunnelRuntimeOptionsAccessor runtimeOptions,
            IReverseTunnelManagementAuthorizer authorizer) =>
        {
            if (await authorizer.AuthorizeAsync(context, runtimeOptions.Current.Management, context.RequestAborted).ConfigureAwait(false) is { } unauthorized)
            {
                return unauthorized;
            }

            return IsManagementPort(context, runtimeOptions.Current.Management)
                ? Results.Content(CreateManagementPageV2(runtimeOptions.Current.Management.PathPrefix), "text/html; charset=utf-8")
                : Results.NotFound();
        })
            .WithDisplayName("AirTunnel management page");
        endpoints.MapGet($"{prefix}/overview", CreateManagementOverviewAsync)
            .WithDisplayName("AirTunnel management overview");
        endpoints.MapGet($"{prefix}/config", ReadManagementConfigAsync)
            .WithDisplayName("AirTunnel management config");
        endpoints.MapPost($"{prefix}/config/validate", ValidateManagementConfigAsync)
            .WithDisplayName("AirTunnel management config validate");
        endpoints.MapPut($"{prefix}/config", SaveManagementConfigAsync)
            .WithDisplayName("AirTunnel management config save");
        endpoints.MapGet($"{prefix}/config/backups", ListManagementBackupsAsync)
            .WithDisplayName("AirTunnel management config backups");
        endpoints.MapPost($"{prefix}/config/backups/{{backupName}}/restore", RestoreManagementBackupAsync)
            .WithDisplayName("AirTunnel management config restore");
        endpoints.MapGet($"{prefix}/config/audit", ListManagementAuditAsync)
            .WithDisplayName("AirTunnel management config audit");
        endpoints.MapGet($"{prefix}/sessions", ReadManagementSessionsAsync)
            .WithDisplayName("AirTunnel management sessions");
        endpoints.MapGet($"{prefix}/metrics", ReadManagementMetricsAsync)
            .WithDisplayName("AirTunnel management metrics");
        endpoints.MapPost($"{prefix}/sessions/{{sessionId}}/disconnect", DisconnectManagementSessionAsync)
            .WithDisplayName("AirTunnel management disconnect session");
        endpoints.MapPost($"{prefix}/certificates/revoke", RevokeManagementCertificateAsync)
            .WithDisplayName("AirTunnel management revoke certificate");
        endpoints.MapPost($"{prefix}/clients/configuration-requests", PushClientConfigurationAsync)
            .WithDisplayName("AirTunnel management client configuration request");
        endpoints.MapGet($"{prefix}/clients/{{agentId}}/configuration", ReadClientConfigurationAsync)
            .WithDisplayName("AirTunnel management client desired configuration");
        endpoints.MapGet($"{prefix}/clients/{{agentId}}/configuration-status", ReadClientConfigurationStatusAsync)
            .WithDisplayName("AirTunnel management client configuration status");
        return endpoints;
    }

    private static IResult CreateDiagnosticsResult<TValue>(ReverseTunnelOptions options, Func<TValue> read)
    {
        var diagnostics = ReverseTunnelDiagnosticsConfigurationReader.Read(options);
        return diagnostics.Enabled ? Results.Ok(read()) : Results.NotFound();
    }

    private static async Task<IResult> CreateManagementOverviewAsync(
        HttpContext context,
        IReverseTunnelRuntimeOptionsAccessor runtimeOptions,
        IReverseTunnelDiagnosticsService diagnostics,
        IReverseTunnelDesiredAgentConfigurationStore desiredConfigurations,
        IReverseTunnelAgentRouteCatalog agentRouteCatalog,
        IReverseTunnelManagementAuthorizer authorizer)
    {
        if (await authorizer.AuthorizeAsync(context, runtimeOptions.Current.Management, context.RequestAborted).ConfigureAwait(false) is { } unauthorized)
        {
            return unauthorized;
        }

        var current = runtimeOptions.Current;
        var snapshot = diagnostics.GetSnapshot();
        var desired = await desiredConfigurations.ListAsync(context.RequestAborted);
        return Results.Ok(new ReverseTunnelManagementOverview
        {
            ControlUrl = ResolveControlUrl(context),
            ManagementUrl = current.Management.BindUrl,
            TcpListeners = current.Tcp.Listeners,
            UdpListeners = current.Udp.Listeners,
            Sessions = snapshot.Sessions,
            Metrics = snapshot.Metrics,
            Version = runtimeOptions.Version,
            AgentConfigurations = desired.Select(item => new ReverseTunnelAgentConfigurationStatus
            {
                AgentId = item.AgentId,
                DesiredVersion = item.Version,
                GeneratedAtUtc = item.GeneratedAtUtc,
                Sha256 = item.Sha256
            }).ToArray(),
            DerivedRoutes = agentRouteCatalog.List(),
            DerivedTcpListeners = agentRouteCatalog.ListTcpListeners(),
            DerivedUdpListeners = agentRouteCatalog.ListUdpListeners()
        });
    }

    private static async Task<IResult> ReadManagementConfigAsync(
        HttpContext context,
        IReverseTunnelRuntimeOptionsAccessor runtimeOptions,
        IReverseTunnelConfigurationStore configurationStore,
        IReverseTunnelManagementAuthorizer authorizer)
    {
        if (await authorizer.AuthorizeAsync(context, runtimeOptions.Current.Management, context.RequestAborted).ConfigureAwait(false) is { } unauthorized)
        {
            return unauthorized;
        }

        return Results.Ok(await configurationStore.ReadAsync(context.RequestAborted));
    }

    private static async Task<IResult> ValidateManagementConfigAsync(
        HttpContext context,
        ReverseTunnelOptions options,
        IReverseTunnelRuntimeOptionsAccessor runtimeOptions,
        IReverseTunnelConfigurationStore configurationStore,
        IReverseTunnelManagementAuthorizer authorizer)
    {
        if (await authorizer.AuthorizeAsync(context, runtimeOptions.Current.Management, context.RequestAborted).ConfigureAwait(false) is { } unauthorized)
        {
            return unauthorized;
        }

        return Results.Ok(await configurationStore.ValidateAsync(options, context.RequestAborted));
    }

    private static async Task<IResult> SaveManagementConfigAsync(
        HttpContext context,
        ReverseTunnelConfigurationSaveRequest request,
        IReverseTunnelRuntimeOptionsAccessor runtimeOptions,
        IReverseTunnelConfigurationStore configurationStore,
        IReverseTunnelManagementAuthorizer authorizer)
    {
        if (await authorizer.AuthorizeAsync(context, runtimeOptions.Current.Management, context.RequestAborted).ConfigureAwait(false) is { } unauthorized)
        {
            return unauthorized;
        }

        if (!runtimeOptions.Current.Management.AllowConfigWrite)
        {
            return Results.Problem("AirTunnel management configuration writes are disabled.", statusCode: StatusCodes.Status403Forbidden);
        }

        if (string.IsNullOrWhiteSpace(request.ChangeReason))
        {
            return Results.BadRequest(new { message = "changeReason is required in the JSON request body." });
        }

        var result = await configurationStore.SaveAsync(
            request.Options,
            context.Request.Headers["X-AirTunnel-Actor"].FirstOrDefault() ?? context.User.Identity?.Name,
            request.ChangeReason,
            context.RequestAborted);
        return result.Succeeded ? Results.Ok(result) : Results.BadRequest(result);
    }

    private static async Task<IResult> RestoreManagementBackupAsync(
        HttpContext context,
        string backupName,
        ReverseTunnelConfigurationRestoreRequest request,
        IReverseTunnelRuntimeOptionsAccessor runtimeOptions,
        IReverseTunnelConfigurationStore configurationStore,
        IReverseTunnelManagementAuthorizer authorizer)
    {
        if (await authorizer.AuthorizeAsync(context, runtimeOptions.Current.Management, context.RequestAborted).ConfigureAwait(false) is { } unauthorized)
        {
            return unauthorized;
        }

        if (!runtimeOptions.Current.Management.AllowConfigWrite)
        {
            return Results.Problem("AirTunnel management configuration writes are disabled.", statusCode: StatusCodes.Status403Forbidden);
        }

        if (string.IsNullOrWhiteSpace(request.ChangeReason))
        {
            return Results.BadRequest(new { message = "changeReason is required in the JSON request body." });
        }

        var result = await configurationStore.RestoreAsync(
            backupName,
            context.Request.Headers["X-AirTunnel-Actor"].FirstOrDefault() ?? context.User.Identity?.Name,
            request.ChangeReason,
            context.RequestAborted);
        return result.Succeeded ? Results.Ok(result) : Results.BadRequest(result);
    }

    private static async Task<IResult> ListManagementAuditAsync(
        HttpContext context,
        IReverseTunnelRuntimeOptionsAccessor runtimeOptions,
        IReverseTunnelConfigurationStore configurationStore,
        IReverseTunnelManagementAuthorizer authorizer)
    {
        if (await authorizer.AuthorizeAsync(context, runtimeOptions.Current.Management, context.RequestAborted).ConfigureAwait(false) is { } unauthorized)
        {
            return unauthorized;
        }

        return Results.Ok(await configurationStore.ListAuditAsync(context.RequestAborted));
    }

    private static async Task<IResult> ListManagementBackupsAsync(
        HttpContext context,
        IReverseTunnelRuntimeOptionsAccessor runtimeOptions,
        IReverseTunnelConfigurationStore configurationStore,
        IReverseTunnelManagementAuthorizer authorizer)
    {
        if (await authorizer.AuthorizeAsync(context, runtimeOptions.Current.Management, context.RequestAborted).ConfigureAwait(false) is { } unauthorized)
        {
            return unauthorized;
        }

        return Results.Ok(await configurationStore.ListBackupsAsync(context.RequestAborted));
    }

    private static async Task<IResult> ReadManagementSessionsAsync(
        HttpContext context,
        IReverseTunnelRuntimeOptionsAccessor runtimeOptions,
        IReverseTunnelDiagnosticsService diagnostics,
        IReverseTunnelManagementAuthorizer authorizer)
    {
        if (await authorizer.AuthorizeAsync(context, runtimeOptions.Current.Management, context.RequestAborted).ConfigureAwait(false) is { } unauthorized)
        {
            return unauthorized;
        }

        return Results.Ok(diagnostics.GetSessions());
    }

    private static async Task<IResult> ReadManagementMetricsAsync(
        HttpContext context,
        IReverseTunnelRuntimeOptionsAccessor runtimeOptions,
        IReverseTunnelDiagnosticsService diagnostics,
        IReverseTunnelManagementAuthorizer authorizer)
    {
        if (await authorizer.AuthorizeAsync(context, runtimeOptions.Current.Management, context.RequestAborted).ConfigureAwait(false) is { } unauthorized)
        {
            return unauthorized;
        }

        return Results.Ok(diagnostics.GetMetrics());
    }

    private static async Task<IResult> DisconnectManagementSessionAsync(
        HttpContext context,
        string sessionId,
        IReverseTunnelRuntimeOptionsAccessor runtimeOptions,
        IReverseTunnelSessionRegistry sessionRegistry,
        IReverseTunnelManagementAuthorizer authorizer)
    {
        if (await authorizer.AuthorizeAsync(context, runtimeOptions.Current.Management, context.RequestAborted).ConfigureAwait(false) is { } unauthorized)
        {
            return unauthorized;
        }

        if (!runtimeOptions.Current.Management.AllowRemoteClientManagement)
        {
            return Results.Problem("AirTunnel remote client management is disabled.", statusCode: StatusCodes.Status403Forbidden);
        }

        return sessionRegistry.Disconnect(sessionId, "Session disconnected from AirTunnel management endpoint.")
            ? Results.Ok(new { sessionId, disconnected = true })
            : Results.NotFound();
    }

    private static async Task<IResult> RevokeManagementCertificateAsync(
        HttpContext context,
        ReverseTunnelCertificateRevocationRequest request,
        IReverseTunnelRuntimeOptionsAccessor runtimeOptions,
        IReverseTunnelCertificateStatusRegistry certificateStatusRegistry,
        IReverseTunnelManagementAuthorizer authorizer)
    {
        if (await authorizer.AuthorizeAsync(context, runtimeOptions.Current.Management, context.RequestAborted).ConfigureAwait(false) is { } unauthorized)
        {
            return unauthorized;
        }

        if (!runtimeOptions.Current.Management.AllowRemoteClientManagement)
        {
            return Results.Problem("AirTunnel remote client management is disabled.", statusCode: StatusCodes.Status403Forbidden);
        }

        if (string.IsNullOrWhiteSpace(request.IssuerThumbprint) || string.IsNullOrWhiteSpace(request.SerialNumber))
        {
            return Results.BadRequest(new { code = "certificate_identity_required", message = "IssuerThumbprint and SerialNumber are required." });
        }

        certificateStatusRegistry.Revoke(request.IssuerThumbprint, request.SerialNumber);
        return Results.Ok(new { revoked = true });
    }

    private static async Task<IResult> PushClientConfigurationAsync(
        HttpContext context,
        ReverseTunnelClientConfigurationPushRequest request,
        IReverseTunnelRuntimeOptionsAccessor runtimeOptions,
        IReverseTunnelSessionRegistry sessionRegistry,
        IReverseTunnelDesiredAgentConfigurationStore desiredConfigurations,
        IReverseTunnelAgentRouteCatalog agentRouteCatalog,
        IReverseTunnelManagementAuthorizer authorizer)
    {
        if (await authorizer.AuthorizeAsync(context, runtimeOptions.Current.Management, context.RequestAborted).ConfigureAwait(false) is { } unauthorized)
        {
            return unauthorized;
        }

        if (!runtimeOptions.Current.Management.AllowRemoteClientManagement)
        {
            return Results.Problem("AirTunnel remote client management is disabled.", statusCode: StatusCodes.Status403Forbidden);
        }

        if ((string.IsNullOrWhiteSpace(request.SessionId) &&
             string.IsNullOrWhiteSpace(request.AgentId)) ||
            request.Configuration is null)
        {
            return Results.BadRequest(new { code = "agent_or_session_and_configuration_required", message = "AgentId or SessionId and Configuration are required." });
        }

        ReverseTunnelSession? session = null;
        if (!string.IsNullOrWhiteSpace(request.SessionId))
        {
            if (!sessionRegistry.TryGet(request.SessionId.Trim(), out session) || session is null)
            {
                return Results.NotFound(new { code = "session_not_found", message = "The target Agent session was not found." });
            }
        }

        ReverseTunnelAgentConfigurationSnapshot snapshotRequest;
        try
        {
            snapshotRequest = request.Configuration switch
            {
                ReverseTunnelAgentConfigurationSnapshot typedSnapshot => typedSnapshot,
                JsonElement element => JsonSerializer.Deserialize<ReverseTunnelAgentConfigurationSnapshot>(element.GetRawText(), ClientConfigurationSerializerOptions) ?? throw new InvalidOperationException("Configuration is empty."),
                _ => JsonSerializer.Deserialize<ReverseTunnelAgentConfigurationSnapshot>(
                        JsonSerializer.Serialize(request.Configuration, ClientConfigurationSerializerOptions), ClientConfigurationSerializerOptions)
                    ?? throw new InvalidOperationException("Configuration is empty.")
            };
        }
        catch (Exception exception)
        {
            return Results.BadRequest(new { code = "invalid_configuration", message = exception.Message });
        }

        var agentId = session?.AgentId ?? request.AgentId?.Trim();
        if (string.IsNullOrWhiteSpace(agentId))
        {
            agentId = snapshotRequest.AgentId?.Trim();
        }

        if (string.IsNullOrWhiteSpace(agentId))
        {
            return Results.BadRequest(new { code = "agent_id_required", message = "AgentId is required." });
        }

        if (!string.Equals(snapshotRequest.AgentId, agentId, StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest(new { code = "agent_id_mismatch", message = "Configuration AgentId does not match the target Agent." });
        }

        // ServiceKey is protocol-internal. Never accept a management client's manual value.
        ReverseTunnelServiceKeyFactory.Apply(snapshotRequest.Configuration);
        var routeFailures = ReverseTunnelServiceKeyFactory.ValidateRoutes(snapshotRequest.Configuration);
        if (routeFailures.Count > 0)
        {
            return Results.BadRequest(new
            {
                code = "agent_route_configuration_invalid",
                message = "Agent public route configuration is invalid.",
                failures = routeFailures
            });
        }

        var saveResult = await desiredConfigurations.TrySaveAsync(
            agentId,
            snapshotRequest.Configuration,
            request.ExpectedVersion,
            snapshots => ReverseTunnelAgentConfigurationConflictValidator.Validate(
                agentId,
                snapshotRequest.Configuration,
                snapshots),
            context.RequestAborted);
        if (saveResult.VersionConflict)
        {
            return Results.Conflict(new
            {
                code = "configuration_version_conflict",
                message = "Configuration version does not match the current desired version."
            });
        }

        if (saveResult.Conflicts.Count > 0)
        {
            return Results.Conflict(new
            {
                code = "agent_configuration_conflict",
                message = "Agent ServiceKey or target mapping conflicts with an existing Agent configuration.",
                conflicts = saveResult.Conflicts
            });
        }

        var snapshot = saveResult.Snapshot!;
        if (session is null)
        {
            await agentRouteCatalog.RefreshAsync(context.RequestAborted);
            return Results.Json(new
            {
                snapshot.Version,
                snapshot.AgentId,
                queued = true,
                message = "Configuration saved and will be sent when the Agent reconnects."
            }, statusCode: StatusCodes.Status202Accepted);
        }

        await agentRouteCatalog.RefreshAsync(context.RequestAborted);
        var result = await session.PushConfigurationAsync(snapshot, TimeSpan.FromSeconds(30), context.RequestAborted);
        if (!result.Accepted)
        {
            return Results.Problem(result.FailureReason ?? "Agent rejected the configuration.", statusCode: StatusCodes.Status409Conflict);
        }

        return Results.Ok(result);
    }

    private static async Task<IResult> ReadClientConfigurationAsync(
        HttpContext context,
        string agentId,
        IReverseTunnelRuntimeOptionsAccessor runtimeOptions,
        IReverseTunnelDesiredAgentConfigurationStore desiredConfigurations,
        IReverseTunnelManagementAuthorizer authorizer)
    {
        if (await authorizer.AuthorizeAsync(context, runtimeOptions.Current.Management, context.RequestAborted).ConfigureAwait(false) is { } unauthorized)
        {
            return unauthorized;
        }

        var snapshot = await desiredConfigurations.GetAsync(agentId, context.RequestAborted);
        return snapshot is null
            ? Results.NotFound(new { code = "desired_configuration_not_found", message = "No desired configuration is stored for the Agent." })
            : Results.Ok(snapshot);
    }

    private static async Task<IResult> ReadClientConfigurationStatusAsync(
        HttpContext context,
        string agentId,
        IReverseTunnelRuntimeOptionsAccessor runtimeOptions,
        IReverseTunnelDesiredAgentConfigurationStore desiredConfigurations,
        IReverseTunnelDiagnosticsService diagnostics,
        IReverseTunnelManagementAuthorizer authorizer)
    {
        if (await authorizer.AuthorizeAsync(context, runtimeOptions.Current.Management, context.RequestAborted).ConfigureAwait(false) is { } unauthorized)
        {
            return unauthorized;
        }

        var normalizedAgentId = agentId?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedAgentId))
        {
            return Results.BadRequest(new { code = "agent_id_required", message = "AgentId is required." });
        }

        var desired = await desiredConfigurations.GetAsync(normalizedAgentId, context.RequestAborted);
        var sessions = diagnostics.GetSessions()
            .Where(item => string.Equals(item.AgentId, normalizedAgentId, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        return Results.Ok(new
        {
            agentId = normalizedAgentId,
            desiredVersion = desired?.Version ?? 0,
            desiredGeneratedAtUtc = desired?.GeneratedAtUtc,
            sessions
        });
    }

    private static bool IsManagementPort(HttpContext context, ReverseTunnelManagementOptions management)
    {
        if (!Uri.TryCreate(management.BindUrl, UriKind.Absolute, out var uri) || uri.Port <= 0)
        {
            return true;
        }

        return context.Connection.LocalPort == uri.Port;
    }

    private static bool IsLoopback(HttpContext context)
    {
        var remote = context.Connection.RemoteIpAddress;
        return remote is null || IPAddress.IsLoopback(remote);
    }

    private static string? ReadManagementToken(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-AirTunnel-Admin-Token", out var header) && header.Count == 1)
        {
            return header[0];
        }

        var authorization = context.Request.Headers.Authorization.FirstOrDefault();
        const string bearerPrefix = "Bearer ";
        return authorization?.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase) == true
            ? authorization[bearerPrefix.Length..]
            : null;
    }

    private static string ResolveControlUrl(HttpContext context)
    {
        return $"{context.Request.Scheme}://{context.Request.Host}";
    }

    private static string CreateManagementPageV2(string prefix)
    {
        const string resourceName = "Air.Cloud.Modules.ReverseTunnel.Management.management.html";
        using var stream = typeof(ReverseTunnelEndpointRouteBuilderExtensions).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded management page resource '{resourceName}' was not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd().Replace("__PREFIX__", prefix, StringComparison.Ordinal);
    }

    private static string CreateManagementPage(string prefix)
    {
        return $$"""
<!doctype html>
<html lang="zh-CN">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>AirTunnel Server 管理端</title>
  <style>
    body{font-family:system-ui,-apple-system,BlinkMacSystemFont,"Segoe UI",sans-serif;margin:24px;background:#f7f8fa;color:#1f2937}
    input,textarea,button{font:inherit}
    button{cursor:pointer;border:1px solid #2563eb;background:#2563eb;color:#fff;border-radius:6px;padding:8px 12px}
    button.secondary{border-color:#d1d5db;background:#fff;color:#1f2937}
    input{min-width:360px;border:1px solid #d1d5db;border-radius:6px;padding:8px 10px}
    textarea{width:100%;height:360px;border:1px solid #d1d5db;border-radius:6px;padding:10px}
    pre{white-space:pre-wrap;word-break:break-word;background:#111827;color:#e5e7eb;border-radius:8px;padding:12px;overflow:auto}
    .card{background:#fff;border:1px solid #e5e7eb;border-radius:10px;padding:16px;margin-bottom:16px}
    .row{display:flex;gap:8px;align-items:center;flex-wrap:wrap}
    .hint{color:#6b7280;font-size:13px}
    .error{color:#b91c1c;font-size:13px;margin-top:8px}
    .hidden{display:none}
    code{background:#f3f4f6;padding:2px 4px;border-radius:4px}
  </style>
</head>
<body>
  <h1>AirTunnel Server 管理端</h1>
  <div id="login" class="card">
    <p class="hint">使用当前控制面登录会话进入管理端；未接入控制面认证的宿主也可以输入兼容的 Server 管理 Token。</p>
    <div class="row">
      <label>Access Token <input id="token" type="password" placeholder="可选：X-AirTunnel-Admin-Token" autocomplete="off" /></label>
      <button onclick="unlock()">进入管理端</button>
    </div>
    <div id="loginError" class="error"></div>
  </div>
  <div id="workspace" class="hidden">
    <div class="card">
      <div class="row">
        <button onclick="loadOverview()">刷新状态</button>
        <button onclick="loadConfig()">读取配置</button>
        <button onclick="saveConfig()">保存配置</button>
        <button class="secondary" onclick="lock()">退出</button>
      </div>
      <p class="hint">管理 API 由宿主授权器鉴权；默认兼容 <code>X-AirTunnel-Admin-Token</code>，AirTunnel 独立端使用控制面登录会话。</p>
    </div>
    <div class="card"><h2>状态</h2><pre id="overview"></pre></div>
    <div class="card"><h2>配置</h2><textarea id="config"></textarea></div>
  </div>
  <script>
    const prefix = "{{prefix}}";
    let adminToken = '';
    function tokenInput(){ return document.getElementById('token'); }
    function headers(){
      const values = {'Content-Type':'application/json'};
      if(adminToken){ values['X-AirTunnel-Admin-Token'] = adminToken; }
      return values;
    }
    function setError(message){ document.getElementById('loginError').textContent = message || ''; }
    function showWorkspace(show){
      document.getElementById('login').classList.toggle('hidden', show);
      document.getElementById('workspace').classList.toggle('hidden', !show);
    }
    async function api(path, options){
      const requestOptions = options || {};
      const requestHeaders = Object.assign({}, headers(), requestOptions.headers || {});
      const response = await fetch(prefix + path, Object.assign({}, requestOptions, {headers: requestHeaders}));
      if(response.status === 401){
        lock('未登录、会话已失效或 Token 无效。');
        throw new Error('unauthorized');
      }
      if(!response.ok){
        throw new Error(await response.text());
      }
      return response;
    }
    async function unlock(){
      const token = tokenInput().value.trim();
      adminToken = token;
      try{
        const response = await api('/overview');
        document.getElementById('overview').textContent = JSON.stringify(await response.json(), null, 2);
        setError('');
        showWorkspace(true);
      }catch(error){
        if(error.message !== 'unauthorized'){
          setError('无法进入管理端：' + error.message);
        }
        showWorkspace(false);
      }
    }
    function lock(message){
      adminToken = '';
      document.getElementById('overview').textContent = '';
      document.getElementById('config').value = '';
      showWorkspace(false);
      setError(message || '');
      tokenInput().focus();
    }
    async function loadOverview(){
      const response = await api('/overview');
      document.getElementById('overview').textContent = JSON.stringify(await response.json(), null, 2);
    }
    async function loadConfig(){
      const response = await api('/config');
      const json = await response.json();
      document.getElementById('config').value = JSON.stringify(json.options, null, 2);
    }
    async function saveConfig(){
      const body = document.getElementById('config').value;
      const response = await api('/config', {method:'PUT', body});
      alert(response.status + ' ' + await response.text());
    }
    tokenInput().addEventListener('keydown', event => { if(event.key === 'Enter'){ unlock(); } });
    showWorkspace(false);
  </script>
</body>
</html>
""";
    }

    private static async Task InvokeFromGatewayAsync(
        HttpContext context,
        string serviceKey,
        string? path,
        IReverseTunnelRuntimeOptionsAccessor optionsAccessor,
        IReverseTunnelSessionRegistry sessionRegistry)
    {
        var options = optionsAccessor.Current;
        if (!options.Enabled || options.TunnelMode != ReverseTunnelMode.Server)
        {
            await WriteErrorAsync(context, HttpStatusCode.ServiceUnavailable, "reverse_tunnel_server_disabled", "Reverse tunnel Server mode is not enabled.");
            return;
        }

        if (!IsGatewayAccessTokenAccepted(context, options.Gateway))
        {
            await WriteErrorAsync(context, HttpStatusCode.Unauthorized, "gateway_access_denied", "Reverse tunnel Gateway access token is invalid.");
            return;
        }

        if (!sessionRegistry.TryGetForServiceKey(serviceKey, out var session) || session is null)
        {
            await WriteErrorAsync(context, HttpStatusCode.ServiceUnavailable, "agent_unavailable", $"No reverse tunnel Agent session is available for service '{serviceKey}'.");
            return;
        }

        var pathAndQuery = "/" + (path ?? string.Empty).TrimStart('/');
        if (context.Request.QueryString.HasValue)
        {
            pathAndQuery += context.Request.QueryString.Value;
        }

        var payload = new ReverseTunnelHttpRequestPayload
        {
            ServiceKey = serviceKey,
            Method = context.Request.Method,
            PathAndQuery = pathAndQuery,
            HasBody = HasRequestBody(context.Request),
            Headers = CollectAspNetRequestHeaders(context.Request)
        };

        var timeout = ResolveGatewayTimeout(context.Request);
        using var content = HasRequestBody(context.Request) ? new StreamContent(context.Request.Body) : null;
        var result = await session.InvokeAsync(payload, content, timeout, context.RequestAborted);
        if (!result.Succeeded || result.Response is null)
        {
            await WriteErrorAsync(context, ResolveFailureStatusCode(result.ErrorCode), result.ErrorCode ?? "agent_error", result.ErrorMessage ?? "Reverse tunnel Agent returned an error.");
            return;
        }

        await WriteTunnelResponseAsync(context, result);
    }

    private static bool IsGatewayAccessTokenAccepted(HttpContext context, ReverseTunnelGatewayOptions gateway)
    {
        if (string.IsNullOrWhiteSpace(gateway.AccessTokenEnvironmentVariable))
        {
            return true;
        }

        var expected = Environment.GetEnvironmentVariable(gateway.AccessTokenEnvironmentVariable);
        if (string.IsNullOrEmpty(expected))
        {
            return false;
        }

        return context.Request.Headers.TryGetValue(ReverseTunnelHttpHeaders.GatewayAccessTokenHeaderName, out var actual) &&
            actual.Count == 1 &&
            string.Equals(actual[0], expected, StringComparison.Ordinal);
    }

    private static List<ReverseTunnelHttpHeader> CollectAspNetRequestHeaders(HttpRequest request)
    {
        var headers = new List<ReverseTunnelHttpHeader>();
        foreach (var header in request.Headers)
        {
            if (ReverseTunnelHttpHeaders.HopByHopHeaders.Contains(header.Key) ||
                string.Equals(header.Key, ReverseTunnelHttpHeaders.GatewayAccessTokenHeaderName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(header.Key, ReverseTunnelHttpHeaders.GatewayTimeoutSecondsHeaderName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var value in header.Value)
            {
                headers.Add(new ReverseTunnelHttpHeader
                {
                    Name = header.Key,
                    Value = value ?? string.Empty
                });
            }
        }

        return headers;
    }

    private static bool HasRequestBody(HttpRequest request)
    {
        if (request.ContentLength is > 0)
        {
            return true;
        }

        return request.Headers.TransferEncoding.Any(value => string.Equals(value, "chunked", StringComparison.OrdinalIgnoreCase)) &&
            !HttpMethods.IsGet(request.Method) &&
            !HttpMethods.IsHead(request.Method) &&
            !HttpMethods.IsTrace(request.Method);
    }

    private static TimeSpan ResolveGatewayTimeout(HttpRequest request)
    {
        if (request.Headers.TryGetValue(ReverseTunnelHttpHeaders.GatewayTimeoutSecondsHeaderName, out var values) &&
            int.TryParse(values.FirstOrDefault(), out var seconds))
        {
            return TimeSpan.FromSeconds(Math.Clamp(seconds, 1, 3600));
        }

        return TimeSpan.FromSeconds(30);
    }

    private static HttpStatusCode ResolveFailureStatusCode(string? errorCode)
    {
        return errorCode switch
        {
            "agent_unavailable" => HttpStatusCode.ServiceUnavailable,
            "timeout" => HttpStatusCode.GatewayTimeout,
            "cancelled" => (HttpStatusCode)499,
            "too_many_concurrent_streams" => HttpStatusCode.TooManyRequests,
            "request_body_too_large" => HttpStatusCode.RequestEntityTooLarge,
            "response_body_too_large" => HttpStatusCode.BadGateway,
            "session_closed" => HttpStatusCode.ServiceUnavailable,
            "local_dispatch_failed" => HttpStatusCode.BadGateway,
            "invalid_agent_response" => HttpStatusCode.BadGateway,
            "gateway_stream_failed" => HttpStatusCode.BadGateway,
            _ => HttpStatusCode.BadGateway
        };
    }

    private static async Task WriteTunnelResponseAsync(HttpContext context, ReverseTunnelHttpExchangeResult result)
    {
        var response = result.Response!;
        context.Response.StatusCode = Math.Clamp(response.StatusCode, 100, 599);
        foreach (var header in response.Headers.Where(item => !string.IsNullOrWhiteSpace(item.Name) && !ReverseTunnelHttpHeaders.HopByHopHeaders.Contains(item.Name)))
        {
            context.Response.Headers.Append(header.Name, header.Value);
        }

        if (result.BodyReader is not null)
        {
            await foreach (var chunk in result.BodyReader.ReadAllAsync(context.RequestAborted))
            {
                await context.Response.Body.WriteAsync(chunk, context.RequestAborted);
            }
        }
        else if (result.Body is { Length: > 0 })
        {
            await context.Response.Body.WriteAsync(result.Body, context.RequestAborted);
        }
    }

    private static async Task WriteErrorAsync(HttpContext context, HttpStatusCode statusCode, string code, string message)
    {
        context.Response.StatusCode = (int)statusCode;
        await context.Response.WriteAsJsonAsync(new
        {
            code,
            message
        }, context.RequestAborted);
    }

    private static ReverseTunnelOptions ValidateServerEndpoint(IServiceProvider serviceProvider)
    {
        var options = serviceProvider.GetRequiredService<IOptions<ReverseTunnelOptions>>().Value;
        if (!options.Enabled)
        {
            throw new InvalidOperationException("Reverse tunnel Server endpoint mapping requires ReverseTunnelSettings:Enabled=true.");
        }

        if (options.TunnelMode != ReverseTunnelMode.Server)
        {
            throw new InvalidOperationException("Reverse tunnel Server endpoint mapping requires ReverseTunnelSettings:TunnelMode to be Server.");
        }

        var failures = serviceProvider
            .GetServices<IValidateOptions<ReverseTunnelOptions>>()
            .Select(validator => validator.Validate(Options.DefaultName, options))
            .Where(result => result.Failed)
            .SelectMany(result => result.Failures ?? [])
            .ToArray();
        if (failures.Length > 0)
        {
            throw new OptionsValidationException(Options.DefaultName, typeof(ReverseTunnelOptions), failures);
        }

        using var issuer = serviceProvider.GetRequiredService<IReverseTunnelCertificateAuthority>().LoadIssuerCertificate();
        return options;
    }
}
