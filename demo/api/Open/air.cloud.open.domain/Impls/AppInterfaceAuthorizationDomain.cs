using air.cloud.open.model.Domains;
using air.cloud.open.model.Dtos;
using air.cloud.open.model.Dtos.AppInterfaceAuthorizationDtos;
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
using Air.Cloud.Core.Extensions.Linqs;
using Air.Cloud.EntityFrameWork.Core.Repositories;

namespace air.cloud.open.domain.Impls
{
    public class AppInterfaceAuthorizationDomain : IAppInterfaceAuthorizationDomain
    {
        private readonly IRepository<AppInterfaceAuthorization> repository;
        private readonly IUserAccountStore userAccountStore;
        public AppInterfaceAuthorizationDomain(IRepository<AppInterfaceAuthorization> repository,
            IUserAccountStore accountStore) {
            this.repository = repository;
            this.userAccountStore = accountStore;
        }
        public async Task<bool> CheckAppInterfaceHasAuthorization(string AppId, string ActionId,string ActionSecret)
        {
            var AppAuth=await repository.FirstOrDefaultAsync(s => s.AppId == AppId && s.ActionId == ActionId);
            if (AppAuth == null) return false;
            if (AppAuth.ExpiredTime < DateTime.Now) return false;
            return true;
        }

        public Task<string> CreateAppInterfaceAuthorization(AppInterfaceAuthorizationSDto dto)
        {
            var AppAuth = new AppInterfaceAuthorization
            {
                AppId = dto.AppId,
                ActionId = AppCore.Guid(),
                Description = dto.Description,
                ExpiredTime = dto.ExpiredTime,
                ActionSecret = AppCore.Guid(),
                ExternalInterfaceId = dto.ExternalInterfaceId,
                Id = AppCore.Guid(),
                DeleteRemark = string.Empty
            };
            return repository.InsertAsync(AppAuth)
                .ContinueWith(t => t.Result.Entity.Id);

        }

        public async Task<bool> DeleteAppInterfaceAuthorization(string AppId, string ActionId, string Remark)
        {
            UserAccountFactory userAccount= await userAccountStore.GetUserAccountAsync();

            var AppInterfaceAuth=await repository.FirstOrDefaultAsync(s => s.AppId == AppId && s.ActionId == ActionId);

            if (AppInterfaceAuth == null) return true;

            AppInterfaceAuth.DeleteRemark = Remark;
            AppInterfaceAuth.IsDelete = security.common.Enums.IsOrNotEnum.是;
            AppInterfaceAuth.DeleteTime = DateTime.Now;
            AppInterfaceAuth.DeleteUserId = userAccount.Id;

            await repository.UpdateIncludeAsync(AppInterfaceAuth, new string[] {
               nameof(AppInterfaceAuth.DeleteRemark),
               nameof(AppInterfaceAuth.DeleteTime),
               nameof(AppInterfaceAuth.DeleteUserId),
               nameof(AppInterfaceAuth.IsDelete)
            });
            return true;
        }

        public async Task<AppInterfaceAuthorizationSDto?> GetAppInterfaceAuthorizationAsync(string AppId, string ActionId)
        {
            var AppAuth = await repository.FirstOrDefaultAsync(s => s.AppId == AppId && s.ActionId == ActionId);
            if (AppAuth == null) return null;
            return new AppInterfaceAuthorizationSDto()
            {
                Id= AppAuth.Id,
                AppId = AppAuth.AppId,
                Description = AppAuth.Description,
                ExternalInterfaceId = AppAuth.ExternalInterfaceId,
                ActionSecret = AppAuth.ActionSecret,
                ActionId = AppAuth.ActionId,
                DeleteRemark = AppAuth.DeleteRemark,
                ExpiredTime = AppAuth.ExpiredTime
            };
        }

        public async Task<PageList<AppInterfaceAuthorizationSDto>> QueryAppInterfaceAuthorization(AppInterfaceAuthorizationQDto dto)
        {
            var linq = LinqExpressionExtensions.And<AppInterfaceAuthorization>();

            linq = linq.And(s => s.IsDelete == IsOrNotEnum.否);
            if (!dto.AppId.IsNullOrEmpty())
            {
                linq = linq.And(s => s.AppId == dto.AppId);
            }
            if (!dto.ExternalInterfaceId.IsNullOrEmpty())
            {
                linq = linq.And(s => s.ExternalInterfaceId == dto.ExternalInterfaceId);
            }
            var appInterfaceAuthorizations = repository.DetachedEntities.Where(linq).OrderByDescending(s=>s.CreateTime)
                .Select(s => new AppInterfaceAuthorizationSDto
            {
                Id = s.Id,
                AppId = s.AppId,
                Description = s.Description,
                ExternalInterfaceId = s.ExternalInterfaceId,
                ActionSecret = s.ActionSecret,
                ActionId = s.ActionId,
                DeleteRemark = s.DeleteRemark,
                ExpiredTime = s.ExpiredTime
            });
            var result = await appInterfaceAuthorizations.ToPageListAsync<AppInterfaceAuthorizationSDto>(dto.Page, dto.Limit);
            return result;
        }

        public async Task<string> UpdateAppInterfaceAuthorization(AppInterfaceAuthorizationSDto dto)
        {
            var AppAuth = await repository.FirstOrDefaultAsync(s => s.AppId == dto.AppId && s.ActionId == dto.ActionId);
            if (AppAuth == null) return string.Empty;

            AppAuth.Description = dto.Description;
            AppAuth.ExpiredTime = dto.ExpiredTime;

            await repository.UpdateIncludeAsync(AppAuth, new string[] {
               nameof(AppAuth.Description),
               nameof(AppAuth.ExpiredTime)
            });
            return AppAuth.Id;
        }
    }
}
