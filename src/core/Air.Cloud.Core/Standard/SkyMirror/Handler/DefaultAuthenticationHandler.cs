
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
using Air.Cloud.Core.Standard.SkyMirror;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

using System.Security.Claims;

namespace Air.Cloud.Core.Standard.SkyMirror.Handler
{
    public class DefaultAuthenticationHandler : ISecurityHandlerStandard
    {

        public AuthenticationTicket GetAuthenticationTicket(HttpContext context,string SchemeName, IDictionary<string, string> claims)
        {
            var identity = new ClaimsIdentity(claims.Select(s => new Claim(s.Key, s.Value)), SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, new AuthenticationProperties(), SchemeName);
            context.User = principal;
            return ticket;
        }

        public AuthenticateResult VerifyAuthorization(HttpContext Context)
        {
            return AuthenticateResult.NoResult();
        }
    }
}
