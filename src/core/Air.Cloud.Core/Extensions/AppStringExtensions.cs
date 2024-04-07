/*
 * Copyright (c) 2024 星曳数据
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * This file is provided under the Mozilla Public License Version 2.0,
 * and the "NO WARRANTY" clause of the MPL is hereby expressly
 * acknowledged.
 */

using System.Text.RegularExpressions;

namespace Air.Cloud.Core.Extensions
{
    public static partial class Extensions
    {
        /// <summary>
        /// 模板正则表达式
        /// </summary>
        private const string commonTemplatePattern = @"\{(?<p>.+?)\}";

        /// <summary>
        /// 读取配置模板正则表达式
        /// </summary>
        private const string configTemplatePattern = @"\#\((?<p>.*?)\)";
        /// <summary>
        /// 渲染模板
        /// </summary>
        /// <param name="template"></param>
        /// <param name="templateData"></param>
        /// <param name="encode"></param>
        /// <returns></returns>
        public static string Render(this string template, IDictionary<string, object> templateData, bool encode = false)
        {
            if (template == null) return default;

            // 如果模板为空，则跳过
            if (templateData == null || templateData.Count == 0) return template;

            // 判断字符串是否包含模板
            if (!Regex.IsMatch(template, commonTemplatePattern)) return template;

            // 获取所有匹配的模板
            var templateValues = Regex.Matches(template, commonTemplatePattern)
                                                       .Select(u => new {
                                                           Template = u.Groups["p"].Value,
                                                           Value = MatchTemplateValue(u.Groups["p"].Value, templateData)
                                                       });

            // 循环替换模板
            foreach (var item in templateValues)
            {
                template = template.Replace($"{{{item.Template}}}", encode ? Uri.EscapeDataString(item.Value?.ToString() ?? string.Empty) : item.Value?.ToString());
            }

            return template;
        }

        /// <summary>
        /// 从配置中渲染字符串模板
        /// </summary>
        /// <param name="template"></param>
        /// <param name="encode"></param>
        /// <returns></returns>
        public static string Render(this string template, bool encode = false)
        {
            if (template == null) return default;

            // 判断字符串是否包含模板
            if (!Regex.IsMatch(template, configTemplatePattern)) return template;

            // 获取所有匹配的模板
            var templateValues = Regex.Matches(template, configTemplatePattern)
                                                       .Select(u => new {
                                                           Template = u.Groups["p"].Value,
                                                           Value = AppConfiguration.Configuration[u.Groups["p"].Value]
                                                       });

            // 循环替换模板
            foreach (var item in templateValues)
            {
                template = template.Replace($"#({item.Template})", encode ? Uri.EscapeDataString(item.Value?.ToString() ?? string.Empty) : item.Value?.ToString());
            }

            return template;
        }
        /// <summary>
        /// 匹配模板值
        /// </summary>
        /// <param name="template"></param>
        /// <param name="templateData"></param>
        /// <returns></returns>
        private static object MatchTemplateValue(string template, IDictionary<string, object> templateData)
        {
            string tmpl;
            if (!template.Contains('.', StringComparison.CurrentCulture)) tmpl = template;
            else tmpl = template.Split('.', StringSplitOptions.RemoveEmptyEntries).First();

            var templateValue = templateData.ContainsKey(tmpl) ? templateData[tmpl] : default;
            return ResolveTemplateValue(template, templateValue);
        }

        /// <summary>
        /// 解析模板的值
        /// </summary>
        /// <param name="template"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private static object ResolveTemplateValue(string template, object data)
        {
            // 根据 . 分割模板
            var propertyCrumbs = template.Split('.', StringSplitOptions.RemoveEmptyEntries);
            return GetValue(propertyCrumbs, data);

            // 静态本地函数
            static object GetValue(string[] propertyCrumbs, object data)
            {
                if (data == null || propertyCrumbs == null || propertyCrumbs.Length <= 1) return data;
                var dataType = data.GetType();

                // 如果是基元类型则直接返回
                if (dataType.IsRichPrimitive()) return data;
                object value = null;

                // 递归获取下一级模板值
                for (var i = 1; i < propertyCrumbs.Length; i++)
                {
                    var propery = dataType.GetProperty(propertyCrumbs[i]);
                    if (propery == null) break;

                    value = propery.GetValue(data);
                    if (i + 1 < propertyCrumbs.Length)
                    {
                        value = GetValue(propertyCrumbs.Skip(i).ToArray(), value);
                    }
                    else break;
                }

                return value;
            }
        }


        /// <summary>
        /// 首字母小写
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string ToLowerCamelCase(this string str)
        {
            if (string.IsNullOrWhiteSpace(str)) return str;

            return string.Concat(str.First().ToString().ToLower(), str.AsSpan(1));
        }


        /// <summary>
        /// 清除字符串前后缀
        /// </summary>
        /// <param name="str">字符串</param>
        /// <param name="pos">0：前后缀，1：后缀，-1：前缀</param>
        /// <param name="affixes">前后缀集合</param>
        /// <returns></returns>
        public static string ClearStringAffixes(this string str, int pos = 0, params string[] affixes)
        {
            // 空字符串直接返回
            if (string.IsNullOrWhiteSpace(str)) return str;

            // 空前后缀集合直接返回
            if (affixes == null || affixes.Length == 0) return str;

            var startCleared = false;
            var endCleared = false;

            string tempStr = null;
            foreach (var affix in affixes)
            {
                if (string.IsNullOrWhiteSpace(affix)) continue;

                if (pos != 1 && !startCleared && str.StartsWith(affix, StringComparison.OrdinalIgnoreCase))
                {
                    tempStr = str[affix.Length..];
                    startCleared = true;
                }
                if (pos != -1 && !endCleared && str.EndsWith(affix, StringComparison.OrdinalIgnoreCase))
                {
                    var _tempStr = !string.IsNullOrWhiteSpace(tempStr) ? tempStr : str;
                    tempStr = _tempStr[..^affix.Length];
                    endCleared = true;
                }
                if (startCleared && endCleared) break;
            }

            return !string.IsNullOrWhiteSpace(tempStr) ? tempStr : str;
        }


        /// <summary>
        /// 格式化字符串
        /// </summary>
        /// <param name="str"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string Format(this string str, params object[] args)
        {
            return args == null || args.Length == 0 ? str : string.Format(str, args);
        }

        /// <summary>
        /// 切割骆驼命名式字符串
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string[] SplitCamelCase(this string str)
        {
            if (str == null) return Array.Empty<string>();

            if (string.IsNullOrWhiteSpace(str)) return new string[] { str };
            if (str.Length == 1) return new string[] { str };

            return Regex.Split(str, @"(?=\p{Lu}\p{Ll})|(?<=\p{Ll})(?=\p{Lu})")
                .Where(u => u.Length > 0)
                .ToArray();
        }
    }
}
