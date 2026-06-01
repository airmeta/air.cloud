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

namespace Air.Cloud.Core.Modules.AppAssembly.Builder
{
    /// <summary>
    /// <para>zh-cn:提供应用程序集构建器工厂方法，用于按程序集文件路径或程序集名称创建和获取装载构建器。</para>
    /// <para>en-us:Provides factory methods for application assembly builders, used to create and retrieve load builders by assembly file path or assembly name.</para>
    /// </summary>
    public class AppAssemblyBuilderFactory
    {
        /// <summary>
        /// <para>zh-cn:根据程序集文件路径创建新的程序集构建器，并初始化程序集装载上下文。</para>
        /// <para>en-us:Creates a new assembly builder by assembly file path and initializes the assembly load context.</para>
        /// </summary>
        /// <param name="AssemblyFilePath"><para>zh-cn:程序集文件路径。</para><para>en-us:The assembly file path.</para></param>
        /// <returns><para>zh-cn:初始化后的程序集构建器。</para><para>en-us:The initialized assembly builder.</para></returns>
        public static IAppAssemblyBuilder Create(string AssemblyFilePath)
        {
            var BuilderNew = new AppAssemblyBuilder();
            BuilderNew.AssemblyFilePath = AssemblyFilePath;
            BuilderNew.InitializeAssemblyLoadContext();
            IAppAssemblyBuilder.AppAssemblyBuilderPool.Set(BuilderNew);
            return BuilderNew;
        }
        /// <summary>
        /// <para>zh-cn:根据程序集名称从构建器池中获取程序集构建器；不存在时创建并缓存新的构建器。</para>
        /// <para>en-us:Gets an assembly builder from the builder pool by assembly name; when not found, creates and caches a new builder.</para>
        /// </summary>
        /// <param name="Name"><para>zh-cn:程序集名称信息。</para><para>en-us:The assembly name information.</para></param>
        /// <returns><para>zh-cn:匹配的程序集构建器。</para><para>en-us:The matched assembly builder.</para></returns>
        public static IAppAssemblyBuilder Get(AssemblyName Name)
        {
            var Builder= IAppAssemblyBuilder.AppAssemblyBuilderPool.Get(Name.Name);
            if (Builder == null)
            {
                Builder = new AppAssemblyBuilder();
                Builder.MainAssemblyName = Name;
                IAppAssemblyBuilder.AppAssemblyBuilderPool.Set(Builder);
            }
            return Builder;
        }
    }
}
