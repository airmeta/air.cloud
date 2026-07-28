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
using air.cloud.account.service.Dtos.AccountDtos;
using air.cloud.account.service.Services.AccountServices.AccountServices;
using air.cloud.account.service.Utils;
using air.cloud.security.common.Enums;
using air.cloud.system.model.Domains.AppInfoDomains;
using air.cloud.system.model.Domains.UserDomains;
using air.cloud.system.model.Dtos.UserAccountLogDtos;
using air.cloud.system.model.Entitys.Apps;

using Air.Cloud.Core;
using Air.Cloud.Core.App;
using Air.Cloud.Core.Extensions;
using Air.Cloud.WebApp.FriendlyException;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using System.ComponentModel;

namespace air.cloud.account.service.Impls.AccountServices.AccountServices
{
    /// <summary>
    /// <para>zh-cn:账户服务</para>
    /// <para>en-us:Account Service</para>
    /// </summary>
    [Route("v1/identity/account")]
    [Description("账户管理")]
    public class AccountService : IAccountService
    {
        private readonly IAppInfoDomain appInfoDomain;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserDomain userDomain;
        private readonly IUserAccountLogDomain userAccountLogDomain;
        public AccountService(IAppInfoDomain appInfoDomain,IUserDomain userDomain, IUserAccountLogDomain userAccountLogDomain, IHttpContextAccessor httpContextAccessor)
        {
            this.appInfoDomain = appInfoDomain;
            this.userDomain = userDomain;   
            this.userAccountLogDomain = userAccountLogDomain;
            this._httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// <para>zh-cn:修改密码</para>
        /// <para>en-us:Change password</para>
        /// </summary>
        /// <param name="dto">
        ///  <para>zh-cn:账户数据传输对象（包含加密负载）</para>
        ///  <para>en-us:Account DTO (contains encrypted payload)</para>
        /// </param>
        /// <returns>
        ///  <para>zh-cn:返回修改结果，true表示成功，false表示失败</para>
        ///  <para>en-us:Returns change result, true indicates success, false indicates failure</para>
        /// </returns>
        [HttpPost("password/change")]
        public async Task<bool> ChangePasswordAsync(AccountDto dto)
        {
            if (dto == null || dto.Content.IsNullOrEmpty())
                throw Oops.Oh("请求数据不能为空");

            string AppId = _httpContextAccessor.HttpContext.Request.Headers["AppId"];

            if (AppId.IsNullOrEmpty()) throw Oops.Oh("客户端非法请求");

            var App = await appInfoDomain.GetAppInfoAsync(AppId);
            if (App == null) throw Oops.Oh("客户端应用不存在或已被禁用");

            string Ticket = _httpContextAccessor.HttpContext.Request.Headers["Ticket"];
            if (Ticket.IsNullOrEmpty()) throw Oops.Oh("客户端非法请求");

            string PayLoadContent = App.Decrypt(dto.Content);
            if (string.IsNullOrEmpty(PayLoadContent)) throw Oops.Oh("请求数据解析失败");
            
            ChangePasswordDto payloadDto = AppRealization.JSON.Deserialize<ChangePasswordDto>(PayLoadContent);
            bool CodeValid = CaptchaCodeUtil.ValidateCaptchaCode(Ticket, payloadDto.Code);
            if (!CodeValid) throw Oops.Oh("验证码无效或已过期，请重新获取验证码后再试");

            if (payloadDto.OldPassword.Contains(payloadDto.NewPassword) || payloadDto.NewPassword.Contains(payloadDto.OldPassword))
            {
                throw Oops.Oh("新密码与旧密码相似度过高，请更换新密码后再试");
            }
            var changeStatus=await userDomain.ChangePasswordAsync(payloadDto.UserId,payloadDto.OldPassword, payloadDto.NewPassword);
            await userAccountLogDomain.CreateUserAccountLogAsync(new UserAccountLogSDto()
            {
                Id = AppCore.Guid(),
                UserId = payloadDto.UserId,
                TypeCode = UserAccountLogTypeEnum.密码修改.ToString(),
                Remark = $"执行密码修改,修改时间:{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")};执行结果:{(changeStatus ? "成功" : "失败")}",
                Meta = AppRealization.JSON.Serialize(new { pwdEncrypted = App.Encrypt(payloadDto.NewPassword) })
            });
            return changeStatus;
        }

        /// <summary>
        /// <para>zh-cn:重置密码</para>
        /// <para>en-us:Reset password</para>
        /// </summary>
        /// <param name="dto">
        ///  <para>zh-cn:账户数据传输对象（包含加密负载）</para>
        ///  <para>en-us:Account DTO (contains encrypted payload)</para>
        /// </param>
        /// <returns>
        ///  <para>zh-cn:返回重置结果，true表示成功，false表示失败</para>
        ///  <para>en-us:Returns reset result, true indicates success, false indicates failure</para>
        /// </returns>
        [HttpPost("password/reset")]
        public async Task<string> ResetPasswordAsync(AccountDto dto)
        {
            if (dto == null || dto.Content.IsNullOrEmpty())
                throw Oops.Oh("请求数据不能为空");
            
            string AppId = _httpContextAccessor.HttpContext.Request.Headers["AppId"];
            if (AppId.IsNullOrEmpty()) throw Oops.Oh("客户端非法请求");
            var App = await appInfoDomain.GetAppInfoAsync(AppId);
            if (App == null) throw Oops.Oh("客户端应用不存在或已被禁用");

            string Ticket = _httpContextAccessor.HttpContext.Request.Headers["Ticket"];
            if (Ticket.IsNullOrEmpty()) throw Oops.Oh("客户端非法请求");

           
            string PayLoadContent = App.Decrypt(dto.Content);
            if (string.IsNullOrEmpty(PayLoadContent)) throw Oops.Oh("请求数据解析失败");

            ResetPasswordDto resetPasswordDto = AppRealization.JSON.Deserialize<ResetPasswordDto>(PayLoadContent);

            bool CodeValid =CaptchaCodeUtil.ValidateCaptchaCode(Ticket,resetPasswordDto.Code);
            if (!CodeValid) throw Oops.Oh("验证码无效或已过期，请重新获取验证码后再试");

            string NewPassword = IUserDomain.GeneratePassword();
            var result=await userDomain.ResetPasswordAsync(resetPasswordDto.UserId, NewPassword);
            await userAccountLogDomain.CreateUserAccountLogAsync(new UserAccountLogSDto()
            {
                Id = AppCore.Guid(),
                UserId = resetPasswordDto.UserId,
                TypeCode = UserAccountLogTypeEnum.密码重置.ToString(),
                Remark = $"执行密码重置,重置时间:{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")};执行结果:{(result ? "成功" : "失败")}",
                Meta=AppRealization.JSON.Serialize(new { pwdEncrypted=App.Encrypt(NewPassword)})
            });
            return result? NewPassword:string.Empty;
        }
    }
}