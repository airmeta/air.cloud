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

        /// <summary><para>zh-cn:添加实体并保存更改。</para><para>en-us:Adds an entity and saves the changes.</para></summary>
        /// <param name="entity"><para>zh-cn:要添加的实体。</para><para>en-us:The entity to add.</para></param>
        /// <param name="cancellationToken"><para>zh-cn:取消异步操作的令牌。</para><para>en-us:The token used to cancel the asynchronous operation.</para></param>
        public async Task InsertAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            await repository.InsertAsync(entity, cancellationToken: cancellationToken);
            await repository.Context.SaveChangesAsync(cancellationToken);
        }

        /// <summary><para>zh-cn:按条件查询第一个实体；查询不跟踪实体。</para><para>en-us:Queries the first entity matching the predicate without tracking.</para></summary>
        /// <param name="predicate"><para>zh-cn:实体筛选条件。</para><para>en-us:The entity filter predicate.</para></param>
        /// <param name="cancellationToken"><para>zh-cn:取消异步操作的令牌。</para><para>en-us:The token used to cancel the asynchronous operation.</para></param>
        public Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return repository.Entities.AsNoTracking().FirstOrDefaultAsync(predicate, cancellationToken);
        }

        /// <summary><para>zh-cn:按条件执行 EF Core 批量更新。</para><para>en-us:Executes an EF Core bulk update for entities matching the predicate.</para></summary>
        /// <param name="predicate"><para>zh-cn:实体筛选条件。</para><para>en-us:The entity filter predicate.</para></param>
        /// <param name="update"><para>zh-cn:配置要更新属性的回调。</para><para>en-us:Callback that configures the properties to update.</para></param>
        /// <param name="cancellationToken"><para>zh-cn:取消异步操作的令牌。</para><para>en-us:The token used to cancel the asynchronous operation.</para></param>
        /// <returns><para>zh-cn:受影响的记录数。</para><para>en-us:The number of affected records.</para></returns>
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

        /// <summary><para>zh-cn:保存当前仓储上下文中的跟踪更改。</para><para>en-us:Saves tracked changes in the current repository context.</para></summary>
        /// <param name="cancellationToken"><para>zh-cn:取消异步操作的令牌。</para><para>en-us:The token used to cancel the asynchronous operation.</para></param>
        /// <returns><para>zh-cn:保存的实体数量。</para><para>en-us:The number of saved entities.</para></returns>
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return repository.Context.SaveChangesAsync(cancellationToken);
        }
    }
}
