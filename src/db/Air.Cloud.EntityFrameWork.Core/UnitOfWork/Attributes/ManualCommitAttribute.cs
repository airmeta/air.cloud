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
/// <para>zh-cn:标记由调用方手动保存更改的工作单元方法；默认的自动 SaveChanges 将被跳过。</para>
/// <para>en-us:Marks a work-unit method whose caller saves changes explicitly; the default automatic SaveChanges is skipped.</para>
/// </summary>
[IgnoreScanning, AttributeUsage(AttributeTargets.Method)]
public sealed class ManualCommitAttribute : Attribute
{
}
