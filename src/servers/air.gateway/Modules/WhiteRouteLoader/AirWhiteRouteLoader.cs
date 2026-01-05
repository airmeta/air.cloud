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
using Air.Cloud.Core.Extensions;
using Air.Cloud.Modules.Consul.Model;

namespace air.gateway.Modules.WhiteRouteLoader
{
    public  class AirWhiteRouteLoader
    {
        private readonly List<string> WhiteListJSON = new List<string>();
        /// <summary>
        /// 系统请求白名单配置地址
        /// </summary>
        private static string WhiteRouteConfigUrl = "{0}/default_app_whiteroute_settings.json";
        /// <summary>
        /// 获取Http请求白名单配置
        /// </summary>
        /// <returns></returns>
        public async Task<List<string>> GetWhiteRouteLoaderAsync()
        {
            var options = AppConfigurationLoader.InnerConfiguration.GetConfig<ConsulServiceOptions>();
            string Url=string.Format(WhiteRouteConfigUrl,options.ServiceName.Replace(".", "/"));
            AppRealization.Output.Print("网关白名单路由信息读取", $"读取地址: {Url},键值对中心是否为空:{AppRealization.KVCenter==null}");
            var result= await AppRealization.KVCenter.GetAsync<ConsulKvCenterServiceInformation>(Url);
            string Value= result.Value;
            if(Value.IsNullOrEmpty())
            {
                await AppRealization.KVCenter.AddOrUpdateAsync(Url,AppRealization.JSON.Serialize(WhiteListJSON));
                return WhiteListJSON;
            }
            return AppRealization.JSON.Deserialize<List<string>>(Value);
        }


    }
}
