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
using System.Runtime.Loader;

namespace Air.Cloud.Core.Modules.AppAssembly.Model
{
    public  class AssemblyLoadContextInformation
    {
    
        public string Name { get; set; }

        public string AssemblyPath { get; set; }

        public AssemblyName AssemblyName { get; set; }

        public Assembly Assembly{ get; set; }

        public AssemblyLoadContext Context { get; set; }

        public DateTime LoadTime { get; set; }
    }
}
