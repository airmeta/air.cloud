import { requestClient } from '#/api/request';

export namespace SystemApi {
  export type ManagementQuery = Record<string, number | string | undefined>;

  export interface SystemPage<T> {
    list: T[];
    total: number;
  }

  export interface Association {
    associationType: string;
    id: string;
    sourceEntityId: string;
    targetEntityId: string;
  }

  export interface User {
    appUserId?: string;
    assignmentIds: string[];
    departmentIds: string[];
    email?: string;
    id: string;
    idCardNo?: string;
    phoneNumber?: string;
    roleIds: string[];
    userName: string;
    account: string;
  }

  export interface Role {
    appId?: string;
    description?: string;
    id: string;
    menuIds: string[];
    roleName: string;
    targetId?: string;
  }

  export interface Menu {
    appId?: string;
    authority?: string;
    checked?: boolean;
    component?: string;
    hide: number;
    icon?: string;
    id: string;
    parentId?: string;
    path: string;
    sortNumber?: number;
    title: string;
    type: number;
  }

  export interface Configuration {
    description?: string;
    id: string;
    key: string;
    status: number;
    value: string;
  }

  export interface CodeName {
    code: string;
    description?: string;
    id: string;
    name: string;
    status: number;
  }

  export interface Assignment {
    appId?: string;
    departmentId: string;
    description?: string;
    id: string;
    name: string;
  }

  export interface Department {
    code: string;
    description?: string;
    id: string;
    name: string;
    parentId?: string;
    managedAppIds?: string;
    managedRegions?: string;
  }

  export interface Application {
    appEncryptType?: number;
    appId: string;
    appName: string;
    appPrivateKey?: string;
    appRedirectUrl?: string;
    description?: string;
    enableMFA: number;
    id: string;
    isCommonApp: number;
    isEnable: number;
    logo?: string;
    publicKey?: string;
  }

  export interface Region {
    appId: string;
    children?: Region[];
    code: string;
    description?: string;
    id: string;
    lngAndSat?: string;
    name: string;
    parentId?: string;
    parentRegionId?: string;
    type: number;
  }

  export interface DictionaryType {
    code: string;
    description?: string;
    id: string;
    name: string;
    meta?: string;
    value: string;
  }

  export interface DictionaryItem {
    code: string;
    description?: string;
    dictionaryTypeId: string;
    id: string;
    name: string;
    meta?: string;
    value: string;
  }
}

function normalizeQuery(params?: SystemApi.ManagementQuery) {
  if (!params) {
    return undefined;
  }

  return Object.fromEntries(
    Object.entries(params).filter(
      ([, value]) => value !== undefined && value !== '',
    ),
  );
}

function toPageQuery(params?: SystemApi.ManagementQuery) {
  return normalizeQuery({
    ...params,
    keywords: params?.keyword || params?.keywords,
    pageNum: params?.pageNum || params?.current || 1,
    pageSize: params?.pageSize || 9999,
  });
}

function toSystemQueryBody(params?: SystemApi.ManagementQuery) {
  return {
    AppId: params?.appId ?? getCurrentAppId(),
    Code: params?.code,
    DepartmentId: params?.departmentId,
    Info: params?.keyword || params?.keywords,
    Label: params?.label,
    Limit: params?.pageSize || 9999,
    Page: params?.pageNum || params?.current || 1,
    ParentId: params?.parentId,
    RoleId: params?.roleId,
  };
}

const SYSTEM_SERVICE_PATH = '/v1/security/system';
const CHOOSE_APP_STORAGE_KEY = 'vea:auth:choose_app';
const APP_CONFIG_STORAGE_KEY = 'app_config';
const SYSTEM_APP_ID =
  import.meta.env.VITE_APP_ID || '4841de0858714c5e8bee36e2fad70614';

function getCurrentAppId() {
  if (typeof sessionStorage === 'undefined') {
    return SYSTEM_APP_ID;
  }

  const appConfigString = sessionStorage.getItem(APP_CONFIG_STORAGE_KEY);
  if (appConfigString) {
    try {
      const appConfig = JSON.parse(appConfigString) as { appId?: string };
      if (appConfig.appId) {
        return appConfig.appId;
      }
    } catch {
      // ignore invalid app config
    }
  }

  return sessionStorage.getItem(CHOOSE_APP_STORAGE_KEY) || SYSTEM_APP_ID;
}

