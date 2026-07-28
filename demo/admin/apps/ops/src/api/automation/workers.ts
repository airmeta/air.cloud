import { requestClient } from '#/api/request';

export namespace AutomationApi {
  export type WorkerStatus = 'active' | 'disabled' | 'idle' | 'inactive' | 'maintenance' | 'starting' | 'working';
  export type WorkerType = 'codex';
  export type EmployeeType = 'audit' | 'development' | 'operations' | 'product' | 'test';
  export type SessionStatus =
    | 'auth_required'
    | 'busy'
    | 'error'
    | 'expired'
    | 'idle'
    | 'offline'
    | 'starting';

  export type WorkerCommandType =
    | 'cancel_current_run'
    | 'reload_config'
    | 'smoke'
    | 'code_audit_index_sync'
    | 'start_bug'
    | 'start_task'
    | 'stop_after_current';

  export interface DigitalWorker {
    agentTokenId?: string;
    agentUserId: string;
    aiPlatformCode: string;
    backendTechCapabilities: string;
    code: string;
    createTime: string;
    createdBy: string;
    description?: string;
    endpointIds: string[];
    employeeType: EmployeeType;
    heartbeatTimeoutSeconds: number;
    idleMaxIntervalSeconds: number;
    codexHome: string;
    codexModel: string;
    codexProvider: string;
    configVersion: number;
    id: string;
    maxConcurrentRuns: number;
    maxRunMinutes: number;
    name: string;
    openAiBaseUrl?: string;
    pollIntervalSeconds: number;
    projectIds: string[];
    runSmokeOnStartup: boolean;
    runtimeProfile: string;
    runsRoot: string;
    sandboxMode: string;
    skillIds: string[];
    smokePrompt?: string;
    status: WorkerStatus;
    updateTime?: string;
    workspaceRoot: string;
    workerType: WorkerType;
    latestHeartbeatAt?: string;
    latestSessionStatus?: string;
    runtimeSummary?: string;
  }

  export interface DigitalWorkerDetail {
    currentRun?: WorkerRun;
    latestSession?: WorkerSession;
    pendingCommands: WorkerCommand[];
    startupProbeResults?: StartupProbeResult[];
    worker: DigitalWorker;
  }

  export interface DigitalWorkerInstallRender {
    createTime: string;
    id: string;
    placeholderValuesJson: string;
    plainSecretEnabled: boolean;
    renderedCompose: string;
    renderedEnv?: string;
    templateId: string;
    templateVersion: number;
    workerId: string;
  }

  export interface DigitalWorkerDeployTemplate {
    backendTechCapabilities: string;
    code: string;
    composeTemplate: string;
    createTime: string;
    description?: string;
    dockerfileExtension?: string;
    id: string;
    name: string;
    runtimeProfile: string;
    sort: number;
    status: number;
    updateTime?: string;
    version: number;
  }

  export interface SaveDigitalWorkerDeployTemplateRequest {
    backendTechCapabilities: string;
    code: string;
    composeTemplate: string;
    description?: string;
    dockerfileExtension?: string;
    name: string;
    runtimeProfile: string;
    sort?: number;
    status?: number;
    version?: number;
  }

  export interface StartupProbeResult {
    command: string;
    createTime: string;
    error?: string;
    exitCode?: number;
    id: string;
    instanceId: string;
    passed: boolean;
    probeCode: string;
    probeConfigId: string;
    probeName: string;
    reportedAt: string;
    required: boolean;
    sessionId?: string;
    stderr?: string;
    stdout?: string;
    workerDeployRenderId?: string;
    workerId: string;
  }

  export interface SaveDigitalWorkerRequest {
    agentTokenId?: string;
    agentUserId: string;
    aiPlatformCode?: string;
    code?: string;
    description?: string;
    endpointIds?: string[];
    employeeType?: EmployeeType;
    heartbeatTimeoutSeconds?: number;
    idleMaxIntervalSeconds?: number;
    codexHome?: string;
    codexModel?: string;
    codexProvider?: string;
    maxConcurrentRuns?: number;
    maxRunMinutes?: number;
    name: string;
    openAiBaseUrl?: string;
    pollIntervalSeconds?: number;
    projectIds?: string[];
    runSmokeOnStartup?: boolean;
    runsRoot?: string;
    sandboxMode?: string;
    skillIds?: string[];
    smokePrompt?: string;
    status?: WorkerStatus;
    workspaceRoot?: string;
    workerType?: WorkerType;
  }

  export interface WorkerSession {
    codexHome?: string;
    codexVersion?: string;
    configTomlExists: boolean;
    containerId?: string;
    currentRunId?: string;
    dotnetVersion?: string;
    errorSummary?: string;
    gitVersion?: string;
    hostName?: string;
    id: string;
    instanceId: string;
    lastHeartbeatAt?: string;
    nodeVersion?: string;
    runsRoot?: string;
    startedAt: string;
    status: SessionStatus;
    stoppedAt?: string;
    workerId: string;
    workspaceRoot?: string;
  }

