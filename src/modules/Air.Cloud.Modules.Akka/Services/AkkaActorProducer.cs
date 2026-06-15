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
using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.Modules.Akka.Services;

/// <summary>
/// <para>zh-cn:Akka Actor 间接生产器，通过应用 DI 容器创建 Actor，并允许运行期显式构造参数与容器依赖共同参与构造函数匹配。</para>
/// <para>en-us:Akka indirect actor producer that creates actors through the application DI container and allows runtime constructor arguments to be combined with container-resolved dependencies.</para>
/// </summary>
internal sealed class AkkaActorProducer : IIndirectActorProducer
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Type _actorType;
    private readonly object[] _args;

    /// <summary>
    /// <para>zh-cn:创建 DI 驱动的 Actor 生产器；`serviceProvider` 与 `actorType` 必须非空，`args` 为空时会按空参数集合处理。</para>
    /// <para>en-us:Creates a DI-driven actor producer; `serviceProvider` and `actorType` must be non-null, and a null `args` value is treated as an empty argument set.</para>
    /// </summary>
    /// <param name="serviceProvider">
    /// <para>zh-cn:用于解析 Actor 构造函数依赖的应用服务提供器。</para>
    /// <para>en-us:The application service provider used to resolve actor constructor dependencies.</para>
    /// </param>
    /// <param name="actorType">
    /// <para>zh-cn:要创建的 Actor CLR 类型，必须能构造为 `ActorBase`。</para>
    /// <para>en-us:The CLR actor type to create, which must be constructible as an `ActorBase`.</para>
    /// </param>
    /// <param name="args">
    /// <para>zh-cn:调用方传入的显式构造参数，会与 DI 容器中的服务一起用于匹配 Actor 构造函数。</para>
    /// <para>en-us:The explicit constructor arguments supplied by the caller, combined with services from the DI container when matching the actor constructor.</para>
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <para>zh-cn:`serviceProvider` 或 `actorType` 为空时抛出。</para>
    /// <para>en-us:Thrown when `serviceProvider` or `actorType` is null.</para>
    /// </exception>
    public AkkaActorProducer(IServiceProvider serviceProvider, Type actorType, object[] args)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _actorType = actorType ?? throw new ArgumentNullException(nameof(actorType));
        _args = args ?? Array.Empty<object>();
    }

    /// <summary>
    /// <para>zh-cn:返回该生产器负责创建的 Actor 类型，供 Akka.NET `Props` 和诊断流程识别目标类型。</para>
    /// <para>en-us:Returns the actor type created by this producer so Akka.NET `Props` and diagnostics can identify the target type.</para>
    /// </summary>
    public Type ActorType => _actorType;

    /// <summary>
    /// <para>zh-cn:创建新的 Actor 实例；每次调用都会通过 `ActivatorUtilities` 重新构造实例，不能复用旧 Actor。</para>
    /// <para>en-us:Creates a new actor instance; each call constructs a fresh instance through `ActivatorUtilities` and never reuses a previous actor.</para>
    /// </summary>
    /// <returns>
    /// <para>zh-cn:新创建的 Actor 实例。</para>
    /// <para>en-us:The newly created actor instance.</para>
    /// </returns>
    public ActorBase Produce()
    {
        return (ActorBase)ActivatorUtilities.CreateInstance(_serviceProvider, _actorType, _args);
    }

    /// <summary>
    /// <para>zh-cn:释放 Akka.NET 交还的 Actor 实例；当实例实现 `IDisposable` 时会调用 `Dispose`，否则不执行额外操作。</para>
    /// <para>en-us:Releases an actor instance returned by Akka.NET; when the instance implements `IDisposable`, `Dispose` is called, otherwise no extra action is taken.</para>
    /// </summary>
    /// <param name="actor">
    /// <para>zh-cn:要释放的 Actor 实例。</para>
    /// <para>en-us:The actor instance to release.</para>
    /// </param>
    public void Release(ActorBase actor)
    {
        if (actor is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
