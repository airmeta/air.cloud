using Air.Cloud.Modules.ReverseTunnel.Agent;
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
            Roles = ReverseTunnelRole.Agent,
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

        public CapturingConnectionClient(int expectedConnectionCount)
        {
            this.expectedConnectionCount = expectedConnectionCount;
        }

        public TaskCompletionSource AllConnectionsStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public List<int> ConnectionIndexes { get; } = [];

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

            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
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

        public bool ShouldRenew(X509Certificate2 certificate, int renewalWindowDays, DateTimeOffset utcNow) => false;

        public string ReadEnrollmentCredential() => string.Empty;
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
