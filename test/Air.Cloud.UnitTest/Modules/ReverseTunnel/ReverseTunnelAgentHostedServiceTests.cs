using Air.Cloud.Modules.ReverseTunnel.Agent;
using Air.Cloud.Modules.ReverseTunnel.Common.Enrollment;
using Air.Cloud.Modules.ReverseTunnel.Common.Options;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Air.Cloud.UnitTest.Modules.ReverseTunnel;

/// <summary>
/// <para>zh-cn:AirTunnel Agent 后台服务生命周期测试集合。</para>
/// <para>en-us:Test suite for the AirTunnel Agent hosted-service lifecycle.</para>
/// </summary>
public sealed class ReverseTunnelAgentHostedServiceTests
{
    /// <summary>
    /// <para>zh-cn:验证单个 Agent 宿主进程会按配置启动多条独立物理连接循环，用于在单实例内分摊隧道并发。</para>
    /// <para>en-us:Verifies that a single Agent host process starts multiple independent physical connection loops according to configuration, allowing tunnel concurrency to be spread within one instance.</para>
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_should_start_configured_agent_connection_loops()
    {
        using var certificate = CreateCertificate();
        using var certificateFiles = TemporaryCertificateFiles.Create();
        var options = new ReverseTunnelOptions
        {
            Enabled = true,
            TunnelMode = ReverseTunnelMode.Agent,
            AgentId = "agent-alpha",
            ServerAddress = "https://airtunnel.example.com",
            AgentConnectionCount = 3,
            AgentCertificate = new ReverseTunnelAgentCertificateOptions
            {
                CertificatePath = certificateFiles.CertificatePath,
                PrivateKeyPath = certificateFiles.PrivateKeyPath
            }
        };
        options.PostConfigure(options, new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build());
        var connectionClient = new CapturingConnectionClient(expectedConnectionCount: 3);
        var service = new ReverseTunnelAgentHostedService(
            Options.Create(options),
            new NonRenewingCertificateMaterialStore(certificate),
            new NoopCertificateClient(),
            connectionClient,
            new NoopLogger<ReverseTunnelAgentHostedService>());

        await service.StartAsync(CancellationToken.None);
        await connectionClient.AllConnectionsStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await service.StopAsync(CancellationToken.None);

        Assert.Equal([0, 1, 2], connectionClient.ConnectionIndexes.Order().ToArray());
    }

    /// <summary>
    /// <para>zh-cn:验证运行期增加 AgentConnectionCount 后会创建新的连接循环，而无需重启宿主进程。</para>
    /// <para>en-us:Verifies that increasing AgentConnectionCount at runtime creates additional connection loops without restarting the host process.</para>
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_should_reconcile_connection_count_after_runtime_update()
    {
        using var certificate = CreateCertificate();
        using var certificateFiles = TemporaryCertificateFiles.Create();
        var options = new ReverseTunnelOptions
        {
            Enabled = true,
            TunnelMode = ReverseTunnelMode.Agent,
            AgentId = "agent-alpha",
            ServerAddress = "https://airtunnel.example.com",
            AgentConnectionCount = 1,
            AgentCertificate = new ReverseTunnelAgentCertificateOptions
            {
                CertificatePath = certificateFiles.CertificatePath,
                PrivateKeyPath = certificateFiles.PrivateKeyPath
            }
        };
        options.PostConfigure(options, new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build());
        var runtimeOptions = new Air.Cloud.Modules.ReverseTunnel.Common.Runtime.ReverseTunnelRuntimeOptionsAccessor(Options.Create(options));
        var connectionClient = new CapturingConnectionClient(expectedConnectionCount: 3);
        var service = new ReverseTunnelAgentHostedService(
            runtimeOptions,
            new InMemoryReverseTunnelAgentConfigurationStore(),
            new NonRenewingCertificateMaterialStore(certificate),
            new NoopCertificateClient(),
            connectionClient,
            new NoopLogger<ReverseTunnelAgentHostedService>());

        await service.StartAsync(CancellationToken.None);
        await connectionClient.WaitForConnectionCountAsync(1);

        var updated = new ReverseTunnelOptions
        {
            Enabled = true,
            TunnelMode = ReverseTunnelMode.Agent,
            AgentId = "agent-alpha",
            ServerAddress = "https://airtunnel.example.com",
            AgentConnectionCount = 3,
            AgentCertificate = options.AgentCertificate
        };
        updated.PostConfigure(updated, new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build());
        runtimeOptions.Update(updated);

        await connectionClient.AllConnectionsStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await service.StopAsync(CancellationToken.None);

        Assert.Contains(2, connectionClient.ConnectionIndexes);
    }

