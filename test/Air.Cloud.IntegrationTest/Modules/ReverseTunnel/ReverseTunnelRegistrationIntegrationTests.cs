using Air.Cloud.Core.App;
using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Diagnostics;
using Air.Cloud.Modules.ReverseTunnel.Common.Routing;
using Air.Cloud.Modules.ReverseTunnel.Extensions;
using Air.Cloud.Modules.ReverseTunnel.Agent;
using Air.Cloud.Modules.ReverseTunnel.Gateway;
using Air.Cloud.Modules.ReverseTunnel.Server;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Ocelot.DependencyInjection;

using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Air.Cloud.IntegrationTest.Modules.ReverseTunnel;

/// <summary>
/// <para>zh-cn:反向隧道模块注册集成测试集合。</para>
/// <para>en-us:Integration test suite for reverse tunnel module registration.</para>
/// </summary>
public class ReverseTunnelRegistrationIntegrationTests
{
    /// <summary>
    /// <para>zh-cn:验证模块注册可在空服务集合中完成，并能解析配置驱动的路由提供程序。该测试不建立网络连接。</para>
    /// <para>en-us:Verifies that module registration completes in an empty service collection and resolves the configuration-driven route provider. The test opens no network connection.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "ReverseTunnel")]
    public void AddReverseTunnel_should_register_route_provider_without_network_side_effects()
    {
        var settings = new ReverseTunnelOptions
        {
            Enabled = true,
            Routes =
            [
                new ReverseTunnelRouteOptions
                {
                    VirtualHost = "tunnel-integration-service",
                    ServiceKey = "integration-service"
                }
            ]
        };
        settings.PostConfigure(settings, new ConfigurationBuilder().Build());
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddReverseTunnel();
        services.AddSingleton<IOptions<ReverseTunnelOptions>>(Options.Create(settings));

        using var serviceProvider = services.BuildServiceProvider();
        var routeProvider = serviceProvider.GetRequiredService<IReverseTunnelRouteProvider>();

        var found = routeProvider.TryResolve("tunnel-integration-service", out var route);

        Assert.True(found);
        Assert.NotNull(route);
        Assert.Equal("integration-service", route.ServiceKey);
    }

    /// <summary>
    /// <para>zh-cn:验证模块注册会加入 Server mTLS 控制面依赖，但不会自动映射 gRPC endpoint。</para>
    /// <para>en-us:Verifies that module registration adds Server mTLS control-plane dependencies without automatically mapping a gRPC endpoint.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "ReverseTunnel")]
    public void AddReverseTunnel_should_register_certificate_control_plane_services()
    {
        var services = new ServiceCollection();

        services.AddReverseTunnel();

        using var serviceProvider = services.BuildServiceProvider();

        Assert.NotNull(serviceProvider.GetRequiredService<IReverseTunnelCertificateAuthority>());
        Assert.NotNull(serviceProvider.GetRequiredService<IReverseTunnelCertificateStatusRegistry>());
        Assert.NotNull(serviceProvider.GetRequiredService<IReverseTunnelAgentCertificateValidator>());
        Assert.NotNull(serviceProvider.GetRequiredService<IReverseTunnelAgentConnectionAuthenticator>());
        Assert.NotNull(serviceProvider.GetRequiredService<IReverseTunnelAgentServiceAuthorizer>());
        Assert.NotNull(serviceProvider.GetRequiredService<IReverseTunnelEnrollmentCredentialStore>());
        Assert.NotNull(serviceProvider.GetRequiredService<IReverseTunnelAgentEnrollmentService>());
        Assert.NotNull(serviceProvider.GetRequiredService<IReverseTunnelAgentCertificateRenewalService>());
        Assert.NotNull(serviceProvider.GetRequiredService<IReverseTunnelAgentCertificateMaterialStore>());
        Assert.NotNull(serviceProvider.GetRequiredService<IReverseTunnelAgentCertificateClient>());
        Assert.NotNull(serviceProvider.GetRequiredService<IReverseTunnelAgentLocalHttpDispatcher>());
        Assert.NotNull(serviceProvider.GetRequiredService<IReverseTunnelAgentConnectionClient>());
        Assert.NotNull(serviceProvider.GetRequiredService<IReverseTunnelDiagnosticsService>());
        Assert.Contains(serviceProvider.GetServices<IHostedService>(), item => item is ReverseTunnelAgentHostedService);
        Assert.Contains(serviceProvider.GetServices<IHostedService>(), item => item is ReverseTunnelDiagnosticsHostedService);
        Assert.NotNull(serviceProvider.GetRequiredService<IReverseTunnelInvoker>());
        Assert.NotNull(serviceProvider.GetRequiredService<ReverseTunnelDelegatingHandler>());
        Assert.Contains(serviceProvider.GetServices<IValidateOptions<ReverseTunnelOptions>>(), item => item is ReverseTunnelOptionsValidation);
    }

