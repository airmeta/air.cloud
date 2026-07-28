using air.cloud.open.model.Domains;
using air.cloud.open.model.Dtos.InternalInterfaceMappingDtos;
using air.cloud.open.model.Taxin.AppRouteDtos;
using air.cloud.open.service.Services.InternalInterfaceMappingServices;
using air.cloud.security.common.Base.Dtos;
using air.cloud.security.common.Model;

using Air.Cloud.Core;
using Air.Cloud.Core.Extensions;
using Air.Cloud.Core.Plugins.InternalAccess;
using Air.Cloud.Core.Standard.Taxin.Client;
using Air.Cloud.WebApp.FriendlyException;
using Air.Cloud.WebApp.UnifyResult.Internal;

using Mapster;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace air.cloud.open.service.Impls.InternalInterfaceMappingServices
{
    [Route("/v1/open/mapping/inter_interface")]
    public class InternalInterfaceMappingService : IInternalInterfaceMappingService
    {
        private readonly IInternalInterfaceMappingDomain interfaceMappingDomain;

        private readonly ITaxinClientStandard taxinClientStandard;
        public InternalInterfaceMappingService(
            IInternalInterfaceMappingDomain internalInterfaceMappingDomain,
            ITaxinClientStandard taxinClientStandard) {
            interfaceMappingDomain = internalInterfaceMappingDomain;
            this.taxinClientStandard=taxinClientStandard;
        }

        [HttpDelete("remove/{Id}")]
        public async Task<bool> DeleteInternalInterfaceAsync(string Id)
        {
            return await interfaceMappingDomain.DeleteInternalInterfaceAsync(Id);
        }
        [HttpGet("detail/{Id}")]
        public async Task<InternalInterfaceMappingSDto> GetInternalInterfaceAsync(string Id)
        {
            return await interfaceMappingDomain.GetInternalInterfaceAsync(Id);
        }
        [HttpPost("query")]
        public async Task<PageList<InternalInterfaceMappingRDto>> GetInternalInterfacePageListAsync(InternalInterfaceMappingQDto dto)
        {
            var data= await interfaceMappingDomain.GetInternalInterfacePageListAsync(dto);

            IInternalAccessValidPlugin internalAccessValidPlugin = AppRealization.AppPlugin.GetPlugin<IInternalAccessValidPlugin>();
            var result= internalAccessValidPlugin.CreateInternalAccessToken();
            AppRealization.Output.Print("内部接口映射-分页查询","内部访问令牌:"+result.Item1+":"+result.Item2,Air.Cloud.Core.Modules.AppPrint.AppPrintLevel.Information);
            var routes = await taxinClientStandard.SendAsync<RESTfulResult<IList<AppRouteQueryByIdsResultDto>>>(AppRouteQueryByIdsDto.ROUTE,
                new AppRouteQueryByIdsDto { Ids = string.Join(",", data.List.Select(s => s.RouteId).Distinct()) });

            if (routes == null)
            {
                AppRealization.Output.Print("远程检索App路由信息", "检索出现异常,检索结果为null,请检查远程服务端日志记录",Air.Cloud.Core.Modules.AppPrint.AppPrintLevel.Warn);
                //throw Oops.Oh("检索路由信息时出现异常");
            }
            if (routes!=null&& routes.Data.Any())
            {
                data.List = data.List.Select(s =>
                {
                    var route = routes.Data.FirstOrDefault(f => f.Id == s.RouteId);
                    if (route != null)
                    {
                        s.RouteAppId = route.AppId;
                        s.RouteAppName = route.AppName;
                        s.Route = route.Route;
                    }
                    else
                    {
                        s.RouteAppId = string.Empty;
                        s.RouteAppName = "该应用已被删除";
                        s.Route = string.Empty;
                    }
                    return s;
                }).ToList();
            }
            return data;
        }
        [HttpPost("save")]
        public async Task<string> SaveInternalInterfaceAsync(InternalInterfaceMappingSDto dto)
        {
            if (dto.Id.IsNullOrEmpty())
            {
                return await interfaceMappingDomain.CreateInternalInterfaceAsync(dto);
            }
           return await interfaceMappingDomain.UpdateInternalInterfaceAsync(dto);
        }
        [HttpPost("select")]
        public async Task<IList<InternalInterfaceMappingRDto>> GetInternalInterfaceSelectAsync(InternalInterfaceMappingQDto dto)
        {
            var data= await interfaceMappingDomain.GetInternalInterfaceSelectAsync(dto);

            IInternalAccessValidPlugin internalAccessValidPlugin = AppRealization.AppPlugin.GetPlugin<IInternalAccessValidPlugin>();

            var accessToken = internalAccessValidPlugin.CreateInternalAccessToken();

            AppRealization.Output.Print("内部接口映射-分页查询", "内部访问令牌:" + accessToken.Item1 + ":" + accessToken.Item2, Air.Cloud.Core.Modules.AppPrint.AppPrintLevel.Information);
            var routes = await taxinClientStandard.SendAsync<RESTfulResult<IList<AppRouteQueryByIdsResultDto>>>(AppRouteQueryByIdsDto.ROUTE,
                new AppRouteQueryByIdsDto { Ids = string.Join(",", data.Select(s => s.RouteId).Distinct()) });
            if (routes == null)
            {
                AppRealization.Output.Print("远程检索App路由信息", "检索出现异常,检索结果为null,请检查远程服务端日志记录", Air.Cloud.Core.Modules.AppPrint.AppPrintLevel.Warn);
                //throw Oops.Oh("检索路由信息时出现异常");
            }
            if (routes != null && routes.Data.Any())
            {
                var result = data.Select(s =>
                {
                    var route = routes.Data.FirstOrDefault(f => f.Id == s.RouteId);
                    var a = s.Adapt<InternalInterfaceMappingRDto>();
                    if (route != null)
                    {
                        a.RouteAppId = route.AppId;
                        a.RouteAppName = route.AppName;
                        a.Route = route.Route;
                    }
                    else
                    {
                        a.RouteAppId = string.Empty;
                        a.RouteAppName = "该应用已被删除";
                        a.Route = string.Empty;
                    }
                    return a;
                }).ToList();
                return result;
            }
            return new List<InternalInterfaceMappingRDto>();
        }

    }
}
