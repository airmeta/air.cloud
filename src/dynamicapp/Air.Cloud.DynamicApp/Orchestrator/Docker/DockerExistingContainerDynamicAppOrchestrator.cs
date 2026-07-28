using System.Net;
using System.Net.Sockets;

using Air.Cloud.DynamicApp.Contracts.Lifecycle;
using Air.Cloud.DynamicApp.Contracts.Manifest;
using Air.Cloud.DynamicApp.Contracts.Runtime;

namespace Air.Cloud.DynamicApp.Orchestrator;

/// <summary>
/// <para>zh-cn:已有 Docker 容器编排器，通过 Docker Engine Unix Socket 启动和停止已经存在的容器。</para>
/// <para>en-us:Existing Docker container orchestrator that starts and stops already-created containers through the Docker Engine Unix socket.</para>
/// </summary>
public sealed class DockerExistingContainerDynamicAppOrchestrator : IDynamicAppOrchestrator, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly DockerExistingContainerDynamicAppOrchestratorOptions _options;
    private readonly IDynamicAppStateStore _stateStore;
    private bool _disposed;

    /// <summary>
    /// <para>zh-cn:创建已有 Docker 容器编排器；构造阶段只准备客户端，不会访问 Docker Socket。</para>
    /// <para>en-us:Creates the existing Docker container orchestrator; construction only prepares the client and does not access the Docker socket.</para>
    /// </summary>
    /// <param name="stateStore">
    /// <para>zh-cn:运行状态存储，用于在容器启动成功后登记目标和在停止成功后更新状态。</para>
    /// <para>en-us:Runtime state store used to register targets after container start and update state after container stop.</para>
    /// </param>
    /// <param name="options">
    /// <para>zh-cn:Docker 编排选项，包含 Socket 路径、容器名称模板和目标地址模板。</para>
    /// <para>en-us:Docker orchestration options, including socket path, container name template, and target address template.</para>
    /// </param>
    public DockerExistingContainerDynamicAppOrchestrator(
        IDynamicAppStateStore stateStore,
        DockerExistingContainerDynamicAppOrchestratorOptions options)
    {
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _httpClient = CreateDockerHttpClient(_options.DockerSocketPath);
    }

    /// <summary>
    /// <para>zh-cn:启动 Manifest 中声明的已有容器，并把该容器登记为 DynamicApp 路由目标；不会创建新容器或拉取镜像。</para>
    /// <para>en-us:Starts the existing container declared by an ExistingContainer-mode manifest and registers it as a DynamicApp route target; it does not create containers or pull images.</para>
    /// </summary>
    /// <param name="manifest">
    /// <para>zh-cn:所属 DynamicApp Manifest。</para>
    /// <para>en-us:The owning DynamicApp manifest.</para>
    /// </param>
    /// <param name="mod">
    /// <para>zh-cn:容器模式 Mod Manifest。</para>
    /// <para>en-us:The ExistingContainer-mode Mod manifest.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌。</para>
    /// <para>en-us:Cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:启动和登记结果。</para>
    /// <para>en-us:The start and registration result.</para>
    /// </returns>
    public async Task<DynamicAppOperationResult> DeployAsync(DynamicAppManifest manifest, DynamicModManifest mod, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(mod);

        var validation = ValidateOptions();
        if (!validation.Succeeded)
        {
            return validation;
        }

        if (mod.ExecutionMode != DynamicModExecutionMode.ExistingContainer)
        {
            return DynamicAppOperationResult.Failure($"Mod `{manifest.Id}/{mod.Id}` is not configured for ExistingContainer mode.");
        }

        var key = new DynamicModVersionKey(manifest.Id, mod.Id, mod.Version);
        var containerName = ResolveContainerName(manifest, mod);
        var start = await SendDockerRequestAsync(HttpMethod.Post, $"/containers/{Uri.EscapeDataString(containerName)}/start", NormalizeTimeout(_options.StartTimeout), cancellationToken).ConfigureAwait(false);
        if (!start.Succeeded)
        {
            return DynamicAppOperationResult.Failure($"Docker container `{containerName}` could not be started for `{key}`. {start.Message}");
        }

        _stateStore.UpsertTarget(new DynamicAppRouteTarget
        {
            Key = key,
            BaseAddress = ResolveTargetBaseAddress(manifest, mod, containerName),
            RoutePrefix = mod.Route.Prefix,
            HealthPath = mod.Health.Path,
            HealthProbeTimeoutSeconds = mod.Health.StartupTimeoutSeconds,
            State = DynamicModLifecycleState.Starting,
            HealthStatus = DynamicModHealthStatus.Starting,
            InstanceId = containerName,
            LastStatusMessage = $"Existing Docker container `{containerName}` started for `{key}`."
        });

        return DynamicAppOperationResult.Success($"Existing Docker container `{containerName}` started for `{key}`.");
    }

    /// <summary>
    /// <para>zh-cn:停止目标键对应的已有 Docker 容器，并把状态更新为 Stopped；容器已停止时视为成功。</para>
    /// <para>en-us:Stops the existing Docker container for the target key and updates state to Stopped; an already stopped container is treated as success.</para>
    /// </summary>
    /// <param name="key">
    /// <para>zh-cn:要停止的目标版本键。</para>
    /// <para>en-us:The target version key to stop.</para>
    /// </param>
    /// <param name="cancellationToken">
    /// <para>zh-cn:取消令牌。</para>
    /// <para>en-us:Cancellation token.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:停止结果。</para>
    /// <para>en-us:The stop result.</para>
    /// </returns>
    public async Task<DynamicAppOperationResult> StopAsync(DynamicModVersionKey key, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var validation = ValidateOptions();
        if (!validation.Succeeded)
        {
            return validation;
        }

        var containerName = ResolveContainerName(key);
        var grace = Math.Max(0, _options.StopGracePeriodSeconds);
        var stop = await SendDockerRequestAsync(HttpMethod.Post, $"/containers/{Uri.EscapeDataString(containerName)}/stop?t={grace}", NormalizeTimeout(_options.StopTimeout), cancellationToken).ConfigureAwait(false);
        if (!stop.Succeeded)
        {
            return DynamicAppOperationResult.Failure($"Docker container `{containerName}` could not be stopped for `{key}`. {stop.Message}");
        }

        _stateStore.UpdateState(key, DynamicModLifecycleState.Stopped, DynamicModHealthStatus.Stopped, $"Existing Docker container `{containerName}` stopped for `{key}`.");
        return DynamicAppOperationResult.Success($"Existing Docker container `{containerName}` stopped for `{key}`.");
    }

    /// <summary>
    /// <para>zh-cn:释放 Docker HTTP 客户端。</para>
    /// <para>en-us:Disposes the Docker HTTP client.</para>
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _httpClient.Dispose();
        _disposed = true;
    }

    private DynamicAppOperationResult ValidateOptions()
    {
        if (!_options.Enabled)
        {
            return DynamicAppOperationResult.Failure("Docker existing-container DynamicApp orchestrator is not enabled.");
        }

        if (string.IsNullOrWhiteSpace(_options.DockerSocketPath))
        {
            return DynamicAppOperationResult.Failure("Docker socket path is not configured.");
        }

        if (!File.Exists(_options.DockerSocketPath))
        {
            return DynamicAppOperationResult.Failure($"Docker socket `{_options.DockerSocketPath}` does not exist.");
        }

        if (string.IsNullOrWhiteSpace(_options.ContainerNameTemplate))
        {
            return DynamicAppOperationResult.Failure("Docker container name template is not configured.");
        }

        return DynamicAppOperationResult.Success("Docker existing-container orchestrator options are valid.");
    }

    private async Task<DynamicAppOperationResult> SendDockerRequestAsync(HttpMethod method, string path, TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var timeoutSource = new CancellationTokenSource(timeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(timeoutSource.Token, cancellationToken);
        using var request = new HttpRequestMessage(method, path);
        try
        {
            using var response = await _httpClient.SendAsync(request, linked.Token).ConfigureAwait(false);
            if (response.StatusCode is HttpStatusCode.NoContent or HttpStatusCode.NotModified)
            {
                return DynamicAppOperationResult.Success($"Docker API returned `{(int)response.StatusCode}`.");
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return DynamicAppOperationResult.Failure($"Docker API returned `{(int)response.StatusCode}`. {body}".Trim());
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return DynamicAppOperationResult.Failure("Docker API request timed out.");
        }
        catch (HttpRequestException exception)
        {
            return DynamicAppOperationResult.Failure(exception.Message);
        }
        catch (SocketException exception)
        {
            return DynamicAppOperationResult.Failure(exception.Message);
        }
    }

    private Uri ResolveTargetBaseAddress(DynamicAppManifest manifest, DynamicModManifest mod, string containerName)
    {
        var value = string.IsNullOrWhiteSpace(_options.TargetBaseAddressTemplate)
            ? mod.Artifact.Endpoint
            : ApplyTemplate(_options.TargetBaseAddressTemplate, manifest.Id.Value, mod.Id.Value, mod.Version.Value, containerName);

        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            ? uri
            : new Uri("http://localhost");
    }

    private string ResolveContainerName(DynamicAppManifest manifest, DynamicModManifest mod)
    {
        return ApplyTemplate(_options.ContainerNameTemplate, manifest.Id.Value, mod.Id.Value, mod.Version.Value, string.Empty);
    }

    private string ResolveContainerName(DynamicModVersionKey key)
    {
        return ApplyTemplate(_options.ContainerNameTemplate, key.DynamicAppId.Value, key.ModId.Value, key.Version.Value, string.Empty);
    }

    private static string ApplyTemplate(string template, string dynamicAppId, string modId, string version, string containerName)
    {
        return template
            .Replace("{dynamicAppId}", dynamicAppId, StringComparison.OrdinalIgnoreCase)
            .Replace("{modId}", modId, StringComparison.OrdinalIgnoreCase)
            .Replace("{version}", version, StringComparison.OrdinalIgnoreCase)
            .Replace("{containerName}", containerName, StringComparison.OrdinalIgnoreCase);
    }

    private static TimeSpan NormalizeTimeout(TimeSpan timeout) => timeout <= TimeSpan.Zero ? TimeSpan.FromSeconds(60) : timeout;

    private static HttpClient CreateDockerHttpClient(string socketPath)
    {
        var handler = new SocketsHttpHandler
        {
            ConnectCallback = async (_, cancellationToken) =>
            {
                var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                try
                {
                    await socket.ConnectAsync(new UnixDomainSocketEndPoint(socketPath), cancellationToken).ConfigureAwait(false);
                    return new NetworkStream(socket, ownsSocket: true);
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }
            }
        };

        return new HttpClient(handler)
        {
            BaseAddress = new Uri("http://docker")
        };
    }
}
