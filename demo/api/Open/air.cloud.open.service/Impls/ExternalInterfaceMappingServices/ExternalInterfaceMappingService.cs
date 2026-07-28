using air.cloud.open.model.Domains;
using air.cloud.open.model.Dtos.ExternalInterfaceMappingDtos;
using air.cloud.open.service.Services.ExternalInterfaceMappingServices;
using air.cloud.security.common.Base.Dtos;
using air.cloud.security.common.Model;

using Air.Cloud.Core.Extensions;

using Microsoft.AspNetCore.Mvc;

namespace air.cloud.open.service.Impls.ExternalInterfaceMappingServices
{
    [Route("/v1/open/mapping/ext_interface")]
    public class ExternalInterfaceMappingService : IExternalInterfaceMappingService
    {
        private readonly IExternalInterfaceMappingDomain externalInterfaceMappingDomain;
        public ExternalInterfaceMappingService(IExternalInterfaceMappingDomain interfaceMappingDomain) { 
            this.externalInterfaceMappingDomain = interfaceMappingDomain;
        }
        [HttpDelete("remove/{Id}")]
        public async Task<bool> DeleteExternalInterfaceAsync(string Id)
        {
            return await externalInterfaceMappingDomain.DeleteExternalInterfaceAsync(Id);
        }
        [HttpGet("detail/{Id}")]
        public async Task<ExternalInterfaceMappingSDto> GetExternalInterfaceAsync(string Id)
        {
           return await externalInterfaceMappingDomain.GetExternalInterfaceAsync(Id);
        }

        [HttpPost("query")]
        public async  Task<PageList<ExternalInterfaceMappingSDto>> GetExternalInterfacePageListAsync(ExternalInterfaceMappingQDto dto)
        {
           return await externalInterfaceMappingDomain.GetExternalInterfacePageListAsync(dto);
        }
        [HttpPost("save")]
        public async Task<string> SaveExternalInterfaceAsync(ExternalInterfaceMappingSDto dto)
        {
            if (dto.Id.IsNullOrEmpty())
            {
                return await externalInterfaceMappingDomain.CreateExternalInterfaceAsync(dto);
            }
            return await externalInterfaceMappingDomain.UpdateExternalInterfaceAsync(dto);
        }
        [HttpPost("select")]
        public async Task<IList<ExternalInterfaceMappingSDto>> GetExternalInterfaceSelectAsync(ExternalInterfaceMappingQDto baseQDto)
        {
            return await externalInterfaceMappingDomain.GetExternalInterfaceSelectAsync(baseQDto);
        }
    }
}
