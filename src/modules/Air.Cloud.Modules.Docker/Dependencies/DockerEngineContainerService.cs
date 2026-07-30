using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;

using Air.Cloud.Core.Standard.Container;
using Air.Cloud.Core.Standard.Container.Model;
using Air.Cloud.Modules.Docker.Options;

namespace Air.Cloud.Modules.Docker.Dependencies;

/// <summary>
/// <para>zh-cn:基于 Docker Engine HTTP API 的容器访问服务。该实现通过 Unix Socket 访问 Docker，并把容器列表、检查、启动和停止能力提供为 Core 容器引擎标准。</para>
/// <para>en-us:Docker Engine HTTP API based container access service. This implementation accesses Docker through Unix sockets and exposes list, inspect, start, and stop capabilities as the Core container engine standard.</para>
/// </summary>
public sealed class DockerEngineContainerService : IContainerEngineStandard, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient httpClient;
    private readonly DockerEngineContainerServiceOptions options;
    private bool disposed;

    /// <summary>
    /// <para>zh-cn:创建 Docker Engine 容器访问服务。构造阶段只创建 HTTP 客户端，不主动访问 Docker Socket。</para>
    /// <para>en-us:Creates the Docker Engine container access service. Construction only creates the HTTP client and does not proactively access the Docker socket.</para>
    /// </summary>
    /// <param name="options">
    /// <para>zh-cn:Docker Engine 访问选项。</para>
    /// <para>en-us:The Docker Engine access options.</para>
    /// </param>
    public DockerEngineContainerService(DockerEngineContainerServiceOptions options)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        httpClient = CreateDockerHttpClient(this.options.DockerSocketPath);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<ContainerEngineInstance>> ListAsync(bool includeStopped = true, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ValidateOptions();

        using var timeout = CreateTimeoutSource(options.RequestTimeout, cancellationToken);
        using var response = await httpClient.GetAsync($"/containers/json?all={includeStopped.ToString().ToLowerInvariant()}", timeout.Token).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(timeout.Token).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Docker API returned `{(int)response.StatusCode}`. {body}".Trim());
        }

        var containers = string.IsNullOrWhiteSpace(body)
            ? Array.Empty<DockerContainerJson>()
            : JsonSerializer.Deserialize<DockerContainerJson[]>(body, JsonOptions) ?? Array.Empty<DockerContainerJson>();

        return containers
            .SelectMany(ToViews)
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <inheritdoc />
    public async Task<ContainerEngineInstance> InspectAsync(string containerNameOrId, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ValidateOptions();
        ValidateContainerName(containerNameOrId);

        var inspect = await GetDockerJsonAsync<DockerContainerInspectJson>($"/containers/{Uri.EscapeDataString(containerNameOrId)}/json", cancellationToken).ConfigureAwait(false);
        return ToView(inspect, containerNameOrId);
    }

    /// <inheritdoc />
    public Task<ContainerEngineOperationResult> StartAsync(string containerNameOrId, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ValidateContainerName(containerNameOrId);
        return SendDockerRequestAsync(HttpMethod.Post, $"/containers/{Uri.EscapeDataString(containerNameOrId)}/start", NormalizeTimeout(options.StartTimeout, TimeSpan.FromSeconds(60)), cancellationToken);
    }

    /// <inheritdoc />
    public Task<ContainerEngineOperationResult> StopAsync(string containerNameOrId, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ValidateContainerName(containerNameOrId);
        var grace = Math.Max(0, options.StopGracePeriodSeconds);
        return SendDockerRequestAsync(HttpMethod.Post, $"/containers/{Uri.EscapeDataString(containerNameOrId)}/stop?t={grace}", NormalizeTimeout(options.StopTimeout, TimeSpan.FromSeconds(60)), cancellationToken);
    }

    /// <summary>
    /// <para>zh-cn:释放 Docker HTTP 客户端。释放后再次调用查询或生命周期方法会抛出对象已释放异常。</para>
    /// <para>en-us:Disposes the Docker HTTP client. Query or lifecycle calls after disposal throw an object-disposed exception.</para>
    /// </summary>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        httpClient.Dispose();
        disposed = true;
    }

    private async Task<ContainerEngineOperationResult> SendDockerRequestAsync(HttpMethod method, string path, TimeSpan timeout, CancellationToken cancellationToken)
    {
        try
        {
            ValidateOptions();
            using var timeoutSource = CreateTimeoutSource(timeout, cancellationToken);
            using var request = new HttpRequestMessage(method, path);
            using var response = await httpClient.SendAsync(request, timeoutSource.Token).ConfigureAwait(false);
            if (response.StatusCode is HttpStatusCode.NoContent or HttpStatusCode.NotModified)
            {
                return ContainerEngineOperationResult.Success($"Docker API returned `{(int)response.StatusCode}`.");
            }

            var body = await response.Content.ReadAsStringAsync(timeoutSource.Token).ConfigureAwait(false);
            return ContainerEngineOperationResult.Failure($"Docker API returned `{(int)response.StatusCode}`. {body}".Trim());
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ContainerEngineOperationResult.Failure("Docker API request timed out.");
        }
        catch (Exception exception) when (exception is HttpRequestException or SocketException or InvalidOperationException)
        {
            return ContainerEngineOperationResult.Failure(exception.Message);
        }
    }

    private async Task<T> GetDockerJsonAsync<T>(string path, CancellationToken cancellationToken)
    {
        using var timeout = CreateTimeoutSource(options.RequestTimeout, cancellationToken);
        using var response = await httpClient.GetAsync(path, timeout.Token).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(timeout.Token).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Docker API returned `{(int)response.StatusCode}`. {body}".Trim());
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            throw new InvalidOperationException("Docker API returned an empty response body.");
        }

        return JsonSerializer.Deserialize<T>(body, JsonOptions)
            ?? throw new InvalidOperationException("Docker API returned an invalid JSON response body.");
    }

    private void ValidateOptions()
    {
        if (!options.Enabled)
        {
            throw new InvalidOperationException("Docker Engine container service is not enabled.");
        }

        if (string.IsNullOrWhiteSpace(options.DockerSocketPath))
        {
            throw new InvalidOperationException("Docker socket path is not configured.");
        }

        if (!File.Exists(options.DockerSocketPath))
        {
            throw new InvalidOperationException($"Docker socket `{options.DockerSocketPath}` does not exist.");
        }
    }

    private static void ValidateContainerName(string containerNameOrId)
    {
        if (string.IsNullOrWhiteSpace(containerNameOrId))
        {
            throw new ArgumentException("Docker container name or id is required.", nameof(containerNameOrId));
        }
    }

    private static TimeSpan NormalizeTimeout(TimeSpan timeout, TimeSpan fallback) => timeout <= TimeSpan.Zero ? fallback : timeout;

    private static CancellationTokenSource CreateTimeoutSource(TimeSpan timeout, CancellationToken cancellationToken)
    {
        var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        source.CancelAfter(NormalizeTimeout(timeout, TimeSpan.FromSeconds(30)));
        return source;
    }

    private static IEnumerable<ContainerEngineInstance> ToViews(DockerContainerJson container)
    {
        var names = container.Names == null || container.Names.Length == 0
            ? new[] { container.Id ?? string.Empty }
            : container.Names;
        foreach (var name in names.Select(item => item.TrimStart('/')).Where(item => !string.IsNullOrWhiteSpace(item)))
        {
            yield return new ContainerEngineInstance
            {
                Id = ShortId(container.Id),
                Name = name,
                Image = container.Image ?? string.Empty,
                Runtime = "Docker",
                State = container.State ?? string.Empty,
                Status = container.Status ?? string.Empty,
                IsRunning = string.Equals(container.State, "running", StringComparison.OrdinalIgnoreCase),
                IsAvailable = string.Equals(container.State, "running", StringComparison.OrdinalIgnoreCase)
            };
        }
    }

    private static ContainerEngineInstance ToView(DockerContainerInspectJson container, string fallbackName)
    {
        var state = container.State?.Status ?? string.Empty;
        var dockerHealth = container.State?.Health?.Status ?? string.Empty;
        var isRunning = container.State?.Running == true;
        var isDockerUnhealthy = string.Equals(dockerHealth, "unhealthy", StringComparison.OrdinalIgnoreCase);
        return new ContainerEngineInstance
        {
            Id = ShortId(container.Id),
            Name = string.IsNullOrWhiteSpace(container.Name) ? fallbackName : container.Name.TrimStart('/'),
            Image = container.Config?.Image ?? string.Empty,
            Runtime = "Docker",
            State = state,
            Status = BuildInspectStatus(state, dockerHealth, isRunning),
            HealthStatus = dockerHealth,
            IsRunning = isRunning,
            IsAvailable = isRunning && !isDockerUnhealthy
        };
    }

    private static string BuildInspectStatus(string state, string dockerHealth, bool isRunning)
    {
        var status = string.IsNullOrWhiteSpace(state)
            ? isRunning ? "running" : "unknown"
            : state;
        return string.IsNullOrWhiteSpace(dockerHealth)
            ? status
            : $"{status} / docker-health:{dockerHealth}";
    }

    private static string ShortId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Length <= 12 ? value : value[..12];
    }

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

    private sealed class DockerContainerJson
    {
        [JsonPropertyName("Id")]
        public string Id { get; set; }

        [JsonPropertyName("Names")]
        public string[] Names { get; set; }

        [JsonPropertyName("Image")]
        public string Image { get; set; }

        [JsonPropertyName("State")]
        public string State { get; set; }

        [JsonPropertyName("Status")]
        public string Status { get; set; }
    }

    private sealed class DockerContainerInspectJson
    {
        [JsonPropertyName("Id")]
        public string Id { get; set; }

        [JsonPropertyName("Name")]
        public string Name { get; set; }

        [JsonPropertyName("Config")]
        public DockerContainerConfigJson Config { get; set; }

        [JsonPropertyName("State")]
        public DockerContainerStateJson State { get; set; }
    }

    private sealed class DockerContainerConfigJson
    {
        [JsonPropertyName("Image")]
        public string Image { get; set; }
    }

    private sealed class DockerContainerStateJson
    {
        [JsonPropertyName("Status")]
        public string Status { get; set; }

        [JsonPropertyName("Running")]
        public bool Running { get; set; }

        [JsonPropertyName("Health")]
        public DockerContainerHealthJson Health { get; set; }
    }

    private sealed class DockerContainerHealthJson
    {
        [JsonPropertyName("Status")]
        public string Status { get; set; }
    }
}
