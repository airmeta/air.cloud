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

using System.Reflection;
using System.Runtime.Loader;

namespace Air.Cloud.Core.Plugins.Reflection;

/// <summary>
/// 内部反射静态类
/// </summary>
public static class Reflect
{
    /// <summary>
    /// 获取入口程序集
    /// </summary>
    /// <returns></returns>
    public static Assembly GetEntryAssembly()
    {
        return Assembly.GetEntryAssembly();
    }

    /// <summary>
    /// 根据程序集名称获取运行时程序集
    /// </summary>
    /// <param name="assemblyName"></param>
    /// <returns></returns>
    public static Assembly GetAssembly(string assemblyName)
    {
        try
        {
            return AssemblyLoadContext.Default.LoadFromAssemblyName(new AssemblyName(assemblyName));
        }
        catch (Exception)
        {
            return default;
        }

    }
    /// <summary>
    /// 根据路径加载程序集
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static Assembly LoadAssembly(string path)
    {
        if (!File.Exists(path)) return default;
        return Assembly.LoadFrom(path);
    }

    /// <summary>
    /// 通过流加载程序集
    /// </summary>
    /// <param name="assembly"></param>
    /// <returns></returns>
    public static Assembly LoadAssembly(MemoryStream assembly)
    {
        return Assembly.Load(assembly.ToArray());
    }

    /// <summary>
    /// 根据程序集名称、类型完整限定名获取运行时类型
    /// </summary>
    /// <param name="assemblyName"></param>
    /// <param name="typeFullName"></param>
    /// <returns></returns>
    public static Type GetType(string assemblyName, string typeFullName)
    {
        return GetAssembly(assemblyName).GetType(typeFullName);
    }

    /// <summary>
    /// 根据程序集和类型完全限定名获取运行时类型
    /// </summary>
    /// <param name="assembly"></param>
    /// <param name="typeFullName"></param>
    /// <returns></returns>
    public static Type GetType(Assembly assembly, string typeFullName)
    {
        return assembly.GetType(typeFullName);
    }

    /// <summary>
    /// 根据程序集和类型完全限定名获取运行时类型
    /// </summary>
    /// <param name="assembly"></param>
    /// <param name="typeFullName"></param>
    /// <returns></returns>
    public static Type GetType(MemoryStream assembly, string typeFullName)
    {
        return LoadAssembly(assembly).GetType(typeFullName);
    }

    /// <summary>
    /// 获取程序集名称
    /// </summary>
    /// <param name="assembly"></param>
    /// <returns></returns>
    public static string GetAssemblyName(Assembly assembly)
    {
        return assembly.GetName().Name;
    }

    /// <summary>
    /// 获取程序集名称
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static string GetAssemblyName(Type type)
    {
        return GetAssemblyName(type.GetTypeInfo());
    }

    /// <summary>
    /// 获取程序集名称
    /// </summary>
    /// <param name="typeInfo"></param>
    /// <returns></returns>
    public static string GetAssemblyName(TypeInfo typeInfo)
    {
        return GetAssemblyName(typeInfo.Assembly);
    }

    /// <summary>
    /// 加载程序集类型，支持格式：程序集;网站类型命名空间
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static Type GetStringType(string str)
    {
        var typeDefinitions = str.Split(";");
        return GetType(typeDefinitions[0], typeDefinitions[1]);
    }
}