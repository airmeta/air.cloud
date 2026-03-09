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
namespace Air.Cloud.EntityFrameWork.Core.Enums;

/// <summary>
/// 数据库函数类型
/// </summary>
public  enum DbFunctionType
{
    /// <summary>
    /// 标量函数
    /// </summary>
    Scalar,

    /// <summary>
    /// 表值函数
    /// </summary>
    Table
}