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
using air.cloud.system.model.Entitys;
using air.cloud.security.common.Base.Dtos;
using air.cloud.security.common.Model;
using air.cloud.system.model.Dtos.RoleDtos;

using Air.Cloud.Core.Standard.DataBase.Domains;
using Air.Cloud.Core.Standard.DynamicServer;

namespace air.cloud.system.model.Domains.RegionDomains
{
    public interface IRegionDomain : IEntityDomain, ITransient
    {
        /// <summary>
        /// 创建区域信息
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        public Task<bool> CreateRegionAsync(RegionSDto dto);


        /// <summary>
        /// 检查区域编码是否存在
        /// </summary>
        /// <param name="Code"></param>
        /// <returns></returns>
        public Task<bool> ExitRegionAsync(string Code);

        /// <summary>
        /// 删除区域信息
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        public Task<bool> DeleteRegionAsync(string  RegionId);

        /// <summary>
        /// 更新区域信息
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        public Task<bool> UpdateRegionAsync(RegionSDto dto);

        /// <summary>
        /// 查询区域
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        public Task<IList<RegionTreeDto>> QueryRegionsTreeAsync(BaseQDto dto);

        /// <summary>
        /// 获取单个区域
        /// </summary>
        /// <returns></returns>
        public Task<Region> GetRegionAsync(string RegionId);

    }
}
