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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace Air.Cloud.Core.Extensions;

/// <summary>
/// <para>zh-cn:Air.Cloud 本地配置目录加载扩展。</para>
/// <para>en-us:Air.Cloud local configuration-directory loading extensions.</para>
/// </summary>
public static class AirCloudConfigurationBuilderExtensions
{
    /// <summary>
    /// <para>zh-cn:从应用运行目录下的配置目录加载 JSON 配置文件，文件按名称升序加载；环境子目录会在基础目录之后加载，因此可以覆盖基础配置。该方法只处理配置加载顺序，不保存密钥、用户、会话或运行期数据。</para>
    /// <para>en-us:Loads JSON configuration files from a configuration directory under the application base directory. Files are loaded by ascending file name; the environment subdirectory is loaded after the base directory so it can override base values. This method only controls configuration loading order and does not store secrets, users, sessions, or runtime data.</para>
    /// </summary>
    /// <param name="builder">
    /// <para>zh-cn:要追加配置源的配置构建器。</para>
    /// <para>en-us:The configuration builder that receives the configuration sources.</para>
    /// </param>
    /// <param name="directoryName">
    /// <para>zh-cn:配置目录名称或绝对路径。相对路径按应用运行目录解析，默认 config。</para>
    /// <para>en-us:The configuration directory name or absolute path. Relative paths are resolved from the application base directory; the default is config.</para>
    /// </param>
    /// <param name="environmentName">
    /// <para>zh-cn:可选环境名称。提供后会额外加载 config/{Environment} 目录中的 JSON 文件。</para>
    /// <para>en-us:Optional environment name. When supplied, JSON files under config/{Environment} are loaded additionally.</para>
    /// </param>
    /// <param name="optional">
    /// <para>zh-cn:目录不存在时是否忽略。默认 true，适合可选能力配置。</para>
    /// <para>en-us:Whether missing directories are ignored. The default is true, which is suitable for optional capability configuration.</para>
    /// </param>
    /// <param name="reloadOnChange">
    /// <para>zh-cn:文件变更时是否触发配置 reload，默认 true。</para>
    /// <para>en-us:Whether file changes trigger configuration reloads. The default is true.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:原配置构建器，便于链式调用。</para>
    /// <para>en-us:The original configuration builder for chaining.</para>
    /// </returns>
    /// <exception cref="DirectoryNotFoundException">
    /// <para>zh-cn:当 optional 为 false 且基础目录不存在时抛出。</para>
    /// <para>en-us:Thrown when optional is false and the base directory does not exist.</para>
    /// </exception>
    public static IConfigurationBuilder AddAirCloudConfigDirectory(
        this IConfigurationBuilder builder,
        string directoryName = "config",
        string? environmentName = null,
        bool optional = true,
        bool reloadOnChange = true)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var baseDirectory = ResolveDirectory(directoryName);
        AddDirectory(builder, baseDirectory, optional, reloadOnChange);

        if (!string.IsNullOrWhiteSpace(environmentName))
        {
            AddDirectory(builder, Path.Combine(baseDirectory, environmentName.Trim()), true, reloadOnChange);
        }

        return builder;
    }

    /// <summary>
    /// <para>zh-cn:解析 Air.Cloud 配置目录中的 JSON 配置文件路径，返回值按最终加载顺序排列。</para>
    /// <para>en-us:Resolves JSON configuration files in an Air.Cloud configuration directory and returns them in the final loading order.</para>
    /// </summary>
    /// <param name="directoryName">
    /// <para>zh-cn:配置目录名称或绝对路径。相对路径按应用运行目录解析。</para>
    /// <para>en-us:The configuration directory name or absolute path. Relative paths are resolved from the application base directory.</para>
    /// </param>
    /// <param name="environmentName">
    /// <para>zh-cn:可选环境名称。提供后会把环境子目录中的 JSON 文件追加到结果末尾。</para>
    /// <para>en-us:Optional environment name. When supplied, JSON files in the environment subdirectory are appended to the result.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:按名称升序排列的 JSON 配置文件绝对路径。</para>
    /// <para>en-us:Absolute JSON configuration file paths ordered by ascending file name.</para>
    /// </returns>
    public static IReadOnlyList<string> GetAirCloudConfigDirectoryFiles(string directoryName = "config", string? environmentName = null)
    {
        var baseDirectory = ResolveDirectory(directoryName);
        var files = new List<string>();
        files.AddRange(EnumerateJsonFiles(baseDirectory));

        if (!string.IsNullOrWhiteSpace(environmentName))
        {
            files.AddRange(EnumerateJsonFiles(Path.Combine(baseDirectory, environmentName.Trim())));
        }

        return files;
    }

    private static void AddDirectory(IConfigurationBuilder builder, string directory, bool optional, bool reloadOnChange)
    {
        if (!Directory.Exists(directory))
        {
            if (optional)
            {
                return;
            }

            throw new DirectoryNotFoundException($"Air.Cloud configuration directory was not found: {directory}");
        }

        var provider = new PhysicalFileProvider(directory);
        foreach (var file in EnumerateJsonFiles(directory))
        {
            builder.AddJsonFile(provider, Path.GetFileName(file), optional: true, reloadOnChange);
        }
    }

    private static IEnumerable<string> EnumerateJsonFiles(string directory)
    {
        return Directory.Exists(directory)
            ? Directory.EnumerateFiles(directory, "*.json", SearchOption.TopDirectoryOnly)
                .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            : [];
    }

    private static string ResolveDirectory(string? directoryName)
    {
        var value = string.IsNullOrWhiteSpace(directoryName) ? "config" : directoryName.Trim();
        return Path.IsPathRooted(value)
            ? Path.GetFullPath(value)
            : Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, value));
    }
}
