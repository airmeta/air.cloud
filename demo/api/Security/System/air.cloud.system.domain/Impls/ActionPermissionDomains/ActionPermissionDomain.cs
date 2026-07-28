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
using air.cloud.security.common.Base.Dtos;
using air.cloud.security.common.Enums;
using air.cloud.system.model.Domains.ActionPermissionDomains;
using air.cloud.system.model.Domains.EntityAssociationDomains;
using air.cloud.system.model.Dtos.PermissionDtos;
using air.cloud.system.model.Entitys.Roles;

using Air.Cloud.Core.App;
using Air.Cloud.Core.Extensions;
using Air.Cloud.Core.Extensions.Linqs;
using Air.Cloud.EntityFrameWork.Core.Repositories;

using Mapster;

using Microsoft.EntityFrameworkCore;

namespace air.cloud.system.domain.Impls.ActionPermissionDomains
{
    public  class ActionPermissionDomain : IActionPermissionDomain
    {

        private readonly IRepository<ActionPermission> repository;
        private readonly IEntityAssociationDomain entityAssociationDomain;
        public ActionPermissionDomain(IRepository<ActionPermission> repository,IEntityAssociationDomain entityAssociationDomain)
        {
            this.repository = repository;
            this.entityAssociationDomain = entityAssociationDomain;
        }

        public async Task<bool> CreateActionPermissionAsync(ActionPermissionSDto dto)
        {
            var actionPermission=await repository.FirstOrDefaultAsync(x=>x.Value==dto.Value && x.AppId==dto.AppId);

            if (actionPermission != null) return true;
            
            actionPermission=dto.Adapt<ActionPermission>();

            actionPermission.Id = AppCore.Guid();
            await repository.InsertAsync(actionPermission);
            return true;
        }

        public async  Task<bool> DeleteActionPermission(string permissionId)
        {
            var actionPermission = await repository.DetachedEntities.FirstOrDefaultAsync(x => x.Id == permissionId);

            if (actionPermission == null) return true;

            await repository.DeleteAsync(actionPermission);

            //删除角色与动作权限的关联关系
            await entityAssociationDomain.TruncateEntityAssignAsync(permissionId, actionPermission.AppId, AssociationTypeEnum.角色与动作权限);

            return true;
        }

        public async Task<ActionPermission> GetActionPermissionAsync(string permissionId)
        {
           var actionPermission=await  repository.DetachedEntities.FirstOrDefaultAsync(x => x.Id == permissionId);
            return actionPermission;
        }

   

        public async  Task<(int, List<ActionPermission>)> QueryActionPermissions(BaseQDto dto)
        {
            var linq = LinqExpressionExtensions.And<ActionPermission>();
            linq = linq.And(s=>s.AppId==dto.AppId);
            if (!dto.Info.IsNullOrEmpty())
            {
                linq=linq.And(x => x.Description.Contains(dto.Info) || x.Value.Contains(dto.Info));
            }
            var ActionPermissions = repository.DetachedEntities.AsNoTracking().Where(linq).AsQueryable() ;

            var total =await ActionPermissions.CountAsync();

            var list = await ActionPermissions
                .OrderByDescending(x => x.CreateTime)
                .Skip((dto.Page - 1) * dto.Limit)
                .Take(dto.Limit)
                .ToListAsync();
            return (total, list);
        }

     

        public Task<ActionToPermissionRDto> StoreActionsToPermissionAsync(string AppId)
        {
            //从指定应用程序处获取所有的动作信息，并存储到权限表中

            return Task.FromResult(new ActionToPermissionRDto()
            {
                AsyncSuccessCount=0,
                AppId=AppId,
                AsyncFailureCount= 0
            });
        }

        public async Task<bool> UpdateActionPermission(ActionPermissionSDto dto)
        {
            
            var actionPermission = await repository.FirstOrDefaultAsync(x => x.Id == dto.Id);

            if(actionPermission==null) return false;

            actionPermission.Value = dto.Value;
            actionPermission.Description = dto.Description;
            actionPermission.ServiceName = dto.ServiceName;
            await repository.UpdateIncludeAsync(actionPermission,new string[]
            {
                nameof(ActionPermission.Value),
                nameof(ActionPermission.Description),
                nameof(ActionPermission.ServiceName)
            });
            return true;


        }
    }
}
