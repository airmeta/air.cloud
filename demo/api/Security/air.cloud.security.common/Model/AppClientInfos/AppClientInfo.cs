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
using Microsoft.AspNetCore.Http;

namespace air.cloud.security.common.Model.AppClientInfos
{
    public class AppClientInfo : IAppClientInfo
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        public AppClientInfo(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;

        }

        public IDictionary<string, string> GetAllClientHeaders()
        {
            var headers = new Dictionary<string, string>();
            foreach (var header in _httpContextAccessor.HttpContext.Request.Headers)
            {
                headers[header.Key] = header.Value;
            }
            return headers;
        }

        public string GetClientAppId()
        {
            string AppId = _httpContextAccessor.HttpContext.Request.Headers[IAppClientInfo.CLIENT_APPID_HEADER];
            return AppId;
        }

        public string GetClientNonce()
        {
            string Nonce = _httpContextAccessor.HttpContext.Request.Headers[IAppClientInfo.CLIENT_NONCE_HEADER];
            return Nonce;
        }

        public string GetClientProperty(string headerKey)
        {
            string Value = _httpContextAccessor.HttpContext.Request.Headers[headerKey];
            return Value;
        }

        public string GetClientSign()
        {
            return _httpContextAccessor.HttpContext.Request.Headers[IAppClientInfo.CLIENT_SIGN_HEADER];
        }

        public string GetClientTicket()
        {
            return _httpContextAccessor.HttpContext.Request.Headers[IAppClientInfo.CLIENT_Ticket_HEADER];
        }

        public string GetClientTimeStamp()
        {
            return _httpContextAccessor.HttpContext.Request.Headers[IAppClientInfo.CLIENT_TIMESTAMP_HEADER];
        }
    }

}
