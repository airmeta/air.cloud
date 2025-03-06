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
using Air.Cloud.Core.Extensions.Aspect;
using Air.Cloud.Core.Modules.AppAspect.Attributes;
using Air.Cloud.Core.Plugins.PID;
using Air.Cloud.Core.Plugins.Security.MD5;

namespace Air.Cloud.Core.Plugins.DefaultDependencies
{
    /// <summary>
    /// PID Provider
    /// </summary>
    public class DefaultPIDPluginDependency : IPIDPlugin
    {
        private static Object locker = new Object();
        private  static string SPID=null;
        /// <summary>
        /// 写入PID
        /// </summary>
        /// <param name="PID">写入值 可为空 为空系统自动生成</param>
        /// <remarks>
        /// </remarks>
        public string Set(string PID = null)
        {
            //此处放置同步执行的代码
            if (!File.Exists(IPIDPlugin.StartPath))
            {
                File.Create(IPIDPlugin.StartPath).Close();
            }
            PID = PID ?? MD5Encryption.GetMd5By32(IPIDPlugin.StartPath);
            using (StreamWriter file = new StreamWriter(IPIDPlugin.StartPath))
            {
                file.Write(PID);
                file.Close();
            }
            DefaultPIDPluginDependency.SPID = PID;
            return PID;
        }

        /// <summary>
        /// 读取PID
        /// </summary>
        /// <returns>PID</returns>
        [Aspect(typeof(ExecuteMethodPrinterAspect))]
        public string Get()
        {
            lock (locker)
            {
                if (SPID.IsNullOrEmpty())
                {
                    return Set();
                }
                else
                {
                    return SPID;
                }
            }
        }
    }
}
