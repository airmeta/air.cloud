
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
using Microsoft.AspNetCore.Authorization;

namespace Air.Cloud.Core.Attributes
{
    /// <summary>
    /// 网络请求服务标记
    /// </summary>
    public class NetworkServiceAttribute : AllowAnonymousAttribute
    {
        public string ServiceName { get; set; } = string.Empty;

        public NetworkServiceAttribute(string serviceName)
        {
            ServiceName = serviceName;
        }
        public NetworkServiceAttribute()
        {
            ServiceName = string.Empty;
        }
    }
}
