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
using air.cloud.security.common.Base.Dtos;
using air.cloud.security.common.Dtos;
using air.cloud.security.common.Extensions;
using air.cloud.security.common.Model;
using air.cloud.system.model.Domains.AppInfoDomains;
using air.cloud.system.model.Dtos.AppAuthDtos;
using air.cloud.system.model.Dtos.AppInfoDtos;
using air.cloud.system.model.Dtos.AppRouteDtos;
using air.cloud.system.model.Entitys.Apps;

using Air.Cloud.Core;
using Air.Cloud.Core.App;
using Air.Cloud.Core.Extensions;
using Air.Cloud.Core.Extensions.Linqs;
using Air.Cloud.Core.Standard.SkyMirror.Model;
using Air.Cloud.Core.Standard.SkyMirror.Model;
using Air.Cloud.EntityFrameWork.Core.Repositories;
using Air.Cloud.WebApp.FriendlyException;

using Mapster;

using Microsoft.EntityFrameworkCore;

namespace air.cloud.system.domain.Impls.AppInfoDomains
{
    public class AppRouteDomain : IAppRouteDomain
    {
        private readonly IRepository<AppRoute> repository;

        private readonly IRepository<AppRouteAuth> authRepository;

        private readonly IAppInfoDomain appInfoDomain;


        public AppRouteDomain(IRepository<AppRoute> repository,
            IRepository<AppRouteAuth> authRepository,
            IAppInfoDomain appInfoDomain)
        {
            this.repository = repository;
            this.authRepository = authRepository;
            this.appInfoDomain = appInfoDomain;
        }

        public async Task<AppAllRouteAuthResultDto> QueryAllAppRouteAuthAsync(string BindAppId)
        {
            AppAllRouteAuthResultDto appAllRouteAuthResultDto = new AppAllRouteAuthResultDto();

            var App = await appInfoDomain.GetAppInfoAsync(BindAppId);

            if (App == null)
            {
                appAllRouteAuthResultDto.AppExist = false;

                return appAllRouteAuthResultDto;
            }

            appAllRouteAuthResultDto.AppExist = true;

            appAllRouteAuthResultDto.AppStatus = new AppStatusResultDto()
            {
                AppIsDelete = App.IsDelete == security.common.Enums.IsOrNotEnum.否 ? false : true,
                AppIsEnable = App.IsEnable == security.common.Enums.IsOrNotEnum.否 ? false : true
            };

            //当前应用所有授权的记录
            var CurrentAppAllAuthRoutes = authRepository.DetachedEntities
                .Where(a => a.AppId == BindAppId)
                .Select(s => new
                {
                    s.RouteId,
                    s.AppId,
                    s.Description,
                    s.CreateTime
                }).AsQueryable();

            var Routes = repository.DetachedEntities
                .Select(s => new
                {
                    s.Route,
                    s.Id,
                    s.AppId,
                    s.AllowAnonymous,
                    s.RequiresAuthorization,
                    s.AuthorizationMeta,
                    //自动创建的不能删除
                    s.IsAutoCreate,
                    s.Method,
                    s.CreateTime,
                    s.Description
                }).AsQueryable();

            var AuthRoutes = await (from a in CurrentAppAllAuthRoutes
                                    join b in Routes on a.RouteId equals b.Id
                                    select new
                                    {
                                        Description = a.Description,
                                        RequiresAuthorization = b.RequiresAuthorization,
                                        Method = b.Method,
                                        AuthorizeData = b.AuthorizationMeta,
                                        IsAllowAnonymous = b.AllowAnonymous,
                                        Path = b.Route
                                    }).ToListAsync();

            appAllRouteAuthResultDto.EndpointDatas = AuthRoutes.Select(s => new EndpointData()
            {
                Description = s.Description,
                AuthorizeDatas = AppRealization.JSON.Deserialize<List<EndPointAuthorizeData>>(s.AuthorizeData),
                IsAllowAnonymous = s.IsAllowAnonymous == security.common.Enums.IsOrNotEnum.是 ? true : false,
                Path = s.Path,
                Method = s.Method,
                RequiresAuthorization = s.RequiresAuthorization == security.common.Enums.IsOrNotEnum.是 ? true : false,

            }).ToList();
            return appAllRouteAuthResultDto;
        }
        public async Task<PageList<AppRouteAuthResultDto>> QueryAppRouteAuthAsync(BaseQDto dto)
        {
            if (dto.AppId.IsNullOrEmpty()) throw Oops.Oh("应用编号不能为空");

            var AllAppInfo = await repository.Change<AppInformation>().DetachedEntities.AsQueryable().Select(s => new
            {
                s.AppId,
                s.AppName,
                s.IsDelete,
                s.IsEnable
            }).ToListAsync();

            //所有的路由记录
            var AllRoutes = repository.DetachedEntities.AsQueryable().Select(s => new
            {
                s.Route,
                s.Id,
                s.AppId,
                s.AllowAnonymous,
                s.RequiresAuthorization,
                s.AuthorizationMeta,
                //自动创建的不能删除
                s.IsAutoCreate,
                s.Method,
                s.CreateTime,
                s.Description
            }).AsQueryable();

            if (!dto.Info.IsNullOrEmpty())
            {
                AllRoutes = AllRoutes.Where(s => s.Route.Contains(dto.Info) || dto.Info.Contains(s.Route) || s.Description.Contains(dto.Info));
            }
            var Count = await AllRoutes.CountAsync();
            var Routes = await AllRoutes.Skip((dto.Page - 1) * dto.Limit).Take(dto.Limit).ToListAsync();

            //当前应用所有授权的记录
            var CurrentAppAllAuthRoutes = await authRepository.DetachedEntities.Where(a => a.AppId == dto.AppId).Select(s => new
            {
                s.RouteId,
                s.AppId,
                s.Description,
                s.CreateTime
            }).AsQueryable().ToListAsync();

            var Routes1 = Routes.Select(s =>
            {
                var CurrentAuth = CurrentAppAllAuthRoutes.FirstOrDefault(a => a.RouteId == s.Id);
                bool IsAuth = CurrentAuth != null;
                var App = AllAppInfo.FirstOrDefault(a => a.AppId == s.AppId);
                return new AppRouteAuthResultDto()
                {
                    AppId = s.AppId,
                    AppName = App?.AppName,
                    AppIsDelete = App?.IsDelete,
                    AppIsEnable = App?.IsEnable,
                    CreateTime = s.CreateTime,
                    AuthTime = CurrentAuth?.CreateTime,
                    //授权时填写的备注
                    Description = CurrentAuth?.Description,
                    //路由备注
                    RouteDescription = s.Description,
                    Route = s.Route,
                    Id = s.Id,
                    AllowAnonymous = s.AllowAnonymous,
                    RequiresAuthorization = s.RequiresAuthorization,
                    AuthorizationMeta = s.AuthorizationMeta,
                    IsAutoCreate = s.IsAutoCreate,
                    Method = s.Method,
                    IsBind = IsAuth
                };
            }).ToList();
            return new PageList<AppRouteAuthResultDto>()
            {
                Count = Count,
                List = Routes1
            };
        }

