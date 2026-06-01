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
using Air.Cloud.Core.Extensions.IEnumerables;

using System.Reflection;

/// <summary>
/// <para>zh-cn:程序集类型加载扩展方法。</para>
/// <para>en-us:Extension methods for loading types from assemblies.</para>
/// </summary>
public static class AssemblyExtensions
{

    /// <summary>
    /// <para>zh-cn:加载程序集内满足指定条件的类型。</para>
    /// <para>en-us:Loads types from an assembly that satisfy the specified predicate.</para>
    /// </summary>
    /// <param name="assembly">
    /// <para>zh-cn:需要扫描的程序集。</para>
    /// <para>en-us:Assembly to scan.</para>
    /// </param>
    /// <param name="CheckTypeAction">
    /// <para>zh-cn:类型过滤条件。</para>
    /// <para>en-us:Type filter predicate.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:匹配条件的类型数组。</para>
    /// <para>en-us:Array of types that match the predicate.</para>
    /// </returns>
    public static Type[] LoadTypes(this Assembly assembly,Func<Type,bool> CheckTypeAction)
    {
        return assembly.GetTypes().Where(CheckTypeAction).ToArray();
    }

}
