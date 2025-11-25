using Air.Cloud.Core.Standard.Authentication;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using System.Text.Encodings.Web;

namespace Air.Cloud.Core.Standard.Security.Handler
{
    public  class InternalAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {

        private readonly ISecurityHandlerStandard authenticationHandlerStandard;

        public InternalAuthenticationHandler(
           IOptionsMonitor<AuthenticationSchemeOptions> options,
           ILoggerFactory logger,
           UrlEncoder encoder,
           ISystemClock clock,
           IConfiguration configuration,
           ISecurityHandlerStandard authenticationHandlerStandard)
           : base(options, logger, encoder, clock)
        {
            authenticationHandlerStandard= authenticationHandlerStandard ?? throw new ArgumentNullException(nameof(authenticationHandlerStandard));
        }

        /// <summary>
        /// 固定Token认证
        /// </summary>
        /// <returns></returns>
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            string isAnonymous = Request.Headers["IsAnonymous"].ToString();
            if (!string.IsNullOrEmpty(isAnonymous))
            {
                bool isAuthenticated = Convert.ToBoolean(isAnonymous);
                if (isAuthenticated)
                    return AuthenticateResult.NoResult();
            }
            return authenticationHandlerStandard.VerifyAuthorization(Context);
        }
    }
}
