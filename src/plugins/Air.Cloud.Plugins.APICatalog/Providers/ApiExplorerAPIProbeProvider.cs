using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;

using System.Collections;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Air.Cloud.Plugins.APICatalog.Providers;

public sealed class ApiExplorerAPIProbeProvider : IAPIProbeProvider
{
    private const int MaxSchemaDepth = 5;
    private readonly IApiDescriptionGroupCollectionProvider _apiDescriptionGroupCollectionProvider;
    private readonly AppSettingsOptions _appSettings;

    public ApiExplorerAPIProbeProvider(IApiDescriptionGroupCollectionProvider apiDescriptionGroupCollectionProvider)
    {
        _apiDescriptionGroupCollectionProvider = apiDescriptionGroupCollectionProvider;
        _appSettings = AppCore.Settings;
    }

    public string ProviderName => "ApiExplorer";

    public Task<APIProbeResult> GetDocumentAsync(APIProbeQuery query, CancellationToken cancellationToken = default)
    {
        var endpoints = new List<APIProbeEndpoint>();
        var groups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var group in _apiDescriptionGroupCollectionProvider.ApiDescriptionGroups.Items)
        {
            foreach (var apiDescription in group.Items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var groupName = ResolveGroupName(apiDescription, group.GroupName);
                if (!string.IsNullOrWhiteSpace(query?.Group) && !string.Equals(query.Group, groupName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                groups.Add(groupName);
                endpoints.Add(CreateEndpoint(apiDescription, groupName, query?.IncludeSchemas != false));
            }
        }

        var result = new APIProbeResult
        {
            ProviderName = ProviderName,
            ApplicationName = AppConst.ApplicationName,
            ApplicationVersion = _appSettings.Version,
            GeneratedAt = DateTimeOffset.Now,
            Groups = groups.OrderBy(item => item).ToList(),
            Endpoints = endpoints
                .OrderBy(item => item.Group)
                .ThenBy(item => item.Path)
                .ThenBy(item => item.Method)
                .ToList()
        };

        return Task.FromResult(result);
    }

    private static APIProbeEndpoint CreateEndpoint(ApiDescription apiDescription, string groupName, bool includeSchemas)
    {
        var path = "/" + (apiDescription.RelativePath ?? string.Empty).Split('?')[0].TrimStart('/');
        var method = apiDescription.HttpMethod ?? "GET";
        var endpoint = new APIProbeEndpoint
        {
            Id = $"{method}:{path}",
            Group = groupName,
            Tag = ResolveTag(apiDescription),
            Name = ResolveName(apiDescription),
            Method = method,
            Path = path,
            Summary = ResolveSummary(apiDescription),
            Description = ResolveDescription(apiDescription),
            Request = CreateRequest(apiDescription, includeSchemas),
            Responses = CreateResponses(apiDescription, includeSchemas)
        };

        ApplyAuthorization(apiDescription, endpoint);
        return endpoint;
    }

    private static APIProbeRequest CreateRequest(ApiDescription apiDescription, bool includeSchemas)
    {
        var request = new APIProbeRequest
        {
            ContentTypes = apiDescription.SupportedRequestFormats
                .Select(format => format.MediaType)
                .Where(mediaType => !string.IsNullOrWhiteSpace(mediaType))
                .Distinct()
                .ToList()
        };

        foreach (var parameter in apiDescription.ParameterDescriptions)
        {
            if (parameter.Source == BindingSource.Body)
            {
                if (includeSchemas && parameter.Type != null)
                {
                    request.Body = CreateSchema(parameter.Type);
                }

                continue;
            }

            request.Parameters.Add(CreateParameter(parameter, includeSchemas));
        }

        return request;
    }

    private static APIProbeParameter CreateParameter(ApiParameterDescription parameter, bool includeSchemas)
    {
        return new APIProbeParameter
        {
            Name = parameter.Name,
            Source = parameter.Source?.Id ?? parameter.Source?.DisplayName,
            Required = parameter.IsRequired,
            Description = parameter.ModelMetadata?.Description,
            DefaultValue = parameter.DefaultValue,
            Schema = includeSchemas && parameter.Type != null ? CreateSchema(parameter.Type) : null
        };
    }

    private static IList<APIProbeResponse> CreateResponses(ApiDescription apiDescription, bool includeSchemas)
    {
        if (apiDescription.SupportedResponseTypes.Count == 0)
        {
            return new List<APIProbeResponse>
            {
                new APIProbeResponse
                {
                    StatusCode = StatusCodes.Status200OK,
                    Description = "Success"
                }
            };
        }

        return apiDescription.SupportedResponseTypes.Select(responseType => new APIProbeResponse
        {
            StatusCode = responseType.StatusCode,
            Description = responseType.ModelMetadata?.Description,
            ContentTypes = responseType.ApiResponseFormats
                .Select(format => format.MediaType)
                .Where(mediaType => !string.IsNullOrWhiteSpace(mediaType))
                .Distinct()
                .ToList(),
            Body = includeSchemas && !IsVoid(responseType.Type) ? CreateSchema(responseType.Type) : null
        }).ToList();
    }

    private static void ApplyAuthorization(ApiDescription apiDescription, APIProbeEndpoint endpoint)
    {
        var authorizeAttributes = ReadEndpointMetadata<AuthorizeAttribute>(apiDescription).ToArray();
        endpoint.IsAllowAnonymous = ReadEndpointMetadata<AllowAnonymousAttribute>(apiDescription).Any();
        endpoint.RequiresAuthorization = !endpoint.IsAllowAnonymous && authorizeAttributes.Length > 0;
        endpoint.AuthorizeDatas = authorizeAttributes.Select(attribute => new APIProbeAuthorizeData
        {
            AuthenticationSchemes = attribute.AuthenticationSchemes,
            Policy = attribute.Policy,
            Roles = attribute.Roles
        }).ToList();
    }

    private static IEnumerable<T> ReadEndpointMetadata<T>(ApiDescription apiDescription) where T : class
    {
        foreach (var metadata in apiDescription.ActionDescriptor.EndpointMetadata.OfType<T>())
        {
            yield return metadata;
        }

        if (apiDescription.ActionDescriptor is not ControllerActionDescriptor controllerActionDescriptor) yield break;

        foreach (var attribute in controllerActionDescriptor.ControllerTypeInfo.GetCustomAttributes(typeof(T), true).OfType<T>())
        {
            yield return attribute;
        }

        foreach (var attribute in controllerActionDescriptor.MethodInfo.GetCustomAttributes(typeof(T), true).OfType<T>())
        {
            yield return attribute;
        }
    }

    private static string ResolveGroupName(ApiDescription apiDescription, string groupName)
    {
        return apiDescription.GroupName
            ?? groupName
            ?? "Default";
    }

    private static string ResolveTag(ApiDescription apiDescription)
    {
        if (apiDescription.ActionDescriptor is not ControllerActionDescriptor controllerActionDescriptor)
        {
            return apiDescription.GroupName ?? "Default";
        }

        return controllerActionDescriptor.ControllerName;
    }

    private static string ResolveName(ApiDescription apiDescription)
    {
        if (!string.IsNullOrWhiteSpace(apiDescription.ActionDescriptor.AttributeRouteInfo?.Name))
        {
            return apiDescription.ActionDescriptor.AttributeRouteInfo.Name;
        }

        return apiDescription.ActionDescriptor is ControllerActionDescriptor controllerActionDescriptor
            ? controllerActionDescriptor.MethodInfo.Name
            : apiDescription.ActionDescriptor.DisplayName;
    }

    private static string ResolveSummary(ApiDescription apiDescription)
    {
        return ReadDescriptionAttributes(apiDescription).FirstOrDefault()?.Description;
    }

    private static string ResolveDescription(ApiDescription apiDescription)
    {
        return ReadDescriptionAttributes(apiDescription).FirstOrDefault()?.Description;
    }

    private static IEnumerable<DescriptionAttribute> ReadDescriptionAttributes(ApiDescription apiDescription)
    {
        foreach (var metadata in apiDescription.ActionDescriptor.EndpointMetadata.OfType<DescriptionAttribute>())
        {
            yield return metadata;
        }

        if (apiDescription.ActionDescriptor is not ControllerActionDescriptor controllerActionDescriptor) yield break;

        foreach (var attribute in controllerActionDescriptor.MethodInfo.GetCustomAttributes<DescriptionAttribute>(true))
        {
            yield return attribute;
        }
    }

    private static APIProbeSchema CreateSchema(Type type)
    {
        return CreateSchema(type, new HashSet<Type>(), 0);
    }

    private static APIProbeSchema CreateSchema(Type type, HashSet<Type> visitedTypes, int depth)
    {
        if (type == null)
        {
            return null;
        }

        var nullableType = Nullable.GetUnderlyingType(type);
        var isNullable = nullableType != null || !type.IsValueType;
        if (nullableType != null) type = nullableType;

        if (type.IsEnum)
        {
            return new APIProbeSchema
            {
                Type = "string",
                TypeName = GetFriendlyTypeName(type),
                Nullable = isNullable,
                EnumValues = Enum.GetNames(type).ToList()
            };
        }

        if (TryCreateScalarSchema(type, isNullable, out var scalarSchema))
        {
            return scalarSchema;
        }

        if (TryGetEnumerableItemType(type, out var itemType))
        {
            return new APIProbeSchema
            {
                Type = "array",
                TypeName = GetFriendlyTypeName(type),
                Nullable = isNullable,
                IsArray = true,
                Items = depth >= MaxSchemaDepth ? null : CreateSchema(itemType, visitedTypes, depth + 1)
            };
        }

        var schema = new APIProbeSchema
        {
            Type = "object",
            TypeName = GetFriendlyTypeName(type),
            Nullable = isNullable
        };

        if (depth >= MaxSchemaDepth || visitedTypes.Contains(type))
        {
            return schema;
        }

        visitedTypes.Add(type);
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                     .Where(property => property.GetIndexParameters().Length == 0))
        {
            schema.Properties.Add(new APIProbeParameter
            {
                Name = property.Name,
                Source = "property",
                Required = property.GetCustomAttribute<RequiredAttribute>() != null,
                Description = property.GetCustomAttribute<DescriptionAttribute>()?.Description,
                Schema = CreateSchema(property.PropertyType, visitedTypes, depth + 1)
            });
        }

        visitedTypes.Remove(type);
        return schema;
    }

