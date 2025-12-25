using Air.Cloud.Core.Plugins.Router;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;

using System.Text.RegularExpressions;

namespace Air.Cloud.WebApp.Router
{
    /// <summary>
    /// 路由匹配器：仅判断是否匹配（支持两种模式）
    /// </summary>
    public class PublicApiRouteMatcher:IRouterMatcherPlugin
    {
        public bool Match(string routeTemplate, string requestPath)
        {
            if (string.IsNullOrWhiteSpace(routeTemplate) || string.IsNullOrWhiteSpace(requestPath))
            {
                return false;
            }
            var template = TemplateParser.Parse(NormalizePath(routeTemplate));
            var matcher = new TemplateMatcher(template, new RouteValueDictionary());

            var values = new RouteValueDictionary();
            return matcher.TryMatch(new PathString(NormalizePath(requestPath)), values);
        }
        /// <summary>
        /// 严格匹配：在结构匹配的基础上校验常见内联约束（int/long/guid/bool/alpha/alnum/string）。
        /// </summary>
        public static bool IsMatchStrict(string routeTemplate, string requestPath)
        {
            if (string.IsNullOrWhiteSpace(routeTemplate) || string.IsNullOrWhiteSpace(requestPath))
            {
                return false;
            }

            var normalizedTemplate = NormalizePath(routeTemplate);
            var normalizedPath = NormalizePath(requestPath);

            var templateSegments = normalizedTemplate.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var pathSegments = normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (templateSegments.Length != pathSegments.Length)
            {
                return false;
            }

            for (int i = 0; i < templateSegments.Length; i++)
            {
                var t = templateSegments[i];
                var p = pathSegments[i];

                if (IsParameterSegment(t))
                {
                    var (name, constraint) = ParseParameter(t);
                    if (!ValidateConstraint(p, constraint))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!string.Equals(t, p, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static string NormalizePath(string path)
        {
            path = path.Trim();
            if (!path.StartsWith("/"))
            {
                path = "/" + path;
            }
            return path;
        }

        private static bool IsParameterSegment(string segment)
        {
            return segment.StartsWith("{", StringComparison.Ordinal) && segment.EndsWith("}", StringComparison.Ordinal) && segment.Length > 2;
        }

        private static (string Name, string Constraint) ParseParameter(string segment)
        {
            var inner = segment.Substring(1, segment.Length - 2);
            var parts = inner.Split(':', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
            {
                return (parts[0], "string");
            }
            return (parts[0], parts[1]);
        }

        private static bool ValidateConstraint(string value, string constraint)
        {
            if (string.IsNullOrWhiteSpace(constraint) || string.Equals(constraint, "string", StringComparison.OrdinalIgnoreCase))
            {
                return !string.IsNullOrEmpty(value);
            }

            switch (constraint.ToLowerInvariant())
            {
                case "int":
                    return int.TryParse(value, out _);
                case "long":
                    return long.TryParse(value, out _);
                case "bool":
                case "boolean":
                    return bool.TryParse(value, out _);
                case "guid":
                case "uuid":
                    return Guid.TryParse(value, out _);
                case "alpha":
                    return Regex.IsMatch(value, "^[A-Za-z]+$");
                case "alnum":
                    return Regex.IsMatch(value, "^[A-Za-z0-9]+$");
                default:
                    return !string.IsNullOrEmpty(value);
            }
        }
    }
}