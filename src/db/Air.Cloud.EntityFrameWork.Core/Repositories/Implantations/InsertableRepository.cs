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

using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Air.Cloud.EntityFrameWork.Core.Repositories.Implantations;

/// <summary>
/// <para>zh-cn:可插入仓储实现。</para>
/// <para>en-us:Repository implementation for insert operations.</para>
/// </summary>
public partial class PrivateRepository<TEntity>
    where TEntity : class, IPrivateEntity, new()
{
    /// <summary>
    /// <para>zh-cn:新增一条记录</para>
    /// <para>en-us:Inserts a record.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <returns><para>zh-cn:代理的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public virtual EntityEntry<TEntity> Insert(TEntity entity, bool? ignoreNullValues = null)
    {
        var entryEntity = Entities.Add(entity);

        // 忽略空值
        IgnoreNullValues(ref entity, ignoreNullValues);

        return entryEntity;
    }

    /// <summary>
    /// <para>zh-cn:新增多条记录</para>
    /// <para>en-us:Inserts a record.</para>
    /// </summary>
    /// <param name="entities"><para>zh-cn:多个实体</para><para>en-us:The entities to process.</para></param>
    public virtual void Insert(params TEntity[] entities)
    {
        Entities.AddRange(entities);
    }

    /// <summary>
    /// <para>zh-cn:新增多条记录</para>
    /// <para>en-us:Inserts a record.</para>
    /// </summary>
    /// <param name="entities"><para>zh-cn:多个实体</para><para>en-us:The entities to process.</para></param>
    public virtual void Insert(IEnumerable<TEntity> entities)
    {
        Entities.AddRange(entities);
    }

    /// <summary>
    /// <para>zh-cn:新增一条记录</para>
    /// <para>en-us:Inserts a record.</para>
    /// </summary>
    /// <param name="entity"><para>zh-cn:实体</para><para>en-us:The entity.</para></param>
    /// <param name="ignoreNullValues"><para>zh-cn:ignoreNullValues 参数。</para><para>en-us:The ignoreNullValues value.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消异步令牌</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:代理的实体</para><para>en-us:The tracked entity entry.</para></returns>
    public virtual async Task<EntityEntry<TEntity>> InsertAsync(TEntity entity, bool? ignoreNullValues = null, CancellationToken cancellationToken = default)
    {
        var entityEntry = await Entities.AddAsync(entity, cancellationToken);

        // 忽略空值
        IgnoreNullValues(ref entity, ignoreNullValues);

        return entityEntry;
    }

    /// <summary>
    /// <para>zh-cn:新增多条记录</para>
    /// <para>en-us:Inserts a record.</para>
    /// </summary>
    /// <param name="entities"><para>zh-cn:多个实体</para><para>en-us:The entities to process.</para></param>
    /// <returns><para>zh-cn:Task</para><para>en-us:A task representing completion.</para></returns>
    public virtual Task InsertAsync(params TEntity[] entities)
    {
        return Entities.AddRangeAsync(entities);
    }

    /// <summary>
    /// <para>zh-cn:新增多条记录</para>
    /// <para>en-us:Inserts a record.</para>
    /// </summary>
    /// <param name="entities"><para>zh-cn:多个实体</para><para>en-us:The entities to process.</para></param>
    /// <param name="cancellationToken"><para>zh-cn:取消异步令牌</para><para>en-us:The cancellation token.</para></param>
    /// <returns><para>zh-cn:操作结果。</para><para>en-us:The operation result.</para></returns>
    public virtual Task InsertAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        return Entities.AddRangeAsync(entities, cancellationToken);
    }

}
