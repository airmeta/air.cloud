
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
using Air.Cloud.Core.App.Options;

namespace Air.Cloud.Core.Standard.SkyMirror.Options
{
    [ConfigurationInfo("AuthenticaSettings")]
    public  class AuthenticaOptions
    {
        /// <summary>
        /// <para>zh-cn:服务端地址</para>
        /// <para>en-us:Server address</para>
        /// </summary>
        public string ServerAddress { get; set; }

        /// <summary>
        /// <para>zh-cn:服务端地址</para>
        /// <para>en-us:Server address</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:如果配置文件中没有配置服务端地址，则使用网关地址</para>
        /// <para>en-us:If the server address is not configured in the configuration file, the gateway address is used</para>
        /// </remarks>
        public string GetServerAddress() => string.IsNullOrEmpty(ServerAddress) ? AppConfiguration.GetConfig<AppSettingsOptions>().GateWayAddress : ServerAddress;
        public string PushRoute { get; set; }="/authenticaton/endpoint/push";

        public int StoreIntervalMillis{get;set; }= 60000;

        /// <summary>
        /// <para>zh-cn:重试间隔时间，单位毫秒</para>
        /// <para>en-us:Retry interval time in milliseconds</para>
        /// </summary>
        public int RetryIntervalMillis{get;set; }= 10000;

    }
}
