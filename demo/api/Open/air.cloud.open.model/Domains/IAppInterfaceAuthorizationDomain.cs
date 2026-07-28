using air.cloud.open.model.Dtos.AppInterfaceAuthorizationDtos;
using air.cloud.security.common.Model;

using Air.Cloud.Core.Standard.DataBase.Domains;
using Air.Cloud.Core.Standard.DynamicServer;

namespace air.cloud.open.model.Domains
{
    /// <summary>
    /// <para>zh-cn:应用接口授权领域</para>
    /// <para>en-us:Application Interface Authorization Domain</para>
    /// </summary>
    public interface IAppInterfaceAuthorizationDomain:IEntityDomain,ITransient
    {

        /// <summary>
        /// <para>zh-cn:创建应用接口授权信息</para>
        /// <para>en-us:Create Application Interface Authorization Information</para>
        /// </summary>
        /// <param name="AppId"></param>
        /// <returns></returns>
        public Task<string > CreateAppInterfaceAuthorization(AppInterfaceAuthorizationSDto dto);

        /// <summary>
        /// <para>zh-cn:删除应用接口授权信息</para>
        /// <para>en-us:Delete Application Interface Authorization Information</para>
        /// </summary>
        /// <param name="AppId"></param>
        /// <param name="ActionId"></param>
        /// <returns></returns>
        public Task<bool> DeleteAppInterfaceAuthorization(string AppId,string ActionId,string Remark);

        /// <summary>
        /// <para>zh-cn:更新应用接口授权信息</para>
        /// <para>en-us:Update Application Interface Authorization Information</para>   
        /// </summary>
        /// <returns></returns>
        public Task<string> UpdateAppInterfaceAuthorization(AppInterfaceAuthorizationSDto dto);

        /// <summary>
        /// <para>zh-cn:检查应用是否具有接口权限</para>
        /// <para>en-us:Check if Application has Interface Permission</para>
        /// </summary>
        /// <param name="AppId"></param>
        /// <param name="ActionId"></param>
        /// <returns></returns>
        public Task<bool> CheckAppInterfaceHasAuthorization(string AppId, string ActionId, string ActionSecret);

        /// <summary>
        /// <para>zh-cn:查询授权详情</para>
        /// <para>en-us:Query Authorization Details</para>
        /// </summary>
        /// <param name="AppId"></param>
        /// <param name="ActionId"></param>
        /// <returns></returns>
        public Task<AppInterfaceAuthorizationSDto?> GetAppInterfaceAuthorizationAsync(string AppId, string ActionId);

        /// <summary>
        /// <para>zh-cn:查询应用接口授权信息</para>
        /// <para>en-us:Query Application Interface Authorization Information</para>
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        public Task<PageList<AppInterfaceAuthorizationSDto>> QueryAppInterfaceAuthorization(AppInterfaceAuthorizationQDto dto);


    }
}
