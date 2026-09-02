using Air.Cloud.Capabilities.ControlPlaneAuth.Extensions;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Air.Cloud.IntegrationTest.Capabilities.ControlPlaneAuth;

/// <summary>
/// <para>zh-cn:控制面认证 endpoint 集成测试。</para>
/// <para>en-us:Integration tests for control-plane authentication endpoints.</para>
/// </summary>
public sealed class ControlPlaneAuthEndpointIntegrationTests
{
    /// <summary>
    /// <para>zh-cn:验证真实 TestServer 管线可以拆分初始化/登录页和控制台容器页，并完成 Bootstrap 初始化、登录、当前用户查询和 iframe 凭证拼接。</para>
    /// <para>en-us:Verifies that the real TestServer pipeline separates the initialization/sign-in page from the console shell page, and completes bootstrap initialization, sign-in, current-user lookup, and iframe ticket propagation.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Capability", "ControlPlaneAuth")]
    public async Task ControlPlaneAuth_endpoints_should_initialize_login_authorize_and_render_shell()
    {
        var directory = CreateTempDirectory();
        try
        {
            using var host = await new HostBuilder()
                .ConfigureWebHost(webBuilder =>
                {
                    webBuilder.UseTestServer();
                    webBuilder.UseContentRoot(directory);
                    webBuilder.ConfigureAppConfiguration(builder =>
                    {
                        builder.AddInMemoryCollection(new Dictionary<string, string?>
                        {
                            ["ControlPlaneAuth:ProductCode"] = "integration",
                            ["ControlPlaneAuth:ProductName"] = "Integration",
                            ["ControlPlaneAuth:BasePath"] = "/_integration/control-plane",
                            ["ControlPlaneAuth:AuthPath"] = "/_integration/control-auth",
                            ["ControlPlaneAuth:PlanePath"] = "/_integration/control-plane",
                            ["ControlPlaneAuth:DefaultReturnUrl"] = "/_integration/control-plane",
                            ["ControlPlaneAuth:StorageDirectory"] = directory,
                            ["ControlPlaneAuth:Bootstrap:PrintTicketToConsole"] = "false",
                            ["ControlPlaneAuth:Shell:Title"] = "Integration Console",
                            ["ControlPlaneAuth:Shell:DefaultMenuKey"] = "dynamicapp",
                            ["ControlPlaneAuth:Shell:Frames:0:Key"] = "dynamicapp",
                            ["ControlPlaneAuth:Shell:Frames:0:Title"] = "DynamicApp",
                            ["ControlPlaneAuth:Shell:Frames:0:Url"] = "/dynamicapp-panel",
                            ["ControlPlaneAuth:Shell:Frames:0:Embedded"] = "true"
                        });
                    });
                    webBuilder.ConfigureServices((context, services) =>
                    {
                        services.AddRouting();
                        services.AddControlPlaneAuth(context.Configuration.GetSection("ControlPlaneAuth"));
                    });
                    webBuilder.Configure(app =>
                    {
                        app.UseRouting();
                        app.UseControlPlaneAuth();
                        app.UseEndpoints(endpoints => endpoints.MapControlPlaneAuth());
                    });
                })
                .StartAsync();

            var client = host.GetTestClient();
            var authPage = await client.GetAsync("/_integration/control-auth");
            var authPageHtml = await authPage.Content.ReadAsStringAsync();
            var anonymousPlane = await client.GetAsync("/_integration/control-plane");
            var bootstrapTicket = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(directory, "initial-admin-ticket"))).RootElement.GetProperty("ticket").GetString();
            var initialize = await client.PostAsJsonAsync("/_integration/control-plane/api/initialize", new { bootstrapTicket, userName = "admin", password = "admin123" });
            var login = await client.PostAsJsonAsync("/_integration/control-plane/api/login", new { userName = "admin", password = "admin123" });
            var loginJson = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
            var ticket = loginJson.RootElement.GetProperty("ticket").GetString();
            using var planeRequest = new HttpRequestMessage(HttpMethod.Get, "/_integration/control-plane");
            planeRequest.Headers.TryAddWithoutValidation("X-Air-ControlPlane-Ticket", ticket);
            var plane = await client.SendAsync(planeRequest);
            var planeHtml = await plane.Content.ReadAsStringAsync();
            using var meRequest = new HttpRequestMessage(HttpMethod.Get, "/_integration/control-plane/api/me");
            meRequest.Headers.TryAddWithoutValidation("X-Air-ControlPlane-Ticket", ticket);
            var me = await client.SendAsync(meRequest);
            var meJson = JsonDocument.Parse(await me.Content.ReadAsStringAsync());

