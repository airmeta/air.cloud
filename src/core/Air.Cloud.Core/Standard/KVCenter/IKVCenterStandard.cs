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
using Air.Cloud.Core.Dependencies;

namespace Air.Cloud.Core.Standard.KVCenter
{
    /// <summary>
    /// <para>en-us: Key-Value storage center standard</para>
    /// <para>zh-cn: 键值存储中心标准</para>
    /// </summary>
    public interface IKVCenterStandard: IStandard, ITransient
    {
        /// <summary>
        /// <para>zh-cn: 查询KV存储</para>
        /// <para>en-us: QueryAsync KV storage</para>
        /// </summary>
        /// <typeparam name="T">
        /// <para>zh-cn:返回列表的元素类型</para>
        /// <para>en-us: Element type of the returned list</para>
        /// </typeparam>
        /// <returns>
        /// <para>zh-cn: 查询结果</para>
        /// <para>en-us: QueryAsync result</para>
        /// </returns>
        public Task<IList<T>> QueryAsync<T>() where T : class, new();
        /// <summary>
        /// <para>zh-cn: 添加或更新KV存储</para>
        /// <para>en-us: Add or update KV storage</para>
        /// </summary>
        /// <param name="Key">
        /// <para>zh-cn:键</para>
        /// <para>en-us: Key</para>
        /// </param>
        /// <param name="Value">
        /// <para>zh-cn: 值</para>
        /// <para>en-us: Value</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn: 查询结果</para>
        /// <para>en-us: QueryAsync result</para>
        /// </returns>
        public Task<bool> AddOrUpdateAsync(string Key, string Value);
        /// <summary>
        /// <para>zh-cn: 根据Key删除Value</para>
        /// <para>en-us: DeleteAsync Value according to Key</para>
        /// </summary>
        /// <param name="Key">
        /// <para>zh-cn:键</para>
        /// <para>en-us: Key</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:是否删除成功</para>
        /// <para>en-us:Whether the deletion is successful</para>
        /// </returns>
        public Task<bool> DeleteAsync(string Key);
        /// <summary>
        /// <para>zh-cn:获取某个键的值 并转化为T类型</para>
        /// <para>en-us:GetAsync the value of a key and convert it to type T</para>
        /// </summary>
        /// <typeparam name="T">
        /// <para>zh-cn:值转换类型</para>
        /// <para>en-us: Type</para>
        /// </typeparam>
        /// <param name="Key">
        /// <para>zh-cn:键</para>
        /// <para>en-us:Key</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:值的转换结果</para>
        /// <para>en-us:Value conversion result</para>
        /// </returns>
        public Task<T> GetAsync<T>(string Key) where T : class, new();
    }
}
