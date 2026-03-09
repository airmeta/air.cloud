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
/// 工作单元拦截器
/// </summary>
public sealed class UnitOfWorkFilter : IAsyncActionFilter
{
    /// <summary>
    /// 过滤器排序
    /// </summary>
    internal const int FilterOrder = 9999;

    /// <summary>
    /// 排序属性
    /// </summary>
    public int Order => FilterOrder;

    /// <summary>
    /// 拦截请求
    /// </summary>
    /// <param name="context">动作方法上下文</param>
    /// <param name="next">中间件委托</param>
    /// <returns></returns>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // 获取动作方法描述器
        var actionDescriptor = context.ActionDescriptor as ControllerActionDescriptor;
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
        var unitOfWorkAttribute = method.GetCustomAttribute<UnitOfWorkAttribute>();

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