using Air.Cloud.Modules.ReverseTunnel.Agent;
using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Protocol;
using Air.Cloud.Modules.ReverseTunnel.Extensions;
using Air.Cloud.Modules.ReverseTunnel.Gateway;
using Air.Cloud.Modules.ReverseTunnel.Server;
using Air.Cloud.Modules.ReverseTunnel.Server.Grpc;

using Google.Protobuf;

using Grpc.Core;
using Grpc.Net.Client;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Air.Cloud.IntegrationTest.Modules.ReverseTunnel;

/// <summary>
/// <para>zh-cn:反向隧道真实 gRPC 双向流端到端集成测试集合。</para>
/// <para>en-us:End-to-end integration tests for the reverse tunnel real gRPC bidirectional stream.</para>
/// </summary>
public sealed class ReverseTunnelGrpcEndToEndIntegrationTests
{
    /// <summary>
    /// <para>zh-cn:验证 Agent 通过真实 `ReverseTunnelTransport.Connect` 建立双向流后，Gateway 调用器可以经 Server 会话把 HTTP 请求转给 Agent 本地服务并拿到响应。</para>
    /// <para>en-us:Verifies that after an Agent opens a real `ReverseTunnelTransport.Connect` bidirectional stream, the Gateway invoker can send an HTTP request through the Server session to the Agent local service and receive the response.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "ReverseTunnel")]
    public async Task Grpc_connect_should_bridge_gateway_invoker_to_agent_local_http_dispatcher()
    {
        using var material = TestCertificateMaterial.Create();
        var options = CreateOptions(material);
        var optionsAccessor = Options.Create(options);
        using var agentKey = RSA.Create(2048);
        var csr = new CertificateRequest("CN=ignored-by-server", agentKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
            .CreateSigningRequest();
        var certificateAuthority = new MountedReverseTunnelCertificateAuthority(optionsAccessor);
        using var issuedCertificate = certificateAuthority.SignAgentCertificate("agent-alpha", csr, DateTimeOffset.UtcNow.AddMinutes(-1));
        using var agentCertificate = issuedCertificate.CopyWithPrivateKey(agentKey);
        var localHttpHandler = new CapturingLocalHttpHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("grpc-agent-ok", Encoding.UTF8, "text/plain")
        });

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddReverseTunnel();
        builder.Services.AddSingleton<IOptions<ReverseTunnelOptions>>(optionsAccessor);
        builder.Services.AddSingleton<IReverseTunnelAgentLocalHttpDispatcher>(_ =>
            new ReverseTunnelAgentLocalHttpDispatcher(optionsAccessor, new FixedHttpClientFactory(new HttpClient(localHttpHandler))));

        await using var app = builder.Build();
        app.UseRouting();
        app.Use(async (context, next) =>
        {
            context.Connection.ClientCertificate = agentCertificate;
            await next();
        });
        app.MapReverseTunnelServer();
        await app.StartAsync();