        public async Task<bool> BindAppRouteAsync(string RouteId, string BindAppId)
        {
            AppRouteAuth appRouteAuth = new AppRouteAuth()
            {
                AppId = BindAppId,
                Id = AppCore.Guid(),
                RouteId = RouteId
            };
            await authRepository.InsertAsync(appRouteAuth);

            await QueryAllAppRouteAuthAsync(BindAppId);
            return true;
        }
        public async Task<bool> RemoveAppRouteAuthAsync(string RouteId, string BindAppId)
        {
            var auth = await authRepository.DetachedEntities.FirstOrDefaultAsync(a => a.AppId == BindAppId && a.RouteId == RouteId);
            if (auth != null)
            {
                await authRepository.DeleteAsync(auth);
            }
            await QueryAllAppRouteAuthAsync(BindAppId);
            return true;
        }
        public async Task<bool> CreateAppRouteAsync(AppRouteSDto dto)
        {
            var HasSameAppRoute = await repository.DetachedEntities.AnyAsync(a => a.Route == dto.Route && a.AppId == dto.AppId);
            if (HasSameAppRoute) throw Oops.Oh("已存在相同的路由地址");

            AppRoute appRoute = new AppRoute()
            {
                AppId = dto.AppId,
                Id = AppCore.Guid(),
                Route = dto.Route,
                Description = dto.Description,
                AuthorizationMeta = dto.AuthorizationMeta,
                Method = dto.Method,
                AllowAnonymous = dto.AllowAnonymous,
                RequiresAuthorization = dto.RequiresAuthorization,
                IsAutoCreate = security.common.Enums.IsOrNotEnum.否
            };
            await repository.InsertAsync(appRoute);
            return true;
        }

        public async Task<bool> DeleteAppRouteAsync(string Id)
        {
            var route = await repository.DetachedEntities.FirstOrDefaultAsync(a => a.Id == Id);
            if (route == null) throw Oops.Oh("路由地址不存在");

            var hasRouteAuth = await authRepository.DetachedEntities.AnyAsync(s => s.RouteId == route.Id);
            if (hasRouteAuth) throw Oops.Oh("该路由地址已被应用,无法删除");

            if (route.IsAutoCreate == security.common.Enums.IsOrNotEnum.是)
            {
                throw Oops.Oh("该路由地址为系统服务自动创建,无法删除");
            }
            await repository.DeleteAsync(route);
            return true;
        }

