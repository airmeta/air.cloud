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
using Air.Cloud.Modules.Nacos.Model;

using Nacos.V2;

namespace Air.Cloud.Modules.Nacos.Util
{
    /// <summary>
    /// <para>zh-cn:Nacos SDK 选项构建器，负责把 Air.Cloud 配置模型映射到官方 SDK 配置。</para>
    /// <para>en-us>Nacos SDK option builder that maps the Air.Cloud option model to official SDK options.</para>
    /// </summary>
    public static class NacosOptionsBuilder
    {
        /// <summary>
        /// <para>zh-cn:创建 Nacos SDK 配置委托，并验证至少存在一个服务端地址。</para>
        /// <para>en-us>Creates a Nacos SDK option delegate and validates that at least one server address exists.</para>
        /// </summary>
        /// <param name="serviceOptions">
        /// <para>zh-cn:Nacos 模块配置。</para>
        /// <para>en-us>The Nacos module options.</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:可直接传入 Nacos SDK 注册方法的配置委托。</para>
        /// <para>en-us>The configure delegate that can be passed to Nacos SDK registration methods.</para>
        /// </returns>
        public static Action<NacosSdkOptions> Build(NacosServiceOptions serviceOptions)
        {
            var addresses = GetServerAddresses(serviceOptions).ToList();
            if (addresses.Count == 0)
            {
                throw new ArgumentException("NacosServiceOptions.ServerAddresses or NacosServiceOptions.ServerAddress is required.", nameof(serviceOptions));
            }

            return sdkOptions =>
            {
                sdkOptions.ServerAddresses = addresses;
                sdkOptions.ContextPath = string.IsNullOrWhiteSpace(serviceOptions.ContextPath) ? "nacos" : serviceOptions.ContextPath;
                sdkOptions.DefaultTimeOut = serviceOptions.DefaultTimeOut;
                sdkOptions.Namespace = serviceOptions.Namespace;
                sdkOptions.UserName = serviceOptions.UserName;
                sdkOptions.Password = serviceOptions.Password;
            };
        }

        private static IEnumerable<string> GetServerAddresses(NacosServiceOptions serviceOptions)
        {
            foreach (var address in serviceOptions.ServerAddresses ?? Array.Empty<string>())
            {
                if (!string.IsNullOrWhiteSpace(address))
                {
                    yield return address;
                }
            }

            if (!string.IsNullOrWhiteSpace(serviceOptions.ServerAddress))
            {
                yield return serviceOptions.ServerAddress;
            }
        }
    }
}
