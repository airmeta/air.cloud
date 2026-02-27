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

using System.Reflection;

namespace Air.Cloud.Core.Standard.Taxin.Model
{
    /// <summary>
    /// <para>zh-cn:传输路由信息</para>
    /// <para>en-us:TaxinRouteInformation</para>
    /// </summary>
    public class TaxinRouteInformation
    {
        /// <summary>
        /// <para>zh-cn:多个服务实例的Key将会相同</para>
        /// <para>en-us:Multiple service instances will have the same Key</para>
        /// </summary>
        /// <remarks>
        ///  <para>zh-cn:多个服务实例的Key将会相同</para>
        /// </remarks>
        public string Key
        {
            get
            {
                return MD5Encryption.GetMd5By32(ServiceFullName + "|" + Route);
            }
        }
        /// <summary>
        /// <para>zh-cn:服务名</para>
        /// <para>en-us:Service name</para>
        /// </summary>
        public string ServiceName { get; set; }
        /// <summary>
        /// <para>zh-cn:服务全名</para>
        /// <para>en-us:Service full name</para>
        /// </summary>
        public string ServiceFullName { get; set; }
        /// <summary>
        /// <para>zh-cn:方法名</para>
        /// <para>en-us:Method name</para>
        /// </summary>
        public string MethodName { get; set; }
        /// <summary>
        /// <para>zh-cn:请求方法</para>
        /// <para>en-us:Request method</para>
        /// </summary>
        public string HttpMethod { get; set; } = "GET";
        /// <summary>
        /// <para>zh-cn:路由</para>
        /// <para>en-us:Route</para>
        /// </summary>
        public string Route { get; set; }
        /// <summary>
        /// <para>zh-cn:参数</para>
        /// <para>en-us:Parameters</para>
        /// </summary>
        public List<TaxinRouteParameter> Parameters { get; set; }
    }


}