function pageResult<T>(
  result: PageResult<T[]> | T[] | undefined,
): SystemApi.SystemPage<T> {
  if (Array.isArray(result)) {
    return { list: result, total: result.length };
  }

  if (!result) {
    return { list: [], total: 0 };
  }

  const response = result as PageResult<T[]> & {
    Count?: number;
    List?: T[];
    Total?: number;
    count?: number;
  };
  const list = response.list || response.List || [];
  const total =
    response.total ??
    response.Total ??
    response.count ??
    response.Count ??
    list.length;
  return { list, total: Number(total) || 0 };
}

function mapPage<TSource, TResult>(
  result: PageResult<TSource[]> | TSource[] | undefined,
  mapper: (row: TSource) => TResult,
): SystemApi.SystemPage<TResult> {
  const page = pageResult(result);
  return { list: page.list.map(mapper), total: page.total };
}

function toNumberIds(ids?: Array<number | string> | null) {
  return (ids || [])
    .map((id) => Number(id))
    .filter((id) => Number.isFinite(id));
}

const dictionaryCodeById = new Map<string, string>();
const dictionaryCodeByItemId = new Map<string, string>();

function splitIds(value?: string[] | string | null) {
  if (Array.isArray(value)) {
    return value.map(String).filter(Boolean);
  }

  return String(value || '')
    .split(',')
    .map((item) => item.trim())
    .filter(Boolean);
}

function getPage<T>(
  url: string,
  params?: SystemApi.ManagementQuery,
): Promise<SystemApi.SystemPage<T>> {
  return requestClient
    .get<PageResult<T[]> | T[]>(url, {
      params: toPageQuery(params),
    })
    .then(pageResult);
}

function mapUser(row: any): SystemApi.User {
  const roleIds = splitIds(row.roleIds ?? row.RoleIds);
  const departmentIds = splitIds(row.departmentIds ?? row.DepartmentIds);
  const assignmentIds = splitIds(row.assignmentIds ?? row.AssignmentIds);
  return {
    account: row.account || row.Account || '',
    appUserId: row.appUserId || row.AppUserId || '',
    assignmentIds,
    departmentIds,
    email: row.email || row.Email || '',
    id: String(row.id || row.Id || ''),
    idCardNo: row.idCardNo || row.IdCardNo || '',
    phoneNumber: row.phoneNumber || row.PhoneNumber || '',
    roleIds,
    userName: row.userName || row.UserName || '',
  };
}

function mapRole(row: any): SystemApi.Role {
  return {
    appId: row.appId || row.AppId || '',
    description: row.description || row.Description || '',
    id: String(row.id || row.Id || ''),
    menuIds: Array.isArray(row.menuIds)
      ? row.menuIds.map(String)
      : splitIds(row.MenuIds),
    roleName: row.roleName || row.RoleName || '',
    targetId: row.targetId || row.TargetId || '',
  };
}

function mapMenu(row: any): SystemApi.Menu {
  return {
    appId: row.appId || row.AppId || '',
    authority: row.authority || row.Authority || '',
    checked: row.checked ?? row.Checked ?? false,
    component: row.component || row.Component || '',
    hide: row.hide ?? row.Hide ?? 0,
    icon: row.icon || row.Icon || '',
    id: String(row.id || row.Id || ''),
    parentId: row.parentId || row.ParentId || '',
    path: row.path || row.Path || '',
    sortNumber: row.sortNumber ?? row.SortNumber ?? 0,
    title: row.title || row.Title || '',
    type: row.type ?? row.Type ?? 1,
  };
}

