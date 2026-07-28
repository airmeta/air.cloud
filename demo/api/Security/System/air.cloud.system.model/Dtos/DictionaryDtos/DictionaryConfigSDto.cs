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
using System;

namespace air.cloud.system.model.Dtos.DictionaryDtos
{
    /// <summary>
    /// <para>zh-cn:字典配置保存传输对象</para>
    /// <para>en-us:Dictionary configuration save DTO</para>
    /// </summary>
    public class DictionaryConfigSDto
    {
        /// <summary>
        /// <para>zh-cn:字典配置ID（新增不填）</para>
        /// <para>en-us:Dictionary config Id (empty when creating)</para>
        /// </summary>
        public string? Id { get; set; }

        /// <summary>
        /// <para>zh-cn:父级ID</para>
        /// <para>en-us:Parent Id</para>
        /// </summary>
        public string? ParentId { get; set; }

        /// <summary>
        /// <para>zh-cn:编码（必填，最大长度64）</para>
        /// <para>en-us:Code (required, max length 64)</para>
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// <para>zh-cn:标签（必填，最大长度128）</para>
        /// <para>en-us:Label (required, max length 128)</para>
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// <para>zh-cn:值（必填，最大长度256）</para>
        /// <para>en-us:Value (required, max length 256)</para>
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// <para>zh-cn:描述（可选，最大长度512）</para>
        /// <para>en-us:Description (optional, max length 512)</para>
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// <para>zh-cn:扩展字段（与数据库TEXT对应）</para>
        /// <para>en-us:Meta info (mapped to TEXT in DB)</para>
        /// </summary>
        public string? Meta { get; set; }

        /// <summary>
        /// <para>zh-cn:参数合法性校验</para>
        /// <para>en-us:Validate DTO arguments</para>
        /// </summary>
        public void Validate()
        {
            // Required checks
            if (string.IsNullOrWhiteSpace(Code))
                throw new ArgumentException("编码不能为空; Code cannot be null or empty.", nameof(Code));
            if (string.IsNullOrWhiteSpace(Label))
                throw new ArgumentException("标签不能为空; Label cannot be null or empty.", nameof(Label));
            if (string.IsNullOrWhiteSpace(Value))
                throw new ArgumentException("值不能为空; Value cannot be null or empty.", nameof(Value));

            // Length limits
            if (Code.Length > 64)
                throw new ArgumentException("编码长度不能超过64; Code length must be <= 64.", nameof(Code));
            if (Label.Length > 128)
                throw new ArgumentException("标签长度不能超过128; Label length must be <= 128.", nameof(Label));
            if (Value.Length > 256)
                throw new ArgumentException("值长度不能超过256; Value length must be <= 256.", nameof(Value));
            if (!string.IsNullOrEmpty(Description) && Description.Length > 512)
                throw new ArgumentException("描述长度不能超过512; Description length must be <= 512.", nameof(Description));
            // Meta 为 TEXT，不做长度限制。如需限制，可根据业务约束添加。
        }
    }
}