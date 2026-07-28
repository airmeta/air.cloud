import { requestClient } from '#/api/request';

export namespace SprintMvpApi {
  export interface CreateProjectRequest {
    architectId: string;
    aiPlatformCode: string;
    backendTechStack: string;
    code: string;
    description?: string;
    developerIds: string[];
    frontendTechStack: string;
    gitAccountId?: string;
    gitRepositoryId?: string;
    name: string;
    productManagerIds: string[];
    projectManagerId: string;
    testerIds: string[];
    testEnvironmentId?: string;
    testEnvironmentUrl?: string;
  }

  export interface UpdateProjectRequest {
    architectId: string;
    aiPlatformCode: string;
    backendTechStack: string;
    description?: string;
    developerIds: string[];
    frontendTechStack: string;
    gitAccountId?: string;
    gitRepositoryId?: string;
    name: string;
    productManagerIds: string[];
    projectManagerId: string;
    testerIds: string[];
    testEnvironmentId?: string;
    testEnvironmentUrl?: string;
  }

  export interface Project {
    architectId?: string;
    aiPlatformCode?: string;
    backendTechStack?: string;
    id: string;
    code: string;
    description?: string;
    developerIds: string[];
    frontendTechStack?: string;
    gitAccountId?: string;
    gitRepositoryId?: string;
    name: string;
    productManagerIds: string[];
    projectManagerId?: string;
    testerIds: string[];
    testEnvironmentId?: string;
    testEnvironmentUrl?: string;
    status: string;
    createdBy: string;
    createTime: string;
  }

  export interface CreateRequirementRequest {
    description?: string;
    endpointId?: string;
    moduleId?: string;
    priority?: number;
    projectId: string;
    requiresReview?: boolean;
    skillIds?: string[];
    stakeholders?: string;
    title: string;
  }

  export interface UpdateRequirementRequest {
    description?: string;
    priority?: number;
    skillIds?: string[];
    stakeholders?: string;
    title: string;
  }

  export interface CreateSkillRequest {
    code?: string;
    content: string;
    description?: string;
    name: string;
    type?: string;
  }

  export interface UpdateSkillRequest {
    content: string;
    description?: string;
    name: string;
    status?: string;
    type?: string;
  }

  export interface Skill {
    code: string;
    content: string;
    createTime: string;
    createdBy: string;
    description?: string;
    id: string;
    name: string;
    status: string;
    type: string;
  }

  export interface SubmitReviewRequest {
    reason: string;
    reviewerIds: string[];
  }

  export interface DecideReviewRequest {
    comment?: string;
  }

  export interface DecomposeRequirementRequest {
    assignmentMode?: 'auto' | 'manual';
    assigneeId?: string;
    assigneeType?: 0 | 1;
    instruction?: string;
    taskCount?: number;
  }

  export interface PreviewRequirementDecompositionRequest {
    aiPlatformCode?: string;
    instruction?: string;
    taskCount?: number;
  }

  export interface DevelopmentTaskDraft {
    description?: string;
    id?: string;
    priority: number;
    title: string;
  }

  export interface ConfirmRequirementDecompositionRequest {
    assignmentMode?: 'auto' | 'manual';
    assigneeId?: string;
    assigneeType?: 0 | 1;
    instruction?: string;
    previewId?: string;
    tasks: DevelopmentTaskDraft[];
  }

  export interface SaveRequirementDecompositionPreviewRequest {
    instruction?: string;
    previewId?: string;
    tasks: DevelopmentTaskDraft[];
  }

  export interface RequirementDecompositionPreview {
    aiPlatformCode?: string;
    confirmedAt?: string;
    confirmedBy?: string;
    createTime: string;
    createdBy?: string;
    errorMessage?: string;
    id: string;
    instruction?: string;
    projectId: string;
    rawContent?: string;
    requirementId: string;
    source: 'ai' | 'local' | string;
    status: 'draft' | 'confirmed' | string;
    tasks: DevelopmentTaskDraft[];
    updateTime?: string;
  }

  export interface ProjectEndpoint {
    code: string;
    createTime: string;
    createdBy: string;
    developerIds: string[];
    id: string;
    name: string;
    ownerId?: string;
    projectId: string;
    skillIds: string[];
    sort: number;
    status: string;
    testerIds: string[];
    type: string;
  }

