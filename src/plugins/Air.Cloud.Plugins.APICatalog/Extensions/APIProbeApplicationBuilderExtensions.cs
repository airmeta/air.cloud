using Air.Cloud.Core;
using Air.Cloud.Core.Plugins.InternalAccess;
using Air.Cloud.Plugins.APICatalog.Options;

using Microsoft.AspNetCore.Http;

using System.Security.Cryptography;
using System.Text;

namespace Air.Cloud.Plugins.APICatalog.Extensions;

[IgnoreScanning]
public static class APIProbeApplicationBuilderExtensions
{
    public static IApplicationBuilder UseAPIProbePlugin(this IApplicationBuilder app)
    {
        var options = AppCore.GetOptions<APIProbeSettingsOptions>();
        if (options.Enabled != true) return app;

        var routePrefix = "/" + options.RoutePrefix.Trim('/');
        app.Map(new PathString(routePrefix), branch =>
        {
            branch.Run(async context =>
            {
                if (!HttpMethods.IsGet(context.Request.Method))
                {
                    context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                    return;
                }

                if (!IsAuthorized(context, options))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        Message = "Unauthorized APIProbe request."
                    });
                    return;
                }

                var providers = context.RequestServices.GetServices<IAPIProbeProvider>().ToArray();
                var path = context.Request.Path.Value?.Trim('/') ?? string.Empty;
                if (string.Equals(path, "providers", StringComparison.OrdinalIgnoreCase))
                {
                    await context.Response.WriteAsJsonAsync(providers.Select(provider => provider.ProviderName));
                    return;
                }

                if (!string.IsNullOrWhiteSpace(path))
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }

                if (providers.Length == 0)
                {
                    context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        Message = "No APIProbe provider has been registered."
                    });
                    return;
                }

                var providerName = context.Request.Query["provider"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(providerName)) providerName = options.DefaultProviderName;

                var provider = providers.FirstOrDefault(item => string.Equals(item.ProviderName, providerName, StringComparison.OrdinalIgnoreCase))
                    ?? providers.First();

                var includeSchemasText = context.Request.Query["includeSchemas"].FirstOrDefault();
                var query = new APIProbeQuery
                {
                    Group = context.Request.Query["group"].FirstOrDefault(),
                    IncludeSchemas = !bool.TryParse(includeSchemasText, out var includeSchemas) || includeSchemas
                };

                var document = await provider.GetDocumentAsync(query, context.RequestAborted);
                await context.Response.WriteAsJsonAsync(document, context.RequestAborted);
            });
        });

        return app;
    }

    private static bool IsAuthorized(HttpContext context, APIProbeSettingsOptions options)
    {
        if (options.EnableAuthorized != true) return true;
        if (IsAccessTokenAuthorized(context, options)) return true;

        return IsInternalAccessTokenAuthorized(context, options);
    }

    private static bool IsAccessTokenAuthorized(HttpContext context, APIProbeSettingsOptions options)
    {
        if (options.AccessTokens == null || options.AccessTokens.Length == 0) return false;
        
        var tokens = ReadRequestTokens(context, options).Where(token => !string.IsNullOrWhiteSpace(token)).ToArray();
        if (tokens.Length == 0) return false;

        return tokens.Any(requestToken => options.AccessTokens.Any(allowedToken => FixedTimeEquals(requestToken, allowedToken)));
    }

    private static bool IsInternalAccessTokenAuthorized(HttpContext context, APIProbeSettingsOptions options)
    {
        if (options.EnableInternalAccessToken != true) return false;

        var internalAccessValidPlugin = GetInternalAccessValidPlugin(context);
        if (internalAccessValidPlugin == null) return false;

        return internalAccessValidPlugin.ValidInternalAccessToken(ReadRequestHeaders(context));
    }

    private static IInternalAccessValidPlugin GetInternalAccessValidPlugin(HttpContext context)
    {
        var plugin = context.RequestServices.GetService<IInternalAccessValidPlugin>();
        if (plugin != null) return plugin;

        try
        {
            return AppRealization.AppPlugin?.GetPlugin<IInternalAccessValidPlugin>();
        }
        catch (KeyNotFoundException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static IDictionary<string, string> ReadRequestHeaders(HttpContext context)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in context.Request.Headers)
        {
            var value = header.Value.ToString();
            if (!string.IsNullOrWhiteSpace(value)) headers[header.Key] = value;
        }

        return headers;
    }

    private static IEnumerable<string> ReadRequestTokens(HttpContext context, APIProbeSettingsOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.HeaderName))
        {
            foreach (var headerToken in context.Request.Headers[options.HeaderName])
            {
                yield return headerToken;
            }
        }

        foreach (var authorization in context.Request.Headers.Authorization)
        {
            const string bearerPrefix = "Bearer ";
            yield return authorization.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase)
                ? authorization[bearerPrefix.Length..]
                : authorization;
        }

        if (string.IsNullOrWhiteSpace(options.QueryName)) yield break;

        foreach (var accessToken in context.Request.Query[options.QueryName])
        {
            yield return accessToken;
        }
    }

    private static bool FixedTimeEquals(string value, string expected)
    {
        if (value == null || expected == null) return false;

        var valueBytes = Encoding.UTF8.GetBytes(value);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        return valueBytes.Length == expectedBytes.Length && CryptographicOperations.FixedTimeEquals(valueBytes, expectedBytes);
    }
}
