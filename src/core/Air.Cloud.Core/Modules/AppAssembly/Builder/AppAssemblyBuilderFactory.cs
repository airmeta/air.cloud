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
    public class AppAssemblyBuilderFactory
    {
        public static IAppAssemblyBuilder Create(string AssemblyFilePath)
        {
            var BuilderNew = new AppAssemblyBuilder();
            BuilderNew.AssemblyFilePath = AssemblyFilePath;
            BuilderNew.InitializeAssemblyLoadContext();
            IAppAssemblyBuilder.AppAssemblyBuilderPool.Set(BuilderNew);
            return BuilderNew;
        }
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
