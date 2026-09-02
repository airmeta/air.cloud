using Air.Cloud.Capabilities.ControlPlaneAuth.Models;
using Air.Cloud.Capabilities.ControlPlaneAuth.Options;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Air.Cloud.Capabilities.ControlPlaneAuth.Stores;

/// <summary>
/// <para>zh-cn:基于本地文件系统的控制面认证运行数据存储。写入采用临时文件加原子替换，降低进程中断造成半文件的风险。</para>
/// <para>en-us:File-system based runtime data store for control-plane authentication. Writes use temporary files plus atomic replacement to reduce partial-file risk when the process is interrupted.</para>
/// </summary>
public sealed class FileControlPlaneAuthStore : IControlPlaneAuthStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IHostEnvironment hostEnvironment;
    private readonly IOptionsMonitor<ControlPlaneAuthOptions> options;
    private readonly SemaphoreSlim writeLock = new(1, 1);

    /// <summary>
    /// <para>zh-cn:创建文件系统控制面认证存储。</para>
    /// <para>en-us:Creates the file-system control-plane authentication store.</para>
    /// </summary>
    /// <param name="hostEnvironment"><para>zh-cn:宿主环境，用于解析相对路径。</para><para>en-us:Host environment used to resolve relative paths.</para></param>
    /// <param name="options"><para>zh-cn:控制面认证配置监视器。</para><para>en-us:Control-plane authentication options monitor.</para></param>
    public FileControlPlaneAuthStore(IHostEnvironment hostEnvironment, IOptionsMonitor<ControlPlaneAuthOptions> options)
    {
        this.hostEnvironment = hostEnvironment;
        this.options = options;
    }

    /// <inheritdoc />
    public Task<ControlPlaneAuthState> ReadStateAsync(CancellationToken cancellationToken = default) =>
        ReadAsync("state.json", new ControlPlaneAuthState(), cancellationToken);

    /// <inheritdoc />
    public Task WriteStateAsync(ControlPlaneAuthState state, CancellationToken cancellationToken = default) =>
        WriteAsync("state.json", state, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ControlPlaneUser>> ReadUsersAsync(CancellationToken cancellationToken = default) =>
        await ReadAsync("users.json", Array.Empty<ControlPlaneUser>(), cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public Task WriteUsersAsync(IReadOnlyList<ControlPlaneUser> users, CancellationToken cancellationToken = default) =>
        WriteAsync("users.json", users, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ControlPlaneSession>> ReadSessionsAsync(CancellationToken cancellationToken = default) =>
        await ReadAsync("sessions.json", Array.Empty<ControlPlaneSession>(), cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public Task WriteSessionsAsync(IReadOnlyList<ControlPlaneSession> sessions, CancellationToken cancellationToken = default) =>
        WriteAsync("sessions.json", sessions, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ControlPlaneServiceTicket>> ReadServiceTicketsAsync(CancellationToken cancellationToken = default) =>
        await ReadAsync("service-tickets.json", Array.Empty<ControlPlaneServiceTicket>(), cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public Task WriteServiceTicketsAsync(IReadOnlyList<ControlPlaneServiceTicket> tickets, CancellationToken cancellationToken = default) =>
        WriteAsync("service-tickets.json", tickets, cancellationToken);

    /// <inheritdoc />
    public Task DeleteBootstrapTicketFileAsync(CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(ResolveDirectory(), options.CurrentValue.Bootstrap.TicketFileName);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task AppendAuditAsync(string action, string? userName, string message, CancellationToken cancellationToken = default)
    {
        var current = options.CurrentValue;
        current.Normalize();
        if (!current.Audit.Enabled)
        {
            return;
        }

        var line = JsonSerializer.Serialize(new
        {
            timeUtc = DateTimeOffset.UtcNow,
            productCode = current.ProductCode,
            action,
            userName,
            message
        }) + Environment.NewLine;

        var path = Path.Combine(ResolveDirectory(), current.Audit.FileName);
        await writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.AppendAllTextAsync(path, line, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            writeLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task WriteBootstrapTicketFileAsync(string plainTextTicket, DateTimeOffset expiresAtUtc, CancellationToken cancellationToken = default)
    {
        var current = options.CurrentValue;
        current.Normalize();
        if (!current.Bootstrap.WriteTicketFile)
        {
            return;
        }

        await WriteAsync(current.Bootstrap.TicketFileName, new
        {
            ticket = plainTextTicket,
            expiresAtUtc,
            note = "Use this ticket only for first administrator initialization. It is deleted after initialization."
        }, cancellationToken).ConfigureAwait(false);
    }

    private async Task<T> ReadAsync<T>(string fileName, T fallback, CancellationToken cancellationToken)
    {
        var path = Path.Combine(ResolveDirectory(), fileName);
        if (!File.Exists(path))
        {
            return fallback;
        }

        await using var stream = File.OpenRead(path);
        var value = await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken).ConfigureAwait(false);
        return value ?? fallback;
    }

    private async Task WriteAsync<T>(string fileName, T value, CancellationToken cancellationToken)
    {
        var path = Path.Combine(ResolveDirectory(), fileName);
        var temporaryPath = path + ".tmp";
        await writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await using (var stream = File.Create(temporaryPath))
            {
                await JsonSerializer.SerializeAsync(stream, value, JsonOptions, cancellationToken).ConfigureAwait(false);
            }

            if (File.Exists(path))
            {
                File.Replace(temporaryPath, path, null);
            }
            else
            {
                File.Move(temporaryPath, path);
            }
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }

            writeLock.Release();
        }
    }

    private string ResolveDirectory()
    {
        var current = options.CurrentValue;
        current.Normalize();
        var directory = current.StorageDirectory;
        return Path.IsPathRooted(directory)
            ? Path.GetFullPath(directory)
            : Path.GetFullPath(Path.Combine(hostEnvironment.ContentRootPath, directory));
    }
}
