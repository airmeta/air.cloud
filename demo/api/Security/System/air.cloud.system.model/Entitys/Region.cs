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
using air.cloud.system.model.Dtos.RegionDtos;
using air.cloud.security.common.Base;
using air.cloud.security.common.Enums;

using Air.Cloud.Core.Extensions;

namespace air.cloud.system.model.Entitys
{
    /// <summary>
    /// 区域
    /// </summary>
    [Table("SYS_REGION")]
    public  class Region : AllEntityBase
    {
        /// <summary>
        /// <para>区域编码</para>
        /// </summary>
        [Column("CODE")]
        public string Code { get; set; }

        /// <summary>
        /// <para>区域名称</para>
        /// </summary>
        [Column("NAME")]
        public string Name { get; set; }

        /// <summary>
        /// <para>区域类型</para>
        /// </summary>
        /// <remarks>
        ///  Template:  0:市区 1:县区 2:乡镇/街道 3:村/社居委
        /// </remarks>
        [Column("TYPE")]
        public RegionTypeEnum Type { get; set; }

        /// <summary>
        /// 所属区域编码
        /// </summary>
        [Column("PARENT_ID")]
        public string? ParentId { get; set; }
        /// <summary>
        /// 上级区域编码(可选,在某些片区的情形下需要此字段作为归属区域判断)
        /// </summary>
        [Column("PARENT_REGION_ID")]
        public string? ParentRegionId { get; set; }
        /// <summary>
        /// 描述
        /// </summary>
        [Column("DESCRIPTION")]
        public string? Description { get; set; }

        /// <summary>
        /// 中心点坐标
        /// </summary>
        [Column("LNG_SAT")]
        public string? LngAndSat { get; set; }

        /// <summary>
        /// 区域对应的应用ID
        /// </summary>
        [Column("APP_ID")]
        public string AppId { get; set; }



        #region  生成树

        /// <summary>
        /// 生成部门树信息
        /// </summary>
        /// <param name="departments"></param>
        /// <returns></returns>
        public static List<RegionTreeDto> CreatTree(List<Region> regions)
        {

            List<RegionTreeDto> regionTreeDtos = regions.Where(s => s.ParentId .IsNullOrEmpty()).Select(d => new RegionTreeDto()
            {
                Code = d.Code,
                Description = d.Description,
                Name = d.Name,
                ParentId =string.Empty,
                Id = d.Id,
                ParentRegionId = d.ParentRegionId,
                LngAndSat = d.LngAndSat,
                Type = d.Type,
                AppId = d.AppId,
                Children = new List<RegionTreeDto>()
            }).ToList();

            foreach (var region in regionTreeDtos)
            {
                region.Children = CreatTree(region, regions);
            }
            return regionTreeDtos;
        }

        /// <summary>
        /// 生成树信息
        /// </summary>
        /// <param name="departmentTreeDto"></param>
        /// <param name="departments"></param>
        /// <returns></returns>
        private static IList<RegionTreeDto> CreatTree(RegionTreeDto regionTreeDto, List<Region> regions)
        {
            var Childrens = regions.Where(s => s.ParentId == regionTreeDto.Id).Select(d =>
            {
                var regionTreeItem = new RegionTreeDto()
                {
                    Code = d.Code,
                    Description = d.Description,
                    Name = d.Name,
                    ParentId = string.Empty,
                    Id = d.Id,
                    ParentRegionId= d.ParentRegionId,
                    LngAndSat= d.LngAndSat,
                    Type= d.Type,   
                    AppId= d.AppId
                };
                regionTreeItem.Children = CreatTree(regionTreeItem, regions);
                return regionTreeItem;
            }).ToList();
            return Childrens;
        }

        #endregion


    }
}
