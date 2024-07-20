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
using Docker.DotNet;

namespace Air.Cloud.Modules.Docker.Helper
{
    /// <summary>
    /// <para>zh-cn:引擎客户端链接帮助类</para>
    /// <para>en-us:Docker engine client helper</para>
    /// </summary>
    public static  class DockerEngineClientHelper
    {
        /// <summary>
        /// <para>zh-cn:Docker 引擎客户端链接</para>
        /// </summary>
        public static DockerClient Client => new DockerClientConfiguration(DockerEngineUriHelper.GetEngineBaseUri()).CreateClient();



    }
}
