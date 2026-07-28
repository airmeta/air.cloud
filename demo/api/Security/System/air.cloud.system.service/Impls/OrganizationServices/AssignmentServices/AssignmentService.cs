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
using air.cloud.system.service.Services.OrganizationServices.AssignmentServices;
using air.cloud.security.common.Base.Dtos;
using air.cloud.security.common.Model;
using air.cloud.system.model.Domains.AppInfoDomains;
using air.cloud.system.model.Domains.OrganizationDomains;
using air.cloud.system.model.Dtos.OrganizationDtos.AssignmentDtos;

using Air.Cloud.Core.Extensions;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using System.ComponentModel;

namespace air.cloud.system.service.Impl.OrganizationServices.AssignmentServices
{
    /// <summary>
    /// <para>zh-cn:职位服务接口</para>
    /// <para>en-us:Assignment Service Interface</para>
    /// </summary>
    [Route("v1/security/system/assignment")]
    [Description("职位管理")]
    public class AssignmentService : IAssignmentService
    {
        private readonly IAssignmentDomain _assignmentDomain;

        public AssignmentService(IAssignmentDomain assignmentDomain,
            IAppInfoDomain appInfoDomain,
            IHttpContextAccessor httpContextAccessor)
        {
            _assignmentDomain = assignmentDomain;
        }

        /// <summary>
        /// <para>zh-cn:删除职位信息</para>
        /// <para>en-us:Delete assignment information</para>
        /// </summary>
        /// <param name="assignmentId">
        ///  <para>zh-cn:职位ID</para>
        ///  <para>en-us:Assignment Id</para>
        /// </param>
        /// <returns>
        ///  <para>zh-cn:返回删除结果，true表示成功，false表示失败</para>
        ///  <para>en-us:Returns the deletion result, true indicates success, false indicates failure</para>
        /// </returns>
        [HttpDelete("remove/{assignmentId}")]
        public async Task<bool> DeleteAssignmentAsync(string assignmentId)
        {
            return await _assignmentDomain.DeleteAssignmentAsync(assignmentId, string.Empty);
        }

        /// <summary>
        /// <para>zh-cn:查询职位详情</para>
        /// <para>en-us:Get assignment details</para>
        /// </summary>
        /// <param name="assignmentId">
        ///  <para>zh-cn:职位ID</para>
        ///  <para>en-us:Assignment Id</para>
        /// </param>
        /// <returns>
        ///  <para>zh-cn:返回职位详情信息</para>
        ///  <para>en-us:Returns assignment detailed information</para>
        /// </returns>
        [HttpGet("detail/{assignmentId}")]
        public async Task<AssignmentSDto> GetAssignmentAsync(string assignmentId)
        {
            return await _assignmentDomain.GetAssignmentDetailAsync(assignmentId);
        }

        /// <summary>
        /// <para>zh-cn:查询职位信息列表</para>
        /// <para>en-us:Query assignment information list</para>
        /// </summary>
        /// <param name="dto">
        ///  <para>zh-cn:基础查询参数</para>
        ///  <para>en-us:Basic query parameters</para>
        /// </param>
        /// <returns>
        ///  <para>zh-cn:返回职位列表（按查询条件筛选）</para>
        ///  <para>en-us:Returns a list of assignments filtered by query conditions</para>
        /// </returns>
        [HttpPost("query")]
        public async Task<PageList<AssignmentSDto>> QueryAssignmentsAsync(AssignmentQDto dto)
        {
            return await _assignmentDomain.QueryAssignmentAsync(dto);
        }

        /// <summary>
        /// <para>zh-cn:创建或更新职位信息</para>
        /// <para>en-us:Create or update assignment information</para>
        /// </summary>
        /// <param name="dto">
        ///  <para>zh-cn:职位信息传输对象</para>
        ///  <para>en-us:Assignment Information Transfer Object</para>
        /// </param>
        /// <returns>
        ///  <para>zh-cn:返回保存结果，true表示成功，false表示失败</para>
        ///  <para>en-us:Returns the save result, true indicates success, false indicates failure</para>
        /// </returns>
        [HttpPost("save")]
        public async Task<bool> SaveAssignmentAsync(AssignmentSDto dto)
        {
            string assignmentId;
            if (dto.Id.IsNullOrEmpty())
            {
                assignmentId = await _assignmentDomain.CreateAssignmentAsync(dto, string.Empty);
            }
            else
            {
                assignmentId = await _assignmentDomain.UpdateAssignmentAsync(dto, string.Empty);
            }

            return !assignmentId.IsNullOrEmpty();
        }
    }
}