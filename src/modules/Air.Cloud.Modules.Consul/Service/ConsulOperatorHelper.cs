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
using Air.Cloud.Modules.Consul.Model;

using Consul;

using System.Net;

namespace Air.Cloud.Modules.Consul.Service
{
    public class ConsulOperatorHelper
    {
        public static ConsulClient ConsulClient = null;
        public ConsulOperatorHelper(string ConsulClusterAddress)
        {
            ConsulClient = new ConsulClient(configuration =>
            {
                //服务注册的地址，集群中任意一个地址
                configuration.Address = new Uri(ConsulClusterAddress);
            });
        }
        public ConsulClient GetConsulClient()
        {
            return ConsulClient;
        }
        public Tuple<bool, ConsulServiceOptions> InitConsulRegistration(ConsulServiceOptions serviceOptions)
        {
            #region  检查Consul服务集群
            var services = ConsulClient.Health.Service(serviceOptions.ServiceName, null, false, null).Result.Response;//健康的服务

            #region 1.检查注册的IP地址是否冲突
            var serviceUrls = services.Select(p => new
            {
                Address = $"http://{p.Service.Address + ":" + p.Service.Port}",
                p.Service
            }).ToList();
            bool CheckSuccess = true;
            //如果有相同IP地址的服务,则不注册
            if (serviceUrls.Count(s => serviceOptions.ServiceAddress.Contains(s.Address) || s.Address.Contains(serviceOptions.ServiceAddress)) > 0)
            {
                var ServicesInfo = serviceUrls.FirstOrDefault(s => s.Service.ID == serviceOptions.ServiceId);
                //排除自己重启的情况
                if (ServicesInfo != null && ServicesInfo.Service.ID != serviceOptions.ServiceId)
                {
                    CheckSuccess = false;
                }
            }
            if (!CheckSuccess)
            {
                AppRealization.Output.Error(new HttpListenerException(1219,"当前目录下的应用程序IP地址已被使用"));
            }
            #endregion

            #region 2.检测当前ID 是否在集群里面冲突 冲突(指的是当前ID绑定了其他端口的项目 并且那个项目正在使用)
            if (serviceUrls.Count(s => s.Service.ID == serviceOptions.ServiceId) > 0)
            {
                var ServicesInfo = serviceUrls.FirstOrDefault(s => s.Service.ID == serviceOptions.ServiceId);
                //判断是否为自己重启 重启不更换PID
                if (!(serviceOptions.ServiceAddress.Contains(ServicesInfo.Address) 
                    || ServicesInfo.Address.Contains(serviceOptions.ServiceAddress)))
                {
                    //表示ID冲突 需要重新生成
                    //出现这个情况是因为 该项目文件是在已注册的服务环境复制出来的 需要进行更换
                    serviceOptions.ServiceId = AppRealization.PID.Get();
                }
            }
            #endregion

            #endregion
            return new Tuple<bool, ConsulServiceOptions>(true, serviceOptions);
        }
    }
}
