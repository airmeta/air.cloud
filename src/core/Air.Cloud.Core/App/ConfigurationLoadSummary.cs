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
using Air.Cloud.Core.Modules.AppPrint;

using Microsoft.Extensions.Configuration;

namespace Air.Cloud.Core.App
{
    /// <summary>
    /// <para>zh-cn:输出配置源加载摘要，只记录来源定位和有效配置项数量，不输出配置值。</para>
    /// <para>en-us:Writes a configuration-source loading summary that records only source metadata and effective key counts, never configuration values.</para>
    /// </summary>
    public static class ConfigurationLoadSummary
    {
        /// <summary>
        /// <para>zh-cn:输出指定配置源的加载摘要。配置项数量仅统计具有值的键，因此不会暴露密码、连接字符串或其他配置内容。</para>
        /// <para>en-us:Writes a loading summary for a configuration source. The key count includes only keys with values and never exposes passwords, connection strings, or other configuration content.</para>
        /// </summary>
        /// <param name="source"><para>zh-cn:配置来源名称，例如本地文件、Consul 或 Nacos。</para><para>en-us:The configuration source name, such as local files, Consul, or Nacos.</para></param>
        /// <param name="endpoint"><para>zh-cn:配置源地址或本地配置目录；URI 用户信息会被移除。</para><para>en-us:The configuration-source endpoint or local configuration directory; URI user information is removed.</para></param>
        /// <param name="location"><para>zh-cn:配置文件路径或配置中心中的定位信息。</para><para>en-us:The configuration-file path or its location in the configuration center.</para></param>
        /// <param name="configuration"><para>zh-cn:已加载的单个配置源。</para><para>en-us:The individual loaded configuration source.</para></param>
        public static void Print(string source, string endpoint, string location, IConfiguration configuration)
        {
            AppRealization.Output.Print("配置加载", CreateContent(source, endpoint, location, configuration), AppPrintLevel.Information);
        }

        /// <summary>
        /// <para>zh-cn:创建配置加载摘要文本，便于测试或交由自定义输出标准处理。</para>
        /// <para>en-us:Creates a configuration loading summary for tests or custom output-standard handling.</para>
        /// </summary>
        /// <param name="source"><para>zh-cn:配置来源名称。</para><para>en-us:The configuration source name.</para></param>
        /// <param name="endpoint"><para>zh-cn:配置源地址或本地目录。</para><para>en-us:The configuration-source endpoint or local directory.</para></param>
        /// <param name="location"><para>zh-cn:配置文件或配置中心定位。</para><para>en-us:The configuration-file or configuration-center location.</para></param>
        /// <param name="configuration"><para>zh-cn:已加载的配置源。</para><para>en-us:The loaded configuration source.</para></param>
        /// <returns><para>zh-cn:不包含配置值的摘要文本。</para><para>en-us:Summary text with no configuration values.</para></returns>
        public static string CreateContent(string source, string endpoint, string location, IConfiguration configuration)
        {
            var keyCount = CountEffectiveKeys(configuration);
            var state = keyCount > 0 ? "加载到配置" : "配置源读取完成，未发现有效配置项";

            return $"{state}: 来源:{source}; 地址:{SanitizeEndpoint(endpoint)}; 配置定位:{location}; 有效配置项:{keyCount}个";
        }

        /// <summary>
        /// <para>zh-cn:统计配置源中具有值的有效键数量。节节点、空配置源和空值不会计入数量。</para>
        /// <para>en-us:Counts effective keys with values in a configuration source. Section nodes, empty sources, and null values are excluded.</para>
        /// </summary>
        /// <param name="configuration"><para>zh-cn:待统计的配置源。</para><para>en-us:The configuration source to count.</para></param>
        /// <returns><para>zh-cn:有效配置项数量。</para><para>en-us:The number of effective configuration keys.</para></returns>
        public static int CountEffectiveKeys(IConfiguration configuration)
        {
            return configuration?.AsEnumerable().Count(item => item.Value != null) ?? 0;
        }

        private static string SanitizeEndpoint(string endpoint)
        {
            if (string.IsNullOrWhiteSpace(endpoint)) return "未配置";
            if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri)
                || (!uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                    && !uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))) return endpoint;

            var builder = new UriBuilder(uri)
            {
                UserName = string.Empty,
                Password = string.Empty
            };
            return builder.Uri.GetLeftPart(UriPartial.Path);
        }
    }
}
