using air.cloud.open.model.Domains;
using air.cloud.open.model.Dtos.AppInterfaceAuthorizationDtos;
using air.cloud.open.service.Dtos.AppInterfaceAuthorizationDtos;
using air.cloud.open.service.Services.AppInterfaceAuthorizationServices;
using air.cloud.security.common.Base.Dtos;
using air.cloud.security.common.Model;

using Air.Cloud.Core.Extensions;

using Microsoft.AspNetCore.Mvc;

namespace air.cloud.open.service.Impls.AppInterfaceAuthorizationServices
{

    /// <summary>
    /// <para>zh-cn:应用接口授权服务</para>
    /// <para>en-us:Application Interface Authorization Service</para>
    /// </summary>
    [Route("/v1/open/config/app_auth")]
    public class AppInterfaceAuthorizationService : IAppInterfaceAuthorizationService
    {
        private readonly IAppInterfaceAuthorizationDomain appInterfaceAuthorizationDomain;
        public AppInterfaceAuthorizationService(IAppInterfaceAuthorizationDomain appInterfaceAuthorizationDomain) { 
            this.appInterfaceAuthorizationDomain = appInterfaceAuthorizationDomain;
        }

        [HttpPost("check")]
        public async Task<bool> CheckAppInterfaceHasAuthorization(AppInterfaceAuthorizationCheckDto dto)
        {
           return await this.appInterfaceAuthorizationDomain.CheckAppInterfaceHasAuthorization(dto.AppId, dto.ActionId, dto.ActionSecret);
        }
        [HttpPost("remove")]
        public async Task<bool> DeleteAppInterfaceAuthorization(AppInterfaceAuthorizationRemoveDto dto)
        {
            return await this.appInterfaceAuthorizationDomain.DeleteAppInterfaceAuthorization(dto.AppId, dto.ActionId, dto.Remark);                                                            
        }
        [HttpGet("detail/{AppId}/{ActionId}")]
        public async Task<AppInterfaceAuthorizationSDto?> GetAppInterfaceAuthorization(string AppId, string ActionId)
        {
            return await this.appInterfaceAuthorizationDomain.GetAppInterfaceAuthorizationAsync(AppId, ActionId);
        }
        [HttpPost("query")]
        public async Task<PageList<AppInterfaceAuthorizationSDto>> QueryAppInterfaceAuthorization(AppInterfaceAuthorizationQDto dto)
        {
           return await this.appInterfaceAuthorizationDomain.QueryAppInterfaceAuthorization(dto);
        }
        [HttpPost("save")]
        public async Task<string> SaveAppInterfaceAuthorization(AppInterfaceAuthorizationSDto dto)
        {
            if (dto.Id.IsNullOrEmpty())
            {
                return await appInterfaceAuthorizationDomain.CreateAppInterfaceAuthorization(dto);
            }
            return await appInterfaceAuthorizationDomain.UpdateAppInterfaceAuthorization(dto);
        }
    }
}
