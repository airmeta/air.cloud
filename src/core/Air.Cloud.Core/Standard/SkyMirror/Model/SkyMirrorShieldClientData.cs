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
namespace Air.Cloud.Core.Standard.SkyMirror.Model
{
    /// <summary>
    /// <para>zh-cn:天镜盾客户端数据模型</para>
    /// <para>en-us:Sky Mirror Shield Client Data Model</para>
    /// </summary>
    public class SkyMirrorShieldClientData
    {
        /// <summary>
        /// <para>zh-cn:客户端应用名称</para>
        /// <para>en-us:Client Application Name</para>
        /// </summary>
        public string ApplicationName { get; set; }

        /// <summary>
        /// <para>zh-cn:应用程序PID</para>
        /// <para>en-us:Application PID</para>
        /// </summary>
        public string ApplicationPID { get; set; }
        /// <summary>
        /// <para>zh-cn:端点信息</para>
        /// <para>en-us:Endpoint Information</para>
        /// </summary>
        public IList<EndpointData> EndpointDatas { get; set; }
    }
}
