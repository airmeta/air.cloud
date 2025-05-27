
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
using Air.Cloud.Core;
using Air.Cloud.Core.App;
using Air.Cloud.Core.App.Startups;
using Air.Cloud.Core.Attributes;
using Air.Cloud.Modules.Kafka.Config;
using Air.Cloud.Modules.Kafka.Model;
using Air.Cloud.Modules.Nexus.Extensions;

using Confluent.Kafka;
using System.Reflection;
namespace unit.kafaka.client.entry
{
    [AppStartup(Order = int.MinValue)]
    public class Startup : AppStartup
    {
        ConsumerConfigModel consumerConfigModel = new ConsumerConfigModel() { TopicName = "fcj_events_test" };
        string GroupId = AppEnvironment.IsDevelopment ? Guid.NewGuid().ToString() : AppConst.ApplicationName;


        ProducerConfigModel producerConfigModel = new ProducerConfigModel() { TopicName = "fcj_events_test" };
        public override void ConfigureServices(IServiceCollection services)
        {
            {
                services.AddEventBus(options =>
                {
                    options.AddSubscribers(Assembly.GetEntryAssembly());
                    options.ReplaceStorager(services =>
                    {
                        var options = AppCore.GetOptions<KafkaSettingsOptions>();
                        consumerConfigModel.Config = new ConsumerConfig()
                        {
                            GroupId = GroupId,
                            BootstrapServers = options.ClusterAddress,
                            EnableSslCertificateVerification = false
                        };
                        return new KafkaEventSourceStorager(producerConfigModel, consumerConfigModel, GroupId);
                    });
                });
            }
        }
        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }
    }

    public class Contents
    {
        public string Content { get; set; }
    }
}
