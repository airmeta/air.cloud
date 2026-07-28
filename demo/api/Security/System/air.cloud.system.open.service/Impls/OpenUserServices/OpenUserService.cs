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
using air.cloud.security.common.Enums;
using air.cloud.system.model.Domains.AppInfoDomains;
using air.cloud.system.model.Domains.UserDomains;
using air.cloud.system.model.Dtos.UserDtos;
using air.cloud.system.open.service.Consts;
using air.cloud.system.open.service.Dtos.OpenUserDtos.Check;
using air.cloud.system.open.service.Dtos.OpenUserDtos.Create;
using air.cloud.system.open.service.Dtos.OpenUserDtos.Delete;
using air.cloud.system.open.service.Dtos.OpenUserDtos.Update;
using air.cloud.system.open.service.Services.IOpenUserServices;

using Air.Cloud.Core.Extensions;
using Air.Cloud.WebApp.FriendlyException;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace air.cloud.system.open.service.Impls.OpenUserServices
{
    /// <summary>
    /// <para>zh-cn:开放用户服务接口</para>
    /// <para>en-us:Open User Service Interface</para>
    /// </summary>
    [Route("v1/open/user")]
    public class OpenUserService : IOpenUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private  readonly IUserDomain userDomain;
        private readonly IAppInfoDomain appInfoDomain;

        public OpenUserService(IHttpContextAccessor httpContextAccessor,IUserDomain userDomain, IAppInfoDomain appInfoDomain)
        {
            _httpContextAccessor = httpContextAccessor;
            this.userDomain = userDomain;
            this.appInfoDomain = appInfoDomain; 
        }

        [HttpGet("check/{AppUserId}")]
        public async Task<OpenUserCheckResult> OpenUserCheckAsync(string AppUserId)
        {
            string AppId = _httpContextAccessor.HttpContext.Request.Headers["AppId"];
            if (AppId.IsNullOrEmpty()) throw Oops.Oh("客户端非法请求");
            var HasApp=await appInfoDomain.HasAppAsync(AppId);
            if (!HasApp) throw Oops.Oh("无效的应用请求");
            var Org = await userDomain.GetUserAsync(AppUserId);
            string Code = Org != null ? Org.IsDelete ==IsOrNotEnum.是 ? CheckResultCode.数据已删除 : CheckResultCode.正常 : CheckResultCode.数据不存在;
            return new OpenUserCheckResult()
            {
                IsExits = Org != null,
                Code = Code
            };
        }
        [HttpPost("create")]
        public async Task<OpenUserCreateResult> OpenUserCreateAsync(OpenUserCreateDto dto)
        {
            string AppId = _httpContextAccessor.HttpContext.Request.Headers["AppId"];
            if (AppId.IsNullOrEmpty()) throw Oops.Oh("客户端非法请求");
            var HasApp = await appInfoDomain.HasAppAsync(AppId);
            if (!HasApp) throw Oops.Oh("无效的应用请求");
          
            if (dto.Account.IsNullOrEmpty())
            {
                throw Oops.Oh("用户账号不能为空");
            }
            if (dto.Password.IsNullOrEmpty()) {
                throw Oops.Oh("用户密码不能为空");
            }
            if (dto.PhoneNumber.IsNullOrEmpty()) {
                throw Oops.Oh("用户手机号不能为空");
            }
            if(dto.AppUserId.IsNullOrEmpty())
            {
                throw Oops.Oh("应用用户ID不能为空");
            }
            if(dto.UserName.IsNullOrEmpty())
            {
                throw Oops.Oh("用户名不能为空");
            }
            var user = new UserSDto()
            {
                AppUserId = dto.AppUserId,
                UserName = dto.UserName,
                Account = dto.Account,
                Password = dto.Password,
                IdCardNo = dto.IdCardNo,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                RoleIds = dto.RoleIds,
                DepartmentIds = dto.DepartmentIds
            };
            var UserId=await  userDomain.CreateUserAsync(user,AppId);

            return new OpenUserCreateResult()
            {
                IsSuccess = !UserId.IsNullOrEmpty(),
                Id = UserId
            };
        }
        [HttpPost("remove/{AppUserId}")]
        public async Task<OpenUserDeleteResult> OpenUserDeleteAsync(string AppUserId)
        {
            string AppId = _httpContextAccessor.HttpContext.Request.Headers["AppId"];
            if (AppId.IsNullOrEmpty()) throw Oops.Oh("客户端非法请求");
            var HasApp = await appInfoDomain.HasAppAsync(AppId);
            if (!HasApp) throw Oops.Oh("无效的应用请求");
            bool isDelete= await userDomain.DeleteUserAsync(AppUserId);

            return new OpenUserDeleteResult()
            {
                IsDelete = isDelete,
                AppUserId = AppUserId
            };  


        }
        [HttpPost("update")]
        public async Task<OpenUserUpdateResult> OpenUserUpdateAsync(OpenUserUpdateDto dto)
        {
            string AppId = _httpContextAccessor.HttpContext.Request.Headers["AppId"];
            if (AppId.IsNullOrEmpty()) throw Oops.Oh("客户端非法请求");
            var HasApp = await appInfoDomain.HasAppAsync(AppId);
            if (!HasApp) throw Oops.Oh("无效的应用请求");
            var user = new UserSDto()
            {
                AppUserId = dto.AppUserId,
                UserName = dto.UserName,
                Account = dto.Account,
                Password = dto.Password,
                IdCardNo = dto.IdCardNo,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                RoleIds = dto.RoleIds,
                DepartmentIds = dto.DepartmentIds,
                Id=dto.AppUserId
            };
            string UserId= await userDomain.UpdateUserAsync(user);
            return new OpenUserUpdateResult()
            {
                IsSuccess = !UserId.IsNullOrEmpty(),
                Id = UserId
            };
        }
    }
}
