<<<<<<< HEAD
﻿/*
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
using Air.Cloud.Core.App;
=======
﻿using Air.Cloud.Core.App;
>>>>>>> aeba4aab7dcf969688fd35ab1ea3ac980b15307d
using Air.Cloud.Core.Standard.Security;
using Air.Cloud.Core.Standard.Security.Model;
using Air.Cloud.Core.Standard.Security.Options;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Air.Cloud.Extensions.Security.Extensions
{
    public static  class SecurityExtensions
    {
        /// <summary>
        /// <para>zh-cn:启用安全认证中间件</para>
        /// <para>en-us:Enable security authentication middleware</para>
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseSecurityServer(this IApplicationBuilder app)
        {
            var options=AppCore.GetOptions<AuthenticaOptions>();
            app.Map(new PathString(options.PushRoute), application =>
            {
                application.Use(next =>
                {
                    return async (context) =>
                    {
                        try
                        {
                            var endpointDatas = await context.Request.ReadFromJsonAsync<IList<EndpointData>>();
                            if (endpointDatas != null)
                            {
                                foreach (var item in endpointDatas)
                                {
                                    ISecurityServerStandard.ServerEndpointDatas.Add(item);
                                }
                            }
                            await context.Response.WriteAsJsonAsync(new SecurityPushResult()
                            {
                                IsSuccess = true,
                                Message="Push success"
                            });
                        }
                        catch (Exception ex)
                        {
                            await context.Response.WriteAsJsonAsync(new SecurityPushResult()
                            {
                                IsSuccess = false,
                                Message= ex.Message
                            });
                        }
                       
                    };
                });
            });
            return app;
        }
    }
}
