using Air.Cloud.Core.App;
using Air.Cloud.Modules.ReverseTunnel.Common.Enrollment;
using Air.Cloud.Modules.ReverseTunnel.Common.Options;
using Air.Cloud.Modules.ReverseTunnel.Common.Diagnostics;
using Air.Cloud.Modules.ReverseTunnel.Common.Routing;
using Air.Cloud.Modules.ReverseTunnel.Common.Runtime;
using Air.Cloud.Modules.ReverseTunnel.Extensions;
using Air.Cloud.Modules.ReverseTunnel.Agent;
using Air.Cloud.Modules.ReverseTunnel.Gateway;
using Air.Cloud.Modules.ReverseTunnel.Management;
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
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.IO.Compression;

namespace Air.Cloud.IntegrationTest.Modules.ReverseTunnel;

/// <summary>
/// <para>zh-cn:反向隧道模块注册集成测试集合。</para>
/// <para>en-us:Integration test suite for reverse tunnel module registration.</para>
/// </summary>
public class ReverseTunnelRegistrationIntegrationTests
{
    /// <summary>
    /// <para>zh-cn:验证模块注册可在空服务集合中完成，且路由提供程序不会读取旧的显式 Routes 配置。该测试不建立网络连接。</para>
    /// <para>en-us:Verifies that module registration completes in an empty service collection and the route provider ignores legacy explicit Routes configuration. The test opens no network connection.</para>
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

