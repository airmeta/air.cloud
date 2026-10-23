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

using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.EntityFrameWork.Core.DataRepository
{
    /// <summary>
    /// <para>zh-cn:基于 EFCore 仓储实现的通用数据仓储访问器。</para>
    /// <para>en-us:Generic data repository accessor implemented by EFCore repositories.</para>
    /// </summary>
    public sealed class EntityFrameworkDataRepositoryAccessor : IDataRepositoryAccessor
    {
        private readonly IServiceProvider serviceProvider;

        /// <summary>
        /// <para>zh-cn:初始化 EFCore 通用数据仓储访问器。</para>
        /// <para>en-us:Initializes the EFCore generic data repository accessor.</para>
        /// </summary>
        public EntityFrameworkDataRepositoryAccessor(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        /// <summary><para>zh-cn:解析指定实体类型的数据仓储。</para><para>en-us:Resolves the data repository for the specified entity type.</para></summary>
        /// <typeparam name="TEntity"><para>zh-cn:实体类型。</para><para>en-us:The entity type.</para></typeparam>
        public IDataRepository<TEntity> Change<TEntity>()
            where TEntity : class, IPrivateEntity, new()
        {
            return serviceProvider.GetRequiredService<IDataRepository<TEntity>>();
        }

        /// <summary><para>zh-cn:判断异常是否表示唯一约束冲突。</para><para>en-us:Determines whether an exception represents a unique-constraint violation.</para></summary>
        /// <param name="exception"><para>zh-cn:要分析的异常。</para><para>en-us:The exception to analyze.</para></param>
        /// <returns><para>zh-cn:检测到唯一或重复键错误时返回 true。</para><para>en-us:True when a unique or duplicate-key error is detected.</para></returns>
        public bool IsUniqueConstraintException(Exception exception)
        {
            var message = exception.InnerException?.Message ?? exception.Message;
            return message.Contains("unique", StringComparison.OrdinalIgnoreCase)
                || message.Contains("duplicate", StringComparison.OrdinalIgnoreCase);
        }
    }
}
