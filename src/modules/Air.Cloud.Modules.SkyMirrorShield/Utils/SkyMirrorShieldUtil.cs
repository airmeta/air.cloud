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
using air.security.common.Dtos.RequestValidDtos;

using Air.Cloud.Core;
using Air.Cloud.Core.App;
using Air.Cloud.WebApp.UnifyResult.Internal;

using System.Net.Http;

namespace Air.Cloud.Modules.SkyMirrorShield.Utils
{
    public static class SkyMirrorShieldUtil
    {
        /// <summary>
        /// 票据验证
        /// </summary>
        public static string TickitValidUrl => AppConfigurationLoader.InnerConfiguration["SkyMirrorShieldSettings:SkyMirrorShieldHeaderValid:TickitValidUrl"];
        public static async Task<bool> VerifyTickitAsync(IHttpClientFactory httpClientFactory, string tickit,string ClientId=null)
        {
            if (string.IsNullOrEmpty(ClientId))
            {
                ClientId = "0";
            }
            if (string.IsNullOrEmpty(TickitValidUrl))
            {
                AppRealization.Output.Print("天镜安全校验", "未配置票据验证地址，默认验证失败");
                return false;
            }
            using (HttpClient client = httpClientFactory.CreateClient())
            {
                client.Timeout = new TimeSpan(0, 3, 0);
                var result = await client.GetAsync(TickitValidUrl.Replace("{Tickit}", tickit).Replace("{ClientId}", ClientId));

                if (result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    string Result = await result.Content.ReadAsStringAsync();
                    var data = AppRealization.JSON.Deserialize<RESTfulResult<RequestValidResult>>(Result);
                    return data.Data.Valid;
                }
                return false;
            }
        }
    }
}
