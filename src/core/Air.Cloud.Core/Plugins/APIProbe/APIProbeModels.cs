namespace Air.Cloud.Core.Plugins.APIProbe;

/// <summary>
/// <para>zh-cn:表示 API 探测文档查询参数，用于筛选文档分组并控制是否返回架构详情。</para>
/// <para>en-us:Represents query options for an API probe document, used to filter the document group and control whether schema details are included.</para>
/// </summary>
public sealed class APIProbeQuery
{
    /// <summary>
    /// <para>zh-cn:获取或设置请求的 API 文档分组名称。</para>
    /// <para>en-us:Gets or sets the requested API document group name.</para>
    /// </summary>
    public string Group { get; set; }

    /// <summary>
    /// <para>zh-cn:获取或设置是否在结果中包含请求体、响应体和参数的架构详情。</para>
    /// <para>en-us:Gets or sets whether the result should include schema details for request bodies, response bodies, and parameters.</para>
    /// </summary>
    public bool IncludeSchemas { get; set; } = true;
}

/// <summary>
/// <para>zh-cn:表示标准化后的 API 探测文档结果，包含文档来源、应用信息、分组和接口端点集合。</para>
/// <para>en-us:Represents a normalized API probe document result, including provider information, application metadata, groups, and endpoint collection.</para>
/// </summary>
public sealed class APIProbeResult
{
    /// <summary>
    /// <para>zh-cn:获取或设置生成当前 API 文档的提供器名称。</para>
    /// <para>en-us:Gets or sets the provider name that generated the current API document.</para>
    /// </summary>
    public string ProviderName { get; set; }

    /// <summary>
    /// <para>zh-cn:获取或设置当前 API 文档所属的应用名称。</para>
    /// <para>en-us:Gets or sets the application name that owns the current API document.</para>
    /// </summary>
    public string ApplicationName { get; set; }

    /// <summary>
    /// <para>zh-cn:获取或设置当前 API 文档所属的应用版本。</para>
    /// <para>en-us:Gets or sets the application version that owns the current API document.</para>
    /// </summary>
    public string ApplicationVersion { get; set; }

    /// <summary>
    /// <para>zh-cn:获取或设置 API 文档生成时间。</para>
    /// <para>en-us:Gets or sets the API document generation time.</para>
    /// </summary>
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// <para>zh-cn:获取或设置当前文档包含的 API 分组集合。</para>
    /// <para>en-us:Gets or sets the API groups included in the current document.</para>
    /// </summary>
    public IList<string> Groups { get; set; } = new List<string>();

    /// <summary>
    /// <para>zh-cn:获取或设置当前文档包含的 API 端点集合。</para>
    /// <para>en-us:Gets or sets the API endpoints included in the current document.</para>
    /// </summary>
    public IList<APIProbeEndpoint> Endpoints { get; set; } = new List<APIProbeEndpoint>();
}

/// <summary>
/// <para>zh-cn:表示标准化后的 API 端点元数据，描述路由、方法、授权、请求和响应信息。</para>
/// <para>en-us:Represents normalized API endpoint metadata, describing route, method, authorization, request, and response information.</para>
/// </summary>
public sealed class APIProbeEndpoint
{
    /// <summary>
    /// <para>zh-cn:获取或设置端点的稳定唯一标识。</para>
    /// <para>en-us:Gets or sets the stable unique identifier of the endpoint.</para>
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// <para>zh-cn:获取或设置端点所属的 API 文档分组。</para>
    /// <para>en-us:Gets or sets the API document group to which the endpoint belongs.</para>
    /// </summary>
    public string Group { get; set; }

    /// <summary>
    /// <para>zh-cn:获取或设置端点标签，通常用于接口目录中的模块或控制器归类。</para>
    /// <para>en-us:Gets or sets the endpoint tag, usually used to classify modules or controllers in an API catalog.</para>
    /// </summary>
    public string Tag { get; set; }

    /// <summary>
    /// <para>zh-cn:获取或设置端点名称。</para>
    /// <para>en-us:Gets or sets the endpoint name.</para>
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// <para>zh-cn:获取或设置端点使用的 HTTP 方法，例如 `GET`、`POST`、`PUT` 或 `DELETE`。</para>
    /// <para>en-us:Gets or sets the HTTP method used by the endpoint, such as `GET`, `POST`, `PUT`, or `DELETE`.</para>
    /// </summary>
    public string Method { get; set; }

    /// <summary>
    /// <para>zh-cn:获取或设置端点路由路径。</para>
    /// <para>en-us:Gets or sets the endpoint route path.</para>
    /// </summary>
    public string Path { get; set; }

