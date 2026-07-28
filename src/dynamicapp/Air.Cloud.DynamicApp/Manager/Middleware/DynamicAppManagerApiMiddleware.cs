using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using Air.Cloud.DynamicApp.Contracts.Identifiers;
using Air.Cloud.DynamicApp.Contracts.Lifecycle;
using Air.Cloud.DynamicApp.Contracts.Manifest;
using Air.Cloud.DynamicApp.Contracts.Runtime;

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

            case "packages/applications" when HttpMethods.IsGet(context.Request.Method):
                await ListPackageApplicationsAsync(context).ConfigureAwait(false);
                return;

            case "packages/applications" when HttpMethods.IsPost(context.Request.Method):
                await CreatePackageApplicationAsync(context).ConfigureAwait(false);
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

        var request = await BuildConfigureModRequestAsync(context, form).ConfigureAwait(false);
        if (request.Mod.ExecutionMode != DynamicModExecutionMode.Process)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await WriteJsonAsync(context, DynamicAppApiResponse<object>.Failure("Zip package upload currently supports Process-mode Mods only.", "dynamicapp.unsupported_upload_mode")).ConfigureAwait(false);
            return;
        }

        var key = new DynamicModVersionKey(new DynamicAppId(request.DynamicAppId), request.Mod.Id, request.Mod.Version);
        var entryFile = ReadFormString(form, "entryFile", request.Mod.Artifact.File ?? string.Empty);
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

        request.Mod.Artifact.File = package.EntryFilePath;
        var response = await ConfigureModAsync(context, request, package).ConfigureAwait(false);
        await WriteConfigureResultAsync(context, response).ConfigureAwait(false);
    }

    /// <summary>
    /// <para>zh-cn:执行单 Mod 配置的发布或部署动作，并返回控制面板统一结果。</para>
    /// <para>en-us:Executes the publish or deploy action for one Mod configuration and returns a unified control-panel result.</para>
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
        var operation = request.Deploy
            ? await DeployConfiguredManifestAsync(context, manifest).ConfigureAwait(false)
            : await context.RequestServices.GetRequiredService<IDynamicAppManager>()
                .PublishAsync(manifest, context.RequestAborted)
                .ConfigureAwait(false);

        return new DynamicAppConfigureModResult
        {
            Manifest = manifest,
            Package = package,
            Operation = operation
        };
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
                Arguments = FirstNotEmpty(mods.Select(mod => mod.Artifact.Arguments)),
                Endpoint = FirstNotEmpty(mods.Select(mod => mod.Artifact.Endpoint))
            },
            Health = new DynamicModHealthManifest
            {
                Path = FirstNotEmpty(mods.Select(mod => mod.Health.Path)) ?? "/Health",
                StartupTimeoutSeconds = startupTimeoutSeconds > 0 ? startupTimeoutSeconds : 60
            },
            Route = new DynamicModRouteManifest
            {
                Prefix = FirstNotEmpty(mods.Select(mod => mod.Route.Prefix)) ?? "/mods/" + modId
            }
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
                    Arguments = ReadFormNullableString(form, "arguments") ?? inherited?.Artifact.Arguments,
                    Endpoint = ReadFormNullableString(form, "endpoint") ?? inherited?.Artifact.Endpoint
                },
                Health = new DynamicModHealthManifest
                {
                    Path = ReadFormString(form, "healthPath", inherited?.Health.Path ?? "/Health"),
                    StartupTimeoutSeconds = ReadFormInt(form, "startupTimeoutSeconds", inherited?.Health.StartupTimeoutSeconds ?? 60)
                },
                Route = new DynamicModRouteManifest
                {
                    Prefix = ReadFormString(form, "routePrefix", inherited?.Route.Prefix ?? "/mods/" + modId.Value)
                }
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
        var result = await StartTargetAsync(context, request.ToKey()).ConfigureAwait(false);
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
        var key = request.ToKey();
        var stop = await StopTargetAsync(context, key).ConfigureAwait(false);
        if (!stop.Succeeded)
        {
            await WriteOperationResultAsync<object>(context, stop).ConfigureAwait(false);
            return;
        }

        var start = await StartTargetAsync(context, key).ConfigureAwait(false);
        await WriteOperationResultAsync<object>(context, start).ConfigureAwait(false);
    }

    private async Task ActivateAsync(HttpContext context)
    {
        var request = await ReadRequiredJsonAsync<DynamicAppTargetKeyRequest>(context).ConfigureAwait(false);
        var result = await context.RequestServices.GetRequiredService<IDynamicAppManager>()
            .ActivateAsync(request.ToKey(), context.RequestAborted)
            .ConfigureAwait(false);
        await WriteOperationResultAsync<object>(context, result).ConfigureAwait(false);
    }

    private async Task RollbackAsync(HttpContext context)
    {
        var request = await ReadRequiredJsonAsync<DynamicAppRollbackRequest>(context).ConfigureAwait(false);
        var result = await context.RequestServices.GetRequiredService<IDynamicAppManager>()
            .RollbackAsync(request.ToDynamicAppId(), request.ToModId(), context.RequestAborted)
            .ConfigureAwait(false);
        await WriteOperationResultAsync<object>(context, result).ConfigureAwait(false);
    }

    private async Task StopAsync(HttpContext context)
    {
        var request = await ReadRequiredJsonAsync<DynamicAppTargetKeyRequest>(context).ConfigureAwait(false);
        var key = request.ToKey();
        var result = await StopTargetAsync(context, key).ConfigureAwait(false);
        if (result.Succeeded)
        {
            context.RequestServices.GetRequiredService<IDynamicAppSelfHealingStateStore>().Remove(key);
        }

        await WriteOperationResultAsync<object>(context, result).ConfigureAwait(false);
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
        var key = request.ToKey();
        var stateStore = context.RequestServices.GetRequiredService<IDynamicAppStateStore>();
        var target = (await ListTargetsWithRuntimeSyncAsync(context).ConfigureAwait(false)).FirstOrDefault(item => item.Key == key);
        if (target == null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await WriteJsonAsync(context, DynamicAppApiResponse<object>.Failure($"Target `{key}` is not registered.", "dynamicapp.target_not_found")).ConfigureAwait(false);
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

        var manifestRecord = await context.RequestServices.GetRequiredService<IDynamicAppManifestStore>()
            .FindAsync(key, context.RequestAborted)
            .ConfigureAwait(false);
        var mod = manifestRecord?.Manifest.Mods.FirstOrDefault(item => item.Id == key.ModId && item.Version == key.Version);
        var health = mod?.Health ?? new DynamicModHealthManifest
        {
            Path = target.HealthPath,
            StartupTimeoutSeconds = target.HealthProbeTimeoutSeconds
        };
        var result = await healthProbe.ProbeAsync(target, health, context.RequestAborted).ConfigureAwait(false);
        var update = result.IsHealthy
            ? stateStore.UpdateState(key, target.State, DynamicModHealthStatus.Healthy, result.Message)
            : stateStore.UpdateState(key, target.State, DynamicModHealthStatus.Unhealthy, result.Message);
        await WriteOperationResultAsync(context, update, result).ConfigureAwait(false);
    }

    private async Task UpdateStateAsync(HttpContext context)
    {
        var request = await ReadRequiredJsonAsync<DynamicAppUpdateStateRequest>(context).ConfigureAwait(false);
        var result = context.RequestServices.GetRequiredService<IDynamicAppStateStore>()
            .UpdateState(request.ToKey(), request.State, request.HealthStatus, request.Message);
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
        var logs = await logSink.QueryAsync(request.ToKey(), cancellationToken: context.RequestAborted).ConfigureAwait(false);
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

                var view = TryBuildInstalledModVersion(record.Manifest.Id.Value, mod.Id.Value, mod.Version.Value, mod.Artifact.File);
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

    private static DynamicAppModPackageVersionView? TryBuildInstalledModVersion(string dynamicAppId, string modId, string version, string? artifactFile)
    {
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
                Version = version,
                PackageFilePath = string.Empty,
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
        var stopMessage = string.Empty;
        if (stateStore.ListTargets().Any(item => item.Key == key))
        {
            var stop = await StopTargetAsync(context, key).ConfigureAwait(false);
            stopMessage = stop.Message;
            stateStore.RemoveTarget(key);
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
        var key = request.ToKey();
        var removed = context.RequestServices.GetRequiredService<IDynamicAppTargetArchiveStore>().Unarchive(key);
        var result = DynamicAppOperationResult.Success(removed
            ? $"Target `{key}` unarchived."
            : $"Target `{key}` was not archived.");
        await WriteOperationResultAsync<object>(context, result).ConfigureAwait(false);
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

            var runtime = await runner.InspectAsync(target.Key, context.RequestAborted).ConfigureAwait(false);
            if (runtime.Succeeded)
            {
                continue;
            }

            stateStore.UpdateState(
                target.Key,
                DynamicModLifecycleState.Failed,
                DynamicModHealthStatus.Unhealthy,
                $"Process target `{target.Key}` runtime inspection failed. {runtime.Message}");
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
        var key = ParseTargetKeyRoute(targetRoute);
        var target = (await ListTargetsWithRuntimeSyncAsync(context).ConfigureAwait(false))
            .FirstOrDefault(item => item.Key == key);
        if (target == null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await WriteJsonAsync(context, DynamicAppApiResponse<object>.Failure($"Target `{key}` is not registered.", "dynamicapp.target_not_found")).ConfigureAwait(false);
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
    /// <param name="key">
    /// <para>zh-cn:目标 Mod 版本键。</para>
    /// <para>en-us:The target Mod version key.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:启动和激活结果。</para>
    /// <para>en-us:The start and activation result.</para>
    /// </returns>
    private async Task<DynamicAppOperationResult> StartTargetAsync(HttpContext context, DynamicModVersionKey key)
    {
        if (context.RequestServices.GetRequiredService<IDynamicAppTargetArchiveStore>().IsArchived(key))
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

        var mod = manifestRecord.Manifest.Mods.FirstOrDefault(item => item.Id == key.ModId && item.Version == key.Version);
        if (mod == null)
        {
            return DynamicAppOperationResult.Failure($"Manifest history record `{manifestRecord.RecordId}` does not contain target `{key}`.");
        }

        var start = await StartModAsync(context, manifestRecord.Manifest, mod).ConfigureAwait(false);
        if (!start.Succeeded)
        {
            var message = await AppendProcessDiagnosticsAsync(context, key, start.Message).ConfigureAwait(false);
            PublishFailedStartState(context, manifestRecord.Manifest, mod, message);
            return DynamicAppOperationResult.Failure(message);
        }

        var stateStore = context.RequestServices.GetRequiredService<IDynamicAppStateStore>();
        var target = stateStore.ListTargets().FirstOrDefault(item => item.Key == key);
        if (target == null)
        {
            stateStore.UpdateState(key, DynamicModLifecycleState.Failed, DynamicModHealthStatus.Unhealthy, $"Target `{key}` was not registered after startup.");
            return DynamicAppOperationResult.Failure($"Target `{key}` was not registered after startup.");
        }

        var runtime = await InspectProcessRuntimeIfNeededAsync(context, target, mod).ConfigureAwait(false);
        if (!runtime.Succeeded)
        {
            var message = await AppendProcessDiagnosticsAsync(context, key, $"Runtime target `{key}` startup inspection failed. {runtime.Message}").ConfigureAwait(false);
            stateStore.UpdateState(key, DynamicModLifecycleState.Failed, DynamicModHealthStatus.Unhealthy, message);
            return DynamicAppOperationResult.Failure(message);
        }

        var healthProbe = context.RequestServices.GetService<IDynamicAppHealthProbe>();
        if (healthProbe == null)
        {
            return DynamicAppOperationResult.Failure("IDynamicAppHealthProbe is not registered.");
        }

        var health = await healthProbe.ProbeAsync(target, mod.Health, context.RequestAborted).ConfigureAwait(false);
        if (!health.IsHealthy)
        {
            var message = await AppendProcessDiagnosticsAsync(context, key, $"Runtime target `{key}` health probe failed. {health.Message}").ConfigureAwait(false);
            stateStore.UpdateState(key, DynamicModLifecycleState.Failed, DynamicModHealthStatus.Unhealthy, message);
            return DynamicAppOperationResult.Failure(message);
        }

        var activation = await context.RequestServices.GetRequiredService<IDynamicAppManager>()
            .ActivateAsync(key, context.RequestAborted)
            .ConfigureAwait(false);
        if (!activation.Succeeded)
        {
            return activation;
        }

        context.RequestServices.GetRequiredService<IDynamicAppSelfHealingStateStore>().Remove(key);
        return DynamicAppOperationResult.Success($"Target `{key}` started, healthy, and active.");
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
            : await runner.InspectAsync(target.Key, context.RequestAborted).ConfigureAwait(false);
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
    private static void PublishFailedStartState(HttpContext context, DynamicAppManifest manifest, DynamicModManifest mod, string message)
    {
        var key = new DynamicModVersionKey(manifest.Id, mod.Id, mod.Version);
        var stateStore = context.RequestServices.GetRequiredService<IDynamicAppStateStore>();
        var statusMessage = string.IsNullOrWhiteSpace(message)
            ? $"Target `{key}` failed to start."
            : message;
        var update = stateStore.UpdateState(key, DynamicModLifecycleState.Failed, DynamicModHealthStatus.Unhealthy, statusMessage);
        if (update.Succeeded)
        {
            return;
        }

        stateStore.UpsertTarget(new DynamicAppRouteTarget
        {
            Key = key,
            BaseAddress = ResolveStartupBaseAddress(mod),
            RoutePrefix = mod.Route.Prefix,
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

    private static async Task<string> AppendProcessDiagnosticsAsync(HttpContext context, DynamicModVersionKey key, string message)
    {
        var logSink = context.RequestServices.GetService<IDynamicAppProcessLogSink>();
        if (logSink == null)
        {
            return message;
        }

        await Task.Delay(TimeSpan.FromMilliseconds(150), context.RequestAborted).ConfigureAwait(false);
        var logs = await logSink.QueryAsync(key, take: 80, context.RequestAborted).ConfigureAwait(false);
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
    /// <param name="key">
    /// <para>zh-cn:目标 Mod 版本键。</para>
    /// <para>en-us:The target Mod version key.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:停止结果。</para>
    /// <para>en-us:The stop result.</para>
    /// </returns>
    private async Task<DynamicAppOperationResult> StopTargetAsync(HttpContext context, DynamicModVersionKey key)
    {
        var manifestRecord = await context.RequestServices.GetRequiredService<IDynamicAppManifestStore>()
            .FindAsync(key, context.RequestAborted)
            .ConfigureAwait(false);
        var mod = manifestRecord?.Manifest.Mods.FirstOrDefault(item => item.Id == key.ModId && item.Version == key.Version);
        DynamicAppOperationResult result;
        if (mod?.ExecutionMode == DynamicModExecutionMode.LocalDevelopment)
        {
            return PublishStoppedStateIfNeeded(context, key, DynamicAppOperationResult.Success($"Local development target `{key}` detached from DynamicApp routing."));
        }

        if (mod?.ExecutionMode == DynamicModExecutionMode.Process)
        {
            var runner = context.RequestServices.GetService<IDynamicAppModRunner>();
            result = runner == null
                ? DynamicAppOperationResult.Failure("IDynamicAppModRunner is not registered.")
                : await runner.StopAsync(key, context.RequestAborted).ConfigureAwait(false);
            return PublishStoppedStateIfNeeded(context, key, result);
        }

        if (mod != null)
        {
            var orchestrator = context.RequestServices.GetService<IDynamicAppOrchestrator>();
            result = orchestrator == null
                ? DynamicAppOperationResult.Failure("IDynamicAppOrchestrator is not registered.")
                : await orchestrator.StopAsync(key, context.RequestAborted).ConfigureAwait(false);
            return PublishStoppedStateIfNeeded(context, key, result);
        }

        result = await StopTargetWithLegacyFallbackAsync(context, key).ConfigureAwait(false);
        return PublishStoppedStateIfNeeded(context, key, result);
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
    private static DynamicAppOperationResult PublishStoppedStateIfNeeded(HttpContext context, DynamicModVersionKey key, DynamicAppOperationResult result)
    {
        if (result.Succeeded)
        {
            context.RequestServices.GetRequiredService<IDynamicAppStateStore>()
                .UpdateState(key, DynamicModLifecycleState.Stopped, DynamicModHealthStatus.Stopped, result.Message);
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
    private async Task<DynamicAppOperationResult> StopTargetWithLegacyFallbackAsync(HttpContext context, DynamicModVersionKey key)
    {
        var runner = context.RequestServices.GetService<IDynamicAppModRunner>();
        var orchestrator = context.RequestServices.GetService<IDynamicAppOrchestrator>();
        var result = runner == null
            ? DynamicAppOperationResult.Failure("IDynamicAppModRunner is not registered.")
            : await runner.StopAsync(key, context.RequestAborted).ConfigureAwait(false);

        if (!result.Succeeded && orchestrator != null)
        {
            result = await orchestrator.StopAsync(key, context.RequestAborted).ConfigureAwait(false);
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
        var archivedKeys = archivedTargets.Select(item => item.Key.ToString()).ToHashSet(StringComparer.OrdinalIgnoreCase);
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
        var target = appTargets.FirstOrDefault(item => item.Key == key);
        var record = appRecords.FirstOrDefault(item => item.Manifest.Mods.Any(mod => mod.Id == key.ModId && mod.Version == key.Version));
        var mod = record?.Manifest.Mods.FirstOrDefault(item => item.Id == key.ModId && item.Version == key.Version);
        var selfHealing = selfHealingStates.FirstOrDefault(item => item.Key == key);
        return new DynamicAppControlPanelModView
        {
            DynamicAppId = key.DynamicAppId,
            ModId = key.ModId,
            Version = key.Version,
            ExecutionMode = mod?.ExecutionMode.ToString() ?? string.Empty,
            RoutePrefix = target?.RoutePrefix ?? mod?.Route.Prefix ?? string.Empty,
            HealthPath = target?.HealthPath ?? mod?.Health.Path ?? string.Empty,
            BaseAddress = target?.BaseAddress.ToString() ?? mod?.Artifact.Endpoint ?? string.Empty,
            State = target?.State,
            HealthStatus = target?.HealthStatus,
            InstanceId = target?.InstanceId ?? string.Empty,
            LastStatusMessage = target?.LastStatusMessage ?? string.Empty,
            UpdatedAtUtc = target?.UpdatedAtUtc,
            HasManifest = record != null,
            ManifestRecordId = record?.RecordId ?? string.Empty,
            ManifestSavedAtUtc = record?.SavedAtUtc,
            SelfHealing = selfHealing == null ? null : new DynamicAppSelfHealingView
            {
                AttemptCount = selfHealing.AttemptCount,
                MaxAttempts = selfHealing.MaxAttempts,
                FirstAttemptUtc = selfHealing.FirstAttemptUtc,
                LastAttemptUtc = selfHealing.LastAttemptUtc,
                NextRetryAtUtc = selfHealing.NextRetryAtUtc,
                WindowResetAtUtc = selfHealing.WindowResetAtUtc,
                IsPaused = selfHealing.IsPaused,
                LastFailureStage = selfHealing.LastFailureStage,
                LastFailureMessage = selfHealing.LastFailureMessage
            }
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
    private static DynamicModVersionKey ParseTargetKeyRoute(string targetRoute)
    {
        var segments = targetRoute.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length != 3)
        {
            throw new ArgumentException("Target route must use `target/{dynamicAppId}/{modId}/{version}` format.", nameof(targetRoute));
        }

        return new DynamicModVersionKey(
            new DynamicAppId(Uri.UnescapeDataString(segments[0])),
            new DynamicModId(Uri.UnescapeDataString(segments[1])),
            new DynamicAppVersion(Uri.UnescapeDataString(segments[2])));
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

        route = value.Length == prefix.Length ? string.Empty : value[prefix.Length..].Trim('/').ToLowerInvariant();
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

    private static bool FixedTimeEquals(string value, string expected)
    {
        var valueBytes = Encoding.UTF8.GetBytes(value);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        return valueBytes.Length == expectedBytes.Length && CryptographicOperations.FixedTimeEquals(valueBytes, expectedBytes);
    }
}
