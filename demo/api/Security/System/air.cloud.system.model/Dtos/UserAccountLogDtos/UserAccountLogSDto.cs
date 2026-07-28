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

namespace air.cloud.system.model.Dtos.UserAccountLogDtos
{
    /// <summary>
    /// <para>zh-cn:用户账户日志保存传输对象</para>
    /// <para>en-us:User account log save DTO</para>
    /// </summary>
    public class UserAccountLogSDto
    {
        /// <summary>
        /// <para>zh-cn:日志ID（新增不填）</para>
        /// <para>en-us:Log Id (empty when creating)</para>
        /// </summary>
        public string? Id { get; set; }

        /// <summary>
        /// <para>zh-cn:用户ID（必填，最大长度64）</para>
        /// <para>en-us:User Id (required, max length 64)</para>
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// <para>zh-cn:类型编码（必填，最大长度64）</para>
        /// <para>en-us:Type code (required, max length 64)</para>
        /// </summary>
        public string TypeCode { get; set; }

        /// <summary>
        /// <para>zh-cn:扩展字段（与数据库TEXT对应）</para>
        /// <para>en-us:Meta info (mapped to TEXT in DB)</para>
        /// </summary>
        public string? Meta { get; set; }

        /// <summary>
        /// <para>zh-cn:备注（可选，最大长度512）</para>
        /// <para>en-us:Remark (optional, max length 512)</para>
        /// </summary>
        public string? Remark { get; set; }

        /// <summary>
        /// <para>zh-cn:参数合法性校验</para>
        /// <para>en-us:Validate DTO arguments</para>
        /// </summary>
        public void Validate()
        {
            // Required checks
            if (string.IsNullOrWhiteSpace(UserId))
                throw new ArgumentException("用户ID不能为空; UserId cannot be null or empty.", nameof(UserId));
            if (string.IsNullOrWhiteSpace(TypeCode))
                throw new ArgumentException("类型编码不能为空; TypeCode cannot be null or empty.", nameof(TypeCode));

            // Length limits
            if (UserId.Length > 64)
                throw new ArgumentException("用户ID长度不能超过64; UserId length must be <= 64.", nameof(UserId));
            if (TypeCode.Length > 64)
                throw new ArgumentException("类型编码长度不能超过64; TypeCode length must be <= 64.", nameof(TypeCode));
            if (!string.IsNullOrEmpty(Remark) && Remark.Length > 512)
                throw new ArgumentException("备注长度不能超过512; Remark length must be <= 512.", nameof(Remark));
            // Meta 为 TEXT，不做长度限制。如需限制，可根据业务约束添加。
        }
    }
}