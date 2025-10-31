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
using Air.Cloud.Core.App;
using Air.Cloud.Core.Modules.AppPrint;
using Air.Cloud.Core.Standard.DataBase.Domains;
using Air.Cloud.Core.Standard.DynamicServer;
using Air.Cloud.Core.Standard.DynamicServer.Extensions;


using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.WebApp.Extensions
{
    /// <summary>
    /// <para>zh-cn:实体领域扩展</para>
    /// <para>en-us:EntityDomain extensions</para>
    /// </summary>
    public static class AppEntityDomainInjectExtensions
    {
        /// <summary>
        /// <para>zh-cn:添加领域的初始化</para>
        /// <para>en-us:Add domain inject</para>
        /// </summary>
        /// <param name="services">
        ///  <para>zh-cn:服务集合</para>
        ///  <para>en-us:Service collection</para>
        /// </param>
        public static void AddEntityDomainInject(this IServiceCollection services)
        {
            try
            {
                var lifetimes = new[] { typeof(ITransient), typeof(IScoped), typeof(ISingleton) };
                Type[] AllDomains = AppCore.LoadSpecifyTypes(typeof(IEntityDomain)).Where(s=>s.IsClass).ToArray();
                #region 调试模式检查
                    #if DEBUG
                        if (AllDomains.Length == 0)
                        {
                            AppRealization.Output.Print(new AppPrintInformation
                            {
                                Title = "domain-warning",
                                Level = AppPrintLevel.Error,
                                Content = $"[code:aircloud_000001]看起来你正在调试一个不具有Domain的服务,你需要检查你的类库引用关系,并确保Domain类库被正常引入",
                                State = true,
                                Type = AppPrintConstType.DEBUG_LOG_TYPE
                            });
                        }
                    #endif
                #endregion
                foreach (var t in AllDomains)
                {
                    var instances = t.GetInterfaces();
                    var regType = instances.First(a => a.GetInterfaces().Contains(typeof(IEntityDomain)));
                    var lifeTime = instances.First(s => lifetimes.Contains(s));
                    if (instances.Contains(typeof(IEntityDomain)) && t.IsPublic)
                    {
                        AppCore.SetService(regType, t, DependencyInjectionServiceCollectionExtensions.TryGetServiceLifetime(lifeTime));
                    }
                }
            }
            catch (Exception ex)
            {
                AppRealization.Output.Print(new AppPrintInformation
                {
                    Title = "domain-errors",
                    Level = AppPrintLevel.Error,
                    AdditionalParams=new Dictionary<string, object>()
                    {
                        { "error",ex}
                    },
                    Content = $"[code:aircloud_000002]在加载Domain的过程中出现异常",
                    State = true
                });
            }
           
        }
    }
}