  export interface CreateProjectEndpointRequest {
    code: string;
    developerIds?: string[];
    name: string;
    ownerId?: string;
    projectId: string;
    skillIds?: string[];
    sort?: number;
    testerIds?: string[];
    type: string;
  }

  export interface UpdateProjectEndpointRequest {
    developerIds?: string[];
    name: string;
    ownerId?: string;
    skillIds?: string[];
    sort?: number;
    status?: string;
    testerIds?: string[];
    type: string;
  }

  export interface FeatureModule {
    code: string;
    createTime: string;
    createdBy: string;
    description?: string;
    developerIds: string[];
    endpointId: string;
    id: string;
    name: string;
    ownerId?: string;
    projectId: string;
    sort: number;
    status: string;
    testerIds: string[];
  }

  export interface CreateFeatureModuleRequest {
    code: string;
    description?: string;
    developerIds?: string[];
    endpointId: string;
    name: string;
    ownerId?: string;
    projectId: string;
    sort?: number;
    testerIds?: string[];
  }

  export interface UpdateFeatureModuleRequest {
    description?: string;
    developerIds?: string[];
    name: string;
    ownerId?: string;
    sort?: number;
    status?: string;
    testerIds?: string[];
  }

  export interface AssignTaskRequest {
    assigneeId: string;
    assigneeType?: 0 | 1;
  }

  export interface Requirement {
    approvedAt?: string;
    closedAt?: string;
    createTime: string;
    createdBy: string;
    description?: string;
    developerId?: string;
    developmentCompletedAt?: string;
    id: string;
    endpointId?: string;
    priority: number;
    moduleId?: string;
    projectId: string;
    reviewedBy?: string;
    skillIds: string[];
    status: string;
    stakeholders?: string;
    submittedAt?: string;
    testedAt?: string;
    testUrl?: string;
    health: 'primary' | 'success' | 'warn';
    sourceFeedbackId?: string;
    sourceRequirementId?: string;
    title: string;
    voidedAt?: string;
  }

  export interface RequirementReview {
    comment?: string;
    createTime: string;
    id: string;
    projectId: string;
    requirementId: string;
    reviewedAt?: string;
    reviewerId: string;
    status: string;
    submitReason?: string;
  }

  export interface RequirementReviewItem {
    project: Project;
    requirement: Requirement;
    reviews: RequirementReview[];
  }

  export interface DevelopmentTask {
    assignedAt?: string;
    assignedBy?: string;
    assigneeId?: string;
    assigneeType: 0 | 1;
    completedAt?: string;
    createTime: string;
    createdBy: string;
    description?: string;
    id: string;
    priority: number;
    projectId: string;
    prompt?: string;
    requirementId: string;
    startedAt?: string;
    status: string;
    title: string;
    updateTime?: string;
  }

  export interface TaskPromptSection {
    content: string;
    notes: string[];
    title: string;
    usage: string[];
  }

  export interface TaskPrompt {
    mcpSetupPrompt: TaskPromptSection;
    prompt: string;
    taskExecutionPrompt: TaskPromptSection;
    taskId: string;
  }

  export interface ClaimTaskRequest {
    ownerDevice?: string;
  }

  export interface CreateFeedbackRequest {
    content?: string;
    developmentTaskId?: string;
    title: string;
  }

  export interface ConvertFeedbackRequest {
    description?: string;
    priority?: number;
    remark?: string;
    stakeholders?: string;
    title: string;
  }

  export interface ConvertRequirementSourcesRequest {
    description?: string;
    feedbackIds?: string[];
    priority?: number;
    remark?: string;
    stakeholders?: string;
    suggestionIds?: string[];
    title: string;
  }

  export interface RequirementFeedback {
    closedAt?: string;
    content?: string;
    convertedAt?: string;
    convertedRequirementId?: string;
    createTime: string;
    createdBy: string;
    developmentTaskId?: string;
    id: string;
    projectId: string;
    requirementId: string;
    status: string;
    title: string;
  }

  export interface ProjectMaterial {
    category?: string;
    contentType?: string;
    createTime: string;
    deletedAt?: string;
    description?: string;
    extension?: string;
    extractStatus: string;
    extractedTextPath?: string;
    id: string;
    itemType: 'file' | 'folder' | string;
    name: string;
    originalFileName?: string;
    parentId?: string;
    projectId: string;
    relativePath?: string;
    sha256?: string;
    sizeBytes: number;
    storageRoot: string;
    summary?: string;
    tags: string[];
    updateTime?: string;
    uploadedBy: string;
  }

