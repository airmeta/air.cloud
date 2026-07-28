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
using air.cloud.security.common.Model;

using Microsoft.EntityFrameworkCore;

namespace air.cloud.security.common.Extensions
{
    public static  class TableQueryExtensions
    {
        public static async Task<PageList<T>> ToPageListAsync<T>(this IQueryable<T> source, int page, int limit)
            where T : class
        {
            var count=await source.CountAsync();
            if (page==0||limit==0)
            {
                var items = await source.ToListAsync();
                return new PageList<T>()
                {
                    Count = count,
                    List = items
                };
            }
            else
            {
                var items = await source.Skip((page - 1) * limit).Take(limit).ToListAsync();
                return new PageList<T>()
                {
                    Count = count,
                    List = items
                };
                
            }
        }

    }
}