        Assert.False(found);
        Assert.Null(route);
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
        Assert.NotNull(serviceProvider.GetRequiredService<IReverseTunnelEnrollmentReplayStore>());
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
            TunnelMode = ReverseTunnelMode.Server
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
            TunnelMode = ReverseTunnelMode.Server,
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
            TunnelMode = ReverseTunnelMode.Server,
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
            TunnelMode = ReverseTunnelMode.Server,
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
            TunnelMode = ReverseTunnelMode.Agent,
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
    /// <para>zh-cn:验证模块关闭时仍可挂载诊断路由，运行期关闭开关时请求返回 404。</para>
    /// <para>en-us:Verifies that diagnostic routes can still be mapped when the module is disabled and return 404 while the runtime switch is off.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "ReverseTunnel")]
    public void MapReverseTunnelDiagnostics_should_map_when_module_is_disabled()
    {
        var options = new ReverseTunnelOptions
        {
            Enabled = false,
            TunnelMode = ReverseTunnelMode.Agent,
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

        app.MapReverseTunnelDiagnostics();

        Assert.Contains(((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints), endpoint =>
            endpoint.DisplayName?.Contains("AirTunnel diagnostics snapshot", StringComparison.OrdinalIgnoreCase) == true);
        Assert.Equal(StatusCodes.Status404NotFound, InvokeEndpoint(app, "AirTunnel diagnostics snapshot"));
    }

    /// <summary>
    /// <para>zh-cn:验证 Server 管理 endpoint 使用独立管理端口和访问 token；同一路由在非管理端口会返回 404。</para>
    /// <para>en-us:Verifies that the Server management endpoint uses the dedicated management port and access token; the same route returns 404 on a non-management port.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "ReverseTunnel")]
    public void MapReverseTunnelManagement_should_require_management_port_and_token()
    {
        var tokenVariable = $"AIR_TUNNEL_ADMIN_TOKEN_TEST_{Guid.NewGuid():N}";
        Environment.SetEnvironmentVariable(tokenVariable, "test-admin-token");
        try
        {
            var options = new ReverseTunnelOptions
            {
                Enabled = true,
                TunnelMode = ReverseTunnelMode.Server,
                Management = new ReverseTunnelManagementOptions
                {
                    Enabled = true,
                    BindUrl = "https://0.0.0.0:18643",
                    AccessTokenEnvironmentVariable = tokenVariable
                }
            };
            options.PostConfigure(options, new ConfigurationBuilder().Build());
            var builder = WebApplication.CreateBuilder();
            builder.Services.AddReverseTunnel();
            builder.Services.AddSingleton<IOptions<ReverseTunnelOptions>>(Options.Create(options));
            using var app = builder.Build();

            app.MapReverseTunnelManagement();

            Assert.Equal(StatusCodes.Status404NotFound, InvokeEndpoint(app, "AirTunnel management metrics", localPort: 18443, adminToken: "test-admin-token"));
            Assert.Equal(StatusCodes.Status401Unauthorized, InvokeEndpoint(app, "AirTunnel management metrics", localPort: 18643, adminToken: "wrong-token"));
            Assert.Equal(StatusCodes.Status200OK, InvokeEndpoint(app, "AirTunnel management metrics", localPort: 18643, adminToken: "test-admin-token"));
            Assert.Equal(StatusCodes.Status401Unauthorized, InvokeEndpoint(app, "AirTunnel management page", localPort: 18643, adminToken: "wrong-token"));
            Assert.Equal(StatusCodes.Status200OK, InvokeEndpoint(app, "AirTunnel management page", localPort: 18643, adminToken: "test-admin-token"));
        }
        finally
        {
            Environment.SetEnvironmentVariable(tokenVariable, null);
        }
    }

    /// <summary>
    /// <para>zh-cn:验证 AirTunnel 管理页面提供左侧菜单树和按菜单切换的独立内容区域。</para>
    /// <para>en-us:Verifies that the AirTunnel management page provides a left navigation tree and separate menu-driven content areas.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "ReverseTunnel")]
    public void MapReverseTunnelManagement_should_render_menu_tree_and_split_views()
    {
        var tokenVariable = $"AIR_TUNNEL_ADMIN_TOKEN_PAGE_{Guid.NewGuid():N}";
        Environment.SetEnvironmentVariable(tokenVariable, "test-admin-token");
        try
        {
            var options = new ReverseTunnelOptions
            {
                Enabled = true,
                TunnelMode = ReverseTunnelMode.Server,
                Management = new ReverseTunnelManagementOptions
                {
                    Enabled = true,
                    BindUrl = "https://0.0.0.0:18643",
                    AccessTokenEnvironmentVariable = tokenVariable
                }
            };
            options.PostConfigure(options, new ConfigurationBuilder().Build());
            var builder = WebApplication.CreateBuilder();
            builder.Services.AddReverseTunnel();
            builder.Services.AddSingleton<IOptions<ReverseTunnelOptions>>(Options.Create(options));
            using var app = builder.Build();

            app.MapReverseTunnelManagement();

            var html = InvokeEndpointBody(app, "AirTunnel management page", 18643, "test-admin-token");

            Assert.Contains("data-menu=\"overview\"", html, StringComparison.Ordinal);
            Assert.Contains("data-menu=\"agents\"", html, StringComparison.Ordinal);
            Assert.Contains("data-menu=\"sessions\"", html, StringComparison.Ordinal);
            Assert.Contains("data-menu=\"config\"", html, StringComparison.Ordinal);
            Assert.Contains("data-menu=\"agentConfig\"", html, StringComparison.Ordinal);
            Assert.Contains("data-menu=\"mappingConfig\"", html, StringComparison.Ordinal);
            Assert.Contains("data-menu=\"audit\"", html, StringComparison.Ordinal);
            Assert.Contains("data-menu=\"backups\"", html, StringComparison.Ordinal);
            Assert.Contains("id=\"overviewView\"", html, StringComparison.Ordinal);
            Assert.Contains("id=\"agentsView\"", html, StringComparison.Ordinal);
            Assert.Contains("id=\"sessionsView\"", html, StringComparison.Ordinal);
            Assert.Contains("id=\"configView\"", html, StringComparison.Ordinal);
            Assert.Contains("id=\"agentConfigView\"", html, StringComparison.Ordinal);
            Assert.Contains("id=\"mappingConfigView\"", html, StringComparison.Ordinal);
            Assert.Contains("id=\"mappingConfigAgents\"", html, StringComparison.Ordinal);
            Assert.Contains("id=\"agentLocalServices\"", html, StringComparison.Ordinal);
            Assert.Contains("id=\"agentManagementList\"", html, StringComparison.Ordinal);
            Assert.Contains("data-agent-runtime", html, StringComparison.Ordinal);
            Assert.Contains("id=\"agentRuntimeDialog\"", html, StringComparison.Ordinal);
            Assert.Contains("agentRuntimeDetail\">运行配置", html, StringComparison.Ordinal);
            Assert.Contains("agentRuntimeLoading", html, StringComparison.Ordinal);
            Assert.Contains("function openAgentRuntimeDialog", html, StringComparison.Ordinal);
            Assert.Contains("id=\"agentRuntimeConfig\"", html, StringComparison.Ordinal);
            Assert.Contains("id=\"createAgentDialog\"", html, StringComparison.Ordinal);
            Assert.Contains("function submitCreateAgent", html, StringComparison.Ordinal);
            Assert.Contains("function downloadAgentPackage", html, StringComparison.Ordinal);
            Assert.Contains("id=\"agentExportDialog\"", html, StringComparison.Ordinal);
            Assert.Contains("function openAgentExportDialog", html, StringComparison.Ordinal);
            Assert.Contains("function submitAgentExport", html, StringComparison.Ordinal);
            Assert.Contains("runtimeEnvironment", html, StringComparison.Ordinal);
            Assert.Contains("id=\"agentSaveDialog\"", html, StringComparison.Ordinal);
            Assert.Contains("agentSaveItemCount", html, StringComparison.Ordinal);
            Assert.Contains("id=\"agentDetailDialog\"", html, StringComparison.Ordinal);
            Assert.Contains("function closeAgentDetail", html, StringComparison.Ordinal);
            Assert.Contains("clients/configuration-requests", html, StringComparison.Ordinal);
            Assert.Contains("airControlPlaneTicket", html, StringComparison.Ordinal);
            Assert.Contains("air-control-plane:set-language", html, StringComparison.Ordinal);
            Assert.Contains("hideHeader", html, StringComparison.Ordinal);
            Assert.Contains("captureChineseText", html, StringComparison.Ordinal);

            var executableScript = Regex.Replace(html, @"/\*[\s\S]*?\*/", string.Empty);
            Assert.DoesNotContain("if(lang()===\"zh-CN\"){document.documentElement.lang=\"zh-CN\";document.getElementById(\"languageIcon\").textContent=\"ZH\";return;}", executableScript, StringComparison.Ordinal);
            Assert.Contains("function updateAgentConfigSelector", executableScript, StringComparison.Ordinal);
            Assert.Contains("data-mapping-tab", executableScript, StringComparison.Ordinal);
            Assert.Contains("data-mapping-edit", executableScript, StringComparison.Ordinal);
            Assert.Contains("data-mapping-delete", executableScript, StringComparison.Ordinal);
            Assert.Contains("mappingServiceDialog", html, StringComparison.Ordinal);
            Assert.Contains("function submitMappingServiceDialog", executableScript, StringComparison.Ordinal);
            Assert.DoesNotContain("data-agent-service-index", executableScript, StringComparison.Ordinal);
            Assert.Contains("function openAgentSaveDialog", executableScript, StringComparison.Ordinal);
            Assert.DoesNotContain("openSaveDialog('agent')", executableScript, StringComparison.Ordinal);
            Assert.DoesNotContain("data-disconnect", executableScript, StringComparison.Ordinal);
            Assert.DoesNotContain("Takes effect after save", html, StringComparison.Ordinal);
            Assert.DoesNotContain("保存后运行期生效", html, StringComparison.Ordinal);
            Assert.DoesNotContain("设置该参数的具体取值和行为", html, StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable(tokenVariable, null);
        }
    }

    /// <summary>
    /// <para>zh-cn:验证配置保存接口从 JSON 请求体读取中文变更原因，并将其写入审计记录，避免浏览器自定义 header 的 ByteString 限制。</para>
    /// <para>en-us:Verifies that configuration save reads a Chinese change reason from the JSON body and persists it to audit records, avoiding the browser ByteString limit for custom headers.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "ReverseTunnel")]
    public async Task MapReverseTunnelManagement_should_save_change_reason_from_json_body()
    {
        var tokenVariable = $"AIR_TUNNEL_ADMIN_TOKEN_BODY_{Guid.NewGuid():N}";
        var root = Path.Combine(Path.GetTempPath(), "air-cloud-reverse-tunnel-body-tests", Guid.NewGuid().ToString("N"));
        Environment.SetEnvironmentVariable(tokenVariable, "test-admin-token");
        Directory.CreateDirectory(root);
        try
        {
            var options = new ReverseTunnelOptions
            {
                Enabled = true,
                TunnelMode = ReverseTunnelMode.None,
                Management = new ReverseTunnelManagementOptions
                {
                    Enabled = true,
                    BindUrl = "test-server",
                    AccessTokenEnvironmentVariable = tokenVariable,
                    ConfigDirectory = Path.Combine(root, "configs"),
                    BackupDirectory = Path.Combine(root, "backups"),
                    AllowConfigWrite = true
                }
            };
            options.PostConfigure(options, new ConfigurationBuilder().Build());
            var requestOptions = JsonSerializer.Deserialize<ReverseTunnelOptions>(JsonSerializer.Serialize(options))!;
            requestOptions.Management.BindUrl = "http://0.0.0.0:18643";

            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            builder.Services.AddReverseTunnel();
            builder.Services.AddSingleton<IOptions<ReverseTunnelOptions>>(Options.Create(options));
            using var app = builder.Build();
            app.MapReverseTunnelManagement();
            app.UseRouting();
            app.UseEndpoints(_ => { });
            await app.StartAsync();
            using var client = app.GetTestClient();
            client.DefaultRequestHeaders.Add("X-AirTunnel-Admin-Token", "test-admin-token");
            using var legacyResponse = await client.PutAsJsonAsync("/_airtunnel/admin/config", requestOptions);
            Assert.Equal(HttpStatusCode.BadRequest, legacyResponse.StatusCode);
            using var response = await client.PutAsJsonAsync(
                "/_airtunnel/admin/config",
                new { options = requestOptions, changeReason = "调整中文路由" });
            var responseBody = await response.Content.ReadAsStringAsync();
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"status={(int)response.StatusCode}, body={responseBody}");

            var audit = await app.Services.GetRequiredService<IReverseTunnelConfigurationStore>().ListAuditAsync();
            Assert.Contains(audit, item => item.Reason == "调整中文路由");
        }
        finally
        {
            Environment.SetEnvironmentVariable(tokenVariable, null);
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    /// <summary>
    /// <para>zh-cn:验证管理端可以创建唯一 Agent，并写入标准 Agent 运行配置。</para>
    /// <para>en-us:Verifies that management can create a unique Agent and persist the standard Agent runtime configuration.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "ReverseTunnel")]
    public async Task MapReverseTunnelManagement_should_create_agent_configuration()
    {
        var tokenVariable = $"AIR_TUNNEL_ADMIN_TOKEN_CREATE_{Guid.NewGuid():N}";
        Environment.SetEnvironmentVariable(tokenVariable, "test-admin-token");
        var store = new TestDesiredAgentConfigurationStore();
        try
        {
            var options = new ReverseTunnelOptions
            {
                Enabled = true,
                TunnelMode = ReverseTunnelMode.None,
                Management = new ReverseTunnelManagementOptions
                {
                    Enabled = true,
                    BindUrl = "test-server",
                    AccessTokenEnvironmentVariable = tokenVariable,
                    AllowRemoteClientManagement = true
                }
            };
            options.PostConfigure(options, new ConfigurationBuilder().Build());
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            builder.Services.AddReverseTunnel();
            builder.Services.AddSingleton<IOptions<ReverseTunnelOptions>>(Options.Create(options));
            builder.Services.AddSingleton<IReverseTunnelDesiredAgentConfigurationStore>(store);
            await using var app = builder.Build();
            app.MapReverseTunnelManagement();
            await app.StartAsync();

            using var request = new HttpRequestMessage(HttpMethod.Post, "/_airtunnel/admin/clients");
            request.Headers.Add("X-AirTunnel-Admin-Token", "test-admin-token");
            request.Content = JsonContent.Create(new
            {
                agentId = "agent-orders",
                serverAddress = "https://192.168.100.165:8090",
                agentConnectionCount = 2,
                heartbeatIntervalSeconds = 20,
                configurationSyncEnabled = true,
                configurationSyncIntervalSeconds = 45
            });

            using var response = await app.GetTestClient().SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Contains("agent-orders", body, StringComparison.Ordinal);
            var snapshot = await store.GetAsync("agent-orders");
            Assert.NotNull(snapshot);
            Assert.Equal("https://192.168.100.165:8090", snapshot!.Configuration.ServerAddress);
            Assert.Equal(2, snapshot.Configuration.AgentConnectionCount);
            Assert.Equal(20, snapshot.Configuration.HeartbeatIntervalSeconds);
            Assert.Equal(45, snapshot.Configuration.SessionIdleTimeoutSeconds);
            Assert.Equal(128, snapshot.Configuration.MaxConcurrentStreams);
            Assert.Equal(64 * 1024, snapshot.Configuration.MaxFramePayloadBytes);
            Assert.Equal(1024, snapshot.Configuration.OutboundFrameQueueCapacity);
            Assert.Equal(5, snapshot.Configuration.AgentReconnectInitialDelaySeconds);
            Assert.Equal(60, snapshot.Configuration.AgentReconnectMaxDelaySeconds);
            Assert.NotNull(snapshot.Configuration.ConfigurationSync);
            Assert.NotNull(snapshot.Configuration.Tcp);
            Assert.NotNull(snapshot.Configuration.Udp);
            Assert.True(snapshot.Configuration.AgentCertificate.PrivateKeyPath.EndsWith("agent.key", StringComparison.Ordinal));
        }
        finally
        {
            Environment.SetEnvironmentVariable(tokenVariable, null);
        }
    }

    /// <summary>
    /// <para>zh-cn:验证 Agent 导出包包含运行配置和 CA 公钥，但不会泄露任何私钥材料。</para>
    /// <para>en-us:Verifies that an Agent export contains runtime configuration and the CA public certificate without leaking private-key material.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "ReverseTunnel")]
    public async Task Agent_package_export_should_exclude_private_keys()
    {
        var snapshot = ReverseTunnelAgentConfigurationSnapshotSerializer.Create(
            "agent-export",
            new ReverseTunnelOptions
            {
                Enabled = true,
                TunnelMode = ReverseTunnelMode.Agent,
                AgentId = "agent-export",
                ServerAddress = "https://server:8090",
                AgentCertificate = new ReverseTunnelAgentCertificateOptions
                {
                    CertificatePath = "/app/data/certs/agent.crt",
                    PrivateKeyPath = "/app/data/certs/agent.key",
                    TrustedIssuerPath = "/app/data/certs/ca.crt"
                }
            },
            1);
        var store = new TestDesiredAgentConfigurationStore(snapshot);
        using var authority = TestExportCertificateAuthority.Create();
        var exporter = new ReverseTunnelAgentPackageExporter(store, authority);

        var package = await exporter.CreateAsync(
            "agent-export",
            new ReverseTunnelAgentPackageExportRequest
            {
                ImageName = "airtunnel-agent:1.0.0.0",
                ContainerName = "airtunnel-agent-export",
                RuntimeEnvironment = new Dictionary<string, string>
                {
                    ["ASPNETCORE_URLS"] = "http://+:18543",
                    ["TZ"] = "Asia/Shanghai"
                }
            });
        using var archive = new ZipArchive(new MemoryStream(package.Content), ZipArchiveMode.Read);
        var names = archive.Entries.Select(entry => entry.FullName).ToArray();
        Assert.Contains("config/00-airtunnel-agent.json", names);
        Assert.Contains("data/certs/ca.crt", names);
        Assert.Contains("docker-compose.yml", names);
        Assert.DoesNotContain("docker-compose.client.yml", names);
        Assert.Contains("README.md", names);
        var composeEntry = archive.GetEntry("docker-compose.yml");
        Assert.NotNull(composeEntry);
        using var composeReader = new StreamReader(composeEntry!.Open());
        var compose = await composeReader.ReadToEndAsync();
        Assert.Contains("image: 'airtunnel-agent:1.0.0.0'", compose, StringComparison.Ordinal);
        Assert.Contains("  airtunnel-agent:", compose, StringComparison.Ordinal);
        Assert.Contains("container_name: 'airtunnel-agent-export'", compose, StringComparison.Ordinal);
        Assert.Contains("ASPNETCORE_URLS: 'http://+:18543'", compose, StringComparison.Ordinal);
        Assert.Contains("TZ: 'Asia/Shanghai'", compose, StringComparison.Ordinal);
        Assert.DoesNotContain(names, name => name.Contains("agent.key", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(names, name => name.Contains("agent.crt", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(names, name => name.Contains("ca.pfx", StringComparison.OrdinalIgnoreCase));
        await Assert.ThrowsAsync<ArgumentException>(() => exporter.CreateAsync(
            "agent-export",
            new ReverseTunnelAgentPackageExportRequest
            {
                ImageName = "AirTunnel-Agent:1.0.0.0",
                ContainerName = "airtunnel-agent-export",
                RuntimeEnvironment = new Dictionary<string, string> { ["TZ"] = "Asia/Shanghai" }
            }));
    }

    /// <summary>
    /// <para>zh-cn:验证管理端保存 Agent 配置时要求使用公开路径，由 Server 统一生成内部 ServiceKey。</para>
    /// <para>en-us:Verifies that Agent configuration management requires a public path and lets the Server generate the internal ServiceKey.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "ReverseTunnel")]
    public async Task MapReverseTunnelManagement_should_reject_agent_service_key_conflict()
    {
        var tokenVariable = $"AIR_TUNNEL_ADMIN_TOKEN_CONFLICT_{Guid.NewGuid():N}";
        Environment.SetEnvironmentVariable(tokenVariable, "test-admin-token");
        var existing = ReverseTunnelAgentConfigurationSnapshotSerializer.Create(
            "agent-a",
            new ReverseTunnelOptions
            {
                Enabled = true,
                TunnelMode = ReverseTunnelMode.Agent,
                AgentId = "agent-a",
                LocalServices = [new ReverseTunnelLocalServiceOptions
                {
                    ServiceKey = "orders",
                    LocalAddress = "http://127.0.0.1:6101"
                }]
            },
            1);
        var store = new TestDesiredAgentConfigurationStore(existing);
        try
        {
            var options = new ReverseTunnelOptions
            {
                Enabled = true,
                TunnelMode = ReverseTunnelMode.None,
                Management = new ReverseTunnelManagementOptions
                {
                    Enabled = true,
                    BindUrl = "test-server",
                    AccessTokenEnvironmentVariable = tokenVariable,
                    AllowRemoteClientManagement = true
                }
            };
            options.PostConfigure(options, new ConfigurationBuilder().Build());
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            builder.Services.AddReverseTunnel();
            builder.Services.AddSingleton<IOptions<ReverseTunnelOptions>>(Options.Create(options));
            builder.Services.AddSingleton<IReverseTunnelDesiredAgentConfigurationStore>(store);
            await using var app = builder.Build();
            app.MapReverseTunnelManagement();
            await app.StartAsync();

            using var request = new HttpRequestMessage(HttpMethod.Post, "/_airtunnel/admin/clients/configuration-requests");
            request.Headers.Add("X-AirTunnel-Admin-Token", "test-admin-token");
            request.Content = JsonContent.Create(new
            {
                agentId = "agent-b",
                expectedVersion = 1,
                configuration = new
                {
                    agentId = "agent-b",
                    configuration = new
                    {
                        enabled = true,
                        tunnelMode = "Agent",
                        agentId = "agent-b",
                        localServices = new[] { new { serviceKey = "orders", localAddress = "http://127.0.0.1:6102" } }
                    }
                }
            });

            using var response = await app.GetTestClient().SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("agent_route_configuration_invalid", body, StringComparison.Ordinal);
            Assert.DoesNotContain("agent-b", store.SavedAgentIds);
        }
        finally
        {
            Environment.SetEnvironmentVariable(tokenVariable, null);
        }
    }

    /// <summary>
    /// <para>zh-cn:验证管理端使用旧版本保存 Agent 配置时返回 409，并且不会写入新快照。</para>
    /// <para>en-us:Verifies that saving an Agent configuration with a stale version returns 409 without writing a new snapshot.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "ReverseTunnel")]
    public async Task MapReverseTunnelManagement_should_reject_stale_agent_configuration_version()
    {
        var tokenVariable = $"AIR_TUNNEL_ADMIN_TOKEN_VERSION_{Guid.NewGuid():N}";
        Environment.SetEnvironmentVariable(tokenVariable, "test-admin-token");
        var existing = ReverseTunnelAgentConfigurationSnapshotSerializer.Create(
            "agent-version",
            new ReverseTunnelOptions { Enabled = true, TunnelMode = ReverseTunnelMode.Agent, AgentId = "agent-version" },
            4);
        var store = new TestDesiredAgentConfigurationStore(existing);
        try
        {
            var options = new ReverseTunnelOptions
            {
                Enabled = true,
                Management = new ReverseTunnelManagementOptions
                {
                    Enabled = true,
                    BindUrl = "test-server",
                    AccessTokenEnvironmentVariable = tokenVariable,
                    AllowRemoteClientManagement = true
                }
            };
            options.PostConfigure(options, new ConfigurationBuilder().Build());
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            builder.Services.AddReverseTunnel();
            builder.Services.AddSingleton<IOptions<ReverseTunnelOptions>>(Options.Create(options));
            builder.Services.AddSingleton<IReverseTunnelDesiredAgentConfigurationStore>(store);
            await using var app = builder.Build();
            app.MapReverseTunnelManagement();
            await app.StartAsync();

            using var request = new HttpRequestMessage(HttpMethod.Post, "/_airtunnel/admin/clients/configuration-requests");
            request.Headers.Add("X-AirTunnel-Admin-Token", "test-admin-token");
            request.Content = JsonContent.Create(new
            {
                agentId = "agent-version",
                expectedVersion = 3,
                configuration = new
                {
                    agentId = "agent-version",
                    configuration = new { enabled = true, tunnelMode = "Agent", agentId = "agent-version" }
                }
            });

            using var response = await app.GetTestClient().SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Contains("configuration_version_conflict", body, StringComparison.Ordinal);
            Assert.Empty(store.SavedAgentIds);
        }
        finally
        {
            Environment.SetEnvironmentVariable(tokenVariable, null);
        }
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
            TunnelMode = ReverseTunnelMode.Agent,
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
                TunnelMode = ReverseTunnelMode.Server,
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
            TunnelMode = ReverseTunnelMode.Server,
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

    /// <summary>
    /// <para>zh-cn:验证管理端不再暴露 Enrollment token 生成接口。</para>
    /// <para>en-us:Verifies that the management endpoint no longer exposes enrollment-token generation.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Module", "ReverseTunnel")]
    public async Task Management_should_not_generate_enrollment_token()
    {
        var tokenVariable = $"AIR_TUNNEL_ADMIN_TOKEN_TEST_{Guid.NewGuid():N}";
        Environment.SetEnvironmentVariable(tokenVariable, "test-admin-token");
        using var material = TestCertificateMaterial.Create();
        var enrollmentDirectory = Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(material.CaPfxPath)!, "enrollment-token"));
        try
        {
            var options = new ReverseTunnelOptions
            {
                Enabled = true,
                TunnelMode = ReverseTunnelMode.Server,
                CertificateAuthority = new ReverseTunnelCertificateAuthorityOptions
                {
                    CertificatePath = material.CaPfxPath,
                    CertificatePasswordEnvironmentVariable = material.PasswordEnvironmentVariable
                },
                Enrollment = new ReverseTunnelEnrollmentOptions
                {
                    Enabled = true,
                    CredentialDirectory = enrollmentDirectory.FullName,
                    CredentialFileExtension = ".token",
                    ConsumeOnUse = true
                },
                Management = new ReverseTunnelManagementOptions
                {
                    Enabled = true,
                    BindUrl = "https://0.0.0.0:0",
                    AccessTokenEnvironmentVariable = tokenVariable,
                    AllowRemoteClientManagement = true
                }
            };
            options.PostConfigure(options, new ConfigurationBuilder().Build());

            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            builder.Services.AddReverseTunnel();
            builder.Services.AddSingleton<IOptions<ReverseTunnelOptions>>(Options.Create(options));
            await using var app = builder.Build();
            app.MapReverseTunnelManagement();
            await app.StartAsync();

            using var request = new HttpRequestMessage(HttpMethod.Post, "/_airtunnel/admin/enrollment-tokens");
            request.Headers.Add("X-AirTunnel-Admin-Token", "test-admin-token");
            request.Content = JsonContent.Create(new ReverseTunnelEnrollmentTokenRequest
            {
                AgentId = "agent-bootstrap",
                ServerAddress = "https://airtunnel.example.test:18443",
                ExpiresInHours = 2
            });

            using var response = await app.GetTestClient().SendAsync(request);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Empty(Directory.EnumerateFiles(enrollmentDirectory.FullName));
        }
        finally
        {
            Environment.SetEnvironmentVariable(tokenVariable, null);
        }
    }

    private sealed class TestDesiredAgentConfigurationStore : IReverseTunnelDesiredAgentConfigurationStore
    {
        private readonly Dictionary<string, ReverseTunnelAgentConfigurationSnapshot> snapshots = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> savedAgentIds = [];

        public TestDesiredAgentConfigurationStore(params ReverseTunnelAgentConfigurationSnapshot[] initialSnapshots)
        {
            foreach (var snapshot in initialSnapshots)
            {
                snapshots[snapshot.AgentId] = snapshot;
            }
        }

        public IReadOnlyList<string> SavedAgentIds => savedAgentIds;

        public Task<ReverseTunnelAgentConfigurationSnapshot?> GetAsync(string agentId, CancellationToken cancellationToken = default)
            => Task.FromResult(snapshots.TryGetValue(agentId.Trim(), out var snapshot) ? snapshot : null);

        public Task SaveAsync(ReverseTunnelAgentConfigurationSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            snapshots[snapshot.AgentId] = snapshot;
            savedAgentIds.Add(snapshot.AgentId);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ReverseTunnelAgentConfigurationSnapshot>> ListAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ReverseTunnelAgentConfigurationSnapshot>>(snapshots.Values.ToArray());
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

    private sealed class TestExportCertificateAuthority : IReverseTunnelCertificateAuthority, IDisposable
    {
        private readonly X509Certificate2 certificate;

        private TestExportCertificateAuthority(X509Certificate2 certificate)
        {
            this.certificate = certificate;
        }

        public static TestExportCertificateAuthority Create()
        {
            using var key = RSA.Create(2048);
            var request = new CertificateRequest("CN=AirCloudExportTestCa", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
            var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
            return new TestExportCertificateAuthority(certificate);
        }

        public X509Certificate2 LoadIssuerCertificate()
            => new(certificate.Export(X509ContentType.Pkcs12, "test"), "test", X509KeyStorageFlags.EphemeralKeySet);

        public X509Certificate2 SignAgentCertificate(string agentId, byte[] certificateSigningRequest, DateTimeOffset notBeforeUtc)
            => throw new NotSupportedException();

        public void Dispose() => certificate.Dispose();
    }

    private static int InvokeEndpoint(WebApplication app, string displayName, int? localPort = null, string? adminToken = null)
    {
        var endpoint = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .Single(item => item.DisplayName?.Contains(displayName, StringComparison.OrdinalIgnoreCase) == true);
        var routeEndpoint = Assert.IsType<RouteEndpoint>(endpoint);
        var httpContext = new DefaultHttpContext
        {
            RequestServices = app.Services
        };
        if (localPort is not null)
        {
            httpContext.Connection.LocalPort = localPort.Value;
        }

        if (!string.IsNullOrEmpty(adminToken))
        {
            httpContext.Request.Headers["X-AirTunnel-Admin-Token"] = adminToken;
        }

        httpContext.Response.Body = new MemoryStream();
        routeEndpoint.RequestDelegate!(httpContext).GetAwaiter().GetResult();
        return httpContext.Response.StatusCode;
    }

    private static string InvokeEndpointBody(WebApplication app, string displayName, int localPort, string adminToken)
    {
        var endpoint = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .Single(item => item.DisplayName?.Contains(displayName, StringComparison.OrdinalIgnoreCase) == true);
        var routeEndpoint = Assert.IsType<RouteEndpoint>(endpoint);
        var httpContext = new DefaultHttpContext
        {
            RequestServices = app.Services
        };
        httpContext.Connection.LocalPort = localPort;
        httpContext.Request.Headers["X-AirTunnel-Admin-Token"] = adminToken;
        httpContext.Response.Body = new MemoryStream();
        routeEndpoint.RequestDelegate!(httpContext).GetAwaiter().GetResult();
        Assert.Equal(StatusCodes.Status200OK, httpContext.Response.StatusCode);
        httpContext.Response.Body.Position = 0;
        using var reader = new StreamReader(httpContext.Response.Body);
        return reader.ReadToEnd();
    }
}


