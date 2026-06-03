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

namespace Air.Cloud.Core.Standard.DataRepository
{
    /// <summary>
    /// <para>zh-cn:通用数据仓储访问器，用于在不依赖具体 ORM 的情况下切换实体仓储。</para>
    /// <para>en-us:Generic data repository accessor used to switch entity repositories without depending on a concrete ORM.</para>
    /// </summary>
    public interface IDataRepositoryAccessor : IStandard
    {
        /// <summary>
        /// <para>zh-cn:切换到指定实体的数据仓储。</para>
        /// <para>en-us:Switches to the data repository of the specified entity.</para>
        /// </summary>
        IDataRepository<TEntity> Change<TEntity>()
            where TEntity : class, IPrivateEntity, new();

        /// <summary>
        /// <para>zh-cn:判断异常是否由唯一约束冲突导致。</para>
        /// <para>en-us:Determines whether the exception is caused by a unique constraint violation.</para>
        /// </summary>
        bool IsUniqueConstraintException(Exception exception);
    }
}
