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
using air.cloud.system.model.Dtos.DictionaryDtos;
using air.cloud.security.common.Model;

using Air.Cloud.Core.Standard.DataBase.Domains;
using Air.Cloud.Core.Standard.DynamicServer;

namespace air.cloud.system.model.Domains.DictionaryDomains
{
    /// <summary>
    /// <para>zh-cn:字典配置领域接口</para>
    /// <para>en-us:Dictionary configuration domain interface</para>
    /// </summary>
    public interface IDictionaryConfigDomain : IEntityDomain, ITransient
    {
        /// <summary>
        /// <para>zh-cn:创建字典配置</para>
        /// <para>en-us:Create dictionary configuration</para>
        /// </summary>
        Task<string> CreateDictionaryConfigAsync(DictionaryConfigSDto config);

        /// <summary>
        /// <para>zh-cn:删除字典配置</para>
        /// <para>en-us:Delete dictionary configuration</para>
        /// </summary>
        Task<bool> DeleteDictionaryConfigAsync(string id);

        /// <summary>
        /// <para>zh-cn:更新字典配置</para>
        /// <para>en-us:Update dictionary configuration</para>
        /// </summary>
        Task<string> UpdateDictionaryConfigAsync(DictionaryConfigSDto config);

        /// <summary>
        /// <para>zh-cn:分页查询字典配置</para>
        /// <para>en-us:Query dictionary configurations with paging</para>
        /// </summary>
        /// <param name="dto">
        ///  <para>zh-cn:字典配置查询参数</para>
        ///  <para>en-us:Dictionary configuration query parameters</para>
        /// </param>
        Task<PageList<DictionaryConfigRDto>> QueryDictionaryConfigsAsync(DictionaryConfigQDto dto);

        /// <summary>
        /// <para>zh-cn:树形查询字典配置</para>
        /// <para>en-us:Query dictionary configurations as tree</para>
        /// </summary>
        /// <param name="dto">
        ///  <para>zh-cn:字典配置查询参数</para>
        ///  <para>en-us:Dictionary configuration query parameters</para>
        /// </param>
        Task<List<DictionaryConfigTreeRDto>> QueryDictionaryConfigTreeAsync(DictionaryConfigQDto dto);

        /// <summary>
        /// <para>zh-cn:查询字典根节点配置</para>
        /// <para>en-us:Query dictionary root node configurations</para>
        /// </summary>
        /// <param name="dto">
        ///  <para>zh-cn:字典配置查询参数</para>
        ///  <para>en-us:Dictionary configuration query parameters</para>
        /// </param>
        Task<List<DictionaryConfigTreeRDto>> QueryDictionaryConfigBaseAsync(DictionaryConfigQDto dto);


        /// <summary>
        /// <para>zh-cn:获取字典配置详情</para>
        /// <para>en-us:Get dictionary configuration detail</para>
        /// </summary>
        Task<DictionaryConfigRDto?> GetDictionaryConfigAsync(string id);
    }
}