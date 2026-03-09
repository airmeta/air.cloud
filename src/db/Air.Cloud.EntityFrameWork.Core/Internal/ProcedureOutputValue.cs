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
namespace Air.Cloud.EntityFrameWork.Core.Internal;

/// <summary>
/// 存储过程输出值模型
/// </summary>
[IgnoreScanning]
public sealed class ProcedureOutputValue
{
    /// <summary>
    /// 输出参数名
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 输出参数值
    /// </summary>
    public object Value { get; set; }
}