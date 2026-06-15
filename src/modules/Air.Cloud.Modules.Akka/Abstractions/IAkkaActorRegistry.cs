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
using Air.Cloud.Modules.Akka.Models;

namespace Air.Cloud.Modules.Akka.Abstractions;

/// <summary>
/// <para>zh-cn:Akka Actor 注册表，维护业务 Actor 名称、类型、领域、角色与运行时引用之间的映射。</para>
/// <para>en-us:Akka actor registry that keeps mappings between business actor names, types, domains, roles, and runtime references.</para>
/// </summary>
public interface IAkkaActorRegistry
{
    /// <summary>
    /// <para>zh-cn:注册或替换 Actor 描述信息；同名注册会覆盖旧值，因此跨领域 Actor 应使用不同前缀避免冲突。</para>
    /// <para>en-us:Registers or replaces an actor descriptor; later registrations with the same actor name overwrite earlier ones, so cross-domain actors should use distinct prefixes to avoid conflicts.</para>
    /// </summary>
    /// <param name="descriptor">
    /// <para>zh-cn:Actor 注册描述信息，必须包含非空的 Actor 名称。</para>
    /// <para>en-us:The actor registration descriptor, which must contain a non-empty actor name.</para>
    /// </param>
    void Register(AkkaActorDescriptor descriptor);

    /// <summary>
    /// <para>zh-cn:按名称查找 Actor 引用；名称匹配不区分大小写，未命中时输出 `ActorRefs.Nobody`。</para>
    /// <para>en-us:Finds an actor reference by name; matching is case-insensitive, and `ActorRefs.Nobody` is returned when no actor is found.</para>
    /// </summary>
    /// <param name="actorName">
    /// <para>zh-cn:已注册的 Actor 名称。</para>
    /// <para>en-us:The registered actor name.</para>
    /// </param>
    /// <param name="actorRef">
    /// <para>zh-cn:解析到的 Actor 引用；未命中时为 `ActorRefs.Nobody`。</para>
    /// <para>en-us:The resolved actor reference, or `ActorRefs.Nobody` when not found.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:找到可用 Actor 引用时返回 `true`。</para>
    /// <para>en-us:Returns `true` when a usable actor reference is found.</para>
    /// </returns>
    bool TryGet(string actorName, out IActorRef actorRef);

    /// <summary>
    /// <para>zh-cn:获取当前注册表快照；返回集合不暴露内部字典，适用于健康检查、诊断和测试断言。</para>
    /// <para>en-us:Gets a snapshot of the current registry; the returned collection does not expose the internal dictionary and is suitable for health checks, diagnostics, and test assertions.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:当前 Actor 注册描述信息集合。</para>
    /// <para>en-us:The current actor registration descriptors.</para>
    /// </returns>
    IReadOnlyCollection<AkkaActorDescriptor> GetDescriptors();
}

