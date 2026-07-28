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
using air.cloud.security.common.Base;
using air.cloud.security.common.Model;

using Air.Cloud.Core.App;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace air.cloud.security.common.Extensions
{
    public static class DataBaseSavingChangesEvents
    {
        /// <summary>
        /// <para>zh-cn:在数据库保存前写入审计字段并规范化 DateTime Kind。当当前请求没有可用账号或票据已经失效时，使用默认审计账号，避免审计失败中断业务保存。</para>
        /// <para>en-us:Writes audit fields and normalizes DateTime Kind before database saving. When the current request has no available account or the ticket is already invalid, default audit values are used so audit failures do not interrupt business persistence.</para>
        /// </summary>
        /// <param name="account">
        /// <para>zh-cn:当前请求账号；为空时写入默认审计账号。</para>
        /// <para>en-us:The current request account; default audit values are written when it is null.</para>
        /// </param>
        /// <param name="eventData">
        /// <para>zh-cn:EF Core 保存事件数据。</para>
        /// <para>en-us:The EF Core saving event data.</para>
        /// </param>
        /// <param name="result">
        /// <para>zh-cn:EF Core 保存拦截结果。</para>
        /// <para>en-us:The EF Core saving interception result.</para>
        /// </param>
        public static void SaveChangesEvents(UserAccountFactory? account, DbContextEventData eventData, InterceptionResult<int> result)
        {
            // 获取当前事件对应上下文
            var dbContext = eventData.Context;
            if (dbContext == null)
            {
                return;
            }
            // 获取所有更改，删除，新增的实体，但排除审计实体（避免死循环）
            var entities = dbContext.ChangeTracker.Entries()
                .Where(u => u.State == EntityState.Modified || u.State == EntityState.Added).ToList();
            // 通过请求中获取当前操作人
            var AccountUserId = string.IsNullOrEmpty(account?.Id) ? "00000000000000000000000000000000" : account?.Id;
            var AccountUserName = string.IsNullOrEmpty(account?.UserName) ? "00000000000000000000000000000000" : account?.UserName;
            var now = ToTimestampWithoutTimeZone(DateTime.Now);
            foreach (var entity in entities)
            {
                switch (entity.State)
                {
                    case EntityState.Modified:

                        if (entity.CurrentValues.Properties.Any(s =>s.Name == nameof(CreateAndUpdateEntityBase.UpdateUserId)))
                        {
                            if (entity.Property(nameof(CreateAndUpdateEntityBase.UpdateUserName)).CurrentValue == null)
                                entity.Property(nameof(CreateAndUpdateEntityBase.UpdateUserName)).CurrentValue = AccountUserName;
                            if (entity.Property(nameof(CreateAndUpdateEntityBase.UpdateUserId)).CurrentValue == null)
                                entity.Property(nameof(CreateAndUpdateEntityBase.UpdateUserId)).CurrentValue = AccountUserId;
                            entity.Property(nameof(CreateAndUpdateEntityBase.UpdateTime)).CurrentValue = now;
                        }
                        break;
                    case EntityState.Added:
                        if (entity.CurrentValues.Properties.Any(s =>s.Name == nameof(CreateEntityBase.CreateUserId)))
                        {
                            if (entity.Property(nameof(LocalEntityBase.Id)).CurrentValue == null)
                                entity.Property(nameof(LocalEntityBase.Id)).CurrentValue = AppCore.Guid();
                            if (entity.Property(nameof(CreateEntityBase.CreateUserName)).CurrentValue == null)
                                entity.Property(nameof(CreateEntityBase.CreateUserName)).CurrentValue = AccountUserName;
                            if (entity.Property(nameof(CreateEntityBase.CreateUserId)).CurrentValue == null)
                                entity.Property(nameof(CreateEntityBase.CreateUserId)).CurrentValue = AccountUserId;
                            entity.Property(nameof(CreateEntityBase.CreateTime)).CurrentValue = now;
                        }
                        break;
                }

                foreach (var property in entity.CurrentValues.Properties)
                {
                    if (property.ClrType == typeof(DateTime))
                    {
                        var value = entity.Property(property.Name).CurrentValue;
                        if (value is DateTime dateTime)
                        {
                            entity.Property(property.Name).CurrentValue = ToTimestampWithoutTimeZone(dateTime);
                        }
                    }
                    else if (property.ClrType == typeof(DateTime?))
                    {
                        var value = entity.Property(property.Name).CurrentValue;
                        if (value is DateTime dateTime)
                        {
                            entity.Property(property.Name).CurrentValue = ToTimestampWithoutTimeZone(dateTime);
                        }
                    }
                }

              

            }
        }

        private static DateTime ToTimestampWithoutTimeZone(DateTime value)
        {
            return DateTime.SpecifyKind(value, DateTimeKind.Unspecified);
        }
    }
}
