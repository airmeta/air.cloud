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

namespace air.cloud.system.model.Entitys.Apps
{
    /// <summary>
    /// 应用路由授权
    /// </summary>
    [Table("APP_ROUTE_AUTH")]
    public  class AppRouteAuth : CreateEntityBase
    {
        /// <summary>
        /// 应用编号
        /// </summary>
        [Column("APP_ID")]
        public string AppId { get; set; }

        /// <summary>
        /// 路由信息编号
        /// </summary>
        [Column("ROUTE_ID")]
        public string RouteId { get; set; }


        /// <summary>
        /// <para>zh-cn:描述</para>
        /// <para>en-us:Description</para>
        /// </summary>
        [Column("DESCRIPTION")]
        public string? Description {  get; set; }
    }
}
