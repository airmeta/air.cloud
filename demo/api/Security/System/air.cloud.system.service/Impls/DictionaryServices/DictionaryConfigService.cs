/*
 * Copyright (c) 2024-2030 –«“∑ ˝æ›
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * This file is provided under the Mozilla Public License Version 2.0,
 * and the "NO WARRANTY" clause of the MPL is hereby expressly
 * acknowledged.
 */
using air.cloud.system.service.Services.DictionaryServices;
using air.cloud.security.common.Model;
using air.cloud.system.model.Domains.DictionaryDomains;
using air.cloud.system.model.Dtos.DictionaryDtos;

using Microsoft.AspNetCore.Mvc;

using System.ComponentModel;

namespace air.cloud.system.service.Impl.DictionaryServices
{
    /// <summary>
    /// <para>zh-cn:◊÷µ‰≈‰÷√∑˛ŒÒ</para>
    /// <para>en-us:Dictionary configuration service</para>
    /// </summary>
    [Route("v1/security/system/dict")]
    [Description("◊÷µ‰≈‰÷√π‹¿Ì")]
    public class DictionaryConfigService : IDictionaryConfigService
    {
        private readonly IDictionaryConfigDomain _domain;

        public DictionaryConfigService(IDictionaryConfigDomain domain)
        {
            _domain = domain;
        }

        /// <summary>
        /// <para>zh-cn:¥¥Ω®ªÚ∏¸–¬◊÷µ‰≈‰÷√</para>
        /// <para>en-us:Create or update dictionary configuration</para>
        /// </summary>
        [HttpPost("save")]
        public async Task<bool> SaveDictionaryConfigAsync(DictionaryConfigSDto dto)
        {
            dto.Validate();
            var id = string.IsNullOrWhiteSpace(dto.Id)
                ? await _domain.CreateDictionaryConfigAsync(dto)
                : await _domain.UpdateDictionaryConfigAsync(dto);
            return !string.IsNullOrWhiteSpace(id);
        }

        /// <summary>
        /// <para>zh-cn:…æ≥˝◊÷µ‰≈‰÷√</para>
        /// <para>en-us:Delete dictionary configuration</para>
        /// </summary>
        [HttpDelete("remove/{id}")]
        public async Task<bool> DeleteDictionaryConfigAsync(string id)
        {
            return await _domain.DeleteDictionaryConfigAsync(id);
        }

        /// <summary>
        /// <para>zh-cn:∑÷“≥≤È—Ø◊÷µ‰≈‰÷√</para>
        /// <para>en-us:Query dictionary configurations with paging</para>
        /// </summary>
        [HttpPost("query")]
        public async Task<PageList<DictionaryConfigRDto>> QueryDictionaryConfigsAsync(DictionaryConfigQDto dto)
        {
            return await _domain.QueryDictionaryConfigsAsync(dto);
        }

        /// <summary>
        /// <para>zh-cn: ˜–Œ≤È—Ø◊÷µ‰≈‰÷√</para>
        /// <para>en-us:Query dictionary configurations as tree</para>
        /// </summary>
        [HttpPost("query-tree")]
        public async Task<List<DictionaryConfigTreeRDto>> QueryDictionaryConfigTreeAsync(DictionaryConfigQDto dto)
        {
            return await _domain.QueryDictionaryConfigTreeAsync(dto);
        }
        /// <summary>
        /// <para>zh-cn: ˜–Œ≤È—Ø◊÷µ‰≈‰÷√</para>
        /// <para>en-us:Query dictionary configurations as tree</para>
        /// </summary>
        [HttpPost("query-base")]
        public async Task<List<DictionaryConfigTreeRDto>> QueryDictionaryConfigBaseAsync(DictionaryConfigQDto dto)
        {
            dto.ParentId = null;
            return await _domain.QueryDictionaryConfigBaseAsync(dto);
        }

        /// <summary>
        /// <para>zh-cn:ªÒ»°◊÷µ‰≈‰÷√œÍ«È</para>
        /// <para>en-us:Get dictionary configuration detail</para>
        /// </summary>
        [HttpGet("detail/{id}")]
        public async Task<DictionaryConfigRDto?> GetDictionaryConfigAsync(string id)
        {
            return await _domain.GetDictionaryConfigAsync(id);
        }
    }
}