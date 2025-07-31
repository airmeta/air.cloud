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
using Air.Cloud.Core.Standard.Taxin.Events;
using Air.Cloud.Core.Standard.Taxin.Model;

namespace Air.Cloud.Core.Standard.Store
{
    /// <summary>
    /// <para>zh-cn:Taxin 存储标准</para>
    /// <para>en-us:Taxin store</para>
    /// </summary>
    public interface ITaxinStoreStandard
    {


        /// <summary>
        /// <para>zh-cn:缓存持久化路径</para>
        /// <para>en-us:Cache persistence path</para>
        /// </summary>
        public static string GetPersistenceCachePath(string PersistencePath, string Separator = ":") => $"{PersistencePath}{Separator}{AppConst.ApplicationName}{Separator}{AppRealization.PID.Get()}".ToLower();

        /// <summary>
        /// <para>zh-cn:键值对配置中心持久化路径拼接</para>
        /// <para>en-us:Key-value configuration center persistence path splicing</para>
        /// /// </summary>
        public static string GetPersistenceKVPath(string PersistencePath ) => $"{PersistencePath}/{AppConst.ApplicationName}/{AppRealization.PID.Get()}.json".ToLower();


        static ITaxinStoreStandard()
        {
            Packages = new Dictionary<string, IEnumerable<TaxinRouteDataPackage>>();
        }
        /// <summary>
        /// <para>zh-cn:数据包</para>
        /// <para>en-us:Taxin data packages</para>
        /// </summary>
        public static IDictionary<string, IEnumerable<TaxinRouteDataPackage>> Packages { get; set; }

        /// <summary>
        /// <para>zh-cn:当前数据包</para>
        /// <para>en-us:Taxin data packages</para>
        /// </summary>
        public static TaxinRouteDataPackage Current { get; set; }

        /// <summary>
        /// <para>zh-cn:路由表信息</para>
        /// <para>en-us:route data</para>
        /// </summary>
        public static IDictionary<string, IDictionary<Version, TaxinRouteInformation>> Routes { get; set; } = new Dictionary<string, IDictionary<Version, TaxinRouteInformation>>();
        /// <summary>
        /// <para>zh-cn:检查标记</para>
        /// <para>en-us:CheckTag</para>
        /// </summary>
        /// <remarks>
        /// <para>zh-cn:在执行检查当前数据是否过时时,服务端会返回一个特定的Guid字符串,可使用该字符串与客户端字符串进行比较以确认当前数据是否过时</para>
        /// <para>en-us:When performing a check to see if the current data is out of date, the server returns a specific Guid string that can be compared with the client string to confirm whether the current data is out of date</para>
        /// </remarks>
        public static string CheckTag { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// <para>zh-cn:是否服务端</para>
        /// <para>en-us:Is Taxin server</para>
        /// </summary>
        public static bool IsTaxinServer = false;
        /// <summary>
        /// <para>zh-cn:持久化数据存储</para>
        /// <para>en-us: Persistent data storage </para>
        /// </summary>
        /// <param name="Packages">
        /// <para>zh-cn: Taxin 数据包</para>
        /// <para>en-us: Taxin data packages</para>
        /// </param>
        /// <returns></returns>
        public Task SetPersistenceAsync(IDictionary<string, IEnumerable<TaxinRouteDataPackage>> Packages);

        /// <summary>
        /// <para>zh-cn:持久化数据存储</para>
        /// <para>en-us: Persistent data storage </para>
        /// </summary>
        /// <returns>
        /// <para>zh-cn:存储的数据</para>
        /// <para>en-us:Taxin store data packages</para>
        /// </returns>
        public Task<IDictionary<string, IEnumerable<TaxinRouteDataPackage>>> GetPersistenceAsync();

        #region 事件
        /// <summary>
        /// <para>zh-cn:写入持久化数据事件</para>
        /// <para>en-us:Write persistent data events</para>
        /// </summary>
        public event EventHandler<TaxinStoreEventArgs> SetPersistenceHandler;
        /// <summary>
        /// <para>zh-cn:读取持久化数据事件</para>
        /// <para>en-us:Read persistent data events</para>
        /// </summary>
        public event EventHandler<TaxinStoreEventArgs> GetPersistenceHandler;
        #endregion
    }


}
