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
using air.cloud.system.model.Domains.OrganizationDomains;
using air.cloud.system.open.service.Consts;
using air.cloud.system.open.service.Dtos.OpenOrgDtos.Check;
using air.cloud.system.open.service.Dtos.OpenOrgDtos.Create;
using air.cloud.system.open.service.Dtos.OpenOrgDtos.Delete;
using air.cloud.system.open.service.Dtos.OpenOrgDtos.Update;
using air.cloud.system.open.service.Services.IOpenOrgService;

using Air.Cloud.Core.Extensions;
using Air.Cloud.WebApp.FriendlyException;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace air.cloud.system.open.service.Impls.OpenOrgService
{
    /// <summary>
    /// <para>zh-cn:开放组织服务接口</para>
    /// <para>en-us:Open Organization Service Interface</para>
    /// </summary>
    [Route("v1/open/org")]
    public class OpenOrgService:IOpenOrgService
    {

        private readonly IDepartmentDomain departmentDomain;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAppInfoDomain appInfoDomain;
        public OpenOrgService(IDepartmentDomain departmentDomain,IHttpContextAccessor httpContextAccessor,IAppInfoDomain appInfoDomain) { 
            this.departmentDomain = departmentDomain;
            this._httpContextAccessor = httpContextAccessor;

            this.appInfoDomain = appInfoDomain;
        }

        /// <summary>
        /// <para>zh-cn:创建开放组织</para>
        /// <para>en-us:Create Open Organization</para>
        /// </summary>
        /// <param name="dto">
        ///  <para>zh-cn:开放组织创建数据传输对象</para>
        ///  <para>en-us:Open Organization Create Data Transfer Object</para>
        /// </param>
        /// <returns>
        ///  <para>zh-cn:开放组织创建结果</para>
        ///  <para>en-us:Open Organization Create Result</para>
        /// </returns>
        [HttpPost("create")]
        public async Task<OpenOrgCreateResult> OpenOrgCreateAsync(OpenOrgCreateDto dto)
        {
            string AppId = _httpContextAccessor.HttpContext.Request.Headers["AppId"];
            if (AppId.IsNullOrEmpty()) throw Oops.Oh("客户端非法请求");
            var HasApp = await appInfoDomain.HasAppAsync(AppId);
            if (!HasApp) throw Oops.Oh("无效的应用请求");
            string Id= await departmentDomain.CreateDepartmentAsync(new model.Dtos.OrganizationDtos.DepartmentDtos.DepartmentSDto
            {
                DepartmentName = dto.DepartmentName,
                Description = dto.Description,
                DepartmentCode = dto.DepartmentCode,
                ParentDepartmentId = dto.ParentDepartmentId,
                ManagedRegions = dto.ManagedRegions,
                AppId= AppId
            });
            return new OpenOrgCreateResult()
            {
                IsSuccess = true,
                Id = Id
            };
        }

        /// <summary>
        /// <para>zh-cn:更新开放组织</para>
        /// <para>en-us:Update Open Organization</para>
        /// </summary>
        /// <param name="dto">
        ///  <para>zh-cn:开放组织更新数据传输对象</para>
        ///  <para>en-us:Open Organization Update Data Transfer Object</para>
        /// </param>
        /// <returns>
        ///  <para>zh-cn:开放组织更新结果</para>
        ///  <para>en-us:Open Organization Update Result</para>
        /// </returns>
        [HttpPost("update")]
        public async Task<OpenOrgUpdateResult> OpenOrgUpdateAsync(OpenOrgUpdateDto dto)
        {
            string AppId = _httpContextAccessor.HttpContext.Request.Headers["AppId"];
            if (AppId.IsNullOrEmpty()) throw Oops.Oh("客户端非法请求");
            var HasApp = await appInfoDomain.HasAppAsync(AppId);
            if (!HasApp) throw Oops.Oh("无效的应用请求");
            string Id = await departmentDomain.UpdateDepartmentAsync(new model.Dtos.OrganizationDtos.DepartmentDtos.DepartmentSDto
            {
                DepartmentName = dto.DepartmentName,
                Description = dto.Description,
                DepartmentCode = dto.DepartmentCode,
                ParentDepartmentId = dto.ParentDepartmentId,
                ManagedRegions = dto.ManagedRegions,
                AppId = AppId,
                ManagedAppIds=AppId
            });
            return new OpenOrgUpdateResult()
            {
                IsSuccess = true,
                Id = Id
            };
        }

        /// <summary>
        /// <para>zh-cn:检查开放组织是否存在</para>
        /// <para>en-us:Check if Open Organization Exists</para>
        /// </summary>
        /// <param name="OrgId">
        ///   <para>zh-cn:应用用户ID</para>
        ///   <para>en-us:Application User ID</para>
        /// </param>
        /// <returns>
        ///  <para>zh-cn:开放组织检查结果</para>
        ///  <para>en-us:Open Organization Check Result</para>
        /// </returns>
        [HttpPost("check/{OrgId}")]
        public async Task<OpenOrgCheckResult> OpenOrgCheckAsync(string OrgId)
        {
            string AppId = _httpContextAccessor.HttpContext.Request.Headers["AppId"];
            if (AppId.IsNullOrEmpty()) throw Oops.Oh("客户端非法请求");
            var HasApp = await appInfoDomain.HasAppAsync(AppId);
            if (!HasApp) throw Oops.Oh("无效的应用请求");
            var Org = await departmentDomain.GetDepartmentAsync(OrgId, AppId);
            string Code = Org!=null ? Org.IsDelete==IsOrNotEnum.是?CheckResultCode.数据已删除:CheckResultCode.正常 : CheckResultCode.数据不存在;
            return new OpenOrgCheckResult()
            {
                IsExits = Org!=null,
                Code= Code
            };
        }

        /// <summary>
        /// <para>zh-cn:删除开放组织</para>
        /// <para>en-us:Delete Open Organization</para>
        /// </summary>
        /// <param name="OrgId">
        ///   <para>zh-cn:应用用户ID</para>
        ///   <para>en-us:Application User ID</para>
        /// </param>
        /// <returns>
        ///  <para>zh-cn:开放组织删除结果</para>
        ///  <para>en-us:Open Organization Delete Result</para>
        /// </returns>
        [HttpPost("remove/{OrgId}")]
        public async Task<OpenOrgDeleteResult> OpenOrgDeleteAsync(string OrgId)
        {
            string AppId = _httpContextAccessor.HttpContext.Request.Headers["AppId"];
            if (AppId.IsNullOrEmpty()) throw Oops.Oh("客户端非法请求");
            var HasApp = await appInfoDomain.HasAppAsync(AppId);
            if (!HasApp) throw Oops.Oh("无效的应用请求");
            var bools=await departmentDomain.DeleteDepartmentAsync(OrgId, AppId);
            return new OpenOrgDeleteResult()
            {
                IsDelete = bools,
                Id= OrgId
            };
        }


    }
}
