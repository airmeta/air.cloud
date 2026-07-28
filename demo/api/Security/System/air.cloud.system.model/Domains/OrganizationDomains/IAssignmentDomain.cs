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
using air.cloud.system.model.Dtos.OrganizationDtos.AssignmentDtos;
using air.cloud.system.model.Entitys.Organization;
using air.cloud.security.common.Base.Dtos;
using air.cloud.security.common.Model;

using Air.Cloud.Core.Standard.DataBase.Domains;
using Air.Cloud.Core.Standard.DynamicServer;

namespace air.cloud.system.model.Domains.OrganizationDomains
{
    public interface IAssignmentDomain:IEntityDomain,ITransient
    {
        /// <summary>
        /// 创建部门信息
        /// </summary>
        /// <returns></returns>
        public Task<string> CreateAssignmentAsync(AssignmentSDto dto,string AppId);
        /// <summary>
        /// 删除部门信息
        /// </summary>
        /// <param name="AssignmentId"></param>
        /// <returns></returns>
        public Task<bool> DeleteAssignmentAsync(string AssignmentId, string AppId);
        /// <summary>
        /// 更新部门信息
        /// </summary>
        /// <param name="AssignmentId"></param>
        /// <param name="AssignmentName"></param>
        /// <param name="AssignmentCode"></param>
        /// <param name="parentAssignmentId"></param>
        /// <returns></returns>
        public Task<string> UpdateAssignmentAsync(AssignmentSDto dto, string AppId);

        /// <summary>
        /// <para>zh-cn:查询职位信息</para>
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        public Task<PageList<AssignmentSDto>> QueryAssignmentAsync(AssignmentQDto dto);
        /// <summary>
        /// <para>zh-cn:检查职位是否存在</para>
        /// <para>en-us:Check if Assignment Exists</para>
        /// </summary>
        /// <param name="asgId"></param>
        /// <returns></returns>
        Task<bool> CheckAssignmentExistsAsync(string asgId, string AppId);

        /// <summary>
        /// <para>zh-cn:获取职位信息</para>
        /// <para>en-us:Get Assignment Information</para>
        /// </summary>
        /// <param name="asgId"></param>
        /// <returns></returns>
        Task<AssignmentSDto> GetAssignmentDetailAsync(string asgId, string AppId=null);

        /// <summary>
        /// <para>zh-cn:获取用户的职位列表</para>
        /// <para>en-us:Get User Assignment List</para>
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<IList<AssignmentSDto>> GetUserAssignmentsAsync(string userId, string AppId = null);


        /// <summary>
        /// <para>zh-cn:根据职位ID列表获取职位信息列表</para>
        /// <para>en-us:Get Assignment Information List by Assignment ID List</para>
        /// </summary>
        /// <param name="assignmentIds"></param>
        /// <returns></returns>
        Task<IList<Assignment>> GetAssignmentsAsync(IList<string> assignmentIds);


    }
}
