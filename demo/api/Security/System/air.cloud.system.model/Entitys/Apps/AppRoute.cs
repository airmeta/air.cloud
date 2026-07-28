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
using air.cloud.security.common.Base;
using air.cloud.security.common.Enums;

using Air.Cloud.Core.Standard.SkyMirror.Model;

namespace air.cloud.system.model.Entitys.Apps
{
    /// <summary>
    /// 应用路由
    /// </summary>
    [Table("APP_ROUTE")]
    public  class AppRoute:CreateEntityBase
    {
        /// <summary>
        /// 应用编号
        /// </summary>
        [Column("APP_ID")]
        public string AppId { get; set; }
        /// <summary>
        /// 路由地址
        /// </summary>
        [Column("ROUTE")]
        public string Route { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        [Column("DESCRIPTION")]
        public string? Description { get; set; }


        /// <summary>
        /// <para>zh-cn:是否允许匿名访问</para>
        /// <para>en-us:Whether to allow anonymous access</para>
        /// </summary>
        [Column("ALLOW_ANONYMOUS")]
        public IsOrNotEnum? AllowAnonymous { get; set; }


        /// <summary>
        /// <para>zh-cn:是否需要授权访问</para>
        /// <para>en-us:Whether authorization is required for access</para>
        /// </summary>
        [Column("REQUIRES_AUTHORIZATION")]
        public IsOrNotEnum? RequiresAuthorization { get; set; }

        /// <summary>
        /// <para>zh-cn:授权元信息</para>
        /// <para>en-us:Authorization Meta Information</para>
        /// </summary>
        /// <remarks>
        ///   <para>zh-cn:存储授权信息,JSON数组字符串,元素类型:<see cref="EndPointAuthorizeData"/></para>
        ///   <para>en-us:Store authorization information, JSON array string, element type: <see cref="EndPointAuthorizeData"/></para>
        /// </remarks>
        [Column("AUTHORIZATION_META")]
        public string AuthorizationMeta { get; set; }


        /// <summary>
        /// <para>zh-cn:请求方法</para>
        /// <para>en-us:Request Method</para>
        /// </summary>
        [Column("METHOD")]
        public string Method { get; set; }


        /// <summary>
        /// <para>zh-cn:是否自动创建</para>
        /// <para>en-us:Whether to auto-create</para>
        /// </summary>
        /// <remarks>
        ///  <para>zh-cn:自动创建的路由为网关推送来的路由数据,不可人工修改调整</para>
        ///  <para>en-us:Auto-created routes are route data pushed by the gateway and cannot be manually modified or adjusted</para>
        /// </remarks>
        [Column("IS_AUTO_CREATE")]
        public IsOrNotEnum IsAutoCreate { get; set; } = IsOrNotEnum.否;

    }
}
