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

namespace Air.Cloud.Core.Standard.DataBase.Repositories
{
    /// <summary>
    /// <para>zh-cn:私有的数据库仓储</para>
    /// <para>en-us:Private nosql Repository</para>
    /// </summary>
    /// <typeparam name="TNoSqlEntity"></typeparam>
    public interface IPrivateNoSqlRepository<TNoSqlEntity>
        where TNoSqlEntity : class, INoSqlEntity, new()
    {

        /// <summary>
        /// <para>zh-cn:数据库仓储信息</para>
        /// <para>en-us:Database repository</para>
        /// </summary>
        public INoSqlRepository<TDodument> Change<TDodument>() where TDodument : class,INoSqlEntity,new();
    }
}