  export interface ProjectMaterialList {
    items: ProjectMaterial[];
    pageIndex: number;
    pageSize: number;
    total: number;
  }

  export interface CreateProjectMaterialFolderRequest {
    category?: string;
    description?: string;
    name: string;
    parentId?: string;
    tags?: string[];
  }

  export interface UpdateProjectMaterialRequest {
    category?: string;
    description?: string;
    name: string;
    tags?: string[];
  }

  export interface MoveProjectMaterialRequest {
    parentId?: string;
  }

  export interface CreateProposalRequest {
    content?: string;
    instruction?: string;
    materialIds?: string[];
    summary?: string;
    title: string;
  }

  export interface UpdateProposalRequest {
    content?: string;
    instruction?: string;
    materialIds?: string[];
    summary?: string;
    title: string;
  }

  export interface ProposalMaterial {
    createTime: string;
    extractedTextSnapshotPath?: string;
    id: string;
    material?: ProjectMaterial;
    materialId: string;
    materialVersionHash?: string;
    projectId: string;
    proposalId: string;
  }

  export interface ProposalConversation {
    content: string;
    createTime: string;
    createdBy: string;
    id: string;
    materialIds: string[];
    projectId: string;
    proposalId: string;
    role: string;
    tokenUsageJson?: string;
  }

  export interface ProposalRequirement {
    createTime: string;
    createdBy: string;
    id: string;
    materialIds: string[];
    projectId: string;
    proposalId: string;
    requirementId: string;
  }

  export interface Proposal {
    aiPromptSnapshot?: string;
    aiResultSnapshot?: string;
    confirmedAt?: string;
    content?: string;
    conversations?: ProposalConversation[];
    convertedAt?: string;
    createTime: string;
    createdBy: string;
    id: string;
    instruction?: string;
    materials: ProposalMaterial[];
    projectId: string;
    requirements?: ProposalRequirement[];
    sourceType: string;
    status: string;
    summary?: string;
    title: string;
    updateTime?: string;
    voidedAt?: string;
  }

  export interface ProposalList {
    items: Proposal[];
    pageIndex: number;
    pageSize: number;
    total: number;
  }

  export interface CompleteDevelopmentRequest {
    testUrl?: string;
  }

  export interface CreateBugRequest {
    description?: string;
    environment?: string;
    projectId: string;
    requirementId: string;
    severity?: string;
    testExecutionId?: string;
    testPlanId?: string;
    title: string;
  }

  export interface CreateFeatureSuggestionRequest {
    content: string;
    endpointId?: string;
    moduleId?: string;
    projectId: string;
    requirementId?: string;
  }

  export interface FeatureSuggestion {
    content: string;
    convertedAt?: string;
    convertedRequirementId?: string;
    createTime: string;
    createdBy: string;
    endpointId?: string;
    id: string;
    moduleId?: string;
    projectId: string;
    requirementId?: string;
    status: string;
  }

  export interface Bug {
    createTime: string;
    createdBy: string;
    description?: string;
    developerId?: string;
    environment: string;
    fixedAt?: string;
    id: string;
    projectId: string;
    requirementId: string;
    severity: string;
    status: string;
    testExecutionId?: string;
    testPlanId?: string;
    title: string;
  }

  export interface Lease {
    completedAt?: string;
    createTime: string;
    expiresAt: string;
    id: string;
    leaseToken: string;
    ownerDevice?: string;
    ownerId: string;
    projectId: string;
    status: string;
    targetId: string;
    targetType: string;
  }

  export interface Summary {
    activeLeaseCount: number;
    completedRequirementCount: number;
    developingRequirementCount: number;
    openBugCount: number;
    projectCount: number;
    readyRequirementCount: number;
    readyTestRequirementCount: number;
    requirementCount: number;
    testingRequirementCount: number;
  }
}

export namespace SprintTestApi {
  export interface CreatePlanRequest {
    bugId?: string;
    environment?: string;
    name: string;
    projectId: string;
    requirementId: string;
    testUrl?: string;
  }

  export interface CompletePlanRequest {
    status: string;
    summary?: string;
  }

