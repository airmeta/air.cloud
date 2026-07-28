import { requestClient } from '#/api/request';

export namespace SystemApi {
  export type ManagementQuery = Record<string, number | string | undefined>;

  export interface Association {
    associationType: string;
    id: string;
    sourceEntityId: string;
    targetEntityId: string;
  }

  export interface User {
    avatar?: string;
    displayName: string;
    email?: string;
    id: string;
    phoneNumber?: string;
    roleIds: string[];
    status: number;
    username: string;
  }

  export interface Role {
    code: string;
    description?: string;
    id: string;
    menuIds: string[];
    name: string;
    permissionIds: string[];
    status: number;
  }

  export interface Menu {
    component?: string;
    icon?: string;
    id: string;
    name: string;
    parentId?: string;
    path: string;
    sort: number;
    status: number;
    type: number;
  }

  export interface Permission {
    code: string;
    id: string;
    menuId?: string;
    name: string;
  }

  export interface AgentToken {
    createTime: string;
    createdBy: string;
    createdByUsername: string;
    expiresAt: string;
    id: string;
    lastUsedAt?: string;
    maskedToken: string;
    name: string;
    ownerDisplayName: string;
    ownerUserId: string;
    ownerUsername: string;
    projectId?: string;
    revokedAt?: string;
    revokedBy?: string;
    status: number;
    token: string;
  }

  export interface CreatedAgentToken {
    id: string;
    metadata: AgentToken;
    token: string;
  }

  export interface Configuration {
    description?: string;
    id: string;
    key: string;
    status: number;
    value: string;
  }

  export interface AiPlatform {
    apiKey?: string;
    code: string;
    description?: string;
    hasApiKey: boolean;
    id: string;
    model: string;
    name: string;
    openAiBaseUrl?: string;
    provider: string;
    sort: number;
    status: number;
  }

  export interface StartAiConversationRequest {
    aiPlatformCode?: string;
    bugId?: string;
    message: string;
    projectId?: string;
    requirementId?: string;
    taskId?: string;
    testPlanId?: string;
    title?: string;
  }

  export interface AiConversation {
    aiPlatformCode: string;
    assistantMessage?: string;
    bugId?: string;
    completedAt?: string;
    contextSnapshot: string;
    createdBy: string;
    errorMessage?: string;
    id: string;
    model: string;
    projectId?: string;
    provider: string;
    requirementId?: string;
    startedAt: string;
    status: string;
    taskId?: string;
    testPlanId?: string;
    title: string;
    userMessage: string;
  }

  export interface CodeName {
    code: string;
    description?: string;
    id: string;
    name: string;
    status: number;
  }

  export interface Department {
    code: string;
    id: string;
    name: string;
    parentId?: string;
    sort: number;
    status: number;
  }

  export interface DictionaryType {
    code: string;
    description?: string;
    id: string;
    name: string;
    sort: number;
    status: number;
  }

  export interface DictionaryItem {
    code: string;
    description?: string;
    dictionaryTypeId: string;
    id: string;
    name: string;
    sort: number;
    status: number;
  }

  export interface RuntimeEnvironment {
    apiBaseUrl?: string;
    code: string;
    composeFilePath?: string;
    deployRoot?: string;
    description?: string;
    dockerDirectory?: string;
    endpointId?: string;
    environmentType: string;
    frontendProxyApiUrl?: string;
    frontendUrl?: string;
    id: string;
    localPackagePaths?: string;
    mcpEndpoint?: string;
    moduleId?: string;
    name: string;
    projectId?: string;
    remotePackagePath?: string;
    serverIps?: string;
    sort: number;
    status: number;
  }

  export interface RuntimeEnvironmentContainer {
    containerPort: number;
    containerType: number;
    deployScript?: string;
    description?: string;
    hostPort: number;
    id: string;
    name: string;
    prompt?: string;
    protocol: string;
    runtimeEnvironmentId: string;
    serverIp?: string;
    sort: number;
    status: number;
  }

  export interface PromptTemplate {
    agentEnvironment: string;
    code: string;
    content: string;
    description?: string;
    id: string;
    name: string;
    sort: number;
    status: number;
  }
}

function normalizeQuery(params?: SystemApi.ManagementQuery) {
  if (!params) {
    return undefined;
  }

  return Object.fromEntries(
    Object.entries(params).filter(([, value]) => value !== undefined && value !== ''),
  );
}

