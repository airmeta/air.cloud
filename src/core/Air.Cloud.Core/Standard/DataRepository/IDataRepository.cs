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

using System.Linq.Expressions;

namespace Air.Cloud.Core.Standard.DataRepository
{
    /// <summary>
    /// <para>zh-cn:通用实体数据仓储，仅暴露跨模块需要的最小数据访问能力。</para>
    /// <para>en-us:Generic entity data repository that exposes only minimal cross-module data access capabilities.</para>
    /// </summary>
    public interface IDataRepository<TEntity>
        where TEntity : class, IPrivateEntity, new()
    {
        /// <summary>
        /// <para>zh-cn:插入实体。</para>
        /// <para>en-us:Inserts an entity.</para>
        /// </summary>
        Task InsertAsync(TEntity entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// <para>zh-cn:查询匹配条件的第一条实体。</para>
        /// <para>en-us:Queries the first entity that matches the predicate.</para>
        /// </summary>
        Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

        /// <summary>
        /// <para>zh-cn:按条件执行原子更新，并返回受影响行数。</para>
        /// <para>en-us:Executes an atomic conditional update and returns affected rows.</para>
        /// </summary>
        Task<int> UpdateWhereAsync(
            Expression<Func<TEntity, bool>> predicate,
            Action<IDataUpdateBuilder<TEntity>> update,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// <para>zh-cn:提交当前仓储上下文中的更改。</para>
        /// <para>en-us:Commits changes in the current repository context.</para>
        /// </summary>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