  export interface SubmitExecutionRequest {
    actualResult?: string;
    bugId?: string;
    createdBugId?: string;
    evidence?: string;
    result: string;
  }

  export interface UpdateExecutionBugRequest {
    bugId?: string;
    createdBugId?: string;
  }

  export interface TestPlan {
    bugId?: string;
    completedAt?: string;
    createTime: string;
    createdBy: string;
    environment: string;
    id: string;
    name: string;
    projectId: string;
    requirementId: string;
    startedAt?: string;
    status: string;
    summary?: string;
    testerId?: string;
    testUrl?: string;
  }

  export interface TestExecution {
    actualResult?: string;
    bugId?: string;
    createTime: string;
    createdBugId?: string;
    evidence?: string;
    executedAt: string;
    id: string;
    requirementId: string;
    result: string;
    testerId: string;
    testPlanId: string;
  }
}

export namespace SprintUserApi {
  export interface UserOption {
    displayName: string;
    id: string;
    username: string;
  }
}

export function createProjectApi(data: SprintMvpApi.CreateProjectRequest) {
  return requestClient.post<SprintMvpApi.Project>('/mvp/projects', data);
}

export function listProjectsApi() {
  return requestClient.get<SprintMvpApi.Project[]>('/mvp/projects');
}

export function updateProjectApi(
  id: string,
  data: SprintMvpApi.UpdateProjectRequest,
) {
  return requestClient.put<SprintMvpApi.Project>(`/mvp/projects/${id}`, data);
}

export function listProjectMaterialsApi(
  projectId: string,
  params?: {
    category?: string;
    itemType?: string;
    keyword?: string;
    pageIndex?: number;
    pageSize?: number;
    parentId?: string;
    uploadedBy?: string;
  },
) {
  return requestClient.get<SprintMvpApi.ProjectMaterialList>(
    `/mvp/projects/${projectId}/materials`,
    { params },
  );
}

export function createProjectMaterialFolderApi(
  projectId: string,
  data: SprintMvpApi.CreateProjectMaterialFolderRequest,
) {
  return requestClient.post<SprintMvpApi.ProjectMaterial>(
    `/mvp/projects/${projectId}/materials/folders`,
    data,
  );
}

export function uploadProjectMaterialApi(projectId: string, data: FormData) {
  return requestClient.post<SprintMvpApi.ProjectMaterial>(
    `/mvp/projects/${projectId}/materials/upload`,
    data,
  );
}

export function updateProjectMaterialApi(
  id: string,
  data: SprintMvpApi.UpdateProjectMaterialRequest,
) {
  return requestClient.put<SprintMvpApi.ProjectMaterial>(
    `/mvp/materials/${id}`,
    data,
  );
}

export function moveProjectMaterialApi(
  id: string,
  data: SprintMvpApi.MoveProjectMaterialRequest,
) {
  return requestClient.put<SprintMvpApi.ProjectMaterial>(
    `/mvp/materials/${id}/move`,
    data,
  );
}

export function deleteProjectMaterialApi(id: string) {
  return requestClient.delete<boolean>(`/mvp/materials/${id}`);
}

export function downloadProjectMaterialApi(id: string) {
  return requestClient.download<Blob>(`/mvp/materials/${id}/download`);
}

export function listProposalsApi(
  projectId: string,
  params?: {
    createdBy?: string;
    keyword?: string;
    pageIndex?: number;
    pageSize?: number;
    status?: string;
  },
) {
  return requestClient.get<SprintMvpApi.ProposalList>(
    `/mvp/projects/${projectId}/proposals`,
    { params },
  );
}

export function createProposalApi(
  projectId: string,
  data: SprintMvpApi.CreateProposalRequest,
) {
  return requestClient.post<SprintMvpApi.Proposal>(
    `/mvp/projects/${projectId}/proposals`,
    data,
  );
}

export function getProposalApi(id: string) {
  return requestClient.get<SprintMvpApi.Proposal>(`/mvp/proposals/${id}`);
}

export function updateProposalApi(
  id: string,
  data: SprintMvpApi.UpdateProposalRequest,
) {
  return requestClient.put<SprintMvpApi.Proposal>(
    `/mvp/proposals/${id}`,
    data,
  );
}

export function confirmProposalApi(id: string) {
  return requestClient.post<SprintMvpApi.Proposal>(
    `/mvp/proposals/${id}/confirm`,
  );
}

