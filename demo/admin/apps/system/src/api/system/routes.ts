import { requestClient } from '#/api/request';

export namespace AppRouteApi {
  export interface AuthorizationMeta {
    authenticationSchemes?: null | string;
    policy?: null | string;
    roles?: null | string;
  }

  export interface Route {
    allowAnonymous?: null | number;
    appId: string;
    authorizationMetas?: AuthorizationMeta[] | null;
    createTime?: null | string;
    description?: null | string;
    id: string;
    method?: null | string;
    requiresAuthorization?: null | number;
    route: string;
  }

  export interface Page {
    list: Route[];
    total: number;
  }
}

function toPage(response: any): AppRouteApi.Page {
  const list = response?.list ?? response?.List ?? [];
  return {
    list: Array.isArray(list) ? list : [],
    total:
      Number(
        response?.total ?? response?.Total ?? response?.Count ?? list.length,
      ) || 0,
  };
}

/**
 * zh-cn: System 路由管理使用后端既有的 AppRoute 接口，不变更 URL、方法或分页字段。
 * en-us: System route management uses the existing AppRoute endpoints without changing their URLs, methods, or page fields.
 */
export const AppRouteApi = {
  detail(id: string) {
    return requestClient.get<AppRouteApi.Route>(
      `/v1/security/system/approute/detail/${id}`,
    );
  },
  query(
    params: { current?: number; keyword?: string; pageSize?: number } = {},
  ) {
    return requestClient
      .post('/v1/security/system/approute/query/route', {
        Info: params.keyword || undefined,
        Limit: params.pageSize || 30,
        Page: params.current || 1,
      })
      .then(toPage);
  },
  remove(id: string) {
    return requestClient.delete(`/v1/security/system/approute/remove/${id}`);
  },
  save(data: AppRouteApi.Route) {
    return requestClient.post('/v1/security/system/approute/save', data);
  },
};
