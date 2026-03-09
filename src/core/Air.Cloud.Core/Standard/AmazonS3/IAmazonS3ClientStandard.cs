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
namespace Air.Cloud.Core.Standard.AmazonS3
{
    /// <summary>
    /// <para>zh-cn:Amazon S3 Client 标准接口</para>
    /// <para>en-us:Amazon S3 client standard interface</para>
    /// </summary>
    public interface IAmazonS3ClientStandard
    {
        /// <summary>
        /// <para>zh-cn:当前使用中的访问键</para>
        /// <para>en-us:Current key in use</para>
        /// </summary>
        public string CurrentKey { get; }

        /// <summary>
        /// <para>zh-cn:已缓存的原始 Client</para>
        /// <para>en-us:Cached raw clients</para>
        /// </summary>
        public IReadOnlyDictionary<string, object> Clients { get; }

        /// <summary>
        /// <para>zh-cn:注册或更新访问键对应的原始 Client</para>
        /// <para>en-us:Register or update the raw client for a key</para>
        /// </summary>
        public IAmazonS3ClientStandard SetClient(string Key, object Client);

        /// <summary>
        /// <para>zh-cn:切换当前访问键</para>
        /// <para>en-us:Switch current key</para>
        /// </summary>
        public IAmazonS3ClientStandard Change(string Key);

        /// <summary>
        /// <para>zh-cn:移除访问键</para>
        /// <para>en-us:Remove key</para>
        /// </summary>
        public bool RemoveKey(string Key);

        /// <summary>
        /// <para>zh-cn:判断访问键是否存在</para>
        /// <para>en-us:Check whether key exists</para>
        /// </summary>
        public bool ContainsKey(string Key);

        /// <summary>
        /// <para>zh-cn:根据访问键获取令牌</para>
        /// <para>en-us:Get token by key</para>
        /// </summary>
        public string Token(string Key = null);

        /// <summary>
        /// <para>zh-cn:获取原始 Client</para>
        /// <para>en-us:Get raw client</para>
        /// </summary>
        public object Client(string Key = null);

        /// <summary>
        /// <para>zh-cn:获取原始 Client（强类型）</para>
        /// <para>en-us:Get raw client (strongly typed)</para>
        /// </summary>
        public TClient Client<TClient>(string Key = null) where TClient : class;
    }
}
