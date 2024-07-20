// Copyright (c) 2020-2022 百小僧, Baiqian Co.,Ltd.
// Furion is licensed under Mulan PSL v2.
// You can use this software according to the terms and conditions of the Mulan PSL v2.
// You may obtain a copy of Mulan PSL v2 at:
//             https://gitee.com/dotnetchina/Furion/blob/master/LICENSE
// THIS SOFTWARE IS PROVIDED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO NON-INFRINGEMENT, MERCHANTABILITY OR FIT FOR A PARTICULAR PURPOSE.
// See the Mulan PSL v2 for more details.

using Air.Cloud.Core;
using Air.Cloud.Core.App;
using Air.Cloud.Core.Extensions;
using Air.Cloud.Core.Standard.Account;
using Air.Cloud.Core.Standard.Print;
using Air.Cloud.Plugins.Jwt.Options;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace Air.Cloud.Plugins.Jwt
{
    /// <summary>
    /// JWT 加解密
    /// </summary>
    public class JWTEncryption
    {
        public static JWTSettingsOptions JWTSettingsOptions => AppCore.GetOptions<JWTSettingsOptions>();

        /// <summary>
        /// 通过账号信息生成 Token
        /// </summary>
        /// <typeparam name="AccountStandardDependency"></typeparam>
        /// <param name="account"></param>
        /// <param name="expiredTime"></param>
        /// <returns></returns>
        public static string Create<AccountStandardDependency>(AccountStandardDependency account, long? expiredTime = null) where AccountStandardDependency : AccountStandard, new()
        {
            return Encrypt(account.AccountInformation, expiredTime);
        }
        /// <summary>
        /// 生成 Token
        /// </summary>
        /// <param name="payload"></param>
        /// <param name="expiredTime">过期时间（分钟）</param>
        /// <returns></returns>
        public static string Encrypt(IDictionary<string, object> Payload, long? expiredTime = null)
        {
            var Payloads = CombinePayload(Payload, expiredTime);
            var stringPayload = Payloads is JwtPayload jwtPayload ? jwtPayload.SerializeToJson() : JsonSerializer.Serialize(Payloads);
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JWTSettingsOptions.IssuerSigningKey));
            var credentials = new SigningCredentials(securityKey, JWTSettingsOptions.Algorithm);
            var tokenHandler = new JsonWebTokenHandler();
            return tokenHandler.CreateToken(stringPayload, credentials);
        }
        /// <summary>
        /// 生成刷新 Token
        /// </summary>
        /// <param name="accessToken"></param>
        /// <param name="expiredTime">刷新 Token 有效期（分钟）</param>
        /// <returns></returns>
        public static string GenerateRefreshToken(string accessToken, int expiredTime = 43200)
        {
            // 分割Token
            var tokenParagraphs = accessToken.Split('.', StringSplitOptions.RemoveEmptyEntries);

            var s = RandomNumberGenerator.GetInt32(10, tokenParagraphs[1].Length / 2 + 2);
            var l = RandomNumberGenerator.GetInt32(3, 13);

            var payload = new Dictionary<string, object>
            {
                { "f",tokenParagraphs[0] },
                { "e",tokenParagraphs[2] },
                { "s",s },
                { "l",l },
                { "k",tokenParagraphs[1].Substring(s,l) }
            };

            return Encrypt(payload, expiredTime);
        }

        /// <summary>
        /// 通过过期Token 和 刷新Token 换取新的 Token
        /// </summary>
        /// <param name="expiredToken"></param>
        /// <param name="refreshToken"></param>
        /// <param name="expiredTime">过期时间（分钟）</param>
        /// <param name="clockSkew">刷新token容差值，秒做单位</param>
        /// <returns></returns>
        public static string Exchange(string expiredToken, string refreshToken, JsonWebToken refreshTokenObj, long? expiredTime = null, long clockSkew = 5)
        {
            // 判断这个刷新Token 是否已刷新过
            var blacklistRefreshKey = "BLACKLIST_REFRESH_TOKEN:" + refreshToken;
            var distributedCache = AppCore.HttpContext?.RequestServices?.GetService<IDistributedCache>();

            // 处理token并发容错问题
            var nowTime = DateTimeOffset.UtcNow;
            var cachedValue = distributedCache?.GetString(blacklistRefreshKey);
            var isRefresh = !string.IsNullOrWhiteSpace(cachedValue);    // 判断是否刷新过
            if (isRefresh)
            {
                var refreshTime = new DateTimeOffset(long.Parse(cachedValue), TimeSpan.Zero);
                // 处理并发时容差值
                if ((nowTime - refreshTime).TotalSeconds > clockSkew) return default;
            }

            // 分割过期Token
            var tokenParagraphs = expiredToken.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (tokenParagraphs.Length < 3) return default;

            // 判断各个部分是否匹配
            if (!refreshTokenObj.GetPayloadValue<string>("f").Equals(tokenParagraphs[0])) return default;
            if (!refreshTokenObj.GetPayloadValue<string>("e").Equals(tokenParagraphs[2])) return default;
            if (!tokenParagraphs[1].Substring(refreshTokenObj.GetPayloadValue<int>("s"), refreshTokenObj.GetPayloadValue<int>("l")).Equals(refreshTokenObj.GetPayloadValue<string>("k"))) return default;

            // 获取过期 Token 的存储信息
            var jwtSecurityToken = SecurityReadJwtToken(expiredToken);
            var payload = jwtSecurityToken.Payload;

            // 移除 Iat，Nbf，Exp
            foreach (var innerKey in DateTypeClaimTypes)
            {
                if (!payload.ContainsKey(innerKey)) continue;

                payload.Remove(innerKey);
            }

            // 交换成功后登记刷新Token，标记失效
            if (!isRefresh)
            {
                distributedCache?.SetString(blacklistRefreshKey, nowTime.Ticks.ToString(), new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = DateTimeOffset.FromUnixTimeSeconds(refreshTokenObj.GetPayloadValue<long>(JwtRegisteredClaimNames.Exp))
                });
            }

            return Encrypt(payload, expiredTime);
        }

        /// <summary>
        /// 检查Token是否有效，无效则刷新Token 并设置新Token 如果刷新Token无效则返回false
        /// </summary>
        /// <param name="context"></param>
        /// <param name="httpContext"></param>
        /// <param name="expiredTime">新 Token 过期时间（分钟）</param>
        /// <param name="refreshTokenExpiredTime">新刷新 Token 有效期（分钟）</param>
        /// <param name="tokenPrefix"></param>
        /// <param name="clockSkew"></param>
        /// <returns></returns>
        public static bool ValidateToken(AuthorizationHandlerContext context, DefaultHttpContext httpContext, long? expiredTime = null, int refreshTokenExpiredTime = 43200, string tokenPrefix = "Bearer ", long clockSkew = 5)
        {
            // 如果验证有效，则跳过刷新
            if ((context?.User?.Identity?.IsAuthenticated)??false) return true;

            // 判断是否含有匿名特性
            if (httpContext.GetEndpoint()?.Metadata?.GetMetadata<AllowAnonymousAttribute>() != null) return true;
            #region  验证Token
            var expiredToken = GetJwtBearerToken(httpContext, JWTSettingsOptions.AuthorizationKey, tokenPrefix: tokenPrefix);
            if (expiredToken.IsNullOrEmpty()) return false;
            var (_isValid, _, _) = Validate(expiredToken);
            if (_isValid)
            {
                #region  验证刷新Token
                var refreshToken = GetJwtBearerToken(httpContext, JWTSettingsOptions.RefreshAuthorizationKey, tokenPrefix: tokenPrefix);
                if (!refreshToken.IsNullOrEmpty())
                {
                    // 判断刷新Token 是否过期
                    var (isValid, refreshTokenObj, _) = Validate(refreshToken);
                    if (!isValid) return true;
                    #endregion
                    #region 刷新Token
                    // 交换新的 Token
                    var accessToken = Exchange(expiredToken, refreshToken, refreshTokenObj, expiredTime, clockSkew);
                    if (string.IsNullOrWhiteSpace(accessToken)) return false;

                    // 读取新的 Token Clamis
                    var claims = ReadJwtToken(accessToken)?.Claims;
                    if (claims == null) return false;

                    // 创建身份信息
                    var claimIdentity = new ClaimsIdentity("AuthenticationTypes.Federation");
                    claimIdentity.AddClaims(claims);
                    var claimsPrincipal = new ClaimsPrincipal(claimIdentity);

                    // 设置 HttpContext.User 并登录
                    httpContext.User = claimsPrincipal;
                    httpContext.SignInAsync(claimsPrincipal);

                    // 返回新的 Token
                    httpContext.Response.Headers[JWTSettingsOptions.AuthorizationKey] = accessToken;
                    // 返回新的 刷新Token
                    httpContext.Response.Headers[JWTSettingsOptions.RefreshAuthorizationKey] = GenerateRefreshToken(accessToken, refreshTokenExpiredTime);

                    // 处理 axios 问题
                    httpContext.Response.Headers.TryGetValue("Access-Control-Expose-Headers", out var acehs);
                    httpContext.Response.Headers["Access-Control-Expose-Headers"] = string.Join(',', StringValues.Concat(acehs, new StringValues(new[] { JWTSettingsOptions.AuthorizationKey, JWTSettingsOptions.RefreshAuthorizationKey })).Distinct());

                }
                #endregion
            }
            #endregion

            return false;
        }

        /// <summary>
        /// 验证 Token
        /// </summary>
        /// <param name="accessToken"></param>
        /// <returns></returns>
        public static (bool IsValid, JsonWebToken Token, TokenValidationResult validationResult) Validate(string accessToken)
        {
            if (JWTSettingsOptions == null) return (false, default, default);

            // 加密Key
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JWTSettingsOptions.IssuerSigningKey));
            var creds = new SigningCredentials(key, JWTSettingsOptions.Algorithm);

            // 创建Token验证参数
            var tokenValidationParameters = CreateTokenValidationParameters(JWTSettingsOptions);
            tokenValidationParameters.IssuerSigningKey ??= creds.Key;

            // 验证 Token
            var tokenHandler = new JsonWebTokenHandler();
            try
            {
                var tokenValidationResult = tokenHandler.ValidateToken(accessToken, tokenValidationParameters);
                if (!tokenValidationResult.IsValid) return (false, null, tokenValidationResult);

                var jsonWebToken = tokenValidationResult.SecurityToken as JsonWebToken;
                return (true, jsonWebToken, tokenValidationResult);
            }
            catch
            {
                return (false, default, default);
            }
        }

        /// <summary>
        /// 验证 Token
        /// </summary>
        /// <param name="httpContext"></param>
        /// <param name="token"></param>
        /// <param name="headerKey"></param>
        /// <param name="tokenPrefix"></param>
        /// <returns></returns>
        public static bool ValidateJwtBearerToken(DefaultHttpContext httpContext, out JsonWebToken token, string headerKey = "Authorization", string tokenPrefix = "Bearer ")
        {
            // 获取 token
            var accessToken = GetJwtBearerToken(httpContext, headerKey, tokenPrefix);
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                token = null;
                return false;
            }

            // 验证token
            var (IsValid, Token, _) = Validate(accessToken);
            token = IsValid ? Token : null;

            return IsValid;
        }

        /// <summary>
        /// 读取 Token，不含验证
        /// </summary>
        /// <param name="accessToken"></param>
        /// <returns></returns>
        public static JsonWebToken ReadJwtToken(string accessToken)
        {
            var tokenHandler = new JsonWebTokenHandler();
            if (tokenHandler.CanReadToken(accessToken))
            {
                return tokenHandler.ReadJsonWebToken(accessToken);
            }

            return default;
        }

        /// <summary>
        /// 读取 Token，不含验证
        /// </summary>
        /// <param name="accessToken"></param>
        /// <returns></returns>
        public static JwtSecurityToken SecurityReadJwtToken(string accessToken)
        {
            var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
            var jwtSecurityToken = jwtSecurityTokenHandler.ReadJwtToken(accessToken);
            return jwtSecurityToken;
        }

        /// <summary>
        /// 获取 JWT Bearer Token
        /// </summary>
        /// <param name="httpContext"></param>
        /// <param name="headerKey"></param>
        /// <param name="tokenPrefix"></param>
        /// <returns></returns>
        public static string GetJwtBearerToken(DefaultHttpContext httpContext, string headerKey = "Authorization", string tokenPrefix = "Bearer ")
        {
            // 判断请求报文头中是否有 "Authorization" 报文头
            var bearerToken = httpContext.Request.Headers[headerKey].ToString();
            if (string.IsNullOrWhiteSpace(bearerToken)) return default;

            var prefixLenght = tokenPrefix.Length;
            return bearerToken.StartsWith(tokenPrefix, true, null) && bearerToken.Length > prefixLenght ? bearerToken[prefixLenght..] : default;
        }

        /// <summary>
        /// 生成Token验证参数
        /// </summary>
        /// <param name="jwtSettings"></param>
        /// <returns></returns>
        public static TokenValidationParameters CreateTokenValidationParameters(JWTSettingsOptions jwtSettings)
        {
            return new TokenValidationParameters
            {
                // 验证签发方密钥
                ValidateIssuerSigningKey = jwtSettings.ValidateIssuerSigningKey.Value,
                // 签发方密钥
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.IssuerSigningKey)),
                // 验证签发方
                ValidateIssuer = jwtSettings.ValidateIssuer.Value,
                // 设置签发方
                ValidIssuer = jwtSettings.ValidIssuer,
                // 验证签收方
                ValidateAudience = jwtSettings.ValidateAudience.Value,
                // 设置接收方
                ValidAudience = jwtSettings.ValidAudience,
                // 验证生存期
                ValidateLifetime = jwtSettings.ValidateLifetime.Value,
                // 过期时间容错值
                ClockSkew = TimeSpan.FromSeconds(jwtSettings.ClockSkew.Value),
            };
        }

        /// <summary>
        /// 组合 Claims 负荷
        /// </summary>
        /// <param name="payload"></param>
        /// <param name="expiredTime">过期时间，单位：分钟</param>
        /// <returns></returns>
        private static IDictionary<string, object> CombinePayload(IDictionary<string, object> payload, long? expiredTime = null)
        {
            var datetimeOffset = DateTimeOffset.UtcNow;

            if (!payload.ContainsKey(JwtRegisteredClaimNames.Iat))
            {
                payload.Add(JwtRegisteredClaimNames.Iat, datetimeOffset.ToUnixTimeSeconds());
            }

            if (!payload.ContainsKey(JwtRegisteredClaimNames.Nbf))
            {
                payload.Add(JwtRegisteredClaimNames.Nbf, datetimeOffset.ToUnixTimeSeconds());
            }

            if (!payload.ContainsKey(JwtRegisteredClaimNames.Exp))
            {
                var minute = expiredTime ?? JWTSettingsOptions?.ExpiredTime ?? 20;
                payload.Add(JwtRegisteredClaimNames.Exp, DateTimeOffset.UtcNow.AddMinutes(minute).ToUnixTimeSeconds());
            }

            if (!payload.ContainsKey(JwtRegisteredClaimNames.Iss))
            {
                payload.Add(JwtRegisteredClaimNames.Iss, JWTSettingsOptions?.ValidIssuer);
            }

            if (!payload.ContainsKey(JwtRegisteredClaimNames.Aud))
            {
                payload.Add(JwtRegisteredClaimNames.Aud, JWTSettingsOptions?.ValidAudience);
            }

            return payload;
        }

        /// <summary>
        /// 设置默认 Jwt 配置
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        internal static JWTSettingsOptions SetDefaultJwtSettings(JWTSettingsOptions options)
        {
            options.ValidateIssuerSigningKey ??= true;
            if (options.ValidateIssuerSigningKey == true) options.IssuerSigningKey ??= GetOrSetDefaultTokenSigningKey();
            options.ValidateIssuer ??= true;
            if (options.ValidateIssuer == true) options.ValidIssuer ??= "air.cloud.cor";
            options.ValidateAudience ??= true;
            if (options.ValidateAudience == true) options.ValidAudience ??= "air.cloud.webapp";
            options.ValidateLifetime ??= true;
            if (options.ValidateLifetime == true) options.ClockSkew ??= 10;
            options.ExpiredTime ??= 20;
            options.Algorithm ??= SecurityAlgorithms.HmacSha256;

            return options;
        }
        /// <summary>
        /// 获取或设置默认的Token签发密钥
        /// </summary>
        /// <returns></returns>
        internal static string GetOrSetDefaultTokenSigningKey()
        {
            string Path = AppConst.ApplicationPath + "jwt_keys.txt";
            string Guids;
            if (File.Exists(Path))
            {
                Guids = File.ReadAllText(Path);
                if (Guids.IsNullOrEmpty()) Guids = Guid.NewGuid().ToString();
            }
            else
            {
                Guids = Guid.NewGuid().ToString();
                File.WriteAllText(Path, Guids);
            }
            AppRealization.Output.Print(new AppPrintInformation
            {
                Title = "domain-security",
                Level = AppPrintInformation.AppPrintLevel.Error,
                Content = "当前系统未设置Token签发密钥,系统已为你自动生成:" + Guids + ",该密钥存储在项目文件夹下的:jwt_keys.txt文件中",
                State = true
            });

            return Guids;
        }
        /// <summary>
        /// 日期类型的 Claim 类型
        /// </summary>
        private static readonly string[] DateTypeClaimTypes = new[] { JwtRegisteredClaimNames.Iat, JwtRegisteredClaimNames.Nbf, JwtRegisteredClaimNames.Exp };
    }
}
