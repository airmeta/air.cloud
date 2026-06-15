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
using Air.Cloud.Core.App;
using Air.Cloud.Core.Standard.KVCenter;
using Air.Cloud.Modules.Nacos.Model;

using Mapster;

using Nacos.V2;

namespace Air.Cloud.Modules.Nacos.Service
{
    /// <summary>
    /// <para>zh-cn:Nacos 配置中心标准实现，使用 dataId/group 表达 Air.Cloud KV 中心的 Key/Value 语义。</para>
    /// <para>en-us>Nacos config-center standard implementation that maps Air.Cloud KV Key/Value semantics to dataId/group.</para>
    /// </summary>
    public class NacosKVCenterDependency : IKVCenterStandard
    {
        private readonly INacosConfigService _configService;
        private readonly NacosServiceOptions _serviceOptions;

        /// <summary>
        /// <para>zh-cn:创建 Nacos KV 中心实现，依赖官方 Nacos 配置服务客户端和 Air.Cloud 配置。</para>
        /// <para>en-us>Creates a Nacos KV center implementation with the official Nacos config service client and Air.Cloud options.</para>
        /// </summary>
        /// <param name="configService">
        /// <para>zh-cn:Nacos 配置服务客户端。</para>
        /// <para>en-us>The Nacos config service client.</para>
        /// </param>
        public NacosKVCenterDependency(INacosConfigService configService)
        {
            _configService = configService;
            _serviceOptions = AppConfigurationLoader.InnerConfiguration.GetConfig<NacosServiceOptions>() ?? new NacosServiceOptions();
        }

        /// <inheritdoc/>
        public Task<IList<T>> QueryAsync<T>(string Prefix = "/") where T : class, new()
        {
            return QueryAsync<T>(Prefix, _serviceOptions.ConfigGroup);
        }

        /// <summary>
        /// <para>zh-cn:按 dataId 查询 Nacos 配置。Nacos SDK 不提供前缀枚举能力，因此 Prefix 被视为具体 dataId。</para>
        /// <para>en-us>Queries Nacos config by dataId. Because the Nacos SDK does not expose prefix enumeration, Prefix is treated as a concrete dataId.</para>
        /// </summary>
        /// <typeparam name="T">
        /// <para>zh-cn:返回项类型。</para>
        /// <para>en-us>The return item type.</para>
        /// </typeparam>
        /// <param name="prefix">
        /// <para>zh-cn:dataId 或调用方约定的键。</para>
        /// <para>en-us>The dataId or caller-defined key.</para>
        /// </param>
        /// <param name="group">
        /// <para>zh-cn:Nacos 配置分组。</para>
        /// <para>en-us>The Nacos config group.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:匹配配置项；未找到时返回空集合。</para>
        /// <para>en-us>The matched config items; returns an empty list when not found.</para>
        /// </returns>
        public async Task<IList<T>> QueryAsync<T>(string prefix, string group) where T : class, new()
        {
            var item = await GetAsync<NacosKvCenterServiceInformation>(prefix, group);
            if (item == null)
            {
                return new List<T>();
            }

            return new List<T> { item.Adapt<T>() };
        }

        /// <inheritdoc/>
        public Task<bool> AddOrUpdateAsync(string Key, string Value)
        {
            return AddOrUpdateAsync(Key, Value, _serviceOptions.ConfigGroup);
        }

        /// <summary>
        /// <para>zh-cn:新增或更新 Nacos 配置项，Key 映射为 dataId，Value 映射为配置内容。</para>
        /// <para>en-us>Adds or updates a Nacos config item where Key maps to dataId and Value maps to content.</para>
        /// </summary>
        /// <param name="key">
        /// <para>zh-cn:Nacos dataId。</para>
        /// <para>en-us>The Nacos dataId.</para>
        /// </param>
        /// <param name="value">
        /// <para>zh-cn:配置内容。</para>
        /// <para>en-us>The config content.</para>
        /// </param>
        /// <param name="group">
        /// <para>zh-cn:Nacos 配置分组。</para>
        /// <para>en-us>The Nacos config group.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:发布是否成功。</para>
        /// <para>en-us>Whether publishing succeeded.</para>
        /// </returns>
        public Task<bool> AddOrUpdateAsync(string key, string value, string group)
        {
            return _configService.PublishConfig(key, NormalizeGroup(group), value);
        }

        /// <inheritdoc/>
        public Task<bool> DeleteAsync(string Key)
        {
            return DeleteAsync(Key, _serviceOptions.ConfigGroup);
        }

        /// <summary>
        /// <para>zh-cn:删除 Nacos 配置项。</para>
        /// <para>en-us>Removes a Nacos config item.</para>
        /// </summary>
        /// <param name="key">
        /// <para>zh-cn:Nacos dataId。</para>
        /// <para>en-us>The Nacos dataId.</para>
        /// </param>
        /// <param name="group">
        /// <para>zh-cn:Nacos 配置分组。</para>
        /// <para>en-us>The Nacos config group.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:删除是否成功。</para>
        /// <para>en-us>Whether removal succeeded.</para>
        /// </returns>
        public Task<bool> DeleteAsync(string key, string group)
        {
            return _configService.RemoveConfig(key, NormalizeGroup(group));
        }

        /// <inheritdoc/>
        public Task<T> GetAsync<T>(string Key) where T : IKVCenterServiceOptions, new()
        {
            return GetAsync<T>(Key, _serviceOptions.ConfigGroup);
        }

        /// <summary>
        /// <para>zh-cn:获取 Nacos 配置并转换为指定 KV 选项类型。</para>
        /// <para>en-us>Gets Nacos config and converts it to the specified KV option type.</para>
        /// </summary>
        /// <typeparam name="T">
        /// <para>zh-cn:返回类型，必须实现 IKVCenterServiceOptions。</para>
        /// <para>en-us>The return type, which must implement IKVCenterServiceOptions.</para>
        /// </typeparam>
        /// <param name="key">
        /// <para>zh-cn:Nacos dataId。</para>
        /// <para>en-us>The Nacos dataId.</para>
        /// </param>
        /// <param name="group">
        /// <para>zh-cn:Nacos 配置分组。</para>
        /// <para>en-us>The Nacos config group.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:配置项；不存在时返回 default。</para>
        /// <para>en-us>The config item; returns default when not found.</para>
        /// </returns>
        public async Task<T> GetAsync<T>(string key, string group) where T : IKVCenterServiceOptions, new()
        {
            var value = await _configService.GetConfig(key, NormalizeGroup(group), _serviceOptions.ConfigTimeoutMs);
            if (value == null)
            {
                return default;
            }

            var data = new NacosKvCenterServiceInformation
            {
                Key = key,
                Value = value,
                Group = NormalizeGroup(group)
            };

            return data.Adapt<T>();
        }

        private string NormalizeGroup(string group)
        {
            return string.IsNullOrWhiteSpace(group) ? _serviceOptions.ConfigGroup : group;
        }
    }
}
