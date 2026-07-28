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

namespace air.cloud.system.model.Dtos.DictionaryDtos
{
    /// <summary>
    /// <para>zh-cn:字典配置查询传输对象</para>
    /// <para>en-us:Dictionary configuration query DTO</para>
    /// </summary>
    public class DictionaryConfigQDto : BaseQDto
    {
        /// <summary>
        /// <para>zh-cn:父级ID（可选）</para>
        /// <para>en-us:Parent Id (optional)</para>
        /// </summary>
        public string? ParentId { get; set; }

        /// <summary>
        /// <para>zh-cn:编码（可选，支持模糊匹配）</para>
        /// <para>en-us:Code (optional, supports fuzzy match)</para>
        /// </summary>
        public string? Code { get; set; }

        /// <summary>
        /// <para>zh-cn:标签（可选，支持模糊匹配）</para>
        /// <para>en-us:Label (optional, supports fuzzy match)</para>
        /// </summary>
        public string? Label { get; set; }
    }
}