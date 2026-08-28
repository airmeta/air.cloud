using System.Text.RegularExpressions;

namespace Air.Cloud.Modules.ReverseTunnel.Common.Routing;

/// <summary>
/// <para>zh-cn:解析 AirTunnel Gateway 的公开路径模板。模板支持固定路径前缀和末尾单段 catch-all 参数，例如 `/api/orders/{path}`。</para>
/// <para>en-us:Parses AirTunnel Gateway public-path templates. Templates support fixed prefixes and one terminal catch-all parameter, such as `/api/orders/{path}`.</para>
/// </summary>
public static class ReverseTunnelRouteTemplate
{
    private static readonly Regex CatchAll = new("^/([^/{}]+/)*\\{([A-Za-z][A-Za-z0-9_]*)\\}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// <para>zh-cn:验证并规范化公开路径模板。固定路径和末尾 catch-all 参数均返回规范化结果，非法模板返回空字符串。</para>
    /// <para>en-us:Validates and normalizes a public-path template. Fixed paths and terminal catch-all parameters return a normalized value; invalid templates return an empty string.</para>
    /// </summary>
    public static string NormalizePublicPath(string? template)
    {
        var path = template?.Trim();
        if (string.IsNullOrWhiteSpace(path) || !path.StartsWith('/') || path.Contains('?') || path.Contains('#'))
        {
            return string.Empty;
        }

        path = path.TrimEnd('/');
        if (string.IsNullOrEmpty(path))
        {
            return "/";
        }

        if (path.Contains('{') || path.Contains('}'))
        {
            var match = CatchAll.Match(path);
            return match.Success ? path[..(path.LastIndexOf("/{", StringComparison.Ordinal) + 1)].ToLowerInvariant() + "{" + match.Groups[2].Value.ToLowerInvariant() + "}" : string.Empty;
        }

        return path.ToLowerInvariant();
    }

    /// <summary>
    /// <para>zh-cn:按模板匹配请求路径，并返回 catch-all 捕获值或固定前缀剩余路径。</para>
    /// <para>en-us:Matches a request path against the template and returns the catch-all capture or the remaining fixed-prefix path.</para>
    /// </summary>
    public static bool TryMatch(string? template, string? requestPath, out string suffix)
    {
        suffix = string.Empty;
        var normalized = NormalizePublicPath(template);
        var path = string.IsNullOrWhiteSpace(requestPath) ? "/" : requestPath!;
        if (string.IsNullOrEmpty(normalized))
        {
            return false;
        }

        var marker = normalized.LastIndexOf("/{", StringComparison.Ordinal);
        if (marker >= 0 && normalized.EndsWith('}'))
        {
            var prefix = normalized[..marker];
            if (path.Equals(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (path.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase))
            {
                suffix = path[(prefix.Length + 1)..];
                return true;
            }

            return false;
        }

        if (normalized == "/")
        {
            suffix = path == "/" ? "/" : path;
            return path.StartsWith("/", StringComparison.Ordinal);
        }

        if (!path.StartsWith(normalized, StringComparison.OrdinalIgnoreCase) ||
            (path.Length > normalized.Length && path[normalized.Length] != '/'))
        {
            return false;
        }

        suffix = path.Length == normalized.Length ? "/" : path[normalized.Length..];
        return true;
    }

    /// <summary>
    /// <para>zh-cn:将路由后缀应用到重写模板。重写模板不含参数时保留原后缀，含任意一个 catch-all 参数时将其替换。</para>
    /// <para>en-us:Applies a route suffix to a rewrite template. A template without a parameter preserves the original suffix; a template with one catch-all parameter replaces it.</para>
    /// </summary>
    public static string ApplyRewrite(string? rewritePath, string suffix)
    {
        var normalized = NormalizeRewritePath(rewritePath);
        if (string.IsNullOrEmpty(normalized))
        {
            return string.IsNullOrEmpty(suffix) ? "/" : suffix.StartsWith("/", StringComparison.Ordinal) ? suffix : "/" + suffix;
        }

        var marker = normalized.IndexOf("/{", StringComparison.Ordinal);
        if (marker >= 0 && normalized.EndsWith('}'))
        {
            var capture = suffix.TrimStart('/');
            return normalized[..marker] + (string.IsNullOrEmpty(capture) ? string.Empty : "/" + capture);
        }

        var cleanSuffix = suffix == "/" ? string.Empty : suffix;
        return normalized + cleanSuffix;
    }

    /// <summary>
    /// <para>zh-cn:验证目标路径重写模板，不允许查询串、片段或非末尾参数。</para>
    /// <para>en-us:Validates a target-path rewrite template and rejects query strings, fragments, and non-terminal parameters.</para>
    /// </summary>
    public static string NormalizeRewritePath(string? rewritePath)
    {
        var path = rewritePath?.Trim();
        if (string.IsNullOrEmpty(path))
        {
            return string.Empty;
        }

        return NormalizePublicPath(path);
    }
}
