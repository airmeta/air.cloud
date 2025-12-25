namespace Air.Cloud.Core.Plugins.Router
{
    /// <summary>
    /// 核心路由匹配器（无Web依赖，仅支持基础匹配）
    /// </summary>
    public class UniversalRouteMatcherCore: IRouterMatcherPlugin
    {
       
        #region 核心匹配逻辑
        /// <summary>
        /// 基础路由匹配（支持{参数名}、*通配符）
        /// </summary>
        /// <param name="routeTemplate">路由模板</param>
        /// <param name="requestPath">请求路径</param>
        /// <returns>匹配结果</returns>
        public bool Match(string routeTemplate, string requestPath)
        {
            // 空值校验
            if (string.IsNullOrWhiteSpace(routeTemplate) || string.IsNullOrWhiteSpace(requestPath))
            {
                return false;
            }
            try
            {
                // 预处理路径：去除首尾斜杠，统一格式
                var templateSegments = NormalizePath(routeTemplate).Split('/', StringSplitOptions.RemoveEmptyEntries);
                var requestSegments = NormalizePath(requestPath).Split('/', StringSplitOptions.RemoveEmptyEntries);

                // 路径段数量不一致（非通配符场景）直接不匹配
                if (templateSegments.Length != requestSegments.Length && !templateSegments.Contains("*"))
                {
                    return false;
                }
                for (int i = 0; i < templateSegments.Length; i++)
                {
                    var templateSegment = templateSegments[i];

                    // 通配符匹配剩余所有段
                    if (templateSegment == "*")
                    {
                        return true;
                    }
                    // 超出请求段长度，不匹配
                    if (i >= requestSegments.Length)
                    {
                        return false;
                    }
                    var requestSegment = requestSegments[i];

                    // 匹配路径参数段（{参数名}格式，兼容{id:int}但忽略约束）
                    if (IsParameterSegment(templateSegment))
                    {
                        var paramName = ExtractParameterName(templateSegment);
                        continue;
                    }
                    // 普通段必须精确匹配（忽略大小写）
                    if (!string.Equals(templateSegment, requestSegment, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
                // 模板段遍历完，请求段也必须遍历完（非通配符场景）
                return  templateSegments.Contains("*") || requestSegments.Length == templateSegments.Length;
            }
            catch
            {
                return false;
            }
        }
        #endregion

        #region 私有工具方法
        /// <summary>
        /// 标准化路径：去除首尾斜杠，替换多斜杠为单斜杠
        /// </summary>
        private static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return string.Empty;
            var normalized = System.Text.RegularExpressions.Regex.Replace(path.Trim(), @"/+", "/");
            return normalized.Trim('/');
        }

        /// <summary>
        /// 判断是否是参数段（{xxx}格式）
        /// </summary>
        private static bool IsParameterSegment(string segment)
        {
            return segment.StartsWith("{") && segment.EndsWith("}") && segment.Length > 2;
        }

        /// <summary>
        /// 提取参数名（兼容{id:int}，只取id）
        /// </summary>
        private static string ExtractParameterName(string segment)
        {
            return segment.Trim('{', '}').Split(':')[0];
        }
        #endregion
    }
}
