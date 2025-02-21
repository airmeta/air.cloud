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
using System.Runtime.InteropServices;

namespace Air.Cloud.Modules.Docker.Helper
{
    /// <summary>
    /// <para>zh-cn:Docker 引擎Uri帮助类</para>
    /// <para>en-us:Docker engine uri helper class</para>
    /// </summary>
    public static  class DockerEngineUriHelper
    {
        /// <summary>
        /// 获取Docker引擎基础Uri
        /// </summary>
        /// <returns></returns>
        public static Uri GetEngineBaseUri()
        {
            return  RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                  ? new Uri("npipe://./pipe/docker_engine")
                  : new Uri("unix:/var/run/docker.sock");
        }

    }
}
