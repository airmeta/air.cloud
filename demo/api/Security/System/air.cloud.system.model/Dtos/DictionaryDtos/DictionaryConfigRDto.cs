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
namespace air.cloud.system.model.Dtos.DictionaryDtos
{
    /// <summary>
    /// <para>zh-cn:字典配置返回传输对象</para>
    /// <para>en-us:Dictionary configuration response DTO</para>
    /// </summary>
    public class DictionaryConfigRDto
    {
        /// <summary>
        /// <para>zh-cn:字典配置ID</para>
        /// <para>en-us:Dictionary config Id</para>
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// <para>zh-cn:父级ID</para>
        /// <para>en-us:Parent Id</para>
        /// </summary>
        public string? ParentId { get; set; }

        /// <summary>
        /// <para>zh-cn:编码</para>
        /// <para>en-us:Code</para>
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// <para>zh-cn:标签</para>
        /// <para>en-us:Label</para>
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// <para>zh-cn:值</para>
        /// <para>en-us:Value</para>
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// <para>zh-cn:描述</para>
        /// <para>en-us:Description</para>
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// <para>zh-cn:扩展字段（与数据库TEXT对应）</para>
        /// <para>en-us:Meta info (mapped to TEXT in DB)</para>
        /// </summary>
        public string? Meta { get; set; }
    }
}