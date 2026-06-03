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
using Air.Cloud.Core.Standard.DataBase.Model;
using Air.Cloud.Core.Standard.DataRepository;
using Air.Cloud.EntityFrameWork.Core.Repositories;

using Microsoft.EntityFrameworkCore;

using System.Linq.Expressions;

namespace Air.Cloud.EntityFrameWork.Core.DataRepository
{
    /// <summary>
    /// <para>zh-cn:基于现有 IRepository 的 EFCore 通用实体数据仓储适配器。</para>
    /// <para>en-us:EFCore generic entity data repository adapter based on the existing IRepository.</para>
    /// </summary>
    public sealed class EntityFrameworkDataRepository<TEntity> : IDataRepository<TEntity>
        where TEntity : class, IPrivateEntity, new()
    {
        private readonly IRepository<TEntity> repository;

        /// <summary>
        /// <para>zh-cn:初始化 EFCore 通用实体数据仓储适配器。</para>
        /// <para>en-us:Initializes the EFCore generic entity data repository adapter.</para>
        /// </summary>
        public EntityFrameworkDataRepository(IRepository<TEntity> repository)
        {
            this.repository = repository;
        }

        /// <inheritdoc />
        public async Task InsertAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            await repository.InsertNowAsync(entity, cancellationToken: cancellationToken);
        }

        /// <inheritdoc />
        public Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return repository.Entities.AsNoTracking().FirstOrDefaultAsync(predicate, cancellationToken);
        }

        /// <inheritdoc />
        public Task<int> UpdateWhereAsync(
            Expression<Func<TEntity, bool>> predicate,
            Action<IDataUpdateBuilder<TEntity>> update,
            CancellationToken cancellationToken = default)
        {
            var updateBuilder = new EntityFrameworkDataUpdateBuilder<TEntity>();
            update(updateBuilder);
            return repository.Entities
                .Where(predicate)
                .ExecuteUpdateAsync(updateBuilder.Build(), cancellationToken);
        }

        /// <inheritdoc />
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return repository.SaveNowAsync(cancellationToken);
        }
    }
}
