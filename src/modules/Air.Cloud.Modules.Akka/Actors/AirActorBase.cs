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
using Akka.Actor;

namespace Air.Cloud.Modules.Akka.Actors;

/// <summary>
/// <para>zh-cn:Air.Cloud Akka Actor 基类，业务 Actor 可继承该类型以复用 `ReceiveActor` 消息处理模型。</para>
/// <para>en-us:Air.Cloud Akka actor base type that business actors can inherit to reuse the `ReceiveActor` message handling model.</para>
/// </summary>
public abstract class AirActorBase : ReceiveActor
{
}