    /// <summary>
    /// <para>zh-cn:获取或设置端点摘要，用于接口列表中的简短说明。</para>
    /// <para>en-us:Gets or sets the endpoint summary used as a short description in API listings.</para>
    /// </summary>
    public string Summary { get; set; }

    /// <summary>
    /// <para>zh-cn:获取或设置端点详细描述。</para>
    /// <para>en-us:Gets or sets the detailed endpoint description.</para>
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// <para>zh-cn:获取或设置端点排序值。</para>
    /// <para>en-us:Gets or sets the endpoint ordering value.</para>
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// <para>zh-cn:获取或设置端点是否显式允许匿名访问。</para>
    /// <para>en-us:Gets or sets whether the endpoint explicitly allows anonymous access.</para>
    /// </summary>
    public bool IsAllowAnonymous { get; set; }

    /// <summary>
    /// <para>zh-cn:获取或设置端点是否需要授权。</para>
    /// <para>en-us:Gets or sets whether the endpoint requires authorization.</para>
    /// </summary>
    public bool RequiresAuthorization { get; set; }

    /// <summary>
    /// <para>zh-cn:获取或设置端点声明的授权元数据集合。</para>
    /// <para>en-us:Gets or sets the authorization metadata declared by the endpoint.</para>
    /// </summary>
    public IList<APIProbeAuthorizeData> AuthorizeDatas { get; set; } = new List<APIProbeAuthorizeData>();

    /// <summary>
    /// <para>zh-cn:获取或设置端点请求信息。</para>
    /// <para>en-us:Gets or sets the endpoint request information.</para>
    /// </summary>
    public APIProbeRequest Request { get; set; } = new APIProbeRequest();

    /// <summary>
    /// <para>zh-cn:获取或设置端点可能返回的响应信息集合。</para>
    /// <para>en-us:Gets or sets the possible response information returned by the endpoint.</para>
    /// </summary>
    public IList<APIProbeResponse> Responses { get; set; } = new List<APIProbeResponse>();
}

/// <summary>
/// <para>zh-cn:表示标准化后的授权元数据，描述认证方案、授权策略和角色约束。</para>
/// <para>en-us:Represents normalized authorization metadata, describing authentication schemes, authorization policies, and role constraints.</para>
/// </summary>
public sealed class APIProbeAuthorizeData
{
    /// <summary>
    /// <para>zh-cn:获取或设置授权要求使用的认证方案。</para>
    /// <para>en-us:Gets or sets the authentication schemes required by the authorization metadata.</para>
    /// </summary>
    public string AuthenticationSchemes { get; set; }

    /// <summary>
    /// <para>zh-cn:获取或设置授权策略名称。</para>
    /// <para>en-us:Gets or sets the authorization policy name.</para>
    /// </summary>
    public string Policy { get; set; }

    /// <summary>
    /// <para>zh-cn:获取或设置访问端点所需的角色表达式。</para>
    /// <para>en-us:Gets or sets the role expression required to access the endpoint.</para>
    /// </summary>
    public string Roles { get; set; }
}

/// <summary>
/// <para>zh-cn:表示标准化后的请求元数据，包含请求内容类型、参数集合和请求体架构。</para>
/// <para>en-us:Represents normalized request metadata, including request content types, parameters, and body schema.</para>
/// </summary>
public sealed class APIProbeRequest
{
    /// <summary>
    /// <para>zh-cn:获取或设置请求支持的内容类型集合。</para>
    /// <para>en-us:Gets or sets the content types supported by the request.</para>
    /// </summary>
    public IList<string> ContentTypes { get; set; } = new List<string>();

    /// <summary>
    /// <para>zh-cn:获取或设置请求参数集合，包括路由、查询、请求头或其他来源的参数。</para>
    /// <para>en-us:Gets or sets the request parameters, including parameters from route, query, header, or other sources.</para>
    /// </summary>
    public IList<APIProbeParameter> Parameters { get; set; } = new List<APIProbeParameter>();

    /// <summary>
    /// <para>zh-cn:获取或设置请求体架构；当端点没有请求体时可为空。</para>
    /// <para>en-us:Gets or sets the request body schema; it can be empty when the endpoint has no request body.</para>
    /// </summary>
    public APIProbeSchema Body { get; set; }
}

