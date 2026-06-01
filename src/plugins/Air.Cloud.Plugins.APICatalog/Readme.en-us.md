# Air.Cloud.Plugins.APICatalog

APICatalog is an API metadata export plugin for Air.Cloud applications. It exposes a normalized JSON document through APIProbe so that external tools can read route, request, response, schema, and authorization metadata.

The default provider is `ApiExplorer`, which reads metadata from ASP.NET Core ApiExplorer.

## Features

- Export API groups and endpoint metadata.
- Export request parameters, request body schema, response schema, and content types.
- Export `[Authorize]` / `[AllowAnonymous]` metadata.
- Protect the document endpoint with configured access tokens.
- Optionally accept Air.Cloud internal access tokens such as Taxin `Launcher` through `IInternalAccessValidPlugin`.
- Allow custom API document providers through `IAPIProbeProvider`.

## Install

Reference the plugin project or package from the target Web application.

```xml
<ProjectReference Include="..\..\src\plugins\Air.Cloud.Plugins.APICatalog\Air.Cloud.Plugins.APICatalog.csproj" />
```

If the Air.Cloud startup scanner loads plugin startup classes, APICatalog is registered automatically by `Air.Cloud.Plugins.APICatalog.Startup`.

If you need to register it manually, add:

```csharp
using Air.Cloud.Plugins.APICatalog.Extensions;

public void ConfigureServices(IServiceCollection services)
{
    services.AddAPIProbe();
}

public void Configure(IApplicationBuilder app)
{
    app.UseAPIProbePlugin();
}
```

## Configuration

Add `APIProbeSettings` to `appsettings.json` or `appsettings.Development.json`.

```json
{
  "APIProbeSettings": {
    "Enabled": true,
    "EnableAuthorized": true,
    "EnableInternalAccessToken": true,
    "RoutePrefix": "api-probe",
    "HeaderName": "X-Air-Document-Token",
    "QueryName": "access_token",
    "AccessTokens": [
      "replace-with-a-strong-random-token"
    ],
    "DefaultProviderName": "ApiExplorer"
  }
}
```

| Key | Default | Description |
| --- | --- | --- |
| `Enabled` | `true` in non-production, `false` in production | Enables the `/api-probe` endpoint. |
| `EnableAuthorized` | `true` | Requires a valid token before returning API metadata. |
| `EnableInternalAccessToken` | `true` | Allows `IInternalAccessValidPlugin` tokens, such as Taxin `Launcher`, as an alternative auth path. |
| `RoutePrefix` | `api-probe` | Endpoint prefix. With the default value, the document URL is `/api-probe`. |
| `HeaderName` | `X-Air-Document-Token` | Header name used for direct APICatalog access tokens. |
| `QueryName` | `access_token` | Query string key used for direct APICatalog access tokens. |
| `AccessTokens` | `[]` | Allowed direct APICatalog access tokens. |
| `DefaultProviderName` | `ApiExplorer` | Provider used when the request does not specify `provider`. |

When `EnableAuthorized` is `true`, APICatalog accepts any one of these credentials:

- Header: `X-Air-Document-Token: <token>` or the configured `HeaderName`.
- Header: `Authorization: Bearer <token>`.
- Query string: `?access_token=<token>` or the configured `QueryName`.
- Internal access plugin token verified by `IInternalAccessValidPlugin`, for example `Launcher`.

## Access Token

APICatalog does not issue or sign access tokens. `AccessTokens` is a server-side allow list. Generate a strong random value, store it in configuration, and send the same value from Postman or an external integration.

PowerShell example:

```powershell
[Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
```

Example configuration:

```json
{
  "APIProbeSettings": {
    "HeaderName": "X-Air-Document-Token",
    "QueryName": "api_catalog_token",
    "AccessTokens": [
      "a-generated-strong-token"
    ]
  }
}
```

For production usage, prefer a header or `Authorization: Bearer` over query string tokens because query strings are more likely to appear in logs.

To rotate tokens, temporarily configure both the old and new values:

```json
{
  "APIProbeSettings": {
    "AccessTokens": [
      "old-token",
      "new-token"
    ]
  }
}
```

Remove the old value after all clients have switched.

## Internal Access Token

Taxin currently sends an internal request header created by `IInternalAccessValidPlugin`. The common implementation uses the `Launcher` header.

APICatalog can accept this token path when:

- `EnableAuthorized` is `true`.
- `EnableInternalAccessToken` is `true`.
- An `IInternalAccessValidPlugin` implementation is available from DI or the Air.Cloud plugin factory.
- The plugin validates the request headers successfully.

Example implementation:

