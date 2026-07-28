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
using air.cloud.security.common.Exceptions;
using air.cloud.security.common.Model.AppClientInfos;

using Air.Cloud.Core;
using Air.Cloud.Core.Extensions;
using Air.Cloud.WebApp.Extensions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

namespace air.cloud.security.common.Filters
{
    public  class GatewayUserDecryptFilter: IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            try
            {
                //查询下游服务是否允许匿名访问
                var actionDescriptor = context.HttpContext.GetControllerActionDescriptor();
                var TypeAllow = Attribute.GetCustomAttribute(actionDescriptor.ControllerTypeInfo, typeof(AllowAnonymousAttribute)) as AllowAnonymousAttribute;
                var ActionAllow = Attribute.GetCustomAttribute(actionDescriptor.MethodInfo, typeof(AllowAnonymousAttribute)) as AllowAnonymousAttribute;
                if (TypeAllow != null || ActionAllow != null)
                {
                    await next();
                    return;
                }
                string? Ticket = context.HttpContext.Request.Headers[IAppClientInfo.CLIENT_Ticket_HEADER];
                if (Ticket.IsNullOrEmpty()) throw new AuthException("Ticket_Valid_Faild", "Ticket不能为空");
                string StoreTicket = AppRealization.RedisCache.String.Get($"Client:Id:{Ticket}");
                if (StoreTicket.IsNullOrEmpty())
                {
                    throw new AuthException("Ticket_Valid_Faild", "Ticket不存在或已过期");
                }
                if (Ticket != StoreTicket)
                {
                    throw new AuthException("Ticket_Valid_Faild", "Ticket验证不合法");
                }
                string PayLoadInfo = AppRealization.RedisCache.String.Get($"Client:Token:{Ticket}");
                if (PayLoadInfo.IsNullOrEmpty())
                {
                    throw new AuthException("Ticket_Payload_Faild", "Ticket负载不存在或已过期");
                }
                await next();
            }
            catch (Exception ex)
            {
                AppRealization.Output.Print("出现异常", ex.Message, AdditionalParams: new Dictionary<string, object>()
                {
                    {"Exception", ex.Message },
                    {"StackTrace", ex.StackTrace ?? "" },
                    {"Source", ex.Source ?? "" }
                });
                await next();
            }
           
        }

    }
}
