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
namespace Air.Cloud.Core.Extensions.GrpcServer.Options
{
    /// <summary>
    /// <para>zh-cn:Grpc服务配置项</para>
    /// <para>en-us:Grpc service configuration options</para>
    /// </summary>
    public class GrpcServiceOptions
    {
        /// <summary>
        /// GRPC 服务端口
        /// </summary>
        public int Port { get; set; }
    }
}
