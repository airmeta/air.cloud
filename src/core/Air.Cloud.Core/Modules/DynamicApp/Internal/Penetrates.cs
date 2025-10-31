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

using Air.Cloud.Core.Modules.DynamicApp.Attributes;
using Air.Cloud.Core.Standard.DynamicServer;

using Microsoft.AspNetCore.Mvc;

using System.Collections.Concurrent;
using System.Reflection;

namespace Air.Cloud.Core.Modules.DynamicApp.Internal;

/// <summary>
/// 常量、公共方法配置类
/// </summary>
public static class Penetrates
{
    /// <summary>
    /// 分组分隔符
    /// </summary>
    public const string GroupSeparator = "@";

    /// <summary>
    /// 请求动词映射字典
    /// </summary>
    public static ConcurrentDictionary<string, string> VerbToHttpMethods { get; private set; }

    /// <summary>
    /// 控制器排序集合
    /// </summary>
    public static ConcurrentDictionary<string, int> ControllerOrderCollection { get; set; }

    /// <summary>
    /// 构造函数
    /// </summary>
    static Penetrates()
    {
        ControllerOrderCollection = new ConcurrentDictionary<string, int>();

        VerbToHttpMethods = new ConcurrentDictionary<string, string>
        {
            ["post"] = "POST",
            ["add"] = "POST",
            ["create"] = "POST",
            ["insert"] = "POST",
            ["submit"] = "POST",

            ["get"] = "GET",
            ["find"] = "GET",
            ["fetch"] = "GET",
            ["query"] = "GET",
            //["getlist"] = "GET",
            //["getall"] = "GET",

            ["put"] = "PUT",
            ["update"] = "PUT",

            ["delete"] = "DELETE",
            ["remove"] = "DELETE",
            ["clear"] = "DELETE",

            ["patch"] = "PATCH"
        };

        IsApiControllerCached = new ConcurrentDictionary<Type, bool>();
    }

    /// <summary>
    /// <see cref="IsApiController(Type)"/> 缓存集合
    /// </summary>
    private static readonly ConcurrentDictionary<Type, bool> IsApiControllerCached;

    /// <summary>
    /// 是否是Api控制器
    /// </summary>
    /// <param name="type">type</param>
    /// <returns></returns>
    public static bool IsApiController(Type type)
    {
        return IsApiControllerCached.GetOrAdd(type, CheckAPIType);
    }
    
    private static bool CheckAPIType(Type type)
    {
        // 不能是非公开、基元类型、值类型、抽象类、接口、泛型类
        if (!type.IsPublic || type.IsPrimitive || type.IsValueType || type.IsAbstract || type.IsInterface || type.IsGenericType) return false;

        // 继承 ControllerBase 或 实现 IDynamicApiController 的类型 或 贴了 [DynamicApiController] 特性
        if (!typeof(Controller).IsAssignableFrom(type) && typeof(ControllerBase).IsAssignableFrom(type) || typeof(IDynamicService).IsAssignableFrom(type) || type.IsDefined(typeof(DynamicApiControllerAttribute), true))
        {
            // 不是能被导出忽略的接口
            if (type.IsDefined(typeof(ApiExplorerSettingsAttribute), true) && type.GetCustomAttribute<ApiExplorerSettingsAttribute>(true).IgnoreApi) return false;

            return true;
        }
        return false;
    }
}