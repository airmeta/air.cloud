using Air.Cloud.Modules.ReverseTunnel.Agent;
using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Protocol;
using Air.Cloud.Modules.ReverseTunnel.Common.Runtime;
using Air.Cloud.Modules.ReverseTunnel.Extensions;
using Air.Cloud.Modules.ReverseTunnel.Server;
using Air.Cloud.Modules.ReverseTunnel.Server.Grpc;

using Grpc.Core;
using Grpc.Net.Client;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Air.Cloud.IntegrationTest.Modules.ReverseTunnel;

/// <summary>
/// <para>zh-cn:验证 Agent 使用 mTLS 通过真实 gRPC 查询 Server 目标配置。</para>
/// <para>en-us:Verifies that an Agent queries the Server desired configuration over real mTLS gRPC.</para>
/// </summary>
public sealed class ReverseTunnelAgentConfigurationGrpcIntegrationTests
{
    /// <summary>
    /// <para>zh-cn:验证高版本目标配置可以被 Agent 获取，而相同版本不会重复返回。</para>
    /// <para>en-us:Verifies that a newer desired version is fetched and an equal version is not returned again.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "ReverseTunnel")]
    public async Task Agent_should_fetch_newer_configuration_and_skip_equal_version()
    {
        using var material = TestCertificateMaterial.Create();
        var port = GetFreeTcpPort();
        var serverOptions = CreateServerOptions(material);
        var desired = ReverseTunnelAgentConfigurationSnapshotSerializer.Create(
            "agent-alpha",
            CreateAgentOptions("https://localhost:" + port, material, 1),
            7);

        await using var app = await StartServerAsync(port, serverOptions, material, desired);
        var agentOptions = CreateAgentOptions($"https://localhost:{port}", material, 0);
        var (client, store) = CreateClient(agentOptions);

        var first = await client.FetchAsync();
        Assert.True(first.Succeeded);
        Assert.NotNull(first.Snapshot);
        Assert.Equal(7, first.ServerVersion);
        Assert.Equal(7, first.Snapshot!.Version);

        await store.SaveAsync(first.Snapshot);
        var second = await client.FetchAsync();
        Assert.True(second.Succeeded);
        Assert.Null(second.Snapshot);
        Assert.Equal(7, second.ServerVersion);
    }

    /// <summary>
    /// <para>zh-cn:验证 Server 返回哈希错误的快照时，Agent 拒绝更新并保留失败原因。</para>
    /// <para>en-us:Verifies that the Agent rejects a snapshot with an invalid hash and exposes the failure reason.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "ReverseTunnel")]
    public async Task Agent_should_reject_invalid_hash_from_server()
    {
        using var material = TestCertificateMaterial.Create();
        var port = GetFreeTcpPort();
        var serverOptions = CreateServerOptions(material);
        var desired = ReverseTunnelAgentConfigurationSnapshotSerializer.Create(
            "agent-alpha",
            CreateAgentOptions("https://localhost:" + port, material, 1),
            8);
        desired.Sha256 = new string('0', 64);

        await using var app = await StartServerAsync(port, serverOptions, material, desired, allowInvalidSnapshot: true);
        var (client, _) = CreateClient(CreateAgentOptions($"https://localhost:{port}", material, 0));

        var result = await client.FetchAsync();

        Assert.False(result.Succeeded);
        Assert.Null(result.Snapshot);
        Assert.Equal(8, result.ServerVersion);
        Assert.Equal("Server returned an invalid configuration snapshot.", result.FailureReason);
    }

