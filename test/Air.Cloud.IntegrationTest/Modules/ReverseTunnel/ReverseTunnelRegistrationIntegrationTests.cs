using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Routing;
using Air.Cloud.Modules.ReverseTunnel.Extensions;
using Air.Cloud.Modules.ReverseTunnel.Agent;
using Air.Cloud.Modules.ReverseTunnel.Gateway;
using Air.Cloud.Modules.ReverseTunnel.Server;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Ocelot.DependencyInjection;

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
        Assert.Contains(serviceProvider.GetServices<IHostedService>(), item => item is ReverseTunnelAgentHostedService);
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
}