```csharp
using Air.Cloud.Core;
using Air.Cloud.Core.Plugins.InternalAccess;
using Air.Cloud.Core.Plugins.Security.RSA;

public class InternalAccessValidPlugin : IInternalAccessValidPlugin
{
    public Tuple<string, string> CreateInternalAccessToken()
    {
        var token = RsaEncryption.Encrypt(
            AppRealization.PID.Get(),
            RsaKeyConst.PUBLIC_KEY,
            RsaKeyConst.PRIVATE_KEY);

        return new Tuple<string, string>("Launcher", token);
    }

    public bool ValidInternalAccessToken(IDictionary<string, string> headers)
    {
        if (!headers.TryGetValue("Launcher", out var value)) return false;
        if (string.IsNullOrWhiteSpace(value)) return false;

        try
        {
            var decrypted = RsaEncryption.Decrypt(
                value,
                RsaKeyConst.PUBLIC_KEY,
                RsaKeyConst.PRIVATE_KEY);

            return !string.IsNullOrWhiteSpace(decrypted);
        }
        catch
        {
            return false;
        }
    }
}
```

If your system does not use `Launcher`, return a different header name from `CreateInternalAccessToken()` and validate that header in `ValidInternalAccessToken()`.

## Custom APIProbe Provider

The default provider is `ApiExplorer`. To export metadata from another source, implement `IAPIProbeProvider`.

```csharp
using Air.Cloud.Core.Plugins.APIProbe;

public sealed class CustomAPIProbeProvider : IAPIProbeProvider
{
    public string ProviderName => "Custom";

    public Task<APIProbeResult> GetDocumentAsync(
        APIProbeQuery query,
        CancellationToken cancellationToken = default)
    {
        var result = new APIProbeResult
        {
            ProviderName = ProviderName,
            ApplicationName = "my-service",
            ApplicationVersion = "1.0.0",
            Groups = new List<string> { "Default" },
            Endpoints = new List<APIProbeEndpoint>()
        };

        return Task.FromResult(result);
    }
}
```

Register the provider:

```csharp
services.AddSingleton<IAPIProbeProvider, CustomAPIProbeProvider>();
```

Then request it with:

```text
GET /api-probe?provider=Custom
```

If `provider` is omitted, APICatalog uses `DefaultProviderName`. If the configured provider is not found, it falls back to the first registered provider.

## Postman Usage

Assume the service is running at:

```text
http://localhost:5220
```

### 1. Get the API document with a header token

Create a request in Postman:

```text
GET http://localhost:5220/api-probe
```

Headers:

```text
X-Air-Document-Token: a-generated-strong-token
```

Send the request. A successful response returns HTTP `200` with an `APIProbeResult` JSON document.

### 2. Get the API document with Bearer token

```text
GET http://localhost:5220/api-probe
```

Headers:

```text
Authorization: Bearer a-generated-strong-token
```

### 3. Get the API document with query token

Default query key:

```text
GET http://localhost:5220/api-probe?access_token=a-generated-strong-token
```

Custom query key example:

```json
{
  "APIProbeSettings": {
    "QueryName": "api_catalog_token"
  }
}
```

Postman URL:

```text
GET http://localhost:5220/api-probe?api_catalog_token=a-generated-strong-token
```

### 4. Filter a group

```text
GET http://localhost:5220/api-probe?group=Default
```

Add the token by header, Bearer token, or configured query key.

### 5. Disable schema output

```text
GET http://localhost:5220/api-probe?includeSchemas=false
```

This keeps endpoint and parameter metadata but omits detailed schema expansion.

### 6. Select a provider

```text
GET http://localhost:5220/api-probe?provider=ApiExplorer
```

### 7. List providers

```text
GET http://localhost:5220/api-probe/providers
```

This returns a string array of registered provider names.

## Response Shape

The document endpoint returns:

```json
{
  "providerName": "ApiExplorer",
  "applicationName": "service-name",
  "applicationVersion": "1.0.0",
  "generatedAt": "2026-05-30T00:00:00+08:00",
  "groups": [
    "Default"
  ],
  "endpoints": [
    {
      "id": "GET:/v1/example",
      "group": "Default",
      "tag": "Example",
      "name": "Query",
      "method": "GET",
      "path": "/v1/example",
      "summary": "Example API",
      "description": "Example API",
      "isAllowAnonymous": false,
      "requiresAuthorization": true,
      "authorizeDatas": [],
      "request": {
        "contentTypes": [],
        "parameters": [],
        "body": null
      },
      "responses": []
    }
  ]
}
```

## Troubleshooting

`401 Unauthorized`

- `EnableAuthorized` is enabled and no valid token was provided.
- `AccessTokens` is empty.
- The request uses a custom header or query key but `HeaderName` or `QueryName` was not configured to match.
- `Launcher` or another internal token was sent, but `IInternalAccessValidPlugin` is missing or returned `false`.

`404 Not Found`

- The request path under the route prefix is not supported. Valid paths are `/api-probe` and `/api-probe/providers` by default.

`405 Method Not Allowed`

- APICatalog only supports `GET`.

`503 Service Unavailable`

- No `IAPIProbeProvider` was registered.

No endpoint data

- Ensure MVC endpoint metadata is available to ApiExplorer.
- Ensure the application has called `services.AddEndpointsApiExplorer()` or `services.AddAPIProbe()`.
- Check whether `group` filters out all endpoints.