    /// <summary>
    /// <para>zh-cn:验证运行期减少 AgentConnectionCount 时，多余连接会收到取消信号并优雅退出。</para>
    /// <para>en-us:Verifies that reducing AgentConnectionCount at runtime cancels surplus loops and lets them exit gracefully.</para>
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_should_stop_surplus_connection_loops_after_runtime_decrease()
    {
        using var certificate = CreateCertificate();
        using var certificateFiles = TemporaryCertificateFiles.Create();
        var options = new ReverseTunnelOptions
        {
            Enabled = true,
            TunnelMode = ReverseTunnelMode.Agent,
            AgentId = "agent-alpha",
            ServerAddress = "https://airtunnel.example.com",
            AgentConnectionCount = 3,
            AgentCertificate = new ReverseTunnelAgentCertificateOptions
            {
                CertificatePath = certificateFiles.CertificatePath,
                PrivateKeyPath = certificateFiles.PrivateKeyPath
            }
        };
        options.PostConfigure(options, new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build());
        var runtimeOptions = new Air.Cloud.Modules.ReverseTunnel.Common.Runtime.ReverseTunnelRuntimeOptionsAccessor(Options.Create(options));
        var connectionClient = new CapturingConnectionClient(expectedConnectionCount: 3);
        var service = new ReverseTunnelAgentHostedService(
            runtimeOptions,
            new InMemoryReverseTunnelAgentConfigurationStore(),
            new NonRenewingCertificateMaterialStore(certificate),
            new NoopCertificateClient(),
            connectionClient,
            new NoopLogger<ReverseTunnelAgentHostedService>());

        await service.StartAsync(CancellationToken.None);
        await connectionClient.AllConnectionsStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await connectionClient.WaitForActiveConnectionCountAsync(3);

        var updated = new ReverseTunnelOptions
        {
            Enabled = true,
            TunnelMode = ReverseTunnelMode.Agent,
            AgentId = "agent-alpha",
            ServerAddress = "https://airtunnel.example.com",
            AgentConnectionCount = 1,
            AgentCertificate = options.AgentCertificate
        };
        updated.PostConfigure(updated, new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build());
        runtimeOptions.Update(updated);

        await connectionClient.WaitForActiveConnectionCountAsync(1);
        Assert.Equal(1, connectionClient.ActiveConnectionCount);
        await service.StopAsync(CancellationToken.None);
    }

