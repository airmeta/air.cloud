using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using Air.Cloud.DynamicApp.Contracts.Identifiers;
using Air.Cloud.DynamicApp.Contracts.Lifecycle;
using Air.Cloud.DynamicApp.Contracts.Manifest;
using Air.Cloud.DynamicApp.Contracts.Runtime;
using Air.Cloud.DynamicApp.Gateway;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.DynamicApp.Manager;

/// <summary>
/// <para>zh-cn:DynamicApp Manager 内部管理 API 中间件，提供发布、部署、激活、停止、回滚、状态更新、日志和历史查询能力。</para>
/// <para>en-us:Internal DynamicApp Manager API middleware that provides publish, deploy, activate, stop, rollback, state update, log, and history query capabilities.</para>
/// </summary>
public sealed class DynamicAppManagerApiMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };
    private static readonly SemaphoreSlim DeploymentCreationGate = new(1, 1);

    private readonly RequestDelegate _next;
    private readonly DynamicAppManagerApiOptions _options;

    /// <summary>
    /// <para>zh-cn:创建 Manager API 中间件。</para>
    /// <para>en-us:Creates the Manager API middleware.</para>
    /// </summary>
    /// <param name="next">
    /// <para>zh-cn:下一个中间件。</para>
    /// <para>en-us:The next middleware.</para>
    /// </param>
    /// <param name="options">
    /// <para>zh-cn:Manager API 选项。</para>
    /// <para>en-us:The Manager API options.</para>
    /// </param>
    public DynamicAppManagerApiMiddleware(RequestDelegate next, DynamicAppManagerApiOptions options)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// <para>zh-cn:处理内部管理请求；非管理路径会继续传递给后续中间件，管理路径会把校验、包上传和运行期异常统一写成 JSON 错误响应。</para>
    /// <para>en-us:Handles internal management requests; non-management paths continue to the next middleware, while management-path validation, package-upload, and runtime exceptions are written as unified JSON error responses.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:当前 HTTP 上下文。</para>
    /// <para>en-us:The current HTTP context.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:异步处理任务。</para>
    /// <para>en-us:The asynchronous handling task.</para>
    /// </returns>
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!TryMatch(context.Request.Path, out var route))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        if (!IsAuthorized(context))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await WriteJsonAsync(context, DynamicAppApiResponse<object>.Failure("Unauthorized DynamicApp Manager request.", "dynamicapp.unauthorized")).ConfigureAwait(false);
            return;
        }

        try
        {
            await DispatchAsync(context, route).ConfigureAwait(false);
        }
        catch (ArgumentException exception)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await WriteJsonAsync(context, DynamicAppApiResponse<object>.Failure(exception.Message, "dynamicapp.bad_request")).ConfigureAwait(false);
        }
        catch (JsonException exception)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await WriteJsonAsync(context, DynamicAppApiResponse<object>.Failure(exception.Message, "dynamicapp.invalid_json")).ConfigureAwait(false);
        }
        catch (InvalidOperationException exception)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await WriteJsonAsync(context, DynamicAppApiResponse<object>.Failure(exception.Message, "dynamicapp.invalid_operation")).ConfigureAwait(false);
        }
        catch (InvalidDataException exception)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await WriteJsonAsync(context, DynamicAppApiResponse<object>.Failure(exception.Message, "dynamicapp.invalid_package")).ConfigureAwait(false);
        }
        catch (FileNotFoundException exception)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await WriteJsonAsync(context, DynamicAppApiResponse<object>.Failure(exception.Message, "dynamicapp.invalid_package")).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await WriteJsonAsync(context, DynamicAppApiResponse<object>.Failure(exception.Message, "dynamicapp.internal_error")).ConfigureAwait(false);
        }
    }

    private async Task DispatchAsync(HttpContext context, string route)
    {
        if (route.StartsWith("manifests/", StringComparison.OrdinalIgnoreCase) && HttpMethods.IsGet(context.Request.Method))
        {
            await ListManifestsAsync(context, route["manifests/".Length..]).ConfigureAwait(false);
            return;
        }

        if (route.StartsWith("target/", StringComparison.OrdinalIgnoreCase) && HttpMethods.IsGet(context.Request.Method))
        {
            await GetTargetAsync(context, route["target/".Length..]).ConfigureAwait(false);
            return;
        }

        if (route.Equals("routes", StringComparison.OrdinalIgnoreCase))
        {
            if (HttpMethods.IsGet(context.Request.Method))
            {
                await ListGatewayRoutesAsync(context).ConfigureAwait(false);
                return;
            }

            if (HttpMethods.IsPost(context.Request.Method))
            {
                await CreateGatewayRouteAsync(context).ConfigureAwait(false);
                return;
            }
        }

        if (route.Equals("routes/validate", StringComparison.OrdinalIgnoreCase)
            && HttpMethods.IsPost(context.Request.Method))
        {
            await ValidateGatewayRoutesAsync(context).ConfigureAwait(false);
            return;
        }

        if (route.StartsWith("routes/", StringComparison.OrdinalIgnoreCase))
        {
            await DispatchGatewayRouteAsync(context, route["routes/".Length..]).ConfigureAwait(false);
            return;
        }

        switch (route)
        {
            case "applications" when HttpMethods.IsGet(context.Request.Method):
                await ListApplicationsAsync(context).ConfigureAwait(false);
                return;

            case "targets" when HttpMethods.IsGet(context.Request.Method):
                await WriteJsonAsync(context, DynamicAppApiResponse<IReadOnlyCollection<DynamicAppRouteTarget>>.Success(
                    await ListTargetsWithRuntimeSyncAsync(context).ConfigureAwait(false),
                    "DynamicApp targets loaded.")).ConfigureAwait(false);
                return;

            case "containers" when HttpMethods.IsGet(context.Request.Method):
                await ListExistingContainersAsync(context).ConfigureAwait(false);
                return;

            case "self-healing" when HttpMethods.IsGet(context.Request.Method):
                await ListSelfHealingAsync(context).ConfigureAwait(false);
                return;

            case "self-healing/reset" when HttpMethods.IsPost(context.Request.Method):
                await ResetSelfHealingAsync(context).ConfigureAwait(false);
                return;

            case "archived" when HttpMethods.IsGet(context.Request.Method):
                await ListArchivedAsync(context).ConfigureAwait(false);
                return;

            case "packages" when HttpMethods.IsGet(context.Request.Method):
                await ListPackagesAsync(context).ConfigureAwait(false);
                return;

            case "deployments" when HttpMethods.IsGet(context.Request.Method):
                await ListDeploymentsAsync(context).ConfigureAwait(false);
                return;

            case "deployments/files" when HttpMethods.IsGet(context.Request.Method):
                await ListDeploymentFilesAsync(context).ConfigureAwait(false);
                return;

            case "deployments/orphans" when HttpMethods.IsGet(context.Request.Method):
                await ListOrphanDeploymentsAsync(context).ConfigureAwait(false);
                return;

            case "deployments/create" when HttpMethods.IsPost(context.Request.Method):
                await CreateDeploymentAsync(context).ConfigureAwait(false);
                return;

            case "deployments/update" when HttpMethods.IsPost(context.Request.Method):
                await UpdateDeploymentAsync(context).ConfigureAwait(false);
                return;

            case "deployments/remove" when HttpMethods.IsPost(context.Request.Method):
                await RemoveDeploymentAsync(context).ConfigureAwait(false);
                return;

            case "deployments/destroy" when HttpMethods.IsPost(context.Request.Method):
                await DestroyDeploymentAsync(context).ConfigureAwait(false);
                return;

            case "packages/applications" when HttpMethods.IsGet(context.Request.Method):
                await ListPackageApplicationsAsync(context).ConfigureAwait(false);
                return;

            case "packages/applications" when HttpMethods.IsPost(context.Request.Method):
                await CreatePackageApplicationAsync(context).ConfigureAwait(false);
                return;

            case "packages/mods" when HttpMethods.IsPost(context.Request.Method):
                await CreatePackageModAsync(context).ConfigureAwait(false);
                return;

            case "packages/mods/remove" when HttpMethods.IsPost(context.Request.Method):
                await RemovePackageModAsync(context).ConfigureAwait(false);
                return;

            case "packages/versions/remove" when HttpMethods.IsPost(context.Request.Method):
                await RemovePackageVersionAsync(context).ConfigureAwait(false);
                return;

            case "archived/remove" when HttpMethods.IsPost(context.Request.Method):
                await RemoveArchivedAsync(context).ConfigureAwait(false);
                return;

            case "manifests" when HttpMethods.IsGet(context.Request.Method):
                await ListAllManifestsAsync(context).ConfigureAwait(false);
                return;

            case "publish" when HttpMethods.IsPost(context.Request.Method):
                await PublishAsync(context).ConfigureAwait(false);
                return;

            case "deploy" when HttpMethods.IsPost(context.Request.Method):
                await DeployAsync(context).ConfigureAwait(false);
                return;

            case "mods/configure" when HttpMethods.IsPost(context.Request.Method):
                await ConfigureModAsync(context).ConfigureAwait(false);
                return;

            case "mods/upload" when HttpMethods.IsPost(context.Request.Method):
                await UploadModPackageAsync(context).ConfigureAwait(false);
                return;

            case "start" when HttpMethods.IsPost(context.Request.Method):
                await StartAsync(context).ConfigureAwait(false);
                return;

            case "restart" when HttpMethods.IsPost(context.Request.Method):
                await RestartAsync(context).ConfigureAwait(false);
                return;

            case "activate" when HttpMethods.IsPost(context.Request.Method):
                await ActivateAsync(context).ConfigureAwait(false);
                return;

            case "health" when HttpMethods.IsPost(context.Request.Method):
                await ProbeHealthAsync(context).ConfigureAwait(false);
                return;

            case "rollback" when HttpMethods.IsPost(context.Request.Method):
                await RollbackAsync(context).ConfigureAwait(false);
                return;

            case "stop" when HttpMethods.IsPost(context.Request.Method):
                await StopAsync(context).ConfigureAwait(false);
                return;

            case "archive" when HttpMethods.IsPost(context.Request.Method):
                await ArchiveAsync(context).ConfigureAwait(false);
                return;

            case "unarchive" when HttpMethods.IsPost(context.Request.Method):
                await UnarchiveAsync(context).ConfigureAwait(false);
                return;

            case "state" when HttpMethods.IsPost(context.Request.Method):
                await UpdateStateAsync(context).ConfigureAwait(false);
                return;

            case "logs" when HttpMethods.IsPost(context.Request.Method):
                await QueryLogsAsync(context).ConfigureAwait(false);
                return;

            default:
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                await WriteJsonAsync(context, DynamicAppApiResponse<object>.Failure($"DynamicApp Manager route `{route}` was not found.", "dynamicapp.route_not_found")).ConfigureAwait(false);
                return;
        }
    }

    private static Task ListGatewayRoutesAsync(HttpContext context)
    {
        var store = context.RequestServices.GetRequiredService<IDynamicAppGatewayRouteStore>();
        return WriteJsonAsync(context, DynamicAppApiResponse<IReadOnlyCollection<DynamicAppGatewayRoute>>.Success(
            store.List(), "DynamicApp Gateway routes loaded."));
    }

    private async Task CreateGatewayRouteAsync(HttpContext context)
    {
        var request = await ReadRequiredJsonAsync<DynamicAppGatewayRouteSaveRequest>(context).ConfigureAwait(false);
        var route = request.Route ?? throw new ArgumentException("Route is required.", nameof(request.Route));
        await WriteGatewayRouteAvailabilityAsync(context, route, request.ConfirmUnavailable).ConfigureAwait(false);
        if (context.Response.StatusCode == StatusCodes.Status409Conflict)
        {
            return;
        }

        var result = await context.RequestServices.GetRequiredService<IDynamicAppGatewayRouteStore>()
            .CreateAsync(route, context.RequestAborted).ConfigureAwait(false);
        await WriteOperationResultAsync(context, result, context.RequestServices.GetRequiredService<IDynamicAppGatewayRouteStore>().Get(route.RouteId)).ConfigureAwait(false);
    }

    private async Task ValidateGatewayRoutesAsync(HttpContext context)
    {
        var routes = await ReadRequiredJsonAsync<DynamicAppGatewayRoute[]>(context).ConfigureAwait(false);
        var errors = DynamicAppGatewayRouteValidator.Validate(routes);
        if (errors.Count > 0)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
        }

        if (errors.Count == 0)
        {
            await WriteJsonAsync(context, DynamicAppApiResponse<IReadOnlyCollection<string>>.Success(Array.Empty<string>(), "DynamicApp Gateway route configuration is valid.")).ConfigureAwait(false);
            return;
        }

        await WriteJsonAsync(context, new DynamicAppApiResponse<IReadOnlyCollection<string>>
        {
            Succeeded = false,
            ErrorCode = "dynamicapp.route_invalid",
            Message = string.Join(Environment.NewLine, errors),
            Data = errors
        }).ConfigureAwait(false);
    }

    private async Task DispatchGatewayRouteAsync(HttpContext context, string routeSegment)
    {
        var segments = routeSegment.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length == 0)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await WriteJsonAsync(context, DynamicAppApiResponse<object>.Failure("Route id is required.", "dynamicapp.route_not_found")).ConfigureAwait(false);
            return;
        }

        var routeId = Uri.UnescapeDataString(segments[0]);
        var store = context.RequestServices.GetRequiredService<IDynamicAppGatewayRouteStore>();
        if (segments.Length == 1 && HttpMethods.IsGet(context.Request.Method))
        {
            var route = store.Get(routeId);
            if (route == null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                await WriteJsonAsync(context, DynamicAppApiResponse<object>.Failure($"Route `{routeId}` was not found.", "dynamicapp.route_not_found")).ConfigureAwait(false);
                return;
            }

            await WriteJsonAsync(context, DynamicAppApiResponse<DynamicAppGatewayRoute>.Success(route, "DynamicApp Gateway route loaded.")).ConfigureAwait(false);
            return;
        }

        if (segments.Length == 1 && HttpMethods.IsDelete(context.Request.Method))
        {
            await WriteOperationResultAsync<object>(context, await store.RemoveAsync(routeId, context.RequestAborted).ConfigureAwait(false)).ConfigureAwait(false);
            return;
        }

        if (segments.Length == 2 && string.Equals(segments[1], "enabled", StringComparison.OrdinalIgnoreCase)
            && (HttpMethods.IsPut(context.Request.Method) || HttpMethods.IsPost(context.Request.Method)))
        {
            var request = await ReadRequiredJsonAsync<DynamicAppGatewayRouteEnabledRequest>(context).ConfigureAwait(false);
            await WriteOperationResultAsync<object>(context, await store.SetEnabledAsync(routeId, request.Enabled, request.ExpectedUpdatedAtUtc, context.RequestAborted).ConfigureAwait(false)).ConfigureAwait(false);
            return;
        }

        if (segments.Length == 2 && string.Equals(segments[1], "enable", StringComparison.OrdinalIgnoreCase) && HttpMethods.IsPost(context.Request.Method))
        {
            await WriteOperationResultAsync<object>(context, await store.SetEnabledAsync(routeId, true, cancellationToken: context.RequestAborted).ConfigureAwait(false)).ConfigureAwait(false);
            return;
        }

        if (segments.Length == 2 && string.Equals(segments[1], "disable", StringComparison.OrdinalIgnoreCase) && HttpMethods.IsPost(context.Request.Method))
        {
            await WriteOperationResultAsync<object>(context, await store.SetEnabledAsync(routeId, false, cancellationToken: context.RequestAborted).ConfigureAwait(false)).ConfigureAwait(false);
            return;
        }

        if (segments.Length == 1 && HttpMethods.IsPut(context.Request.Method))
        {
            var request = await ReadRequiredJsonAsync<DynamicAppGatewayRouteSaveRequest>(context).ConfigureAwait(false);
            var route = request.Route ?? throw new ArgumentException("Route is required.", nameof(request.Route));
            if (string.IsNullOrWhiteSpace(route.RouteId)) route.RouteId = routeId;
            if (!string.Equals(route.RouteId, routeId, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("RouteId in the path and body must match.", nameof(route.RouteId));
            }

            await WriteGatewayRouteAvailabilityAsync(context, route, request.ConfirmUnavailable).ConfigureAwait(false);
            if (context.Response.StatusCode == StatusCodes.Status409Conflict)
            {
                return;
            }

            await WriteOperationResultAsync(context, await store.UpdateAsync(route, request.ExpectedUpdatedAtUtc, context.RequestAborted).ConfigureAwait(false), store.Get(routeId)).ConfigureAwait(false);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status404NotFound;
        await WriteJsonAsync(context, DynamicAppApiResponse<object>.Failure($"DynamicApp route operation `{routeSegment}` was not found.", "dynamicapp.route_not_found")).ConfigureAwait(false);
    }

    private static async Task WriteGatewayRouteAvailabilityAsync(HttpContext context, DynamicAppGatewayRoute route, bool confirmed)
    {
        var validationErrors = DynamicAppGatewayRouteValidator.Validate(new[] { route });
        if (validationErrors.Count > 0)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await WriteJsonAsync(context, new DynamicAppApiResponse<IReadOnlyCollection<string>>
            {
                Succeeded = false,
                ErrorCode = "dynamicapp.route_invalid",
                Message = string.Join(Environment.NewLine, validationErrors),
                Data = validationErrors
            }).ConfigureAwait(false);
            return;
        }

        var stateStore = context.RequestServices.GetRequiredService<IDynamicAppStateStore>();
        var candidates = stateStore.ListTargets()
            .Where(target => string.Equals(target.Key.DynamicAppId.Value, route.TargetDynamicAppId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(target.Key.ModId.Value, route.TargetModId, StringComparison.OrdinalIgnoreCase)
                && (string.IsNullOrWhiteSpace(route.TargetVersion)
                    || string.Equals(target.Key.Version.Value, route.TargetVersion, StringComparison.OrdinalIgnoreCase))
                && (string.IsNullOrWhiteSpace(route.TargetDeploymentId)
                    || string.Equals(target.DeploymentId, route.TargetDeploymentId, StringComparison.OrdinalIgnoreCase)))
            .ToArray();
        var available = candidates.Any(target => target.State == DynamicModLifecycleState.Active && target.HealthStatus == DynamicModHealthStatus.Healthy);
        if (available || confirmed)
        {
            return;
        }

        context.Response.StatusCode = StatusCodes.Status409Conflict;
        var targetLabel = $"{route.TargetDynamicAppId}/{route.TargetModId}/{route.TargetVersion ?? "active"}";
        var notice = new DynamicAppGatewayRouteTargetNotice
        {
            Available = false,
            RequiresConfirmation = true,
            MatchingTargetCount = candidates.Length,
            Message = candidates.Length == 0
                ? $"Target `{targetLabel}` does not exist. Confirm to save this route anyway."
                : $"Target `{targetLabel}` is not Active and Healthy. Confirm to save this route anyway."
        };
        await WriteJsonAsync(context, new DynamicAppApiResponse<DynamicAppGatewayRouteTargetNotice>
        {
            Succeeded = false,
            ErrorCode = "dynamicapp.route_target_unavailable_confirmation_required",
            Message = notice.Message,
            Data = notice
        }).ConfigureAwait(false);
    }

    private async Task PublishAsync(HttpContext context)
    {
        var manifest = await ReadRequiredJsonAsync<DynamicAppManifest>(context).ConfigureAwait(false);
        var result = await context.RequestServices.GetRequiredService<IDynamicAppManager>()
            .PublishAsync(manifest, context.RequestAborted)
            .ConfigureAwait(false);
        await WriteOperationResultAsync<object>(context, result).ConfigureAwait(false);
    }

    private async Task DeployAsync(HttpContext context)
    {
        var coordinator = context.RequestServices.GetService<IDynamicAppHostCoordinator>();
        if (coordinator == null)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await WriteJsonAsync(context, DynamicAppApiResponse<object>.Failure("IDynamicAppHostCoordinator is not registered.", "dynamicapp.host_not_registered")).ConfigureAwait(false);
            return;
        }

        var manifest = await ReadRequiredJsonAsync<DynamicAppManifest>(context).ConfigureAwait(false);
        var result = await coordinator.DeployAsync(manifest, context.RequestAborted).ConfigureAwait(false);
        await WriteOperationResultAsync<object>(context, result).ConfigureAwait(false);
    }

    /// <summary>
    /// <para>zh-cn:按 JSON 请求动态配置一个 Mod；根据请求中的 Deploy 标志执行发布或部署。</para>
    /// <para>en-us:Dynamically configures one Mod from a JSON request and publishes or deploys it according to the Deploy flag.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:当前 HTTP 上下文，Body 必须是 DynamicAppConfigureModRequest。</para>
    /// <para>en-us:The current HTTP context; the body must be DynamicAppConfigureModRequest.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:异步处理任务。</para>
    /// <para>en-us:The asynchronous handling task.</para>
    /// </returns>
    private async Task ConfigureModAsync(HttpContext context)
    {
        var request = await ReadRequiredJsonAsync<DynamicAppConfigureModRequest>(context).ConfigureAwait(false);
        var response = await ConfigureModAsync(context, request, package: null).ConfigureAwait(false);
        await WriteConfigureResultAsync(context, response).ConfigureAwait(false);
    }

    /// <summary>
    /// <para>zh-cn:接收 multipart zip 包上传并动态配置 Process 模式 Mod；包保存成功后生成 Manifest 并发布或部署。</para>
    /// <para>en-us:Accepts a multipart zip upload and dynamically configures a Process-mode Mod; after saving the package it generates a manifest and publishes or deploys it.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:当前 HTTP 上下文，必须使用 multipart/form-data 并包含 package 文件字段。</para>
    /// <para>en-us:The current HTTP context; it must use multipart/form-data and include a package file field.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:异步处理任务。</para>
    /// <para>en-us:The asynchronous handling task.</para>
    /// </returns>
    private async Task UploadModPackageAsync(HttpContext context)
    {
        if (!context.Request.HasFormContentType)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await WriteJsonAsync(context, DynamicAppApiResponse<object>.Failure("Mod upload must use multipart/form-data.", "dynamicapp.invalid_form")).ConfigureAwait(false);
            return;
        }

        var form = await context.Request.ReadFormAsync(context.RequestAborted).ConfigureAwait(false);
        var file = form.Files["package"] ?? form.Files.FirstOrDefault();
        if (file == null || file.Length == 0)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await WriteJsonAsync(context, DynamicAppApiResponse<object>.Failure("Mod upload must include a non-empty package file.", "dynamicapp.package_required")).ConfigureAwait(false);
            return;
        }

        var dynamicAppId = ReadRequiredFormString(form, "dynamicAppId");
        var modId = new DynamicModId(ReadRequiredFormString(form, "modId"));
        var version = new DynamicAppVersion(ReadRequiredFormString(form, "version"));
        var key = new DynamicModVersionKey(new DynamicAppId(dynamicAppId), modId, version);
        var entryFile = ReadFormString(form, "entryFile", string.Empty);
        await using var stream = file.OpenReadStream();
        var package = await context.RequestServices.GetRequiredService<IDynamicAppModPackageStore>()
            .SaveAsync(new DynamicAppModPackageSaveRequest
            {
                Key = key,
                OriginalFileName = file.FileName,
                EntryFile = entryFile,
                Overwrite = ReadFormBool(form, "overwrite", fallback: false)
            }, stream, context.RequestAborted)
            .ConfigureAwait(false);

        await WriteJsonAsync(context, DynamicAppApiResponse<DynamicAppModPackageRecord>.Success(
            package,
            $"DynamicApp package `{key}` was uploaded and extracted. Configure it from Application Runtime to create an instance.")).ConfigureAwait(false);
    }

    /// <summary>
    /// <para>zh-cn:执行单 Mod 配置动作；入口会先生成 Manifest，再按执行模式拆分为 Process、Container、ExistingContainer 或 LocalDevelopment 的配置链路。</para>
    /// <para>en-us:Executes a single-Mod configuration action; the entry first generates a manifest and then dispatches to the Process, Container, ExistingContainer, or LocalDevelopment configuration path by execution mode.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:当前 HTTP 上下文。</para>
    /// <para>en-us:The current HTTP context.</para>
    /// </param>
    /// <param name="request">
    /// <para>zh-cn:单 Mod 配置请求。</para>
    /// <para>en-us:The single-Mod configuration request.</para>
    /// </param>
    /// <param name="package">
    /// <para>zh-cn:可选包保存记录；纯配置请求传入 null。</para>
    /// <para>en-us:Optional package save record; pass null for configuration-only requests.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:配置结果。</para>
    /// <para>en-us:The configuration result.</para>
    /// </returns>
    private static async Task<DynamicAppConfigureModResult> ConfigureModAsync(HttpContext context, DynamicAppConfigureModRequest request, DynamicAppModPackageRecord? package)
    {
        var manifest = request.ToManifest();
        var operation = await ConfigureManifestByExecutionModeAsync(context, request, manifest).ConfigureAwait(false);

        return new DynamicAppConfigureModResult
        {
            Manifest = manifest,
            Package = package,
            Operation = operation
        };
    }

    /// <summary>
    /// <para>zh-cn:根据 Mod 执行模式分发动态配置操作；不同模式的登记、部署和运行态写入边界在这里被显式隔离，避免继续用 Deploy 标志做单线处理。</para>
    /// <para>en-us:Dispatches dynamic configuration by Mod execution mode; registration, deployment, and runtime-state boundaries are explicitly separated here instead of continuing the old single path driven only by the Deploy flag.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:当前 HTTP 上下文，用于解析依赖服务和请求取消令牌。</para>
    /// <para>en-us:The current HTTP context used to resolve dependent services and the request cancellation token.</para>
    /// </param>
    /// <param name="request">
    /// <para>zh-cn:动态配置请求；其中 Deploy 表示该类型是否进入立即部署链路。</para>
    /// <para>en-us:The dynamic configuration request; its Deploy flag indicates whether this execution mode should enter the immediate deployment path.</para>
    /// </param>
    /// <param name="manifest">
    /// <para>zh-cn:由请求生成的单 Mod Manifest。</para>
    /// <para>en-us:The single-Mod manifest generated from the request.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:按执行模式处理后的操作结果。</para>
    /// <para>en-us:The operation result after execution-mode-specific processing.</para>
    /// </returns>
    private static Task<DynamicAppOperationResult> ConfigureManifestByExecutionModeAsync(HttpContext context, DynamicAppConfigureModRequest request, DynamicAppManifest manifest)
    {
        if (manifest.Mods.Count != 1)
        {
            throw new ArgumentException("DynamicApp configure request must contain exactly one Mod.", nameof(manifest));
        }

        var mod = manifest.Mods[0];

        return mod.ExecutionMode switch
        {
            DynamicModExecutionMode.Process => ConfigureProcessManifestAsync(context, request, manifest),
            DynamicModExecutionMode.Container => ConfigureContainerManifestAsync(context, request, manifest),
            DynamicModExecutionMode.ExistingContainer => ConfigureExistingContainerManifestAsync(context, request, manifest),
            DynamicModExecutionMode.LocalDevelopment => ConfigureLocalDevelopmentManifestAsync(context, request, manifest),
            _ => Task.FromResult(DynamicAppOperationResult.Failure($"DynamicApp execution mode `{mod.ExecutionMode}` is not supported."))
        };
    }

    /// <summary>
    /// <para>zh-cn:处理 Process 模式配置；Deploy 为 true 时启动子进程并激活，Deploy 为 false 时只保存包与 Manifest，不写运行态。</para>
    /// <para>en-us:Handles Process-mode configuration; Deploy true starts and activates the child process, while Deploy false only saves the package and manifest without writing runtime state.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:当前 HTTP 上下文，用于访问 Host 协调器或 Manifest 存储。</para>
    /// <para>en-us:The current HTTP context used to access the Host coordinator or manifest store.</para>
    /// </param>
    /// <param name="request">
    /// <para>zh-cn:动态配置请求，Deploy 控制是否立即部署。</para>
    /// <para>en-us:The dynamic configuration request whose Deploy flag controls immediate deployment.</para>
    /// </param>
    /// <param name="manifest">
    /// <para>zh-cn:Process 模式 Manifest。</para>
    /// <para>en-us:The Process-mode manifest.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:Process 模式配置结果。</para>
    /// <para>en-us:The Process-mode configuration result.</para>
    /// </returns>
    private static Task<DynamicAppOperationResult> ConfigureProcessManifestAsync(HttpContext context, DynamicAppConfigureModRequest request, DynamicAppManifest manifest)
    {
        return request.Deploy
            ? DeployConfiguredManifestAsync(context, manifest)
            : RegisterConfiguredManifestAsync(context, manifest);
    }

    /// <summary>
    /// <para>zh-cn:处理托管 Container 模式配置；Deploy 为 true 时交给容器编排器部署，Deploy 为 false 时仅登记 Manifest。</para>
    /// <para>en-us:Handles managed Container-mode configuration; Deploy true delegates to the container orchestrator, while Deploy false only registers the manifest.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:当前 HTTP 上下文，用于访问 Host 协调器或 Manifest 存储。</para>
    /// <para>en-us:The current HTTP context used to access the Host coordinator or manifest store.</para>
    /// </param>
    /// <param name="request">
    /// <para>zh-cn:动态配置请求，Deploy 控制是否立即部署。</para>
    /// <para>en-us:The dynamic configuration request whose Deploy flag controls immediate deployment.</para>
    /// </param>
    /// <param name="manifest">
    /// <para>zh-cn:Container 模式 Manifest。</para>
    /// <para>en-us:The Container-mode manifest.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:Container 模式配置结果。</para>
    /// <para>en-us:The Container-mode configuration result.</para>
    /// </returns>
    private static Task<DynamicAppOperationResult> ConfigureContainerManifestAsync(HttpContext context, DynamicAppConfigureModRequest request, DynamicAppManifest manifest)
    {
        return request.Deploy
            ? DeployConfiguredManifestAsync(context, manifest)
            : RegisterConfiguredManifestAsync(context, manifest);
    }

    /// <summary>
    /// <para>zh-cn:处理 ExistingContainer 模式配置；登记已有容器时只持久化 Manifest 和容器名绑定，只有显式 Deploy 时才调用容器引擎生命周期编排。</para>
    /// <para>en-us:Handles ExistingContainer-mode configuration; registering an existing container only persists the manifest and container-name binding, and Docker lifecycle orchestration runs only when Deploy is explicitly true.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:当前 HTTP 上下文，用于访问 Host 协调器或 Manifest 存储。</para>
    /// <para>en-us:The current HTTP context used to access the Host coordinator or manifest store.</para>
    /// </param>
    /// <param name="request">
    /// <para>zh-cn:动态配置请求，Deploy 控制是否显式接管并启动已有容器。</para>
    /// <para>en-us:The dynamic configuration request whose Deploy flag controls whether the existing container is explicitly adopted and started.</para>
    /// </param>
    /// <param name="manifest">
    /// <para>zh-cn:ExistingContainer 模式 Manifest。</para>
    /// <para>en-us:The ExistingContainer-mode manifest.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:ExistingContainer 模式配置结果。</para>
    /// <para>en-us:The ExistingContainer-mode configuration result.</para>
    /// </returns>
    private static Task<DynamicAppOperationResult> ConfigureExistingContainerManifestAsync(HttpContext context, DynamicAppConfigureModRequest request, DynamicAppManifest manifest)
    {
        return request.Deploy
            ? DeployConfiguredManifestAsync(context, manifest)
            : RegisterConfiguredManifestAsync(context, manifest);
    }

    /// <summary>
    /// <para>zh-cn:处理 LocalDevelopment 模式配置；Deploy 为 true 时注册外部调试目标并执行健康探测，Deploy 为 false 时只保存联调 Manifest。</para>
    /// <para>en-us:Handles LocalDevelopment-mode configuration; Deploy true registers the external debug target and probes health, while Deploy false only saves the debugging manifest.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:当前 HTTP 上下文，用于访问 Host 协调器或 Manifest 存储。</para>
    /// <para>en-us:The current HTTP context used to access the Host coordinator or manifest store.</para>
    /// </param>
    /// <param name="request">
    /// <para>zh-cn:动态配置请求，Deploy 控制是否立即挂载本地联调目标。</para>
    /// <para>en-us:The dynamic configuration request whose Deploy flag controls whether the local debug target is mounted immediately.</para>
    /// </param>
    /// <param name="manifest">
    /// <para>zh-cn:LocalDevelopment 模式 Manifest。</para>
    /// <para>en-us:The LocalDevelopment-mode manifest.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:LocalDevelopment 模式配置结果。</para>
    /// <para>en-us:The LocalDevelopment-mode configuration result.</para>
    /// </returns>
    private static Task<DynamicAppOperationResult> ConfigureLocalDevelopmentManifestAsync(HttpContext context, DynamicAppConfigureModRequest request, DynamicAppManifest manifest)
    {
        return request.Deploy
            ? DeployConfiguredManifestAsync(context, manifest)
            : RegisterConfiguredManifestAsync(context, manifest);
    }

    /// <summary>
    /// <para>zh-cn:仅登记动态配置生成的 Manifest；该路径会校验并持久化 Manifest，但不会写入 Runtime Target，也不会启动、停止或激活任何 Mod。</para>
    /// <para>en-us:Registers only the manifest generated by dynamic configuration; this path validates and persists the manifest without writing runtime targets or starting, stopping, or activating any Mod.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:当前 HTTP 上下文，用于从依赖注入容器获取 Manifest 校验器和存储，并传递请求取消令牌。</para>
    /// <para>en-us:The current HTTP context used to resolve the manifest validator and store from dependency injection and to pass the request cancellation token.</para>
    /// </param>
    /// <param name="manifest">
    /// <para>zh-cn:需要登记保存的 DynamicApp Manifest。</para>
    /// <para>en-us:The DynamicApp manifest to register and save.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:登记操作结果；校验失败时返回失败信息，保存成功时返回成功信息。</para>
    /// <para>en-us:The registration operation result; validation failures return failure messages and successful saves return a success message.</para>
    /// </returns>
    private static async Task<DynamicAppOperationResult> RegisterConfiguredManifestAsync(HttpContext context, DynamicAppManifest manifest)
    {
        if (context.RequestAborted.IsCancellationRequested)
        {
            return DynamicAppOperationResult.Failure("Manifest registration was cancelled.");
        }

        var validation = context.RequestServices.GetRequiredService<IDynamicAppManifestValidator>().Validate(manifest);
        if (!validation.IsValid)
        {
            return DynamicAppOperationResult.Failure(string.Join(Environment.NewLine, validation.Errors));
        }

        await context.RequestServices.GetRequiredService<IDynamicAppManifestStore>()
            .SaveAsync(manifest, context.RequestAborted)
            .ConfigureAwait(false);

        return DynamicAppOperationResult.Success($"DynamicApp `{manifest.Id}` manifest registered with {manifest.Mods.Count} Mods.");
    }

    private static async Task<DynamicModManifest?> FindLatestManifestModMetadataAsync(HttpContext context, string dynamicAppId, string modId)
    {
        var manifestStore = context.RequestServices.GetService<IDynamicAppManifestStore>();
        if (manifestStore == null)
        {
            return null;
        }

        var records = await manifestStore.ListAsync(new DynamicAppId(dynamicAppId), context.RequestAborted).ConfigureAwait(false);
        var mods = records
            .OrderByDescending(record => record.SavedAtUtc)
            .SelectMany(record => record.Manifest.Mods)
            .Where(mod => string.Equals(mod.Id.Value, modId, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (mods.Length == 0)
        {
            return null;
        }

        var latest = mods[0];
        var startupTimeoutSeconds = mods.Select(mod => mod.Health.StartupTimeoutSeconds).FirstOrDefault(value => value > 0);
        return new DynamicModManifest
        {
            Id = latest.Id,
            Version = latest.Version,
            ExecutionMode = latest.ExecutionMode,
            Artifact = new DynamicModArtifactManifest
            {
                File = FirstNotEmpty(mods.Select(mod => mod.Artifact.File)),
                ContainerName = FirstNotEmpty(mods.Select(mod => mod.Artifact.ContainerName)),
                Arguments = FirstNotEmpty(mods.Select(mod => mod.Artifact.Arguments)),
                Endpoint = FirstNotEmpty(mods.Select(mod => mod.Artifact.Endpoint))
            },
            Health = new DynamicModHealthManifest
            {
                Path = FirstNotEmpty(mods.Select(mod => mod.Health.Path)) ?? "/Health",
                StartupTimeoutSeconds = startupTimeoutSeconds > 0 ? startupTimeoutSeconds : 60
            },
        };
    }

    private static string? FirstNotEmpty(IEnumerable<string?> values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private static string GetFileNameOrEmpty(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        return Path.GetFileName(path);
    }

    /// <summary>
    /// <para>zh-cn:部署动态配置生成的 Manifest；Host 协调器不存在时返回失败而不抛出空引用异常。</para>
    /// <para>en-us:Deploys a manifest generated from dynamic configuration; returns failure instead of throwing a null-reference exception when the Host coordinator is absent.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:当前 HTTP 上下文。</para>
    /// <para>en-us:The current HTTP context.</para>
    /// </param>
    /// <param name="manifest">
    /// <para>zh-cn:需要部署的 Manifest。</para>
    /// <para>en-us:The manifest to deploy.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:部署操作结果。</para>
    /// <para>en-us:The deployment operation result.</para>
    /// </returns>
    private static async Task<DynamicAppOperationResult> DeployConfiguredManifestAsync(HttpContext context, DynamicAppManifest manifest)
    {
        var coordinator = context.RequestServices.GetService<IDynamicAppHostCoordinator>();
        return coordinator == null
            ? DynamicAppOperationResult.Failure("IDynamicAppHostCoordinator is not registered.")
            : await coordinator.DeployAsync(manifest, context.RequestAborted).ConfigureAwait(false);
    }

    /// <summary>
    /// <para>zh-cn:从 multipart 表单字段构建单 Mod 配置请求；缺省字段会从同应用同 Mod 最近 Manifest 中继承，便于更新包时只提交必要字段。</para>
    /// <para>en-us:Builds a single-Mod configuration request from multipart form fields; missing fields inherit from the latest manifest for the same application and Mod so package updates can submit only required fields.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:当前 HTTP 上下文，用于读取 Manifest 存储和取消令牌。</para>
    /// <para>en-us:The current HTTP context used to read the manifest store and cancellation token.</para>
    /// </param>
    /// <param name="form">
    /// <para>zh-cn:上传请求表单。</para>
    /// <para>en-us:The upload request form.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:单 Mod 配置请求。</para>
    /// <para>en-us:The single-Mod configuration request.</para>
    /// </returns>
    private static async Task<DynamicAppConfigureModRequest> BuildConfigureModRequestAsync(HttpContext context, IFormCollection form)
    {
        var dynamicAppId = ReadRequiredFormString(form, "dynamicAppId");
        var modId = new DynamicModId(ReadRequiredFormString(form, "modId"));
        var version = new DynamicAppVersion(ReadRequiredFormString(form, "version"));
        var inherited = await FindLatestManifestModMetadataAsync(context, dynamicAppId, modId.Value).ConfigureAwait(false);
        var executionMode = ReadFormEnum(form, "executionMode", DynamicModExecutionMode.Process);
        return new DynamicAppConfigureModRequest
        {
            DynamicAppId = dynamicAppId,
            AppVersion = ReadFormString(form, "appVersion", version.Value),
            GatewayPort = ReadFormInt(form, "gatewayPort", 8080),
            ManagerPort = ReadFormInt(form, "managerPort", 8081),
            ManagerInternalOnly = ReadFormBool(form, "managerInternalOnly", fallback: true),
            Deploy = ReadFormBool(form, "deploy", fallback: false),
            Mod = new DynamicModManifest
            {
                Id = modId,
                Version = version,
                ExecutionMode = executionMode,
                Artifact = new DynamicModArtifactManifest
                {
                    File = ReadFormString(form, "entryFile", GetFileNameOrEmpty(inherited?.Artifact.File)),
                    ContainerName = ReadFormNullableString(form, "containerName") ?? inherited?.Artifact.ContainerName,
                    Arguments = ReadFormNullableString(form, "arguments") ?? inherited?.Artifact.Arguments,
                    Endpoint = ReadFormNullableString(form, "endpoint") ?? inherited?.Artifact.Endpoint
                },
                Health = new DynamicModHealthManifest
                {
                    Path = ReadFormString(form, "healthPath", inherited?.Health.Path ?? "/Health"),
                    StartupTimeoutSeconds = ReadFormInt(form, "startupTimeoutSeconds", inherited?.Health.StartupTimeoutSeconds ?? 60)
                },
            }
        };
    }

    /// <summary>
    /// <para>zh-cn:写出单 Mod 配置响应；底层操作失败时保持 HTTP 400，成功时保持 HTTP 200。</para>
    /// <para>en-us:Writes a single-Mod configuration response; failed underlying operations use HTTP 400 while successful operations keep HTTP 200.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:当前 HTTP 上下文。</para>
    /// <para>en-us:The current HTTP context.</para>
    /// </param>
    /// <param name="result">
    /// <para>zh-cn:单 Mod 配置结果。</para>
    /// <para>en-us:The single-Mod configuration result.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:异步写出任务。</para>
    /// <para>en-us:The asynchronous write task.</para>
    /// </returns>
    private static Task WriteConfigureResultAsync(HttpContext context, DynamicAppConfigureModResult result)
    {
        if (!result.Operation.Succeeded && context.Response.StatusCode == StatusCodes.Status200OK)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
        }

        return WriteJsonAsync(context, DynamicAppApiResponse<DynamicAppConfigureModResult>.FromOperation(result.Operation, result));
    }

    private static async Task ListExistingContainersAsync(HttpContext context)
    {
        var catalog = context.RequestServices.GetService<IDynamicAppExistingContainerCatalog>();
        if (catalog == null)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await WriteJsonAsync(context, DynamicAppApiResponse<object>.Failure("IDynamicAppExistingContainerCatalog is not registered.", "dynamicapp.container_catalog_not_registered")).ConfigureAwait(false);
            return;
        }

        var containers = await catalog.ListAsync(context.RequestAborted).ConfigureAwait(false);
        await WriteJsonAsync(context, DynamicAppApiResponse<IReadOnlyCollection<DynamicAppExistingContainerView>>.Success(containers, "DynamicApp existing containers loaded.")).ConfigureAwait(false);
    }

    /// <summary>
    /// <para>zh-cn:启动已发布 Manifest 中的单个 Mod 版本，并在健康探测通过后激活该版本。</para>
    /// <para>en-us:Starts a single Mod version from published manifest history and activates it after the health probe succeeds.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:当前 HTTP 上下文，Body 必须包含 DynamicAppTargetKeyRequest。</para>
    /// <para>en-us:The current HTTP context; the body must contain DynamicAppTargetKeyRequest.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:异步处理任务。</para>
    /// <para>en-us:The asynchronous handling task.</para>
    /// </returns>
    private async Task StartAsync(HttpContext context)
    {
        var request = await ReadRequiredJsonAsync<DynamicAppTargetKeyRequest>(context).ConfigureAwait(false);
        var runtimeKey = request.ToRuntimeKey();
        await using var runtimeOperation = await context.RequestServices
            .GetRequiredService<IDynamicAppRuntimeOperationCoordinator>()
            .AcquireAsync(runtimeKey, context.RequestAborted)
            .ConfigureAwait(false);
        var result = await StartTargetAsync(context, runtimeKey).ConfigureAwait(false);
        await WriteOperationResultAsync<object>(context, result).ConfigureAwait(false);
    }

    /// <summary>
    /// <para>zh-cn:重启单个 Mod 版本；先停止当前目标，再基于 Manifest 历史重新启动并健康激活。</para>
    /// <para>en-us:Restarts a single Mod version; it stops the current target first, then starts and activates it from manifest history after health probing.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:当前 HTTP 上下文，Body 必须包含 DynamicAppTargetKeyRequest。</para>
    /// <para>en-us:The current HTTP context; the body must contain DynamicAppTargetKeyRequest.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:异步处理任务。</para>
    /// <para>en-us:The asynchronous handling task.</para>
    /// </returns>
    private async Task RestartAsync(HttpContext context)
    {
        var request = await ReadRequiredJsonAsync<DynamicAppTargetKeyRequest>(context).ConfigureAwait(false);
        var runtimeKey = request.ToRuntimeKey();
        await using var runtimeOperation = await context.RequestServices
            .GetRequiredService<IDynamicAppRuntimeOperationCoordinator>()
            .AcquireAsync(runtimeKey, context.RequestAborted)
            .ConfigureAwait(false);
        var stop = await StopTargetAsync(context, runtimeKey).ConfigureAwait(false);
        if (!stop.Succeeded)
        {
            await WriteOperationResultAsync<object>(context, stop).ConfigureAwait(false);
            return;
        }

        var start = await StartTargetAsync(context, runtimeKey).ConfigureAwait(false);
        await WriteOperationResultAsync<object>(context, start).ConfigureAwait(false);
    }

    private async Task ActivateAsync(HttpContext context)
    {
        var request = await ReadRequiredJsonAsync<DynamicAppTargetKeyRequest>(context).ConfigureAwait(false);
        await using var operation = await context.RequestServices
            .GetRequiredService<IDynamicAppRuntimeOperationCoordinator>()
            .AcquirePackageAsync(request.ToKey(), context.RequestAborted)
            .ConfigureAwait(false);
        var result = await context.RequestServices.GetRequiredService<IDynamicAppManager>()
            .ActivateAsync(request.ToKey(), context.RequestAborted)
            .ConfigureAwait(false);
        await WriteOperationResultAsync<object>(context, result).ConfigureAwait(false);
    }

    private async Task RollbackAsync(HttpContext context)
    {
        var request = await ReadRequiredJsonAsync<DynamicAppRollbackRequest>(context).ConfigureAwait(false);
        var dynamicAppId = request.ToDynamicAppId();
        var modId = request.ToModId();
        if (await IsExistingContainerModAsync(context, dynamicAppId, modId).ConfigureAwait(false))
        {
            await WriteOperationResultAsync<object>(
                context,
                DynamicAppOperationResult.Failure($"ExistingContainer Mod `{dynamicAppId}/{modId}` cannot be rolled back; manage its Docker container lifecycle with start, stop, or restart."))
                .ConfigureAwait(false);
            return;
        }

        var result = await context.RequestServices.GetRequiredService<IDynamicAppManager>()
            .RollbackAsync(dynamicAppId, modId, context.RequestAborted)
            .ConfigureAwait(false);
        await WriteOperationResultAsync<object>(context, result).ConfigureAwait(false);
    }

    private async Task StopAsync(HttpContext context)
    {
        var request = await ReadRequiredJsonAsync<DynamicAppTargetKeyRequest>(context).ConfigureAwait(false);
        var runtimeKey = request.ToRuntimeKey();
        await using var operation = await context.RequestServices
            .GetRequiredService<IDynamicAppRuntimeOperationCoordinator>()
            .AcquireAsync(runtimeKey, context.RequestAborted)
            .ConfigureAwait(false);
        var result = await StopTargetAsync(context, runtimeKey).ConfigureAwait(false);
        if (result.Succeeded)
        {
            context.RequestServices.GetRequiredService<IDynamicAppSelfHealingStateStore>().Remove(runtimeKey);
        }

        await WriteOperationResultAsync<object>(context, result).ConfigureAwait(false);
    }

    /// <summary>
    /// <para>zh-cn:判断指定 DynamicApp/Mod 当前登记的最新 Manifest 是否为 ExistingContainer 模式；用于阻断回滚等不适合接管容器的历史版本操作。</para>
    /// <para>en-us:Determines whether the latest registered manifest for the specified DynamicApp/Mod is ExistingContainer mode; this blocks historical-version operations such as rollback that are not valid for adopted containers.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:当前 HTTP 上下文，用于读取 Manifest 历史存储和请求取消令牌。</para>
    /// <para>en-us:The current HTTP context used to read manifest history storage and the request cancellation token.</para>
    /// </param>
    /// <param name="dynamicAppId">
    /// <para>zh-cn:DynamicApp 应用标识。</para>
    /// <para>en-us:The DynamicApp application identifier.</para>
    /// </param>
    /// <param name="modId">
    /// <para>zh-cn:Mod 标识。</para>
    /// <para>en-us:The Mod identifier.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:若最新登记的同名 Mod 为 ExistingContainer 模式则返回 true；没有 Manifest 历史时返回 false。</para>
    /// <para>en-us:Returns true when the latest registered Mod with the same name is ExistingContainer mode; returns false when no manifest history exists.</para>
    /// </returns>
    private static async Task<bool> IsExistingContainerModAsync(HttpContext context, DynamicAppId dynamicAppId, DynamicModId modId)
    {
        var records = await context.RequestServices.GetRequiredService<IDynamicAppManifestStore>()
            .ListAsync(dynamicAppId, context.RequestAborted)
            .ConfigureAwait(false);
        var latestMod = records
            .OrderByDescending(record => record.SavedAtUtc)
            .SelectMany(record => record.Manifest.Mods)
            .FirstOrDefault(mod => mod.Id == modId);

        return latestMod?.ExecutionMode == DynamicModExecutionMode.ExistingContainer;
    }

    /// <summary>
    /// <para>zh-cn:对指定 Mod 版本执行一次即时健康探测，并把探测结果回写到运行状态存储。</para>
    /// <para>en-us:Runs an immediate health probe for the specified Mod version and writes the probe result back to runtime state storage.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:当前 HTTP 上下文，Body 必须包含 DynamicAppTargetKeyRequest。</para>
    /// <para>en-us:The current HTTP context; the body must contain DynamicAppTargetKeyRequest.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:异步处理任务。</para>
    /// <para>en-us:The asynchronous handling task.</para>
    /// </returns>
    private async Task ProbeHealthAsync(HttpContext context)
    {
        var request = await ReadRequiredJsonAsync<DynamicAppTargetKeyRequest>(context).ConfigureAwait(false);
        var runtimeKey = request.ToRuntimeKey();
        await using var healthOperation = await context.RequestServices
            .GetRequiredService<IDynamicAppRuntimeOperationCoordinator>()
            .AcquireAsync(runtimeKey, context.RequestAborted)
            .ConfigureAwait(false);
        var key = runtimeKey.PackageKey;
        var stateStore = context.RequestServices.GetRequiredService<IDynamicAppStateStore>();
        var target = (await ListTargetsWithRuntimeSyncAsync(context).ConfigureAwait(false))
            .FirstOrDefault(item => ToRuntimeKey(item) == runtimeKey);
        if (target == null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await WriteJsonAsync(context, DynamicAppApiResponse<object>.Failure($"Target `{key}` is not registered.", "dynamicapp.target_not_found")).ConfigureAwait(false);
            return;
        }

        var manifestRecord = await context.RequestServices.GetRequiredService<IDynamicAppManifestStore>()
            .FindAsync(key, context.RequestAborted)
            .ConfigureAwait(false);
        var mod = manifestRecord?.Manifest.Mods.FirstOrDefault(item => item.Id == key.ModId && item.Version == key.Version);
        if (mod?.ExecutionMode == DynamicModExecutionMode.ExistingContainer)
        {
            var (operation, view) = await InspectExistingContainerStatusAsync(context, key).ConfigureAwait(false);
            var nextState = view?.IsAvailable == true
                ? target.State == DynamicModLifecycleState.Failed ? DynamicModLifecycleState.Active : target.State
                : DynamicModLifecycleState.Failed;
            var nextHealth = view?.IsAvailable == true ? DynamicModHealthStatus.Healthy : DynamicModHealthStatus.Unhealthy;
            var dockerStatusUpdate = stateStore.UpdateState(runtimeKey, nextState, nextHealth, operation.Message);
            await WriteOperationResultAsync(context, dockerStatusUpdate.Succeeded && operation.Succeeded ? operation : DynamicAppOperationResult.Failure(operation.Message), view).ConfigureAwait(false);
            return;
        }

        if (target.State == DynamicModLifecycleState.Stopped)
        {
            await WriteOperationResultAsync<object>(context, DynamicAppOperationResult.Failure($"Target `{key}` is stopped and cannot be health-probed.")).ConfigureAwait(false);
            return;
        }

        var healthProbe = context.RequestServices.GetService<IDynamicAppHealthProbe>();
        if (healthProbe == null)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await WriteJsonAsync(context, DynamicAppApiResponse<object>.Failure("IDynamicAppHealthProbe is not registered.", "dynamicapp.health_probe_not_registered")).ConfigureAwait(false);
            return;
        }

        var health = mod?.Health ?? new DynamicModHealthManifest
        {
            Path = target.HealthPath,
            StartupTimeoutSeconds = target.HealthProbeTimeoutSeconds
        };
        var result = await healthProbe.ProbeAsync(target, health, context.RequestAborted).ConfigureAwait(false);
        var healthNextState = result.IsHealthy
            ? target.State is DynamicModLifecycleState.Failed or DynamicModLifecycleState.Pending
                ? DynamicModLifecycleState.Active
                : target.State
            : target.State == DynamicModLifecycleState.Starting
                ? DynamicModLifecycleState.Failed
                : target.State;
        var healthNextStatus = result.IsHealthy
            ? healthNextState == DynamicModLifecycleState.Draining
                ? DynamicModHealthStatus.Draining
                : DynamicModHealthStatus.Healthy
            : healthNextState == DynamicModLifecycleState.Stopped
                ? DynamicModHealthStatus.Stopped
                : DynamicModHealthStatus.Unhealthy;
        var update = stateStore.UpdateState(runtimeKey, healthNextState, healthNextStatus, result.Message);
        await WriteOperationResultAsync(context, update, result).ConfigureAwait(false);
    }

    private async Task UpdateStateAsync(HttpContext context)
    {
        var request = await ReadRequiredJsonAsync<DynamicAppUpdateStateRequest>(context).ConfigureAwait(false);
        var runtimeKey = request.ToRuntimeKey();
        await using var operation = await context.RequestServices
            .GetRequiredService<IDynamicAppRuntimeOperationCoordinator>()
            .AcquireAsync(runtimeKey, context.RequestAborted)
            .ConfigureAwait(false);
        var result = context.RequestServices.GetRequiredService<IDynamicAppStateStore>()
            .UpdateState(runtimeKey, request.State, request.HealthStatus, request.Message);
        await WriteOperationResultAsync<object>(context, result).ConfigureAwait(false);
    }

    private async Task QueryLogsAsync(HttpContext context)
    {
        var logSink = context.RequestServices.GetService<IDynamicAppProcessLogSink>();
        if (logSink == null)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await WriteJsonAsync(context, DynamicAppApiResponse<object>.Failure("IDynamicAppProcessLogSink is not registered.", "dynamicapp.log_sink_not_registered")).ConfigureAwait(false);
            return;
        }

        var request = await ReadRequiredJsonAsync<DynamicAppTargetKeyRequest>(context).ConfigureAwait(false);
        var logs = await logSink.QueryAsync(request.ToRuntimeKey(), cancellationToken: context.RequestAborted).ConfigureAwait(false);
        await WriteJsonAsync(context, DynamicAppApiResponse<IReadOnlyCollection<DynamicAppProcessLogEntry>>.Success(logs, "DynamicApp process logs loaded.")).ConfigureAwait(false);
    }

    /// <summary>
    /// <para>zh-cn:列出全部自愈状态，用于控制面板或外部运维系统观察重试窗口和暂停原因。</para>
    /// <para>en-us:Lists all self-healing states so the control panel or external operations systems can observe retry windows and pause reasons.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:当前 HTTP 上下文。</para>
    /// <para>en-us:The current HTTP context.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:异步处理任务。</para>
    /// <para>en-us:The asynchronous handling task.</para>
    /// </returns>
    private static Task ListSelfHealingAsync(HttpContext context)
    {
        var states = context.RequestServices.GetRequiredService<IDynamicAppSelfHealingStateStore>().List();
        return WriteJsonAsync(context, DynamicAppApiResponse<IReadOnlyCollection<DynamicAppSelfHealingState>>.Success(states, "DynamicApp self-healing states loaded."));
    }

    /// <summary>
    /// <para>zh-cn:重置指定目标的自愈状态，清除持久化重试计数和暂停状态。</para>
    /// <para>en-us:Resets the self-healing state for the specified target, clearing persisted retry counters and pause state.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:当前 HTTP 上下文，Body 必须包含 DynamicAppTargetKeyRequest。</para>
    /// <para>en-us:The current HTTP context; the body must contain DynamicAppTargetKeyRequest.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:异步处理任务。</para>
    /// <para>en-us:The asynchronous handling task.</para>
    /// </returns>
    private async Task ResetSelfHealingAsync(HttpContext context)
    {
        var request = await ReadRequiredJsonAsync<DynamicAppTargetKeyRequest>(context).ConfigureAwait(false);
        var key = request.ToKey();
        var removed = context.RequestServices.GetRequiredService<IDynamicAppSelfHealingStateStore>().Remove(key);
        var result = DynamicAppOperationResult.Success(removed
            ? $"Self-healing state for `{key}` reset."
            : $"Self-healing state for `{key}` was already empty.");
        await WriteOperationResultAsync<object>(context, result).ConfigureAwait(false);
    }

    /// <summary>
    /// <para>zh-cn:列出全部已归档目标，用于审计坏包或错误 Manifest 治理记录。</para>
    /// <para>en-us:Lists all archived targets for auditing bad-package or invalid-manifest governance records.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:当前 HTTP 上下文。</para>
    /// <para>en-us:The current HTTP context.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:异步处理任务。</para>
    /// <para>en-us:The asynchronous handling task.</para>
    /// </returns>
    private static Task ListArchivedAsync(HttpContext context)
    {
        var archives = context.RequestServices.GetRequiredService<IDynamicAppTargetArchiveStore>().List();
        return WriteJsonAsync(context, DynamicAppApiResponse<IReadOnlyCollection<DynamicAppArchivedTarget>>.Success(archives, "DynamicApp archived targets loaded."));
    }

    /// <summary>
    /// <para>zh-cn:列出包存储中已经上传的所有 Mod 包版本，供控制面板包管理器展示现有包和版本文件。</para>
    /// <para>en-us:Lists all uploaded Mod package versions in the package store so the panel package manager can display existing packages and version files.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:当前 HTTP 上下文。</para>
    /// <para>en-us:The current HTTP context.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:异步处理任务。</para>
    /// <para>en-us:The asynchronous handling task.</para>
    /// </returns>
    private static async Task ListPackagesAsync(HttpContext context)
    {
        var uploadedPackages = context.RequestServices.GetRequiredService<IDynamicAppModPackageStore>().ListPackages();
        var packages = await BuildMergedPackageInventoryAsync(context, uploadedPackages).ConfigureAwait(false);
        await WriteJsonAsync(context, DynamicAppApiResponse<IReadOnlyCollection<DynamicAppModPackageModView>>.Success(packages, "DynamicApp packages loaded.")).ConfigureAwait(false);
    }

    private static async Task<IReadOnlyCollection<DynamicAppModPackageModView>> BuildMergedPackageInventoryAsync(HttpContext context, IReadOnlyCollection<DynamicAppModPackageModView> uploadedPackages)
    {
        var uploadedKeys = uploadedPackages
            .SelectMany(mod => mod.Versions.Select(version => BuildPackageVersionKey(version.DynamicAppId, version.ModId, version.Version)))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var installedPackages = await ListInstalledModPackagesAsync(context, uploadedKeys).ConfigureAwait(false);
        return uploadedPackages
            .Concat(installedPackages)
            .GroupBy(item => BuildPackageModKey(item.DynamicAppId, item.ModId), StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var first = group.First();
                return new DynamicAppModPackageModView
                {
                    DynamicAppId = first.DynamicAppId,
                    ModId = first.ModId,
                    Versions = group
                        .SelectMany(item => item.Versions)
                        .GroupBy(version => BuildPackageVersionKey(version.DynamicAppId, version.ModId, version.Version), StringComparer.OrdinalIgnoreCase)
                        .Select(versionGroup => versionGroup.First())
                        .OrderBy(version => version.Version, StringComparer.OrdinalIgnoreCase)
                        .ToList()
                };
            })
            .OrderBy(item => item.DynamicAppId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.ModId, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// <para>zh-cn:列出包管理应用命名空间；统计优先使用合并后的包库存，只有没有包库存时才用 Manifest 历史兜底，并保留空应用目录。</para>
    /// <para>en-us:Lists package-management application namespaces; counts prefer the merged package inventory, fall back to manifest history only when no package inventory exists, and keep empty application directories.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:当前 HTTP 上下文。</para>
    /// <para>en-us:The current HTTP context.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:异步处理任务。</para>
    /// <para>en-us:The asynchronous handling task.</para>
    /// </returns>
    private static async Task ListPackageApplicationsAsync(HttpContext context)
    {
        var store = context.RequestServices.GetRequiredService<IDynamicAppModPackageStore>();
        var directoryApplications = store.ListApplications();
        var packageViews = await BuildMergedPackageInventoryAsync(context, store.ListPackages()).ConfigureAwait(false);
        var packageApplications = BuildPackageApplications(packageViews);
        var manifestApplications = await ListManifestPackageApplicationsAsync(context, packageViews).ConfigureAwait(false);
        var packageApplicationMap = packageApplications.ToDictionary(item => item.DynamicAppId, StringComparer.OrdinalIgnoreCase);
        var manifestApplicationMap = manifestApplications.ToDictionary(item => item.DynamicAppId, StringComparer.OrdinalIgnoreCase);
        var applicationIds = directoryApplications
            .Select(item => item.DynamicAppId)
            .Concat(packageApplicationMap.Keys)
            .Concat(manifestApplicationMap.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase);
        var applications = applicationIds
            .Select(dynamicAppId =>
            {
                if (packageApplicationMap.TryGetValue(dynamicAppId, out var packageApplication))
                {
                    return packageApplication;
                }

                if (manifestApplicationMap.TryGetValue(dynamicAppId, out var manifestApplication))
                {
                    return manifestApplication;
                }

                return new DynamicAppPackageApplicationView
                {
                    DynamicAppId = dynamicAppId,
                    ModCount = 0,
                    VersionCount = 0
                };
            })
            .OrderBy(item => item.DynamicAppId, StringComparer.OrdinalIgnoreCase)
            .ToList();
        await WriteJsonAsync(context, DynamicAppApiResponse<IReadOnlyCollection<DynamicAppPackageApplicationView>>.Success(applications, "DynamicApp package applications loaded.")).ConfigureAwait(false);
    }

    private static IReadOnlyCollection<DynamicAppPackageApplicationView> BuildPackageApplications(IReadOnlyCollection<DynamicAppModPackageModView> packageViews)
    {
        return packageViews
            .GroupBy(item => item.DynamicAppId, StringComparer.OrdinalIgnoreCase)
            .Select(group => new DynamicAppPackageApplicationView
            {
                DynamicAppId = group.Key,
                ModCount = group.Select(item => item.ModId).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                VersionCount = group.Sum(item => item.Versions.Count)
            })
            .ToList();
    }

    /// <summary>
    /// <para>zh-cn:创建包管理应用命名空间；该操作只持久化应用目录，不发布 Manifest，也不启动服务。</para>
    /// <para>en-us:Creates a package-management application namespace; this only persists the application directory and does not publish manifests or start services.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:当前 HTTP 上下文，Body 必须包含 DynamicAppPackageApplicationRequest。</para>
    /// <para>en-us:The current HTTP context; the body must contain DynamicAppPackageApplicationRequest.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:异步处理任务。</para>
    /// <para>en-us:The asynchronous handling task.</para>
    /// </returns>
    private static async Task CreatePackageApplicationAsync(HttpContext context)
    {
        var request = await ReadRequiredJsonAsync<DynamicAppPackageApplicationRequest>(context).ConfigureAwait(false);
        var application = context.RequestServices.GetRequiredService<IDynamicAppModPackageStore>().CreateApplication(request.DynamicAppId);
        await WriteJsonAsync(context, DynamicAppApiResponse<DynamicAppPackageApplicationView>.Success(application, "DynamicApp package application created.")).ConfigureAwait(false);
    }

    /// <summary>
    /// <para>zh-cn:创建包管理器中的 Mod 配置；该操作只创建包空间目录，上传包和运行实例仍由后续页面流程分别处理。</para>
    /// <para>en-us:Creates a Mod configuration in the package manager; this operation only creates package-space directories, while package upload and runtime-instance creation remain separate workflows.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:当前 HTTP 上下文，Body 必须包含 DynamicAppPackageModRequest。</para>
    /// <para>en-us:The current HTTP context; the body must contain DynamicAppPackageModRequest.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:异步创建结果。</para>
    /// <para>en-us:The asynchronous creation result.</para>
    /// </returns>
    private static async Task CreatePackageModAsync(HttpContext context)
    {
        var request = await ReadRequiredJsonAsync<DynamicAppPackageModRequest>(context).ConfigureAwait(false);
        var appId = new DynamicAppId(request.DynamicAppId);
        var modId = new DynamicModId(request.ModId);
        var manifestStore = context.RequestServices.GetRequiredService<IDynamicAppManifestStore>();
        if ((await manifestStore.ListAsync(appId, context.RequestAborted).ConfigureAwait(false))
            .Any(record => record.Manifest.Mods.Any(mod => mod.Id == modId)))
        {
            throw new InvalidOperationException($"DynamicApp Mod `{appId}/{modId}` already exists.");
        }

        var application = context.RequestServices.GetRequiredService<IDynamicAppModPackageStore>()
            .CreateMod(request.DynamicAppId, request.ModId);
        await WriteJsonAsync(
            context,
            DynamicAppApiResponse<DynamicAppPackageApplicationView>.Success(
                application,
                $"DynamicApp Mod `{request.DynamicAppId}/{request.ModId}` created. Upload a package from this Mod group."))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// <para>zh-cn:删除没有包版本、运行实例或归档文件的 Mod 服务，并清理其 Manifest、路由状态和自愈状态。</para>
    /// <para>en-us:Deletes a Mod service with no package versions, runtime instances, or archived files and cleans its manifests, route state, and self-healing state.</para>
    /// </summary>
    private static async Task RemovePackageModAsync(HttpContext context)
    {
        var request = await ReadRequiredJsonAsync<DynamicAppPackageModRequest>(context).ConfigureAwait(false);
        var appId = new DynamicAppId(request.DynamicAppId);
        var modId = new DynamicModId(request.ModId);
        var operationCoordinator = context.RequestServices.GetService<IDynamicAppRuntimeOperationCoordinator>();
        await using var familyOperation = operationCoordinator == null
            ? null
            : await operationCoordinator.AcquirePackageAsync(
                    new DynamicModVersionKey(appId, modId, new DynamicAppVersion("__mod__")),
                    context.RequestAborted)
                .ConfigureAwait(false);
        var store = context.RequestServices.GetRequiredService<IDynamicAppModPackageStore>();
        var stateStore = context.RequestServices.GetRequiredService<IDynamicAppStateStore>();
        var archiveStore = context.RequestServices.GetRequiredService<IDynamicAppTargetArchiveStore>();

        if (store.ListPackages().Any(item =>
                item.DynamicAppId.Equals(appId.Value, StringComparison.OrdinalIgnoreCase)
                && item.ModId.Equals(modId.Value, StringComparison.OrdinalIgnoreCase)
                && item.Versions.Count > 0))
        {
            throw new InvalidOperationException($"DynamicApp Mod `{appId}/{modId}` cannot be removed while package versions exist.");
        }

        if (stateStore.ListTargets().Any(item => item.Key.DynamicAppId == appId && item.Key.ModId == modId))
        {
            throw new InvalidOperationException($"DynamicApp Mod `{appId}/{modId}` cannot be removed while runtime targets exist.");
        }

        if (store.ListDeploymentDirectories().Any(item => item.Key.DynamicAppId == appId && item.Key.ModId == modId)
            || archiveStore.HasArchives(appId, modId))
        {
            throw new InvalidOperationException($"DynamicApp Mod `{appId}/{modId}` cannot be removed while deployment or archive files exist.");
        }

        foreach (var selfHealing in context.RequestServices.GetRequiredService<IDynamicAppSelfHealingStateStore>().List()
                     .Where(item => item.Key.DynamicAppId == appId && item.Key.ModId == modId)
                     .ToArray())
        {
            context.RequestServices.GetRequiredService<IDynamicAppSelfHealingStateStore>().Remove(
                new DynamicAppRuntimeKey(selfHealing.Key, string.IsNullOrWhiteSpace(selfHealing.DeploymentId) ? "default" : selfHealing.DeploymentId));
        }

        var removed = store.RemoveMod(appId.Value, modId.Value);
        var manifestChanges = await context.RequestServices.GetRequiredService<IDynamicAppManifestStore>()
            .RemoveModAsync(appId, modId, context.RequestAborted)
            .ConfigureAwait(false);
        if (!removed && manifestChanges == 0)
        {
            throw new FileNotFoundException($"DynamicApp Mod `{appId}/{modId}` was not found.");
        }

        await WriteOperationResultAsync<object>(
                context,
                DynamicAppOperationResult.Success($"DynamicApp Mod `{appId}/{modId}` was removed."))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// <para>zh-cn:删除未被运行实例、部署目录或归档引用的包版本，并清理对应 Manifest 历史。</para>
    /// <para>en-us:Deletes a package version that has no runtime, deployment-directory, or archive references and removes its manifest history.</para>
    /// </summary>
    private static async Task RemovePackageVersionAsync(HttpContext context)
    {
        var request = await ReadRequiredJsonAsync<DynamicAppTargetKeyRequest>(context).ConfigureAwait(false);
        var key = request.ToKey();
        await using var operation = await context.RequestServices
            .GetRequiredService<IDynamicAppRuntimeOperationCoordinator>()
            .AcquirePackageAsync(key, context.RequestAborted)
            .ConfigureAwait(false);
        var store = context.RequestServices.GetRequiredService<IDynamicAppModPackageStore>();
        var stateStore = context.RequestServices.GetRequiredService<IDynamicAppStateStore>();
        var archiveStore = context.RequestServices.GetRequiredService<IDynamicAppTargetArchiveStore>();
        if (stateStore.ListTargets().Any(item => item.Key == key)
            || store.ListDeploymentDirectories().Any(item => item.Key == key)
            || archiveStore.List().Any(item => item.Key == key))
        {
            throw new InvalidOperationException($"DynamicApp package `{key}` cannot be removed while it is used by a runtime instance or archive.");
        }

        var removed = store.RemovePackageVersion(key);
        var manifestChanges = await context.RequestServices.GetRequiredService<IDynamicAppManifestStore>()
            .RemoveVersionAsync(key, context.RequestAborted).ConfigureAwait(false);
        if (!removed && manifestChanges == 0)
        {
            throw new FileNotFoundException($"DynamicApp package `{key}` was not found.");
        }

        await WriteOperationResultAsync<object>(context, DynamicAppOperationResult.Success($"DynamicApp package `{key}` was removed.")).ConfigureAwait(false);
    }

    /// <summary>
    /// <para>zh-cn:列出已经创建的独立部署实例；部署实例与包库存分离，用于集群和灰度目录隔离。</para>
    /// <para>en-us:Lists isolated deployment instances; deployments are separated from package inventory for cluster and gray-release directory isolation.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:当前 HTTP 上下文。</para>
    /// <para>en-us:The current HTTP context.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:异步处理任务。</para>
    /// <para>en-us:The asynchronous handling task.</para>
    /// </returns>
    private static async Task ListDeploymentsAsync(HttpContext context)
    {
        var deployments = context.RequestServices.GetRequiredService<IDynamicAppStateStore>()
            .ListTargets()
            .Where(item => !string.Equals(item.DeploymentId, "default", StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.Key.DynamicAppId.Value, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Key.ModId.Value, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Key.Version.Value, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.DeploymentId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        await WriteJsonAsync(context, DynamicAppApiResponse<IReadOnlyCollection<DynamicAppRouteTarget>>.Success(deployments, "DynamicApp deployments loaded.")).ConfigureAwait(false);
    }

    /// <summary>
    /// <para>zh-cn:列出指定运行实例独立副本中的文件；该接口不会读取或回退到包库存目录。</para>
    /// <para>en-us:Lists files from one isolated runtime deployment; this endpoint never reads or falls back to the package inventory.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:当前 HTTP 上下文，查询参数必须包含 DynamicAppId、ModId、Version，可选 DeploymentId。</para>
    /// <para>en-us:The current HTTP context; query parameters must include DynamicAppId, ModId, and Version, with optional DeploymentId.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:异步文件清单处理任务。</para>
    /// <para>en-us:The asynchronous deployment-file inventory task.</para>
    /// </returns>
    private static async Task ListDeploymentFilesAsync(HttpContext context)
    {
        var request = new DynamicAppTargetKeyRequest
        {
            DynamicAppId = context.Request.Query["dynamicAppId"].FirstOrDefault() ?? string.Empty,
            ModId = context.Request.Query["modId"].FirstOrDefault() ?? string.Empty,
            Version = context.Request.Query["version"].FirstOrDefault() ?? string.Empty,
            DeploymentId = context.Request.Query["deploymentId"].FirstOrDefault()
        };
        var runtimeKey = request.ToRuntimeKey();
        var stateTarget = context.RequestServices.GetRequiredService<IDynamicAppStateStore>()
            .ListTargets()
            .FirstOrDefault(target => new DynamicAppRuntimeKey(target.Key, string.IsNullOrWhiteSpace(target.DeploymentId) ? "default" : target.DeploymentId.Trim()) == runtimeKey);
        var packageStore = context.RequestServices.GetRequiredService<IDynamicAppModPackageStore>();
        var files = stateTarget != null && !string.IsNullOrWhiteSpace(stateTarget.DeploymentDirectory)
            ? packageStore.ListDeploymentFiles(runtimeKey, stateTarget.DeploymentDirectory)
            : packageStore.ListDeploymentFiles(runtimeKey);
        await WriteJsonAsync(
            context,
            DynamicAppApiResponse<IReadOnlyCollection<DynamicAppModPackageFileView>>.Success(
                files,
                $"DynamicApp deployment files loaded for `{runtimeKey}`."))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// <para>zh-cn:列出磁盘上存在但运行状态未登记的部署目录，用于人工确认和后续清理。</para>
    /// <para>en-us:Lists deployment directories present on disk but absent from runtime state for manual confirmation and later cleanup.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:当前 HTTP 上下文。</para>
    /// <para>en-us:The current HTTP context.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:孤儿部署目录查询任务。</para>
    /// <para>en-us:The orphan deployment-directory query task.</para>
    /// </returns>
    private static Task ListOrphanDeploymentsAsync(HttpContext context)
    {
        var stateKeys = context.RequestServices.GetRequiredService<IDynamicAppStateStore>()
            .ListTargets()
            .Select(ToRuntimeKey)
            .ToHashSet();
        var orphans = context.RequestServices.GetRequiredService<IDynamicAppModPackageStore>()
            .ListDeploymentDirectories()
            .Where(item => !stateKeys.Contains(new DynamicAppRuntimeKey(item.Key, item.DeploymentId)))
            .Select(item => new DynamicAppOrphanDeploymentView
            {
                Key = item.Key,
                DeploymentId = item.DeploymentId,
                DeploymentDirectory = item.DeploymentDirectory,
                LastModifiedUtc = item.LastModifiedUtc
            })
            .ToArray();
        return WriteJsonAsync(
            context,
            DynamicAppApiResponse<IReadOnlyCollection<DynamicAppOrphanDeploymentView>>.Success(
                orphans,
                "DynamicApp orphan deployment directories loaded."));
    }

    /// <summary>
    /// <para>zh-cn:删除指定 Process 部署副本；先停止运行实例，再删除隔离目录，只有全部成功后才清理状态和自愈计数。</para>
    /// <para>en-us:Removes one Process deployment replica by stopping it first, deleting its isolated directory, and clearing state and self-healing data only after all steps succeed.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:当前 HTTP 上下文，Body 必须包含 DynamicAppTargetKeyRequest。</para>
    /// <para>en-us:The current HTTP context; the body must contain DynamicAppTargetKeyRequest.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:副本删除操作结果。</para>
    /// <para>en-us:The deployment-removal operation result.</para>
    /// </returns>
    private async Task RemoveDeploymentAsync(HttpContext context)
    {
        var request = await ReadRequiredJsonAsync<DynamicAppTargetKeyRequest>(context).ConfigureAwait(false);
        var runtimeKey = request.ToRuntimeKey();
        if (string.Equals(runtimeKey.DeploymentId, "default", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The default deployment cannot be removed through the clustered deployment API.");
        }

        await using var familyOperation = await context.RequestServices
            .GetRequiredService<IDynamicAppRuntimeOperationCoordinator>()
            .AcquirePackageAsync(runtimeKey.PackageKey, context.RequestAborted)
            .ConfigureAwait(false);
        await using var operation = await context.RequestServices
            .GetRequiredService<IDynamicAppRuntimeOperationCoordinator>()
            .AcquireAsync(runtimeKey, context.RequestAborted)
            .ConfigureAwait(false);

        var stateStore = context.RequestServices.GetRequiredService<IDynamicAppStateStore>();
        var target = stateStore.ListTargets().FirstOrDefault(item => ToRuntimeKey(item) == runtimeKey);
        var manifestRecord = await context.RequestServices.GetRequiredService<IDynamicAppManifestStore>()
            .FindAsync(runtimeKey.PackageKey, context.RequestAborted)
            .ConfigureAwait(false);
        var mod = manifestRecord?.Manifest.Mods.FirstOrDefault(item =>
            item.Id == runtimeKey.PackageKey.ModId && item.Version == runtimeKey.PackageKey.Version);

        if (mod?.ExecutionMode == DynamicModExecutionMode.ExistingContainer)
        {
            throw new InvalidOperationException($"ExistingContainer deployment `{runtimeKey}` cannot be removed through the Process deployment API.");
        }

        if (target != null
            && target.State is (DynamicModLifecycleState.Active or DynamicModLifecycleState.Starting)
            && mod == null)
        {
            throw new InvalidOperationException($"DynamicApp deployment `{runtimeKey}` cannot be removed because its manifest is unavailable and the running process cannot be safely identified.");
        }

        if (target != null
            && mod?.ExecutionMode == DynamicModExecutionMode.Process
            && target.State is not DynamicModLifecycleState.Pending
            and not DynamicModLifecycleState.Stopped)
        {
            var stop = await StopTargetAsync(context, runtimeKey).ConfigureAwait(false);
            if (!stop.Succeeded)
            {
                await WriteOperationResultAsync<object>(context, stop).ConfigureAwait(false);
                return;
            }
        }

        var packageStore = context.RequestServices.GetRequiredService<IDynamicAppModPackageStore>();
        var removed = await packageStore.RemoveDeploymentAsync(runtimeKey, context.RequestAborted).ConfigureAwait(false);
        if (!removed && target == null)
        {
            throw new FileNotFoundException($"DynamicApp deployment `{runtimeKey}` was not found.");
        }

        stateStore.RemoveTarget(runtimeKey);
        context.RequestServices.GetRequiredService<IDynamicAppSelfHealingStateStore>().Remove(runtimeKey);
        await WriteOperationResultAsync<object>(
            context,
            DynamicAppOperationResult.Success($"DynamicApp deployment `{runtimeKey}` was removed."))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// <para>zh-cn:销毁已停止的运行实例，将实例配置、自愈状态和运行文件迁移到归档空间。</para>
    /// <para>en-us:Destroys a stopped runtime instance and moves its configuration, self-healing state, and runtime files into archive space.</para>
    /// </summary>
    private async Task DestroyDeploymentAsync(HttpContext context)
    {
        var request = await ReadRequiredJsonAsync<DynamicAppTargetKeyRequest>(context).ConfigureAwait(false);
        var runtimeKey = request.ToRuntimeKey();
        await using var operation = await context.RequestServices
            .GetRequiredService<IDynamicAppRuntimeOperationCoordinator>()
            .AcquireAsync(runtimeKey, context.RequestAborted)
            .ConfigureAwait(false);

        var stateStore = context.RequestServices.GetRequiredService<IDynamicAppStateStore>();
        var target = stateStore.ListTargets().FirstOrDefault(item => ToRuntimeKey(item) == runtimeKey);
        if (target == null)
        {
            throw new FileNotFoundException($"DynamicApp deployment `{runtimeKey}` was not found.");
        }

        if (target.State != DynamicModLifecycleState.Stopped)
        {
            throw new InvalidOperationException($"DynamicApp deployment `{runtimeKey}` can only be destroyed after it is stopped.");
        }

        // A persisted Stopped state is not proof that a process/container has exited.
        // Reconcile it immediately before moving the runtime directory into archive space.
        var stop = await StopTargetAsync(context, runtimeKey).ConfigureAwait(false);
        if (!stop.Succeeded)
        {
            await WriteOperationResultAsync<object>(context, DynamicAppOperationResult.Failure(
                    $"DynamicApp deployment `{runtimeKey}` cannot be destroyed until its runtime is confirmed stopped. {stop.Message}"))
                .ConfigureAwait(false);
            return;
        }

        var packageStore = context.RequestServices.GetRequiredService<IDynamicAppModPackageStore>();
        var moved = await packageStore.ArchiveDeploymentAsync(runtimeKey, context.RequestAborted).ConfigureAwait(false);
        var selfHealingStore = context.RequestServices.GetRequiredService<IDynamicAppSelfHealingStateStore>();
        var archive = context.RequestServices.GetRequiredService<IDynamicAppTargetArchiveStore>()
            .ArchiveDeployment(
                runtimeKey.PackageKey,
                runtimeKey.DeploymentId,
                target,
                selfHealingStore.Get(runtimeKey),
                moved.Moved ? moved.ArchiveDirectory : null,
                "Destroyed by DynamicApp Manager.");

        stateStore.RemoveTarget(runtimeKey);
        selfHealingStore.Remove(runtimeKey);
        await WriteOperationResultAsync(
                context,
                DynamicAppOperationResult.Success($"DynamicApp deployment `{runtimeKey}` was destroyed and archived."),
                archive)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// <para>zh-cn:从现有包创建独立部署目录，并可选择立即启动；启动过程会使用部署副本中的入口文件和端口。</para>
    /// <para>en-us:Creates an isolated deployment directory from an existing package and optionally starts it using the copied entry file and configured port.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:当前 HTTP 上下文，Body 必须是 DynamicAppCreateDeploymentRequest。</para>
    /// <para>en-us:The current HTTP context; the body must contain DynamicAppCreateDeploymentRequest.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:部署创建和可选启动结果。</para>
    /// <para>en-us:The deployment creation and optional startup result.</para>
    /// </returns>
    private async Task CreateDeploymentAsync(HttpContext context)
    {
        var request = await ReadRequiredJsonAsync<DynamicAppCreateDeploymentRequest>(context).ConfigureAwait(false);
        var prepared = await PrepareDeploymentAsync(context, request).ConfigureAwait(false);
        var operation = request.Start
            ? await StartPreparedDeploymentAsync(context, prepared).ConfigureAwait(false)
            : DynamicAppOperationResult.Success($"Deployment `{prepared.RuntimeKey}` was created and is pending start.");
        await WriteOperationResultAsync(context, operation, prepared.Deployment).ConfigureAwait(false);
    }

    /// <summary>
    /// <para>zh-cn:先停止当前实例并释放其监听端口，再使用固定的原端口和端点创建、启动并健康检查新包。</para>
    /// <para>en-us:Stops the current instance and releases its listener first, then creates, starts, and health-checks the new package using the immutable original port and endpoint.</para>
    /// </summary>
    private async Task UpdateDeploymentAsync(HttpContext context)
    {
        var request = await ReadRequiredJsonAsync<DynamicAppUpdateDeploymentRequest>(context).ConfigureAwait(false);
        var currentKey = new DynamicModVersionKey(
            new DynamicAppId(request.CurrentDynamicAppId),
            new DynamicModId(request.CurrentModId),
            new DynamicAppVersion(request.CurrentVersion));
        var currentRuntimeKey = new DynamicAppRuntimeKey(
            currentKey,
            NormalizeDeploymentId(request.CurrentDeploymentId));
        var stateStore = context.RequestServices.GetRequiredService<IDynamicAppStateStore>();
        var currentTarget = stateStore.ListTargets().FirstOrDefault(item => ToRuntimeKey(item) == currentRuntimeKey);
        if (currentTarget == null)
        {
            throw new FileNotFoundException($"Current deployment `{currentRuntimeKey}` was not found.");
        }

        var currentRecord = await context.RequestServices.GetRequiredService<IDynamicAppManifestStore>()
            .FindAsync(currentKey, context.RequestAborted)
            .ConfigureAwait(false);
        var currentMod = currentRecord?.Manifest.Mods.FirstOrDefault(item => item.Id == currentKey.ModId && item.Version == currentKey.Version);
        var currentEndpoint = currentTarget.BaseAddress;
        if (currentEndpoint.Port <= 0)
        {
            throw new InvalidOperationException($"Current deployment `{currentRuntimeKey}` does not have a valid listener port.");
        }

        await using var familyOperation = await context.RequestServices
            .GetRequiredService<IDynamicAppRuntimeOperationCoordinator>()
            .AcquirePackageAsync(currentKey, context.RequestAborted)
            .ConfigureAwait(false);
        await using var oldOperation = await context.RequestServices
            .GetRequiredService<IDynamicAppRuntimeOperationCoordinator>()
            .AcquireAsync(currentRuntimeKey, context.RequestAborted)
            .ConfigureAwait(false);

        var requestedNextKey = new DynamicModVersionKey(
            new DynamicAppId(request.DynamicAppId),
            new DynamicModId(request.ModId),
            new DynamicAppVersion(request.Version));
        if (requestedNextKey == currentKey)
        {
            throw new InvalidOperationException("Deployment update must select a different package version.");
        }

        var packageExists = context.RequestServices.GetRequiredService<IDynamicAppModPackageStore>()
            .ListPackages()
            .Any(item => item.DynamicAppId.Equals(requestedNextKey.DynamicAppId.Value, StringComparison.OrdinalIgnoreCase)
                && item.ModId.Equals(requestedNextKey.ModId.Value, StringComparison.OrdinalIgnoreCase)
                && item.Versions.Any(version => version.Version.Equals(requestedNextKey.Version.Value, StringComparison.OrdinalIgnoreCase)));
        if (!packageExists)
        {
            throw new FileNotFoundException($"DynamicApp package `{requestedNextKey}` was not found.");
        }

        var stop = await StopTargetAsync(context, currentRuntimeKey).ConfigureAwait(false);
        if (!stop.Succeeded)
        {
            await WriteOperationResultAsync<object>(context, stop).ConfigureAwait(false);
            return;
        }

        var nextRequest = new DynamicAppCreateDeploymentRequest
        {
            DynamicAppId = request.DynamicAppId,
            ModId = request.ModId,
            Version = request.Version,
            DeploymentId = request.DeploymentId,
            Port = currentEndpoint.Port,
            Endpoint = currentEndpoint.ToString(),
            Arguments = request.Arguments ?? currentMod?.Artifact.Arguments,
            HealthPath = request.HealthPath ?? currentTarget.HealthPath,
            StartupTimeoutSeconds = request.StartupTimeoutSeconds > 0
                ? request.StartupTimeoutSeconds
                : currentTarget.HealthProbeTimeoutSeconds,
            EntryFile = request.EntryFile,
            Start = true
        };
        var nextKey = new DynamicModVersionKey(
            new DynamicAppId(nextRequest.DynamicAppId),
            new DynamicModId(nextRequest.ModId),
            new DynamicAppVersion(nextRequest.Version));
        if (nextKey == currentKey)
        {
            throw new InvalidOperationException("Deployment update must select a different package version.");
        }

        DynamicAppModPackageDeploymentRecord? preparedDeployment = null;
        DynamicAppOperationResult operation;
        try
        {
            var prepared = await PrepareDeploymentAsync(context, nextRequest, familyLockHeld: true).ConfigureAwait(false);
            preparedDeployment = prepared.Deployment;
            operation = await StartPreparedDeploymentAsync(context, prepared).ConfigureAwait(false);
            operation = operation.Succeeded
                ? DynamicAppOperationResult.Success($"Deployment `{currentRuntimeKey}` was updated to `{prepared.RuntimeKey}` using listener `{currentEndpoint}`.")
                : await RestorePreviousDeploymentAfterUpdateFailureAsync(context, currentRuntimeKey, operation.Message).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            var failure = $"Deployment update from `{currentRuntimeKey}` failed before the new instance became active. {exception.Message}";
            operation = await RestorePreviousDeploymentAfterUpdateFailureAsync(context, currentRuntimeKey, failure).ConfigureAwait(false);
        }

        await WriteOperationResultAsync(context, operation, preparedDeployment).ConfigureAwait(false);
    }

    /// <summary>
    /// <para>zh-cn:更新切换失败时恢复旧实例；旧实例只有重新启动、运行态检查、健康检查和激活全部成功才算回滚成功。</para>
    /// <para>en-us:Restores the previous instance when an update switch fails; rollback succeeds only after restart, runtime inspection, health probing, and activation all pass.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:当前 HTTP 上下文。</para>
    /// <para>en-us:The current HTTP context.</para>
    /// </param>
    /// <param name="runtimeKey">
    /// <para>zh-cn:已在更新前停止的旧实例键。</para>
    /// <para>en-us:The previous instance key that was stopped before the update.</para>
    /// </param>
    /// <param name="failureMessage">
    /// <para>zh-cn:新版本更新失败原因。</para>
    /// <para>en-us:The reason the new-version update failed.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:包含原失败原因和回滚结果的操作结果。</para>
    /// <para>en-us:An operation result containing the original failure and rollback outcome.</para>
    /// </returns>
    private async Task<DynamicAppOperationResult> RestorePreviousDeploymentAfterUpdateFailureAsync(
        HttpContext context,
        DynamicAppRuntimeKey runtimeKey,
        string failureMessage)
    {
        try
        {
            // A disconnected update request must not cancel restoration of the listener that was just released.
            var rollbackContext = context;
            if (context.RequestAborted.IsCancellationRequested)
            {
                rollbackContext = new DefaultHttpContext
                {
                    RequestServices = context.RequestServices
                };
                rollbackContext.RequestAborted = CancellationToken.None;
            }

            var rollback = await StartTargetAsync(rollbackContext, runtimeKey).ConfigureAwait(false);
            return rollback.Succeeded
                ? DynamicAppOperationResult.Failure($"{failureMessage} Previous deployment `{runtimeKey}` was restored successfully.")
                : DynamicAppOperationResult.Failure($"{failureMessage} Previous deployment `{runtimeKey}` rollback failed: {rollback.Message}");
        }
        catch (Exception exception)
        {
            return DynamicAppOperationResult.Failure($"{failureMessage} Previous deployment `{runtimeKey}` rollback failed unexpectedly: {exception.Message}");
        }
    }

    private async Task<(DynamicAppManifest Manifest, DynamicModManifest Mod, DynamicAppModPackageDeploymentRecord Deployment, DynamicAppRuntimeKey RuntimeKey)> PrepareDeploymentAsync(
        HttpContext context,
        DynamicAppCreateDeploymentRequest request,
        bool familyLockHeld = false)
    {
        if (request.Port is <= 0 or > 65535)
        {
            throw new ArgumentException("Deployment port must be between 1 and 65535.", nameof(request.Port));
        }

        var key = new DynamicModVersionKey(
            new DynamicAppId(request.DynamicAppId),
            new DynamicModId(request.ModId),
            new DynamicAppVersion(request.Version));
        var deploymentId = NormalizeDeploymentId(request.DeploymentId);
        var endpoint = ResolveDeploymentEndpoint(request.Endpoint, request.Port);
        if (!string.IsNullOrWhiteSpace(request.EntryFile)
            && !request.EntryFile.Trim().EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Deployment entry file must be a DLL.", nameof(request.EntryFile));
        }
        var healthPath = NormalizeInternalRoutePrefix(
            string.IsNullOrWhiteSpace(request.HealthPath) ? "/Health" : request.HealthPath);
        var timeout = request.StartupTimeoutSeconds > 0 ? request.StartupTimeoutSeconds : 60;
        var sourceMod = new DynamicModManifest
        {
            Id = key.ModId,
            Version = key.Version,
            ExecutionMode = DynamicModExecutionMode.Process,
            Artifact = new DynamicModArtifactManifest
            {
                File = request.EntryFile,
                Arguments = request.Arguments,
                Endpoint = endpoint.ToString()
            },
            Health = new DynamicModHealthManifest
            {
                Path = healthPath,
                StartupTimeoutSeconds = timeout
            },
        };
        var manifest = new DynamicAppManifest
        {
            Id = key.DynamicAppId,
            Version = key.Version,
            Runtime = new DynamicAppRuntimeManifest { Profile = DynamicAppRuntimeProfile.Compact },
            Endpoints = new DynamicAppEndpointManifest
            {
                GatewayPort = 8080,
                ManagerPort = 8081,
                ManagerInternalOnly = true
            },
            Mods = [sourceMod]
        };
        var stateStore = context.RequestServices.GetRequiredService<IDynamicAppStateStore>();
        var packageStore = context.RequestServices.GetRequiredService<IDynamicAppModPackageStore>();
        DynamicAppModPackageDeploymentRecord deployment;
        await DeploymentCreationGate.WaitAsync(context.RequestAborted).ConfigureAwait(false);
        try
        {
            var packageOptions = context.RequestServices.GetService<DynamicAppPackageStoreOptions>()
                ?? new DynamicAppPackageStoreOptions();
            await using var familyOperation = familyLockHeld || context.RequestServices.GetService<IDynamicAppRuntimeOperationCoordinator>() == null
                ? null
                : await context.RequestServices.GetRequiredService<IDynamicAppRuntimeOperationCoordinator>()
                    .AcquirePackageAsync(key, context.RequestAborted)
                    .ConfigureAwait(false);
            await using var fileLock = await DynamicAppDeploymentFileLock
                .AcquireAsync(packageOptions.RootPath, context.RequestAborted)
                .ConfigureAwait(false);
            ValidateDeploymentConflicts(stateStore, key, deploymentId, endpoint);
            EnsureDeploymentEndpointAvailable(endpoint);
            deployment = await packageStore
                .CreateDeploymentAsync(key, deploymentId, context.RequestAborted, request.EntryFile)
                .ConfigureAwait(false);
        }
        finally
        {
            DeploymentCreationGate.Release();
        }

        sourceMod.Artifact.DeploymentId = deploymentId;
        sourceMod.Artifact.DeploymentDirectory = deployment.DeploymentDirectory;
        sourceMod.Artifact.File = deployment.EntryFilePath;
        sourceMod.Artifact.Endpoint = endpoint.ToString();
        sourceMod.Artifact.Arguments = AppendUrlsArgument(sourceMod.Artifact.Arguments, endpoint);
        var runtimeKey = new DynamicAppRuntimeKey(key, deploymentId);
        try
        {
            await context.RequestServices.GetRequiredService<IDynamicAppManifestStore>()
                .SaveAsync(manifest, context.RequestAborted)
                .ConfigureAwait(false);
            stateStore.UpsertTarget(new DynamicAppRouteTarget
            {
                Key = key,
                DeploymentId = deploymentId,
                DeploymentDirectory = deployment.DeploymentDirectory,
                DeploymentEntryFilePath = deployment.EntryFilePath,
                BaseAddress = endpoint,
                HealthPath = sourceMod.Health.Path,
                HealthProbeTimeoutSeconds = sourceMod.Health.StartupTimeoutSeconds,
                State = DynamicModLifecycleState.Pending,
                HealthStatus = DynamicModHealthStatus.Unknown,
                LastStatusMessage = $"Deployment `{runtimeKey}` was created from package `{key}`."
            });
            return (manifest, sourceMod, deployment, runtimeKey);
        }
        catch
        {
            // The request may already be cancelled here. Cleanup must still remove the
            // newly-created directory so a failed or disconnected request cannot leave a
            // deployment artifact that later collides with a retry.
            await packageStore.RemoveDeploymentAsync(runtimeKey, CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    private async Task<DynamicAppOperationResult> StartPreparedDeploymentAsync(
        HttpContext context,
        (DynamicAppManifest Manifest, DynamicModManifest Mod, DynamicAppModPackageDeploymentRecord Deployment, DynamicAppRuntimeKey RuntimeKey) prepared)
    {
        await using var runtimeOperation = await context.RequestServices
            .GetRequiredService<IDynamicAppRuntimeOperationCoordinator>()
            .AcquireAsync(prepared.RuntimeKey, context.RequestAborted)
            .ConfigureAwait(false);
        var stateStore = context.RequestServices.GetRequiredService<IDynamicAppStateStore>();
        var start = await StartModAsync(context, prepared.Manifest, prepared.Mod).ConfigureAwait(false);
        if (!start.Succeeded)
        {
            var message = await AppendProcessDiagnosticsAsync(context, prepared.RuntimeKey, start.Message).ConfigureAwait(false);
            return await FailTargetAndCleanupAsync(context, prepared.RuntimeKey, message).ConfigureAwait(false);
        }

        var target = stateStore.ListTargets().FirstOrDefault(item => ToRuntimeKey(item) == prepared.RuntimeKey);
        if (target == null)
        {
            return await FailTargetAndCleanupAsync(
                    context,
                    prepared.RuntimeKey,
                    $"Deployment `{prepared.RuntimeKey}` was not registered after startup.")
                .ConfigureAwait(false);
        }

        var runtime = await InspectProcessRuntimeIfNeededAsync(context, target, prepared.Mod).ConfigureAwait(false);
        if (!runtime.Succeeded)
        {
            return await FailTargetAndCleanupAsync(context, prepared.RuntimeKey, runtime.Message).ConfigureAwait(false);
        }

        var healthProbe = context.RequestServices.GetService<IDynamicAppHealthProbe>();
        if (healthProbe == null)
        {
            return await FailTargetAndCleanupAsync(
                    context,
                    prepared.RuntimeKey,
                    "IDynamicAppHealthProbe is not registered.")
                .ConfigureAwait(false);
        }

        var health = await healthProbe.ProbeAsync(target, prepared.Mod.Health, context.RequestAborted).ConfigureAwait(false);
        if (!health.IsHealthy)
        {
            var message = $"Deployment `{prepared.RuntimeKey}` health probe failed. {health.Message}";
            return await FailTargetAndCleanupAsync(context, prepared.RuntimeKey, message).ConfigureAwait(false);
        }

        var activation = await context.RequestServices.GetRequiredService<IDynamicAppManager>()
            .ActivateInstanceAsync(prepared.RuntimeKey, context.RequestAborted)
            .ConfigureAwait(false);
        if (!activation.Succeeded)
        {
            return await FailTargetAndCleanupAsync(context, prepared.RuntimeKey, activation.Message).ConfigureAwait(false);
        }

        return DynamicAppOperationResult.Success($"Deployment `{prepared.RuntimeKey}` started, healthy, and active.");
    }

    private async Task<DynamicAppOperationResult> FailTargetAndCleanupAsync(
        HttpContext context,
        DynamicAppRuntimeKey runtimeKey,
        string message)
    {
        // Once a process has been started, client disconnect must not cancel the cleanup that releases its port.
        var cleanupContext = context;
        if (context.RequestAborted.IsCancellationRequested)
        {
            cleanupContext = new DefaultHttpContext
            {
                RequestServices = context.RequestServices
            };
            cleanupContext.RequestAborted = CancellationToken.None;
        }

        var stop = await StopTargetAsync(cleanupContext, runtimeKey).ConfigureAwait(false);
        var finalMessage = stop.Succeeded
            ? $"{message} Runtime `{runtimeKey}` was stopped after the failure."
            : $"{message} Cleanup failed for `{runtimeKey}`: {stop.Message}";
        context.RequestServices.GetRequiredService<IDynamicAppStateStore>()
            .UpdateState(runtimeKey, DynamicModLifecycleState.Failed, DynamicModHealthStatus.Unhealthy, finalMessage);
        return DynamicAppOperationResult.Failure(finalMessage);
    }

    /// <summary>
    /// <para>zh-cn:在串行创建闸门内校验副本标识和监听端点，避免同一包版本重复登记或多个实例占用同一个协议、主机、端口。</para>
    /// <para>en-us:Validates the deployment identifier and listener endpoint inside a serialized creation gate, preventing duplicate registration for one package version and preventing multiple instances from sharing one protocol, host, and port.</para>
    /// </summary>
    /// <param name="stateStore">
    /// <para>zh-cn:当前 DynamicApp 运行状态存储。</para>
    /// <para>en-us:The current DynamicApp runtime state store.</para>
    /// </param>
    /// <param name="key">
    /// <para>zh-cn:待创建部署对应的 DynamicApp、Mod、版本键。</para>
    /// <para>en-us:The DynamicApp, Mod, and version key for the deployment being created.</para>
    /// </param>
    /// <param name="deploymentId">
    /// <para>zh-cn:待创建的部署实例标识。</para>
    /// <para>en-us:The deployment-instance identifier being created.</para>
    /// </param>
    /// <param name="endpoint">
    /// <para>zh-cn:待创建实例的基础监听地址；比较时仅使用协议、主机和端口。</para>
    /// <para>en-us:The new instance base listener address; only scheme, host, and port participate in conflict comparison.</para>
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// <para>zh-cn:当部署标识或监听端点已经被占用时抛出。</para>
    /// <para>en-us:Thrown when the deployment identifier or listener endpoint is already in use.</para>
    /// </exception>
    private static void ValidateDeploymentConflicts(
        IDynamicAppStateStore stateStore,
        DynamicModVersionKey key,
        string deploymentId,
        Uri endpoint)
    {
        var targets = stateStore.ListTargets();
        var duplicateDeployment = targets.FirstOrDefault(item =>
            item.Key == key
            && string.Equals(NormalizeDeploymentIdValue(item.DeploymentId), deploymentId, StringComparison.OrdinalIgnoreCase));
        if (duplicateDeployment != null)
        {
            throw new InvalidOperationException($"DynamicApp deployment `{key}/{deploymentId}` already exists.");
        }

        var endpointConflict = targets.FirstOrDefault(item => item.State != DynamicModLifecycleState.Stopped
            && IsSameListenerEndpoint(item.BaseAddress, endpoint));
        if (endpointConflict != null)
        {
            var existingRuntimeKey = ToRuntimeKey(endpointConflict);
            throw new InvalidOperationException($"Deployment endpoint `{endpoint}` is already used by `{existingRuntimeKey}`.");
        }
    }

    /// <summary>
    /// <para>zh-cn:将空部署标识统一视为默认实例，保证旧状态记录与新集群状态使用相同比较规则。</para>
    /// <para>en-us:Normalizes an empty deployment identifier to the default instance so legacy state records and clustered state use the same comparison rule.</para>
    /// </summary>
    /// <param name="deploymentId">
    /// <para>zh-cn:待比较的部署实例标识。</para>
    /// <para>en-us:The deployment identifier to compare.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:规范化后的部署实例标识。</para>
    /// <para>en-us:The normalized deployment identifier.</para>
    /// </returns>
    private static string NormalizeDeploymentIdValue(string? deploymentId)
        => string.IsNullOrWhiteSpace(deploymentId) ? "default" : deploymentId.Trim();

    /// <summary>
    /// <para>zh-cn:比较两个基础监听地址是否指向同一个协议、主机和端口；路径不同不会绕过监听冲突。</para>
    /// <para>en-us:Checks whether two base listener addresses use the same scheme, host, and port; different paths do not bypass a listener conflict.</para>
    /// </summary>
    /// <param name="left">
    /// <para>zh-cn:已登记的监听地址。</para>
    /// <para>en-us:The registered listener address.</para>
    /// </param>
    /// <param name="right">
    /// <para>zh-cn:待创建的监听地址。</para>
    /// <para>en-us:The listener address being created.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:两个地址是否占用相同的监听端点。</para>
    /// <para>en-us:Whether both addresses occupy the same listener endpoint.</para>
    /// </returns>
    private static bool IsSameListenerEndpoint(Uri left, Uri right)
        => string.Equals(left.Scheme, right.Scheme, StringComparison.OrdinalIgnoreCase)
            && string.Equals(left.Host, right.Host, StringComparison.OrdinalIgnoreCase)
            && left.Port == right.Port;

    /// <summary>
    /// <para>zh-cn:尝试绑定本机部署监听端点，提前发现被其他进程占用的端口；远程主机地址不在本机预检范围内。</para>
    /// <para>en-us:Attempts to bind the local deployment listener endpoint to detect ports already occupied by other processes; remote host addresses are outside the local preflight scope.</para>
    /// </summary>
    /// <param name="endpoint">
    /// <para>zh-cn:待创建部署的基础监听地址。</para>
    /// <para>en-us:The base listener address of the deployment being created.</para>
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// <para>zh-cn:本机端口无法绑定时抛出。</para>
    /// <para>en-us:Thrown when the local port cannot be bound.</para>
    /// </exception>
    private static void EnsureDeploymentEndpointAvailable(Uri endpoint)
    {
        if (!TryResolveLocalListenerAddress(endpoint.Host, out var address))
        {
            return;
        }

        try
        {
            using var listener = new TcpListener(address, endpoint.Port);
            listener.Start();
            listener.Stop();
        }
        catch (SocketException exception)
        {
            throw new InvalidOperationException(
                $"Deployment endpoint `{endpoint}` is not available for local binding. {exception.SocketErrorCode}.",
                exception);
        }
    }

    /// <summary>
    /// <para>zh-cn:将回环、通配监听地址和明确的 IP 地址解析为本机端口预检地址；无法确认属于本机的主机名返回 false。</para>
    /// <para>en-us:Resolves loopback, wildcard, and explicit IP listener hosts for local port preflight; host names that cannot be confirmed as local return false.</para>
    /// </summary>
    /// <param name="host">
    /// <para>zh-cn:URI 主机名。</para>
    /// <para>en-us:The URI host name.</para>
    /// </param>
    /// <param name="address">
    /// <para>zh-cn:解析得到的本机监听地址。</para>
    /// <para>en-us:The resolved local listener address.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:是否应执行本机端口预检。</para>
    /// <para>en-us:Whether local port preflight should be performed.</para>
    /// </returns>
    private static bool TryResolveLocalListenerAddress(string host, out IPAddress address)
    {
        if (host is "localhost" or "127.0.0.1")
        {
            address = IPAddress.Loopback;
            return true;
        }

        if (host is "0.0.0.0" or "*" or "+")
        {
            address = IPAddress.Any;
            return true;
        }

        if (host == "::")
        {
            address = IPAddress.IPv6Any;
            return true;
        }

        if (IPAddress.TryParse(host, out address!))
        {
            return IPAddress.IsLoopback(address);
        }

        address = IPAddress.None;
        return false;
    }

    private static async Task<IReadOnlyCollection<DynamicAppPackageApplicationView>> ListManifestPackageApplicationsAsync(HttpContext context, IReadOnlyCollection<DynamicAppModPackageModView> packageViews)
    {
        var manifestStore = context.RequestServices.GetService<IDynamicAppManifestStore>();
        if (manifestStore == null)
        {
            return Array.Empty<DynamicAppPackageApplicationView>();
        }

        var packageCounts = packageViews
            .GroupBy(item => item.DynamicAppId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => (ModCount: group.Select(item => item.ModId).Distinct(StringComparer.OrdinalIgnoreCase).Count(), VersionCount: group.Sum(item => item.Versions.Count)),
                StringComparer.OrdinalIgnoreCase);
        var records = await manifestStore.ListAllAsync(context.RequestAborted).ConfigureAwait(false);
        return records
            .GroupBy(record => record.Manifest.Id.Value, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var manifestMods = group.SelectMany(record => record.Manifest.Mods).ToList();
                packageCounts.TryGetValue(group.Key, out var packageCount);
                return new DynamicAppPackageApplicationView
                {
                    DynamicAppId = group.Key,
                    ModCount = Math.Max(packageCount.ModCount, manifestMods.Select(mod => mod.Id.Value).Distinct(StringComparer.OrdinalIgnoreCase).Count()),
                    VersionCount = Math.Max(packageCount.VersionCount, manifestMods.Select(mod => $"{mod.Id.Value}\u001f{mod.Version.Value}").Distinct(StringComparer.OrdinalIgnoreCase).Count())
                };
            })
            .ToList();
    }

    private static async Task<IReadOnlyCollection<DynamicAppModPackageModView>> ListInstalledModPackagesAsync(HttpContext context, ISet<string> existingVersionKeys)
    {
        var manifestStore = context.RequestServices.GetService<IDynamicAppManifestStore>();
        if (manifestStore == null)
        {
            return Array.Empty<DynamicAppModPackageModView>();
        }

        var records = await manifestStore.ListAllAsync(context.RequestAborted).ConfigureAwait(false);
        var versions = new List<DynamicAppModPackageVersionView>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var record in records.OrderByDescending(item => item.SavedAtUtc))
        {
            foreach (var mod in record.Manifest.Mods)
            {
                var versionKey = BuildPackageVersionKey(record.Manifest.Id.Value, mod.Id.Value, mod.Version.Value);
                if (existingVersionKeys.Contains(versionKey) || !seen.Add(versionKey))
                {
                    continue;
                }

                var view = TryBuildInstalledModVersion(record.Manifest.Id.Value, mod, record.SavedAtUtc);
                if (view != null)
                {
                    versions.Add(view);
                }
            }
        }

        return versions
            .GroupBy(version => BuildPackageModKey(version.DynamicAppId, version.ModId), StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var first = group.First();
                return new DynamicAppModPackageModView
                {
                    DynamicAppId = first.DynamicAppId,
                    ModId = first.ModId,
                    Versions = group.OrderBy(version => version.Version, StringComparer.OrdinalIgnoreCase).ToList()
                };
            })
            .OrderBy(item => item.DynamicAppId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.ModId, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static DynamicAppModPackageVersionView? TryBuildInstalledModVersion(string dynamicAppId, DynamicModManifest mod, DateTimeOffset savedAtUtc)
    {
        var modId = mod.Id.Value;
        var version = mod.Version.Value;
        if (mod.ExecutionMode == DynamicModExecutionMode.ExistingContainer)
        {
            return new DynamicAppModPackageVersionView
            {
                DynamicAppId = dynamicAppId,
                ModId = modId,
                ExecutionMode = mod.ExecutionMode.ToString(),
                Version = version,
                SourceKind = "ManifestOnly",
                ContainerName = mod.Artifact.ContainerName ?? string.Empty,
                Endpoint = mod.Artifact.Endpoint ?? string.Empty,
                PackageFilePath = string.Empty,
                ExtractedDirectory = string.Empty,
                PackageBytes = 0,
                SavedAtUtc = savedAtUtc,
                Files = Array.Empty<DynamicAppModPackageFileView>()
            };
        }

        var artifactFile = mod.Artifact.File;
        if (string.IsNullOrWhiteSpace(artifactFile))
        {
            return null;
        }

        var fullEntryPath = Path.GetFullPath(Path.IsPathFullyQualified(artifactFile)
            ? artifactFile
            : Path.Combine(AppContext.BaseDirectory, artifactFile));
        if (!File.Exists(fullEntryPath))
        {
            return null;
        }

        var directory = Path.GetDirectoryName(fullEntryPath);
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return null;
        }

        try
        {
            var files = Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
                .Select(path => BuildInstalledModFileView(path))
                .OrderBy(file => file.RelativePath, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (files.Count == 0)
            {
                return null;
            }

            return new DynamicAppModPackageVersionView
            {
                DynamicAppId = dynamicAppId,
                ModId = modId,
                ExecutionMode = mod.ExecutionMode.ToString(),
                Version = version,
                PackageFilePath = string.Empty,
                SourceKind = "InstalledMod",
                ContainerName = mod.Artifact.ContainerName ?? string.Empty,
                Endpoint = mod.Artifact.Endpoint ?? string.Empty,
                ExtractedDirectory = directory,
                PackageBytes = files.Sum(file => file.Length),
                SavedAtUtc = files.Max(file => file.LastModifiedUtc),
                Files = files
            };
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static DynamicAppModPackageFileView BuildInstalledModFileView(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var file = new FileInfo(fullPath);
        return new DynamicAppModPackageFileView
        {
            Name = file.Name,
            RelativePath = Path.GetRelativePath(AppContext.BaseDirectory, fullPath),
            FullPath = fullPath,
            Length = file.Length,
            LastModifiedUtc = file.LastWriteTimeUtc,
            Kind = "InstalledMod"
        };
    }

    private static string BuildPackageModKey(string dynamicAppId, string modId) => $"{dynamicAppId}\u001f{modId}";

    private static string BuildPackageVersionKey(string dynamicAppId, string modId, string version) => $"{dynamicAppId}\u001f{modId}\u001f{version}";

    /// <summary>
    /// <para>zh-cn:归档指定目标版本，停止并移除当前运行目标，清理自愈计数，然后把版本从默认面板列表和启动链路中隔离。</para>
    /// <para>en-us:Archives the specified target version by stopping and removing the current runtime target, clearing self-healing counters, and isolating the version from default panel lists and startup paths.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:当前 HTTP 上下文，Body 必须包含 DynamicAppArchiveTargetRequest。</para>
    /// <para>en-us:The current HTTP context; the body must contain DynamicAppArchiveTargetRequest.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:异步处理任务。</para>
    /// <para>en-us:The asynchronous handling task.</para>
    /// </returns>
    private async Task ArchiveAsync(HttpContext context)
    {
        var request = await ReadRequiredJsonAsync<DynamicAppArchiveTargetRequest>(context).ConfigureAwait(false);
        var key = request.ToKey();
        var stateStore = context.RequestServices.GetRequiredService<IDynamicAppStateStore>();
        await using var familyOperation = await context.RequestServices
            .GetRequiredService<IDynamicAppRuntimeOperationCoordinator>()
            .AcquirePackageAsync(key, context.RequestAborted)
            .ConfigureAwait(false);
        var stopMessage = string.Empty;
        var runtimeKeys = stateStore.ListTargets()
            .Where(item => item.Key == key)
            .Select(ToRuntimeKey)
            .ToArray();
        var stoppedKeys = new List<DynamicAppRuntimeKey>();
        foreach (var runtimeKey in runtimeKeys)
        {
            await using var operation = await context.RequestServices
                .GetRequiredService<IDynamicAppRuntimeOperationCoordinator>()
                .AcquireAsync(runtimeKey, context.RequestAborted)
                .ConfigureAwait(false);
            var stop = await StopTargetAsync(context, runtimeKey).ConfigureAwait(false);
            if (!stop.Succeeded)
            {
                await WriteOperationResultAsync<object>(
                        context,
                        DynamicAppOperationResult.Failure($"Target `{runtimeKey}` was not archived because it could not be stopped. {stop.Message}"))
                    .ConfigureAwait(false);
                return;
            }

            stopMessage = string.IsNullOrWhiteSpace(stopMessage)
                ? stop.Message
                : $"{stopMessage} | {stop.Message}";
            stoppedKeys.Add(runtimeKey);
        }

        foreach (var runtimeKey in stoppedKeys)
        {
            var removed = stateStore.RemoveTarget(runtimeKey);
            if (!removed.Succeeded)
            {
                await WriteOperationResultAsync<object>(
                        context,
                        DynamicAppOperationResult.Failure($"Target `{key}` was stopped but could not be removed from runtime state: {removed.Message}"))
                    .ConfigureAwait(false);
                return;
            }

            context.RequestServices.GetRequiredService<IDynamicAppSelfHealingStateStore>().Remove(runtimeKey);
        }
        context.RequestServices.GetRequiredService<IDynamicAppSelfHealingStateStore>().Remove(key);
        var archive = context.RequestServices.GetRequiredService<IDynamicAppTargetArchiveStore>().Archive(key, request.Reason);
        var message = string.IsNullOrWhiteSpace(stopMessage)
            ? $"Target `{key}` archived."
            : $"Target `{key}` archived. Stop result: {stopMessage}";
        await WriteOperationResultAsync(context, DynamicAppOperationResult.Success(message), archive).ConfigureAwait(false);
    }

    /// <summary>
    /// <para>zh-cn:取消归档指定目标版本，使其重新参与默认面板列表和启动链路。</para>
    /// <para>en-us:Unarchives the specified target version so it participates in default panel lists and startup paths again.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:当前 HTTP 上下文，Body 必须包含 DynamicAppTargetKeyRequest。</para>
    /// <para>en-us:The current HTTP context; the body must contain DynamicAppTargetKeyRequest.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:异步处理任务。</para>
    /// <para>en-us:The asynchronous handling task.</para>
    /// </returns>
    private async Task UnarchiveAsync(HttpContext context)
    {
        var request = await ReadRequiredJsonAsync<DynamicAppTargetKeyRequest>(context).ConfigureAwait(false);
        var runtimeKey = request.ToRuntimeKey();
        await using var operation = await context.RequestServices
            .GetRequiredService<IDynamicAppRuntimeOperationCoordinator>()
            .AcquireAsync(runtimeKey, context.RequestAborted)
            .ConfigureAwait(false);
        var archiveStore = context.RequestServices.GetRequiredService<IDynamicAppTargetArchiveStore>();
        var deploymentArchive = string.IsNullOrWhiteSpace(request.DeploymentId)
            ? null
            : archiveStore.FindDeployment(runtimeKey);
        if (deploymentArchive != null)
        {
            var packageStore = context.RequestServices.GetRequiredService<IDynamicAppModPackageStore>();
            DynamicAppModPackageDeploymentArchiveRecord? restore = null;
            if (!string.IsNullOrWhiteSpace(deploymentArchive.ArchiveDirectory))
            {
                restore = await packageStore.RestoreArchivedDeploymentAsync(runtimeKey, context.RequestAborted).ConfigureAwait(false);
            }

            var target = deploymentArchive.RuntimeTarget
                ?? throw new InvalidOperationException($"Archived runtime target `{runtimeKey}` has no configuration snapshot.");
            if (restore?.Moved == true)
            {
                target.DeploymentDirectory = restore.DeploymentDirectory;
                if (!string.IsNullOrWhiteSpace(deploymentArchive.ArchiveDirectory))
                {
                    target.DeploymentEntryFilePath = target.DeploymentEntryFilePath
                        .Replace(deploymentArchive.ArchiveDirectory, restore.DeploymentDirectory, StringComparison.OrdinalIgnoreCase);
                }
            }
            target.State = DynamicModLifecycleState.Stopped;
            target.HealthStatus = DynamicModHealthStatus.Stopped;
            target.DeploymentId = runtimeKey.DeploymentId;
            target.LastStatusMessage = "Restored from archive; the instance remains stopped.";
            context.RequestServices.GetRequiredService<IDynamicAppStateStore>().UpsertTarget(target);
            if (deploymentArchive.SelfHealing != null)
            {
                context.RequestServices.GetRequiredService<IDynamicAppSelfHealingStateStore>().Upsert(deploymentArchive.SelfHealing);
            }

            archiveStore.RemoveDeployment(runtimeKey);
            await WriteOperationResultAsync<object>(
                    context,
                    DynamicAppOperationResult.Success($"DynamicApp deployment `{runtimeKey}` was restored from archive."))
                .ConfigureAwait(false);
            return;
        }

        var key = request.ToKey();
        var removed = archiveStore.Unarchive(key);
        var result = DynamicAppOperationResult.Success(removed
            ? $"Target `{key}` unarchived."
            : $"Target `{key}` was not archived.");
        await WriteOperationResultAsync<object>(context, result).ConfigureAwait(false);
    }

    /// <summary>
    /// <para>zh-cn:删除归档记录及其对应的实例归档文件；旧版本级归档只删除记录。</para>
    /// <para>en-us:Deletes an archive record and its corresponding runtime archive files; legacy version-level archives only remove the record.</para>
    /// </summary>
    private static async Task RemoveArchivedAsync(HttpContext context)
    {
        var request = await ReadRequiredJsonAsync<DynamicAppTargetKeyRequest>(context).ConfigureAwait(false);
        var archiveStore = context.RequestServices.GetRequiredService<IDynamicAppTargetArchiveStore>();
        var deploymentArchive = string.IsNullOrWhiteSpace(request.DeploymentId)
            ? null
            : archiveStore.FindDeployment(request.ToRuntimeKey());
        if (deploymentArchive != null)
        {
            var runtimeKey = request.ToRuntimeKey();
            await context.RequestServices.GetRequiredService<IDynamicAppModPackageStore>()
                .RemoveArchivedDeploymentAsync(runtimeKey, context.RequestAborted)
                .ConfigureAwait(false);
            archiveStore.RemoveDeployment(runtimeKey);
            await WriteOperationResultAsync<object>(
                    context,
                    DynamicAppOperationResult.Success($"DynamicApp deployment archive `{runtimeKey}` was deleted."))
                .ConfigureAwait(false);
            return;
        }

        if (!string.IsNullOrWhiteSpace(request.DeploymentId))
        {
            var runtimeKey = request.ToRuntimeKey();
            var removedOrphanedFiles = await context.RequestServices.GetRequiredService<IDynamicAppModPackageStore>()
                .RemoveArchivedDeploymentAsync(runtimeKey, context.RequestAborted)
                .ConfigureAwait(false);
            if (removedOrphanedFiles)
            {
                await WriteOperationResultAsync<object>(
                        context,
                        DynamicAppOperationResult.Success($"DynamicApp deployment archive `{runtimeKey}` files were deleted."))
                    .ConfigureAwait(false);
                return;
            }
        }

        var removed = archiveStore.Unarchive(request.ToKey());
        await WriteOperationResultAsync<object>(
                context,
                DynamicAppOperationResult.Success(removed
                    ? $"Target archive `{request.ToKey()}` was deleted."
                    : $"Target archive `{request.ToKey()}` was not found."))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// <para>zh-cn:读取运行目标前同步 Process 模式运行态；当持久化状态显示可运行但当前宿主没有对应活进程时，将目标修正为 Failed/Unhealthy。</para>
    /// <para>en-us:Synchronizes Process-mode runtime state before reading targets; when persisted state looks runnable but the current host no longer owns a live process, the target is corrected to Failed/Unhealthy.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:当前 HTTP 上下文，用于解析状态存储、Manifest 存储和 Process Runner。</para>
    /// <para>en-us:The current HTTP context used to resolve the state store, manifest store, and Process runner.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:同步后的目标快照集合。</para>
    /// <para>en-us:The synchronized target snapshot collection.</para>
    /// </returns>
    private static async Task<IReadOnlyCollection<DynamicAppRouteTarget>> ListTargetsWithRuntimeSyncAsync(HttpContext context)
    {
        var stateStore = context.RequestServices.GetRequiredService<IDynamicAppStateStore>();
        var manifestStore = context.RequestServices.GetService<IDynamicAppManifestStore>();
        var runner = context.RequestServices.GetService<IDynamicAppModRunner>();
        var targets = stateStore.ListTargets();
        if (manifestStore == null || runner == null)
        {
            return targets;
        }

        var changed = false;
        foreach (var target in targets.Where(ShouldInspectProcessRuntime))
        {
            var manifestRecord = await manifestStore.FindAsync(target.Key, context.RequestAborted).ConfigureAwait(false);
            var mod = manifestRecord?.Manifest.Mods.FirstOrDefault(item => item.Id == target.Key.ModId && item.Version == target.Key.Version);
            if (mod?.ExecutionMode != DynamicModExecutionMode.Process)
            {
                continue;
            }

            var runtime = await runner.InspectAsync(ToRuntimeKey(target), context.RequestAborted).ConfigureAwait(false);
            if (runtime.Succeeded)
            {
                continue;
            }

            stateStore.UpdateState(
                ToRuntimeKey(target),
                DynamicModLifecycleState.Failed,
                DynamicModHealthStatus.Unhealthy,
                $"Process target `{ToRuntimeKey(target)}` runtime inspection failed. {runtime.Message}");
            changed = true;
        }

        return changed ? stateStore.ListTargets() : targets;
    }

    /// <summary>
    /// <para>zh-cn:判断运行目标是否需要执行 Process 进程存活检查；已停止和已失败目标不会重复检查。</para>
    /// <para>en-us:Determines whether a runtime target needs Process liveness inspection; stopped and failed targets are not inspected repeatedly.</para>
    /// </summary>
    /// <param name="target">
    /// <para>zh-cn:运行目标快照。</para>
    /// <para>en-us:The runtime target snapshot.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:是否需要检查当前宿主持有的进程。</para>
    /// <para>en-us:Whether the process owned by the current host should be inspected.</para>
    /// </returns>
    private static bool ShouldInspectProcessRuntime(DynamicAppRouteTarget target)
    {
        return target.State is DynamicModLifecycleState.Active or DynamicModLifecycleState.Starting or DynamicModLifecycleState.Draining
            || target.HealthStatus is DynamicModHealthStatus.Healthy or DynamicModHealthStatus.Starting or DynamicModHealthStatus.Draining;
    }

    /// <summary>
    /// <para>zh-cn:列出控制面板应用视图，合并 Manifest 历史和当前运行目标，便于前端一次性渲染挂载 Mod 状态。</para>
    /// <para>en-us:Lists control-panel application views by merging manifest history and current runtime targets for one-shot rendering of mounted Mod state.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:当前 HTTP 上下文。</para>
    /// <para>en-us:The current HTTP context.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:异步处理任务。</para>
    /// <para>en-us:The asynchronous handling task.</para>
    /// </returns>
    private async Task ListApplicationsAsync(HttpContext context)
    {
        var manifestRecords = await context.RequestServices.GetRequiredService<IDynamicAppManifestStore>()
            .ListAllAsync(context.RequestAborted)
            .ConfigureAwait(false);
        var targets = await ListTargetsWithRuntimeSyncAsync(context).ConfigureAwait(false);
        var selfHealingStates = context.RequestServices.GetRequiredService<IDynamicAppSelfHealingStateStore>().List();
        var archivedTargets = context.RequestServices.GetRequiredService<IDynamicAppTargetArchiveStore>().List();
        var views = BuildControlPanelApplications(manifestRecords, targets, selfHealingStates, archivedTargets);
        await WriteJsonAsync(context, DynamicAppApiResponse<IReadOnlyCollection<DynamicAppControlPanelApplicationView>>.Success(views, "DynamicApp applications loaded.")).ConfigureAwait(false);
    }

    /// <summary>
    /// <para>zh-cn:列出全部 Manifest 历史记录；用于控制面板审计或人工选择历史版本。</para>
    /// <para>en-us:Lists all manifest history records for control-panel audit or manual historical-version selection.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:当前 HTTP 上下文。</para>
    /// <para>en-us:The current HTTP context.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:异步处理任务。</para>
    /// <para>en-us:The asynchronous handling task.</para>
    /// </returns>
    private async Task ListAllManifestsAsync(HttpContext context)
    {
        var records = await context.RequestServices.GetRequiredService<IDynamicAppManifestStore>()
            .ListAllAsync(context.RequestAborted)
            .ConfigureAwait(false);
        await WriteJsonAsync(context, DynamicAppApiResponse<IReadOnlyCollection<DynamicAppManifestRecord>>.Success(records, "DynamicApp manifest history loaded.")).ConfigureAwait(false);
    }

    private async Task ListManifestsAsync(HttpContext context, string dynamicAppId)
    {
        var records = await context.RequestServices.GetRequiredService<IDynamicAppManifestStore>()
            .ListAsync(new DynamicAppId(dynamicAppId), context.RequestAborted)
            .ConfigureAwait(false);
        await WriteJsonAsync(context, DynamicAppApiResponse<IReadOnlyCollection<DynamicAppManifestRecord>>.Success(records, "DynamicApp manifest history loaded.")).ConfigureAwait(false);
    }

    /// <summary>
    /// <para>zh-cn:按路径参数读取单个运行目标，路径格式为 target/{dynamicAppId}/{modId}/{version}。</para>
    /// <para>en-us:Reads one runtime target by path parameters in the target/{dynamicAppId}/{modId}/{version} format.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:当前 HTTP 上下文。</para>
    /// <para>en-us:The current HTTP context.</para>
    /// </param>
    /// <param name="targetRoute">
    /// <para>zh-cn:去掉 target/ 前缀后的目标路由片段。</para>
    /// <para>en-us:The target route segment after removing the target/ prefix.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:异步处理任务。</para>
    /// <para>en-us:The asynchronous handling task.</para>
    /// </returns>
    private async Task GetTargetAsync(HttpContext context, string targetRoute)
    {
        var runtimeKey = ParseRuntimeKeyRoute(targetRoute);
        var target = (await ListTargetsWithRuntimeSyncAsync(context).ConfigureAwait(false))
            .FirstOrDefault(item => ToRuntimeKey(item) == runtimeKey);
        if (target == null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await WriteJsonAsync(context, DynamicAppApiResponse<object>.Failure($"Target `{runtimeKey}` is not registered.", "dynamicapp.target_not_found")).ConfigureAwait(false);
            return;
        }

        await WriteJsonAsync(context, DynamicAppApiResponse<DynamicAppRouteTarget>.Success(target, "DynamicApp target loaded.")).ConfigureAwait(false);
    }

    /// <summary>
    /// <para>zh-cn:基于 Manifest 历史启动一个指定 Mod 版本，健康探测通过后自动激活。</para>
    /// <para>en-us:Starts a specified Mod version from manifest history and automatically activates it after a successful health probe.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:当前 HTTP 上下文。</para>
    /// <para>en-us:The current HTTP context.</para>
    /// </param>
    /// <param name="runtimeKey">
    /// <para>zh-cn:目标 Mod 版本键。</para>
    /// <para>en-us:The target Mod version key.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:启动和激活结果。</para>
    /// <para>en-us:The start and activation result.</para>
    /// </returns>
    private async Task<DynamicAppOperationResult> StartTargetAsync(HttpContext context, DynamicAppRuntimeKey runtimeKey)
    {
        var key = runtimeKey.PackageKey;
        if (context.RequestServices.GetRequiredService<IDynamicAppTargetArchiveStore>().IsArchived(runtimeKey))
        {
            return DynamicAppOperationResult.Failure($"Target `{key}` is archived and cannot be started until it is unarchived.");
        }

        var manifestRecord = await context.RequestServices.GetRequiredService<IDynamicAppManifestStore>()
            .FindAsync(key, context.RequestAborted)
            .ConfigureAwait(false);
        if (manifestRecord == null)
        {
            return DynamicAppOperationResult.Failure($"Manifest history for target `{key}` was not found.");
        }

        var sourceMod = manifestRecord.Manifest.Mods.FirstOrDefault(item => item.Id == key.ModId && item.Version == key.Version);
        if (sourceMod == null)
        {
            return DynamicAppOperationResult.Failure($"Manifest history record `{manifestRecord.RecordId}` does not contain target `{key}`.");
        }

        var stateStore = context.RequestServices.GetRequiredService<IDynamicAppStateStore>();
        var existingTarget = stateStore.ListTargets().FirstOrDefault(item => ToRuntimeKey(item) == runtimeKey);
        if (!string.Equals(runtimeKey.DeploymentId, "default", StringComparison.OrdinalIgnoreCase) && existingTarget == null)
        {
            return DynamicAppOperationResult.Failure($"Deployment target `{runtimeKey}` is not registered.");
        }

        var mod = CloneMod(sourceMod);
        ApplyRuntimeTarget(mod, existingTarget);
        var start = await StartModAsync(context, manifestRecord.Manifest, mod).ConfigureAwait(false);
        if (!start.Succeeded)
        {
            var message = await AppendProcessDiagnosticsAsync(context, runtimeKey, start.Message).ConfigureAwait(false);
            PublishFailedStartState(context, manifestRecord.Manifest, mod, runtimeKey, message);
            return DynamicAppOperationResult.Failure(message);
        }

        var target = stateStore.ListTargets().FirstOrDefault(item => ToRuntimeKey(item) == runtimeKey);
        if (target == null)
        {
            return await FailTargetAndCleanupAsync(
                    context,
                    runtimeKey,
                    $"Target `{runtimeKey}` was not registered after startup.")
                .ConfigureAwait(false);
        }

        if (mod.ExecutionMode == DynamicModExecutionMode.ExistingContainer)
        {
            return await ActivateExistingContainerTargetAsync(context, key, target).ConfigureAwait(false);
        }

        var runtime = await InspectProcessRuntimeIfNeededAsync(context, target, mod).ConfigureAwait(false);
        if (!runtime.Succeeded)
        {
            var message = await AppendProcessDiagnosticsAsync(context, runtimeKey, $"Runtime target `{runtimeKey}` startup inspection failed. {runtime.Message}").ConfigureAwait(false);
            return await FailTargetAndCleanupAsync(context, runtimeKey, message).ConfigureAwait(false);
        }

        var healthProbe = context.RequestServices.GetService<IDynamicAppHealthProbe>();
        if (healthProbe == null)
        {
            return DynamicAppOperationResult.Failure("IDynamicAppHealthProbe is not registered.");
        }

        var health = await healthProbe.ProbeAsync(target, mod.Health, context.RequestAborted).ConfigureAwait(false);
        if (!health.IsHealthy)
        {
            var message = await AppendProcessDiagnosticsAsync(context, runtimeKey, $"Runtime target `{runtimeKey}` health probe failed. {health.Message}").ConfigureAwait(false);
            return await FailTargetAndCleanupAsync(context, runtimeKey, message).ConfigureAwait(false);
        }

        var activation = await context.RequestServices.GetRequiredService<IDynamicAppManager>()
            .ActivateInstanceAsync(runtimeKey, context.RequestAborted)
            .ConfigureAwait(false);
        if (!activation.Succeeded)
        {
            return await FailTargetAndCleanupAsync(context, runtimeKey, activation.Message).ConfigureAwait(false);
        }

        context.RequestServices.GetRequiredService<IDynamicAppSelfHealingStateStore>().Remove(runtimeKey);
        return DynamicAppOperationResult.Success($"Target `{runtimeKey}` started, healthy, and active.");
    }

    /// <summary>
    /// <para>zh-cn:激活 ExistingContainer 目标；该模式只通过容器引擎标准检查实例状态，不发起 HTTP 健康检查或路由测试。</para>
    /// <para>en-us:Activates an ExistingContainer target; this mode checks instance status only through the container engine standard and does not issue HTTP health checks or route tests.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:当前 HTTP 上下文，用于解析 Docker 容器目录、状态存储和 Manager。</para>
    /// <para>en-us:The current HTTP context used to resolve the Docker container catalog, state store, and manager.</para>
    /// </param>
    /// <param name="runtimeKey">
    /// <para>zh-cn:目标 Mod 版本键。</para>
    /// <para>en-us:The target Mod version key.</para>
    /// </param>
    /// <param name="target">
    /// <para>zh-cn:刚启动并写入状态存储的运行目标。</para>
    /// <para>en-us:The runtime target that was just started and written to the state store.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:容器引擎状态检查和激活结果。</para>
    /// <para>en-us:The container engine status inspection and activation result.</para>
    /// </returns>
    private static async Task<DynamicAppOperationResult> ActivateExistingContainerTargetAsync(HttpContext context, DynamicModVersionKey runtimeKey, DynamicAppRouteTarget target)
    {
        var (operation, view) = await InspectExistingContainerStatusAsync(context, runtimeKey).ConfigureAwait(false);
        var stateStore = context.RequestServices.GetRequiredService<IDynamicAppStateStore>();
        if (!operation.Succeeded || view?.IsAvailable != true)
        {
            var message = operation.Message;
            stateStore.UpdateState(runtimeKey, DynamicModLifecycleState.Failed, DynamicModHealthStatus.Unhealthy, message);
            return DynamicAppOperationResult.Failure(message);
        }

        var activation = await context.RequestServices.GetRequiredService<IDynamicAppManager>()
            .ActivateInstanceAsync(new DynamicAppRuntimeKey(runtimeKey, target.DeploymentId), context.RequestAborted)
            .ConfigureAwait(false);
        if (!activation.Succeeded)
        {
            return activation;
        }

        context.RequestServices.GetRequiredService<IDynamicAppSelfHealingStateStore>().Remove(runtimeKey);
        return DynamicAppOperationResult.Success($"ExistingContainer target `{runtimeKey}` is active from Docker container `{view.Name}`. {view.Status}");
    }

    /// <summary>
    /// <para>zh-cn:读取 ExistingContainer 目标的容器引擎即时状态；缺少已有容器目录或容器引擎访问失败时返回失败结果。</para>
    /// <para>en-us:Reads the live container engine status for an ExistingContainer target; returns a failed result when the existing-container catalog is missing or the container engine access fails.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:当前 HTTP 上下文，用于解析 Docker 容器目录。</para>
    /// <para>en-us:The current HTTP context used to resolve the Docker container catalog.</para>
    /// </param>
    /// <param name="runtimeKey">
    /// <para>zh-cn:目标 Mod 版本键。</para>
    /// <para>en-us:The target Mod version key.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:容器引擎状态检查结果和容器视图；失败时容器视图为空。</para>
    /// <para>en-us:The container engine status operation and container view; the view is null when inspection fails.</para>
    /// </returns>
    private static async Task<(DynamicAppOperationResult Operation, DynamicAppExistingContainerView? View)> InspectExistingContainerStatusAsync(HttpContext context, DynamicModVersionKey runtimeKey)
    {
        var catalog = context.RequestServices.GetService<IDynamicAppExistingContainerCatalog>();
        if (catalog == null)
        {
            return (DynamicAppOperationResult.Failure("IDynamicAppExistingContainerCatalog is not registered."), null);
        }

        try
        {
            var view = await catalog.InspectAsync(runtimeKey, context.RequestAborted).ConfigureAwait(false);
            var message = view.IsAvailable
                ? $"Docker container `{view.Name}` is running for `{runtimeKey}`. {view.Status}"
                : $"Docker container `{view.Name}` is not available for `{runtimeKey}`. {view.Status}";
            return (view.IsAvailable ? DynamicAppOperationResult.Success(message) : DynamicAppOperationResult.Failure(message), view);
        }
        catch (Exception exception) when (exception is InvalidOperationException or HttpRequestException or SocketException or IOException)
        {
            return (DynamicAppOperationResult.Failure($"Docker container status inspection failed for `{runtimeKey}`. {exception.Message}"), null);
        }
    }

    /// <summary>
    /// <para>zh-cn:按 Mod 执行模式检查启动后的运行态；Process 模式必须由当前 Runner 持有活进程，其他模式直接视为通过。</para>
    /// <para>en-us:Inspects post-start runtime by Mod execution mode; Process mode must be backed by a live process owned by the current runner, while other modes pass through.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:当前 HTTP 上下文，用于解析 Process Runner。</para>
    /// <para>en-us:The current HTTP context used to resolve the Process runner.</para>
    /// </param>
    /// <param name="target">
    /// <para>zh-cn:刚启动并写入状态存储的运行目标。</para>
    /// <para>en-us:The runtime target that was just started and written to the state store.</para>
    /// </param>
    /// <param name="mod">
    /// <para>zh-cn:目标 Mod Manifest，用于判断执行模式。</para>
    /// <para>en-us:The target Mod manifest used to determine execution mode.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:运行态检查结果；非 Process 模式返回成功。</para>
    /// <para>en-us:The runtime inspection result; non-Process modes return success.</para>
    /// </returns>
    private static async Task<DynamicAppOperationResult> InspectProcessRuntimeIfNeededAsync(HttpContext context, DynamicAppRouteTarget target, DynamicModManifest mod)
    {
        if (mod.ExecutionMode != DynamicModExecutionMode.Process)
        {
            return DynamicAppOperationResult.Success($"Target `{target.Key}` does not require Process runtime inspection.");
        }

        var runner = context.RequestServices.GetService<IDynamicAppModRunner>();
        return runner == null
            ? DynamicAppOperationResult.Failure("IDynamicAppModRunner is not registered.")
            : await runner.InspectAsync(ToRuntimeKey(target), context.RequestAborted).ConfigureAwait(false);
    }

    /// <summary>
    /// <para>zh-cn:启动失败时发布 Failed/Unhealthy 状态；如果目标尚未注册，则根据 Manifest 创建一个失败目标，避免界面继续展示旧 Active 状态。</para>
    /// <para>en-us:Publishes Failed/Unhealthy state after startup failure; when the target is not registered yet, a failed target is created from the manifest so the UI does not keep showing stale Active state.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:当前 HTTP 上下文，用于访问状态存储。</para>
    /// <para>en-us:The current HTTP context used to access the state store.</para>
    /// </param>
    /// <param name="manifest">
    /// <para>zh-cn:包含失败 Mod 的 Manifest。</para>
    /// <para>en-us:The manifest containing the failed Mod.</para>
    /// </param>
    /// <param name="mod">
    /// <para>zh-cn:启动失败的 Mod Manifest。</para>
    /// <para>en-us:The Mod manifest that failed to start.</para>
    /// </param>
    /// <param name="message">
    /// <para>zh-cn:底层启动失败消息。</para>
    /// <para>en-us:The underlying startup failure message.</para>
    /// </param>
    /// <param name="runtimeKey">
    /// <para>zh-cn:启动失败的运行实例键。</para>
    /// <para>en-us:The runtime-instance key that failed to start.</para>
    /// </param>
    private static void PublishFailedStartState(
        HttpContext context,
        DynamicAppManifest manifest,
        DynamicModManifest mod,
        DynamicAppRuntimeKey runtimeKey,
        string message)
    {
        var stateStore = context.RequestServices.GetRequiredService<IDynamicAppStateStore>();
        var statusMessage = string.IsNullOrWhiteSpace(message)
            ? $"Target `{runtimeKey}` failed to start."
            : message;
        var update = stateStore.UpdateState(runtimeKey, DynamicModLifecycleState.Failed, DynamicModHealthStatus.Unhealthy, statusMessage);
        if (update.Succeeded)
        {
            return;
        }

        stateStore.UpsertTarget(new DynamicAppRouteTarget
        {
            Key = runtimeKey.PackageKey,
            DeploymentId = runtimeKey.DeploymentId,
            DeploymentDirectory = mod.Artifact.DeploymentDirectory ?? string.Empty,
            DeploymentEntryFilePath = mod.Artifact.File ?? string.Empty,
            BaseAddress = ResolveStartupBaseAddress(mod),
            HealthPath = mod.Health.Path,
            HealthProbeTimeoutSeconds = mod.Health.StartupTimeoutSeconds,
            State = DynamicModLifecycleState.Failed,
            HealthStatus = DynamicModHealthStatus.Unhealthy,
            InstanceId = string.Empty,
            LastStatusMessage = statusMessage
        });
    }

    /// <summary>
    /// <para>zh-cn:按 Mod 执行模式分派启动操作；Process 模式使用 Runner，容器模式使用 Orchestrator，本地开发联调模式只注册外部目标。</para>
    /// <para>en-us:Dispatches start operations by Mod execution mode; Process mode uses the runner, container modes use the orchestrator, and local development mode only registers the external target.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:当前 HTTP 上下文。</para>
    /// <para>en-us:The current HTTP context.</para>
    /// </param>
    /// <param name="manifest">
    /// <para>zh-cn:包含目标 Mod 的 Manifest。</para>
    /// <para>en-us:The manifest containing the target Mod.</para>
    /// </param>
    /// <param name="mod">
    /// <para>zh-cn:需要启动的 Mod Manifest。</para>
    /// <para>en-us:The Mod manifest to start.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:启动结果。</para>
    /// <para>en-us:The start result.</para>
    /// </returns>
    private async Task<DynamicAppOperationResult> StartModAsync(HttpContext context, DynamicAppManifest manifest, DynamicModManifest mod)
    {
        return mod.ExecutionMode switch
        {
            DynamicModExecutionMode.Process => await StartProcessModAsync(context, manifest, mod).ConfigureAwait(false),
            DynamicModExecutionMode.LocalDevelopment => StartLocalDevelopmentMod(context, manifest, mod),
            _ => await StartOrchestratedModAsync(context, manifest, mod).ConfigureAwait(false)
        };
    }

    /// <summary>
    /// <para>zh-cn:注册本地开发联调 Mod；该模式假定业务服务已经由开发者在另一个终端或 IDE 中启动，因此不会创建进程或操作容器。</para>
    /// <para>en-us:Registers a local development Mod; this mode assumes the business service is already started by the developer in another terminal or IDE, so it does not create a process or operate containers.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:当前 HTTP 上下文。</para>
    /// <para>en-us:The current HTTP context.</para>
    /// </param>
    /// <param name="manifest">
    /// <para>zh-cn:包含目标 Mod 的 Manifest。</para>
    /// <para>en-us:The manifest containing the target Mod.</para>
    /// </param>
    /// <param name="mod">
    /// <para>zh-cn:需要注册的本地开发联调 Mod。</para>
    /// <para>en-us:The local development Mod to register.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:注册结果；后续由调用方执行健康探测和激活。</para>
    /// <para>en-us:The registration result; the caller performs health probing and activation afterwards.</para>
    /// </returns>
    private static DynamicAppOperationResult StartLocalDevelopmentMod(HttpContext context, DynamicAppManifest manifest, DynamicModManifest mod)
    {
        var key = new DynamicModVersionKey(manifest.Id, mod.Id, mod.Version);
        var result = context.RequestServices.GetRequiredService<IDynamicAppStateStore>()
            .UpdateState(
                key,
                DynamicModLifecycleState.Starting,
                DynamicModHealthStatus.Starting,
                $"Local development target `{key}` is registered; DynamicApp will forward to `{mod.Artifact.Endpoint}` after health activation.");
        return result.Succeeded
            ? DynamicAppOperationResult.Success($"Local development target `{key}` registered for external debugging.")
            : result;
    }

    /// <summary>
    /// <para>zh-cn:通过进程 Runner 启动 Process 模式 Mod；Runner 未注册时返回失败。</para>
    /// <para>en-us:Starts a Process-mode Mod through the process runner; returns failure when the runner is not registered.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:当前 HTTP 上下文。</para>
    /// <para>en-us:The current HTTP context.</para>
    /// </param>
    /// <param name="manifest">
    /// <para>zh-cn:包含目标 Mod 的 Manifest。</para>
    /// <para>en-us:The manifest containing the target Mod.</para>
    /// </param>
    /// <param name="mod">
    /// <para>zh-cn:需要启动的 Process 模式 Mod。</para>
    /// <para>en-us:The Process-mode Mod to start.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:启动结果。</para>
    /// <para>en-us:The start result.</para>
    /// </returns>
    private async Task<DynamicAppOperationResult> StartProcessModAsync(HttpContext context, DynamicAppManifest manifest, DynamicModManifest mod)
    {
        var runner = context.RequestServices.GetService<IDynamicAppModRunner>();
        return runner == null
            ? DynamicAppOperationResult.Failure("IDynamicAppModRunner is not registered.")
            : await runner.StartAsync(manifest, mod, context.RequestAborted).ConfigureAwait(false);
    }

    private static async Task<string> AppendProcessDiagnosticsAsync(HttpContext context, DynamicAppRuntimeKey runtimeKey, string message)
    {
        var logSink = context.RequestServices.GetService<IDynamicAppProcessLogSink>();
        if (logSink == null)
        {
            return message;
        }

        await Task.Delay(TimeSpan.FromMilliseconds(150), context.RequestAborted).ConfigureAwait(false);
        var logs = await logSink.QueryAsync(runtimeKey, take: 80, context.RequestAborted).ConfigureAwait(false);
        var candidates = logs
            .Where(item => item.Stream.Equals("stderr", StringComparison.OrdinalIgnoreCase) || item.Stream.Equals("exit", StringComparison.OrdinalIgnoreCase))
            .Select(item => $"[{item.Stream}] {item.Message}")
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var diagnostics = candidates
            .Where(IsImportantProcessDiagnostic)
            .TakeLast(8)
            .DefaultIfEmpty()
            .ToArray();
        if (diagnostics.Length == 1 && diagnostics[0] == null)
        {
            diagnostics = candidates.TakeLast(6).ToArray();
        }

        return diagnostics.Length == 0
            ? message
            : message + " Recent process diagnostics: " + string.Join(" | ", diagnostics);
    }

    private static bool IsImportantProcessDiagnostic(string message)
    {
        return message.Contains("exception", StringComparison.OrdinalIgnoreCase)
            || message.Contains("failed to bind", StringComparison.OrdinalIgnoreCase)
            || message.Contains("address already in use", StringComparison.OrdinalIgnoreCase)
            || message.Contains("unhandled", StringComparison.OrdinalIgnoreCase)
            || message.Contains("exit code", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// <para>zh-cn:通过 Orchestrator 启动容器类 Mod；Orchestrator 未注册时返回失败。</para>
    /// <para>en-us:Starts a container-style Mod through the orchestrator; returns failure when the orchestrator is not registered.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:当前 HTTP 上下文。</para>
    /// <para>en-us:The current HTTP context.</para>
    /// </param>
    /// <param name="manifest">
    /// <para>zh-cn:包含目标 Mod 的 Manifest。</para>
    /// <para>en-us:The manifest containing the target Mod.</para>
    /// </param>
    /// <param name="mod">
    /// <para>zh-cn:需要启动的容器类 Mod。</para>
    /// <para>en-us:The container-style Mod to start.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:启动结果。</para>
    /// <para>en-us:The start result.</para>
    /// </returns>
    private async Task<DynamicAppOperationResult> StartOrchestratedModAsync(HttpContext context, DynamicAppManifest manifest, DynamicModManifest mod)
    {
        var orchestrator = context.RequestServices.GetService<IDynamicAppOrchestrator>();
        return orchestrator == null
            ? DynamicAppOperationResult.Failure("IDynamicAppOrchestrator is not registered.")
            : await orchestrator.DeployAsync(manifest, mod, context.RequestAborted).ConfigureAwait(false);
    }

    /// <summary>
    /// <para>zh-cn:停止指定目标；优先按 Manifest 执行模式选择 Runner 或 Orchestrator，缺少 Manifest 时回退到旧的双通道尝试逻辑。</para>
    /// <para>en-us:Stops the specified target; it prefers runner or orchestrator by manifest execution mode and falls back to the legacy dual-channel attempt when manifest history is absent.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:当前 HTTP 上下文。</para>
    /// <para>en-us:The current HTTP context.</para>
    /// </param>
    /// <param name="runtimeKey">
    /// <para>zh-cn:目标 Mod 版本键。</para>
    /// <para>en-us:The target Mod version key.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:停止结果。</para>
    /// <para>en-us:The stop result.</para>
    /// </returns>
    private async Task<DynamicAppOperationResult> StopTargetAsync(HttpContext context, DynamicAppRuntimeKey runtimeKey)
    {
        var key = runtimeKey.PackageKey;
        var manifestRecord = await context.RequestServices.GetRequiredService<IDynamicAppManifestStore>()
            .FindAsync(key, context.RequestAborted)
            .ConfigureAwait(false);
        var mod = manifestRecord?.Manifest.Mods.FirstOrDefault(item => item.Id == key.ModId && item.Version == key.Version);
        DynamicAppOperationResult result;
        if (mod?.ExecutionMode == DynamicModExecutionMode.LocalDevelopment)
        {
            return PublishStoppedStateIfNeeded(context, runtimeKey, DynamicAppOperationResult.Success($"Local development target `{runtimeKey}` detached from DynamicApp routing."));
        }

        if (mod?.ExecutionMode == DynamicModExecutionMode.Process)
        {
            var runner = context.RequestServices.GetService<IDynamicAppModRunner>();
            result = runner == null
                ? DynamicAppOperationResult.Failure("IDynamicAppModRunner is not registered.")
                : await runner.StopAsync(runtimeKey, context.RequestAborted).ConfigureAwait(false);
            return PublishStoppedStateIfNeeded(context, runtimeKey, result);
        }

        if (mod != null)
        {
            var orchestrator = context.RequestServices.GetService<IDynamicAppOrchestrator>();
            result = orchestrator == null
                ? DynamicAppOperationResult.Failure("IDynamicAppOrchestrator is not registered.")
                : await orchestrator.StopAsync(runtimeKey, context.RequestAborted).ConfigureAwait(false);
            return PublishStoppedStateIfNeeded(context, runtimeKey, result);
        }

        result = await StopTargetWithLegacyFallbackAsync(context, runtimeKey).ConfigureAwait(false);
        return PublishStoppedStateIfNeeded(context, runtimeKey, result);
    }

    /// <summary>
    /// <para>zh-cn:停止操作成功后统一发布 Stopped/Stopped 状态，覆盖不同 Runner 或 Orchestrator 对状态回写能力的不一致。</para>
    /// <para>en-us:Publishes a unified Stopped/Stopped state after successful stop operations, covering inconsistent state-write behavior across runners or orchestrators.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:当前 HTTP 上下文。</para>
    /// <para>en-us:The current HTTP context.</para>
    /// </param>
    /// <param name="key">
    /// <para>zh-cn:目标 Mod 版本键。</para>
    /// <para>en-us:The target Mod version key.</para>
    /// </param>
    /// <param name="result">
    /// <para>zh-cn:底层停止操作结果。</para>
    /// <para>en-us:The underlying stop operation result.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:原始停止结果。</para>
    /// <para>en-us:The original stop result.</para>
    /// </returns>
    private static DynamicAppOperationResult PublishStoppedStateIfNeeded(HttpContext context, DynamicAppRuntimeKey key, DynamicAppOperationResult result)
    {
        if (result.Succeeded)
        {
            var stateUpdate = context.RequestServices.GetRequiredService<IDynamicAppStateStore>()
                .UpdateState(key, DynamicModLifecycleState.Stopped, DynamicModHealthStatus.Stopped, result.Message);
            if (!stateUpdate.Succeeded)
            {
                return DynamicAppOperationResult.Failure(
                    $"Stop operation for `{key}` completed, but publishing Stopped state failed: {stateUpdate.Message}");
            }
        }

        return result;
    }

    /// <summary>
    /// <para>zh-cn:保留旧版停止语义，先尝试 Runner，失败后再尝试 Orchestrator。</para>
    /// <para>en-us:Preserves legacy stop semantics by trying the runner first and then the orchestrator after runner failure.</para>
    /// </summary>
    /// <param name="context">
    /// <para>zh-cn:当前 HTTP 上下文。</para>
    /// <para>en-us:The current HTTP context.</para>
    /// </param>
    /// <param name="key">
    /// <para>zh-cn:目标 Mod 版本键。</para>
    /// <para>en-us:The target Mod version key.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:停止结果。</para>
    /// <para>en-us:The stop result.</para>
    /// </returns>
    private async Task<DynamicAppOperationResult> StopTargetWithLegacyFallbackAsync(HttpContext context, DynamicAppRuntimeKey key)
    {
        var packageKey = key.PackageKey;
        var runner = context.RequestServices.GetService<IDynamicAppModRunner>();
        var orchestrator = context.RequestServices.GetService<IDynamicAppOrchestrator>();
        var result = runner == null
            ? DynamicAppOperationResult.Failure("IDynamicAppModRunner is not registered.")
            : await runner.StopAsync(key, context.RequestAborted).ConfigureAwait(false);

        if (!result.Succeeded && orchestrator != null)
        {
            result = await orchestrator.StopAsync(packageKey, context.RequestAborted).ConfigureAwait(false);
        }

        return result;
    }

    /// <summary>
    /// <para>zh-cn:构建控制面板应用视图集合，Manifest 负责描述可启动版本，运行状态负责描述当前挂载目标。</para>
    /// <para>en-us:Builds control-panel application views where manifests describe startable versions and runtime state describes currently mounted targets.</para>
    /// </summary>
    /// <param name="manifestRecords">
    /// <para>zh-cn:Manifest 历史记录集合。</para>
    /// <para>en-us:The manifest history records.</para>
    /// </param>
    /// <param name="targets">
    /// <para>zh-cn:当前运行目标集合。</para>
    /// <para>en-us:The current runtime targets.</para>
    /// </param>
    /// <param name="selfHealingStates">
    /// <para>zh-cn:持久化自愈状态集合，用于把重试窗口信息合并到控制面板视图。</para>
    /// <para>en-us:Persisted self-healing states used to merge retry-window information into control-panel views.</para>
    /// </param>
    /// <param name="archivedTargets">
    /// <para>zh-cn:已归档目标集合，用于从默认控制面板视图中过滤坏包或错误 Manifest 版本。</para>
    /// <para>en-us:Archived targets used to filter bad-package or invalid-manifest versions from default control-panel views.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:控制面板应用视图集合。</para>
    /// <para>en-us:The control-panel application views.</para>
    /// </returns>
    private static IReadOnlyCollection<DynamicAppControlPanelApplicationView> BuildControlPanelApplications(
        IReadOnlyCollection<DynamicAppManifestRecord> manifestRecords,
        IReadOnlyCollection<DynamicAppRouteTarget> targets,
        IReadOnlyCollection<DynamicAppSelfHealingState> selfHealingStates,
        IReadOnlyCollection<DynamicAppArchivedTarget> archivedTargets)
    {
        var orderedRecords = manifestRecords.OrderByDescending(item => item.SavedAtUtc).ToArray();
        var archivedKeys = archivedTargets
            .Where(item => string.IsNullOrWhiteSpace(item.DeploymentId))
            .Select(item => item.Key.ToString())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var manifestAppIds = orderedRecords
            .Where(record => record.Manifest.Mods.Any(mod => !archivedKeys.Contains(new DynamicModVersionKey(record.Manifest.Id, mod.Id, mod.Version).ToString())))
            .Select(item => item.Manifest.Id.Value);
        var appIds = manifestAppIds
            .Concat(targets.Where(item => !archivedKeys.Contains(item.Key.ToString())).Select(item => item.Key.DynamicAppId.Value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(item => item, StringComparer.OrdinalIgnoreCase);

        return appIds.Select(appId => BuildControlPanelApplication(new DynamicAppId(appId), orderedRecords, targets, selfHealingStates, archivedKeys)).ToList();
    }

    /// <summary>
    /// <para>zh-cn:构建单个 DynamicApp 的控制面板应用视图。</para>
    /// <para>en-us:Builds the control-panel application view for one DynamicApp.</para>
    /// </summary>
    /// <param name="dynamicAppId">
    /// <para>zh-cn:DynamicApp 应用标识。</para>
    /// <para>en-us:The DynamicApp application identifier.</para>
    /// </param>
    /// <param name="orderedRecords">
    /// <para>zh-cn:按保存时间倒序排列的 Manifest 历史记录。</para>
    /// <para>en-us:The manifest history records ordered by save time descending.</para>
    /// </param>
    /// <param name="targets">
    /// <para>zh-cn:当前运行目标集合。</para>
    /// <para>en-us:The current runtime targets.</para>
    /// </param>
    /// <param name="selfHealingStates">
    /// <para>zh-cn:持久化自愈状态集合，用于构建单个应用下各 Mod 的自愈展示信息。</para>
    /// <para>en-us:Persisted self-healing states used to build self-healing display information for each Mod under one application.</para>
    /// </param>
    /// <param name="archivedKeys">
    /// <para>zh-cn:已归档目标键集合，用于过滤不应默认展示或启动的版本。</para>
    /// <para>en-us:Archived target key set used to filter versions that should not be shown or started by default.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:控制面板应用视图。</para>
    /// <para>en-us:The control-panel application view.</para>
    /// </returns>
    private static DynamicAppControlPanelApplicationView BuildControlPanelApplication(
        DynamicAppId dynamicAppId,
        IReadOnlyCollection<DynamicAppManifestRecord> orderedRecords,
        IReadOnlyCollection<DynamicAppRouteTarget> targets,
        IReadOnlyCollection<DynamicAppSelfHealingState> selfHealingStates,
        ISet<string> archivedKeys)
    {
        var appRecords = orderedRecords.Where(item => item.Manifest.Id == dynamicAppId).ToArray();
        var appTargets = targets.Where(item => item.Key.DynamicAppId == dynamicAppId).ToArray();
        var keys = appRecords
            .SelectMany(record => record.Manifest.Mods.Select(mod => new DynamicModVersionKey(record.Manifest.Id, mod.Id, mod.Version)))
            .Concat(appTargets.Select(item => item.Key))
            .Where(key => !archivedKeys.Contains(key.ToString()))
            .Distinct()
            .OrderBy(item => item.ModId.Value, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Version.Value, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var mods = keys.Select(key => BuildControlPanelMod(key, appRecords, appTargets, selfHealingStates)).ToList();
        var latest = appRecords.FirstOrDefault();
        return new DynamicAppControlPanelApplicationView
        {
            DynamicAppId = dynamicAppId,
            LatestVersion = latest?.Manifest.Version.Value ?? string.Empty,
            LatestPublishedAtUtc = latest?.SavedAtUtc,
            ModCount = mods.Select(item => item.ModId.Value).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            ActiveModCount = mods.Count(item => item.State == DynamicModLifecycleState.Active && item.HealthStatus == DynamicModHealthStatus.Healthy),
            Mods = mods
        };
    }

    /// <summary>
    /// <para>zh-cn:构建单个 Mod 版本控制面板视图，优先使用运行目标的实时信息，并补充 Manifest 历史元数据。</para>
    /// <para>en-us:Builds one Mod-version control-panel view, preferring runtime target data and enriching it with manifest history metadata.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:目标 Mod 版本键。</para>
    /// <para>en-us:The target Mod version key.</para>
    /// </param>
    /// <param name="appRecords">
    /// <para>zh-cn:当前应用的 Manifest 历史记录。</para>
    /// <para>en-us:The manifest history records for the current application.</para>
    /// </param>
    /// <param name="appTargets">
    /// <para>zh-cn:当前应用的运行目标集合。</para>
    /// <para>en-us:The runtime targets for the current application.</para>
    /// </param>
    /// <param name="selfHealingStates">
    /// <para>zh-cn:持久化自愈状态集合，用于查找当前 Mod 版本的重试状态。</para>
    /// <para>en-us:Persisted self-healing states used to find retry state for the current Mod version.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:控制面板 Mod 视图。</para>
    /// <para>en-us:The control-panel Mod view.</para>
    /// </returns>
    private static DynamicAppControlPanelModView BuildControlPanelMod(
        DynamicModVersionKey key,
        IReadOnlyCollection<DynamicAppManifestRecord> appRecords,
        IReadOnlyCollection<DynamicAppRouteTarget> appTargets,
        IReadOnlyCollection<DynamicAppSelfHealingState> selfHealingStates)
    {
        var deploymentTargets = appTargets
            .Where(item => item.Key == key)
            .OrderByDescending(RuntimeTargetPriority)
            .ThenByDescending(item => item.UpdatedAtUtc)
            .ThenBy(item => item.DeploymentId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var target = deploymentTargets.FirstOrDefault();
        var record = appRecords.FirstOrDefault(item => item.Manifest.Mods.Any(mod => mod.Id == key.ModId && mod.Version == key.Version));
        var mod = record?.Manifest.Mods.FirstOrDefault(item => item.Id == key.ModId && item.Version == key.Version);
        var deploymentId = target == null || string.IsNullOrWhiteSpace(target.DeploymentId) ? "default" : target.DeploymentId.Trim();
        var selfHealing = FindSelfHealingState(selfHealingStates, key, deploymentId);
        return new DynamicAppControlPanelModView
        {
            DynamicAppId = key.DynamicAppId,
            ModId = key.ModId,
            Version = key.Version,
            DeploymentId = deploymentId,
            ExecutionMode = mod?.ExecutionMode.ToString() ?? string.Empty,
            HealthPath = target?.HealthPath ?? mod?.Health.Path ?? string.Empty,
            BaseAddress = target?.BaseAddress.ToString() ?? mod?.Artifact.Endpoint ?? string.Empty,
            Arguments = mod?.Artifact.Arguments ?? string.Empty,
            EntryFile = target?.DeploymentEntryFilePath ?? mod?.Artifact.File ?? string.Empty,
            StartupTimeoutSeconds = target?.HealthProbeTimeoutSeconds ?? mod?.Health.StartupTimeoutSeconds ?? 60,
            State = target?.State,
            HealthStatus = target?.HealthStatus,
            InstanceId = target?.InstanceId ?? string.Empty,
            LastStatusMessage = target?.LastStatusMessage ?? string.Empty,
            UpdatedAtUtc = target?.UpdatedAtUtc,
            HasManifest = record != null,
            ManifestRecordId = record?.RecordId ?? string.Empty,
            ManifestSavedAtUtc = record?.SavedAtUtc,
            SelfHealing = BuildSelfHealingView(selfHealing),
            Deployments = deploymentTargets
                .Select(item =>
                {
                    var itemDeploymentId = string.IsNullOrWhiteSpace(item.DeploymentId) ? "default" : item.DeploymentId.Trim();
                    return new DynamicAppControlPanelDeploymentView
                    {
                        DeploymentId = itemDeploymentId,
                        BaseAddress = item.BaseAddress.ToString(),
                        HealthPath = item.HealthPath,
                        Arguments = mod?.Artifact.Arguments ?? string.Empty,
                        EntryFile = item.DeploymentEntryFilePath,
                        StartupTimeoutSeconds = item.HealthProbeTimeoutSeconds,
                        State = item.State,
                        HealthStatus = item.HealthStatus,
                        InstanceId = item.InstanceId,
                        LastStatusMessage = item.LastStatusMessage,
                        UpdatedAtUtc = item.UpdatedAtUtc,
                        SelfHealing = BuildSelfHealingView(FindSelfHealingState(selfHealingStates, key, itemDeploymentId))
                    };
                })
                .ToArray()
        };
    }

    private static int RuntimeTargetPriority(DynamicAppRouteTarget target)
    {
        if (target.State == DynamicModLifecycleState.Active && target.HealthStatus == DynamicModHealthStatus.Healthy)
        {
            return 4;
        }

        if (target.State == DynamicModLifecycleState.Active)
        {
            return 3;
        }

        if (target.State is DynamicModLifecycleState.Starting or DynamicModLifecycleState.Draining)
        {
            return 2;
        }

        return target.State == DynamicModLifecycleState.Pending ? 1 : 0;
    }

    private static DynamicAppSelfHealingState? FindSelfHealingState(
        IReadOnlyCollection<DynamicAppSelfHealingState> selfHealingStates,
        DynamicModVersionKey key,
        string deploymentId)
    {
        return selfHealingStates.FirstOrDefault(item =>
            item.Key == key
            && string.Equals(
                string.IsNullOrWhiteSpace(item.DeploymentId) ? "default" : item.DeploymentId.Trim(),
                deploymentId,
                StringComparison.OrdinalIgnoreCase));
    }

    private static DynamicAppSelfHealingView? BuildSelfHealingView(DynamicAppSelfHealingState? state)
    {
        return state == null
            ? null
            : new DynamicAppSelfHealingView
            {
                AttemptCount = state.AttemptCount,
                MaxAttempts = state.MaxAttempts,
                FirstAttemptUtc = state.FirstAttemptUtc,
                LastAttemptUtc = state.LastAttemptUtc,
                NextRetryAtUtc = state.NextRetryAtUtc,
                WindowResetAtUtc = state.WindowResetAtUtc,
                IsPaused = state.IsPaused,
                LastFailureStage = state.LastFailureStage,
                LastFailureMessage = state.LastFailureMessage
            };
    }

    /// <summary>
    /// <para>zh-cn:解析 target/{dynamicAppId}/{modId}/{version} 路由片段为目标版本键；片段数量不正确时抛出参数异常。</para>
    /// <para>en-us:Parses a target/{dynamicAppId}/{modId}/{version} route segment into a target version key; throws an argument exception when the segment count is invalid.</para>
    /// </summary>
    /// <param name="targetRoute">
    /// <para>zh-cn:目标路由片段。</para>
    /// <para>en-us:The target route segment.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:目标 Mod 版本键。</para>
    /// <para>en-us:The target Mod version key.</para>
    /// </returns>
    private static DynamicAppRuntimeKey ParseRuntimeKeyRoute(string targetRoute)
    {
        var segments = targetRoute.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length is not (3 or 4))
        {
            throw new ArgumentException("Target route must use `target/{dynamicAppId}/{modId}/{version}` or `target/{dynamicAppId}/{modId}/{version}/{deploymentId}` format.", nameof(targetRoute));
        }

        var key = new DynamicModVersionKey(
            new DynamicAppId(Uri.UnescapeDataString(segments[0])),
            new DynamicModId(Uri.UnescapeDataString(segments[1])),
            new DynamicAppVersion(Uri.UnescapeDataString(segments[2])));
        return new DynamicAppRuntimeKey(
            key,
            segments.Length == 4 && !string.IsNullOrWhiteSpace(segments[3])
                ? Uri.UnescapeDataString(segments[3])
                : "default");
    }

    private bool TryMatch(PathString path, out string route)
    {
        route = string.Empty;
        var prefix = NormalizePrefix(_options.RoutePrefix);
        var value = path.Value ?? string.Empty;
        if (!value.Equals(prefix, StringComparison.OrdinalIgnoreCase)
            && !value.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var rawRoute = value.Length == prefix.Length ? string.Empty : value[prefix.Length..].Trim('/');
        route = rawRoute.StartsWith("target/", StringComparison.OrdinalIgnoreCase)
            ? rawRoute
            : rawRoute.ToLowerInvariant();
        return true;
    }

    private bool IsAuthorized(HttpContext context)
    {
        if (!_options.RequireTicket)
        {
            return true;
        }

        if (_options.Tickets.Length == 0 || string.IsNullOrWhiteSpace(_options.HeaderName))
        {
            return false;
        }

        var requestTokens = context.Request.Headers[_options.HeaderName].Where(token => !string.IsNullOrWhiteSpace(token)).ToArray();
        return requestTokens.Any(requestToken => _options.Tickets.Any(allowedTicket => FixedTimeEquals(requestToken!, allowedTicket)));
    }

    private static async Task<T> ReadRequiredJsonAsync<T>(HttpContext context)
    {
        var value = await JsonSerializer.DeserializeAsync<T>(context.Request.Body, JsonOptions, context.RequestAborted).ConfigureAwait(false);
        return value ?? throw new JsonException($"Request body cannot be deserialized as `{typeof(T).Name}`.");
    }

    private static string ReadRequiredFormString(IFormCollection form, string key)
    {
        var value = ReadFormString(form, key, string.Empty);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"Form field `{key}` is required.", key);
        }

        return value;
    }

    private static string ReadFormString(IFormCollection form, string key, string fallback)
    {
        return form.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value.FirstOrDefault())
            ? value.FirstOrDefault()!.Trim()
            : fallback;
    }

    private static string? ReadFormNullableString(IFormCollection form, string key)
    {
        var value = ReadFormString(form, key, string.Empty);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static int ReadFormInt(IFormCollection form, string key, int fallback)
    {
        var value = ReadFormString(form, key, string.Empty);
        return int.TryParse(value, out var parsed) ? parsed : fallback;
    }

    private static bool ReadFormBool(IFormCollection form, string key, bool fallback)
    {
        var value = ReadFormString(form, key, string.Empty);
        return bool.TryParse(value, out var parsed) ? parsed : fallback;
    }

    private static TEnum ReadFormEnum<TEnum>(IFormCollection form, string key, TEnum fallback)
        where TEnum : struct
    {
        var value = ReadFormString(form, key, string.Empty);
        return Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed) ? parsed : fallback;
    }

    private static Task WriteOperationResultAsync<T>(HttpContext context, DynamicAppOperationResult result, T? data = default)
    {
        if (!result.Succeeded && context.Response.StatusCode == StatusCodes.Status200OK)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
        }

        return WriteJsonAsync(context, DynamicAppApiResponse<T>.FromOperation(result, data));
    }

    private static Task WriteJsonAsync(HttpContext context, object value)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        return JsonSerializer.SerializeAsync(context.Response.Body, value, JsonOptions, context.RequestAborted);
    }

    private static DynamicAppRuntimeKey ToRuntimeKey(DynamicAppRouteTarget target)
        => new(
            target.Key,
            string.IsNullOrWhiteSpace(target.DeploymentId)
                ? "default"
                : target.DeploymentId.Trim());

    private static void ApplyRuntimeTarget(DynamicModManifest mod, DynamicAppRouteTarget? target)
    {
        if (target == null)
        {
            return;
        }

        mod.Artifact.DeploymentId = ToRuntimeKey(target).DeploymentId;
        mod.Artifact.DeploymentDirectory = target.DeploymentDirectory;
        if (!string.IsNullOrWhiteSpace(target.DeploymentEntryFilePath))
        {
            mod.Artifact.File = target.DeploymentEntryFilePath;
        }

        mod.Artifact.Endpoint = target.BaseAddress.ToString();
        mod.Artifact.Arguments = AppendUrlsArgument(mod.Artifact.Arguments, target.BaseAddress);
    }

    private static DynamicAppManifest CloneManifest(DynamicAppManifest source, DynamicModManifest mod)
    {
        return new DynamicAppManifest
        {
            Id = source.Id,
            Version = source.Version,
            Runtime = new DynamicAppRuntimeManifest
            {
                Profile = source.Runtime.Profile
            },
            Endpoints = new DynamicAppEndpointManifest
            {
                GatewayPort = source.Endpoints.GatewayPort,
                ManagerPort = source.Endpoints.ManagerPort,
                ManagerInternalOnly = source.Endpoints.ManagerInternalOnly
            },
            Mods = new List<DynamicModManifest> { mod }
        };
    }

    private static DynamicModManifest CloneMod(DynamicModManifest source)
    {
        return new DynamicModManifest
        {
            Id = source.Id,
            Version = source.Version,
            ExecutionMode = source.ExecutionMode,
            Artifact = new DynamicModArtifactManifest
            {
                File = source.Artifact.File,
                Image = source.Artifact.Image,
                ContainerName = source.Artifact.ContainerName,
                Arguments = source.Artifact.Arguments,
                Endpoint = source.Artifact.Endpoint,
                DeploymentId = source.Artifact.DeploymentId,
                DeploymentDirectory = source.Artifact.DeploymentDirectory
            },
            Health = new DynamicModHealthManifest
            {
                Path = source.Health.Path,
                StartupTimeoutSeconds = source.Health.StartupTimeoutSeconds
            },
        };
    }

    private static Uri ResolveDeploymentEndpoint(string? configuredEndpoint, int port)
    {
        var value = string.IsNullOrWhiteSpace(configuredEndpoint)
            ? $"http://127.0.0.1:{port}"
            : configuredEndpoint.Trim();
        if (!Uri.TryCreate(value, UriKind.Absolute, out var endpoint)
            || endpoint.Scheme is not ("http" or "https")
            || endpoint.Port <= 0)
        {
            throw new ArgumentException("Deployment endpoint must be an absolute http or https URL with a valid port.", nameof(configuredEndpoint));
        }

        return endpoint;
    }

    private static string AppendUrlsArgument(string? arguments, Uri endpoint)
    {
        var suffix = $"--urls {endpoint}";
        if (string.IsNullOrWhiteSpace(arguments))
        {
            return suffix;
        }

        var tokens = arguments
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
        for (var index = tokens.Count - 1; index >= 0; index--)
        {
            if (tokens[index].StartsWith("--urls=", StringComparison.OrdinalIgnoreCase))
            {
                tokens.RemoveAt(index);
                continue;
            }

            if (tokens[index].Equals("--urls", StringComparison.OrdinalIgnoreCase))
            {
                tokens.RemoveAt(index);
                if (index < tokens.Count)
                {
                    tokens.RemoveAt(index);
                }
            }
        }

        tokens.Add("--urls");
        tokens.Add(endpoint.ToString());
        return string.Join(' ', tokens);
    }

    private static string NormalizeDeploymentId(string? deploymentId)
    {
        if (string.IsNullOrWhiteSpace(deploymentId))
        {
            throw new ArgumentException("DeploymentId is required.", nameof(deploymentId));
        }

        var value = deploymentId.Trim();
        if (value is "." or ".."
            || value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
            || value.Contains('/') || value.Contains('\\'))
        {
            throw new ArgumentException("DeploymentId must be one safe path segment.", nameof(deploymentId));
        }

        return value;
    }

    private static Uri ResolveStartupBaseAddress(DynamicModManifest mod)
    {
        if (Uri.TryCreate(mod.Artifact.Endpoint, UriKind.Absolute, out var configured))
        {
            return configured;
        }

        var parsed = TryResolveBaseAddressFromArguments(mod.Artifact.Arguments);
        return parsed ?? new Uri(mod.ExecutionMode == DynamicModExecutionMode.Process ? "http://127.0.0.1" : "http://localhost");
    }

    private static Uri? TryResolveBaseAddressFromArguments(string? arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments))
        {
            return null;
        }

        var tokens = arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (var index = 0; index < tokens.Length; index++)
        {
            var token = tokens[index];
            var value = token.StartsWith("--urls=", StringComparison.OrdinalIgnoreCase)
                ? token["--urls=".Length..]
                : token.Equals("--urls", StringComparison.OrdinalIgnoreCase) && index + 1 < tokens.Length
                    ? tokens[index + 1]
                    : null;

            var uri = TryCreatePreferredBaseAddress(value);
            if (uri != null)
            {
                return uri;
            }
        }

        return null;
    }

    private static Uri? TryCreatePreferredBaseAddress(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        foreach (var candidate in value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri))
            {
                continue;
            }

            if (uri.Host is "*" or "+" or "0.0.0.0")
            {
                var builder = new UriBuilder(uri)
                {
                    Host = "127.0.0.1"
                };
                return builder.Uri;
            }

            return uri;
        }

        return null;
    }

    private static string NormalizePrefix(string prefix)
    {
        return string.IsNullOrWhiteSpace(prefix) ? "/dynamicapp-manager" : "/" + prefix.Trim('/');
    }

    private static string NormalizeInternalRoutePrefix(string routePrefix)
    {
        if (string.IsNullOrWhiteSpace(routePrefix))
        {
            throw new ArgumentException("Route prefix cannot be empty.", nameof(routePrefix));
        }

        var normalized = routePrefix.Trim();
        if (!normalized.StartsWith("/", StringComparison.Ordinal)
            || normalized.Contains('?', StringComparison.Ordinal)
            || normalized.Contains('#', StringComparison.Ordinal)
            || normalized.Contains('\\', StringComparison.Ordinal))
        {
            throw new ArgumentException("Route prefix must be an absolute path without query, fragment, or backslash.", nameof(routePrefix));
        }

        normalized = "/" + normalized.Trim('/');
        return normalized == "/" ? normalized : normalized.TrimEnd('/');
    }

    private static bool FixedTimeEquals(string value, string expected)
    {
        var valueBytes = Encoding.UTF8.GetBytes(value);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        return valueBytes.Length == expectedBytes.Length && CryptographicOperations.FixedTimeEquals(valueBytes, expectedBytes);
    }
}
