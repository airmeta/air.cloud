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
using air.cloud.system.model.Domains.EntityAssociationDomains;
using air.cloud.system.model.Entitys.Associations;

using Air.Cloud.Core.Extensions;
using Air.Cloud.EntityFrameWork.Core.Repositories;

using Microsoft.EntityFrameworkCore;

namespace air.cloud.system.domain.Impls.EntityAssociationDomains
{
    public  class EntityAssociationDomain : IEntityAssociationDomain
    {

        private readonly IRepository<EntityAssociation> repository;
        public EntityAssociationDomain(IRepository<EntityAssociation> repository)
        {
            this.repository = repository;
        }

        public async Task<bool> AddEntityAssignAsync(string SoureEntityId, string TargetEntityId, AssociationTypeEnum associationType, string AppId)
        {
            var entityAssociation=await repository.FirstOrDefaultAsync(x => x.SourceEntityId == SoureEntityId && x.TargetEntityId== TargetEntityId && x.AssociationType== associationType && x.AppId== AppId);
            if (entityAssociation != null)
            {
                return true;
            }
            entityAssociation = new EntityAssociation
            {
                SourceEntityId = SoureEntityId,
                TargetEntityId = TargetEntityId,
                AssociationType = associationType,
                AppId = AppId
            };
            await repository.InsertAsync(entityAssociation);
            return true;
        }

        public async Task<bool> CopyEntityAssignsAsync(string sourceEntityId, string targetEntityId, string appId, params AssociationTypeEnum[] associationTypeEnums)
        {
           var assigns= await GetEntityAssociationsAsync(sourceEntityId, appId, associationTypeEnums);
            foreach (var assign in assigns)
            {
                await AddEntityAssignAsync(targetEntityId, assign.TargetEntityId, assign.AssociationType, appId);
            }
            return true;
        }

      

        public async Task<IList<EntityAssociation>> GetEntityAssociationsAsync(string entityId, string appId, params AssociationTypeEnum[] associationTypeEnums)
        {
            var query = repository.DetachedEntities.Where(x => x.SourceEntityId == entityId || x.TargetEntityId == entityId);
            if (associationTypeEnums != null && associationTypeEnums.Length > 0)
            {
                query = query.Where(x => associationTypeEnums.Contains(x.AssociationType));
            }
            if (!appId.IsNullOrEmpty())
            {
                query = query.Where(x => x.AppId == appId);
            }
            return await query.ToListAsync();
        }

        public async Task<IList<EntityAssociation>> GetEntityAssociationsAsync(IList<string> entityIds, string appId, params AssociationTypeEnum[] associationTypeEnums)
        {
            var query = repository.DetachedEntities.Where(x => entityIds.Contains(x.SourceEntityId) || entityIds.Contains(x.TargetEntityId));
            if (associationTypeEnums != null && associationTypeEnums.Length > 0)
            {
                query = query.Where(x => associationTypeEnums.Contains(x.AssociationType));
            }
            if (!appId.IsNullOrEmpty())
            {
                query = query.Where(x => x.AppId == appId);
            }
            return await query.ToListAsync();
        }

        public async Task<IQueryable<EntityAssociation>> GetEntityAssociationsQueryableAsync(string entityId, string appId, params AssociationTypeEnum[] associationTypeEnums)
        {
            var query = repository.DetachedEntities.Where(x => x.SourceEntityId == entityId || x.TargetEntityId == entityId);
            if (associationTypeEnums != null && associationTypeEnums.Length > 0)
            {
                query = query.Where(x => associationTypeEnums.Contains(x.AssociationType));
            }
            if (!appId.IsNullOrEmpty())
            {
                query = query.Where(x => x.AppId == appId);
            }
            return query;
        }

        public async Task<IQueryable<EntityAssociation>> GetEntityAssociationsQueryableAsync(IList<string> entityIds, string appId, params AssociationTypeEnum[] associationTypeEnums)
        {
            var query = repository.DetachedEntities.Where(x => entityIds.Contains(x.SourceEntityId) || entityIds.Contains(x.TargetEntityId));
            if (associationTypeEnums != null && associationTypeEnums.Length > 0)
            {
                query = query.Where(x => associationTypeEnums.Contains(x.AssociationType));
            }
            if (!appId.IsNullOrEmpty())
            {
                query = query.Where(x => x.AppId == appId);
            }
            return query;
        }
        public async Task<bool> MergeEntityAssignsAsync(string oldTargetEntityId, string newTargetEntityId, AssociationTypeEnum associationType, string appId)
        {
           var assigns=  await repository.DetachedEntities
                .Where(x => x.TargetEntityId == oldTargetEntityId && x.AssociationType == associationType && x.AppId == appId)
                .ExecuteUpdateAsync(s=>s.SetProperty(x => x.TargetEntityId, newTargetEntityId));
            return assigns>0;
        }

        public async Task<bool> RemoveEntityAssignAsync(string SoureEntityId, string TargetEntityId, AssociationTypeEnum associationType, string AppId)
        {
            var association =await  repository.FirstOrDefaultAsync(x => x.SourceEntityId == SoureEntityId 
                && x.TargetEntityId == TargetEntityId 
                && x.AssociationType == associationType 
                && x.AppId == AppId); 
            if (association != null)
            {
                await repository.DeleteAsync(association);
            }
            return true;
        }

        public async Task<bool> TruncateEntityAssignAsync(string EntityId, string appId, params AssociationTypeEnum[] associationTypeEnums)
        {
            int line= await repository.Where(x => (x.SourceEntityId == EntityId || x.TargetEntityId == EntityId)).AsQueryable().ExecuteDeleteAsync();
            return line>0;
        }
        public async Task<bool> ExitsEntityAssignAsync(string EntityId, string AppId, AssociationTypeEnum associationType)
        {
            if (!AppId.IsNullOrEmpty())
            {
                return await repository.AnyAsync(x => (x.SourceEntityId == EntityId || x.TargetEntityId == EntityId)
                    && x.AppId == AppId
                    && x.AssociationType == associationType);
            }
            return await repository.AnyAsync(x => (x.SourceEntityId == EntityId|| x.TargetEntityId == EntityId)
                && x.AssociationType == associationType);
        }

    }
}
