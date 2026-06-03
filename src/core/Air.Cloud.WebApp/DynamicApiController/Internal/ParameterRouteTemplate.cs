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
namespace Air.Cloud.WebApp.DynamicApiController.Internal;

/// <summary>
/// 参数路由模板
/// </summary>
internal class ParameterRouteTemplate
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public ParameterRouteTemplate()
    {
        ControllerStartTemplates = new List<string>();
        ControllerEndTemplates = new List<string>();
        ActionStartTemplates = new List<string>();
        ActionEndTemplates = new List<string>();
    }

    /// <summary>
    /// 控制器之前的参数
    /// </summary>
    public IList<string> ControllerStartTemplates { get; set; }

    /// <summary>
    /// 控制器之后的参数
    /// </summary>
    public IList<string> ControllerEndTemplates { get; set; }

    /// <summary>
    /// 行为之前的参数
    /// </summary>
    public IList<string> ActionStartTemplates { get; set; }

    /// <summary>
    /// 行为之后的参数
    /// </summary>
    public IList<string> ActionEndTemplates { get; set; }
}