function mapDepartment(row: any): SystemApi.Department {
  return {
    code:
      row.code || row.Code || row.departmentCode || row.DepartmentCode || '',
    description: row.description || row.Description || '',
    id: String(row.id || row.Id || ''),
    managedAppIds: row.managedAppIds || row.ManagedAppIds || '',
    managedRegions: row.managedRegions || row.ManagedRegions || '',
    name:
      row.name || row.Name || row.departmentName || row.DepartmentName || '',
    parentId:
      row.parentId ||
      row.ParentId ||
      row.parentDepartmentId ||
      row.ParentDepartmentId ||
      '',
  };
}

function mapAssignment(row: any): SystemApi.Assignment {
  return {
    appId: row.appId || row.AppId || '',
    departmentId: row.departmentId || row.DepartmentId || '',
    description: row.description || row.Description || '',
    id: String(row.id || row.Id || ''),
    name: row.name || row.Name || '',
  };
}

function mapDictionaryType(row: any): SystemApi.DictionaryType {
  const id = String(row.id || row.Id || row.code || row.Code || '');
  const code = row.code || row.Code || '';
  if (id && code) {
    dictionaryCodeById.set(id, code);
  }

  return {
    code,
    description: row.description || row.Description || '',
    id,
    meta: row.meta || row.Meta || '',
    name: row.label || row.Label || '',
    value: row.value || row.Value || code,
  };
}

function mapDictionaryItem(row: any): SystemApi.DictionaryItem {
  const id = String(row.id || row.Id || '');
  const dictCode = row.parentId || row.ParentId || row.dictionaryTypeId || '';
  if (id && dictCode) {
    dictionaryCodeByItemId.set(id, dictCode);
  }

  return {
    code: row.code || row.Code || row.value || row.Value || '',
    description: row.description || row.Description || '',
    dictionaryTypeId:
      row.parentId || row.ParentId || row.dictionaryTypeId || '',
    id,
    meta: row.meta || row.Meta || '',
    name: row.label || row.Label || '',
    value: row.value || row.Value || row.code || row.Code || '',
  };
}

function mapConfiguration(row: any): SystemApi.Configuration {
  return {
    description: row.description || row.remark || '',
    id: String(row.id || ''),
    key: row.key || row.configKey || '',
    status: row.status ?? 1,
    value: row.value || row.configValue || '',
  };
}

export function listSystemUsersApi(
  params?: SystemApi.ManagementQuery,
): Promise<SystemApi.SystemPage<SystemApi.User>> {
  return requestClient
    .post<PageResult<any[]> | any[]>(
      `${SYSTEM_SERVICE_PATH}/user/query`,
      toSystemQueryBody(params),
    )
    .then((result) => mapPage(result, mapUser));
}

export function saveSystemUserApi(
  data: Partial<SystemApi.User> & { password?: string },
) {
  const payload = {
    Account: data.account,
    AppUserId: data.appUserId,
    AssignmentIds: (data.assignmentIds || []).join(','),
    DepartmentIds: (data.departmentIds || []).join(','),
    Email: data.email,
    Id: data.id,
    IdCardNo: data.idCardNo,
    Password: data.password,
    PhoneNumber: data.phoneNumber,
    RoleIds: (data.roleIds || []).join(','),
    UserName: data.userName,
  };
  const action = requestClient.post(
    `${SYSTEM_SERVICE_PATH}/user/save`,
    payload,
  );
  return action.then(() => mapUser(data));
}

export function deleteSystemUserApi(id: string) {
  return requestClient
    .post(`${SYSTEM_SERVICE_PATH}/user/remove/${id}`)
    .then(() => true);
}

export function listSystemRolesApi(
  params?: SystemApi.ManagementQuery,
): Promise<SystemApi.SystemPage<SystemApi.Role>> {
  return requestClient
    .post<PageResult<any[]> | any[]>(
      `${SYSTEM_SERVICE_PATH}/role/query`,
      toSystemQueryBody(params),
    )
    .then((result) => mapPage(result, mapRole));
}

export function saveSystemRoleApi(data: Partial<SystemApi.Role>) {
  const payload = {
    AppId: data.appId || getCurrentAppId(),
    Description: data.description,
    Id: data.id,
    RoleName: data.roleName,
    TargetId: data.targetId,
  };
  const action = requestClient.post(
    `${SYSTEM_SERVICE_PATH}/role/save`,
    payload,
  );
  return action.then(() => mapRole(data));
}

