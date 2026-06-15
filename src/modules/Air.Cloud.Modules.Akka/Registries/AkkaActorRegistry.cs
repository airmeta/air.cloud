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
using Air.Cloud.Modules.Akka.Abstractions;
using Air.Cloud.Modules.Akka.Models;

namespace Air.Cloud.Modules.Akka.Registries;

/// <summary>
/// <para>zh-cn:默认 Akka Actor 注册表，使用不区分大小写的内存字典保存 Actor 描述信息。</para>
/// <para>en-us:Default Akka actor registry that stores actor descriptors in a case-insensitive in-memory dictionary.</para>
/// </summary>
public class AkkaActorRegistry : IAkkaActorRegistry
{
    private readonly Dictionary<string, AkkaActorDescriptor> _descriptors = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _locker = new();

    /// <summary>
    /// <para>zh-cn:注册或替换 Actor 描述信息；实现会用锁保护内部字典，适合宿主启动期间自动注册和运行期手动注册。</para>
    /// <para>en-us:Registers or replaces an actor descriptor; this implementation protects the internal dictionary with a lock and is suitable for host startup and runtime manual registration.</para>
    /// </summary>
    /// <param name="descriptor">
    /// <para>zh-cn:Actor 注册描述信息，必须非空且包含非空 Actor 名称。</para>
    /// <para>en-us:The actor registration descriptor, which must contain a non-empty actor name.</para>
    /// </param>
    public void Register(AkkaActorDescriptor descriptor)
    {
        if (descriptor == null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (string.IsNullOrWhiteSpace(descriptor.ActorName))
        {
            throw new ArgumentException("ActorName can not be empty.", nameof(descriptor));
        }

        lock (_locker)
        {
            _descriptors[descriptor.ActorName] = descriptor;
        }
    }

    /// <summary>
    /// <para>zh-cn:按名称查找 Actor 引用，名称匹配不区分大小写；未命中时输出 `ActorRefs.Nobody`。</para>
    /// <para>en-us:Finds an actor reference by name using case-insensitive matching; when no match is found, the output reference is `ActorRefs.Nobody`.</para>
    /// </summary>
    /// <param name="actorName">
    /// <para>zh-cn:Actor 注册名称。</para>
    /// <para>en-us:The actor registration name.</para>
    /// </param>
    /// <param name="actorRef">
    /// <para>zh-cn:解析到的 Actor 引用。</para>
    /// <para>en-us:The resolved actor reference.</para>
    /// </param>
    /// <returns>
    /// <para>zh-cn:找到已注册且引用非空的 Actor 时返回 `true`。</para>
    /// <para>en-us:Returns `true` when a registered actor is found.</para>
    /// </returns>
    public bool TryGet(string actorName, out IActorRef actorRef)
    {
        lock (_locker)
        {
            if (_descriptors.TryGetValue(actorName, out var descriptor) && descriptor.ActorRef != null)
            {
                actorRef = descriptor.ActorRef;
                return true;
            }
        }

        actorRef = ActorRefs.Nobody;
        return false;
    }

    /// <summary>
    /// <para>zh-cn:返回当前注册描述信息快照；复制过程在锁内完成，避免外部枚举可变内部集合。</para>
    /// <para>en-us:Returns a snapshot of current registration descriptors; the copy is made under lock to avoid external enumeration over the mutable internal collection.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:Actor 注册描述信息快照。</para>
    /// <para>en-us:A snapshot of actor registration descriptors.</para>
    /// </returns>
    public IReadOnlyCollection<AkkaActorDescriptor> GetDescriptors()
    {
        lock (_locker)
        {
            return _descriptors.Values.ToArray();
        }
    }
}

