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
using air.cloud.system.model.Domains.AppInfoDomains;
using air.cloud.system.model.Domains.OrganizationDomains;
using air.cloud.system.open.service.Consts;
using air.cloud.system.open.service.Dtos.OpenAsgDtos.Check;
using air.cloud.system.open.service.Dtos.OpenAsgDtos.Create;
using air.cloud.system.open.service.Dtos.OpenAsgDtos.Delete;
using air.cloud.system.open.service.Dtos.OpenAsgDtos.Update;
using air.cloud.system.open.service.Services.IOpenAsgServices;

using Air.Cloud.Core.Extensions;
using Air.Cloud.WebApp.FriendlyException;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace air.cloud.system.open.service.Impls.OpenAsgServices
{
    /// <summary>
    /// <para>zh-cn:开放职位服务接口</para>
    /// <para>en-us:Open Assignment Service Interface</para>
    /// </summary>
    [Route("v1/open/asg")]
    public class OpenAsgService:IOpenAsgService
    {
        private readonly IAssignmentDomain assignmentDomain;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAppInfoDomain appInfoDomain;
        public OpenAsgService(IAssignmentDomain assignmentDomain,IHttpContextAccessor _httpContextAccessor,
            IAppInfoDomain appInfoDomain) { 
            this.assignmentDomain = assignmentDomain;
            this._httpContextAccessor = _httpContextAccessor;
            this.appInfoDomain = appInfoDomain;
        }

        /// <summary>
        /// <para>zh-cn:创建开放职位</para>
        /// <para>en-us:Create Open Assignment</para>
        /// </summary>
        /// <param name="dto">
        ///  <para>zh-cn:创建开放职位传输对象</para>
        ///  <para>en-us:Create Open Assignment Data Transfer Object</para>
        /// </param>
        /// <returns>
        ///  <para>zh-cn:创建开放职位结果对象</para>
        ///  <para>en-us:Create Open Assignment Result Object</para>
        /// </returns>
        [HttpPost("create")]
        public async Task<OpenAsgCreateResult> OpenAsgCreateAsync(OpenAsgCreateDto dto)
        {
            string AppId = _httpContextAccessor.HttpContext.Request.Headers["AppId"];
            if (AppId.IsNullOrEmpty()) throw Oops.Oh("客户端非法请求");
            var HasApp = await appInfoDomain.HasAppAsync(AppId);
            if (!HasApp) throw Oops.Oh("无效的应用请求");
            string Id = await assignmentDomain.CreateAssignmentAsync(new model.Dtos.OrganizationDtos.AssignmentDtos.AssignmentSDto()
            {
                DepartmentId = dto.DepartmentId,
                Description = dto.Description,
                Name = dto.Name
            }, AppId);
            return new OpenAsgCreateResult()
            {
                Id = Id,
                IsSuccess = !string.IsNullOrEmpty(Id)   
            };
        }

        /// <summary>
        /// <para>zh-cn:更新开放职位</para>
        /// <para>en-us:Update Open Assignment</para>
        /// </summary>
        /// <param name="dto">
        ///  <para>zh-cn:更新开放职位传输对象</para>
        ///  <para>en-us:Update Open Assignment Data Transfer Object</para>
        /// </param>
        /// <returns>
        ///  <para>zh-cn:更新开放职位结果对象</para>
        ///  <para>en-us:Update Open Assignment Result Object</para>
        /// </returns>
        [HttpPost("update")]
        public async Task<OpenAsgUpdateResult> OpenAsgUpdateAsync(OpenAsgUpdateDto dto)
        {
            string AppId = _httpContextAccessor.HttpContext.Request.Headers["AppId"];
            if (AppId.IsNullOrEmpty()) throw Oops.Oh("客户端非法请求");
            var HasApp = await appInfoDomain.HasAppAsync(AppId);
            if (!HasApp) throw Oops.Oh("无效的应用请求");
            string Id = await assignmentDomain.UpdateAssignmentAsync(new model.Dtos.OrganizationDtos.AssignmentDtos.AssignmentSDto()
            {
                DepartmentId = dto.DepartmentId,
                Description = dto.Description,
                Name = dto.Name,
                Id= dto.Id
            },AppId);
            return new OpenAsgUpdateResult()
            {
                Id = Id,
                IsSuccess = !string.IsNullOrEmpty(Id)
            };
        }

        /// <summary>
        /// <para>zh-cn:检查开放职位是否存在</para>
        /// <para>en-us:Check if Open Assignment Exists</para>
        /// </summary>
        /// <param name="AsgId">
        ///  <para>zh-cn:开放职位ID</para>
        ///  <para>en-us:Open Assignment ID</para>
        /// </param>
        /// <returns>
        /// <para>zh-cn:检查开放职位结果对象</para>
        /// <para>en-us:Check Open Assignment Result Object</para>
        /// </returns>
        [HttpGet("check/{AsgId}")]
        public async Task<OpenAsgCheckResult> OpenAsgCheckAsync(string AsgId)
        {
            string AppId = _httpContextAccessor.HttpContext.Request.Headers["AppId"];
            if (AppId.IsNullOrEmpty()) throw Oops.Oh("客户端非法请求");
            var HasApp = await appInfoDomain.HasAppAsync(AppId);
            if (!HasApp) throw Oops.Oh("无效的应用请求");
            var Asg = await assignmentDomain.GetAssignmentDetailAsync(AsgId, AppId);
            string Code = Asg != null ? CheckResultCode.正常 : CheckResultCode.数据不存在;
            return new OpenAsgCheckResult()
            {
                IsExits = Asg != null,
                Code = Code
            };
        }

        /// <summary>
        /// <para>zh-cn:删除开放职位</para>
        /// <para>en-us:Delete Open Assignment</para>
        /// </summary>
        /// <param name="AsgId">
        ///  <para>zh-cn:开放职位ID</para>
        ///  <para>en-us:Open Assignment ID</para>
        /// </param>
        /// <returns>
        ///  <para>zh-cn:删除开放职位结果对象</para>
        ///  <para>en-us:Delete Open Assignment Result Object</para>
        /// </returns>
        [HttpGet("remove/{AsgId}")]
        public async Task<OpenAsgDeleteResult> OpenAsgDeleteAsync(string AsgId)
        {
            string AppId = _httpContextAccessor.HttpContext.Request.Headers["AppId"];
            if (AppId.IsNullOrEmpty()) throw Oops.Oh("客户端非法请求");
            var HasApp = await appInfoDomain.HasAppAsync(AppId);
            if (!HasApp) throw Oops.Oh("无效的应用请求");
            return await assignmentDomain.DeleteAssignmentAsync(AsgId, AppId)
                .ContinueWith(t => new OpenAsgDeleteResult()
                {
                    IsDelete = t.Result,
                    Id=AsgId
                });
        }

    }
}
