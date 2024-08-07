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
using Air.Cloud.Core.Standard.MessageQueue;
using Air.Cloud.Modules.Kafka.Helper;

using Microsoft.Extensions.DependencyInjection;

namespace Air.Cloud.Modules.Kafka.Extensions
{
    public static class KafkaModuleExtensions
    {
        /// <summary>
        /// Web初始化Kafka
        /// </summary>
        /// <param name="services">Web服务</param>
        /// <returns>服务列表</returns>
        public static IServiceCollection AddKafkaService(this IServiceCollection services)
        {
            services.AddSingleton<IMessageQueueStandard, KafkaMessageQueueDependency>();
            return services;
        }
    }
}
