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
using Air.Cloud.Core.Plugins.Security.MD5;
using Air.Cloud.Core.Standard.DynamicServer;
using Air.Cloud.Core.Standard.Taxin.Attributes;
using Air.Cloud.Core.Standard.Taxin.Model;

using Microsoft.AspNetCore.Mvc;
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
                var DynamicService = AppCore.CrucialTypes.Where(s => s.GetInterfaces().Contains(typeof(IDynamicService)));
                //数据包信息
                var Package = new TaxinRouteDataPackage()
                {
                    InstancePId = AppRealization.PID.Get(),
                    InstanceName = Assembly.GetEntryAssembly().GetName().Name,
                    InstanceVersion = AppCore.Settings.VersionSerialize,
                    CreateDataTime = DateTime.Now,
                    UniqueKey = MD5Encryption.GetMd5By32($"{Assembly.GetEntryAssembly().GetName().Name}_{AppCore.Settings.Version.ToString()}")
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
                    if (attr.ServiceName.IsNullOrEmpty())
                    {
                        continue;
                    }
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
                                ServiceName = attr.ServiceName,
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


        /// <summary>
        /// <para>zh-cn:设置客户端存储的数据包</para>
        /// <para>en-us:Set data packages</para>
        /// </summary>
        /// <param name="Result"></param>
        /// <returns></returns>
        public static async Task SetPackages(IEnumerable<IEnumerable<TaxinRouteDataPackage>> Result)
        {
            IList<TaxinRouteDataPackage> ItemValues = null;
            foreach (var item in Result)
            {
                foreach (var s in item)
                {
                    string Key = s.UniqueKey;
                    //ignore current
                    if (Key == ITaxinStoreStandard.Current.UniqueKey) continue;
                    if (ITaxinStoreStandard.Packages.ContainsKey(Key))
                    {
                        ItemValues = ITaxinStoreStandard.Packages[Key].ToList();
                    }
                    else
                    {
                        ItemValues = new List<TaxinRouteDataPackage>();
                    }
                    if (ItemValues.Any(x => x.InstancePId == s.InstancePId))
                    {
                        ItemValues = ItemValues.Where(x => x.InstancePId != s.InstancePId).ToList();
                    }
                    ItemValues.Add(s);
                    ITaxinStoreStandard.Packages[Key] = ItemValues;
                }
            }
            _ = SortRoutes(ITaxinStoreStandard.Packages);
        }

        /// <summary>
        /// <para>zh-cn:复杂数据结构处理与分析</para>
        /// <para>en-us:Data sort</para>
        /// </summary>
        /// <param name="Packages">
        /// <para>zh-cn:数据包信息</para>
        /// <para>en-us:Data</para>
        /// </param>
        /// <returns>
        ///  <para>zh-cn:排序处理后的结果</para>
        ///  <para>en-us:result</para>
        /// </returns>
        private static IDictionary<string, IDictionary<Version, TaxinRouteInformation>> SortRoutes(IDictionary<string, IEnumerable<TaxinRouteDataPackage>> Packages)
        {
            //结果
            IDictionary<string, IDictionary<Version, TaxinRouteInformation>> KeyValues = new Dictionary<string, IDictionary<Version, TaxinRouteInformation>>();
            IDictionary<Version, TaxinRouteInformation> VersionRoutes = new Dictionary<Version, TaxinRouteInformation>();
            foreach (var item in Packages)
            {
                // 路由 版本 路由信息
                var AllRoutes = item.Value.Where(s => s.Routes != null && s.Routes.Any()).Select(s => new { s.Routes, s.InstanceVersion }).SelectMany(s => s.Routes.Select(x => new Tuple<string, Version, TaxinRouteInformation>(x.ServiceName, s.InstanceVersion, x))).ToList();
                var AllRouteKeys = AllRoutes.GroupBy(s => s.Item1).ToList();
                foreach (var Route in AllRouteKeys)
                {
                    VersionRoutes.Clear();
                    var Versions = AllRoutes.Where(s => s.Item1 == Route.Key).GroupBy(s => s.Item2).ToList();
                    foreach (var item1 in Versions)
                    {
                        //相同版本的接口应该是相同的 所以这里只需要取一条数据就可以了
                        var VersionRoute = AllRoutes.Where(s => s.Item1 == Route.Key && s.Item2 == item1.Key).Select(s => s.Item3).FirstOrDefault();
                        if (VersionRoute != null)
                            VersionRoutes[item1.Key] = VersionRoute;
                    }
                    if (VersionRoutes.Values.Any())
                        KeyValues[Route.Key] = VersionRoutes;
                }
            }
            ITaxinStoreStandard.Routes = KeyValues;
            return KeyValues;
        }


    }
}
