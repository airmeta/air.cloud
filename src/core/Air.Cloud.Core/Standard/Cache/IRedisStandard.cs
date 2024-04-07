/*
 * Copyright (c) 2024 星曳数据
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * This file is provided under the Mozilla Public License Version 2.0,
 * and the "NO WARRANTY" clause of the MPL is hereby expressly
 * acknowledged.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Air.Cloud.Core.Standard.Cache
{

    /// <summary>
    /// Redis缓存约定
    /// </summary>
    public interface IRedisStandard : IAppCacheStandard
    {
        /// <summary>
        /// 当前Db是否可用
        /// </summary>
        /// <remarks>
        /// 集群环境下的redis只有0号库可用
        /// </remarks>
        /// <returns></returns>
        public bool DbCanUse();
    }
    public interface IRedisStringCacheStandard : IRedisStandard
    {
        /// <summary>
        /// 使用指定的Db
        /// </summary>
        /// <param name="DbNumber"></param>
        /// <returns></returns>
        IRedisStandard UseDb(int DbNumber = 0);

    }
}
