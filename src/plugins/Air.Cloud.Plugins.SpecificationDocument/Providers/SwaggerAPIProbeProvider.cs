using Air.Cloud.Core.Plugins.APIProbe;
using Air.Cloud.Plugins.SpecificationDocument.Builders;

using Microsoft.OpenApi.Models;

using Swashbuckle.AspNetCore.Swagger;

namespace Air.Cloud.Plugins.SpecificationDocument.Providers;

public sealed class SwaggerAPIProbeProvider : IAPIProbeProvider
{
    private const int MaxSchemaDepth = 5;
    private readonly ISwaggerProvider _swaggerProvider;
    private readonly AppSettingsOptions _appSettings;

    public SwaggerAPIProbeProvider(ISwaggerProvider swaggerProvider)
    {
        _swaggerProvider = swaggerProvider;
        _appSettings = AppCore.Settings;
    }

    public string ProviderName => "Swagger";

    public Task<APIProbeResult> GetDocumentAsync(APIProbeQuery query, CancellationToken cancellationToken = default)
    {
        var groups = string.IsNullOrWhiteSpace(query?.Group)
            ? SpecificationDocumentBuilder.DocumentGroups.ToArray()
            : new[] { query.Group };

        var endpoints = new List<APIProbeEndpoint>();
        foreach (var group in groups)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var swaggerDocument = _swaggerProvider.GetSwagger(group);
            foreach (var path in swaggerDocument.Paths)
            {
                foreach (var operation in path.Value.Operations)
                {
                    endpoints.Add(CreateEndpoint(group, path.Key, operation.Key, operation.Value, swaggerDocument, query?.IncludeSchemas != false));
                }
            }
        }

        var result = new APIProbeResult
        {
            ProviderName = ProviderName,
            ApplicationName = AppConst.ApplicationName,
            ApplicationVersion = _appSettings.Version,
            GeneratedAt = DateTimeOffset.Now,
            Groups = groups.ToList(),
            Endpoints = endpoints
                .OrderBy(item => item.Group)
                .ThenBy(item => item.Path)
                .ThenBy(item => item.Method)
                .ToList()
        };

        return Task.FromResult(result);
    }

    private static APIProbeEndpoint CreateEndpoint(
        string group,
        string path,
        OperationType operationType,
        OpenApiOperation operation,
        OpenApiDocument swaggerDocument,
        bool includeSchemas)
    {
        var method = operationType.ToString().ToUpperInvariant();
        return new APIProbeEndpoint
        {
            Id = operation.OperationId ?? $"{method}:{path}",
            Group = group,
            Tag = operation.Tags?.FirstOrDefault()?.Name,
            Name = operation.OperationId,
            Method = method,
            Path = path,
            Summary = operation.Summary,
            Description = operation.Description,
            RequiresAuthorization = HasSecurityRequirement(operation, swaggerDocument),
            IsAllowAnonymous = !HasSecurityRequirement(operation, swaggerDocument),
            Request = CreateRequest(operation, swaggerDocument, includeSchemas),
            Responses = CreateResponses(operation, swaggerDocument, includeSchemas)
        };
    }

    private static APIProbeRequest CreateRequest(OpenApiOperation operation, OpenApiDocument swaggerDocument, bool includeSchemas)
    {
        var request = new APIProbeRequest();

        foreach (var parameter in operation.Parameters ?? Enumerable.Empty<OpenApiParameter>())
        {
            request.Parameters.Add(new APIProbeParameter
            {
                Name = parameter.Name,
                Source = parameter.In?.ToString(),
                Required = parameter.Required,
                Description = parameter.Description,
                Schema = includeSchemas ? CreateSchema(parameter.Schema, swaggerDocument) : null
            });
        }

        if (operation.RequestBody == null) return request;

        request.ContentTypes = operation.RequestBody.Content.Keys.ToList();
        var bodySchema = operation.RequestBody.Content.Values.FirstOrDefault()?.Schema;
        request.Body = includeSchemas ? CreateSchema(bodySchema, swaggerDocument) : null;

        return request;
    }

    private static bool HasSecurityRequirement(OpenApiOperation operation, OpenApiDocument swaggerDocument)
    {
        return operation.Security?.Count > 0 || swaggerDocument.SecurityRequirements?.Count > 0;
    }

    private static IList<APIProbeResponse> CreateResponses(OpenApiOperation operation, OpenApiDocument swaggerDocument, bool includeSchemas)
    {
        return operation.Responses.Select(response => new APIProbeResponse
        {
            StatusCode = int.TryParse(response.Key, out var statusCode) ? statusCode : 0,
            Description = response.Value.Description,
            ContentTypes = response.Value.Content.Keys.ToList(),
            Body = includeSchemas ? CreateSchema(response.Value.Content.Values.FirstOrDefault()?.Schema, swaggerDocument) : null
        }).ToList();
    }

    private static APIProbeSchema CreateSchema(OpenApiSchema schema, OpenApiDocument swaggerDocument)
    {
        return CreateSchema(schema, swaggerDocument, new HashSet<string>(StringComparer.OrdinalIgnoreCase), 0);
    }

    private static APIProbeSchema CreateSchema(
        OpenApiSchema schema,
        OpenApiDocument swaggerDocument,
        HashSet<string> visitedReferences,
        int depth)
    {
        if (schema == null) return null;

        var referenceId = schema.Reference?.Id;
        if (!string.IsNullOrWhiteSpace(referenceId))
        {
            if (visitedReferences.Contains(referenceId) || depth >= MaxSchemaDepth)
            {
                return new APIProbeSchema
                {
                    Type = "object",
                    TypeName = referenceId,
                    Nullable = schema.Nullable
                };
            }

            if (swaggerDocument.Components?.Schemas != null && swaggerDocument.Components.Schemas.TryGetValue(referenceId, out var referenceSchema))
            {
                visitedReferences.Add(referenceId);
                var resolvedSchema = CreateSchema(referenceSchema, swaggerDocument, visitedReferences, depth + 1);
                visitedReferences.Remove(referenceId);
                if (resolvedSchema != null && string.IsNullOrWhiteSpace(resolvedSchema.TypeName)) resolvedSchema.TypeName = referenceId;
                return resolvedSchema;
            }
        }

        var realSchema = schema.AllOf?.FirstOrDefault()
            ?? schema.OneOf?.FirstOrDefault()
            ?? schema.AnyOf?.FirstOrDefault()
            ?? schema;

        var result = new APIProbeSchema
        {
            Type = realSchema.Type,
            Format = realSchema.Format,
            TypeName = referenceId,
            Nullable = realSchema.Nullable,
            IsArray = string.Equals(realSchema.Type, "array", StringComparison.OrdinalIgnoreCase),
            EnumValues = realSchema.Enum?.Select(item => item.ToString()).ToList() ?? new List<string>()
        };

        if (result.IsArray)
        {
            result.Items = CreateSchema(realSchema.Items, swaggerDocument, visitedReferences, depth + 1);
            return result;
        }

        if (realSchema.Properties == null || depth >= MaxSchemaDepth) return result;

        foreach (var property in realSchema.Properties)
        {
            result.Properties.Add(new APIProbeParameter
            {
                Name = property.Key,
                Source = "property",
                Required = realSchema.Required?.Contains(property.Key) == true,
                Description = property.Value.Description,
                Schema = CreateSchema(property.Value, swaggerDocument, visitedReferences, depth + 1)
            });
        }

        return result;
    }
}
