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
using air.cloud.security.common.Auths;
using air.cloud.security.common.Base.Dtos;
using air.cloud.security.common.Enums;
using air.cloud.security.common.Model;
using air.cloud.system.model.Domains.EntityAssociationDomains;
using air.cloud.system.model.Domains.OrganizationDomains;
using air.cloud.system.model.Domains.RegionDomains;
using air.cloud.system.model.Dtos.RegionDtos;
using air.cloud.system.model.Entitys;

using Air.Cloud.Core.Extensions;
using Air.Cloud.Core.Extensions.Linqs;
using Air.Cloud.EntityFrameWork.Core.Repositories;
using Air.Cloud.WebApp.FriendlyException;

using Mapster;

using Microsoft.EntityFrameworkCore;

namespace air.cloud.system.domain.Impls.RegionDomains
{
    public class RegionDomain : IRegionDomain
    {

        private readonly IRepository<Region> repository;
        private readonly IEntityAssociationDomain entityAssociationDomain;
        private readonly IDepartmentDomain departmentDomain;
        private readonly IUserAccountStore userAccount;
        public RegionDomain(IRepository<Region> repository,
            IEntityAssociationDomain entityAssociationDomain, 
            IDepartmentDomain departmentDomain,
                IUserAccountStore userAccount)
        {
            this.repository = repository;
            this.entityAssociationDomain = entityAssociationDomain;
            this.departmentDomain = departmentDomain;
            this.userAccount= userAccount;
        }

        public async Task<bool> CreateRegionAsync(RegionSDto dto)
        {
            var region = dto.Adapt<Region>();
            await repository.InsertAsync(region);
            return true;
        }

        public async Task<bool> ExitRegionAsync(string Code)
        {
            var count = await repository.CountAsync(x => x.Code == Code);
            return count > 0;
        }

        public async Task<bool> DeleteRegionAsync(string RegionId)
        {
            var region= repository.Change<Region>().DetachedEntities.FirstOrDefault(x => x.Id == RegionId);
            if(region==null) return false;
            //区域关联有部门则不允许删除
            var departments = await entityAssociationDomain.GetEntityAssociationsAsync(RegionId, region.AppId, AssociationTypeEnum.部门与区域);
            if (departments.Any())
            {
                var departmentInfos = await departmentDomain.GetDepartmentAsync(departments.Select(s => s.SourceEntityId).ToList());

                throw Oops.Oh($"当前区域关联以下部门:[{string.Join(',', departmentInfos.Select(s => s.DepartmentName).ToList())}],无法删除");
            }
            UserAccountFactory accountFactory = await userAccount.GetUserAccountAsync();

            region.IsDelete = IsOrNotEnum.是;
            region.DeleteTime = DateTime.Now;
            region.DeleteUserId = accountFactory.Id;
            region.DeleteUserName = accountFactory.UserName;

            await repository.UpdateIncludeAsync(region,new string[]
            {
                nameof(region.IsDelete),
                nameof(region.DeleteTime),
                nameof(region.DeleteUserId),
                nameof(region.DeleteUserName)
            });
            return true;
        }

        public async Task<Region> GetRegionAsync(string RegionId)
        {
            var region = await repository.Change<Region>().DetachedEntities.FirstOrDefaultAsync(x => x.Id == RegionId);
            return region;
        }

        public async Task<IList<RegionTreeDto>> QueryRegionsTreeAsync(BaseQDto dto)
        {
            var linq = LinqExpressionExtensions.And<Region>();
            if (!dto.AppId.IsNullOrEmpty())
            {
                linq = linq.And(s => s.AppId == dto.AppId);
            }
            linq = linq.And(s => s.IsDelete == IsOrNotEnum.否);

            var AllDepartments = await repository.DetachedEntities.Where(linq).ToListAsync();

            var TreeDepartments = Region.CreatTree(AllDepartments);

            return TreeDepartments;
        }

        public async Task<bool> UpdateRegionAsync(RegionSDto dto)
        {
            var region = await repository.Change<Region>().DetachedEntities.FirstOrDefaultAsync(x => x.Id == dto.Id);
            if (region == null) return false;
            region.Name=dto.Name;
            region.Description=dto.Description;
            region.LngAndSat = dto.LngAndSat;
            region.ParentId = dto.ParentId;
            region.ParentRegionId = dto.ParentRegionId;
            region.Type = dto.Type;
            await repository.UpdateIncludeAsync(region, new string[]
            {
                nameof(region.Name),
                nameof(region.Description),
                nameof(region.LngAndSat),
                nameof(region.ParentId),
                nameof(region.ParentRegionId),
                nameof(region.Type)
            });
            return true;
        }
    }
}