/// <summary>
/// <para>zh-cn:表示标准化后的响应元数据，包含状态码、描述、内容类型和响应体架构。</para>
/// <para>en-us:Represents normalized response metadata, including status code, description, content types, and body schema.</para>
/// </summary>
public sealed class APIProbeResponse
{
    /// <summary>
    /// <para>zh-cn:获取或设置响应 HTTP 状态码。</para>
    /// <para>en-us:Gets or sets the response HTTP status code.</para>
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// <para>zh-cn:获取或设置响应说明。</para>
    /// <para>en-us:Gets or sets the response description.</para>
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// <para>zh-cn:获取或设置响应支持的内容类型集合。</para>
    /// <para>en-us:Gets or sets the content types supported by the response.</para>
    /// </summary>
    public IList<string> ContentTypes { get; set; } = new List<string>();

    /// <summary>
    /// <para>zh-cn:获取或设置响应体架构；当响应没有结构化响应体时可为空。</para>
    /// <para>en-us:Gets or sets the response body schema; it can be empty when the response has no structured body.</para>
    /// </summary>
    public APIProbeSchema Body { get; set; }
}

/// <summary>
/// <para>zh-cn:表示标准化后的请求参数元数据，描述参数名称、来源、是否必填、默认值和参数架构。</para>
/// <para>en-us:Represents normalized request parameter metadata, describing parameter name, source, required state, default value, and schema.</para>
/// </summary>
public sealed class APIProbeParameter
{
    /// <summary>
    /// <para>zh-cn:获取或设置参数名称。</para>
    /// <para>en-us:Gets or sets the parameter name.</para>
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// <para>zh-cn:获取或设置参数来源，例如路由、查询、请求头或请求体。</para>
    /// <para>en-us:Gets or sets the parameter source, such as route, query, header, or body.</para>
    /// </summary>
    public string Source { get; set; }

    /// <summary>
    /// <para>zh-cn:获取或设置参数是否为必填项。</para>
    /// <para>en-us:Gets or sets whether the parameter is required.</para>
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// <para>zh-cn:获取或设置参数说明。</para>
    /// <para>en-us:Gets or sets the parameter description.</para>
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// <para>zh-cn:获取或设置参数默认值。</para>
    /// <para>en-us:Gets or sets the parameter default value.</para>
    /// </summary>
    public object DefaultValue { get; set; }

    /// <summary>
    /// <para>zh-cn:获取或设置参数值架构。</para>
    /// <para>en-us:Gets or sets the parameter value schema.</para>
    /// </summary>
    public APIProbeSchema Schema { get; set; }
}

/// <summary>
/// <para>zh-cn:表示标准化后的数据架构元数据，描述类型、格式、数组元素、枚举值和对象属性。</para>
/// <para>en-us:Represents normalized data schema metadata, describing type, format, array items, enum values, and object properties.</para>
/// </summary>
public sealed class APIProbeSchema
{
    /// <summary>
    /// <para>zh-cn:获取或设置架构基础类型，例如 `string`、`integer`、`object` 或 `array`。</para>
    /// <para>en-us:Gets or sets the base schema type, such as `string`, `integer`, `object`, or `array`.</para>
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// <para>zh-cn:获取或设置架构格式，用于进一步描述基础类型。</para>
    /// <para>en-us:Gets or sets the schema format used to further describe the base type.</para>
    /// </summary>
    public string Format { get; set; }

    /// <summary>
    /// <para>zh-cn:获取或设置架构对应的 .NET 类型名称。</para>
    /// <para>en-us:Gets or sets the .NET type name associated with the schema.</para>
    /// </summary>
    public string TypeName { get; set; }

    /// <summary>
    /// <para>zh-cn:获取或设置该架构值是否允许为空。</para>
    /// <para>en-us:Gets or sets whether the schema value allows null.</para>
    /// </summary>
    public bool Nullable { get; set; }

    /// <summary>
    /// <para>zh-cn:获取或设置该架构是否表示数组类型。</para>
    /// <para>en-us:Gets or sets whether the schema represents an array type.</para>
    /// </summary>
    public bool IsArray { get; set; }

    /// <summary>
    /// <para>zh-cn:获取或设置数组元素架构；仅当当前架构表示数组时使用。</para>
    /// <para>en-us:Gets or sets the array item schema; used only when the current schema represents an array.</para>
    /// </summary>
    public APIProbeSchema Items { get; set; }

    /// <summary>
    /// <para>zh-cn:获取或设置枚举值集合。</para>
    /// <para>en-us:Gets or sets the enum value collection.</para>
    /// </summary>
    public IList<string> EnumValues { get; set; } = new List<string>();

    /// <summary>
    /// <para>zh-cn:获取或设置对象属性集合，每个属性以参数元数据形式描述。</para>
    /// <para>en-us:Gets or sets the object property collection, with each property described as parameter metadata.</para>
    /// </summary>
    public IList<APIProbeParameter> Properties { get; set; } = new List<APIProbeParameter>();
}
