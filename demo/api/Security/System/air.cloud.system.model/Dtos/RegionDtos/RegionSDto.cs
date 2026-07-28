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

namespace air.cloud.system.model.Dtos.RegionDtos
{
    /// <summary>
    /// <para>zh-cn:区域保存模型</para>
    /// <para>en-us:Region Save Dto</para>
    /// </summary>
    public class RegionSDto
    {

        /// <summary>
        /// <para>zh-cn:区域编号</para>
        /// <para>en-us:Region Id</para>
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// <para>zh-cn:区域编码</para>
        /// <para>en-us:Region Code</para>
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// <para>zh-cn:区域名称</para> 
        /// <para>en-us:Region Name</para>
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// <para>zh-cn:区域类型</para>
        /// <para>en-us:Region Type</para>
        /// </summary>
        /// <remarks>
        ///  Template:  0:市区 1:县区 2:乡镇/街道 3:村/社居委
        /// </remarks>
        public RegionTypeEnum Type { get; set; }

        /// <summary>
        /// <para>zh-cn:实际上级区域编号</para>
        /// <para>en-us:Parent Id</para>
        /// </summary>
        public string? ParentId { get; set; }
        /// <summary>
        /// <para>zh-cn:名义上级区域编号(可选,在某些片区的情形下需要此字段作为归属区域判断)</para>
        /// <para>en-us:Parent Region Id (Optional, used for certain scenarios)</para>
        /// </summary>
        public string ParentRegionId { get; set; }
        /// <summary>
        /// <para>zh-cn:区域描述</para>
        /// <para>en-us:Region Description</para>
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// <para>zh-cn:经纬度及卫星定位信息</para>
        /// <para>en-us:Longitude, Latitude and Satellite Information</para>
        /// </summary>
        public string LngAndSat { get; set; }

        /// <summary>
        /// <para>zh-cn:所属应用编号</para>
        /// <para>en-us:App Id</para>
        /// </summary>
        public string AppId { get; set; }


    }
}