export function voidProposalApi(id: string) {
  return requestClient.post<SprintMvpApi.Proposal>(
    `/mvp/proposals/${id}/void`,
  );
}

export function createSkillApi(data: SprintMvpApi.CreateSkillRequest) {
  return requestClient.post<SprintMvpApi.Skill>('/mvp/skills', data);
}

export function listSkillsApi(
  activeOnly = false,
  params?: {
    keyword?: string;
    status?: string;
    type?: string;
  },
) {
  return requestClient.get<SprintMvpApi.Skill[]>('/mvp/skills', {
    params: { ...params, activeOnly },
  });
}

export function updateSkillApi(
  id: string,
  data: SprintMvpApi.UpdateSkillRequest,
) {
  return requestClient.put<SprintMvpApi.Skill>(`/mvp/skills/${id}`, data);
}

export function createProjectEndpointApi(
  data: SprintMvpApi.CreateProjectEndpointRequest,
) {
  return requestClient.post<SprintMvpApi.ProjectEndpoint>('/mvp/endpoints', data);
}

export function listProjectEndpointsApi(projectId?: string) {
  return requestClient.get<SprintMvpApi.ProjectEndpoint[]>('/mvp/endpoints', {
    params: { projectId },
  });
}

export function updateProjectEndpointApi(
  id: string,
  data: SprintMvpApi.UpdateProjectEndpointRequest,
) {
  return requestClient.put<SprintMvpApi.ProjectEndpoint>(
    `/mvp/endpoints/${id}`,
    data,
  );
}

export function createFeatureModuleApi(
  data: SprintMvpApi.CreateFeatureModuleRequest,
) {
  return requestClient.post<SprintMvpApi.FeatureModule>('/mvp/modules', data);
}

export function listFeatureModulesApi(projectId?: string, endpointId?: string) {
  return requestClient.get<SprintMvpApi.FeatureModule[]>('/mvp/modules', {
    params: { projectId, endpointId },
  });
}

export function updateFeatureModuleApi(
  id: string,
  data: SprintMvpApi.UpdateFeatureModuleRequest,
) {
  return requestClient.put<SprintMvpApi.FeatureModule>(
    `/mvp/modules/${id}`,
    data,
  );
}

export function deleteFeatureModuleApi(id: string) {
  return requestClient.delete<boolean>(`/mvp/modules/${id}`);
}

export function createRequirementApi(
  data: SprintMvpApi.CreateRequirementRequest,
) {
  return requestClient.post<SprintMvpApi.Requirement>(
    '/mvp/requirements',
    data,
  );
}

export function listRequirementsApi(
  projectId?: string,
  params?: {
    health?: string;
    keyword?: string;
    status?: string;
  },
) {
  return requestClient.get<SprintMvpApi.Requirement[]>('/mvp/requirements', {
    params: { ...params, projectId },
  });
}

export function updateRequirementApi(
  id: string,
  data: SprintMvpApi.UpdateRequirementRequest,
) {
  return requestClient.put<SprintMvpApi.Requirement>(
    `/mvp/requirements/${id}`,
    data,
  );
}

export function submitRequirementReviewApi(
  id: string,
  data: SprintMvpApi.SubmitReviewRequest,
) {
  return requestClient.post<SprintMvpApi.Requirement>(
    `/mvp/requirements/${id}/submit-review`,
    data,
  );
}

export function listMyPendingReviewsApi(params?: {
  keyword?: string;
  projectId?: string;
  status?: string;
}) {
  return requestClient.get<SprintMvpApi.RequirementReviewItem[]>(
    '/mvp/reviews/my-pending',
    { params },
  );
}

export function listRequirementReviewsApi(id: string) {
  return requestClient.get<SprintMvpApi.RequirementReview[]>(
    `/mvp/requirements/${id}/reviews`,
  );
}

export function createRequirementFeedbackApi(
  id: string,
  data: SprintMvpApi.CreateFeedbackRequest,
) {
  return requestClient.post<SprintMvpApi.RequirementFeedback>(
    `/mvp/requirements/${id}/feedback`,
    data,
  );
}

export function listRequirementFeedbackApi(id: string) {
  return requestClient.get<SprintMvpApi.RequirementFeedback[]>(
    `/mvp/requirements/${id}/feedback`,
  );
}

