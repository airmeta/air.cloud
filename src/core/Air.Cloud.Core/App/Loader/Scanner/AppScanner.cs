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
using Microsoft.AspNetCore.Hosting;

using System.Collections.Concurrent;
using System.Reflection;
namespace Air.Cloud.Core.App.Loader.Scanner
{
    public class AppScanner
    {
        //扫描所有继承IStartUp的类 改类为StartUp启动类
        public static ConcurrentBag<IStartup> Scanning()
        {
            ConcurrentBag<IStartup> StartupBag = new ConcurrentBag<IStartup>();
            //
            var startups = AppCore.CrucialTypes
                    .Where(u => typeof(IStartup).IsAssignableFrom(u) && u.IsClass && !u.IsAbstract && !u.IsGenericType);
            foreach (var item in startups)
            {
                var StartUpOrder = item.GetCustomAttribute<StartupAttribute>();
                var ConfigureMethod = typeof(IStartup).GetMethods().Where(s => s.Name == nameof(IStartup.Configure));
                var ConfigureServiceMethod = typeof(IStartup).GetMethods().Where(s => s.Name == nameof(IStartup.Configure));
            }
            return StartupBag;
        }
    }
}
