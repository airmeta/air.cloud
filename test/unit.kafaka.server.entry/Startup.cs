
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
using Air.Cloud.Core.Standard.MessageQueue;
using Air.Cloud.Core.Standard.MessageQueue.Config;
using Air.Cloud.Modules.Kafka.Config;
using Air.Cloud.Modules.Kafka.Model;

using Confluent.Kafka;
namespace unit.skywlking.entry
{
    [AppStartup(Order = int.MinValue)]
    public class Startup : AppStartup
    {
        public override void ConfigureServices(IServiceCollection services)
        {
           
        }
        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            //ProducerConfigModel producerConfigModel = new ProducerConfigModel();
            //producerConfigModel.TopicName= "fcj_network_service";
            //AppRealization.Queue.Publish<ProducerConfig, Contents>(producerConfigModel, new Contents()
            //{
            //    Content = "123123"
            //});
            ConsumerConfigModel producerConfigModel = new ConsumerConfigModel();

            producerConfigModel.TopicName = "fcj_workflow_audit_test";
            string GroupId = AppEnvironment.IsDevelopment ? Guid.NewGuid().ToString() : AppConst.ApplicationName;
            var options = AppCore.GetOptions<KafkaSettingsOptions>();
            producerConfigModel.Config = new ConsumerConfig()
            {
                GroupId = GroupId,
                BootstrapServers = options.ClusterAddress,
                EnableSslCertificateVerification=false
            };
            AppRealization.Queue.Subscribe<ConsumerConfig, object>(producerConfigModel, (s) =>
            {
                Console.WriteLine(AppRealization.JSON.Serialize(s));
            }, GroupId);
        }
    }
    public class Contents 
    {
        public string Content { get ; set ; }
    }
}