            Assert.Equal(HttpStatusCode.OK, authPage.StatusCode);
            Assert.Contains("validateInitialPassword", authPageHtml, StringComparison.Ordinal);
            Assert.Contains("密码策略", authPageHtml, StringComparison.Ordinal);
            Assert.Contains("minLength: 8", authPageHtml, StringComparison.Ordinal);
            Assert.DoesNotContain("hostFrame", authPageHtml, StringComparison.Ordinal);
            Assert.Equal(HttpStatusCode.Redirect, anonymousPlane.StatusCode);
            Assert.Contains("/_integration/control-auth", anonymousPlane.Headers.Location?.ToString(), StringComparison.Ordinal);
            Assert.Equal(HttpStatusCode.OK, initialize.StatusCode);
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            Assert.Equal(HttpStatusCode.OK, plane.StatusCode);
            Assert.Contains("/dynamicapp-panel", planeHtml, StringComparison.Ordinal);
            Assert.Contains("frameMenus", planeHtml, StringComparison.Ordinal);
            Assert.Contains("hostFrame", planeHtml, StringComparison.Ordinal);
            Assert.Contains("class=\"user-chip\"", planeHtml, StringComparison.Ordinal);
            Assert.Contains("class=\"language-switch\"", planeHtml, StringComparison.Ordinal);
            Assert.Contains("class=\"icon-button danger\"", planeHtml, StringComparison.Ordinal);
            Assert.Contains("id=\"languageIcon\"", planeHtml, StringComparison.Ordinal);
            Assert.Contains("currentUser:'你好，{0}'", planeHtml, StringComparison.Ordinal);
            Assert.Contains("languageIcon:'EN'", planeHtml, StringComparison.Ordinal);
            Assert.Contains("data-i18n=\"overview\"", planeHtml, StringComparison.Ordinal);
            Assert.Contains("data-i18n=\"serviceTickets\"", planeHtml, StringComparison.Ordinal);
            Assert.Contains("const defaultMenuKey = \"dynamicapp\";", planeHtml, StringComparison.Ordinal);
            Assert.Contains("airControlPlaneTicket", planeHtml, StringComparison.Ordinal);
            Assert.Contains("embedded=1", planeHtml, StringComparison.Ordinal);
            Assert.Contains("hideHeader=1", planeHtml, StringComparison.Ordinal);
            Assert.Contains("data-ticket-action", planeHtml, StringComparison.Ordinal);
            Assert.Contains("air-control-plane:set-language", planeHtml, StringComparison.Ordinal);
            Assert.Contains("air-control-plane:logout", planeHtml, StringComparison.Ordinal);
            Assert.True(
                planeHtml.IndexOf("class=\"language-switch\"", StringComparison.Ordinal) < planeHtml.IndexOf("id=\"currentUser\"", StringComparison.Ordinal) &&
                planeHtml.IndexOf("id=\"currentUser\"", StringComparison.Ordinal) < planeHtml.IndexOf("id=\"logoutButton\"", StringComparison.Ordinal),
                "Top actions should render language switch before current user and logout.");
            Assert.DoesNotContain("<button onclick=\"loadTickets()\">刷新</button>", planeHtml, StringComparison.Ordinal);
            Assert.DoesNotContain("当前用户：", planeHtml, StringComparison.Ordinal);
            Assert.DoesNotContain("__", planeHtml, StringComparison.Ordinal);
            Assert.Equal(HttpStatusCode.OK, me.StatusCode);
            Assert.Equal(ticket, meJson.RootElement.GetProperty("sessionTicket").GetString());
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// <para>zh-cn:验证 TestServer 上的 MFA 绑定 endpoint 和 TOTP 登录链路。</para>
    /// <para>en-us:Verifies the MFA enrollment endpoints and TOTP login flow on TestServer.</para>
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Capability", "ControlPlaneAuth")]
    public async Task ControlPlaneAuth_mfa_endpoints_should_enroll_and_require_totp()
    {
        var directory = CreateTempDirectory();
        try
        {
            using var host = await new HostBuilder()
                .ConfigureWebHost(webBuilder =>
                {
                    webBuilder.UseTestServer();
                    webBuilder.UseContentRoot(directory);
                    webBuilder.ConfigureAppConfiguration(builder =>
                    {
                        builder.AddInMemoryCollection(new Dictionary<string, string?>
                        {
                            ["ControlPlaneAuth:ProductCode"] = "integration-mfa",
                            ["ControlPlaneAuth:ProductName"] = "Integration MFA",
                            ["ControlPlaneAuth:BasePath"] = "/_integration-mfa/control-plane",
                            ["ControlPlaneAuth:AuthPath"] = "/_integration-mfa/control-auth",
                            ["ControlPlaneAuth:PlanePath"] = "/_integration-mfa/control-plane",
                            ["ControlPlaneAuth:StorageDirectory"] = directory,
                            ["ControlPlaneAuth:EnableMFA"] = "true",
                            ["ControlPlaneAuth:MfaIssuer"] = "integration-issuer",
                            ["ControlPlaneAuth:Bootstrap:PrintTicketToConsole"] = "false"
                        });
                    });
                    webBuilder.ConfigureServices((context, services) =>
                    {
                        services.AddRouting();
                        services.AddControlPlaneAuth(context.Configuration.GetSection("ControlPlaneAuth"));
                    });
                    webBuilder.Configure(app =>
                    {
                        app.UseRouting();
                        app.UseControlPlaneAuth();
                        app.UseEndpoints(endpoints => endpoints.MapControlPlaneAuth());
                    });
                })
                .StartAsync();

            var client = host.GetTestClient();
            var authPage = await client.GetAsync("/_integration-mfa/control-auth");
            var authPageHtml = await authPage.Content.ReadAsStringAsync();
            var bootstrapTicket = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(directory, "initial-admin-ticket"))).RootElement.GetProperty("ticket").GetString();
            var initialize = await client.PostAsJsonAsync("/_integration-mfa/control-plane/api/initialize", new { bootstrapTicket, userName = "admin", password = "admin123" });
            var setupRequiredLogin = await client.PostAsJsonAsync("/_integration-mfa/control-plane/api/login", new { userName = "admin", password = "admin123", mfaCode = (string?)null });
            var setupRequiredJson = JsonDocument.Parse(await setupRequiredLogin.Content.ReadAsStringAsync());
            var setup = await client.PostAsJsonAsync("/_integration-mfa/control-plane/api/mfa/setup", new { userName = "admin", password = "admin123" });
            var setupJson = JsonDocument.Parse(await setup.Content.ReadAsStringAsync());
            var provisioningUri = setupJson.RootElement.GetProperty("provisioningUri").GetString()!;
            var secret = ExtractProvisioningSecret(provisioningUri);
            var setupToken = setupJson.RootElement.GetProperty("setupToken").GetString()!;
            var code = CreateTotpCode(secret);
            var confirm = await client.PostAsJsonAsync("/_integration-mfa/control-plane/api/mfa/setup/confirm", new { setupToken, code });
            var missingCodeLogin = await client.PostAsJsonAsync("/_integration-mfa/control-plane/api/login", new { userName = "admin", password = "admin123", mfaCode = (string?)null });
            var missingCodeJson = JsonDocument.Parse(await missingCodeLogin.Content.ReadAsStringAsync());
            var invalidCode = code[0] == '0' ? "1" + code[1..] : "0" + code[1..];
            var invalidCodeLogin = await client.PostAsJsonAsync("/_integration-mfa/control-plane/api/login", new { userName = "admin", password = "admin123", mfaCode = invalidCode });
            var invalidCodeJson = JsonDocument.Parse(await invalidCodeLogin.Content.ReadAsStringAsync());
            var login = await client.PostAsJsonAsync("/_integration-mfa/control-plane/api/login", new { userName = "admin", password = "admin123", mfaCode = code });