    /// <summary>
    /// <para>zh-cn:验证 Agent 以旧版本重连时收到配置下发，而以最新版本重连时不会重复下发。</para>
    /// <para>en-us:Verifies that reconnecting with an old version receives a configuration offer and reconnecting with the latest version does not receive it again.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "ReverseTunnel")]
    public async Task Server_should_replay_only_when_agent_configuration_version_is_old()
    {
        using var material = TestCertificateMaterial.Create();
        var port = GetFreeTcpPort();
        var serverOptions = CreateServerOptions(material);
        var desired = ReverseTunnelAgentConfigurationSnapshotSerializer.Create(
            "agent-alpha",
            CreateAgentOptions($"https://localhost:{port}", material, 1),
            9);

        await using var app = await StartServerAsync(port, serverOptions, material, desired);
        using var clientCertificate = LoadClientCertificate(material);
        using var handler = new HttpClientHandler();
        handler.ClientCertificates.Add(clientCertificate);
        handler.ServerCertificateCustomValidationCallback = (_, certificate, _, errors) =>
            ValidateServerCertificate(certificate, material.CaCrtPath, errors);
        using var channel = GrpcChannel.ForAddress($"https://localhost:{port}", new GrpcChannelOptions
        {
            HttpHandler = handler
        });
        var client = new ReverseTunnelTransport.ReverseTunnelTransportClient(channel);

        using var oldVersionTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var oldVersionCall = client.Connect(cancellationToken: oldVersionTimeout.Token);
        await oldVersionCall.RequestStream.WriteAsync(new TunnelFrameMessage
        {
            ProtocolVersion = ReverseTunnelProtocol.CurrentVersion,
            SessionId = "old-version-session",
            Kind = TunnelFrameKindMessage.Hello,
            AgentId = "agent-alpha",
            ConfigurationVersion = 0
        }, oldVersionTimeout.Token);
        Assert.True(await oldVersionCall.ResponseStream.MoveNext(oldVersionTimeout.Token));
        Assert.Equal(TunnelFrameKindMessage.Welcome, oldVersionCall.ResponseStream.Current.Kind);
        Assert.True(await oldVersionCall.ResponseStream.MoveNext(oldVersionTimeout.Token));
        Assert.Equal(TunnelFrameKindMessage.ConfigurationOffer, oldVersionCall.ResponseStream.Current.Kind);
        await oldVersionCall.RequestStream.CompleteAsync();

        using var currentVersionTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var currentVersionCall = client.Connect(cancellationToken: currentVersionTimeout.Token);
        await currentVersionCall.RequestStream.WriteAsync(new TunnelFrameMessage
        {
            ProtocolVersion = ReverseTunnelProtocol.CurrentVersion,
            SessionId = "current-version-session",
            Kind = TunnelFrameKindMessage.Hello,
            AgentId = "agent-alpha",
            ConfigurationVersion = 9
        }, currentVersionTimeout.Token);
        Assert.True(await currentVersionCall.ResponseStream.MoveNext(currentVersionTimeout.Token));
        Assert.Equal(TunnelFrameKindMessage.Welcome, currentVersionCall.ResponseStream.Current.Kind);
        using var noOfferTimeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(400));
        var noOfferException = await Assert.ThrowsAsync<RpcException>(
            () => currentVersionCall.ResponseStream.MoveNext(noOfferTimeout.Token));
        Assert.Equal(StatusCode.Cancelled, noOfferException.StatusCode);
        await currentVersionCall.RequestStream.CompleteAsync();
    }

    private static async Task<WebApplication> StartServerAsync(
        int port,
        ReverseTunnelOptions options,
        TestCertificateMaterial material,
        ReverseTunnelAgentConfigurationSnapshot desired,
        bool allowInvalidSnapshot = false)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.ConfigureKestrel(serverOptions =>
        {
            serverOptions.ListenLocalhost(port, listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http2;
                listenOptions.UseHttps(material.ServerPfxPath, "test-ca-password", httpsOptions =>
                {
                    httpsOptions.ClientCertificateMode = ClientCertificateMode.AllowCertificate;
                    httpsOptions.ClientCertificateValidation = (_, _, _) => true;
                });
            });
        });
        builder.Services.AddReverseTunnel();
        builder.Services.AddSingleton<IOptions<ReverseTunnelOptions>>(Options.Create(options));
        builder.Services.AddSingleton<IReverseTunnelDesiredAgentConfigurationStore>(
            new FixedDesiredConfigurationStore(desired, allowInvalidSnapshot));

        var app = builder.Build();
        app.UseRouting();
        app.MapReverseTunnelServer();
        await app.StartAsync();
        return app;
    }

    private static (IReverseTunnelAgentConfigurationClient Client, IReverseTunnelAgentConfigurationStore Store) CreateClient(
        ReverseTunnelOptions options)
    {
        var startupOptions = Options.Create(options);
        var runtimeOptions = new ReverseTunnelRuntimeOptionsAccessor(startupOptions);
        var store = new MountedReverseTunnelAgentConfigurationStore(startupOptions);
        var materialStore = new MountedReverseTunnelAgentCertificateMaterialStore(startupOptions);
        return (
            new ReverseTunnelAgentConfigurationClient(runtimeOptions, store, materialStore),
            store);
    }

    private static ReverseTunnelOptions CreateServerOptions(TestCertificateMaterial material)
    {
        var options = new ReverseTunnelOptions
        {
            Enabled = true,
            TunnelMode = ReverseTunnelMode.Server,
            CertificateAuthority = new ReverseTunnelCertificateAuthorityOptions
            {
                CertificatePath = material.CaPfxPath,
                CertificatePasswordEnvironmentVariable = material.PasswordEnvironmentVariable
            }
        };
        options.PostConfigure(options, new ConfigurationBuilder().Build());
        return options;
    }

    private static ReverseTunnelOptions CreateAgentOptions(
        string serverAddress,
        TestCertificateMaterial material,
        long _)
    {
        var options = new ReverseTunnelOptions
        {
            Enabled = true,
            TunnelMode = ReverseTunnelMode.Agent,
            AgentId = "agent-alpha",
            ServerAddress = serverAddress,
            AgentCertificate = new ReverseTunnelAgentCertificateOptions
            {
                CertificatePath = material.AgentCertificatePath,
                PrivateKeyPath = material.AgentPrivateKeyPath,
                TrustedIssuerPath = material.CaCrtPath
            }
        };
        options.PostConfigure(options, new ConfigurationBuilder().Build());
        return options;
    }

    private static int GetFreeTcpPort()
    {
        var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static bool ValidateServerCertificate(X509Certificate? certificate, string trustedIssuerPath, SslPolicyErrors errors)
    {
        if ((errors & ~SslPolicyErrors.RemoteCertificateChainErrors) != SslPolicyErrors.None || certificate is null)
        {
            return false;
        }

        using var trustedIssuer = X509CertificateLoader.LoadCertificateFromFile(trustedIssuerPath);
        using var serverCertificate = certificate is X509Certificate2 certificate2
            ? new X509Certificate2(certificate2)
            : new X509Certificate2(certificate);
        using var chain = new X509Chain();
        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        chain.ChainPolicy.CustomTrustStore.Add(trustedIssuer);
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        return chain.Build(serverCertificate);
    }

    private static X509Certificate2 LoadClientCertificate(TestCertificateMaterial material)
    {
        using var certificate = X509CertificateLoader.LoadCertificateFromFile(material.AgentCertificatePath);
        using var key = RSA.Create();
        key.ImportFromPem(File.ReadAllText(material.AgentPrivateKeyPath));
        using var certificateWithPrivateKey = certificate.CopyWithPrivateKey(key);
        return X509CertificateLoader.LoadPkcs12(
            certificateWithPrivateKey.Export(X509ContentType.Pkcs12),
            password: null,
            X509KeyStorageFlags.Exportable);
    }

    private sealed class FixedDesiredConfigurationStore(
        ReverseTunnelAgentConfigurationSnapshot desired,
        bool allowInvalidSnapshot) : IReverseTunnelDesiredAgentConfigurationStore
    {
        public Task<ReverseTunnelAgentConfigurationSnapshot?> GetAsync(string agentId, CancellationToken cancellationToken = default)
        {
            if (!string.Equals(agentId, desired.AgentId, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult<ReverseTunnelAgentConfigurationSnapshot?>(null);
            }

            var snapshot = new ReverseTunnelAgentConfigurationSnapshot
            {
                Version = desired.Version,
                AgentId = desired.AgentId,
                GeneratedAtUtc = desired.GeneratedAtUtc,
                Sha256 = desired.Sha256,
                Configuration = ReverseTunnelOptionsCloner.CloneAndNormalize(desired.Configuration)
            };
            if (allowInvalidSnapshot)
            {
                snapshot.Sha256 = new string('0', 64);
            }
            return Task.FromResult<ReverseTunnelAgentConfigurationSnapshot?>(snapshot);
        }

        public Task SaveAsync(ReverseTunnelAgentConfigurationSnapshot snapshot, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<ReverseTunnelAgentConfigurationSnapshot>> ListAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ReverseTunnelAgentConfigurationSnapshot>>([desired]);
    }

    private sealed class TestCertificateMaterial : IDisposable
    {
        private TestCertificateMaterial(
            string directory,
            string caPfxPath,
            string caCrtPath,
            string serverPfxPath,
            string passwordEnvironmentVariable,
            string agentCertificatePath,
            string agentPrivateKeyPath)
        {
            DirectoryPath = directory;
            CaPfxPath = caPfxPath;
            CaCrtPath = caCrtPath;
            ServerPfxPath = serverPfxPath;
            PasswordEnvironmentVariable = passwordEnvironmentVariable;
            AgentCertificatePath = agentCertificatePath;
            AgentPrivateKeyPath = agentPrivateKeyPath;
            AgentDirectory = directory;
        }

        public string DirectoryPath { get; }
        public string CaPfxPath { get; }
        public string CaCrtPath { get; }
        public string ServerPfxPath { get; }
        public string PasswordEnvironmentVariable { get; }
        public string AgentCertificatePath { get; }
        public string AgentPrivateKeyPath { get; }
        public string AgentDirectory { get; }

        public static TestCertificateMaterial Create()
        {
            var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "air-cloud-reverse-tunnel-config-grpc", Guid.NewGuid().ToString("N")));
            const string password = "test-ca-password";
            var caPfxPath = Path.Combine(directory.FullName, "ca.pfx");
            var caCrtPath = Path.Combine(directory.FullName, "ca.crt");
            var serverPfxPath = Path.Combine(directory.FullName, "server.pfx");
            var agentCertificatePath = Path.Combine(directory.FullName, "agent.crt");
            var agentPrivateKeyPath = Path.Combine(directory.FullName, "agent.key");
            var passwordEnvironmentVariable = $"AIR_CLOUD_REVERSE_TUNNEL_CONFIG_GRPC_{Guid.NewGuid():N}";

            using var caKey = RSA.Create(2048);
            var caRequest = new CertificateRequest("CN=AirCloudReverseTunnelConfigTestCa", caKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            caRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
            caRequest.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, true));
            using var caCertificate = caRequest.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
            File.WriteAllBytes(caPfxPath, caCertificate.Export(X509ContentType.Pkcs12, password));
            File.WriteAllBytes(caCrtPath, caCertificate.Export(X509ContentType.Cert));
            File.WriteAllBytes(serverPfxPath, CreateServerCertificate(caCertificate, password));
            Environment.SetEnvironmentVariable(passwordEnvironmentVariable, password);

            using var agentKey = RSA.Create(2048);
            var agentRequest = new CertificateRequest("CN=agent-alpha", agentKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            agentRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
            agentRequest.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));
            agentRequest.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection
            {
                new("1.3.6.1.5.5.7.3.2", "Client Authentication")
            }, true));
            using var agentCertificate = agentRequest.Create(
                caCertificate,
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddMonths(1),
                RandomNumberGenerator.GetBytes(16));
            File.WriteAllBytes(agentCertificatePath, agentCertificate.Export(X509ContentType.Cert));
            File.WriteAllText(agentPrivateKeyPath, agentKey.ExportPkcs8PrivateKeyPem());

            return new TestCertificateMaterial(
                directory.FullName,
                caPfxPath,
                caCrtPath,
                serverPfxPath,
                passwordEnvironmentVariable,
                agentCertificatePath,
                agentPrivateKeyPath);
        }

        private static byte[] CreateServerCertificate(X509Certificate2 caCertificate, string password)
        {
            using var serverKey = RSA.Create(2048);
            var request = new CertificateRequest("CN=localhost", serverKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
            request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));
            request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection
            {
                new("1.3.6.1.5.5.7.3.1", "Server Authentication")
            }, true));
            var san = new SubjectAlternativeNameBuilder();
            san.AddDnsName("localhost");
            san.AddIpAddress(IPAddress.Loopback);
            request.CertificateExtensions.Add(san.Build());
            using var certificate = request.Create(
                caCertificate,
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddMonths(1),
                RandomNumberGenerator.GetBytes(16));
            using var withPrivateKey = certificate.CopyWithPrivateKey(serverKey);
            return withPrivateKey.Export(X509ContentType.Pkcs12, password);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable(PasswordEnvironmentVariable, null);
            if (Directory.Exists(DirectoryPath))
            {
                Directory.Delete(DirectoryPath, true);
            }
        }
    }
}