    private static bool TryCreateScalarSchema(Type type, bool isNullable, out APIProbeSchema schema)
    {
        schema = type switch
        {
            _ when type == typeof(string) => new APIProbeSchema { Type = "string" },
            _ when type == typeof(bool) => new APIProbeSchema { Type = "boolean" },
            _ when type == typeof(byte) || type == typeof(short) || type == typeof(int) => new APIProbeSchema { Type = "integer", Format = "int32" },
            _ when type == typeof(long) => new APIProbeSchema { Type = "integer", Format = "int64" },
            _ when type == typeof(float) => new APIProbeSchema { Type = "number", Format = "float" },
            _ when type == typeof(double) || type == typeof(decimal) => new APIProbeSchema { Type = "number", Format = "double" },
            _ when type == typeof(DateTime) || type == typeof(DateTimeOffset) => new APIProbeSchema { Type = "string", Format = "date-time" },
            _ when type == typeof(DateOnly) => new APIProbeSchema { Type = "string", Format = "date" },
            _ when type == typeof(TimeOnly) || type == typeof(TimeSpan) => new APIProbeSchema { Type = "string", Format = "time" },
            _ when type == typeof(Guid) => new APIProbeSchema { Type = "string", Format = "uuid" },
            _ when type == typeof(IFormFile) => new APIProbeSchema { Type = "string", Format = "binary" },
            _ => null
        };

        if (schema == null) return false;

        schema.TypeName = GetFriendlyTypeName(type);
        schema.Nullable = isNullable;
        return true;
    }

    private static bool TryGetEnumerableItemType(Type type, out Type itemType)
    {
        itemType = null;
        if (type == typeof(string) || !typeof(IEnumerable).IsAssignableFrom(type)) return false;

        if (type.IsArray)
        {
            itemType = type.GetElementType();
            return itemType != null;
        }

        itemType = type.GetInterfaces()
            .Concat(new[] { type })
            .Where(item => item.IsGenericType && item.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            .Select(item => item.GetGenericArguments()[0])
            .FirstOrDefault();

        return itemType != null;
    }

    private static bool IsVoid(Type type)
    {
        return type == null || type == typeof(void) || type == typeof(Task);
    }

    private static string GetFriendlyTypeName(Type type)
    {
        if (!type.IsGenericType) return type.Name;

        var typeName = type.Name.Split('`')[0];
        var genericArguments = string.Join(",", type.GetGenericArguments().Select(GetFriendlyTypeName));
        return $"{typeName}<{genericArguments}>";
    }
}
