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
using System.Reflection;

namespace Air.Cloud.Core.Standard.Event.Extensions;

/// <summary>
/// 事件总线拓展类
/// </summary>
public static class EventExtensions
{
    /// <summary>
    /// 将事件枚举 Id 转换成字符串对象
    /// </summary>
    /// <param name="em"></param>
    /// <returns></returns>
    public static string ParseToString(this Enum em)
    {
        var enumType = em.GetType();
        return $"{enumType.Assembly.GetName().Name};{enumType.FullName}.{em}";
    }

    /// <summary>
    /// 将事件枚举字符串转换成枚举对象
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static Enum ParseToEnum(this string str)
    {
        var assemblyName = str[..str.IndexOf(';')];
        var fullName = str[(str.IndexOf(';') + 1)..str.LastIndexOf('.')];
        var name = str[(str.LastIndexOf('.') + 1)..];

        return Enum.Parse(Assembly.Load(assemblyName).GetType(fullName), name) as Enum;
    }
}