export function listSystemUsersApi(params?: SystemApi.ManagementQuery) {
  return requestClient.get<SystemApi.User[]>('/system/users', {
    params: normalizeQuery(params),
  });
}

export function saveSystemUserApi(data: Partial<SystemApi.User> & { password?: string }) {
  return requestClient.post<SystemApi.User>('/system/users', data);
}

export function deleteSystemUserApi(id: string) {
  return requestClient.delete<boolean>(`/system/users/${id}`);
}

export function listSystemRolesApi(params?: SystemApi.ManagementQuery) {
  return requestClient.get<SystemApi.Role[]>('/system/roles', {
    params: normalizeQuery(params),
  });
}

export function saveSystemRoleApi(data: Partial<SystemApi.Role>) {
  return requestClient.post<SystemApi.Role>('/system/roles', data);
}

export function deleteSystemRoleApi(id: string) {
  return requestClient.delete<boolean>(`/system/roles/${id}`);
}

export function listSystemMenusApi(params?: SystemApi.ManagementQuery) {
  return requestClient.get<SystemApi.Menu[]>('/system/menus', {
    params: normalizeQuery(params),
  });
}

export function saveSystemMenuApi(data: Partial<SystemApi.Menu>) {
  return requestClient.post<SystemApi.Menu>('/system/menus', data);
}

export function deleteSystemMenuApi(id: string) {
  return requestClient.delete<boolean>(`/system/menus/${id}`);
}

export function listSystemPermissionsApi(params?: SystemApi.ManagementQuery) {
  return requestClient.get<SystemApi.Permission[]>('/system/permissions', {
    params: normalizeQuery(params),
  });
}

export function saveSystemPermissionApi(data: Partial<SystemApi.Permission>) {
  return requestClient.post<SystemApi.Permission>('/system/permissions', data);
}

export function deleteSystemPermissionApi(id: string) {
  return requestClient.delete<boolean>(`/system/permissions/${id}`);
}

export function listAgentTokensApi(params?: SystemApi.ManagementQuery) {
  return requestClient.get<SystemApi.AgentToken[]>('/system/agent-tokens', {
    params: normalizeQuery(params),
  });
}

export function createAgentTokenApi(data: {
  expiresAt: string;
  name: string;
  ownerUserId?: string;
  projectId?: string;
}) {
  return requestClient.post<SystemApi.CreatedAgentToken>('/system/agent-tokens', data);
}

export function revokeAgentTokenApi(id: string) {
  return requestClient.delete<boolean>(`/system/agent-tokens/${id}`);
}

export function listSystemConfigurationsApi(params?: SystemApi.ManagementQuery) {
  return requestClient.get<SystemApi.Configuration[]>('/system/configurations', {
    params: normalizeQuery(params),
  });
}

export function saveSystemConfigurationApi(data: Partial<SystemApi.Configuration>) {
  return requestClient.post<SystemApi.Configuration>('/system/configurations', data);
}

export function deleteSystemConfigurationApi(id: string) {
  return requestClient.delete<boolean>(`/system/configurations/${id}`);
}

export function listAiPlatformsApi(params?: SystemApi.ManagementQuery) {
  return requestClient.get<SystemApi.AiPlatform[]>('/system/ai-platforms', {
    params: normalizeQuery(params),
  });
}

export function saveAiPlatformApi(data: Partial<SystemApi.AiPlatform>) {
  return requestClient.post<SystemApi.AiPlatform>('/system/ai-platforms', data);
}

export function deleteAiPlatformApi(id: string) {
  return requestClient.delete<boolean>(`/system/ai-platforms/${id}`);
}

export function listAiConversationsApi(params?: SystemApi.ManagementQuery) {
  return requestClient.get<SystemApi.AiConversation[]>('/system/ai-conversations', {
    params: normalizeQuery(params),
  });
}

