using Air.Cloud.Core.Standard.Authentication;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

using System.Security.Claims;

namespace Air.Cloud.Core.Standard.Security.Handler
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
