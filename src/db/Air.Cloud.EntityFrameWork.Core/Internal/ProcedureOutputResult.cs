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
/// 存储过程输出返回值
/// </summary>
[IgnoreScanning]
public sealed class ProcedureOutputResult : ProcedureOutputResult<DataSet>
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public ProcedureOutputResult() : base()
    {
    }
}

/// <summary>
/// 存储过程输出返回值
/// </summary>
/// <typeparam name="TResult">泛型版本</typeparam>
[IgnoreScanning]
public class ProcedureOutputResult<TResult>
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public ProcedureOutputResult()
    {
        OutputValues = new List<ProcedureOutputValue>();
        ReturnValue = DBNull.Value;
        Result = default!;
    }

    /// <summary>
    /// 输出值
    /// </summary>
    public IEnumerable<ProcedureOutputValue> OutputValues { get; set; }

    /// <summary>
    /// 返回值
    /// </summary>
    public object ReturnValue { get; set; }

    /// <summary>
    /// 结果集
    /// </summary>
    public TResult Result { get; set; }
}
