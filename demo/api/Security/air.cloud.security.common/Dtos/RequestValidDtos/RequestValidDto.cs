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
using Air.Cloud.Core.Standard.SkyMirror.Model;

namespace air.cloud.security.common.Dtos.RequestValidDtos
{
    public  class RequestValidDto
    {
        /// <summary>
        /// <para>zh-cn:请求地址</para>
        /// </summary>
        public string Path { get; set; }
        /// <summary>
        /// <para>zh-cn:请求头</para>
        /// </summary>
        public IDictionary<string, string> Headers { get; set; }

        /// <summary>
        /// <para>zh-cn:终结点信息</para>
        /// </summary>
        public EndpointData EndpointData { get; set; }

    }
}