export function convertRequirementFeedbackApi(
  id: string,
  feedbackId: string,
  data: SprintMvpApi.ConvertFeedbackRequest,
) {
  return requestClient.post<SprintMvpApi.Requirement>(
    `/mvp/requirements/${id}/feedback/${feedbackId}/convert`,
    data,
  );
}

export function convertRequirementSourcesApi(
  id: string,
  data: SprintMvpApi.ConvertRequirementSourcesRequest,
) {
  return requestClient.post<SprintMvpApi.Requirement>(
    `/mvp/requirements/${id}/convert-sources`,
    data,
  );
}

export function createFeatureSuggestionApi(
  data: SprintMvpApi.CreateFeatureSuggestionRequest,
) {
  return requestClient.post<SprintMvpApi.FeatureSuggestion>(
    '/mvp/suggestions',
    data,
  );
}

export function listFeatureSuggestionsApi(params?: {
  moduleId?: string;
  projectId?: string;
  requirementId?: string;
}) {
  return requestClient.get<SprintMvpApi.FeatureSuggestion[]>('/mvp/suggestions', {
    params,
  });
}

export function approveRequirementReviewApi(
  id: string,
  data: SprintMvpApi.DecideReviewRequest,
) {
  return requestClient.post<SprintMvpApi.Requirement>(
    `/mvp/requirements/${id}/review/approve`,
    data,
  );
}

export function rejectRequirementReviewApi(
  id: string,
  data: SprintMvpApi.DecideReviewRequest,
) {
  return requestClient.post<SprintMvpApi.Requirement>(
    `/mvp/requirements/${id}/review/reject`,
    data,
  );
}

export function approveRequirementApi(id: string) {
  return requestClient.post<SprintMvpApi.Requirement>(
    `/mvp/requirements/${id}/approve`,
  );
}

export function voidRequirementApi(id: string) {
  return requestClient.post<SprintMvpApi.Requirement>(
    `/mvp/requirements/${id}/void`,
  );
}

export function deleteDraftRequirementApi(id: string) {
  return requestClient.delete<boolean>(`/mvp/requirements/${id}`);
}

export function decomposeRequirementApi(
  id: string,
  data: SprintMvpApi.DecomposeRequirementRequest,
) {
  return requestClient.post<SprintMvpApi.DevelopmentTask[]>(
    `/mvp/requirements/${id}/decompose`,
    data,
  );
}

export function listRequirementDecompositionPreviewsApi(id: string) {
  return requestClient.get<SprintMvpApi.RequirementDecompositionPreview[]>(
    `/mvp/requirements/${id}/decomposition-previews`,
  );
}

export function streamRequirementDecompositionPreviewApi(
  id: string,
  data: SprintMvpApi.PreviewRequirementDecompositionRequest,
  options: {
    onMessage?: (chunk: string) => void;
    onEnd?: () => void;
  },
) {
  return requestClient.postSSE(
    `/mvp/requirements/${id}/decomposition-previews/stream`,
    data,
    options,
  );
}

export function confirmRequirementDecompositionApi(
  id: string,
  data: SprintMvpApi.ConfirmRequirementDecompositionRequest,
) {
  return requestClient.post<SprintMvpApi.DevelopmentTask[]>(
    `/mvp/requirements/${id}/decomposition-previews/confirm`,
    data,
  );
}

export function saveRequirementDecompositionPreviewApi(
  id: string,
  data: SprintMvpApi.SaveRequirementDecompositionPreviewRequest,
) {
  return requestClient.post<SprintMvpApi.RequirementDecompositionPreview>(
    `/mvp/requirements/${id}/decomposition-previews/save`,
    data,
  );
}

export function listDevelopmentTasksApi(params?: {
  assigneeId?: string;
  projectId?: string;
  relatedUserId?: string;
  requirementId?: string;
  status?: string;
}) {
  return requestClient.get<SprintMvpApi.DevelopmentTask[]>('/mvp/tasks', {
    params,
  });
}

export function listMyDevelopmentTasksApi(params?: {
  projectId?: string;
  requirementId?: string;
  status?: string;
}) {
  return requestClient.get<SprintMvpApi.DevelopmentTask[]>('/mvp/tasks/my', {
    params,
  });
}

