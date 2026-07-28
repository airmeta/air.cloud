using air.cloud.open.model.Dtos.AppInterfaceAuthorizationDtos;
using air.cloud.open.service.Dtos.AppInterfaceAuthorizationDtos;
using air.cloud.security.common.Base.Dtos;
using air.cloud.security.common.Model;

using Air.Cloud.Core.Standard.DynamicServer;
namespace air.cloud.open.service.Services.AppInterfaceAuthorizationServices
{
    /// <summary>
    /// <para>zh-cn:应用接口授权服务</para>
    /// <para>en-us:Application Interface Authorization Service</para>
    /// </summary>
    public interface IAppInterfaceAuthorizationService : IDynamicService, ITransient
    {
        /// <summary>
        /// <para>zh-cn:创建应用接口授权信息</para>
        /// <para>en-us:Create Application Interface Authorization Information</para>
        /// </summary>
        /// <param name="AppId"></param>
        /// <returns></returns>
        public Task<string> SaveAppInterfaceAuthorization(AppInterfaceAuthorizationSDto dto);

        /// <summary>
        /// <para>zh-cn:删除应用接口授权信息</para>
        /// <para>en-us:Delete Application Interface Authorization Information</para>
        /// </summary>
        /// <param name="AppId"></param>
        /// <param name="ActionId"></param>
        /// <returns></returns>
        public Task<bool> DeleteAppInterfaceAuthorization(AppInterfaceAuthorizationRemoveDto dto);

        /// <summary>
        /// <para>zh-cn:检查应用是否具有接口权限</para>
        /// <para>en-us:Check if Application has Interface Permission</para>
        /// </summary>
        /// <param name="AppId"></param>
        /// <param name="ActionId"></param>
        /// <returns></returns>
        public Task<bool> CheckAppInterfaceHasAuthorization(AppInterfaceAuthorizationCheckDto dto);

        /// <summary>
        /// <para>zh-cn:查询授权详情</para>
        /// <para>en-us:Query Authorization Details</para>
        /// </summary>
        /// <param name="AppId"></param>
        /// <param name="ActionId"></param>
        /// <returns></returns>
        public Task<AppInterfaceAuthorizationSDto?> GetAppInterfaceAuthorization(string AppId, string ActionId);

        /// <summary>
        /// <para>zh-cn:查询应用接口授权信息</para>
        /// <para>en-us:Query Application Interface Authorization Information</para>
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        public Task<PageList<AppInterfaceAuthorizationSDto>> QueryAppInterfaceAuthorization(AppInterfaceAuthorizationQDto dto);


    }
}
