using air.cloud.open.model.Domains;
using air.cloud.open.model.Dtos.ExternalInterfaceMappingDtos;
using air.cloud.open.model.Entitys;
using air.cloud.open.model.Models;
using air.cloud.security.common.Auths;
using air.cloud.security.common.Base.Dtos;
using air.cloud.security.common.Enums;
using air.cloud.security.common.Extensions;
using air.cloud.security.common.Model;

using Air.Cloud.Core;
using Air.Cloud.Core.App;
using Air.Cloud.Core.Extensions;
using Air.Cloud.Core.Extensions.IEnumerables;
using Air.Cloud.Core.Extensions.Linqs;
using Air.Cloud.EntityFrameWork.Core.Repositories;
using Air.Cloud.WebApp.FriendlyException;

using Microsoft.EntityFrameworkCore;

namespace air.cloud.open.domain.Impls
{
    public class ExternalInterfaceMappingDomain : IExternalInterfaceMappingDomain
    {
        private readonly IRepository<ExternalInterfaceMapping> repository;
        private readonly IUserAccountStore userAccountStore;
        public ExternalInterfaceMappingDomain(IRepository<ExternalInterfaceMapping> repository,IUserAccountStore userAccountStore) { 
            this.repository = repository;
            this.userAccountStore = userAccountStore;
        
        }

        public async Task<string> CreateExternalInterfaceAsync(ExternalInterfaceMappingSDto dto)
        {
            ExternalInterfaceMapping entity = new ExternalInterfaceMapping
            {
                Id= AppCore.Guid(),
                Name= dto.Name,
                InternalInterfaceId = dto.InternalInterfaceId,
                Description = dto.Description,
                EnableAppEncrypt = dto.EnableAppEncrypt,
                RequestParameters = AppRealization.JSON.Serialize(dto.RequestParameters),
                ResponseParameters = AppRealization.JSON.Serialize(dto.ResponseParameters)
            };
            await repository.InsertAsync(entity);
            return entity.Id;
        }

        public async  Task<bool> DeleteExternalInterfaceAsync(string Id)
        {
            if (string.IsNullOrEmpty(Id)) return true;
            UserAccountFactory userAccountFactory =await userAccountStore.GetUserAccountAsync();
            if (userAccountFactory == null)
            {
                AppRealization.Output.Print("删除外部接口映射", "无法获取当前登录用户信息,已返回true");
                return true;
            }
            var externalInterface = repository.FirstOrDefault(x => x.Id == Id);
            if (externalInterface == null)
            {
                AppRealization.Output.Print("删除外部接口映射", "无法获取当前外部接口映射,已返回true");
                return true;
            }
            externalInterface.IsDelete = security.common.Enums.IsOrNotEnum.是;
            externalInterface.DeleteUserId = userAccountFactory.Id;
            externalInterface.DeleteUserName = userAccountFactory.UserName;
            externalInterface.DeleteTime = DateTime.Now;
            await repository.UpdateIncludeAsync(externalInterface, new string[]
            {
                nameof(externalInterface.IsDelete),
                nameof(externalInterface.DeleteUserId),
                nameof(externalInterface.DeleteUserName),
                nameof(externalInterface.DeleteTime)
            });
            return true;
        }

        public async  Task<ExternalInterfaceMappingSDto> GetExternalInterfaceAsync(string Id)
        {
            if (string.IsNullOrEmpty(Id)) return null;
            var externalInterface =await  repository.FirstOrDefaultAsync(y => y.Id == Id&&y.IsDelete==IsOrNotEnum.否);
            if (externalInterface == null) return null;
            ExternalInterfaceMappingSDto externalInterfaceMappingSDto=new ExternalInterfaceMappingSDto();
            externalInterfaceMappingSDto.Id = Id;
            externalInterfaceMappingSDto.Name= externalInterface.Name;
            externalInterfaceMappingSDto.InternalInterfaceId = externalInterface.InternalInterfaceId;
            externalInterfaceMappingSDto.ResponseParameters = AppRealization.JSON.Deserialize<IList<InterfaceResponseParameter>>(externalInterface.ResponseParameters);
            externalInterfaceMappingSDto.RequestParameters= AppRealization.JSON.Deserialize<IList<InterfaceRequestParameter>>(externalInterface.RequestParameters);
            externalInterfaceMappingSDto.Description = externalInterface.Description;
            externalInterfaceMappingSDto.EnableAppEncrypt = externalInterface.EnableAppEncrypt;
            return externalInterfaceMappingSDto;
        }

