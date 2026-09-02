using Air.Cloud.Capabilities.ControlPlaneAuth.Options;
using Air.Cloud.Capabilities.ControlPlaneAuth.Services;
using Air.Cloud.Capabilities.ControlPlaneAuth.Stores;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Air.Cloud.Capabilities.ControlPlaneAuth.UnitTest.ControlPlaneAuth;

/// <summary>
/// <para>zh-cn:控制面认证服务单元测试。</para>
/// <para>en-us:Unit tests for the control-plane authentication service.</para>
/// </summary>
public sealed class ControlPlaneAuthServiceTests
{
    /// <summary>
    /// <para>zh-cn:验证首次启动生成 Bootstrap Ticket，初始化成功后删除明文文件，并允许管理员登录与会话授权。</para>
    /// <para>en-us:Verifies that first startup generates a bootstrap ticket, successful initialization deletes the clear-text file, and administrator login plus session authorization works.</para>
    /// </summary>
    [Fact]
    public async Task Initialize_and_login_should_create_admin_session()
    {
        var directory = CreateTempDirectory();
        try
        {
            var options = CreateOptions(directory);
            var store = new FileControlPlaneAuthStore(new FakeHostEnvironment(directory), new OptionsMonitorStub<ControlPlaneAuthOptions>(options));
            var service = new ControlPlaneAuthService(store, new OptionsMonitorStub<ControlPlaneAuthOptions>(options), NullLogger<ControlPlaneAuthService>.Instance);

            await service.EnsureBootstrapTicketAsync();
            var bootstrapFile = Path.Combine(directory, options.Bootstrap.TicketFileName);
            var bootstrapTicket = JsonDocument.Parse(await File.ReadAllTextAsync(bootstrapFile)).RootElement.GetProperty("ticket").GetString()!;

            var initialized = await service.InitializeAsync(bootstrapTicket, "admin", "admin123");
            var login = await service.LoginAsync("admin", "admin123");
            var context = new DefaultHttpContext();
            context.Request.Headers[options.Session.HeaderName] = login.Ticket;
            var authorized = await service.AuthorizeAsync(context);

            Assert.True(initialized.Succeeded);
            Assert.False(File.Exists(bootstrapFile));
            Assert.True(login.Succeeded);
            Assert.False(string.IsNullOrWhiteSpace(login.Ticket));
            Assert.True(authorized.Succeeded);
            Assert.Equal("admin", authorized.User!.UserName);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// <para>zh-cn:验证服务间票据创建后只返回一次明文，持久化列表不包含可直接使用的票据值。</para>
    /// <para>en-us:Verifies that creating a service-to-service ticket returns clear text only once and the persisted list does not contain a directly usable ticket value.</para>
    /// </summary>
    [Fact]
    public async Task CreateServiceTicket_should_persist_hash_without_plain_text()
    {
        var directory = CreateTempDirectory();
        try
        {
            var options = CreateOptions(directory);
            var store = new FileControlPlaneAuthStore(new FakeHostEnvironment(directory), new OptionsMonitorStub<ControlPlaneAuthOptions>(options));
            var service = new ControlPlaneAuthService(store, new OptionsMonitorStub<ControlPlaneAuthOptions>(options), NullLogger<ControlPlaneAuthService>.Instance);

            var created = await service.CreateServiceTicketAsync("airtunnel-gateway", "gateway-token");
            var tickets = await service.ReadServiceTicketsAsync();

            Assert.False(string.IsNullOrWhiteSpace(created.PlainTextTicket));
            Assert.Single(tickets);
            Assert.Equal("airtunnel-gateway", tickets[0].Type);
            Assert.DoesNotContain(created.PlainTextTicket, await File.ReadAllTextAsync(Path.Combine(directory, "service-tickets.json")));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// <para>zh-cn:验证控制面会话 Ticket 可以通过受限的同站查询参数传入，用于 iframe 承载页把登录态桥接给宿主产品面板。</para>
    /// <para>en-us:Verifies that the control-plane session ticket can be supplied through the restricted same-site query parameter, allowing iframe-hosted pages to bridge sign-in state to the host product panel.</para>
    /// </summary>
    [Fact]
    public async Task AuthorizeAsync_should_accept_configured_query_ticket()
    {
        var directory = CreateTempDirectory();
        try
        {
            var options = CreateOptions(directory);
            var store = new FileControlPlaneAuthStore(new FakeHostEnvironment(directory), new OptionsMonitorStub<ControlPlaneAuthOptions>(options));
            var service = new ControlPlaneAuthService(store, new OptionsMonitorStub<ControlPlaneAuthOptions>(options), NullLogger<ControlPlaneAuthService>.Instance);

            await service.EnsureBootstrapTicketAsync();
            var bootstrapTicket = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(directory, options.Bootstrap.TicketFileName))).RootElement.GetProperty("ticket").GetString()!;
            await service.InitializeAsync(bootstrapTicket, "admin", "admin123");
            var login = await service.LoginAsync("admin", "admin123");
            var context = new DefaultHttpContext();
            context.Request.QueryString = new QueryString("?" + options.Session.QueryName + "=" + Uri.EscapeDataString(login.Ticket!));

            var authorized = await service.AuthorizeAsync(context);

            Assert.True(authorized.Succeeded);
            Assert.Equal("admin", authorized.User!.UserName);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// <para>zh-cn:验证请求头里存在失效旧 Ticket 时，认证服务仍会继续尝试 Cookie 中的有效会话，避免控制台页面在 /api/me 401 与登录页之间循环跳转。</para>
    /// <para>en-us:Verifies that the authentication service still tries the valid cookie session when the request header contains a stale ticket, preventing console pages from looping between /api/me 401 and the sign-in page.</para>
    /// </summary>
    [Fact]
    public async Task AuthorizeAsync_should_fallback_to_cookie_when_header_ticket_is_stale()
    {
        var directory = CreateTempDirectory();
        try
        {
            var options = CreateOptions(directory);
            var store = new FileControlPlaneAuthStore(new FakeHostEnvironment(directory), new OptionsMonitorStub<ControlPlaneAuthOptions>(options));
            var service = new ControlPlaneAuthService(store, new OptionsMonitorStub<ControlPlaneAuthOptions>(options), NullLogger<ControlPlaneAuthService>.Instance);

            await service.EnsureBootstrapTicketAsync();
            var bootstrapTicket = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(directory, options.Bootstrap.TicketFileName))).RootElement.GetProperty("ticket").GetString()!;
            await service.InitializeAsync(bootstrapTicket, "admin", "admin123");
            var login = await service.LoginAsync("admin", "admin123");
            var context = new DefaultHttpContext();
            context.Request.Headers[options.Session.HeaderName] = "stale-ticket";
            context.Request.Headers.Cookie = options.Session.CookieName + "=" + login.Ticket;

            var authorized = await service.AuthorizeAsync(context);

            Assert.True(authorized.Succeeded);
            Assert.Equal("admin", authorized.User!.UserName);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// <para>zh-cn:验证启用 MFA 后必须完成 Authenticator 绑定，登录需要 TOTP，且同一时间片验证码不能重复使用。</para>
    /// <para>en-us:Verifies that MFA enrollment is mandatory when enabled, login requires TOTP, and the same time-step code cannot be reused.</para>
    /// </summary>
    [Fact]
    public async Task EnableMfa_should_require_enrollment_and_totp_login()
    {
        var directory = CreateTempDirectory();
        try
        {
            var options = CreateOptions(directory);
            options.EnableMFA = true;
            var store = new FileControlPlaneAuthStore(new FakeHostEnvironment(directory), new OptionsMonitorStub<ControlPlaneAuthOptions>(options));
            var service = new ControlPlaneAuthService(store, new OptionsMonitorStub<ControlPlaneAuthOptions>(options), NullLogger<ControlPlaneAuthService>.Instance);

            await service.EnsureBootstrapTicketAsync();
            var bootstrapTicket = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(directory, options.Bootstrap.TicketFileName))).RootElement.GetProperty("ticket").GetString()!;
            Assert.True((await service.InitializeAsync(bootstrapTicket, "admin", "admin123")).Succeeded);

            var setupRequired = await service.LoginAsync("admin", "admin123");
            var setup = await service.BeginMfaSetupAsync("admin", "admin123");
            var code = CreateTotpCode(setup.Secret!);
            var completed = await service.CompleteMfaSetupAsync(setup.SetupToken!, code);
            var missingCode = await service.LoginAsync("admin", "admin123");
            var login = await service.LoginAsync("admin", "admin123", code);
            var replay = await service.LoginAsync("admin", "admin123", code);
            var usersJson = await File.ReadAllTextAsync(Path.Combine(directory, "users.json"));

            Assert.False(setupRequired.Succeeded);
            Assert.True(setupRequired.MfaSetupRequired);
            Assert.True(setup.Succeeded);
            Assert.StartsWith("otpauth://totp/", setup.ProvisioningUri, StringComparison.Ordinal);
            Assert.Contains("issuer=airForge", setup.ProvisioningUri, StringComparison.Ordinal);
            Assert.True(completed.Succeeded);
            Assert.False(missingCode.Succeeded);
            Assert.Equal("mfa_required", missingCode.ErrorCode);
            Assert.True(login.Succeeded);
            Assert.False(replay.Succeeded);
            Assert.Equal("invalid_mfa", replay.ErrorCode);
            Assert.DoesNotContain(setup.Secret!, usersJson, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// <para>zh-cn:验证服务器侧 MFA 应急重置会清除绑定状态，并要求用户重新绑定 Authenticator。</para>
    /// <para>en-us:Verifies that a server-side MFA emergency reset clears enrollment and requires the user to enroll again.</para>
    /// </summary>
    [Fact]
    public async Task ResetMfa_should_require_reenrollment()
    {
        var directory = CreateTempDirectory();
        try
        {
            var options = CreateOptions(directory);
            options.EnableMFA = true;
            var store = new FileControlPlaneAuthStore(new FakeHostEnvironment(directory), new OptionsMonitorStub<ControlPlaneAuthOptions>(options));
            var service = new ControlPlaneAuthService(store, new OptionsMonitorStub<ControlPlaneAuthOptions>(options), NullLogger<ControlPlaneAuthService>.Instance);

            await service.EnsureBootstrapTicketAsync();
            var bootstrapTicket = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(directory, options.Bootstrap.TicketFileName))).RootElement.GetProperty("ticket").GetString()!;
            await service.InitializeAsync(bootstrapTicket, "admin", "admin123");
            var setup = await service.BeginMfaSetupAsync("admin", "admin123");
            await service.CompleteMfaSetupAsync(setup.SetupToken!, CreateTotpCode(setup.Secret!));

            var reset = await service.ResetMfaAsync("admin");
            var login = await service.LoginAsync("admin", "admin123");

            Assert.True(reset.Succeeded);
            Assert.False(login.Succeeded);
            Assert.True(login.MfaSetupRequired);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static ControlPlaneAuthOptions CreateOptions(string directory)
    {
        return new ControlPlaneAuthOptions
        {
            ProductCode = "unit",
            ProductName = "Unit",
            BasePath = "/_unit",
            StorageDirectory = directory,
            Bootstrap = new ControlPlaneBootstrapOptions
            {
                PrintTicketToConsole = false,
                WriteTicketFile = true
            },
            PasswordPolicy = new ControlPlanePasswordPolicyOptions
            {
                MinLength = 8,
                RequireDigit = true,
                RequireLowercase = true
            }
        };
    }

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "air-cloud-control-plane-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static string CreateTotpCode(string secret)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var normalized = secret.TrimEnd('=').ToUpperInvariant();
        var buffer = 0;
        var bits = 0;
        var secretBytes = new List<byte>();
        foreach (var character in normalized)
        {
            buffer = (buffer << 5) | alphabet.IndexOf(character);
            bits += 5;
            if (bits >= 8)
            {
                bits -= 8;
                secretBytes.Add((byte)(buffer >> bits));
                buffer = bits == 0 ? 0 : buffer & ((1 << bits) - 1);
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

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public FakeHostEnvironment(string contentRootPath)
        {
            ContentRootPath = contentRootPath;
        }

        public string EnvironmentName { get; set; } = Environments.Development;

        public string ApplicationName { get; set; } = "Air.Cloud.UnitTest";

        public string ContentRootPath { get; set; }

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class OptionsMonitorStub<T> : IOptionsMonitor<T>
    {
        public OptionsMonitorStub(T value)
        {
            CurrentValue = value;
        }

        public T CurrentValue { get; }

        public T Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