    /// <summary>
    /// <para>zh-cn:验证 Ocelot builder 扩展可显式登记反向隧道 DelegatingHandler，而不需要修改 Ocelot 源码。</para>
    /// <para>en-us:Verifies that the Ocelot builder extension can explicitly register the reverse tunnel DelegatingHandler without modifying Ocelot source code.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "ReverseTunnel")]
    public void AddReverseTunnelDelegatingHandler_should_register_ocelot_handler_extension()
    {
        var services = new ServiceCollection();

        services.AddReverseTunnel();
        services.AddOcelot(new ConfigurationBuilder().Build())
            .AddReverseTunnelDelegatingHandler();

        using var serviceProvider = services.BuildServiceProvider();

        Assert.NotNull(serviceProvider.GetRequiredService<ReverseTunnelDelegatingHandler>());
    }

    /// <summary>
    /// <para>zh-cn:验证显式映射 Server endpoint 时，如果模块未启用会立即失败，避免误暴露 Agent 接入面。</para>
    /// <para>en-us:Verifies that explicit Server endpoint mapping fails immediately when the module is not enabled, preventing accidental exposure of the Agent ingress surface.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "ReverseTunnel")]
    public void MapReverseTunnelServer_should_require_enabled_server_role()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddReverseTunnel();
        builder.Services.AddSingleton<IOptions<ReverseTunnelOptions>>(Options.Create(new ReverseTunnelOptions
        {
            Enabled = false,
            Roles = ReverseTunnelRole.Server
        }));
        using var app = builder.Build();

        var exception = Assert.Throws<InvalidOperationException>(() => app.MapReverseTunnelServer());

