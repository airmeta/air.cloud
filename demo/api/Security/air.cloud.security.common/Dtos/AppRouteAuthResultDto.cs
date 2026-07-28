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
using air.cloud.security.common.Enums;

namespace air.cloud.security.common.Dtos
{
    public  class AppRouteAuthResultDto
    {

        /// <summary>
        /// <para>zh-cn:Id</para>
        /// <para>en-us:Id</para>
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// <para>zh-cn:应用Id</para>
        /// <para>en-us:App Id</para>
        /// </summary>
        public string AppId { get; set; }

        /// <summary>
        /// <para>zh-cn:应用名称</para>
        /// <para>en-us:App Name</para>
        /// </summary>
        public string AppName { get; set; }

        /// <summary>
        /// <para>zh-cn:路由地址</para>
        /// <para>en-us:Route</para>
        /// </summary>
        public string Route { get; set; }

        /// <summary>
        /// <para>zh-cn:路由描述</para>
        /// <para>en-us:Route Description</para>
        /// </summary>
        public string RouteDescription {  get; set; }

        /// <summary>
        /// <para>zh-cn:描述</para>
        /// <para>en-us:Description</para>
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// <para>zh-cn:创建时间</para>
        /// <para>en-us:Create Time</para>
        /// </summary>
        public DateTime? CreateTime { get; set; }
        /// <summary>
        /// <para>zh-cn:授权时间</para>
        /// <para>en-us:Authorization Time</para>
        /// </summary>
        public DateTime? AuthTime { get; set; }
        /// <summary>
        /// <para>zh-cn:应用是否被删除</para>
        /// <para>en-us:Whether the App is deleted</para>
        /// </summary>
        public IsOrNotEnum? AppIsDelete { get; set; }
        /// <summary>
        /// <para>zh-cn:应用是否启用</para>
        /// <para>en-us:Whether the App is enabled</para>
        /// </summary>
        public IsOrNotEnum? AppIsEnable { get; set; }
        /// <summary>
        /// <para>zh-cn:是否允许匿名访问</para>
        /// <para>en-us:Whether to allow anonymous access</para>
        /// </summary>
        public IsOrNotEnum? AllowAnonymous { get; set; }
        /// <summary>
        /// <para>zh-cn:是否需要授权</para>
        /// <para>en-us:Whether authorization is required</para>
        /// </summary>
        public IsOrNotEnum? RequiresAuthorization { get; set; }
        /// <summary>
        /// <para>zh-cn:授权元数据</para>
        /// <para>en-us:Authorization Meta</para>
        /// </summary>
        public string AuthorizationMeta { get; set; }
        /// <summary>
        /// <para>zh-cn:是否自动创建</para>
        /// <para>en-us:Whether to auto-create</para>
        /// </summary>
        public IsOrNotEnum IsAutoCreate { get; set; }
        /// <summary>
        /// <para>zh-cn:请求方法</para>
        /// <para>en-us:Request Method</para>
        /// </summary>
        public string Method { get; set; }
        /// <summary>
        /// <para>zh-cn:是否已授权</para>
        /// <para>en-us:Whether it is authorized</para>
        /// </summary>
        public bool IsBind { get; set; }
    }
}
