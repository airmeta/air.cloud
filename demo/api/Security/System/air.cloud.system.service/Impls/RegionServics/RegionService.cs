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
using air.cloud.system.service.Services.RegionServices;
using air.cloud.security.common.Base.Dtos;
using air.cloud.security.common.Model;
using air.cloud.system.model.Domains.RegionDomains;
using air.cloud.system.model.Dtos.RegionDtos;
using air.cloud.system.model.Entitys;

using Air.Cloud.Core.App;
using Air.Cloud.Core.Extensions;
using Air.Cloud.WebApp.FriendlyException;

using Microsoft.AspNetCore.Mvc;

namespace air.cloud.system.service.Impl.RegionServics
{
    [Route("v1/security/system/region")]
    public class RegionService : IRegionService
    {
        private readonly IRegionDomain _regionDomain;
        public RegionService(IRegionDomain regionDomain)
        {
            this._regionDomain = regionDomain;
        }
        [HttpDelete("remove/{regionId}")]
        public async Task<bool> DeleteRegionAsync(string regionId)
        {
            return await _regionDomain.DeleteRegionAsync(regionId);
        }
        [HttpGet("detail/{regionId}")]
        public async Task<Region> GetRegionAsync(string regionId)
        {
            return await _regionDomain.GetRegionAsync(regionId);
        }
        [HttpPost("query")]
        public async Task<IList<RegionTreeDto>> QueryRegionsTreeAsync(BaseQDto dto)
        {
            return await _regionDomain.QueryRegionsTreeAsync(dto);    
        }
        [HttpPost("save")]
        public async Task<bool> SaveRegionAsync(RegionSDto dto)
        {
            if (dto.Id.IsNullOrEmpty())
            {
                if (await _regionDomain.ExitRegionAsync(dto.Code))
                {
                    throw Oops.Oh("当前区域编码已被使用");
                }
                dto.Id = AppCore.Guid();
                return await _regionDomain.CreateRegionAsync(dto);
            }
            else
            {
                return await _regionDomain.UpdateRegionAsync(dto);  
            }
        }
    }
}
