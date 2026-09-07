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
using Air.Cloud.Core.Standard.DynamicServer;
using Air.Cloud.EntityFrameWork.Core.ContextPools;
using Air.Cloud.EntityFrameWork.Core.Filters;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

using System.Data.Common;
using System.Reflection;

namespace Air.Cloud.UnitTest.Modules.DataBase;

/// <summary>
/// <para>zh-cn:自动保存过滤器的动态服务边界测试。</para>
/// <para>en-us:Tests for the dynamic-service boundary of the automatic save filter.</para>
/// </summary>
public sealed class AutoSaveChangesFilterTests
{
    /// <summary>
    /// <para>zh-cn:动态服务执行成功后应触发上下文池保存。</para>
    /// <para>en-us:Successful dynamic-service execution should save the context pool.</para>
    /// </summary>
    [Fact]
    public async Task Dynamic_service_should_trigger_pool_save()
    {
        var pool = new TrackingPool();
        var filter = new AutoSaveChangesFilter();
        var context = CreateContext(typeof(DynamicService), nameof(DynamicService.Execute), pool);

        await filter.OnActionExecutionAsync(context, () => Task.FromResult(new ActionExecutedContext(
            context,
            new List<IFilterMetadata>(),
            controller: null!)));

        Assert.Equal(1, pool.SaveCount);
    }

    /// <summary>
    /// <para>zh-cn:普通 MVC 控制器执行成功后不应被自动保存过滤器提交。</para>
    /// <para>en-us:Successful ordinary MVC controller execution should not be committed by the automatic save filter.</para>
    /// </summary>
    [Fact]
    public async Task Ordinary_controller_should_not_trigger_pool_save()
    {
        var pool = new TrackingPool();
        var filter = new AutoSaveChangesFilter();
        var context = CreateContext(typeof(OrdinaryController), nameof(OrdinaryController.Execute), pool);

        await filter.OnActionExecutionAsync(context, () => Task.FromResult(new ActionExecutedContext(
            context,
            new List<IFilterMetadata>(),
            controller: null!)));

        Assert.Equal(0, pool.SaveCount);
    }

    /// <summary>
    /// <para>zh-cn:同一请求重复注册过滤器时只允许自动保存一次。</para>
    /// <para>en-us:Repeated filter registration within one request must trigger automatic saving only once.</para>
    /// </summary>
    [Fact]
    public async Task Repeated_filter_registration_should_save_once_per_request()
    {
        var pool = new TrackingPool();
        var firstFilter = new AutoSaveChangesFilter();
        var secondFilter = new AutoSaveChangesFilter();
        var context = CreateContext(typeof(DynamicService), nameof(DynamicService.Execute), pool);

        await firstFilter.OnActionExecutionAsync(context, () => Task.FromResult(new ActionExecutedContext(
            context,
            new List<IFilterMetadata>(),
            controller: null!)));
        await secondFilter.OnActionExecutionAsync(context, () => Task.FromResult(new ActionExecutedContext(
            context,
            new List<IFilterMetadata>(),
            controller: null!)));

        Assert.Equal(1, pool.SaveCount);
    }

    private static ActionExecutingContext CreateContext(Type controllerType, string methodName, TrackingPool pool)
    {
        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection()
                .AddSingleton<IDbContextPool>(pool)
                .BuildServiceProvider()
        };
        var descriptor = new ControllerActionDescriptor
        {
            ControllerTypeInfo = controllerType.GetTypeInfo(),
            MethodInfo = controllerType.GetMethod(methodName)!
        };
        var actionContext = new ActionContext(httpContext, new RouteData(), descriptor);
        return new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object?>(), null!);
    }

    private sealed class DynamicService : IDynamicService
    {
        public void Execute()
        {
        }
    }

    private sealed class OrdinaryController : ControllerBase
    {
        public void Execute()
        {
        }
    }

    private sealed class TrackingPool : IDbContextPool
    {
        public int SaveCount { get; private set; }

        public IDbContextTransaction? DbContextTransaction => null;

        public System.Collections.Concurrent.ConcurrentDictionary<Guid, DbContext> GetDbContexts() => new();

        public void AddToPool(DbContext dbContext)
        {
        }

        public Task AddToPoolAsync(DbContext dbContext) => Task.CompletedTask;

        public int SavePoolNow()
        {
            SaveCount++;
            return 0;
        }

        public int SavePoolNow(bool acceptAllChangesOnSuccess) => SavePoolNow();

        public Task<int> SavePoolNowAsync(CancellationToken cancellationToken = default) => Task.FromResult(SavePoolNow());

        public Task<int> SavePoolNowAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default) => Task.FromResult(SavePoolNow());

        public void BeginTransaction(bool ensureTransaction = false)
        {
        }

        public void CommitTransaction(bool withCloseAll = false)
        {
        }

        public void RollbackTransaction(bool withCloseAll = false)
        {
        }

        public void CloseAll()
        {
        }
    }
}
