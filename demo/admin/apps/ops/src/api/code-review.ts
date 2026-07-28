import { requestClient } from '#/api/request';
import type { AutomationApi } from '#/api/automation/workers';

export namespace CodeReviewApi {
  export type AuditTargetType =
    | 'development_task'
    | 'feature_description'
    | 'files'
    | 'folders'
    | 'release_preflight'
    | 'requirement_module';

  export type TaskStatus =
    | 'blocked'
    | 'cancelled'
    | 'failed'
    | 'needs_changes'
    | 'passed'
    | 'pending'
    | 'running';

  export interface CodeAuditTask {
    auditTargetType: AuditTargetType;
    baseCommitId?: string;
    branch: string;
    completedAt?: string;
    conclusion?: string;
    createTime: string;
    createdBy: string;
    currentBranchHeadCommitId?: string;
    gitRepositoryId: string;
    headCommitId?: string;
    id: string;
    instruction?: string;
    moduleId?: string;
    projectId: string;
    requirementId?: string;
    selectedSkillIds: string[];
    auditCommandId?: string;
    sourceCommandId?: string;
    sourceGitCommitId?: string;
    sourceRunId?: string;
    sourceTaskId?: string;
    startedAt?: string;
    status: TaskStatus;
    targetId?: string;
    updateTime?: string;
    workerId: string;
    workspaceDirtyReason?: string;
  }

  export interface CodeAuditResult {
    annotationIssuesJson?: string;
    auditTaskId: string;
    branch?: string;
    changedFilesJson?: string;
    conclusion?: string;
    createTime: string;
    gitCommitId?: string;
    id: string;
    issuesJson?: string;
    manualCheckItemsJson?: string;
    rawResult?: string;
    structuredResultJson?: string;
    updateTime?: string;
    workerCommandId?: string;
    workerRunId?: string;
  }

  export interface CodeAuditTaskDetail {
    result?: CodeAuditResult;
    task: CodeAuditTask;
  }

  export interface CodeAuditResultListItem {
    result?: CodeAuditResult;
    task: CodeAuditTask;
  }

  export interface CreateCodeAuditTaskRequest {
    auditTargetType: AuditTargetType;
    branch?: string;
    instruction?: string;
    projectId: string;
    scopeJson?: string;
    selectedSkillIds?: string[];
    targetId?: string;
    workerId: string;
  }

  export interface CodeAuditFile {
    auditStatus: 'abnormal' | 'deleted' | 'normal' | 'not_audited';
    blockingIssueCount: number;
    branch: string;
    createTime: string;
    fileContentHash?: string;
    filePath: string;
    fileType: string;
    gitRepositoryId: string;
    highIssueCount: number;
    id: string;
    issueCount: number;
    lastAuditAt?: string;
    lastAuditResultId?: string;
    lastAuditTaskId?: string;
    lastCommitId?: string;
    lowIssueCount: number;
    mediumIssueCount: number;
    projectId: string;
    summary?: string;
    updateTime?: string;
  }

  export interface CreateIndexSyncCommandRequest {
    branch?: string;
    projectId: string;
    workerId: string;
  }

  export interface ReleaseReport {
    auditTaskId: string;
    baseCommitId?: string;
    blockingIssueCount: number;
    blockingSummaries: string[];
    branch: string;
    canRelease: boolean;
    changedFileCount: number;
    completedAt?: string;
    conclusion?: string;
    currentBranchHeadCommitId?: string;
    gitCommitId?: string;
    gitRepositoryId: string;
    headCommitId?: string;
    highIssueCount: number;
    issueCount: number;
    lowIssueCount: number;
    manualCheckCount: number;
    manualCheckItems: string[];
    mediumIssueCount: number;
    projectId: string;
    status: TaskStatus;
  }
}

function normalizeQuery(params?: Record<string, string | undefined>) {
  if (!params) {
    return undefined;
  }

  return Object.fromEntries(
    Object.entries(params).filter(([, value]) => value !== undefined && value !== ''),
  );
}

export function listCodeAuditTasksApi(params?: {
  auditTargetType?: string;
  keyword?: string;
  projectId?: string;
  status?: string;
}) {
  return requestClient.get<CodeReviewApi.CodeAuditTask[]>('/code-audit/tasks', {
    params: normalizeQuery(params),
  });
}

export function createCodeAuditTaskApi(data: CodeReviewApi.CreateCodeAuditTaskRequest) {
  return requestClient.post<CodeReviewApi.CodeAuditTask>('/code-audit/tasks', data);
}

export function cancelCodeAuditTaskApi(id: string) {
  return requestClient.post<CodeReviewApi.CodeAuditTask>(`/code-audit/tasks/${id}/cancel`);
}

export function retryCodeAuditTaskApi(id: string) {
  return requestClient.post<CodeReviewApi.CodeAuditTask>(`/code-audit/tasks/${id}/retry`);
}

export function getCodeAuditTaskApi(id: string) {
  return requestClient.get<CodeReviewApi.CodeAuditTaskDetail>(`/code-audit/tasks/${id}`);
}

export function getCodeAuditResultApi(id: string) {
  return requestClient.get<CodeReviewApi.CodeAuditResult | undefined>(
    `/code-audit/tasks/${id}/result`,
  );
}

export function listCodeAuditResultsApi(params?: {
  keyword?: string;
  projectId?: string;
  status?: string;
}) {
  return requestClient.get<CodeReviewApi.CodeAuditResultListItem[]>('/code-audit/results', {
    params: normalizeQuery(params),
  });
}

export function listCodeAuditFilesApi(params?: {
  auditStatus?: string;
  branch?: string;
  fileType?: string;
  keyword?: string;
  projectId?: string;
}) {
  return requestClient.get<CodeReviewApi.CodeAuditFile[]>('/code-audit/files', {
    params: normalizeQuery(params),
  });
}

export function createCodeAuditIndexSyncCommandApi(
  data: CodeReviewApi.CreateIndexSyncCommandRequest,
) {
  return requestClient.post<AutomationApi.WorkerCommand>(
    '/code-audit/file-index/sync-commands',
    data,
  );
}

export function getCodeAuditReleaseReportApi(id: string) {
  return requestClient.get<CodeReviewApi.ReleaseReport>(
    `/code-audit/tasks/${id}/release-report`,
  );
}
