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
using air.cloud.security.common.Auths;
using air.cloud.security.common.Extensions;
using air.cloud.security.common.Model;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace air.cloud.open.repository.DbContexts
{
    /// <summary>
    /// 系统数据库配置
    /// </summary>
    [AppDbContext("OpenConnectionString")]
    public class DefaultDbContext : AppDbContext<DefaultDbContext>
    {
        private readonly IUserAccountStore userAccountStore;
        public DefaultDbContext(DbContextOptions<DefaultDbContext> options, IUserAccountStore userAccountStore) : base(options)
        {
            this.userAccountStore = userAccountStore;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.UseTimestampWithoutTimeZoneForDateTime();
        }

        /// <summary>
        /// <para>zh-cn:在保存开放平台数据前写入审计字段。该 EF Hook 是同步生命周期，不能使用 async void；当票据缺失或失效时，账号读取失败会降级为默认审计账号，避免后台异步异常导致进程退出。</para>
        /// <para>en-us:Writes audit fields before open-platform data is saved. This EF hook has a synchronous lifecycle and must not use async void; when the ticket is missing or invalid, account lookup failures fall back to default audit values so background async exceptions cannot terminate the process.</para>
        /// </summary>
        /// <param name="eventData">
        /// <para>zh-cn:EF Core 保存事件数据。</para>
        /// <para>en-us:The EF Core saving event data.</para>
        /// </param>
        /// <param name="result">
        /// <para>zh-cn:EF Core 保存拦截结果。</para>
        /// <para>en-us:The EF Core saving interception result.</para>
        /// </param>
        protected override void SavingChangesEvent(DbContextEventData eventData, InterceptionResult<int> result)
        {
            var account = GetAuditAccountOrDefault();
            //这里实现日志审计的具体逻辑
            DataBaseSavingChangesEvents.SaveChangesEvents(account, eventData, result);

        }

        private UserAccountFactory? GetAuditAccountOrDefault()
        {
            try
            {
                return userAccountStore.GetUserAccountAsync().GetAwaiter().GetResult();
            }
            catch
            {
                return null;
            }
        }
    }
}