        public async Task<security.common.Model.PageList<ExternalInterfaceMappingSDto>> GetExternalInterfacePageListAsync(ExternalInterfaceMappingQDto dto)
        {
            var linq = LinqExpressionExtensions.And<ExternalInterfaceMapping>();

            linq = linq.And(s => s.IsDelete == IsOrNotEnum.否);

            if (!dto.Info.IsNullOrEmpty())
            {
                linq = linq.And(s => s.Name.Contains(dto.Info));
            }
            if (!dto.InternalInterfaceId.IsNullOrEmpty())
            {
                linq=linq.And(s=>s.InternalInterfaceId== dto.InternalInterfaceId);
            }
            var externalInterfaceMappings = repository.DetachedEntities.Where(linq);

            var Count = await externalInterfaceMappings.CountAsync();

            var List = from a in externalInterfaceMappings.OrderByDescending(s => s.CreateTime)
                       join b in (repository.Change<InternalInterfaceMapping>().DetachedEntities.Select(s => new
                       {
                           s.Id,
                           s.Name,
                           s.Description
                       }))
                       on a.InternalInterfaceId equals b.Id
                       select new
                       {
                           Id = a.Id,
                           Name = a.Name,
                           InternalInterfaceId = a.InternalInterfaceId,
                           Description = a.Description,
                           EnableAppEncrypt = a.EnableAppEncrypt,
                           RequestParameters = a.RequestParameters,
                           ResponseParameters = a.ResponseParameters,
                           InternalInterfaceName = b.Name,
                           InternalInterfaceDescription= b.Description
                       };
            var result=new PageList<ExternalInterfaceMappingSDto>()
            {
                Count= Count,
                List= List.Skip((dto.Page - 1) * dto.Limit).Take(dto.Limit).Select(s=>new ExternalInterfaceMappingSDto
                {
                    Id=s.Id,
                    Name=s.Name,    
                    InternalInterfaceId=s.InternalInterfaceId,
                    Description=s.Description,
                    EnableAppEncrypt=s.EnableAppEncrypt,
                    RequestParameters= AppRealization.JSON.Deserialize<IList<InterfaceRequestParameter>>(s.RequestParameters),
                    ResponseParameters= AppRealization.JSON.Deserialize<IList<InterfaceResponseParameter>>(s.ResponseParameters),
                    InternalInterfaceName=s.InternalInterfaceName,
                    InternalInterfaceDescription=s.InternalInterfaceDescription

                }).ToList()
            };
            return result;
        }

        public async Task<IList<ExternalInterfaceMappingSDto>> GetExternalInterfaceSelectAsync(ExternalInterfaceMappingQDto dto)
        {
            var linq = LinqExpressionExtensions.And<ExternalInterfaceMapping>();

            linq = linq.And(s => s.IsDelete == IsOrNotEnum.否);

            if (!dto.Info.IsNullOrEmpty())
            {
                linq = linq.And(s => s.Name.Contains(dto.Info));
            }
            if (!dto.InternalInterfaceId.IsNullOrEmpty())
            {
                linq = linq.And(s => s.InternalInterfaceId == dto.InternalInterfaceId);
            }
            var internalInterfaceMappings = repository.DetachedEntities.Where(linq);

            var page = await internalInterfaceMappings.OrderByDescending(s => s.CreateTime).Select(s => new ExternalInterfaceMappingSDto
            {
                Id = s.Id,
                Name = s.Name,
                EnableAppEncrypt = s.EnableAppEncrypt,
                InternalInterfaceId=s.InternalInterfaceId,
                Description = s.Description
            }).Skip((dto.Page - 1) * dto.Limit).Take(dto.Limit).ToListAsync();
            return page;
        }

        public async Task<string> UpdateExternalInterfaceAsync(ExternalInterfaceMappingSDto dto)
        {
            if (string.IsNullOrEmpty(dto.Id)) throw Oops.Oh("外部接口映射编号不能为空");
            var externalInterface = repository.FirstOrDefault(x => x.Id == dto.Id);

            if(externalInterface==null) throw Oops.Oh("外部接口映射不存在");
            externalInterface.Name = dto.Name;
            externalInterface.EnableAppEncrypt= dto.EnableAppEncrypt;
            externalInterface.Description= dto.Description;
            externalInterface.RequestParameters= AppRealization.JSON.Serialize(dto.RequestParameters);
            externalInterface.ResponseParameters= AppRealization.JSON.Serialize(dto.ResponseParameters);

            await repository.UpdateIncludeAsync(externalInterface, new string[]
            {
                nameof(externalInterface.EnableAppEncrypt),
                nameof(externalInterface.Description),
                nameof(externalInterface.RequestParameters),
                nameof(externalInterface.ResponseParameters),
                nameof(externalInterface.Name)
            });
            return externalInterface.Id;
        }
    }
}