export function saveSystemRoleMenusApi(roleId: string, menuIds: string[]) {
  return requestClient
    .post(`${SYSTEM_SERVICE_PATH}/role/batch-change-menu`, {
      MenuIds: menuIds,
      RoleId: roleId,
    })
    .then(() => true);
}

export function listSystemRoleMenusApi(
  roleId: string,
): Promise<SystemApi.Menu[]> {
  return requestClient
    .get<any[]>(`${SYSTEM_SERVICE_PATH}/role/role-menu/${roleId}`)
    .then((rows) => rows.map(mapMenu));
}

export function deleteSystemRoleApi(id: string) {
  return requestClient
    .delete(`${SYSTEM_SERVICE_PATH}/role/remove/${id}`)
    .then(() => true);
}

export function listSystemMenusApi(
  params?: SystemApi.ManagementQuery,
): Promise<SystemApi.SystemPage<SystemApi.Menu>> {
  return requestClient
    .post<PageResult<any[]> | any[]>(
      `${SYSTEM_SERVICE_PATH}/menu/query`,
      toSystemQueryBody(params),
    )
    .then((result) => mapPage(result, mapMenu));
}

export function saveSystemMenuApi(data: Partial<SystemApi.Menu>) {
  return requestClient
    .post<boolean>(`${SYSTEM_SERVICE_PATH}/menu/save`, {
      AppId: data.appId || getCurrentAppId(),
      Authority: data.authority,
      Component: data.component,
      Hide: data.hide ?? 0,
      Icon: data.icon,
      Id: data.id,
      ParentId: data.parentId,
      Path: data.path,
      SortNumber: data.sortNumber,
      Title: data.title,
      Type: data.type ?? 1,
    })
    .then(() => data as SystemApi.Menu);
}

export function deleteSystemMenuApi(id: string) {
  return requestClient.delete<boolean>(
    `${SYSTEM_SERVICE_PATH}/menu/remove/${id}`,
  );
}

export function listSystemConfigurationsApi(
  params?: SystemApi.ManagementQuery,
): Promise<SystemApi.SystemPage<SystemApi.Configuration>> {
  return getPage<any>('/v1/config/page', params).then((result) => ({
    list: result.list.map(mapConfiguration),
    total: result.total,
  }));
}

export function saveSystemConfigurationApi(
  data: Partial<SystemApi.Configuration>,
) {
  const payload = {
    configKey: data.key,
    configName: data.key,
    configValue: data.value,
    id: data.id,
    remark: data.description,
  };
  const action = data.id
    ? requestClient.put(`/v1/config/${data.id}`, payload)
    : requestClient.post('/v1/config', payload);
  return action.then(() => mapConfiguration(data));
}

export function deleteSystemConfigurationApi(id: string) {
  return requestClient.delete(`/v1/config/${id}`).then(() => true);
}

export function listUserGroupsApi() {
  return requestClient.get<SystemApi.CodeName[]>('/system/user-groups');
}

export function saveUserGroupApi(data: Partial<SystemApi.CodeName>) {
  return requestClient.post<SystemApi.CodeName>('/system/user-groups', data);
}

export function deleteUserGroupApi(id: string) {
  return requestClient.delete<boolean>(`/system/user-groups/${id}`);
}

export function listRoleGroupsApi() {
  return requestClient.get<SystemApi.CodeName[]>('/system/role-groups');
}

export function saveRoleGroupApi(data: Partial<SystemApi.CodeName>) {
  return requestClient.post<SystemApi.CodeName>('/system/role-groups', data);
}

export function deleteRoleGroupApi(id: string) {
  return requestClient.delete<boolean>(`/system/role-groups/${id}`);
}

export function listDepartmentsApi(
  params?: SystemApi.ManagementQuery,
): Promise<SystemApi.SystemPage<SystemApi.Department>> {
  return requestClient
    .post<any[] | PageResult<any[]>>(
      `${SYSTEM_SERVICE_PATH}/department/query`,
      toSystemQueryBody(params),
    )
    .then((result) => mapPage(result, mapDepartment));
}