        public async Task<AppRoute> GetAppRouteAsync(string Id)
        {
            var route = await repository.DetachedEntities.FirstOrDefaultAsync(a => a.Id == Id);
            if (route == null) throw Oops.Oh("路由地址不存在");
            return route;
        }

        public async Task<PageList<AppRouteSDto>> QueryAppRoutesAsync(BaseQDto dto)
        {
            var linq = LinqExpressionExtensions.And<AppRoute>();
            if (!string.IsNullOrEmpty(dto.AppId))
            {
                linq = linq.And(s => s.AppId == dto.AppId);
            }
            if (!string.IsNullOrEmpty(dto.Info))
            {
                linq = linq.And(s => s.Route.Contains(dto.Info));
            }
            var query = repository.Change<AppRoute>().DetachedEntities.Where(linq);
            var data = await query.OrderByDescending(s => s.CreateTime).ToPageListAsync<AppRoute>(dto.Page, dto.Limit);

            return new PageList<AppRouteSDto>()
            {
                Count = data.Count,
                List = data.List.Select(s =>
                {
                    var route = s.Adapt<AppRouteSDto>();
                    route.SetAuthorizationMetasFromString(s.AuthorizationMeta);
                    return route;
                }).ToList()
            };

        }

        public async Task<bool> UpdateAppRouteAsync(AppRouteSDto dto)
        {
            var HasSameAppRoute = await repository.DetachedEntities.AnyAsync(a => a.Route == dto.Route && a.AppId == dto.AppId && a.Id != dto.Id);
            if (HasSameAppRoute) throw Oops.Oh("已存在相同的路由地址");
            var route = await repository.DetachedEntities.FirstOrDefaultAsync(a => a.Id == dto.Id);
            if (route != null)
            {
                if (route.IsAutoCreate == security.common.Enums.IsOrNotEnum.是)
                {
                    throw Oops.Oh("系统自动创建的路由不允许修改");
                }
                route.Route = dto.Route;
                route.Description = dto.Description;
                route.Method = dto.Method;
                route.AuthorizationMeta = dto.AuthorizationMeta;
                route.AllowAnonymous = dto.AllowAnonymous;
                route.RequiresAuthorization = dto.RequiresAuthorization;
                await repository.UpdateIncludeAsync(route, new string[]
                {
                    nameof(route.Route),
                    nameof(route.Description),
                    nameof(route.Method),
                    nameof(route.AuthorizationMeta),
                    nameof(route.AllowAnonymous),
                    nameof(route.RequiresAuthorization)
                });
                return true;
            }
            return true;
        }

        public async Task<bool> BindCurrentServiceAllRouteToAppAsync(IList<AppRouteSDto> routes, string AppId)
        {
            var CheckThisAppIsBind = await repository.DetachedEntities.AnyAsync(s => s.AppId == AppId && s.IsAutoCreate == security.common.Enums.IsOrNotEnum.是);
            if (CheckThisAppIsBind) return true;
            //创建所有路由
            IList<AppRoute> appRoutes = routes.Select(s =>
            {
                var r = s.Adapt<AppRoute>();
                r.IsAutoCreate = security.common.Enums.IsOrNotEnum.是;
                r.AppId = AppId;
                r.Id = AppCore.Guid();
                return r;
            }).ToList();
            await repository.InsertAsync(appRoutes.ToArray());

            //绑定所有路由到应用授权
            IList<AppRouteAuth> appRouteAuths = appRoutes.Select(s =>
            {
                var auth = new AppRouteAuth()
                {
                    AppId = AppId,
                    RouteId = s.Id,
                    Id = AppCore.Guid()
                };
                return auth;
            }).ToList();
            await authRepository.InsertAsync(appRouteAuths.ToArray());
            return true;
        }


        public async Task<IList<AppRouteQueryByIdsResultDto>> QueryAppRouteDataAsync(AppRouteQueryByIdsDto dto)
        {
            var ids = dto.Ids.Split(',').Distinct().ToList();
            var datas = from a in (repository.DetachedEntities
                        .Where(s => ids.Contains(s.Id))
                        .Select(s => new
                        {
                            Id = s.Id,
                            AppId = s.AppId,
                            Route = s.Route
                        }))
                        join b in (repository.Change<AppInformation>().DetachedEntities.Select(s => new
                        {
                            s.AppId,
                            s.AppName
                        }))
                        on a.AppId equals b.AppId
                        select new AppRouteQueryByIdsResultDto
                        {
                            Id = a.Id,
                            AppId = b.AppId,
                            Route = a.Route,
                            AppName = b.AppName
                        };
            return await datas.ToListAsync();
        }

    }
}
