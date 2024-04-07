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

namespace Air.Cloud.Core.App.Loader
{
    /// <summary>
    /// 启动配置项扩展
    /// </summary>
    public interface IStartupExtensions : IStartup
    {
        /// <summary>
        /// 启动配置项
        /// </summary>
        public static ConcurrentBag<IStartup> Starts;

        static IStartupExtensions()
        {
            Starts = new ConcurrentBag<IStartup>();
        }
    }

    /// <summary>
    /// 启动配置项扩展实现
    /// </summary>
    public static class StartupExtensions
    {
        public static ConcurrentBag<IStartup> AddStartUp(this IStartup startUp)
        {
            IStartupExtensions.Starts.Add(startUp);
            return IStartupExtensions.Starts;
        }

        public static void Clear(this IStartup startUp)
        {
            IStartupExtensions.Starts.Clear();
        }
    }
}
