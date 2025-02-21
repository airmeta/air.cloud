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

namespace Air.Cloud.Core.Standard.Store
{
    /// <summary>
    /// <para>zh-cn:存储服务</para>
    /// <para>en-us:Store standard</para>
    /// </summary>
    /// <typeparam name="TEventArgs">
    ///  <para>zh-cn:事件信息</para>
    ///  <para>en-us:Eventargs</para>
    /// </typeparam>
    /// <typeparam name="TData">
    ///  <para>zh-cn:数据类型</para>
    ///  <para>en-us:Data type</para>
    /// </typeparam>
    public interface IStoreStandard<TData,TEventArgs> 
        where TEventArgs : EventArgs, new()
        where TData: class
    {

        #region 事件
        /// <summary>
        /// <para>zh-cn:写入持久化数据事件</para>
        /// <para>en-us:Write persistent data events</para>
        /// </summary>
        public event EventHandler<TEventArgs> SetPersistenceHandler;
        /// <summary>
        /// <para>zh-cn:读取持久化数据事件</para>
        /// <para>en-us:Read persistent data events</para>
        /// </summary>
        public event EventHandler<TEventArgs> GetPersistenceHandler;
        #endregion

        /// <summary>
        /// <para>zh-cn:持久化数据存储</para>
        /// <para>en-us:Write persistent data storage </para>
        /// </summary>
        /// <param name="Packages">
        /// <para>zh-cn: 数据包</para>
        /// <para>en-us: Data packages</para>
        /// </param>
        /// <returns></returns>
        public Task SetPersistenceAsync(IDictionary<string, IEnumerable<TData>> Packages);

        /// <summary>
        /// <para>zh-cn:持久化数据读取</para>
        /// <para>en-us: Read persistent data storage </para>
        /// </summary>
        /// <returns>
        /// <para>zh-cn:存储的数据</para>
        /// <para>en-us:Store data packages</para>
        /// </returns>
        public Task<IDictionary<string, IEnumerable<TData>>> GetPersistenceAsync();
    }
}
