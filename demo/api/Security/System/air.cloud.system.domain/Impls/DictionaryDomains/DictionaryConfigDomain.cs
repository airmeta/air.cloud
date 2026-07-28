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
using air.cloud.security.common.Base.Dtos;
using air.cloud.security.common.Extensions;
using air.cloud.security.common.Model;
using air.cloud.system.model.Domains.DictionaryDomains;
using air.cloud.system.model.Dtos.DictionaryDtos;
using air.cloud.system.model.Entitys.Dictonrays;

using Air.Cloud.Core.Extensions;
using Air.Cloud.Core.Extensions.Linqs;
using Air.Cloud.EntityFrameWork.Core.Repositories;

using Microsoft.EntityFrameworkCore;

namespace air.cloud.system.domain.Impls.DictionaryDomains
{
    /// <summary>
    /// <para>zh-cn:字典配置领域实现</para>
    /// <para>en-us:Dictionary configuration domain implementation</para>
    /// </summary>
    public class DictionaryConfigDomain : IDictionaryConfigDomain
    {
        private readonly IRepository<DictionaryConfig> _repository;

        public DictionaryConfigDomain(IRepository<DictionaryConfig> repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// <para>zh-cn:创建字典配置</para>
        /// <para>en-us:Create dictionary configuration</para>
        /// </summary>
        public async Task<string> CreateDictionaryConfigAsync(DictionaryConfigSDto config)
        {
            if (config == null) return string.Empty;
            config.Validate();

            var entity = new DictionaryConfig
            {
                Id = string.IsNullOrWhiteSpace(config.Id) ? Guid.NewGuid().ToString("N") : config.Id!,
                ParentId = config.ParentId,
                Code = config.Code,
                Label = config.Label,
                Value = config.Value,
                Description = config.Description,
                Meta = config.Meta
            };
            await _repository.InsertAsync(entity);
            return entity.Id;
        }

        /// <summary>
        /// <para>zh-cn:删除字典配置（物理删除）</para>
        /// <para>en-us:Delete dictionary configuration (physical)</para>
        /// </summary>
        public async Task<bool> DeleteDictionaryConfigAsync(string id)
        {
            if (id.IsNullOrEmpty()) return false;
            var entity = await _repository.DetachedEntities.FirstOrDefaultAsync(s => s.Id == id);
            if (entity == null) return false;
            await _repository.DeleteAsync(entity);
            return true;
        }

        /// <summary>
        /// <para>zh-cn:更新字典配置</para>
        /// <para>en-us:Update dictionary configuration</para>
        /// </summary>
        public async Task<string> UpdateDictionaryConfigAsync(DictionaryConfigSDto config)
        {
            if (config == null || config.Id.IsNullOrEmpty()) return string.Empty;
            config.Validate();

            var entity = await _repository.DetachedEntities.FirstOrDefaultAsync(s => s.Id == config.Id);
            if (entity == null) return string.Empty;

            entity.ParentId = config.ParentId;
            entity.Code = config.Code;
            entity.Label = config.Label;
            entity.Value = config.Value;
            entity.Description = config.Description;
            entity.Meta = config.Meta;

            await _repository.UpdateIncludeAsync(entity, new[]
            {
                nameof(DictionaryConfig.ParentId),
                nameof(DictionaryConfig.Code),
                nameof(DictionaryConfig.Label),
                nameof(DictionaryConfig.Value),
                nameof(DictionaryConfig.Description),
                nameof(DictionaryConfig.Meta)
            });
            return entity.Id;
        }

        /// <summary>
        /// <para>zh-cn:分页查询字典配置</para>
        /// <para>en-us:Query dictionary configurations with paging</para>
        /// </summary>
        public async Task<PageList<DictionaryConfigRDto>> QueryDictionaryConfigsAsync(DictionaryConfigQDto dto)
        {
            var linq = LinqExpressionExtensions.And<DictionaryConfig>();
            if (!dto.Info.IsNullOrEmpty())
            {
                linq = linq.And(s =>
                    s.Code.Contains(dto.Info) ||
                    s.Label.Contains(dto.Info) ||
                    s.Value.Contains(dto.Info) ||
                    s.Description.Contains(dto.Info));
            }
            if (!dto.ParentId.IsNullOrEmpty())
            {
                linq = linq.And(s => s.ParentId == dto.ParentId);
            }
            if (!dto.Code.IsNullOrEmpty())
            {
                linq = linq.And(s => s.Code.Contains(dto.Code));
            }
            if (!dto.Label.IsNullOrEmpty())
            {
                linq = linq.And(s => s.Label.Contains(dto.Label));
            }

            var query = _repository.Entities.Where(linq)
                .OrderByDescending(s => s.Id)
                .Select(s => new DictionaryConfigRDto
                {
                    Id = s.Id,
                    ParentId = s.ParentId,
                    Code = s.Code,
                    Label = s.Label,
                    Value = s.Value,
                    Description = s.Description,
                    Meta = s.Meta
                })
                .AsQueryable();

            return await query.ToPageListAsync<DictionaryConfigRDto>(dto.Page, dto.Limit);
        }

        /// <summary>
        /// <para>zh-cn:以树形结构查询字典配置（按 ParentId 构建层级）</para>
        /// <para>en-us:Query dictionary configurations as a tree (build hierarchy by ParentId)</para>
        /// </summary>
        public async Task<List<DictionaryConfigTreeRDto>> QueryDictionaryConfigTreeAsync(DictionaryConfigQDto dto)
        {
            var linq = LinqExpressionExtensions.And<DictionaryConfig>();
            if (!dto.Info.IsNullOrEmpty())
            {
                linq = linq.And(s =>
                    s.Code.Contains(dto.Info) ||
                    s.Label.Contains(dto.Info) ||
                    s.Value.Contains(dto.Info) ||
                    s.Description.Contains(dto.Info));
            }
            if (!dto.ParentId.IsNullOrEmpty())
            {
                linq = linq.And(s => s.ParentId == dto.ParentId);
            }
            if (!dto.Code.IsNullOrEmpty())
            {
                linq = linq.And(s => s.Code.Contains(dto.Code));
            }
            if (!dto.Label.IsNullOrEmpty())
            {
                linq = linq.And(s => s.Label.Contains(dto.Label));
            }

            var all = await _repository.DetachedEntities
                .Where(linq)
                .OrderBy(s => s.Code)
                .Select(s => new DictionaryConfigTreeRDto
                {
                    Id = s.Id,
                    ParentId = s.ParentId,
                    Code = s.Code,
                    Label = s.Label,
                    Value = s.Value,
                    Description = s.Description,
                    Meta = s.Meta
                })
                .ToListAsync();

            var dict = all.ToDictionary(x => x.Id, x => x);
            var roots = new List<DictionaryConfigTreeRDto>();

            foreach (var node in all)
            {
                if (string.IsNullOrWhiteSpace(node.ParentId) || !dict.ContainsKey(node.ParentId))
                {
                    roots.Add(node);
                }
                else
                {
                    dict[node.ParentId].Children.Add(node);
                }
            }

            return roots;
        }

        /// <summary>
        /// <para>zh-cn:以树形结构查询字典配置（按 ParentId 构建层级）</para>
        /// <para>en-us:Query dictionary configurations as a tree (build hierarchy by ParentId)</para>
        /// </summary>
        public async Task<List<DictionaryConfigTreeRDto>> QueryDictionaryConfigBaseAsync(DictionaryConfigQDto dto)
        {
            var linq = LinqExpressionExtensions.And<DictionaryConfig>();
            if (!dto.Info.IsNullOrEmpty())
            {
                linq = linq.And(s =>
                    s.Code.Contains(dto.Info) ||
                    s.Label.Contains(dto.Info) ||
                    s.Value.Contains(dto.Info) ||
                    s.Description.Contains(dto.Info));
            }
            linq = linq.And(s => string.IsNullOrEmpty(s.ParentId));
            if (!dto.Code.IsNullOrEmpty())
            {
                linq = linq.And(s => s.Code.Contains(dto.Code));
            }
            if (!dto.Label.IsNullOrEmpty())
            {
                linq = linq.And(s => s.Label.Contains(dto.Label));
            }
            var all = await _repository.DetachedEntities
                .Where(linq)
                .OrderBy(s => s.Code)
                .Select(s => new DictionaryConfigTreeRDto
                {
                    Id = s.Id,
                    ParentId = s.ParentId,
                    Code = s.Code,
                    Label = s.Label,
                    Value = s.Value,
                    Description = s.Description,
                    Meta = s.Meta
                })
                .ToListAsync();
            return all;
        }
        /// <summary>
        /// <para>zh-cn:获取字典配置详情</para>
        /// <para>en-us:Get dictionary configuration detail</para>
        /// </summary>
        public async Task<DictionaryConfigRDto?> GetDictionaryConfigAsync(string id)
        {
            if (id.IsNullOrEmpty()) return null;
            var s = await _repository.DetachedEntities.FirstOrDefaultAsync(x => x.Id == id);
            if (s == null) return null;

            return new DictionaryConfigRDto
            {
                Id = s.Id,
                ParentId = s.ParentId,
                Code = s.Code,
                Label = s.Label,
                Value = s.Value,
                Description = s.Description,
                Meta = s.Meta
            };
        }
    }
}