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
using Air.Cloud.EntityFrameWork.Core.ContextPools;
using Air.Cloud.EntityFrameWork.Core.UnitOfWork.Attributes;
using Air.Cloud.Core.Standard.DynamicServer;

using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.EntityFrameWork.Core.Filters;

/// <summary>
/// <para>zh-cn:仅为动态服务自动调用 SaveChanges 的 MVC 过滤器。</para>
/// <para>en-us:MVC filter that automatically invokes SaveChanges only for dynamic services.</para>
/// </summary>
public  sealed class AutoSaveChangesFilter : IAsyncActionFilter, IOrderedFilter
{
    private const string AutoSaveCompletedItemKey = "Air.Cloud.EntityFrameWork.Core.AutoSaveChangesCompleted";

    /// <summary>
    /// <para>zh-cn:过滤器排序值。</para>
    /// <para>en-us:The filter order value.</para>
    /// </summary>
    internal const int FilterOrder = 9999;

    /// <summary>
    /// <para>zh-cn:返回 MVC 过滤器排序值。</para>
    /// <para>en-us:Returns the MVC filter order value.</para>
    /// </summary>
    public int Order => FilterOrder;

    /// <summary>
    /// <para>zh-cn:执行动态服务请求，并在成功且未显式提交时保存上下文池。</para>
    /// <para>en-us:Executes a dynamic-service request and saves the context pool after successful execution unless manual commit is requested.</para>
    /// </summary>
    /// <param name="context"><para>zh-cn:当前动作执行上下文。</para><para>en-us:The current action execution context.</para></param>
    /// <param name="next"><para>zh-cn:用于继续执行后续过滤器和动作的委托。</para><para>en-us:The delegate that continues execution of later filters and the action.</para></param>
    /// <returns><para>zh-cn:表示动作执行完成的任务。</para><para>en-us:A task representing completion of action execution.</para></returns>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // 获取动作方法描述器
        if (context.ActionDescriptor is not ControllerActionDescriptor actionDescriptor)
        {
            await next();
            return;
        }
        var method = actionDescriptor.MethodInfo;

        // AutoSaveChanges 仅服务于动态服务；普通 MVC 控制器必须显式调用事务或 SaveChanges。
        var isDynamicService = typeof(IDynamicService).IsAssignableFrom(method.DeclaringType)
            || typeof(IDynamicService).IsAssignableFrom(actionDescriptor.ControllerTypeInfo.AsType());
        if (!isDynamicService)
        {
            await next();
            return;
        }

        // 获取请求上下文
        var httpContext = context.HttpContext;

        // 判断是否贴有工作单元特性
        if (method.IsDefined(typeof(UnitOfWorkAttribute), true))
        {
            _ = await next();

            return;
        }

        // 调用方法
        var resultContext = await next();

        // 判断是否手动提交
        var isManualSaveChanges = method.IsDefined(typeof(ManualCommitAttribute), true);

        // 判断是否异常，并且没有贴 [ManualSaveChanges] 特性
        if (resultContext.Exception == null
            && !isManualSaveChanges
            && !httpContext.Items.ContainsKey(AutoSaveCompletedItemKey))
        {
            httpContext.RequestServices.GetService<IDbContextPool>()?.SavePoolNow();
            // 兼容旧项目手动注册过滤器，确保同一请求只自动提交一次。
            httpContext.Items[AutoSaveCompletedItemKey] = true;
        }
    }
}
