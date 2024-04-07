// Copyright (c) 2020-2022 百小僧, Baiqian Co.,Ltd.
// Furion is licensed under Mulan PSL v2.
// You can use this software according to the terms and conditions of the Mulan PSL v2.
// You may obtain a copy of Mulan PSL v2 at:
//             https://gitee.com/dotnetchina/Furion/blob/master/LICENSE
// THIS SOFTWARE IS PROVIDED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO NON-INFRINGEMENT, MERCHANTABILITY OR FIT FOR A PARTICULAR PURPOSE.
// See the Mulan PSL v2 for more details.
using Microsoft.Extensions.Configuration;

namespace Air.Cloud.Core.App.Options;

/// <summary>
/// 应用全局配置
/// </summary>
[ConfigurationInfo("AppSettings")]
public sealed class AppSettingsOptions : IConfigurableOptions<AppSettingsOptions>
{
    #region 该文件夹存放在根目录下 可以将相关插件类库的发布dll等放在该文件夹下,框架会自动加载并应用该类库信息
    /// <summary>
    /// 插件Dll文件夹
    /// </summary>
    public const string PLUGINS_DIR = "plugins";
    #endregion
    /// <summary>
    /// 是否启用引用程序集扫描
    /// </summary>
    public bool? EnabledReferenceAssemblyScan { get; set; } = false;

    /// <summary>
    /// 是否输出原始 Sql 执行日志（ADO.NET）
    /// </summary>
    public bool? OutputOriginalSqlExecuteLog { get; set; } = true;

    /// <summary>
    /// 网关地址
    /// </summary>
    public string GateWayAddress { get; set; }

    /// <summary>
    /// 是否启用规范化文档
    /// </summary>
    public bool? InjectSpecificationDocument { get; set; }

    /// <summary>
    /// 外部程序集
    /// </summary>
    /// <remarks>扫描 dll 文件，如果是单文件发布，需拷贝放在根目录下</remarks>
    public string[] ExternalAssemblies { get; set; }

    /// <summary>
    /// 是否打印数据库连接信息到 MiniProfiler 中
    /// </summary>
    public bool? PrintDbConnectionInfo { get; set; }

    /// <summary>
    /// 配置支持的包前缀名
    /// </summary>
    public string[] SupportPackageNamePrefixs { get; set; }
    /// <summary>
    /// 后期配置
    /// </summary>
    /// <param name="options"></param>
    /// <param name="configuration"></param>
    public void PostConfigure(AppSettingsOptions options, IConfiguration configuration)
    {
        options.InjectSpecificationDocument ??= true;
        options.EnabledReferenceAssemblyScan ??= false;
        options.ExternalAssemblies ??= Array.Empty<string>();
        options.PrintDbConnectionInfo ??= true;
        options.OutputOriginalSqlExecuteLog ??= true;
        options.SupportPackageNamePrefixs ??= Array.Empty<string>();
    }
}