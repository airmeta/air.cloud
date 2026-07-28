using air.cloud.open.model.Domains;
using air.cloud.open.model.Dtos.InternalInterfaceMappingDtos;
using air.cloud.open.model.Entitys;
using air.cloud.open.model.Models;
using air.cloud.open.model.Taxin.AppRouteDtos;
using air.cloud.security.common.Auths;
using air.cloud.security.common.Base.Dtos;
using air.cloud.security.common.Enums;
using air.cloud.security.common.Extensions;
using air.cloud.security.common.Model;

using Air.Cloud.Core;
using Air.Cloud.Core.App;
using Air.Cloud.Core.Extensions;
using Air.Cloud.Core.Extensions.Linqs;
using Air.Cloud.Core.Standard.Taxin.Client;
using Air.Cloud.EntityFrameWork.Core.Repositories;
using Air.Cloud.WebApp.FriendlyException;

using Microsoft.EntityFrameworkCore;

using System.Transactions;

namespace air.cloud.open.domain.Impls
{
    public class InternalInterfaceMappingDomain : IInternalInterfaceMappingDomain
    {
        private readonly IRepository<InternalInterfaceMapping> repository;
        private readonly IUserAccountStore userAccountStore;

        public InternalInterfaceMappingDomain(IRepository<InternalInterfaceMapping> repository,
            IUserAccountStore userAccountStore)
        {
            this.repository = repository;
            this.userAccountStore = userAccountStore;
        }

        public async Task<string> CreateInternalInterfaceAsync(InternalInterfaceMappingSDto dto)
        {
            InternalInterfaceMapping entity = new InternalInterfaceMapping
            {
                Id = AppCore.Guid(),
                Name = dto.Name,
                RouteId = dto.RouteId,
                Description = dto.Description,
                RequestParameters = AppRealization.JSON.Serialize(dto.RequestParameters),
                ResponseParameters = AppRealization.JSON.Serialize(dto.ResponseParameters)
            };
            await repository.InsertAsync(entity);
            return entity.RouteId;
        }

        public async Task<bool> DeleteInternalInterfaceAsync(string Id)
        {
            UserAccountFactory userAccountFactory = await userAccountStore.GetUserAccountAsync();
            if (userAccountFactory == null)
            {
                AppRealization.Output.Print("删除内部接口映射", "无法获取当前登录用户信息,已返回true");
                return true;
            }
            var internalInterface = repository.FirstOrDefault(x => x.Id == Id);
            if (internalInterface == null)
            {
                AppRealization.Output.Print("删除内部接口映射", "无法获取当前内部接口映射,已返回true");
                return true;
            }
            internalInterface.IsDelete = IsOrNotEnum.是;
            internalInterface.DeleteUserId = userAccountFactory.Id;
            internalInterface.DeleteUserName = userAccountFactory.UserName;
            internalInterface.DeleteTime = DateTime.Now;
            await repository.UpdateIncludeAsync(internalInterface, new string[]
            {
                nameof(internalInterface.IsDelete),
                nameof(internalInterface.DeleteUserId),
                nameof(internalInterface.DeleteUserName),
                nameof(internalInterface.DeleteTime)
            });
            return true;
        }

        public async Task<InternalInterfaceMappingSDto> GetInternalInterfaceAsync(string Id)
        {
            if (Id.IsNullOrEmpty()) return null;
            var internalInterface = await repository.FirstOrDefaultAsync(y => y.Id == Id&&y.IsDelete==IsOrNotEnum.否);
            if (internalInterface == null) return null;
            InternalInterfaceMappingSDto internalInterfaceMappingSDto = new InternalInterfaceMappingSDto();
            internalInterfaceMappingSDto.Id = internalInterface.Id;
            internalInterfaceMappingSDto.Name = internalInterface.Name;
            internalInterfaceMappingSDto.RouteId = internalInterface.RouteId;
            internalInterfaceMappingSDto.Description = internalInterface.Description;
            internalInterfaceMappingSDto.ResponseParameters = AppRealization.JSON.Deserialize<IList<InterfaceResponseParameter>>(internalInterface.ResponseParameters);
            internalInterfaceMappingSDto.RequestParameters = AppRealization.JSON.Deserialize<IList<InterfaceRequestParameter>>(internalInterface.RequestParameters);
            return internalInterfaceMappingSDto;
        }

