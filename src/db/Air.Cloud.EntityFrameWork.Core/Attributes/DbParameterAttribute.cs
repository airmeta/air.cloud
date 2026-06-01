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
namespace Air.Cloud.EntityFrameWork.Core.Attributes;

/// <summary>
/// DbParameter 配置特性
/// </summary>
[IgnoreScanning, AttributeUsage(AttributeTargets.Property)]
public sealed class DbParameterAttribute : Attribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public DbParameterAttribute()
    {
        Direction = ParameterDirection.Input;
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="direction">参数方向</param>
    public DbParameterAttribute(ParameterDirection direction)
    {
        Direction = direction;
    }

    /// <summary>
    /// 参数输出方向
    /// </summary>
    public ParameterDirection Direction { get; set; }

    /// <summary>
    /// 数据库对应类型
    /// </summary>
    public object? DbType { get; set; }

    /// <summary>
    /// 大小
    /// </summary>
    /// <remarks>Nvarchar/varchar类型需指定</remarks>
    public int Size { get; set; }
}
