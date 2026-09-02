using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace Air.Cloud.DynamicApp.Gateway;

/// <summary>
/// <para>zh-cn:解析和匹配 Ocelot 风格的动态路由路径模板，支持字面量、单段命名参数以及尾部 path/everything 捕获参数。</para>
/// <para>en-us:Parses and matches Ocelot-style dynamic route templates with literals, single-segment named parameters, and trailing path/everything catch-all parameters.</para>
/// </summary>
public sealed class DynamicAppGatewayRouteTemplate
{
    private static readonly Regex ParameterName = new("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.CultureInvariant);
    private readonly IReadOnlyList<Segment> _segments;

    private DynamicAppGatewayRouteTemplate(string normalizedTemplate, IReadOnlyList<Segment> segments)
    {
        NormalizedTemplate = normalizedTemplate;
        _segments = segments;
    }

    /// <summary>
    /// <para>zh-cn:规范化后的绝对路径模板。</para>
    /// <para>en-us:Normalized absolute path template.</para>
    /// </summary>
    public string NormalizedTemplate { get; }

    /// <summary>
    /// <para>zh-cn:解析路径模板并校验模板语法。</para>
    /// <para>en-us:Parses a path template and validates its syntax.</para>
    /// </summary>
    /// <param name="template">
    /// <para>zh-cn:不包含查询字符串、片段或反斜杠的绝对路径模板。</para>
    /// <para>en-us:Absolute path template without a query string, fragment, or backslash.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:可复用的模板对象。</para>
    /// <para>en-us:Reusable parsed template.</para>
    /// </returns>
    public static DynamicAppGatewayRouteTemplate Parse(string template)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            throw new ArgumentException("Route template cannot be empty.", nameof(template));
        }

        var normalized = NormalizePath(template, nameof(template));
        if (normalized == "/")
        {
            return new DynamicAppGatewayRouteTemplate(normalized, Array.Empty<Segment>());
        }

        var rawSegments = normalized.Trim('/').Split('/');
        var segments = new List<Segment>(rawSegments.Length);
        var parameters = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < rawSegments.Length; index++)
        {
            var raw = rawSegments[index];
            if (raw.Length >= 2 && raw[0] == '{' && raw[^1] == '}')
            {
                var name = raw[1..^1];
                if (!ParameterName.IsMatch(name))
                {
                    throw new ArgumentException($"Route template parameter `{name}` is invalid.", nameof(template));
                }

                if (!parameters.Add(name))
                {
                    throw new ArgumentException($"Route template parameter `{name}` is duplicated.", nameof(template));
                }

                var catchAll = index == rawSegments.Length - 1
                    && (name.Equals("path", StringComparison.OrdinalIgnoreCase)
                        || name.Equals("everything", StringComparison.OrdinalIgnoreCase));
                segments.Add(new Segment(name, catchAll, null));
                continue;
            }

            if (raw.Contains('{') || raw.Contains('}'))
            {
                throw new ArgumentException("Route parameters must occupy a complete path segment.", nameof(template));
            }

            segments.Add(new Segment(null, false, raw));
        }

        return new DynamicAppGatewayRouteTemplate(normalized, new ReadOnlyCollection<Segment>(segments));
    }

    /// <summary>
    /// <para>zh-cn:尝试匹配请求路径并提取模板参数。</para>
    /// <para>en-us:Attempts to match a request path and extract template parameters.</para>
    /// </summary>
    /// <param name="requestPath">
    /// <para>zh-cn:不含查询字符串的请求路径。</para>
    /// <para>en-us:Request path without a query string.</para>
    /// </param>
    /// <param name="parameters">
    /// <para>zh-cn:成功时返回参数值；失败时返回空集合。</para>
    /// <para>en-us:Parameter values on success; an empty collection on failure.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:是否匹配成功。</para>
    /// <para>en-us:Whether the path matched.</para>
    /// </returns>
    public bool TryMatch(string requestPath, out IReadOnlyDictionary<string, string> parameters)
    {
        parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var normalizedPath = NormalizePath(requestPath, nameof(requestPath));
        if (normalizedPath == "/")
        {
            return _segments.Count == 0;
        }

        var pathSegments = normalizedPath.Trim('/').Split('/');
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var pathIndex = 0;
        for (var templateIndex = 0; templateIndex < _segments.Count; templateIndex++)
        {
            var segment = _segments[templateIndex];
            if (segment.IsCatchAll)
            {
                values[segment.ParameterName!] = pathIndex >= pathSegments.Length
                    ? string.Empty
                    : string.Join('/', pathSegments[pathIndex..]);
                pathIndex = pathSegments.Length;
                break;
            }

            if (pathIndex >= pathSegments.Length)
            {
                return false;
            }

            if (segment.ParameterName != null)
            {
                values[segment.ParameterName] = pathSegments[pathIndex];
            }
            else if (!string.Equals(segment.Literal, pathSegments[pathIndex], StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            pathIndex++;
        }

        if (pathIndex != pathSegments.Length)
        {
            return false;
        }

        parameters = new ReadOnlyDictionary<string, string>(values);
        return true;
    }

    /// <summary>
    /// <para>zh-cn:将参数值替换到下游模板中，并返回规范化路径。</para>
    /// <para>en-us:Substitutes parameter values into a downstream template and returns a normalized path.</para>
    /// </summary>
    /// <param name="downstreamTemplate">
    /// <para>zh-cn:下游绝对路径模板。</para>
    /// <para>en-us:Absolute downstream path template.</para>
    /// </param>
    /// <param name="parameters">
    /// <para>zh-cn:上游模板提取的参数集合。</para>
    /// <para>en-us:Parameters extracted from the upstream template.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:替换后的下游路径。</para>
    /// <para>en-us:Substituted downstream path.</para>
    /// </returns>
    public static string BuildDownstreamPath(string downstreamTemplate, IReadOnlyDictionary<string, string> parameters)
    {
        var parsed = Parse(downstreamTemplate);
        var segments = parsed._segments.Select(segment =>
        {
            if (segment.ParameterName == null)
            {
                return segment.Literal!;
            }

            if (!parameters.TryGetValue(segment.ParameterName, out var value))
            {
                throw new ArgumentException($"No value was supplied for downstream parameter `{segment.ParameterName}`.", nameof(parameters));
            }

            if (value.Contains('/') && !segment.IsCatchAll)
            {
                throw new ArgumentException($"Downstream parameter `{segment.ParameterName}` contains a path separator.", nameof(parameters));
            }

            return value;
        }).ToArray();
        return segments.Length == 0 ? "/" : "/" + string.Join('/', segments);
    }

    internal IReadOnlyList<(string? ParameterName, bool IsCatchAll, string? Literal)> Segments
        => _segments.Select(segment => (segment.ParameterName, segment.IsCatchAll, segment.Literal)).ToArray();

    private static string NormalizePath(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Route path cannot be empty.", parameterName);
        }

        var normalized = value.Trim();
        if (!normalized.StartsWith("/", StringComparison.Ordinal)
            || normalized.Contains("?", StringComparison.Ordinal)
            || normalized.Contains("#", StringComparison.Ordinal)
            || normalized.Contains("\\", StringComparison.Ordinal)
            || normalized.Contains("//", StringComparison.Ordinal))
        {
            throw new ArgumentException("Route path must be an absolute path without query, fragment, backslash, or empty segments.", parameterName);
        }

        return normalized.Length > 1 ? normalized.TrimEnd('/') : normalized;
    }

    private sealed record Segment(string? ParameterName, bool IsCatchAll, string? Literal);
}
