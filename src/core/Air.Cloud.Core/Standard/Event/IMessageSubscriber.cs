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
namespace Air.Cloud.Core.Standard.Event;


/// <summary>
/// 事件处理器依赖接口
/// </summary>
/// <remarks>
/// <para>可自定义事件处理方法，但须符合 Func{EventSubscribeExecutingContext, Task} 签名</para>
/// <para>通常只做依赖查找，不做服务调用</para>
/// </remarks>
public interface IMessageSubscriber
{
    /*
     * // 事件处理程序定义规范
     * [EventDescriptionAttribute(YourEventID)]
     * public Task YourHandler(EventHandlerExecutingContext context)
     * {
     *     // To Do...
     * }
     */
}