export function saveDepartmentApi(data: Partial<SystemApi.Department>) {
  const payload = {
    AppId: getCurrentAppId(),
    DepartmentCode: data.code,
    DepartmentName: data.name,
    Description: data.description,
    Id: data.id,
    ManagedAppIds: data.managedAppIds || '',
    ManagedRegions: data.managedRegions || '',
    ParentDepartmentId: data.parentId || '',
  };
  const action = requestClient.post(
    `${SYSTEM_SERVICE_PATH}/department/save`,
    payload,
  );
  return action.then(() => mapDepartment(data));
}

export function deleteDepartmentApi(id: string) {
  return requestClient
    .delete(`${SYSTEM_SERVICE_PATH}/department/remove/${id}`)
    .then(() => true);
}

/**
 * zh-cn: 查询当前账号可管理的应用。接口返回分页对象时统一提取列表，避免页面依赖后端的分页包装细节。
 * en-us: Lists applications available to the current account. Page wrappers are normalized so callers receive a stable list.
 */
export function listApplicationsApi(
  params?: SystemApi.ManagementQuery,
): Promise<SystemApi.SystemPage<SystemApi.Application>> {
  return requestClient
    .post<PageResult<SystemApi.Application[]> | SystemApi.Application[]>(
      '/v1/security/system/app/query',
      toSystemQueryBody(params),
    )
    .then(pageResult);
}

/**
 * zh-cn: 保存普通应用注册信息。应用私钥用于平台解密该应用发送的载荷；平台响应密钥由服务端创建并通过应用初始化协议下发，管理页面不提交或回显该密钥。
 * en-us: Saves a regular application registration. The application private key lets the platform decrypt payloads from that application; the platform response key is generated server-side and delivered through the application initialization protocol, never submitted or echoed by this management page.
 *
 * @param data
 * zh-cn: 普通应用的可维护注册字段，其中 appPrivateKey 为必填项。
 * en-us: Maintainable registration fields for a regular application, where appPrivateKey is required.
 */
export function saveApplicationApi(data: Partial<SystemApi.Application>) {
  return requestClient
    .post<boolean>(`${SYSTEM_SERVICE_PATH}/app/save`, {
      AppEncryptType: data.appEncryptType ?? 0,
      AppId: data.appId,
      AppName: data.appName,
      AppPrivateKey: data.appPrivateKey,
      AppRedirectUrl: data.appRedirectUrl,
      Description: data.description,
      EnableMFA: data.enableMFA ?? 0,
      Id: data.id,
      IsCommonApp: data.isCommonApp ?? 0,
      IsEnable: data.isEnable ?? 1,
      Logo: data.logo,
    })
    .then(() => data as SystemApi.Application);
}

export function deleteApplicationApi(appId: string) {
  return requestClient.delete<boolean>(
    `${SYSTEM_SERVICE_PATH}/app/remove/${appId}`,
  );
}

/**
 * zh-cn: 查询当前应用范围内的区域树。请求使用空查询对象，返回值保留后端树结构供页面展开或扁平展示。
 * en-us: Loads the region tree in the current application scope. The tree response is preserved for expanded or flattened views.
 */
export function listRegionsApi(params?: SystemApi.ManagementQuery) {
  return requestClient.post<SystemApi.Region[]>(
    '/v1/security/system/region/query',
    toSystemQueryBody(params),
  );
}

export function saveRegionApi(data: Partial<SystemApi.Region>) {
  const payload = {
    AppId: data.appId || getCurrentAppId(),
    Code: data.code,
    Description: data.description,
    Id: data.id,
    LngAndSat: data.lngAndSat || '',
    Name: data.name,
    ParentId: data.parentId || null,
    ParentRegionId: data.parentRegionId || '',
    Type: data.type ?? 0,
  };
  return requestClient
    .post<boolean>(`${SYSTEM_SERVICE_PATH}/region/save`, payload)
    .then(() => data as SystemApi.Region);
}

export function deleteRegionApi(id: string) {
  return requestClient.delete<boolean>(
    `${SYSTEM_SERVICE_PATH}/region/remove/${id}`,
  );
}

