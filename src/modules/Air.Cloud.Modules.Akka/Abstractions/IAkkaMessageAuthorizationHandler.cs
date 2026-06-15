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
using Air.Cloud.Modules.Akka.Models;

namespace Air.Cloud.Modules.Akka.Abstractions;

/// <summary>
/// <para>zh-cn:Akka 消息发送授权处理器，用于在消息进入目标 Actor 前执行业务级访问控制。</para>
/// <para>en-us:Akka message authorization handler used to apply business-level access control before a message reaches the target actor.</para>
/// </summary>
public interface IAkkaMessageAuthorizationHandler
{
    /// <summary>
    /// <para>zh-cn:判断指定消息是否允许发送到目标 Actor；返回 `false` 会阻止 `Tell` 或 `Ask` 并触发授权异常。</para>
    /// <para>en-us:Determines whether a message may be sent to the target actor; returning `false` blocks `Tell` or `Ask` and raises an authorization exception.</para>
    /// </summary>
    /// <param name="descriptor">
    /// <para>zh-cn:目标 Actor 的注册描述信息。</para>
    /// <para>en-us:The registration descriptor of the target actor.</para>
    /// </param>
    /// <param name="message">
    /// <para>zh-cn:即将发送的消息对象。</para>
    /// <para>en-us:The message about to be sent.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:允许发送时返回 `true`，否则返回 `false`。</para>
    /// <para>en-us:Returns `true` when sending is allowed.</para>
    /// </returns>
    bool CanSend(AkkaActorDescriptor descriptor, object message);
}