    private static X509Certificate2 CreateCertificate()
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=agent-alpha", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
    }

    private sealed class CapturingConnectionClient : IReverseTunnelAgentConnectionClient
    {
        private readonly int expectedConnectionCount;
        private int startedConnectionCount;
        private int activeConnectionCount;

        public CapturingConnectionClient(int expectedConnectionCount)
        {
            this.expectedConnectionCount = expectedConnectionCount;
        }

        public TaskCompletionSource AllConnectionsStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public List<int> ConnectionIndexes { get; } = [];

        public int ActiveConnectionCount => Volatile.Read(ref activeConnectionCount);

        public async Task WaitForConnectionCountAsync(int count)
        {
            while (true)
            {
                lock (ConnectionIndexes)
                {
                    if (ConnectionIndexes.Count >= count)
                    {
                        return;
                    }
                }

                await Task.Delay(10);
            }
        }

        public Task RunOnceAsync(CancellationToken cancellationToken = default)
        {
            return RunOnceAsync(0, cancellationToken);
        }

        public async Task RunOnceAsync(int connectionIndex, CancellationToken cancellationToken = default)
        {
            lock (ConnectionIndexes)
            {
                ConnectionIndexes.Add(connectionIndex);
            }

            if (Interlocked.Increment(ref startedConnectionCount) == expectedConnectionCount)
            {
                AllConnectionsStarted.TrySetResult();
            }

            Interlocked.Increment(ref activeConnectionCount);
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            finally
            {
                Interlocked.Decrement(ref activeConnectionCount);
            }
        }

        public async Task WaitForActiveConnectionCountAsync(int count)
        {
            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (DateTime.UtcNow < deadline)
            {
                if (ActiveConnectionCount == count)
                {
                    return;
                }

                await Task.Delay(10);
            }

            throw new TimeoutException($"Expected {count} active connections, actual {ActiveConnectionCount}.");
        }
    }

    private sealed class NonRenewingCertificateMaterialStore : IReverseTunnelAgentCertificateMaterialStore
    {
        private readonly X509Certificate2 certificate;

        public NonRenewingCertificateMaterialStore(X509Certificate2 certificate)
        {
            this.certificate = certificate;
        }

        public byte[] CreateCertificateSigningRequest(string agentId) => [];

        public void SaveIssuedCertificate(byte[] certificate, byte[] issuerCertificate)
        {
        }

        public X509Certificate2 LoadClientCertificate() => X509CertificateLoader.LoadCertificate(certificate.RawData);

        public X509Certificate2 LoadCertificate() => X509CertificateLoader.LoadCertificate(certificate.RawData);

        public X509Certificate2 LoadTrustedIssuerCertificate() => X509CertificateLoader.LoadCertificate(certificate.RawData);

        public bool ShouldRenew(X509Certificate2 certificate, int renewalWindowDays, DateTimeOffset utcNow) => false;

        public string ReadEnrollmentCredential() => string.Empty;

        public ReverseTunnelEnrollmentToken ReadEnrollmentToken() => ReverseTunnelEnrollmentToken.Parse("test-token");
    }

    private sealed class NoopCertificateClient : IReverseTunnelAgentCertificateClient
    {
        public Task<ReverseTunnelAgentCertificateClientResult> EnrollAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ReverseTunnelAgentCertificateClientResult.Success(DateTimeOffset.UtcNow.AddDays(30)));
        }

        public Task<ReverseTunnelAgentCertificateClientResult> RenewAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ReverseTunnelAgentCertificateClientResult.Success(DateTimeOffset.UtcNow.AddDays(30)));
        }
    }

    private sealed class InMemoryReverseTunnelAgentConfigurationStore : IReverseTunnelAgentConfigurationStore
    {
        public long CurrentVersion { get; private set; }

        public Task<Air.Cloud.Modules.ReverseTunnel.Common.Runtime.ReverseTunnelAgentConfigurationSnapshot?> LoadAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<Air.Cloud.Modules.ReverseTunnel.Common.Runtime.ReverseTunnelAgentConfigurationSnapshot?>(null);

        public Task SaveAsync(Air.Cloud.Modules.ReverseTunnel.Common.Runtime.ReverseTunnelAgentConfigurationSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            CurrentVersion = snapshot.Version;
            return Task.CompletedTask;
        }
    }

    private sealed class NoopLogger<T> : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => false;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
        }
    }

    private sealed class TemporaryCertificateFiles : IDisposable
    {
        private TemporaryCertificateFiles(string directory, string certificatePath, string privateKeyPath)
        {
            Directory = directory;
            CertificatePath = certificatePath;
            PrivateKeyPath = privateKeyPath;
        }

        public string Directory { get; }

        public string CertificatePath { get; }

        public string PrivateKeyPath { get; }

        public static TemporaryCertificateFiles Create()
        {
            var directory = Path.Combine(Path.GetTempPath(), "air-cloud-reverse-tunnel-agent-hosted-service-tests", Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(directory);
            var certificatePath = Path.Combine(directory, "agent.cer");
            var privateKeyPath = Path.Combine(directory, "agent.key");
            File.WriteAllText(certificatePath, "placeholder");
            File.WriteAllText(privateKeyPath, "placeholder");
            return new TemporaryCertificateFiles(directory, certificatePath, privateKeyPath);
        }

        public void Dispose()
        {
            if (System.IO.Directory.Exists(Directory))
            {
                System.IO.Directory.Delete(Directory, recursive: true);
            }
        }
    }
}


