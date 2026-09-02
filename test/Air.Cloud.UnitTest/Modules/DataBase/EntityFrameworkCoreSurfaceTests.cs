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
using Air.Cloud.EntityFrameWork.Core;
using Air.Cloud.EntityFrameWork.Core.Repositories;

namespace Air.Cloud.UnitTest.Modules.DataBase;

/// <summary>
/// <para>zh-cn:验证 Core 只暴露 EF Core 仓储和事务边界，不再暴露旧 SQL/ADO.NET 仓储入口。</para>
/// <para>en-us:Verifies that Core exposes EF Core repositories and transaction boundaries without the legacy SQL/ADO.NET repository surface.</para>
/// </summary>
public sealed class EntityFrameworkCoreSurfaceTests
{
    /// <summary>
    /// <para>zh-cn:验证旧 SQL 仓储类型、工厂方法和仓储 Sql 方法均已移除。</para>
    /// <para>en-us:Verifies that the legacy SQL repository type, factory methods, and repository Sql methods are removed.</para>
    /// </summary>
    [Fact]
    public void Core_should_not_expose_legacy_sql_repository_surface()
    {
        var assembly = typeof(Db).Assembly;

        Assert.Null(assembly.GetType("Air.Cloud.EntityFrameWork.Core.Repositories.ISqlRepository"));
        Assert.Null(assembly.GetType("Air.Cloud.EntityFrameWork.Core.Repositories.ISqlRepository`1"));
        Assert.DoesNotContain(typeof(Db).GetMethods(), method => method.Name == "GetSqlRepository");
        Assert.DoesNotContain(typeof(IRepository).GetMethods(), method => method.Name == "Sql");
    }
}
