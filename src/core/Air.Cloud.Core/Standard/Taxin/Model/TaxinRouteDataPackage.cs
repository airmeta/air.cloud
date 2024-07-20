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
namespace Air.Cloud.Core.Standard.Taxin.Model
{
    /// <summary>
    /// <para>zh-cn:传输路由数据包</para>
    /// <para>en-us:Taxin Route Data Package</para>
    /// </summary>
    public class TaxinRouteDataPackage
    {
        /// <summary>
        /// <para>zh-cn:实例名称</para>
        /// <para>en-us:Instance name</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:一般来说为启动类库名称</para>
        /// <para>en-us:Generally the name of the startup library</para>
        /// </remarks>
        public string InstanceName { get; set; }
        /// <summary>
        /// <para>zh-cn:实例PID</para>
        /// <para>en-us:Instance PID</para>
        /// </summary>
        public string InstancePId { get; set; }
        /// <summary>
        /// <para>zh-cn:实例版本</para>
        /// <para>en-us:Instance version</para>
        /// </summary>
        public Version InstanceVersion { get; set; }
        /// <summary>
        /// <para>zh-cn:唯一编码</para>
        /// <para>en-us:UniqueKey</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:多个相同版本的实例相同</para>
        /// <para>en-us:Multiple instances of the same version are the same</para>
        /// </remarks>
        public string UniqueKey { get; set; }
        /// <summary>
        /// <para>zh-cn:路由信息</para>
        /// <para>en-us:Route information</para>
        /// </summary>
        public List<TaxinRouteInformation> Routes { get; set; } = new List<TaxinRouteInformation>();
        /// <summary>
        /// <para>zh-cn:数据创建的时间</para>
        /// <para></para>
        /// </summary>
        public DateTime CreateDataTime { get; set; } = DateTime.Now;
    }
}