        var channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions
        {
            HttpHandler = app.GetTestServer().CreateHandler()
        });
        var transportClient = new ReverseTunnelTransport.ReverseTunnelTransportClient(channel);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var call = transportClient.Connect(cancellationToken: timeout.Token);
        await call.RequestStream.WriteAsync(new TunnelFrameMessage
        {
            ProtocolVersion = ReverseTunnelProtocol.CurrentVersion,
            SessionId = "agent-session-alpha",
            Kind = TunnelFrameKindMessage.Hello,
            AgentId = "agent-alpha",
            ServiceKeys = { "order-service" }
        }, timeout.Token);

        var agentLoop = RunAgentResponseLoopAsync(app.Services.GetRequiredService<IReverseTunnelAgentFrameProcessor>(), call, timeout.Token);
        var registry = app.Services.GetRequiredService<IReverseTunnelSessionRegistry>();
        await WaitForSessionAsync(registry, "order-service", timeout.Token);
        var invoker = app.Services.GetRequiredService<IReverseTunnelInvoker>();

        using var gatewayRequest = new HttpRequestMessage(HttpMethod.Post, "http://tunnel-order-service/api/orders?id=7")
        {
            Content = new StringContent("grpc-gateway-body", Encoding.UTF8, "application/json")
        };
        gatewayRequest.Headers.TryAddWithoutValidation("X-Trace-Id", "trace-grpc-e2e");
        using var response = await invoker.InvokeAsync(options.Routes[0], gatewayRequest, timeout.Token);
        var responseBody = await response.Content!.ReadAsStringAsync(timeout.Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("grpc-agent-ok", responseBody);
        Assert.Equal(new Uri("http://127.0.0.1:5101/api/orders?id=7"), localHttpHandler.LastRequestUri);
        Assert.Equal("grpc-gateway-body", localHttpHandler.LastBody);
        Assert.Contains("X-Trace-Id", localHttpHandler.LastRequestHeaderNames);

        await call.RequestStream.CompleteAsync();
        await agentLoop.WaitAsync(TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// <para>zh-cn:验证已通过 mTLS 认证的 Agent 如果声明未被 Server 白名单授权的 ServiceKey，会被 Connect 以 PermissionDenied 拒绝且不会注册会话。</para>
    /// <para>en-us:Verifies that an mTLS-authenticated Agent declaring a ServiceKey outside the Server allowlist is rejected from Connect with PermissionDenied and no session is registered.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "ReverseTunnel")]
    public async Task Grpc_connect_should_reject_service_keys_not_allowed_for_authenticated_agent()
    {
        using var material = TestCertificateMaterial.Create();
        var options = CreateOptions(material);
        options.AgentAuthorizations =
        [
            new ReverseTunnelAgentAuthorizationOptions
            {
                AgentId = "agent-alpha",
                AllowedServiceKeys = ["system"]
            }
        ];
        options.PostConfigure(options, new ConfigurationBuilder().Build());
        var optionsAccessor = Options.Create(options);
        using var agentKey = RSA.Create(2048);
        var csr = new CertificateRequest("CN=ignored-by-server", agentKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
            .CreateSigningRequest();
        var certificateAuthority = new MountedReverseTunnelCertificateAuthority(optionsAccessor);
        using var issuedCertificate = certificateAuthority.SignAgentCertificate("agent-alpha", csr, DateTimeOffset.UtcNow.AddMinutes(-1));
        using var agentCertificate = issuedCertificate.CopyWithPrivateKey(agentKey);

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddReverseTunnel();
        builder.Services.AddSingleton<IOptions<ReverseTunnelOptions>>(optionsAccessor);

        await using var app = builder.Build();
        app.UseRouting();
        app.Use(async (context, next) =>
        {
            context.Connection.ClientCertificate = agentCertificate;
            await next();
        });
        app.MapReverseTunnelServer();
        await app.StartAsync();

        var channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions
        {
            HttpHandler = app.GetTestServer().CreateHandler()
        });
        var transportClient = new ReverseTunnelTransport.ReverseTunnelTransportClient(channel);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var call = transportClient.Connect(cancellationToken: timeout.Token);
        await call.RequestStream.WriteAsync(new TunnelFrameMessage
        {
            ProtocolVersion = ReverseTunnelProtocol.CurrentVersion,
            SessionId = "agent-session-unauthorized",
            Kind = TunnelFrameKindMessage.Hello,
            AgentId = "agent-alpha",
            ServiceKeys = { "account" }
        }, timeout.Token);

        var exception = await Assert.ThrowsAsync<RpcException>(() => call.ResponseStream.MoveNext(timeout.Token));
        var registry = app.Services.GetRequiredService<IReverseTunnelSessionRegistry>();

        Assert.Equal(StatusCode.PermissionDenied, exception.StatusCode);
        Assert.Equal("Agent 'agent-alpha' is not authorized to declare ServiceKey 'account'.", exception.Status.Detail);
        Assert.False(registry.TryGetForServiceKey("account", out _));
    }

    private static async Task RunAgentResponseLoopAsync(IReverseTunnelAgentFrameProcessor frameProcessor, AsyncDuplexStreamingCall<TunnelFrameMessage, TunnelFrameMessage> call, CancellationToken cancellationToken)
    {
        while (await call.ResponseStream.MoveNext(cancellationToken))
        {
            var responses = await frameProcessor.ProcessAsync(call.ResponseStream.Current, cancellationToken);
            foreach (var response in responses)
            {
                await call.RequestStream.WriteAsync(response, cancellationToken);
            }
        }
    }

    private static async Task WaitForSessionAsync(IReverseTunnelSessionRegistry registry, string serviceKey, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (registry.TryGetForServiceKey(serviceKey, out _))
            {
                return;
            }

            await Task.Delay(25, cancellationToken);
        }
    }

    private static ReverseTunnelOptions CreateOptions(TestCertificateMaterial material)
    {
        var options = new ReverseTunnelOptions
        {
            Enabled = true,
            Roles = ReverseTunnelRole.Server | ReverseTunnelRole.Gateway,
            MaxRequestBodyBytes = 1024 * 1024,
            CertificateAuthority = new ReverseTunnelCertificateAuthorityOptions
            {
                CertificatePath = material.CaPfxPath,
                CertificatePasswordEnvironmentVariable = material.PasswordEnvironmentVariable
            },
            Routes =
            [
                new ReverseTunnelRouteOptions
                {
                    VirtualHost = "tunnel-order-service",
                    ServiceKey = "order-service",
                    TimeoutSeconds = 5
                }
            ],
            LocalServices =
            [
                new ReverseTunnelLocalServiceOptions
                {
                    ServiceKey = "order-service",
                    LocalAddress = "http://127.0.0.1:5101"
                }
            ]
        };
        options.PostConfigure(options, new ConfigurationBuilder().Build());
        return options;
    }

    private sealed class FixedHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient client;

        public FixedHttpClientFactory(HttpClient client)
        {
            this.client = client;
        }

        public HttpClient CreateClient(string name) => client;
    }

    private sealed class CapturingLocalHttpHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage response;

        public CapturingLocalHttpHandler(HttpResponseMessage response)
        {
            this.response = response;
        }

        public Uri? LastRequestUri { get; private set; }

        public string? LastBody { get; private set; }

        public HashSet<string> LastRequestHeaderNames { get; } = new(StringComparer.OrdinalIgnoreCase);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            LastBody = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            foreach (var header in request.Headers)
            {
                LastRequestHeaderNames.Add(header.Key);
            }

            return response;
        }
    }

    private sealed class TestCertificateMaterial : IDisposable
    {
        private TestCertificateMaterial(string caPfxPath, string passwordEnvironmentVariable)
        {
            CaPfxPath = caPfxPath;
            PasswordEnvironmentVariable = passwordEnvironmentVariable;
        }

        public string CaPfxPath { get; }

        public string PasswordEnvironmentVariable { get; }

        public static TestCertificateMaterial Create()
        {
            var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "air-cloud-reverse-tunnel-grpc-tests", Guid.NewGuid().ToString("N")));
            var passwordEnvironmentVariable = $"AIR_CLOUD_REVERSE_TUNNEL_GRPC_CA_PASSWORD_{Guid.NewGuid():N}";
            const string password = "test-ca-password";
            var caPfxPath = Path.Combine(directory.FullName, "ca.pfx");

            using var caKey = RSA.Create(2048);
            var request = new CertificateRequest("CN=AirCloudReverseTunnelGrpcTestCa", caKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
            request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, true));
            using var caCertificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
            File.WriteAllBytes(caPfxPath, caCertificate.Export(X509ContentType.Pkcs12, password));
            Environment.SetEnvironmentVariable(passwordEnvironmentVariable, password);

            return new TestCertificateMaterial(caPfxPath, passwordEnvironmentVariable);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable(PasswordEnvironmentVariable, null);
            var directory = Path.GetDirectoryName(CaPfxPath);
            if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
