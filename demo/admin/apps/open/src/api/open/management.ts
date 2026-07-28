import { requestClient } from '#/api/request';

export namespace OpenManagementApi {
  export interface Page<T> {
    list: T[];
    total: number;
  }

  export interface PageQuery {
    current?: number;
    keyword?: string;
    page?: number;
    pageSize?: number;
  }

  export interface InterfaceRequestParameter {
    defaultValue?: null | string;
    description?: null | string;
    encrypt?: number;
    isRequired?: boolean;
    items?: InterfaceRequestParameter[];
    name: string;
    type: string | number;
    validConditions?: string[];
    [key: string]: unknown;
  }

  export interface InterfaceResponseParameter {
    description?: null | string;
    encrypt?: number;
    items?: InterfaceResponseParameter[];
    name: string;
    type: string | number;
    value?: null | string;
    [key: string]: unknown;
  }

  export interface AppInterfaceAuthorizationForm {
    actionId: string;
    actionSecret: string;
    appId: string;
    deleteRemark?: null | string;
    description?: null | string;
    expiredTime: string;
    externalInterfaceId: string;
    id: string;
  }

  export interface InternalInterfaceMappingForm {
    description?: null | string;
    id: string;
    name: string;
    requestParameters?: InterfaceRequestParameter[] | null;
    responseParameters?: InterfaceResponseParameter[] | null;
    routeId: string;
  }

  export interface InternalInterfaceMapping extends InternalInterfaceMappingForm {
    route?: null | string;
    routeAppId?: null | string;
    routeAppName?: null | string;
  }

  export interface ExternalInterfaceMappingForm {
    description?: null | string;
    enableAppEncrypt?: number;
    id: string;
    internalInterfaceId: string;
    internalInterfaceDescription?: null | string;
    internalInterfaceName?: null | string;
    name?: null | string;
    requestParameters?: InterfaceRequestParameter[] | null;
    responseParameters?: InterfaceResponseParameter[] | null;
  }

  export interface ExternalInterfaceMapping extends ExternalInterfaceMappingForm {}

  export interface AuthorizationMeta {
    authenticationSchemes?: null | string;
    policy?: null | string;
    roles?: null | string;
  }

  export interface AppRouteForm {
    allowAnonymous?: null | number;
    appId: string;
    authorizationMetas?: AuthorizationMeta[] | null;
    description?: null | string;
    id: string;
    method?: null | string;
    requiresAuthorization?: null | number;
    route: string;
  }

  export interface AppRoute extends AppRouteForm {
    createTime?: null | string;
    createUserId?: null | string;
    createUserName?: null | string;
    isAutoCreate?: null | number;
  }
}

function toPage<T>(response: any): OpenManagementApi.Page<T> {
  if (Array.isArray(response)) {
    return { list: response, total: response.length };
  }

  const list = response?.list ?? response?.List ?? response?.data ?? [];
  return {
    list: Array.isArray(list) ? list : [],
    total: Number(
      response?.total ??
        response?.Total ??
        response?.count ??
        response?.Count ??
        list?.length ??
        0,
    ),
  };
}

function toLegacyPageQuery(query: OpenManagementApi.PageQuery = {}) {
  return {
    keyword: query.keyword || undefined,
    limit: query.pageSize || 30,
    page: query.page || query.current || 1,
  };
}

/**
 * zh-cn: 保留旧接口开放平台的 URL、HTTP 方法和分页参数，统一转换服务端可能返回的分页包装。
 * en-us: Keeps the legacy Open-platform URLs, HTTP methods, and page parameters while normalizing page wrappers.
 */
