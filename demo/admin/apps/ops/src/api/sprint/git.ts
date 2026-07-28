import { requestClient } from '#/api/request';

export namespace SprintGitApi {
  export interface GitAccount {
    code: string;
    commitAuthorEmail?: string;
    commitAuthorName?: string;
    createTime: string;
    createdBy: string;
    description?: string;
    hasAccessToken: boolean;
    id: string;
    name: string;
    status: string;
    username: string;
  }

  export interface SaveGitAccountRequest {
    accessToken?: string;
    commitAuthorEmail?: string;
    commitAuthorName?: string;
    description?: string;
    name: string;
    status?: string;
    username: string;
  }

  export interface GitRepository {
    code: string;
    createTime: string;
    createdBy: string;
    defaultBranch?: string;
    description?: string;
    gitAccountId?: string;
    id: string;
    localPath?: string;
    name: string;
    repositoryUrl: string;
    status: string;
  }

  export interface SaveGitRepositoryRequest {
    defaultBranch?: string;
    description?: string;
    gitAccountId?: string;
    localPath?: string;
    name: string;
    repositoryUrl: string;
    status?: string;
  }

  export interface BranchOperation {
    accountId?: string;
    backupBranch?: string;
    branchName: string;
    commitHash?: string;
    commitMessage?: string;
    createTime: string;
    createdBy: string;
    id: string;
    message?: string;
    operationType: string;
    pushedAt?: string;
    repositoryId: string;
    sourceBranch?: string;
    status: string;
  }

  export interface CreateBranchRequest {
    branchName: string;
    sourceBranch?: string;
  }

  export interface DeleteBranchRequest {
    backupBranch?: string;
    branchName: string;
  }
}

export function createGitAccountApi(data: SprintGitApi.SaveGitAccountRequest) {
  return requestClient.post<SprintGitApi.GitAccount>('/git/accounts', data);
}

export function listGitAccountsApi(params?: { keyword?: string; status?: string }) {
  return requestClient.get<SprintGitApi.GitAccount[]>('/git/accounts', { params });
}

export function updateGitAccountApi(id: string, data: SprintGitApi.SaveGitAccountRequest) {
  return requestClient.put<SprintGitApi.GitAccount>(`/git/accounts/${id}`, data);
}

export function createGitRepositoryApi(data: SprintGitApi.SaveGitRepositoryRequest) {
  return requestClient.post<SprintGitApi.GitRepository>('/git/repositories', data);
}

export function listGitRepositoriesApi(params?: {
  gitAccountId?: string;
  keyword?: string;
  status?: string;
}) {
  return requestClient.get<SprintGitApi.GitRepository[]>('/git/repositories', { params });
}

export function updateGitRepositoryApi(
  id: string,
  data: SprintGitApi.SaveGitRepositoryRequest,
) {
  return requestClient.put<SprintGitApi.GitRepository>(`/git/repositories/${id}`, data);
}

export function createGitBranchApi(id: string, data: SprintGitApi.CreateBranchRequest) {
  return requestClient.post<SprintGitApi.BranchOperation>(
    `/git/repositories/${id}/branches`,
    data,
  );
}

export function deleteGitBranchApi(id: string, data: SprintGitApi.DeleteBranchRequest) {
  return requestClient.post<SprintGitApi.BranchOperation>(
    `/git/repositories/${id}/branches/delete`,
    data,
  );
}

export function readGitPushRecordsApi(id: string, branch?: string) {
  return requestClient.get<SprintGitApi.BranchOperation[]>(
    `/git/repositories/${id}/push-records`,
    { params: { branch } },
  );
}
