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

namespace Air.Cloud.Modules.SkyWalking.Const
{
    /// <summary>
    /// <para>zh-cn:Skyapm 常量</para>
    /// <para>en-us:Skyapm const </para>
    /// </summary>
    public static class SkyApmConst
    {
        /// <summary>
        /// <para>zh-cn:skywalking 配置文件名称</para>
        /// <para>en-us:skywalking config file name</para>
        /// </summary>
        public const string SKYWALKING_CONFIG_NAME = "skyapm.json";
        /// <summary>
        ///<para>zh-cn:环境变量 ASPNETCORE_HOSTINGSTARTUPASSEMBLIES</para>
        ///<para>en-us:Environment variables ASPNETCORE_HOSTINGSTARTUPASSEMBLIES</para>
        /// </summary>
        public const string ASPNETCORE_HOSTINGSTARTUPASSEMBLIES="ASPNETCORE_HOSTINGSTARTUPASSEMBLIES";
        /// <summary>
        /// <para>zh-cn:环境变量 ASPNETCORE_HOSTINGSTARTUPASSEMBLIES的值</para>
        /// <para>en-us:Environment variables ASPNETCORE_HOSTINGSTARTUPASSEMBLIES value</para>
        /// </summary>
        public const string ASPNETCORE_HOSTINGSTARTUPASSEMBLIES_VALUE = "SkyAPM.Agent.AspNetCore";
        /// <summary>
        /// <para>zh-cn:服务名称</para>
        /// <para>en-us:SKYWALKING__SERVICENAME</para>
        /// </summary>
        public const string SKYWALKING__SERVICENAME = "SKYWALKING__SERVICENAME";

    }
}