        public async Task<PageList<InternalInterfaceMappingRDto>> GetInternalInterfacePageListAsync(InternalInterfaceMappingQDto dto)
        {
            var linq = LinqExpressionExtensions.And<InternalInterfaceMapping>();

            linq = linq.And(s => s.IsDelete == IsOrNotEnum.否);

            if (!dto.Info.IsNullOrEmpty())
            {
                linq=linq.And(s => s.Name.Contains(dto.Info)||s.Description.Contains(dto.Info));
            }

            if (!dto.RouteId.IsNullOrEmpty())
            {
                linq=linq.And(s => s.RouteId == dto.RouteId ||dto.RouteId.Contains(s.RouteId));   
            }

            var internalInterfaceMappings = repository.DetachedEntities.Where(linq).OrderByDescending(s=>s.CreateTime);

            var page = await internalInterfaceMappings.ToPageListAsync(dto.Page, dto.Limit);

            var result = new PageList<InternalInterfaceMappingRDto>()
            {
                Count = page.Count,
                List = page.List.Select(s => new InternalInterfaceMappingRDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    RouteId = s.RouteId,
                    Description = s.Description,
                    RequestParameters = AppRealization.JSON.Deserialize<IList<InterfaceRequestParameter>>(s.RequestParameters),
                    ResponseParameters = AppRealization.JSON.Deserialize<IList<InterfaceResponseParameter>>(s.ResponseParameters)
                }).ToList()
            };
            return result;
        }

        public async Task<IList<InternalInterfaceMappingSDto>> GetInternalInterfaceSelectAsync(InternalInterfaceMappingQDto dto)
        {
            var linq = LinqExpressionExtensions.And<InternalInterfaceMapping>();

            linq = linq.And(s => s.IsDelete == IsOrNotEnum.否);

            if (!dto.Info.IsNullOrEmpty())
            {
                linq = linq.And(s => s.Name.Contains(dto.Info) || s.Description.Contains(dto.Info));
            }
            var internalInterfaceMappings = repository.DetachedEntities.Where(linq);

            var page = await internalInterfaceMappings.OrderBy(s => s.CreateTime).Select(s => new InternalInterfaceMappingSDto
            {
                Id = s.Id,
                Name=s.Name,
                RouteId = s.RouteId,
                Description = s.Description
            }).Skip((dto.Page - 1) * dto.Limit).Take(dto.Limit).ToListAsync();
            if (!dto.RouteId.IsNullOrEmpty())
            {
                var internalInterfaceMapping = await repository.DetachedEntities.FirstOrDefaultAsync(s=>s.Id==dto.RouteId);
                if (internalInterfaceMapping!=null)
                {

                    page.Add(new InternalInterfaceMappingSDto()
                    {
                        Id = internalInterfaceMapping.Id,
                        Name = internalInterfaceMapping.Name,
                        RouteId = internalInterfaceMapping.RouteId,
                        Description = internalInterfaceMapping.Description
                    });
                }
            }
            return page;
        }

        public async Task<string> UpdateInternalInterfaceAsync(InternalInterfaceMappingSDto dto)
        {
            var internalInterface = repository.FirstOrDefault(x => x.Id == dto.Id);

            if (internalInterface == null) throw Oops.Oh("内部接口映射不存在");

            internalInterface.RouteId = dto.RouteId;
            internalInterface.Name = dto.Name;
            internalInterface.Description = dto.Description;
            internalInterface.RequestParameters = AppRealization.JSON.Serialize(dto.RequestParameters);
            internalInterface.ResponseParameters = AppRealization.JSON.Serialize(dto.ResponseParameters);

            await repository.UpdateIncludeAsync(internalInterface, new string[]
            {
                nameof(internalInterface.Description),
                nameof(internalInterface.RequestParameters),
                nameof(internalInterface.ResponseParameters),
                nameof(internalInterface.RouteId),
                nameof(internalInterface.Name)
            });
            return internalInterface.Id;
        }
    }
}
