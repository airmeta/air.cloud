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
using Air.Cloud.Core.Modules.DynamicApp.Internal;

using Microsoft.AspNetCore.Mvc;

namespace Air.Cloud.WebApp.DynamicApiController.Attributes;

/// <summary>
/// 接口描述设置
/// </summary>
[IgnoreScanning, AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Method)]
public sealed class ApiDescriptionSettingsAttribute : ApiExplorerSettingsAttribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public ApiDescriptionSettingsAttribute() : base()
    {
        Order = 0;
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="enabled">是否启用</param>
    public ApiDescriptionSettingsAttribute(bool enabled) : base()
    {
        IgnoreApi = !enabled;
        Order = 0;
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="groups">分组列表</param>
    public ApiDescriptionSettingsAttribute(params string[] groups) : base()
    {
        GroupName = string.Join(Penetrates.GroupSeparator, groups);
        Groups = groups;
        Order = 0;
    }

    /// <summary>
    /// 自定义名称
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 保留原有名称（Boolean 类型）
    /// </summary>
    public object KeepName { get; set; }

    /// <summary>
    /// 切割骆驼命名（Boolean 类型）
    /// </summary>
    public object SplitCamelCase { get; set; }

    /// <summary>
    /// 小驼峰命名（首字符小写）
    /// </summary>
    public object AsLowerCamelCase { get; set; }

    /// <summary>
    /// 保留路由谓词（Boolean 类型）
    /// </summary>
    public object KeepVerb { get; set; }

    /// <summary>
    /// 小写路由（Boolean 类型）
    /// </summary>
    public object LowercaseRoute { get; set; }

    /// <summary>
    /// 模块名
    /// </summary>
    public string Module { get; set; }

    /// <summary>
    /// 版本号
    /// </summary>
    public string Version { get; set; }

    /// <summary>
    /// 分组
    /// </summary>
    public string[] Groups { get; set; }

    /// <summary>
    /// 标签
    /// </summary>
    public string Tag { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 配置控制器区域（只对控制器有效）
    /// </summary>
    public string Area { get; set; }

    /// <summary>
    /// 额外描述，支持 HTML
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// 强制携带路由前缀，即使使用 [Route] 重写，仅对 Class/Controller 有效
    /// </summary>
    public object ForceWithRoutePrefix { get; set; }
}