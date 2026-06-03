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
using Air.Cloud.Core.Extensions;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Air.Cloud.WebApp.UnifyResult.Attributes;

/// <summary>
/// 规范化结果配置
/// </summary>
[IgnoreScanning, AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class UnifyResultAttribute : ProducesResponseTypeAttribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="statusCode"></param>
    public UnifyResultAttribute(int statusCode) : base(statusCode)
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="type"></param>
    public UnifyResultAttribute(Type type) : base(type, StatusCodes.Status200OK)
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="type"></param>
    /// <param name="statusCode"></param>
    public UnifyResultAttribute(Type type, int statusCode) : base(type, statusCode)
    {
    }

    /// <summary>
    /// 包装类型
    /// </summary>
    /// <param name="type"></param>
    internal void ConfigureResultModel(Type resultModelType)
    {
        if (Type != null)
        {
            if (!Type.HasImplementedRawGeneric(resultModelType))
            {
                Type = resultModelType.MakeGenericType(Type);
            }
            else Type = default;
        }
    }
}
