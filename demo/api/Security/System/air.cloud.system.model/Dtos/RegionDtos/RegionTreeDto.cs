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
    public  class RegionTreeDto
    {
        /// <summary>
        /// 区域编码
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// <para>区域编码</para>
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// <para>区域名称</para>
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// <para>区域类型</para>
        /// </summary>
        /// <remarks>
        ///  Template:  0:市区 1:县区 2:乡镇/街道 3:村/社居委
        /// </remarks>
        public RegionTypeEnum Type { get; set; }
        /// <summary>
        /// 父级区域编码
        /// </summary>
        public string ParentId { get; set; }
        /// <summary>
        /// 上级区域编码(可选,在某些片区的情形下需要此字段作为归属区域判断)
        /// </summary>
        public string ParentRegionId { get; set; }
        /// <summary>
        /// 描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 中心点坐标
        /// </summary>
        public string LngAndSat { get; set; }

        /// <summary>
        /// 区域对应的应用ID
        /// </summary>
        public string AppId { get; set; }
        /// <summary>
        /// 下级区域
        /// </summary>
        public IList<RegionTreeDto> Children { get; set; }

    }
}