            Assert.Equal(HttpStatusCode.OK, authPage.StatusCode);
            Assert.Contains("createSvgTag", authPageHtml, StringComparison.Ordinal);
            Assert.Contains("renderMfaQrCode", authPageHtml, StringComparison.Ordinal);
            Assert.DoesNotContain("id=\"mfaSecret\"", authPageHtml, StringComparison.Ordinal);
            Assert.DoesNotContain("id=\"mfaUri\"", authPageHtml, StringComparison.Ordinal);
            Assert.Equal(HttpStatusCode.OK, initialize.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, setupRequiredLogin.StatusCode);
            Assert.True(setupRequiredJson.RootElement.GetProperty("mfaSetupRequired").GetBoolean());
            Assert.Equal(HttpStatusCode.OK, setup.StatusCode);
            Assert.StartsWith("otpauth://totp/", provisioningUri, StringComparison.Ordinal);
            Assert.Contains("integration-issuer", provisioningUri, StringComparison.Ordinal);
            Assert.False(setupJson.RootElement.TryGetProperty("secret", out _));
            Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, missingCodeLogin.StatusCode);
            Assert.Equal("mfa_required", missingCodeJson.RootElement.GetProperty("errorCode").GetString());
            Assert.Equal(HttpStatusCode.Unauthorized, invalidCodeLogin.StatusCode);
            Assert.Equal("invalid_mfa", invalidCodeJson.RootElement.GetProperty("errorCode").GetString());
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "air-cloud-control-plane-integration-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static string CreateTotpCode(string secret)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var buffer = 0;
        var bits = 0;
        var secretBytes = new List<byte>();
        foreach (var character in secret.TrimEnd('=').ToUpperInvariant())
        {
            buffer = (buffer << 5) | alphabet.IndexOf(character);
            bits += 5;
            if (bits >= 8)
            {
                bits -= 8;
                secretBytes.Add((byte)(buffer >> bits));
            }
        }

        var timeStep = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;
        Span<byte> counter = stackalloc byte[8];
        for (var index = counter.Length - 1; index >= 0; index--)
        {
            counter[index] = (byte)(timeStep & 0xff);
            timeStep >>= 8;
        }

        using var hmac = new HMACSHA1(secretBytes.ToArray());
        var hash = hmac.ComputeHash(counter.ToArray());
        var offset = hash[^1] & 0x0f;
        var binaryCode = ((hash[offset] & 0x7f) << 24)
            | (hash[offset + 1] << 16)
            | (hash[offset + 2] << 8)
            | hash[offset + 3];
        return (binaryCode % 1_000_000).ToString("D6");
    }

    private static string ExtractProvisioningSecret(string provisioningUri)
    {
        var query = new Uri(provisioningUri).Query.TrimStart('?');
        foreach (var part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = part.Split('=', 2);
            if (pair.Length == 2 && string.Equals(pair[0], "secret", StringComparison.OrdinalIgnoreCase))
            {
                return Uri.UnescapeDataString(pair[1]);
            }
        }

        throw new InvalidOperationException("The provisioning URI does not contain a secret.");
    }
}
