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

using Air.Cloud.Core;
using Air.Cloud.Core.Modules.AppPrint;
using Air.Cloud.EntityFrameWork.Core.UnitOfWork;
using Air.Cloud.EntityFrameWork.Core.UnitOfWork.Attributes;

using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.EntityFrameWork.Core.Filters;

/// <summary>
/// <para>zh-cn:根据 UnitOfWorkAttribute 管理动态服务请求事务的 MVC 过滤器。</para>
/// <para>en-us:MVC filter that manages dynamic-service request transactions according to UnitOfWorkAttribute.</para>
/// </summary>
public sealed class UnitOfWorkFilter : IAsyncActionFilter
{
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
    /// <para>zh-cn:执行请求并在动作成功或失败后提交或回滚工作单元。</para>
    /// <para>en-us:Executes the request and commits or rolls back the work unit after action success or failure.</para>
    /// </summary>
    /// <param name="context"><para>zh-cn:当前动作执行上下文。</para><para>en-us:The current action execution context.</para></param>
    /// <param name="next"><para>zh-cn:用于继续执行后续过滤器和动作的委托。</para><para>en-us:The delegate that continues execution of later filters and the action.</para></param>
    /// <returns><para>zh-cn:表示动作执行完成的任务。</para><para>en-us:A task representing completion of action execution.</para></returns>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // 获取动作方法描述器
        if (context.ActionDescriptor is not ControllerActionDescriptor actionDescriptor)
        {
            _ = await next();
            return;
        }

        var method = actionDescriptor.MethodInfo;

        // 获取请求上下文
        var httpContext = context.HttpContext;

        // 如果没有定义工作单元过滤器，则跳过
        if (!method.IsDefined(typeof(UnitOfWorkAttribute), true))
        {
            // 调用方法
            _ = await next();

            return;
        }
        // 打印工作单元开始消息
        AppRealization.Output.Print(new AppPrintInformation
        {
            Title = "unitofwork",
            Level = AppPrintLevel.Information,
            Content = "start",
            State = true
        });

        // 解析工作单元服务
        var _unitOfWork = httpContext.RequestServices.GetRequiredService<IUnitOfWork>();

        // 获取工作单元特性
        var unitOfWorkAttribute = method.GetCustomAttribute<UnitOfWorkAttribute>()
            ?? throw new InvalidOperationException($"方法 {method.DeclaringType?.FullName}.{method.Name} 未找到工作单元配置特性。");

        // 调用开启事务方法
        _unitOfWork.BeginTransaction(context, unitOfWorkAttribute);

        // 获取执行 Action 结果
        var resultContext = await next();

        if (resultContext.Exception == null)
        {
            // 调用提交事务方法
            _unitOfWork.CommitTransaction(resultContext, unitOfWorkAttribute);
        }
        else
        {
            // 调用回滚事务方法
            _unitOfWork.RollbackTransaction(resultContext, unitOfWorkAttribute);
        }

        // 调用执行完毕方法
        _unitOfWork.OnCompleted(context, resultContext);

        // 打印工作单元开始消息
        AppRealization.Output.Print(new AppPrintInformation
        {
            Title = "unitofwork",
            Level = AppPrintLevel.Information,
            Content = "finish",
            State = true
        });

    }
}