export function listAssignmentsApi(
  params?: SystemApi.ManagementQuery,
): Promise<SystemApi.SystemPage<SystemApi.Assignment>> {
  return requestClient
    .post<PageResult<any[]> | any[]>(
      `${SYSTEM_SERVICE_PATH}/assignment/query`,
      toSystemQueryBody(params),
    )
    .then((result) => mapPage(result, mapAssignment));
}

export function saveAssignmentApi(data: Partial<SystemApi.Assignment>) {
  return requestClient
    .post<boolean>(`${SYSTEM_SERVICE_PATH}/assignment/save`, {
      AppId: data.appId || getCurrentAppId(),
      DepartmentId: data.departmentId,
      Description: data.description,
      Id: data.id,
      Name: data.name,
    })
    .then(() => data as SystemApi.Assignment);
}

export function deleteAssignmentApi(id: string) {
  return requestClient.delete<boolean>(
    `${SYSTEM_SERVICE_PATH}/assignment/remove/${id}`,
  );
}

export function listDictionaryTypesApi(
  params?: SystemApi.ManagementQuery,
): Promise<SystemApi.SystemPage<SystemApi.DictionaryType>> {
  return requestClient
    .post<PageResult<any[]> | any[]>(`${SYSTEM_SERVICE_PATH}/dict/query-base`, {
      ...toSystemQueryBody(params),
      Code: params?.keyword,
      Label: params?.keyword,
    })
    .then((result) => mapPage(result, mapDictionaryType));
}

export function saveDictionaryTypeApi(data: Partial<SystemApi.DictionaryType>) {
  const payload = {
    Code: data.code,
    Description: data.description,
    Id: data.id,
    Label: data.name,
    Meta: data.meta,
    ParentId: null,
    Value: data.value || data.code,
  };
  const action = requestClient.post(
    `${SYSTEM_SERVICE_PATH}/dict/save`,
    payload,
  );
  return action.then(() => mapDictionaryType(data));
}

export function deleteDictionaryTypeApi(id: string) {
  return requestClient
    .delete(`${SYSTEM_SERVICE_PATH}/dict/remove/${id}`)
    .then(() => true);
}

export function listDictionaryItemsApi(
  dictionaryTypeId?: string,
  params?: SystemApi.ManagementQuery,
): Promise<SystemApi.SystemPage<SystemApi.DictionaryItem>> {
  const parentId = dictionaryTypeId || '';
  if (!parentId) {
    return Promise.resolve({ list: [], total: 0 });
  }
  return requestClient
    .post<PageResult<Record<string, any>[]> | Record<string, any>[]>(
      `${SYSTEM_SERVICE_PATH}/dict/query`,
      {
        ...toSystemQueryBody(params),
        Code: params?.keyword,
        Label: params?.keyword,
        ParentId: parentId,
      },
    )
    .then((result) =>
      mapPage(result, (item) =>
        mapDictionaryItem({ ...item, dictionaryTypeId }),
      ),
    );
}

export function saveDictionaryItemApi(data: Partial<SystemApi.DictionaryItem>) {
  const payload = {
    Code: data.code,
    Description: data.description,
    Id: data.id,
    Label: data.name,
    Meta: data.meta,
    ParentId: data.dictionaryTypeId,
    Value: data.value || data.code,
  };
  return requestClient
    .post(`${SYSTEM_SERVICE_PATH}/dict/save`, payload)
    .then(() => mapDictionaryItem(data));
}

export function deleteDictionaryItemApi(id: string) {
  return requestClient
    .delete(`${SYSTEM_SERVICE_PATH}/dict/remove/${id}`)
    .then(() => true);
}

export function listAssociationsApi() {
  return requestClient.get<SystemApi.Association[]>('/system/associations');
}

export function createAssociationApi(data: Omit<SystemApi.Association, 'id'>) {
  return requestClient.post<SystemApi.Association>(
    '/system/associations',
    data,
  );
}

export function deleteAssociationApi(id: string) {
  return requestClient.delete<boolean>(`/system/associations/${id}`);
}
