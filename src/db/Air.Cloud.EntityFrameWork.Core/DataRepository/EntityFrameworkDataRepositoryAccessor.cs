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

        /// <inheritdoc />
        public IDataRepository<TEntity> Change<TEntity>()
            where TEntity : class, IPrivateEntity, new()
        {
            return serviceProvider.GetRequiredService<IDataRepository<TEntity>>();
        }

        /// <inheritdoc />
        public bool IsUniqueConstraintException(Exception exception)
        {
            var message = exception.InnerException?.Message ?? exception.Message;
            return message.Contains("unique", StringComparison.OrdinalIgnoreCase)
                || message.Contains("duplicate", StringComparison.OrdinalIgnoreCase);
        }
    }
}
