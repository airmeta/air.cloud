/*
 * Copyright (c) 2024 星曳数据
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 *
 * This file is provided under the Mozilla Public License Version 2.0,
 * and the "NO WARRANTY" clause of the MPL is hereby expressly
 * acknowledged.
 */
using Air.Cloud.Core.Plugins.IdGenerator;

using Mapster;

using Air.Cloud.Core.Plugins.IdGenerator.Impl;

namespace Air.Cloud.Core.App
{
    public static partial class AppCore
    {
        /// <summary>
        /// 泛型实现连续生成GUID 
        /// </summary>
        /// <remarks>
        /// 自定义实现  你可以实现自己的方式
        /// </remarks>
        /// <returns></returns>
        public static string Guid<T, K>(T t, K k, bool Format = true) where T : class, IUniqueGuidGenerator, new() where K : IUniqueGuidCreatOptions, new()
        {
            k = k == null ? new UniqueGuidCreatOptions { LittleEndianBinary16Format = true }.Adapt<K>() : k;
            var guid = t.Create(k);
            if (Format) return guid.ToString().Replace("-", "");
            return guid.ToString();
        }

        /// <summary>
        /// 默认方式生成Guid
        /// </summary>
        /// <returns></returns>
        public static string Guid(bool Format = true)
        {
            return Guid(new UniqueGuidGenerator(), new UniqueGuidCreatOptions { LittleEndianBinary16Format = true }, Format);
        }
    }
}
