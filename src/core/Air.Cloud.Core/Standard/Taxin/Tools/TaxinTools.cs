/*
 * Copyright (c) 2024 星曳数据
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * This file is provided under the Mozilla Public License Version 2.0,
 * and the "NO WARRANTY" clause of the MPL is hereby expressly
 * acknowledged.
 */
using Air.Cloud.Core.Plugins.Security.MD5;
using Air.Cloud.Core.Standard.DynamicServer;
using Air.Cloud.Core.Standard.Taxin.Attributes;
using Air.Cloud.Modules.Taxin.Model;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.Routing;

using System.Reflection;

namespace Air.Cloud.Core.Standard.Taxin.Tools
{
    /// <summary>
    /// <para>zh-cn:Taxin 工具</para>
    /// <para>en-us:Taxin tools</para>
    /// </summary>
    public static  class TaxinTools
    {
        /// <summary>
        /// <para>zh-cn:扫描类库信息</para>
        /// <para>en-us:Scanning assembly service information</para>
        /// </summary>
        /// <returns>
        /// <para>zh-cn:服务信息</para>
        /// <para>en-us:Taxin route data package</para>
        /// </returns>
        public static TaxinRouteDataPackage Scanning()
        {
            try
            {
                //扫描动态服务
                var DynamicService = AppCore.CrucialTypes.Where(s => typeof(IDynamicService).IsInstanceOfType(s));
                //数据包信息
                var Package = new TaxinRouteDataPackage()
                {
                    InstancePId = AppRealization.PID.Get(),
                    InstanceName = Assembly.GetExecutingAssembly().GetName().Name,
                    InstanceVersion = AppCore.Settings.VersionSerialize,
                    CreateDataTime = DateTime.Now,
                    UniqueKey = MD5Encryption.GetMd5By32($"{Assembly.GetExecutingAssembly().GetName().Name}_{AppCore.Settings.Version.ToString()}")
                };
                //开始收集数据包
                foreach (var item in DynamicService)
                {
                    var Routes = GetTaxinRouteInformation(item);
                    Package.Routes.AddRange(Routes);
                }
                ITaxinStoreStandard.Current = Package;
                return Package;
            }
            catch (Exception ex)
            {

                throw;
            }
           
        }
        /// <summary>
        /// <para>zh-cn:扫描Taxin路由信息</para>
        /// <para>en-us:Scan Taxin route information</para>
        /// </summary>
        /// <param name="v">
        /// <para>zh-cn:需要扫描的类型</para>
        /// <para>en-us:Type to be scanned</para>
        /// </param>
        /// <returns></returns>
        private static IList<TaxinRouteInformation> GetTaxinRouteInformation(Type v)
        {
            IList<TaxinRouteInformation> taxinRoutes = new List<TaxinRouteInformation>();
            var TypeRouteTemplate = v.GetCustomAttribute<RouteAttribute>(false);
            string InterfaceTemplate = TypeRouteTemplate == null ? null : TypeRouteTemplate.Template;
            bool AllMethodIsTaxinService = false;
            if (v.GetCustomAttribute(typeof(TaxinServiceAttribute)) != null) AllMethodIsTaxinService = true;
            MethodInfo[] methods = v.GetMethods().Where(s => s.IsPublic == true && s.IsStatic == false).ToArray();
            foreach (var item in methods)
            {
                var attr = item.GetCustomAttribute<TaxinServiceAttribute>();
                if (attr != null || AllMethodIsTaxinService)
                {
                    var HttpMethods = item.GetCustomAttribute<HttpMethodAttribute>();
                    var Route = item.GetCustomAttribute<RouteAttribute>();
                    if (HttpMethods != null || Route != null)
                    {
                        string? Template = HttpMethods != null ?
                            HttpMethods.Template.IsNullOrEmpty() ?
                                Route?.Template : HttpMethods?.Template
                             : Route?.Template;
                        if (!Template.IsNullOrEmpty())
                        {
                            var Parameters = item.GetParameters().ToList();
                            taxinRoutes.Add(new TaxinRouteInformation
                            {
                                ServiceName = attr.ServiceName.IsNullOrEmpty() ? (v.Name + "." + item.Name) : attr.ServiceName,
                                ServiceFullName = v.FullName,
                                HttpMethod = new HttpMethod(HttpMethods.HttpMethods.First()),
                                MethodName = v.Name + "." + item.Name,
                                Route = (InterfaceTemplate.IsNullOrEmpty() ? string.Empty : (InterfaceTemplate + "/")) + Template,
                                Parameters = Parameters
                            });
                        }
                    }
                }
            }
            return taxinRoutes;
        }
    }
}
