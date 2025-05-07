
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
using Air.Cloud.DataBase.Contexts;
using Air.Cloud.DataBase.Contexts.Attributes;
using Air.Cloud.DataBase.Internal;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace unit.webapp.repository.DbContexts
{
    /// <summary>
    /// 系统数据库配置
    /// </summary>
    [AppDbContext("OracleConnectionString", DbProvider.Oracle)]
    public class DefaultDbContext : AppDbContext<DefaultDbContext>
    {
        public DefaultDbContext(DbContextOptions<DefaultDbContext> options) : base(options)
        {
            //开启实体监听
            //EnabledEntityChangedListener = true;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }

        /// <summary>
        /// 日志审计，记录操作的具体内容
        /// </summary>
        /// <param name="eventData"></param>
        /// <param name="result"></param>
        protected override async void SavingChangesEvent(DbContextEventData eventData, InterceptionResult<int> result)
        {
            //记录你的操作

        }
    }
}