export function assignDevelopmentTaskApi(
  id: string,
  data: SprintMvpApi.AssignTaskRequest,
) {
  return requestClient.post<SprintMvpApi.DevelopmentTask>(
    `/mvp/tasks/${id}/assign`,
    data,
  );
}

export function getDevelopmentTaskPromptApi(id: string) {
  return requestClient.get<SprintMvpApi.TaskPrompt>(`/mvp/tasks/${id}/prompt`);
}

export function completeDevelopmentTaskApi(id: string) {
  return requestClient.post<SprintMvpApi.DevelopmentTask>(`/mvp/tasks/${id}/complete`);
}

export function claimRequirementApi(
  id: string,
  data: SprintMvpApi.ClaimTaskRequest,
) {
  return requestClient.post<SprintMvpApi.Lease>(
    `/mvp/requirements/${id}/claim`,
    data,
  );
}

export function completeRequirementDevelopmentApi(
  id: string,
  data: SprintMvpApi.CompleteDevelopmentRequest,
) {
  return requestClient.post<SprintMvpApi.Requirement>(
    `/mvp/requirements/${id}/complete-development`,
    data,
  );
}

export function startRequirementTestingApi(id: string) {
  return requestClient.post<SprintMvpApi.Requirement>(
    `/mvp/requirements/${id}/start-testing`,
  );
}

export function markRequirementTestedApi(id: string) {
  return requestClient.post<SprintMvpApi.Requirement>(
    `/mvp/requirements/${id}/mark-tested`,
  );
}

export function closeRequirementApi(id: string) {
  return requestClient.post<SprintMvpApi.Requirement>(
    `/mvp/requirements/${id}/close`,
  );
}

export function createBugApi(data: SprintMvpApi.CreateBugRequest) {
  return requestClient.post<SprintMvpApi.Bug>('/mvp/bugs', data);
}

export function listBugsApi(projectId?: string, requirementId?: string) {
  return requestClient.get<SprintMvpApi.Bug[]>('/mvp/bugs', {
    params: { projectId, requirementId },
  });
}

export function claimBugApi(id: string, data: SprintMvpApi.ClaimTaskRequest) {
  return requestClient.post<SprintMvpApi.Lease>(`/mvp/bugs/${id}/claim`, data);
}

export function fixBugApi(id: string) {
  return requestClient.post<SprintMvpApi.Bug>(`/mvp/bugs/${id}/fix`);
}

export function closeBugApi(id: string) {
  return requestClient.post<SprintMvpApi.Bug>(`/mvp/bugs/${id}/close`);
}

export function listActiveLeasesApi() {
  return requestClient.get<SprintMvpApi.Lease[]>('/mvp/leases/active');
}

export function getMvpSummaryApi() {
  return requestClient.get<SprintMvpApi.Summary>('/mvp/summary');
}

export function listUserOptionsApi() {
  return requestClient.get<SprintUserApi.UserOption[]>('/user/options');
}

export function createTestPlanApi(data: SprintTestApi.CreatePlanRequest) {
  return requestClient.post<SprintTestApi.TestPlan>('/test/plans', data);
}

export function listTestPlansApi(projectId?: string, requirementId?: string) {
  return requestClient.get<SprintTestApi.TestPlan[]>('/test/plans', {
    params: { projectId, requirementId },
  });
}

export function startTestPlanApi(id: string) {
  return requestClient.post<SprintTestApi.TestPlan>(`/test/plans/${id}/start`);
}

export function completeTestPlanApi(
  id: string,
  data: SprintTestApi.CompletePlanRequest,
) {
  return requestClient.post<SprintTestApi.TestPlan>(
    `/test/plans/${id}/complete`,
    data,
  );
}

export function submitTestExecutionApi(
  id: string,
  data: SprintTestApi.SubmitExecutionRequest,
) {
  return requestClient.post<SprintTestApi.TestExecution>(
    `/test/plans/${id}/executions`,
    data,
  );
}

export function updateTestExecutionBugApi(
  planId: string,
  executionId: string,
  data: SprintTestApi.UpdateExecutionBugRequest,
) {
  return requestClient.put<SprintTestApi.TestExecution>(
    `/test/plans/${planId}/executions/${executionId}/bug`,
    data,
  );
}

export function listTestExecutionsApi(id: string) {
  return requestClient.get<SprintTestApi.TestExecution[]>(
    `/test/plans/${id}/executions`,
  );
}
