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
using Air.Cloud.Core.Standard.Store;
using Air.Cloud.Core.Standard.Taxin.Attributes;
using Air.Cloud.Core.Standard.Taxin.Model;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

using System.Reflection;

#nullable enable annotations

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
        /// <para>en-us:Execute assembly service information</para>
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
            catch
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
                            if (Parameters.Any(s => s.ParameterType.IsInterface)) break;
                            taxinRoutes.Add(new TaxinRouteInformation
                            {
                                ServiceName = attr.ServiceName,
                                ServiceFullName = v.FullName,
                                HttpMethod = HttpMethods.HttpMethods.First(),
                                MethodName = v.Name + "." + item.Name,
                                Route = (InterfaceTemplate.IsNullOrEmpty() ? string.Empty : (InterfaceTemplate + "/")) + Template,
                                Parameters = Parameters.Select(s => new TaxinRouteParameter
                                {
                                    Name = s.Name,
                                    ParameterType = s.ParameterType.FullName,
                                    IsOptional = s.IsOptional
                                }).ToList()
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
        public static void SetPackages(IEnumerable<IEnumerable<TaxinRouteDataPackage>> Result)
        {
            foreach (var item in Result) SetPackages(item,false);
            _ = SortRoutes(ITaxinStoreStandard.Packages);
        }

        /// <summary>
        /// <para>zh-cn:设置客户端存储的数据包</para>
        /// <para>en-us:Set data packages</para>
        /// </summary>
        /// <param name="Result"></param>
        /// <param name="EnableSort"></param>
        /// <returns></returns>
        public static void SetPackages(IEnumerable<TaxinRouteDataPackage> Result,bool EnableSort=true)
        {
            IList<TaxinRouteDataPackage> ItemValues = null;
            foreach (var item in Result)
            {
                string Key = item.UniqueKey;
                if (Key == ITaxinStoreStandard.Current.UniqueKey) continue;
                if (ITaxinStoreStandard.Packages.ContainsKey(Key))
                {
                    ItemValues = ITaxinStoreStandard.Packages[Key].ToList();
                }
                else
                {
                    ItemValues = new List<TaxinRouteDataPackage>();
                }
                if (ItemValues.Any(x => x.InstancePId == item.InstancePId))
                {
                    ItemValues = ItemValues.Where(x => x.InstancePId != item.InstancePId).ToList();
                }
                ItemValues.Add(item);
                ITaxinStoreStandard.Packages[Key] = ItemValues;
            }
            if (EnableSort)
            {
                _ = SortRoutes(ITaxinStoreStandard.Packages);
            }
        }
        /// <summary>
        /// <para>zh-cn:候选 Taxin 路由排序实现。每个动作键独立构建版本到路由的映射，避免同一服务包中的后续动作覆盖前序动作。该方法当前不写入全局路由表，供替换现有排序实现前进行逻辑验证。</para>
        /// <para>en-us:Candidate Taxin route-sorting implementation. Each action key builds its own version-to-route map so later actions in the same service package cannot overwrite earlier actions. This method does not write the global route table and is intended for validation before replacing the current sorter.</para>
        /// </summary>
        /// <param name="Packages">
        /// <para>zh-cn:按服务实例标识分组的 Taxin 路由数据包。</para>
        /// <para>en-us:Taxin route data packages grouped by service instance identifier.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:按动作名和实例版本索引的候选路由表。</para>
        /// <para>en-us:A candidate route table indexed by action name and instance version.</para>
        /// </returns>
        private static IDictionary<string, IDictionary<Version, TaxinRouteInformation>> SortRoutes(IDictionary<string, IEnumerable<TaxinRouteDataPackage>> Packages)
        {
            var keyValues = new Dictionary<string, IDictionary<Version, TaxinRouteInformation>>();

            foreach (var package in Packages)
            {
                var allRoutes = package.Value
                    .Where(item => item.Routes != null && item.Routes.Any())
                    .Select(item => new { item.Routes, item.InstanceVersion })
                    .SelectMany(item => item.Routes.Select(route => new Tuple<string, Version, TaxinRouteInformation>(route.ServiceName, item.InstanceVersion, route)));

                foreach (var actionRoutes in allRoutes.GroupBy(item => item.Item1))
                {
                    // A route dictionary belongs to one action only; it must not be shared by sibling actions.
                    var versionRoutes = new Dictionary<Version, TaxinRouteInformation>();

                    foreach (var versionRoutesGroup in actionRoutes.GroupBy(item => item.Item2))
                    {
                        var route = versionRoutesGroup.Select(item => item.Item3).FirstOrDefault();
                        if (route != null)
                        {
                            versionRoutes[versionRoutesGroup.Key] = route;
                        }
                    }
                    if (versionRoutes.Count > 0)
                    {
                        keyValues[actionRoutes.Key] = versionRoutes;
                    }
                }
            }
            ITaxinStoreStandard.Routes = keyValues;
            return keyValues;
        }


    }
}