export function startAiConversationApi(data: SystemApi.StartAiConversationRequest) {
  return requestClient.post<SystemApi.AiConversation>('/system/ai-conversations', data);
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

export function listDepartmentsApi(params?: SystemApi.ManagementQuery) {
  return requestClient.get<SystemApi.Department[]>('/system/departments', {
    params: normalizeQuery(params),
  });
}

export function saveDepartmentApi(data: Partial<SystemApi.Department>) {
  return requestClient.post<SystemApi.Department>('/system/departments', data);
}

export function deleteDepartmentApi(id: string) {
  return requestClient.delete<boolean>(`/system/departments/${id}`);
}

export function listAssignmentsApi(params?: SystemApi.ManagementQuery) {
  return requestClient.get<SystemApi.CodeName[]>('/system/assignments', {
    params: normalizeQuery(params),
  });
}

export function saveAssignmentApi(data: Partial<SystemApi.CodeName>) {
  return requestClient.post<SystemApi.CodeName>('/system/assignments', data);
}

export function deleteAssignmentApi(id: string) {
  return requestClient.delete<boolean>(`/system/assignments/${id}`);
}

export function listDictionaryTypesApi(params?: SystemApi.ManagementQuery) {
  return requestClient.get<SystemApi.DictionaryType[]>('/system/dictionary-types', {
    params: normalizeQuery(params),
  });
}

export function saveDictionaryTypeApi(data: Partial<SystemApi.DictionaryType>) {
  return requestClient.post<SystemApi.DictionaryType>('/system/dictionary-types', data);
}

export function deleteDictionaryTypeApi(id: string) {
  return requestClient.delete<boolean>(`/system/dictionary-types/${id}`);
}

export function listDictionaryItemsApi(dictionaryTypeId?: string, params?: SystemApi.ManagementQuery) {
  return requestClient.get<SystemApi.DictionaryItem[]>('/system/dictionary-items', {
    params: normalizeQuery({ ...params, dictionaryTypeId }),
  });
}

export function saveDictionaryItemApi(data: Partial<SystemApi.DictionaryItem>) {
  return requestClient.post<SystemApi.DictionaryItem>('/system/dictionary-items', data);
}

export function deleteDictionaryItemApi(id: string) {
  return requestClient.delete<boolean>(`/system/dictionary-items/${id}`);
}

export function listRuntimeEnvironmentsApi(params?: {
  endpointId?: string;
  moduleId?: string;
  projectId?: string;
}) {
  return requestClient.get<SystemApi.RuntimeEnvironment[]>('/system/runtime-environments', {
    params,
  });
}

export function saveRuntimeEnvironmentApi(data: Partial<SystemApi.RuntimeEnvironment>) {
  return requestClient.post<SystemApi.RuntimeEnvironment>('/system/runtime-environments', data);
}

export function deleteRuntimeEnvironmentApi(id: string) {
  return requestClient.delete<boolean>(`/system/runtime-environments/${id}`);
}

export function listRuntimeEnvironmentContainersApi(runtimeEnvironmentId: string) {
  return requestClient.get<SystemApi.RuntimeEnvironmentContainer[]>('/system/runtime-environment-containers', {
    params: { runtimeEnvironmentId },
  });
}

export function saveRuntimeEnvironmentContainerApi(data: Partial<SystemApi.RuntimeEnvironmentContainer>) {
  return requestClient.post<SystemApi.RuntimeEnvironmentContainer>('/system/runtime-environment-containers', data);
}

export function deleteRuntimeEnvironmentContainerApi(id: string) {
  return requestClient.delete<boolean>(`/system/runtime-environment-containers/${id}`);
}

export function listPromptTemplatesApi(agentEnvironment?: string, params?: SystemApi.ManagementQuery) {
  return requestClient.get<SystemApi.PromptTemplate[]>('/system/prompt-templates', {
    params: normalizeQuery({ ...params, agentEnvironment }),
  });
}

export function savePromptTemplateApi(data: Partial<SystemApi.PromptTemplate>) {
  return requestClient.post<SystemApi.PromptTemplate>('/system/prompt-templates', data);
}

export function deletePromptTemplateApi(id: string) {
  return requestClient.delete<boolean>(`/system/prompt-templates/${id}`);
}

export function listAssociationsApi() {
  return requestClient.get<SystemApi.Association[]>('/system/associations');
}

export function createAssociationApi(data: Omit<SystemApi.Association, 'id'>) {
  return requestClient.post<SystemApi.Association>('/system/associations', data);
}

export function deleteAssociationApi(id: string) {
  return requestClient.delete<boolean>(`/system/associations/${id}`);
}
