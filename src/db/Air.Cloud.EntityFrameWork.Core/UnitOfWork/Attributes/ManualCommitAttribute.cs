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
namespace Air.Cloud.EntityFrameWork.Core.UnitOfWork.Attributes;

/// <summary>
/// 手动提交标识
/// <para>默认情况下，框架会自动在方法结束之时调用 SaveChanges 方法，贴此特性可以忽略该行为</para>
/// </summary>
[IgnoreScanning, AttributeUsage(AttributeTargets.Method)]
public sealed class ManualCommitAttribute : Attribute
{
}