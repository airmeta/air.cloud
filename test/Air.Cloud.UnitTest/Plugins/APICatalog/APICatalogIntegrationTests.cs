using Air.Cloud.Core.App;
using Air.Cloud.Core.Plugins.APIProbe;
using Air.Cloud.Core.Plugins.InternalAccess;
using Air.Cloud.Plugins.APICatalog.Extensions;
using Air.Cloud.Plugins.APICatalog.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Air.Cloud.UnitTest.Plugins.APICatalog;

public class APICatalogIntegrationTests
{
    [Fact]
    public async Task UseAPIProbePlugin_should_require_token_when_enabled()
    {
        var previousRoot = AppCore.RootServices;
        try
        {
            var services = new ServiceCollection();
            services.AddSingleton<IOptions<APIProbeSettingsOptions>>(Options.Create(new APIProbeSettingsOptions
            {
                Enabled = true,
                EnableAuthorized = true,
                RoutePrefix = "api-probe",
                HeaderName = "X-Air-Document-Token",
                AccessTokens = new[] { "valid-token" },
                DefaultProviderName = "FakeProvider"
            }));

            var rootProvider = services.BuildServiceProvider();
            AppCore.RootServices = rootProvider;

            // Build application with root services
            var app = new ApplicationBuilder(rootProvider);
            app.UseAPIProbePlugin();
            var pipeline = app.Build();

            // Create context without token -> expect 401
            var context = CreateContext();
            context.Request.Path = "/api-probe";
            context.Request.Method = HttpMethods.Get;

            await pipeline(context);

            Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);

            // Create context with token -> expect 200 and document
            var context2 = CreateContext();
            context2.Request.Path = "/api-probe";
            context2.Request.Method = HttpMethods.Get;
            context2.Request.Headers["X-Air-Document-Token"] = "valid-token";

            await pipeline(context2);

            Assert.Equal(StatusCodes.Status200OK, context2.Response.StatusCode);
            context2.Response.Body.Seek(0, System.IO.SeekOrigin.Begin);
            var doc = await JsonSerializer.DeserializeAsync<APIProbeResult>(context2.Response.Body, JsonOptions);
            Assert.NotNull(doc);
            Assert.Equal("FakeProvider", doc.ProviderName);
        }
        finally
        {
            AppCore.RootServices = previousRoot;
        }
    }

    [Fact]
    public async Task UseAPIProbePlugin_should_list_providers_and_handle_unknown_path()
    {
        var previousRoot = AppCore.RootServices;
        try
        {
            var services = new ServiceCollection();
            services.AddSingleton<IOptions<APIProbeSettingsOptions>>(Options.Create(new APIProbeSettingsOptions
            {
                Enabled = true,
                EnableAuthorized = false,
                RoutePrefix = "api-probe",
                DefaultProviderName = "FakeProvider"
            }));

            var rootProvider = services.BuildServiceProvider();
            AppCore.RootServices = rootProvider;

            var app = new ApplicationBuilder(rootProvider);
            app.UseAPIProbePlugin();
            var pipeline = app.Build();

            // providers list
            var ctx = CreateContext();
            ctx.Request.Path = "/api-probe/providers";
            ctx.Request.Method = HttpMethods.Get;

            await pipeline(ctx);
            Assert.Equal(StatusCodes.Status200OK, ctx.Response.StatusCode);
            ctx.Response.Body.Seek(0, System.IO.SeekOrigin.Begin);
            var list = await JsonSerializer.DeserializeAsync<string[]>(ctx.Response.Body, JsonOptions);
            Assert.NotNull(list);
            Assert.Contains("FakeProvider", list!);

            // unknown subpath -> 404
            var ctx2 = CreateContext();
            ctx2.Request.Path = "/api-probe/unknown";
            ctx2.Request.Method = HttpMethods.Get;

            await pipeline(ctx2);
            Assert.Equal(StatusCodes.Status404NotFound, ctx2.Response.StatusCode);
        }
        finally
        {
            AppCore.RootServices = previousRoot;
        }
    }

    [Fact]
    public async Task UseAPIProbePlugin_should_accept_internal_access_token()
    {
        var previousRoot = AppCore.RootServices;
        try
        {
            var services = new ServiceCollection();
            services.AddSingleton<IOptions<APIProbeSettingsOptions>>(Options.Create(new APIProbeSettingsOptions
            {
                Enabled = true,
                EnableAuthorized = true,
                EnableInternalAccessToken = true,
                RoutePrefix = "api-probe",
                AccessTokens = Array.Empty<string>(),
                DefaultProviderName = "FakeProvider"
            }));

            var rootProvider = services.BuildServiceProvider();
            AppCore.RootServices = rootProvider;

            var app = new ApplicationBuilder(rootProvider);
            app.UseAPIProbePlugin();
            var pipeline = app.Build();

            var context = CreateContext(new FakeInternalAccessValidPlugin());
            context.Request.Path = "/api-probe";
            context.Request.Method = HttpMethods.Get;
            context.Request.Headers["Launcher"] = "internal-token";

            await pipeline(context);

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        }
        finally
        {
            AppCore.RootServices = previousRoot;
        }
    }

    [Fact]
    public async Task UseAPIProbePlugin_should_accept_configured_query_token_name()
    {
        var previousRoot = AppCore.RootServices;
        try
        {
            var services = new ServiceCollection();
            services.AddSingleton<IOptions<APIProbeSettingsOptions>>(Options.Create(new APIProbeSettingsOptions
            {
                Enabled = true,
                EnableAuthorized = true,
                EnableInternalAccessToken = false,
                RoutePrefix = "api-probe",
                QueryName = "api_catalog_token",
                AccessTokens = new[] { "valid-token" },
                DefaultProviderName = "FakeProvider"
            }));

            var rootProvider = services.BuildServiceProvider();
            AppCore.RootServices = rootProvider;

            var app = new ApplicationBuilder(rootProvider);
            app.UseAPIProbePlugin();
            var pipeline = app.Build();

            var context = CreateContext();
            context.Request.Path = "/api-probe";
            context.Request.Method = HttpMethods.Get;
            context.Request.QueryString = new QueryString("?api_catalog_token=valid-token");

            await pipeline(context);

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        }
        finally
        {
            AppCore.RootServices = previousRoot;
        }
    }

    private static IServiceProvider BuildRequestServices(IInternalAccessValidPlugin? internalAccessValidPlugin = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IAPIProbeProvider>(new FakeProvider());
        if (internalAccessValidPlugin != null) services.AddSingleton(internalAccessValidPlugin);
        return services.BuildServiceProvider();
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static DefaultHttpContext CreateContext(IInternalAccessValidPlugin? internalAccessValidPlugin = null)
    {
        return new DefaultHttpContext
        {
            RequestServices = BuildRequestServices(internalAccessValidPlugin),
            Response =
            {
                Body = new MemoryStream()
            }
        };
    }

    private sealed class FakeProvider : IAPIProbeProvider
    {
        public string ProviderName => "FakeProvider";

        public Task<APIProbeResult> GetDocumentAsync(APIProbeQuery query, CancellationToken cancellationToken = default)
        {
            var result = new APIProbeResult
            {
                ProviderName = ProviderName,
                ApplicationName = "unit-test",
                ApplicationVersion = "1.0",
                GeneratedAt = DateTimeOffset.Now,
                Groups = new List<string> { "Default" },
                Endpoints = new List<APIProbeEndpoint>()
            };

            return Task.FromResult(result);
        }
    }

    private sealed class FakeInternalAccessValidPlugin : IInternalAccessValidPlugin
    {
        public Tuple<string, string> CreateInternalAccessToken()
        {
            return new Tuple<string, string>("Launcher", "internal-token");
        }

        public bool ValidInternalAccessToken(IDictionary<string, string> Headers)
        {
            return Headers.TryGetValue("Launcher", out var token) && token == "internal-token";
        }
    }
}
