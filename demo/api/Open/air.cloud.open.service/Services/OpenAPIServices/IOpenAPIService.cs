using air.cloud.open.service.Dtos.OpenAPIDtos;
using air.cloud.security.common.Base.Dtos;

namespace air.cloud.open.service.Services.OpenAPIServices
{
    public interface IOpenAPIService
    {
        /// <summary>
        /// <para>zh-cn:执行开放接口</para>
        /// <para>en-us:Execute Open API</para>
        /// </summary>
        /// <param name="ActionId">
        /// <para>zh-cn:动作标识</para>
        /// <para>en-us:Action Id</para>
        /// </param>
        /// <returns>
        ///  <para>zh-cn:执行结果</para>
        ///  <para>en-us:Execution result</para>
        /// </returns>
        public Task<ExecuteOpenAPIResponse> ExecuteOpenAPIAsync(ExecuteOpenAPIRequest request);

        #region  接口文档中心
        ///// <summary>
        ///// <para>zh-cn:保存开放接口分组</para>
        ///// <para>en-us:Save Open API Group</para>
        ///// </summary>
        ///// <param name="groupSDto">
        /////   <para>zh-cn:开放接口分组数据传输对象</para>
        /////   <para>en-us:Open API Group Data Transfer Object</para>
        ///// </param>
        ///// <returns></returns>
        //public Task<bool> SaveOpenAPIGroupAsync(OpenAPIGroupSDto groupSDto);

        ///// <summary>
        ///// <para>zh-cn:查询开放接口分组</para>
        ///// <para>en-us:Query Open API Group</para>
        ///// </summary>
        ///// <param name="dto">
        ///// <para>zh-cn:查询条件数据传输对象</para>
        ///// <para>en-us:Query Condition Data Transfer Object</para>
        ///// </param>
        ///// <returns>
        /////   <para>zh-cn:开放接口分组列表</para>
        /////   <para>en-us:Open API Group List</para>
        ///// </returns>
        //public Task<IList<OpenAPIGroupRDto>> QueryOpenAPIGroupAsync(BaseQDto dto);

        ///// <summary>
        ///// <para>zh-cn:查询开放接口分组信息</para>
        ///// <para>en-us:Query Open API Group Info</para>
        ///// </summary>
        ///// <param name="OpenAPIGroupId">
        ///// <para>zh-cn:开放接口分组标识</para>
        ///// <para>en-us:Open API Group Id</para>
        ///// </param>
        ///// <returns>
        ///// <para>zh-cn:开放接口分组信息</para>
        ///// <para>en-us:Open API Group Info</para>
        ///// </returns>
        //public Task<OpenAPIGroupRDto> QueryOpenAPIGroupInfoAsync(string OpenAPIGroupId);

        ///// <summary>
        ///// <para>zh-cn:删除开放接口分组信息</para>
        ///// <para>en-us:Remove Open API Group Info</para>
        ///// </summary>
        ///// <param name="OpenAPIGroupId">
        ///// <para>zh-cn:开放接口分组标识</para>
        ///// <para>en-us:Open API Group Id</para>
        ///// </param>
        //public Task RemoveOpenAPIGroupInfoAsync(string OpenAPIGroupId);

        #endregion
    }
}