        Assert.Equal("Reverse tunnel Server endpoint mapping requires ReverseTunnelSettings:Enabled=true.", exception.Message);
    }

    /// <summary>
    /// <para>zh-cn:验证 CA 挂载配置有效时，显式映射会把反向隧道 gRPC Server endpoint 加入路由表。</para>
    /// <para>en-us:Verifies that explicit mapping adds the reverse tunnel gRPC Server endpoint to the route table when CA mount configuration is valid.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "ReverseTunnel")]
    public void MapReverseTunnelServer_should_add_grpc_endpoint_when_ca_mount_is_valid()
    {
        using var material = TestCertificateMaterial.Create();
        var options = new ReverseTunnelOptions
        {
            Enabled = true,
            Roles = ReverseTunnelRole.Server,
            CertificateAuthority = new ReverseTunnelCertificateAuthorityOptions
            {
                CertificatePath = material.CaPfxPath,
                CertificatePasswordEnvironmentVariable = material.PasswordEnvironmentVariable
            }
        };
        options.PostConfigure(options, new ConfigurationBuilder().Build());
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddReverseTunnel();
        builder.Services.AddSingleton<IOptions<ReverseTunnelOptions>>(Options.Create(options));
        using var app = builder.Build();
        var endpoints = (IEndpointRouteBuilder)app;

        app.MapReverseTunnelServer();

        Assert.NotEmpty(endpoints.DataSources);
        Assert.Contains(endpoints.DataSources.SelectMany(source => source.Endpoints), endpoint =>
            endpoint.DisplayName?.Contains("ReverseTunnelTransport", StringComparison.OrdinalIgnoreCase) == true);
        Assert.Contains(endpoints.DataSources.SelectMany(source => source.Endpoints), endpoint =>
            endpoint.DisplayName?.Contains("ReverseTunnelCertificateManagement", StringComparison.OrdinalIgnoreCase) == true);
        Assert.DoesNotContain(endpoints.DataSources.SelectMany(source => source.Endpoints), endpoint =>
            endpoint.DisplayName?.Contains("ReverseTunnelEnrollment", StringComparison.OrdinalIgnoreCase) == true);
        Assert.Contains(endpoints.DataSources.SelectMany(source => source.Endpoints), endpoint =>
            endpoint.DisplayName?.Contains("AirTunnel diagnostics snapshot", StringComparison.OrdinalIgnoreCase) == true);
    }

    /// <summary>
    /// <para>zh-cn:验证诊断 endpoint 会固定映射到路由表，显式启用诊断配置后请求返回当前进程快照。</para>
    /// <para>en-us:Verifies that diagnostic endpoints are always mapped into the route table and return current-process snapshots when diagnostics are explicitly enabled.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "ReverseTunnel")]
    public void MapReverseTunnelServer_should_add_diagnostic_endpoints_when_enabled()
    {
        using var material = TestCertificateMaterial.Create();
        var options = new ReverseTunnelOptions
        {
            Enabled = true,
            Roles = ReverseTunnelRole.Server,
            CertificateAuthority = new ReverseTunnelCertificateAuthorityOptions
            {
                CertificatePath = material.CaPfxPath,
                CertificatePasswordEnvironmentVariable = material.PasswordEnvironmentVariable
            },
            Diagnostics = new ReverseTunnelDiagnosticsOptions
            {
                Enabled = true,
                PathPrefix = "/airtunnel/status"
            }
        };
        options.PostConfigure(options, new ConfigurationBuilder().Build());
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddReverseTunnel();
        builder.Services.AddSingleton<IOptions<ReverseTunnelOptions>>(Options.Create(options));
        using var app = builder.Build();
        var endpoints = (IEndpointRouteBuilder)app;

        app.MapReverseTunnelServer();

        var displayNames = endpoints.DataSources
            .SelectMany(source => source.Endpoints)
            .Select(endpoint => endpoint.DisplayName)
            .Where(displayName => !string.IsNullOrWhiteSpace(displayName))
            .ToArray();
        Assert.Contains(displayNames, displayName => displayName!.Contains("AirTunnel diagnostics snapshot", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(displayNames, displayName => displayName!.Contains("AirTunnel diagnostics metrics", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(displayNames, displayName => displayName!.Contains("AirTunnel diagnostics sessions", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(StatusCodes.Status200OK, InvokeEndpoint(app, "AirTunnel diagnostics snapshot"));
    }

    /// <summary>
    /// <para>zh-cn:验证诊断 endpoint 在运行期配置关闭时仍存在于路由表，但请求会返回 404。</para>
    /// <para>en-us:Verifies that diagnostic endpoints remain in the route table when runtime diagnostics are disabled, but requests return 404.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "ReverseTunnel")]
    public void MapReverseTunnelServer_should_return_not_found_when_diagnostics_are_disabled()
    {
        using var material = TestCertificateMaterial.Create();
        var options = new ReverseTunnelOptions
        {
            Enabled = true,
            Roles = ReverseTunnelRole.Server,
            CertificateAuthority = new ReverseTunnelCertificateAuthorityOptions
            {
                CertificatePath = material.CaPfxPath,
                CertificatePasswordEnvironmentVariable = material.PasswordEnvironmentVariable
            },
            Diagnostics = new ReverseTunnelDiagnosticsOptions
            {
                Enabled = false
            }
        };
        options.PostConfigure(options, new ConfigurationBuilder().Build());
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddReverseTunnel();
        builder.Services.AddSingleton<IOptions<ReverseTunnelOptions>>(Options.Create(options));
        using var app = builder.Build();

        app.MapReverseTunnelServer();

        Assert.Equal(StatusCodes.Status404NotFound, InvokeEndpoint(app, "AirTunnel diagnostics snapshot"));
    }

    /// <summary>
    /// <para>zh-cn:验证诊断 endpoint 可以独立映射在 Agent-only 进程中，不依赖 Server 角色、CA 挂载或 gRPC Server endpoint。</para>
    /// <para>en-us:Verifies that diagnostic endpoints can be mapped independently in an Agent-only process without depending on the Server role, CA mounts, or gRPC Server endpoints.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "ReverseTunnel")]
    public void MapReverseTunnelDiagnostics_should_not_require_server_role()
    {
        var options = new ReverseTunnelOptions
        {
            Enabled = true,
            Roles = ReverseTunnelRole.Agent,
            AgentId = "agent-diagnostics",
            ServerAddress = "https://airtunnel.example.com",
            Diagnostics = new ReverseTunnelDiagnosticsOptions
            {
                Enabled = true
            },
            LocalServices =
            [
                new ReverseTunnelLocalServiceOptions
                {
                    ServiceKey = "account",
                    LocalAddress = "http://127.0.0.1:5100"
                }
            ]
        };
        options.PostConfigure(options, new ConfigurationBuilder().Build());
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddReverseTunnel();
        builder.Services.AddSingleton<IOptions<ReverseTunnelOptions>>(Options.Create(options));
        using var app = builder.Build();
        var endpoints = (IEndpointRouteBuilder)app;

        app.MapReverseTunnelDiagnostics();

        Assert.Contains(endpoints.DataSources.SelectMany(source => source.Endpoints), endpoint =>
            endpoint.DisplayName?.Contains("AirTunnel diagnostics snapshot", StringComparison.OrdinalIgnoreCase) == true);
        Assert.DoesNotContain(endpoints.DataSources.SelectMany(source => source.Endpoints), endpoint =>
            endpoint.DisplayName?.Contains("ReverseTunnelTransport", StringComparison.OrdinalIgnoreCase) == true);
        Assert.Equal(StatusCodes.Status200OK, InvokeEndpoint(app, "AirTunnel diagnostics snapshot"));
    }

    /// <summary>
    /// <para>zh-cn:验证诊断 endpoint 放在业务中间件之前时会直接返回诊断响应，后续签名、白名单或 Ocelot 类中间件不会拦截该路径。</para>
    /// <para>en-us:Verifies that when diagnostic endpoints are placed before business middlewares, the diagnostic response is returned directly and later signature, whitelist, or Ocelot-like middlewares do not intercept the path.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "ReverseTunnel")]
    public async Task UseReverseTunnelDiagnostics_should_short_circuit_later_middlewares()
    {
        var options = new ReverseTunnelOptions
        {
            Enabled = true,
            Roles = ReverseTunnelRole.Agent,
            AgentId = "agent-diagnostics",
            ServerAddress = "https://airtunnel.example.com",
            Diagnostics = new ReverseTunnelDiagnosticsOptions
            {
                Enabled = true
            },
            LocalServices =
            [
                new ReverseTunnelLocalServiceOptions
                {
                    ServiceKey = "account",
                    LocalAddress = "http://127.0.0.1:5100"
                }
            ]
        };
        options.PostConfigure(options, new ConfigurationBuilder().Build());
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddReverseTunnel();
        builder.Services.AddSingleton<IOptions<ReverseTunnelOptions>>(Options.Create(options));
        await using var app = builder.Build();

        app.UseRouting();
        app.UseReverseTunnelDiagnostics();
        app.Run(async context =>
        {
            context.Response.StatusCode = StatusCodes.Status418ImATeapot;
            await context.Response.WriteAsync("blocked-by-business-middleware");
        });

        await app.StartAsync();
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/airtunnel/diagnostics/snapshot");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Air.Cloud.Modules.ReverseTunnel", await response.Content.ReadAsStringAsync());
    }

    /// <summary>
    /// <para>zh-cn:验证诊断开关以运行期 `AppCore.Configuration` 为准，而不是只依赖启动时绑定的选项实例。</para>
    /// <para>en-us:Verifies that the diagnostic switch follows runtime `AppCore.Configuration` instead of only relying on the startup-bound options instance.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "ReverseTunnel")]
    public void Diagnostic_endpoint_should_use_runtime_appcore_configuration_switch()
    {
        var originalConfiguration = AppConfigurationLoader.Configurations;
        try
        {
            AppConfigurationLoader.Configurations = new ConfigurationManager();
            AppConfigurationLoader.Configurations.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ReverseTunnelSettings:Diagnostics:Enabled"] = "true",
                ["ReverseTunnelSettings:Diagnostics:SnapshotCollectionIntervalSeconds"] = "3"
            });
            using var material = TestCertificateMaterial.Create();
            var options = new ReverseTunnelOptions
            {
                Enabled = true,
                Roles = ReverseTunnelRole.Server,
                CertificateAuthority = new ReverseTunnelCertificateAuthorityOptions
                {
                    CertificatePath = material.CaPfxPath,
                    CertificatePasswordEnvironmentVariable = material.PasswordEnvironmentVariable
                },
                Diagnostics = new ReverseTunnelDiagnosticsOptions
                {
                    Enabled = false
                }
            };
            options.PostConfigure(options, new ConfigurationBuilder().Build());
            var builder = WebApplication.CreateBuilder();
            builder.Services.AddReverseTunnel();
            builder.Services.AddSingleton<IOptions<ReverseTunnelOptions>>(Options.Create(options));
            using var app = builder.Build();

            app.MapReverseTunnelServer();

            Assert.Equal(StatusCodes.Status200OK, InvokeEndpoint(app, "AirTunnel diagnostics snapshot"));
            var snapshot = app.Services.GetRequiredService<IReverseTunnelDiagnosticsService>().GetSnapshot();
            Assert.True(snapshot.DiagnosticsEnabled);
            Assert.Equal(3, snapshot.SnapshotCollectionIntervalSeconds);
        }
        finally
        {
            AppConfigurationLoader.Configurations = originalConfiguration;
        }
    }

    /// <summary>
    /// <para>zh-cn:验证显式启用 Enrollment 且挂载目录有效时，Server endpoint 映射会加入 Enrollment gRPC 服务。</para>
    /// <para>en-us:Verifies that Server endpoint mapping adds the Enrollment gRPC service when enrollment is explicitly enabled and the mounted directory is valid.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "ReverseTunnel")]
    public void MapReverseTunnelServer_should_add_enrollment_endpoint_when_enrollment_is_enabled()
    {
        using var material = TestCertificateMaterial.Create();
        var enrollmentDirectory = Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(material.CaPfxPath)!, "enrollment"));
        var options = new ReverseTunnelOptions
        {
            Enabled = true,
            Roles = ReverseTunnelRole.Server,
            CertificateAuthority = new ReverseTunnelCertificateAuthorityOptions
            {
                CertificatePath = material.CaPfxPath,
                CertificatePasswordEnvironmentVariable = material.PasswordEnvironmentVariable
            },
            Enrollment = new ReverseTunnelEnrollmentOptions
            {
                Enabled = true,
                CredentialDirectory = enrollmentDirectory.FullName
            }
        };
        options.PostConfigure(options, new ConfigurationBuilder().Build());
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddReverseTunnel();
        builder.Services.AddSingleton<IOptions<ReverseTunnelOptions>>(Options.Create(options));
        using var app = builder.Build();
        var endpoints = (IEndpointRouteBuilder)app;

        app.MapReverseTunnelServer();

        Assert.Contains(endpoints.DataSources.SelectMany(source => source.Endpoints), endpoint =>
            endpoint.DisplayName?.Contains("ReverseTunnelEnrollment", StringComparison.OrdinalIgnoreCase) == true);
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
            var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "air-cloud-reverse-tunnel-integration-tests", Guid.NewGuid().ToString("N")));
            var passwordEnvironmentVariable = $"AIR_CLOUD_REVERSE_TUNNEL_INTEGRATION_CA_PASSWORD_{Guid.NewGuid():N}";
            const string password = "test-ca-password";
            var caPfxPath = Path.Combine(directory.FullName, "ca.pfx");

            using var caKey = RSA.Create(2048);
            var request = new CertificateRequest("CN=AirCloudReverseTunnelIntegrationTestCa", caKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
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

    private static int InvokeEndpoint(WebApplication app, string displayName)
    {
        var endpoint = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .Single(item => item.DisplayName?.Contains(displayName, StringComparison.OrdinalIgnoreCase) == true);
        var routeEndpoint = Assert.IsType<RouteEndpoint>(endpoint);
        var httpContext = new DefaultHttpContext
        {
            RequestServices = app.Services
        };
        httpContext.Response.Body = new MemoryStream();
        routeEndpoint.RequestDelegate!(httpContext).GetAwaiter().GetResult();
        return httpContext.Response.StatusCode;
    }
}