export const OpenManagementApi = {
  appAuthorization: {
    check(
      data: Pick<
        OpenManagementApi.AppInterfaceAuthorizationForm,
        'actionId' | 'actionSecret' | 'appId'
      >,
    ) {
      return requestClient.post<boolean>(
        '/v1/open/config/app_auth/check',
        data,
      );
    },
    detail(appId: string, actionId: string) {
      return requestClient.get<OpenManagementApi.AppInterfaceAuthorizationForm>(
        `/v1/open/config/app_auth/detail/${appId}/${actionId}`,
      );
    },
    query(
      query: OpenManagementApi.PageQuery & {
        appId?: string;
        externalInterfaceId?: string;
      } = {},
    ) {
      return requestClient
        .post('/v1/open/config/app_auth/query', {
          ...toLegacyPageQuery(query),
          appId: query.appId || undefined,
          externalInterfaceId: query.externalInterfaceId || undefined,
        })
        .then(toPage<OpenManagementApi.AppInterfaceAuthorizationForm>);
    },
    remove(data: { actionId: string; appId: string; remark?: null | string }) {
      return requestClient.post<boolean>(
        '/v1/open/config/app_auth/remove',
        data,
      );
    },
    save(data: OpenManagementApi.AppInterfaceAuthorizationForm) {
      return requestClient.post<string>('/v1/open/config/app_auth/save', data);
    },
  },
  externalInterfaces: {
    detail(id: string) {
      return requestClient.get<OpenManagementApi.ExternalInterfaceMapping>(
        `/v1/open/mapping/ext_interface/detail/${id}`,
      );
    },
    query(query: OpenManagementApi.PageQuery = {}) {
      return requestClient
        .post('/v1/open/mapping/ext_interface/query', toLegacyPageQuery(query))
        .then(toPage<OpenManagementApi.ExternalInterfaceMapping>);
    },
    remove(id: string) {
      return requestClient.delete<boolean>(
        `/v1/open/mapping/ext_interface/remove/${id}`,
      );
    },
    save(data: OpenManagementApi.ExternalInterfaceMappingForm) {
      return requestClient.post<string>(
        '/v1/open/mapping/ext_interface/save',
        data,
      );
    },
    select(query: OpenManagementApi.PageQuery = {}) {
      return requestClient
        .post('/v1/open/mapping/ext_interface/select', {
          info: query.keyword || undefined,
          limit: query.pageSize || 9999,
          page: query.page || query.current || 1,
        })
        .then(
          (response) =>
            toPage<OpenManagementApi.ExternalInterfaceMapping>(response).list,
        );
    },
  },
  internalInterfaces: {
    detail(id: string) {
      return requestClient.get<OpenManagementApi.InternalInterfaceMapping>(
        `/v1/open/mapping/inter_interface/detail/${id}`,
      );
    },
    query(query: OpenManagementApi.PageQuery = {}) {
      return requestClient
        .post(
          '/v1/open/mapping/inter_interface/query',
          toLegacyPageQuery(query),
        )
        .then(toPage<OpenManagementApi.InternalInterfaceMapping>);
    },
    remove(id: string) {
      return requestClient.delete<boolean>(
        `/v1/open/mapping/inter_interface/remove/${id}`,
      );
    },
    save(data: OpenManagementApi.InternalInterfaceMappingForm) {
      return requestClient.post<string>(
        '/v1/open/mapping/inter_interface/save',
        data,
      );
    },
    select(query: OpenManagementApi.PageQuery = {}) {
      return requestClient
        .post('/v1/open/mapping/inter_interface/select', {
          info: query.keyword || undefined,
          limit: query.pageSize || 9999,
          page: query.page || query.current || 1,
        })
        .then(
          (response) =>
            toPage<OpenManagementApi.InternalInterfaceMapping>(response).list,
        );
    },
  },
  routes: {
    bind(routeId: string, appId: string) {
      return requestClient.get(
        `/v1/security/system/approute/bind/${routeId}/${appId}`,
      );
    },
    detail(id: string) {
      return requestClient.get<OpenManagementApi.AppRoute>(
        `/v1/security/system/approute/detail/${id}`,
      );
    },
    query(query: OpenManagementApi.PageQuery = {}) {
      return requestClient
        .post('/v1/security/system/approute/query/route', {
          info: query.keyword || undefined,
          limit: query.pageSize || 30,
          page: query.page || query.current || 1,
        })
        .then(toPage<OpenManagementApi.AppRoute>);
    },
    queryAllAuth(appId: string) {
      return requestClient.get(
        `/v1/security/system/approute/query/all-auth/${appId}`,
      );
    },
    queryAuth(query: OpenManagementApi.PageQuery = {}) {
      return requestClient.post('/v1/security/system/approute/query/auth', {
        info: query.keyword || undefined,
        limit: query.pageSize || 30,
        page: query.page || query.current || 1,
      });
    },
    remove(id: string) {
      return requestClient.delete<boolean>(
        `/v1/security/system/approute/remove/${id}`,
      );
    },
    removeBind(routeId: string, appId: string) {
      return requestClient.get(
        `/v1/security/system/approute/remove/${routeId}/${appId}`,
      );
    },
    save(data: OpenManagementApi.AppRouteForm) {
      return requestClient.post('/v1/security/system/approute/save', data);
    },
  },
};
