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

using Air.Cloud.EntityFrameWork.Core.Extensions.Repositories;
using Air.Cloud.EntityFrameWork.Core.Repositories.Dependencies;

using System.Reflection;

namespace Air.Cloud.UnitTest.Modules.DataBase;

/// <summary>
/// <para>zh-cn:验证核心 EF 仓储不再暴露单实体立即保存 API。</para>
/// <para>en-us:Verifies that the core EF repositories no longer expose single-entity immediate-save APIs.</para>
/// </summary>
public sealed class ImmediateSaveApiRemovalTests
{
    /// <summary>
    /// <para>zh-cn:验证仓储接口和实体扩展不再暴露 InsertNow、UpdateNow、DeleteNow 或 SaveNow，由调用方显式决定保存边界。</para>
    /// <para>en-us:Verifies that repository contracts and entity extensions no longer expose InsertNow, UpdateNow, DeleteNow, or SaveNow, leaving the save boundary explicit to callers.</para>
    /// </summary>
    [Fact]
    public void Immediate_save_methods_should_not_be_exposed_by_core_repository_api()
    {
        var repositoryMethods = typeof(IInsertableRepository<>).GetMethods();
        var updateMethods = typeof(IUpdateableRepository<>).GetMethods();
        var deleteMethods = typeof(IDeletableRepository<>).GetMethods();
        var writableMethods = typeof(IPrivateWritableRepository<>).GetMethods();
        var entityExtensionMethods = typeof(IEntityExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static);
        var locatorExtensionMethods = typeof(IEntityWithDbContextLocatorExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static);

        Assert.DoesNotContain(repositoryMethods, IsImmediateSaveMethod);
        Assert.DoesNotContain(updateMethods, IsImmediateSaveMethod);
        Assert.DoesNotContain(deleteMethods, IsImmediateSaveMethod);
        Assert.DoesNotContain(writableMethods, IsImmediateSaveMethod);
        Assert.DoesNotContain(entityExtensionMethods, IsImmediateSaveMethod);
        Assert.DoesNotContain(locatorExtensionMethods, IsImmediateSaveMethod);
    }

    private static bool IsImmediateSaveMethod(MethodInfo method)
    {
        return method.Name.StartsWith("InsertNow", StringComparison.Ordinal)
            || method.Name.StartsWith("UpdateNow", StringComparison.Ordinal)
            || method.Name.StartsWith("UpdateIncludeNow", StringComparison.Ordinal)
            || method.Name.StartsWith("UpdateExcludeNow", StringComparison.Ordinal)
            || method.Name.StartsWith("DeleteNow", StringComparison.Ordinal)
            || method.Name.StartsWith("SaveNow", StringComparison.Ordinal);
    }
}
