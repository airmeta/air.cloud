/*
 * Copyright (c) 2024-2030 星曳数据
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * This file is provided under the Mozilla Public License Version 2.0,
 * and the "NO WARRANTY" clause of the MPL is hereby expressly
 * acknowledged.
 */
using Air.Cloud.Core.Standard.Taxin.Model;

using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Air.Cloud.Modules.Taxin.Tools
{
    /// <summary>
    /// <para>zh-cn:Taxin 路由模板参数绑定工具，用请求数据中的同名属性安全渲染路由路径。</para>
    /// <para>en-us:Taxin route-template parameter binder that safely renders route paths from equally named request-data properties.</para>
    /// </summary>
    public static class TaxinRouteParameterBinder
    {
        private static readonly Regex RouteParameterRegex = new(
            @"\{(?<catchAll>\*{0,2})(?<name>[^{}:/?]+)(?<policies>(?::[^{}?]+)*)(?<optional>\?)?\}",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>
        /// <para>zh-cn:使用请求数据渲染 Taxin 路由模板。仅替换模板占位符，路径值会进行 URI 编码；必填参数缺失、为空或模板未完全渲染时抛出异常。</para>
        /// <para>en-us:Renders a Taxin route template with request data. Only template placeholders are replaced and path values are URI-escaped; an exception is thrown for missing or empty required parameters or an incompletely rendered template.</para>
        /// </summary>
        /// <param name="routeInformation">
        /// <para>zh-cn:Taxin 路由元数据，包含路由模板及方法参数的可选性信息。</para>
        /// <para>en-us:Taxin route metadata containing the route template and method-parameter optionality.</para>
        /// </param>
        /// <param name="data">
        /// <para>zh-cn:请求数据，可以是匿名对象、普通对象或以参数名为键的字典。</para>
        /// <para>en-us:Request data, which can be an anonymous object, a regular object, or a dictionary keyed by parameter name.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:已完成参数绑定且可用于构造请求 URI 的路由路径。</para>
        /// <para>en-us:The parameter-bound route path that can be used to construct the request URI.</para>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <para>zh-cn:路由元数据或路由模板为空时抛出。</para>
        /// <para>en-us:Thrown when route metadata or its route template is null.</para>
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// <para>zh-cn:必填参数缺失、可选参数无法作为独立路径段省略，或模板存在未渲染占位符时抛出。</para>
        /// <para>en-us:Thrown when a required parameter is missing, an optional parameter cannot be omitted as an entire path segment, or the template contains an unrendered placeholder.</para>
        /// </exception>
        public static string Bind(TaxinRouteInformation routeInformation, object data)
        {
            ArgumentNullException.ThrowIfNull(routeInformation);
            ArgumentNullException.ThrowIfNull(routeInformation.Route);

            var values = CreateValueMap(data);
            var segments = routeInformation.Route.Split('/', StringSplitOptions.None);
            var renderedSegments = new List<string>(segments.Length);

            foreach (var segment in segments)
            {
                var wholeSegmentParameter = RouteParameterRegex.Match(segment);
                var omitSegment = false;
                var renderedSegment = RouteParameterRegex.Replace(segment, match =>
                {
                    var parameterName = match.Groups["name"].Value;
                    var isOptional = match.Groups["optional"].Success || IsOptional(routeInformation, parameterName);
                    if (!values.TryGetValue(parameterName, out var value) || value == null || string.IsNullOrWhiteSpace(ConvertToString(value)))
                    {
                        if (isOptional && wholeSegmentParameter.Success && wholeSegmentParameter.Value == match.Value)
                        {
                            omitSegment = true;
                            return string.Empty;
                        }

                        throw new InvalidOperationException($"Taxin route '{routeInformation.Route}' requires parameter '{parameterName}'.");
                    }

                    return EscapePathValue(ConvertToString(value), match.Groups["catchAll"].Success && match.Groups["catchAll"].Value.Length > 0);
                });

                if (!omitSegment)
                {
                    renderedSegments.Add(renderedSegment);
                }
            }

            var route = string.Join('/', renderedSegments);
            if (route.Contains('{') || route.Contains('}'))
            {
                throw new InvalidOperationException($"Taxin route '{routeInformation.Route}' contains an unrendered route parameter.");
            }

            return route;
        }

        private static bool IsOptional(TaxinRouteInformation routeInformation, string parameterName)
        {
            return routeInformation.Parameters?.Any(parameter =>
                string.Equals(parameter.Name, parameterName, StringComparison.OrdinalIgnoreCase) && parameter.IsOptional) == true;
        }

        private static Dictionary<string, object> CreateValueMap(object data)
        {
            var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (data == null)
            {
                return values;
            }

            if (data is IDictionary dictionary)
            {
                foreach (DictionaryEntry entry in dictionary)
                {
                    if (entry.Key is string key)
                    {
                        values[key] = entry.Value;
                    }
                }

                return values;
            }

            foreach (var property in data.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (property.CanRead && property.GetIndexParameters().Length == 0)
                {
                    values[property.Name] = property.GetValue(data);
                }
            }

            return values;
        }

        private static string ConvertToString(object value)
        {
            return value is IFormattable formattable
                ? formattable.ToString(null, CultureInfo.InvariantCulture)
                : value.ToString();
        }

        private static string EscapePathValue(string value, bool isCatchAll)
        {
            if (!isCatchAll)
            {
                return Uri.EscapeDataString(value);
            }

            return string.Join('/', value.Split('/', StringSplitOptions.None).Select(Uri.EscapeDataString));
        }
    }
}
