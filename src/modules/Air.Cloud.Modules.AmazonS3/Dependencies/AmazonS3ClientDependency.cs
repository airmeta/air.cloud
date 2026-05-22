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
using Air.Cloud.Core.Standard.AmazonS3;
using Air.Cloud.Core.Standard.AmazonS3.Options;
using Air.Cloud.Modules.AmazonS3.ClientFactory;

using Amazon.S3;

using Microsoft.Extensions.Options;

using System.Collections.Concurrent;
using System.Text.Json;

namespace Air.Cloud.Modules.AmazonS3.Dependencies
{
    /// <summary>
    /// <para>zh-cn:Amazon S3 Client 实现</para>
    /// <para>en-us:Amazon S3 client implementation</para>
    /// </summary>
    public class AmazonS3ClientDependency : IAmazonS3ClientStandard
    {
        private readonly ConcurrentDictionary<string, object> _clients = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, AmazonS3TokenOption> _tokenOptions = new(StringComparer.OrdinalIgnoreCase);
        private string _currentKey = string.Empty;

        public AmazonS3ClientDependency()
        {
        }

        public AmazonS3ClientDependency(IOptions<AmazonS3Options> options, IAmazonS3ClientFactory clientFactory)
        {
            if (options?.Value is null)
            {
                return;
            }

            foreach (var item in options.Value.Tokens)
            {
                _tokenOptions[item.Key] = item.Value;

                try
                {
                    _clients[item.Key] = clientFactory.CreateClient(item.Key);
                }
                catch
                {
                }
            }

            _currentKey = _tokenOptions.Keys.FirstOrDefault() ?? string.Empty;
        }

        /// <summary>
        /// <para>zh-cn:当前使用中的访问键</para>
        /// <para>en-us:Current key in use</para>
        /// </summary>
        public string CurrentKey => _currentKey;

        /// <summary>
        /// <para>zh-cn:已缓存的原始 Client</para>
        /// <para>en-us:Cached raw clients</para>
        /// </summary>
        public IReadOnlyDictionary<string, object> Clients => _clients;

        /// <summary>
        /// <para>zh-cn:注册或更新访问键对应的原始 Client</para>
        /// <para>en-us:Register or update the raw client for a key</para>
        /// </summary>
        public IAmazonS3ClientStandard SetClient(string Key, object Client)
        {
            if (string.IsNullOrWhiteSpace(Key))
            {
                throw new ArgumentException("Key can not be empty.", nameof(Key));
            }

            ArgumentNullException.ThrowIfNull(Client);
            _clients[Key] = Client;

            if (string.IsNullOrWhiteSpace(_currentKey))
            {
                _currentKey = Key;
            }

            return this;
        }

        /// <summary>
        /// <para>zh-cn:切换当前访问键</para>
        /// <para>en-us:Switch current key</para>
        /// </summary>
        public IAmazonS3ClientStandard Change(string Key)
        {
            if (!ContainsKey(Key))
            {
                throw new KeyNotFoundException($"Key '{Key}' is not registered.");
            }

            _currentKey = Key;
            return this;
        }

        /// <summary>
        /// <para>zh-cn:移除访问键</para>
        /// <para>en-us:Remove key</para>
        /// </summary>
        public bool RemoveKey(string Key)
        {
            var tokenRemoved = _tokenOptions.TryRemove(Key, out _);
            var removed = _clients.TryRemove(Key, out _);

            if (!removed && !tokenRemoved)
            {
                return false;
            }

            if (string.Equals(_currentKey, Key, StringComparison.OrdinalIgnoreCase))
            {
                _currentKey = _clients.Keys.FirstOrDefault() ?? _tokenOptions.Keys.FirstOrDefault() ?? string.Empty;
            }

            return true;
        }

        /// <summary>
        /// <para>zh-cn:判断访问键是否存在</para>
        /// <para>en-us:Check whether key exists</para>
        /// </summary>
        public bool ContainsKey(string Key)
        {
            if (string.IsNullOrWhiteSpace(Key))
            {
                return false;
            }

            return _clients.ContainsKey(Key) || _tokenOptions.ContainsKey(Key);
        }

        /// <summary>
        /// <para>zh-cn:根据访问键获取令牌</para>
        /// <para>en-us:Get token by key</para>
        /// </summary>
        public string Token(string Key = null)
        {
            var key = string.IsNullOrWhiteSpace(Key) ? _currentKey : Key;
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            if (!_tokenOptions.TryGetValue(key, out var option) || option?.Token == null)
            {
                return null;
            }

            return JsonSerializer.Serialize(option.Token);
        }

        /// <summary>
        /// <para>zh-cn:获取原始 Client</para>
        /// <para>en-us:Get raw client</para>
        /// </summary>
        public object Client(string Key = null)
        {
            var key = string.IsNullOrWhiteSpace(Key) ? _currentKey : Key;

            if (string.IsNullOrWhiteSpace(key) || !_clients.TryGetValue(key, out var client))
            {
                throw new KeyNotFoundException($"Key '{key}' is not registered.");
            }

            return client;
        }

        /// <summary>
        /// <para>zh-cn:获取原始 Client（强类型）</para>
        /// <para>en-us:Get raw client (strongly typed)</para>
        /// </summary>
        public TClient Client<TClient>(string Key = null) where TClient : class
        {
            var client = Client(Key);
            if (client is TClient typedClient)
            {
                return typedClient;
            }

            throw new InvalidCastException($"Client for key '{Key ?? _currentKey}' is not of type '{typeof(TClient).FullName}'.");
        }

        /// <summary>
        /// <para>zh-cn:获取 AmazonS3 原始客户端</para>
        /// <para>en-us:Get raw AmazonS3 client</para>
        /// </summary>
        public IAmazonS3 GetAmazonS3Client(string Key = null) => Client<IAmazonS3>(Key);
    }
}