  export interface WorkerCommand {
    ackedAt?: string;
    changedFilesJson?: string;
    commandType: WorkerCommandType;
    completedAt?: string;
    createTime: string;
    createdBy: string;
    error?: string;
    expiresAt?: string;
    gitCommitId?: string;
    id: string;
    payloadJson?: string;
    resultJson?: string;
    sessionId?: string;
    startedAt?: string;
    status: string;
    title: string;
    workerId: string;
  }

  export interface WorkerRun {
    commandId?: string;
    completedAt?: string;
    error?: string;
    exitCode?: number;
    finalPath?: string;
    id: string;
    manifestPath?: string;
    promptPath?: string;
    runType: string;
    sessionId: string;
    startedAt: string;
    status: string;
    stderrPath?: string;
    stdoutPath?: string;
    targetId?: string;
    targetType?: string;
    timedOut: boolean;
    workerId: string;
    workspacePath?: string;
  }

  export interface WorkerEvent {
    createTime: string;
    eventType: string;
    id: string;
    level: string;
    message: string;
    payloadJson?: string;
    runId?: string;
    sessionId?: string;
    workerId: string;
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

export function listDigitalWorkersApi(params?: {
  keyword?: string;
  status?: string;
  workerType?: string;
}) {
  return requestClient.get<AutomationApi.DigitalWorker[]>('/workers', {
    params: normalizeQuery(params),
  });
}

export function createDigitalWorkerApi(data: AutomationApi.SaveDigitalWorkerRequest) {
  return requestClient.post<AutomationApi.DigitalWorker>('/workers', data);
}

export function updateDigitalWorkerApi(id: string, data: AutomationApi.SaveDigitalWorkerRequest) {
  return requestClient.put<AutomationApi.DigitalWorker>(`/workers/${id}`, data);
}

export function getDigitalWorkerDetailApi(id: string) {
  return requestClient.get<AutomationApi.DigitalWorkerDetail>(`/workers/${id}/detail`);
}

export function setDigitalWorkerStatusApi(id: string, status: AutomationApi.WorkerStatus) {
  return requestClient.post<AutomationApi.DigitalWorker>(`/workers/${id}/status`, { status });
}

export function generateDigitalWorkerInstallApi(
  id: string,
  data: { apiBaseUrl?: string; plainSecretEnabled?: boolean; templateId?: string },
) {
  return requestClient.post<AutomationApi.DigitalWorkerInstallRender>(`/workers/${id}/install`, data);
}

export function listDigitalWorkerDeployTemplatesApi(params?: { keyword?: string; status?: string }) {
  return requestClient.get<AutomationApi.DigitalWorkerDeployTemplate[]>('/workers/deploy-templates', {
    params: normalizeQuery(params),
  });
}

export function createDigitalWorkerDeployTemplateApi(data: AutomationApi.SaveDigitalWorkerDeployTemplateRequest) {
  return requestClient.post<AutomationApi.DigitalWorkerDeployTemplate>('/workers/deploy-templates', data);
}

export function updateDigitalWorkerDeployTemplateApi(
  id: string,
  data: AutomationApi.SaveDigitalWorkerDeployTemplateRequest,
) {
  return requestClient.put<AutomationApi.DigitalWorkerDeployTemplate>(`/workers/deploy-templates/${id}`, data);
}

export function deleteDigitalWorkerApi(id: string) {
  return requestClient.delete<AutomationApi.DigitalWorker>(`/workers/${id}`);
}

export function listStartupProbeResultsApi(id: string) {
  return requestClient.get<AutomationApi.StartupProbeResult[]>(`/workers/${id}/startup-probes`);
}

export function createWorkerCommandApi(data: {
  commandType: AutomationApi.WorkerCommandType;
  expiresAt?: string;
  payloadJson?: string;
  sessionId?: string;
  title?: string;
  workerId: string;
}) {
  return requestClient.post<AutomationApi.WorkerCommand>('/workers/commands', data);
}

export function listWorkerCommandsApi(params?: {
  commandType?: string;
  sessionId?: string;
  status?: string;
  workerId?: string;
}) {
  return requestClient.get<AutomationApi.WorkerCommand[]>('/workers/commands', {
    params: normalizeQuery(params),
  });
}

export function replayWorkerCommandApi(commandId: string) {
  return requestClient.post<AutomationApi.WorkerCommand>(`/workers/commands/${commandId}/replay`);
}

export function listWorkerSessionsApi(params?: { status?: string; workerId?: string }) {
  return requestClient.get<AutomationApi.WorkerSession[]>('/workers/sessions', {
    params: normalizeQuery(params),
  });
}

export function listWorkerRunsApi(params?: {
  sessionId?: string;
  status?: string;
  targetId?: string;
  targetType?: string;
  workerId?: string;
}) {
  return requestClient.get<AutomationApi.WorkerRun[]>('/workers/runs', {
    params: normalizeQuery(params),
  });
}

export function listWorkerEventsApi(params?: {
  eventType?: string;
  runId?: string;
  sessionId?: string;
  workerId?: string;
}) {
  return requestClient.get<AutomationApi.WorkerEvent[]>('/workers/events', {
    params: normalizeQuery(params),
  });
}
