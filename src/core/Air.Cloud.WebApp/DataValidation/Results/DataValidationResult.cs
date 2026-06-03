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

using System.ComponentModel.DataAnnotations;

namespace Air.Cloud.WebApp.DataValidation.Results;

/// <summary>
/// 数据验证结果。
/// </summary>
[IgnoreScanning]
public sealed class DataValidationResult
{
    /// <summary>
    /// 验证是否通过。
    /// </summary>
    public bool Passed { get; set; }

    /// <summary>
    /// 验证是否失败。
    /// </summary>
    public bool Failed => !Passed;

    /// <summary>
    /// 验证失败明细。
    /// </summary>
    public ICollection<ValidationResult> ValidationResults { get; set; } = Array.Empty<ValidationResult>();

    /// <summary>
    /// 成员名或原始值。
    /// </summary>
    public object MemberOrValue { get; set; }
}
