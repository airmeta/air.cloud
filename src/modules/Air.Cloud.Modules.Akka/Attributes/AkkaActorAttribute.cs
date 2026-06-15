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
namespace Air.Cloud.Modules.Akka.Attributes;

/// <summary>
/// <para>zh-cn:声明 Akka Actor 自动注册元数据，模块启动时会扫描该特性并创建匹配 Actor。</para>
/// <para>en-us:Declares Akka actor auto-registration metadata scanned by the module during startup to create matching actors.</para>
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class AkkaActorAttribute : Attribute
{
    /// <summary>
    /// <para>zh-cn:创建 Actor 自动注册标记；`actorName` 是业务发送消息时使用的基础名称，启用领域前缀时模块会组合最终注册名。</para>
    /// <para>en-us:Creates an actor auto-registration marker; `actorName` is the base name used by business code when sending messages, and the final registered name is composed by the module when a Domain prefix is enabled.</para>
    /// </summary>
    /// <param name="actorName">
    /// <para>zh-cn:Actor 基础名称，不能为空。</para>
    /// <para>en-us:The actor base name, which must not be empty.</para>
    /// </param>
    public AkkaActorAttribute(string actorName)
    {
        ActorName = actorName;
    }

    /// <summary>
    /// <para>zh-cn:Actor 基础名称；未配置领域时直接作为注册名称使用。</para>
    /// <para>en-us:The actor base name; when no domain is configured, it is used directly as the registration name.</para>
    /// </summary>
    public string ActorName { get; }

    /// <summary>
    /// <para>zh-cn:业务领域名称，用于组合注册名称并读取领域隔离配置。</para>
    /// <para>en-us:The business domain name used to compose the registration name and read domain isolation settings.</para>
    /// </summary>
    public string Domain { get; set; }

    /// <summary>
    /// <para>zh-cn:运行角色；设置后只有当前节点包含该角色时才会注册该 Actor。</para>
    /// <para>en-us:The runtime role; when set, the actor is registered only if the current node contains this role.</para>
    /// </summary>
    public string Role { get; set; }
}

