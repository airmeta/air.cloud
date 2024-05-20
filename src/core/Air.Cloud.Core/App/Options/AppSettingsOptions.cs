// Copyright (c) 2020-2022 百小僧, Baiqian Co.,Ltd.
// Furion is licensed under Mulan PSL v2.
// You can use this software according to the terms and conditions of the Mulan PSL v2.
// You may obtain a copy of Mulan PSL v2 at:
//             https://gitee.com/dotnetchina/Furion/blob/master/LICENSE
// THIS SOFTWARE IS PROVIDED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO NON-INFRINGEMENT, MERCHANTABILITY OR FIT FOR A PARTICULAR PURPOSE.
// See the Mulan PSL v2 for more details.
using Air.Cloud.Core.Standard.DefaultDependencies;

using Microsoft.Extensions.Configuration;

namespace Air.Cloud.Core.App.Options;

/// <summary>
/// 应用全局配置
/// </summary>
[ConfigurationInfo("AppSettings")]
public sealed class AppSettingsOptions : IConfigurableOptions<AppSettingsOptions>
{
    /// <summary>
    /// <para>zh-cn: 是否启用引用程序集扫描</para>
    /// <para>en-us: Whether to enable reference assembly scanning</para>
    /// </summary>
    public bool? EnabledReferenceAssemblyScan { get; set; } = false;

    /// <summary>
    /// <para>zh-cn: 是否输出原始 SQL 执行日志</para>
    /// <para>en-us: Whether to output the original SQL execution log</para>
    /// </summary>
    public bool? OutputOriginalSqlExecuteLog { get; set; } = true;

    /// <summary>
    /// <para>zh-cn: 网关地址</para>
    /// <para>en-us: Gateway address</para>
    /// </summary>
    /// <remarks>
    /// <para>zh-cn: 该配置暂时只会应用于微服务系统中</para>
    /// <para>en-us: This configuration will only be applied to microservice systems for the time being</para>
    /// </remarks>
    public string GateWayAddress { get; set; }

    /// <summary>
    /// <para>zh-cn:是否启用规范化文档</para>
    /// <para>en-us:Enable SpecificationDocument</para>
    /// </summary>
    /// <remarks>
    /// <para> zh-cn: 如果没有主动配置是否启用规范化文档，则生产环境默认关闭,其他环境默认开启</para>
    /// <para> en-us: If the configuration of whether to enable the specification document is not actively configured, it is turned off by default in the production environment, and turned on by default in other environments</para>
    /// </remarks>
    public bool? InjectSpecificationDocument { get; set; }

    /// <summary>
    /// 是否打印数据库连接信息
    /// </summary>
    public bool? PrintDbConnectionInfo { get; set; }

    /// <summary>
    /// 配置支持的包前缀名
    /// </summary>
    public string[] SupportPackageNamePrefixs { get; set; }
    /// <summary>
    /// <para>zh-cn:版本信息</para>
    /// <para>en-us:Version information</para>
    /// </summary>
    public string Version { get; set; } = "1.0.0.0";
    /// <summary>
    /// <para>zh-cn:序列化后的版本信息</para>
    /// <para>en-us:Serialize version information</para>
    /// </summary>
    public Version VersionSerialize => new Version(Version);
    /// <summary>
    /// 后期配置
    /// </summary>
    /// <param name="options"></param>
    /// <param name="configuration"></param>
    public void PostConfigure(AppSettingsOptions options, IConfiguration configuration)
    {
        options.InjectSpecificationDocument ??= (AppEnvironment.IsProduction?false:true);
        options.EnabledReferenceAssemblyScan ??= false;
        options.PrintDbConnectionInfo ??= (AppEnvironment.IsProduction ? false : true);
        options.OutputOriginalSqlExecuteLog ??= (AppEnvironment.IsProduction ? false : true);
        options.SupportPackageNamePrefixs ??= Array.Empty<string>();
        options.GateWayAddress??="http://localhost:5000";
    }
}