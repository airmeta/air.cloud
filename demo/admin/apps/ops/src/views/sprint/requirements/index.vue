<script lang="ts" setup>
import type { SprintMvpApi, SprintUserApi } from '#/api/sprint/mvp';
import type { FormInstanceFunctions, FormRules } from 'tdesign-vue-next';
import type { PrimaryTableCol } from 'tdesign-vue-next';

import { computed, onActivated, onMounted, reactive, ref, watch } from 'vue';
import { useRouter } from 'vue-router';

import { IconifyIcon } from '@vben/icons';

import {
  Button as TButton,
  Collapse as TCollapse,
  CollapsePanel as TCollapsePanel,
  Dialog as TDialog,
  Divider as TDivider,
  Drawer as TDrawer,
  Form as TForm,
  FormItem as TFormItem,
  Input as TInput,
  Link as TLink,
  MessagePlugin,
  Select as TSelect,
  Space as TSpace,
  StepItem as TStepItem,
  Steps as TSteps,
  Switch as TSwitch,
  Table as TTable,
  Tag as TTag,
  Textarea as TTextarea,
} from 'tdesign-vue-next';

import {
  closeRequirementApi,
  completeRequirementDevelopmentApi,
  confirmRequirementDecompositionApi,
  convertRequirementFeedbackApi,
  convertRequirementSourcesApi,
  createRequirementFeedbackApi,
  createRequirementApi,
  deleteDraftRequirementApi,
  decomposeRequirementApi,
  listDevelopmentTasksApi,
  listFeatureSuggestionsApi,
  listFeatureModulesApi,
  listRequirementDecompositionPreviewsApi,
  listProjectEndpointsApi,
  listRequirementFeedbackApi,
  listRequirementReviewsApi,
  listRequirementsApi,
  listSkillsApi,
  listUserOptionsApi,
  saveRequirementDecompositionPreviewApi,
  streamRequirementDecompositionPreviewApi,
  submitRequirementReviewApi,
  updateRequirementApi,
  voidRequirementApi,
} from '#/api/sprint/mvp';
import { listDigitalWorkersApi, type AutomationApi } from '#/api/automation/workers';
import {
  requiredArrayRule,
  requiredRule,
  validateForm,
} from '#/views/_shared/form-rules';
import { formatDateTime } from '#/views/_shared/date-format';
import { confirmAndClose } from '#/views/_shared/dialog-confirm';
import { withSerialColumn } from '#/views/_shared/table-columns';

import { useProjectContextStore } from '#/store/project-context';
import HeaderProjectSelect from '#/components/header-project-select/header-project-select.vue';
import MarkdownEditor from '../_shared/markdown-editor.vue';
import { renderMarkdown } from '../_shared/markdown';
import SkillSelectOption from '../_shared/skill-select-option.vue';
import '../_shared/table-layout.css';

const convertingFeedback = ref(false);
const decomposing = ref(false);
const feedbackSaving = ref(false);
const loading = ref(false);
const requirementSaving = ref(false);
const reviewSubmitting = ref(false);
const router = useRouter();
const projectContext = useProjectContextStore();
const selectedProjectId = computed(() => projectContext.selectedProjectId);
const selectedRequirement = ref<SprintMvpApi.Requirement>();
const selectedRequirementKeys = ref<Array<number | string>>([]);
const editorVisible = ref(false);
const requirementFormRef = ref<FormInstanceFunctions>();
const detailVisible = ref(false);
const reviewVisible = ref(false);
const reviewFormRef = ref<FormInstanceFunctions>();
const decomposeVisible = ref(false);
const assignVisible = ref(false);
const feedbackVisible = ref(false);
const feedbackFormRef = ref<FormInstanceFunctions>();
const convertFeedbackVisible = ref(false);
const convertFeedbackFormRef = ref<FormInstanceFunctions>();
const pageProjectSelectRef = ref<InstanceType<typeof HeaderProjectSelect>>();
const projects = computed(() => projectContext.projects);
const endpoints = ref<SprintMvpApi.ProjectEndpoint[]>([]);
const modules = ref<SprintMvpApi.FeatureModule[]>([]);
const skills = ref<SprintMvpApi.Skill[]>([]);
const requirements = ref<SprintMvpApi.Requirement[]>([]);
const developmentTasks = ref<SprintMvpApi.DevelopmentTask[]>([]);
const digitalWorkers = ref<AutomationApi.DigitalWorker[]>([]);
const requirementFeedback = ref<SprintMvpApi.RequirementFeedback[]>([]);
const requirementFeedbackMap = ref<Record<string, SprintMvpApi.RequirementFeedback[]>>({});
const requirementSuggestionMap = ref<Record<string, SprintMvpApi.FeatureSuggestion[]>>({});
const requirementReviews = ref<SprintMvpApi.RequirementReview[]>([]);
const users = ref<SprintUserApi.UserOption[]>([]);
const selectedFeedback = ref<SprintMvpApi.RequirementFeedback>();
const selectedFeedbackTaskId = ref('');
const assigningRequirement = ref<SprintMvpApi.Requirement>();
const expandedRequirementIds = ref<Array<number | string>>([]);
const pendingRequirementActionIds = ref(new Set<string>());

const filters = reactive({
  endpointId: '',
  requirementInfo: '',
  status: '',
});
const requirementForm = reactive({
  description: '',
  endpointId: '',
  moduleId: '',
  priority: 3,
  projectId: '',
  requiresReview: true,
  skillIds: [] as string[],
  stakeholderIds: [] as string[],
  title: '',
});
const reviewForm = reactive({
  reason: '',
  reviewerIds: [] as string[],
});
const decomposeForm = reactive({
  instruction: '',
});
const assignForm = reactive({
  assigneeId: '',
  assigneeType: 0 as 0 | 1,
  assignmentMode: 'auto' as 'auto' | 'manual',
});
const decompositionPreviews = ref<SprintMvpApi.RequirementDecompositionPreview[]>([]);
const decompositionDrafts = ref<SprintMvpApi.DevelopmentTaskDraft[]>([]);
const editingDecompositionDraftTitles = ref<Record<number, boolean>>({});
const selectedPreview = ref<SprintMvpApi.RequirementDecompositionPreview>();
const decompositionPreviewMessage = ref('');
const decompositionPhase = ref<'assign' | 'confirm' | 'decompose'>('decompose');
let decompositionSseBuffer = '';
const feedbackForm = reactive({
  content: '',
  title: '',
});
const convertFeedbackForm = reactive({
  description: '',
  feedbackIds: [] as string[],
  priority: 3,
  remark: '',
  stakeholderIds: [] as string[],
  suggestionIds: [] as string[],
  title: '',
});
const requirementRules: FormRules<typeof requirementForm> = {
  endpointId: requiredRule('请选择端', 'change'),
  moduleId: requiredRule('请选择功能模块', 'change'),
  priority: requiredRule('请选择优先级', 'change'),
  title: requiredRule('请输入需求标题'),
};
const reviewRules: FormRules<typeof reviewForm> = {
  reason: requiredRule('请输入提交缘由'),
  reviewerIds: requiredArrayRule('请选择评审人'),
};
const feedbackRules: FormRules<typeof feedbackForm> = {
  title: requiredRule('请输入反馈标题'),
};
const convertFeedbackRules: FormRules<typeof convertFeedbackForm> = {
  priority: requiredRule('请选择优先级', 'change'),
  title: requiredRule('请输入后续需求标题'),
};
const pagination = reactive({
  current: 1,
  pageSize: 30,
});

const userOptions = computed(() =>
  users.value.map((user) => ({
    label: `${user.displayName} (${user.username})`,
    value: user.id,
  })),
);
const assignEmployeeOptions = computed(() => {
  const requirement = assigningRequirement.value || selectedRequirement.value || selectedRequirementForAction.value;
  const preferredIds = requirement ? resolveRequirementDeveloperIds(requirement) : [];
  const optionIds = preferredIds.length > 0 ? preferredIds : users.value.map((user) => user.id);
  return optionIds.map((id) => ({
    label: userMap.value[id]
      ? `${userMap.value[id].displayName} (${userMap.value[id].username})`
      : id,
    value: id,
  }));
});
const assignDigitalWorkerOptions = computed(() =>
  digitalWorkers.value
    .filter((worker) => ['active', 'idle', 'working'].includes(worker.status))
    .map((worker) => ({
      label: `${worker.name} (${worker.code})`,
      value: worker.agentUserId,
    })),
);
const assignAssigneeOptions = computed(() =>
  assignForm.assigneeType === 1 ? assignDigitalWorkerOptions.value : assignEmployeeOptions.value,
);
const autoAssignmentDescription = computed(() => {
  const requirement = assigningRequirement.value || selectedRequirement.value || selectedRequirementForAction.value;
  if (!requirement) return '请选择一条需求后再指派。';
  const developerIds = resolveRequirementDeveloperIds(requirement);
  if (developerIds.length === 0) {
    return '自动指派：当前需求未配置模块、端或项目研发人员，将创建待指派任务。';
  }

  return `自动指派：按模块、端、项目研发人员配置轮询分配，候选人：${developerIds
    .map((id) => resolveUserName(id))
    .join('、')}`;
});
const decompositionStepCurrent = computed(() => {
  if (decompositionPhase.value === 'assign') return 2;
  if (decompositionPhase.value === 'confirm') return 1;
  return 0;
});
const isRequirementAiDecomposing = computed(
  () => selectedRequirement.value?.status === 'ai_decomposing',
);
const isDecompositionInputLocked = computed(() => decomposing.value || isRequirementAiDecomposing.value);
const isDecompositionDecomposeStep = computed(() => decompositionPhase.value === 'decompose');
const isDecompositionConfirmStep = computed(() => decompositionPhase.value === 'confirm');
const isDecompositionAssignStep = computed(() => decompositionPhase.value === 'assign');
const isAiDecompositionReady = computed(
  () =>
    isDecompositionConfirmStep.value &&
    (selectedRequirement.value?.status === 'ai_decomposed' || selectedPreview.value?.source === 'ai'),
);
const showDecompositionActionButtons = computed(() =>
  isDecompositionDecomposeStep.value && !isAiDecompositionReady.value,
);
const decompositionDraftTitle = computed(() =>
  selectedPreview.value?.source === 'local' ? '预拆解草案' : 'AI预拆解草案',
);
const skillOptions = computed(() =>
  skills.value.map((skill) => ({
    label: `${skill.code} - ${skill.name}`,
    skill,
    value: skill.id,
  })),
);
const priorityOptions: Array<{
  label: string;
  theme: 'danger' | 'default' | 'success';
  value: number;
}> = [
  { label: '加急', theme: 'danger', value: 1 },
  { label: '正常', theme: 'default', value: 2 },
  { label: '可延后', theme: 'success', value: 3 },
];
type TDesignTheme = 'danger' | 'default' | 'primary' | 'success' | 'warning';

const getPriorityTheme = (
  priority: number,
  value: number,
  theme: TDesignTheme,
): TDesignTheme =>
  priority === value ? theme : 'default';
const getPriorityButtonClass = (priority: number, value: number) => ({
  'priority-option-button': true,
  'priority-option-button--danger': priority !== value && value === 1,
  'priority-option-button--success': priority !== value && value === 3,
});
function isRequirementActionPending(requirementId: string) {
  return pendingRequirementActionIds.value.has(requirementId);
}

function setRequirementActionPending(requirementId: string, pending: boolean) {
  const nextIds = new Set(pendingRequirementActionIds.value);
  if (pending) {
    nextIds.add(requirementId);
  } else {
    nextIds.delete(requirementId);
  }
  pendingRequirementActionIds.value = nextIds;
}
const userMap = computed(() => Object.fromEntries(users.value.map((item) => [item.id, item])));
const userNameMap = computed(() => Object.fromEntries(users.value.map((item) => [item.username, item])));
const digitalWorkerMap = computed(() =>
  Object.fromEntries(digitalWorkers.value.map((item) => [item.agentUserId, item])),
);
const endpointMap = computed(() =>
  Object.fromEntries(endpoints.value.map((endpoint) => [endpoint.id, endpoint])),
);
const endpointOptions = computed(() =>
  endpoints.value
    .filter((endpoint) => !requirementForm.projectId || endpoint.projectId === requirementForm.projectId)
    .map((endpoint) => ({
      label: endpoint.name,
      value: endpoint.id,
    })),
);
const currentProjectEndpointOptions = computed(() =>
  endpoints.value
    .filter((endpoint) => endpoint.projectId === selectedProjectId.value)
    .map((endpoint) => ({
      label: `${endpoint.name} (${endpoint.code})`,
      value: endpoint.id,
    })),
);
const moduleOptions = computed(() =>
  modules.value
    .filter(
      (module) =>
        (!requirementForm.projectId || module.projectId === requirementForm.projectId) &&
        (!requirementForm.endpointId || module.endpointId === requirementForm.endpointId),
    )
    .map((module) => ({
      label: module.name,
      value: module.id,
    })),
);
const selectedProject = computed(() =>
  projects.value.find((item) => item.id === selectedProjectId.value),
);
const requirementPageTitle = computed(() =>
  selectedProject.value
    ? `${selectedProject.value.name}需求管理(${selectedProject.value.code})`
    : '需求管理',
);
const convertFeedbackOptions = computed(() =>
  (selectedRequirement.value ? getRequirementFeedback(selectedRequirement.value.id) : [])
    .filter((feedback) => feedback.status === 'open')
    .map((feedback) => ({ label: feedback.title, value: feedback.id })),
);
const convertSuggestionOptions = computed(() =>
  (selectedRequirement.value ? requirementSuggestionMap.value[selectedRequirement.value.id] || [] : [])
    .filter((suggestion) => suggestion.status === 'open')
    .map((suggestion) => ({
      label: suggestion.content.length > 48 ? `${suggestion.content.slice(0, 48)}...` : suggestion.content,
      value: suggestion.id,
    })),
);
const childRequirementsBySource = computed(() => {
  const map: Record<string, SprintMvpApi.Requirement[]> = {};
  for (const requirement of requirements.value) {
    if (!requirement.sourceRequirementId) continue;
    const children = map[requirement.sourceRequirementId] || [];
    children.push(requirement);
    map[requirement.sourceRequirementId] = children;
  }
  return map;
});
const developmentTasksByRequirement = computed(() => {
  const map: Record<string, SprintMvpApi.DevelopmentTask[]> = {};
  for (const task of developmentTasks.value) {
    const tasks = map[task.requirementId] || [];
    tasks.push(task);
    map[task.requirementId] = tasks;
  }

  for (const tasks of Object.values(map)) {
    tasks.sort(
      (left, right) =>
        left.priority - right.priority || right.createTime.localeCompare(left.createTime),
    );
  }

  return map;
});
const visibleRequirementIds = computed(() =>
  new Set(requirements.value.map((requirement) => requirement.id)),
);

const statusText: Record<string, string> = {
  approved: '待拆解',
  ai_decomposed: 'AI拆解完成',
  ai_decomposing: 'AI拆解中',
  completed: '已完成',
  decomposed: '待推进',
  developing: '已推进',
  draft: '草稿',
  pending_fix: '待修复',
  pending_review: '待评审',
  ready_development: '待拆解',
  ready_test: '待测试',
  rejected: '评审驳回',
  tested: '已测试',
  testing: '测试中',
  voided: '已作废',
};

const reviewStatusText: Record<string, string> = {
  approved: '已通过',
  pending: '待评审',
  rejected: '已驳回',
};
const feedbackStatusText: Record<string, string> = {
  closed: '已关闭',
  converted: '已转需求',
  open: '待处理',
};
const taskStatusText: Record<string, string> = {
  assigned: '已指派',
  completed: '已完成',
  in_progress: '推进中',
  pending_assign: '待指派',
};
const decomposeAllowedStatuses = new Set([
  'approved',
  'ready_development',
  'ai_decomposing',
  'ai_decomposed',
]);
const feedbackAllowedStatuses = new Set(['tested', 'completed']);
const editAllowedStatuses = new Set(['draft']);
const reviewAllowedStatuses = new Set(['draft', 'rejected']);

const columns: PrimaryTableCol[] = [
  { colKey: 'row-select', type: 'single', width: 48 },
  { colKey: 'title', ellipsis: true, title: '需求名' },
  { colKey: 'endpointId', title: '所属端', width: 140 },
  { colKey: 'status', title: '状态', width: 120 },
  { colKey: 'priority', title: '优先级', width: 90 },
  { colKey: 'createdBy', title: '产品经理', width: 130 },
  { colKey: 'createTime', title: '创建时间', width: 170 },
  { colKey: 'stakeholders', title: '干系人', width: 160 },
  { colKey: 'actions', title: '操作', width: 100 },
];
const statusOptions = computed(() =>
  Object.entries(statusText).map(([value, label]) => ({ label, value })),
);
const selectedRequirementForAction = computed(() =>
  requirements.value.find((requirement) => requirement.id === selectedRequirementKeys.value[0]),
);
const canSubmitSelectedRequirement = computed(() =>
  Boolean(selectedRequirementForAction.value && canSubmitReview(selectedRequirementForAction.value)),
);
const canDecomposeSelectedRequirement = computed(() =>
  Boolean(
    selectedRequirementForAction.value &&
      decomposeAllowedStatuses.has(selectedRequirementForAction.value.status),
  ),
);
const canAssignSelectedRequirement = computed(() =>
  Boolean(
    selectedRequirementForAction.value &&
      ['approved', 'ready_development'].includes(selectedRequirementForAction.value.status),
  ),
);
const canDeleteDraftSelectedRequirement = computed(() =>
  selectedRequirementForAction.value?.status === 'draft',
);
const canVoidSelectedRequirement = computed(() =>
  selectedRequirementForAction.value?.status === 'rejected',
);
const canCloseSelectedRequirement = computed(() =>
  selectedRequirementForAction.value?.status === 'tested',
);
const canCreateSelectedFeedback = computed(() =>
  Boolean(selectedRequirementForAction.value && canCreateFeedback(selectedRequirementForAction.value)),
);

const tablePagination = computed(() => ({
  current: pagination.current,
  pageSize: pagination.pageSize,
  pageSizeOptions: [30, 50, 100, 200],
  showJumper: true,
  showPageSize: true,
  size: 'small' as const,
  total: requirements.value.length,
}));

function handlePageChange(pageInfo: { current: number; pageSize: number }) {
  pagination.current = pageInfo.current;
  pagination.pageSize = pageInfo.pageSize;
}

function syncExpandedRequirementIds() {
  expandedRequirementIds.value = expandedRequirementIds.value.filter((key) => {
    const requirementId = String(key);
    return visibleRequirementIds.value.has(requirementId);
  });
}

function canCreateFeedback(requirement: SprintMvpApi.Requirement) {
  return feedbackAllowedStatuses.has(requirement.status) && !requirement.sourceFeedbackId;
}

function canEditRequirement(requirement: SprintMvpApi.Requirement) {
  return editAllowedStatuses.has(requirement.status);
}

function canSubmitReview(requirement: SprintMvpApi.Requirement) {
  return reviewAllowedStatuses.has(requirement.status);
}

function getRequirementFeedback(requirementId: string) {
  return requirementFeedbackMap.value[requirementId] || [];
}

function getRequirementTasks(requirementId: string) {
  return developmentTasksByRequirement.value[requirementId] || [];
}

function resolveRequirementDeveloperIds(requirement: SprintMvpApi.Requirement): string[] {
  const moduleDevelopers = modules.value.find((module) => module.id === requirement.moduleId)?.developerIds || [];
  if (moduleDevelopers.length > 0) return [...new Set(moduleDevelopers)];

  const endpointDevelopers = endpoints.value.find((endpoint) => endpoint.id === requirement.endpointId)?.developerIds || [];
  if (endpointDevelopers.length > 0) return [...new Set(endpointDevelopers)];

  const projectDevelopers = projects.value.find((project) => project.id === requirement.projectId)?.developerIds || [];
  return [...new Set(projectDevelopers)];
}

function deserializeStakeholders(value?: string) {
  if (!value) return [];
  const userByUsername = Object.fromEntries(users.value.map((user) => [user.username, user.id]));
  return value
    .split(',')
    .map((item) => item.trim())
    .filter(Boolean)
    .map((item) => userByUsername[item] || item)
    .filter((item, index, all) => all.indexOf(item) === index);
}

function serializeStakeholders(userIds: string[]) {
  const userById = userMap.value;
  return userIds
    .map((id) => userById[id]?.username || id)
    .filter(Boolean)
    .join(',');
}

function resolveStakeholderNames(value?: string) {
  const ids = deserializeStakeholders(value);
  if (ids.length === 0) return '未填写';
  return ids
    .map((id) => userMap.value[id]?.displayName || userMap.value[id]?.username || id)
    .join('、');
}

function resolveUserName(userId?: string) {
  if (!userId) return '未指定';
  const user = userMap.value[userId] || userNameMap.value[userId];
  return user?.displayName || user?.username || userId;
}

function resolveTaskAssigneeName(task: SprintMvpApi.DevelopmentTask) {
  if (!task.assigneeId) return '未指定';
  if (task.assigneeType === 1) {
    const worker = digitalWorkerMap.value[task.assigneeId];
    return worker ? `${worker.name} (${worker.code})` : task.assigneeId;
  }

  return resolveUserName(task.assigneeId);
}

function resolveEndpointName(endpointId?: string) {
  if (!endpointId) return '未归属';
  return endpointMap.value[endpointId]?.name || endpointId;
}

function resolvePriorityText(priority: number) {
  return priorityOptions.find((item) => item.value === priority)?.label || `优先级 ${priority}`;
}

function getRequirementFollowUpItems(requirement: SprintMvpApi.Requirement) {
  const feedbackItems = getRequirementFeedback(requirement.id).map((feedback) => ({
    content: feedback.content || '暂无内容',
    createdAt: feedback.createTime,
    feedback,
    id: `feedback:${feedback.id}`,
    status: feedback.status,
    title: feedback.title,
    type: 'feedback' as const,
  }));
  const childItems = (childRequirementsBySource.value[requirement.id] || []).map((child) => ({
    child,
    content: child.description || '暂无内容',
    createdAt: child.createTime,
    id: `requirement:${child.id}`,
    status: child.status,
    title: child.title,
    type: 'requirement' as const,
  }));
  return [...feedbackItems, ...childItems].sort((left, right) =>
    right.createdAt.localeCompare(left.createdAt),
  );
}

async function ensureRequirementFeedback(requirementId: string) {
  if (requirementFeedbackMap.value[requirementId]) return;
  const feedback = await listRequirementFeedbackApi(requirementId);
  requirementFeedbackMap.value = {
    ...requirementFeedbackMap.value,
    [requirementId]: feedback,
  };
}

async function ensureRequirementSuggestions(requirement: SprintMvpApi.Requirement) {
  if (requirementSuggestionMap.value[requirement.id]) return;
  const suggestions = await listFeatureSuggestionsApi({
    projectId: requirement.projectId,
    requirementId: requirement.id,
  });
  requirementSuggestionMap.value = {
    ...requirementSuggestionMap.value,
    [requirement.id]: suggestions,
  };
}

async function handleExpandedRowKeysChange(keys: Array<number | string>) {
  expandedRequirementIds.value = keys;
  await Promise.all(keys.map((key) => ensureRequirementFeedback(String(key))));
  syncExpandedRequirementIds();
}

function resetForm() {
  const projectId = selectedProjectId.value || '';
  const endpointId = endpoints.value.find((endpoint) => endpoint.projectId === projectId)?.id || '';
  const endpoint = endpoints.value.find((item) => item.id === endpointId);
  Object.assign(requirementForm, {
    description: '',
    endpointId,
    moduleId:
      modules.value.find(
        (module) => module.projectId === projectId && module.endpointId === endpointId,
      )?.id || '',
    priority: 3,
    projectId,
    requiresReview: true,
    skillIds: [...(endpoint?.skillIds || [])],
    stakeholderIds: [],
    title: '',
  });
}

function openProjectSelectDrawer() {
  pageProjectSelectRef.value?.openDrawer();
}

function openCreate() {
  if (!selectedProjectId.value) {
    MessagePlugin.warning('请先在顶部选择项目');
    return;
  }
  selectedRequirement.value = undefined;
  resetForm();
  editorVisible.value = true;
}

function openEdit(requirement: SprintMvpApi.Requirement) {
  if (!canEditRequirement(requirement)) {
    MessagePlugin.warning('需求提交评审后不支持编辑');
    return;
  }
  selectedRequirement.value = requirement;
  Object.assign(requirementForm, {
    description: requirement.description || '',
    endpointId: requirement.endpointId || '',
    moduleId: requirement.moduleId || '',
    priority: requirement.priority,
    projectId: requirement.projectId,
    requiresReview: true,
    skillIds: [...(requirement.skillIds || [])],
    stakeholderIds: deserializeStakeholders(requirement.stakeholders),
    title: requirement.title,
  });
  editorVisible.value = true;
}

async function openDetail(requirement: SprintMvpApi.Requirement) {
  selectedRequirement.value = requirement;
  const [reviews, feedback] = await Promise.all([
    listRequirementReviewsApi(requirement.id),
    listRequirementFeedbackApi(requirement.id),
  ]);
  requirementReviews.value = reviews;
  requirementFeedback.value = feedback;
  requirementFeedbackMap.value = {
    ...requirementFeedbackMap.value,
    [requirement.id]: feedback,
  };
  detailVisible.value = true;
}

function openReview(requirement: SprintMvpApi.Requirement) {
  selectedRequirement.value = requirement;
  reviewForm.reason = '';
  reviewForm.reviewerIds = users.value
    .filter((user) => requirement.stakeholders?.includes(user.username) || user.username === 'admin')
    .map((user) => user.id);
  reviewVisible.value = true;
}

async function openDecompose(requirement: SprintMvpApi.Requirement) {
  if (!decomposeAllowedStatuses.has(requirement.status)) {
    MessagePlugin.warning('需求评审通过后才能拆解任务');
    return;
  }
  selectedRequirement.value = requirement;
  decomposeForm.instruction = '';
  decompositionDrafts.value = [];
  editingDecompositionDraftTitles.value = {};
  selectedPreview.value = undefined;
  decompositionPreviewMessage.value = '';
  decompositionPhase.value = 'decompose';
  assignForm.assignmentMode = 'auto';
  assignForm.assigneeType = 0;
  assignForm.assigneeId = '';
  decomposeVisible.value = true;
  await loadDecompositionPreviews(requirement.id);
}

async function loadDecompositionPreviews(requirementId: string) {
  decompositionPreviews.value = await listRequirementDecompositionPreviewsApi(requirementId);
  const latestDraft = decompositionPreviews.value.find((item) => item.status === 'draft');
  if (latestDraft) {
    applyDecompositionPreview(latestDraft);
  } else if (selectedRequirement.value?.status === 'ai_decomposed') {
    decompositionPreviewMessage.value = 'AI拆解已完成，但未找到可确认的任务草案';
    decompositionPhase.value = 'confirm';
  } else if (decompositionDrafts.value.length === 0) {
    decompositionPhase.value = 'decompose';
  }
}

function applyDecompositionPreview(preview: SprintMvpApi.RequirementDecompositionPreview) {
  const requirementPriority = selectedRequirement.value?.priority || 3;
  selectedPreview.value = preview;
  decompositionDrafts.value = preview.tasks.map((task) => ({
    description: task.description || '',
    id: task.id,
    priority: requirementPriority,
    title: task.title,
  }));
  editingDecompositionDraftTitles.value = {};
  decomposeForm.instruction = preview.instruction || decomposeForm.instruction;
  decompositionPreviewMessage.value = preview.errorMessage || '';
  decompositionPhase.value = 'confirm';
}

function toggleDecompositionDraftTitleEdit(index: number, editing: boolean) {
  editingDecompositionDraftTitles.value = {
    ...editingDecompositionDraftTitles.value,
    [index]: editing,
  };
}

function addDecompositionDraft() {
  decompositionDrafts.value = [
    ...decompositionDrafts.value,
    {
      description: '',
      id: undefined,
      priority: selectedRequirement.value?.priority || 3,
      title: '',
    },
  ];
  toggleDecompositionDraftTitleEdit(decompositionDrafts.value.length - 1, true);
}

function removeDecompositionDraft(index: number) {
  decompositionDrafts.value = decompositionDrafts.value.filter((_, currentIndex) => currentIndex !== index);
  editingDecompositionDraftTitles.value = Object.fromEntries(
    Object.entries(editingDecompositionDraftTitles.value)
      .map(([key, value]) => {
        const currentIndex = Number(key);
        if (currentIndex === index) return undefined;
        return [String(currentIndex > index ? currentIndex - 1 : currentIndex), value] as const;
      })
      .filter((item): item is readonly [string, boolean] => Boolean(item)),
  );
}

function openFeedback(requirement: SprintMvpApi.Requirement, task?: SprintMvpApi.DevelopmentTask) {
  if (!canCreateFeedback(requirement)) {
    MessagePlugin.warning('閸ョ偤顩潪顒佸床閸戣櫣娈戦崥搴ｇ敾闂団偓濮瑰倷绗夐弨顖涘瘮閸愬秵顐奸崶鐐侯洯');
    return;
  }
  if (task && task.status !== 'completed') {
    MessagePlugin.warning('请先完成任务后再记录回馈');
    return;
  }
  selectedRequirement.value = requirement;
  selectedFeedbackTaskId.value = task?.id || '';
  Object.assign(feedbackForm, {
    content: task?.description || '',
    title: task ? `任务回馈 - ${task.title}` : '',
  });
  feedbackVisible.value = true;
}

async function openConvertFeedback(feedback: SprintMvpApi.RequirementFeedback) {
  selectedFeedback.value = feedback;
  if (selectedRequirement.value) {
    await ensureRequirementSuggestions(selectedRequirement.value);
  }
  Object.assign(convertFeedbackForm, {
    description: feedback.content || '',
    feedbackIds: [feedback.id],
    priority: selectedRequirement.value?.priority || 3,
    remark: '',
    stakeholderIds: deserializeStakeholders(selectedRequirement.value?.stakeholders),
    suggestionIds: [] as string[],
    title: feedback.title,
  });
  convertFeedbackVisible.value = true;
}

async function openConvertFeedbackFromRequirement(
  requirement: SprintMvpApi.Requirement,
  feedback: SprintMvpApi.RequirementFeedback,
) {
  selectedRequirement.value = requirement;
  await ensureRequirementFeedback(requirement.id);
  await openConvertFeedback(feedback);
}

async function loadProjects() {
  const [, nextEndpoints, nextModules, nextSkills] = await Promise.all([
    projectContext.loadProjects(),
    listProjectEndpointsApi(),
    listFeatureModulesApi(),
    listSkillsApi(true),
  ]);
  endpoints.value = nextEndpoints;
  modules.value = nextModules;
  skills.value = nextSkills;
  syncCurrentEndpoint();
}

async function loadRequirements() {
  loading.value = true;
  try {
    const [nextRequirements, nextTasks] = await Promise.all([
      listRequirementsApi(selectedProjectId.value || undefined, {
        keyword: filters.requirementInfo || undefined,
        status: filters.status || undefined,
      }),
      listDevelopmentTasksApi({ projectId: selectedProjectId.value || undefined }),
    ]);
    const endpointId = filters.endpointId;
    const visibleRequirements = endpointId
      ? nextRequirements.filter((requirement) => requirement.endpointId === endpointId)
      : nextRequirements;
    visibleRequirements.sort((left, right) => right.createTime.localeCompare(left.createTime));
    const visibleRequirementIdSet = new Set(
      visibleRequirements.map((requirement) => requirement.id),
    );
    requirements.value = visibleRequirements;
    developmentTasks.value = endpointId
      ? nextTasks.filter((task) => visibleRequirementIdSet.has(task.requirementId))
      : nextTasks;
    selectedRequirementKeys.value = selectedRequirementKeys.value.filter((id) =>
      visibleRequirements.some((requirement) => requirement.id === id),
    );
    pagination.current = 1;
    const feedbackEntries = await Promise.all(
      requirements.value
        .filter((requirement) => feedbackAllowedStatuses.has(requirement.status))
        .map(async (requirement) => [
          requirement.id,
          await listRequirementFeedbackApi(requirement.id),
        ] as const),
    );
    requirementFeedbackMap.value = Object.fromEntries(feedbackEntries);
    const suggestionEntries = await Promise.all(
      requirements.value
        .filter((requirement) => feedbackAllowedStatuses.has(requirement.status))
        .map(async (requirement) => [
          requirement.id,
          await listFeatureSuggestionsApi({
            projectId: requirement.projectId,
            requirementId: requirement.id,
          }),
        ] as const),
    );
    requirementSuggestionMap.value = Object.fromEntries(suggestionEntries);
    syncExpandedRequirementIds();
    if (selectedRequirement.value) {
      selectedRequirement.value = requirements.value.find(
        (item) => item.id === selectedRequirement.value?.id,
      );
    }
  } finally {
    loading.value = false;
  }
}

function goTaskAdvance(task: SprintMvpApi.DevelopmentTask) {
  router.push(`/sprint/tasks/detail/${task.id}`);
}

async function queryRequirements() {
  handleLocalFilterChange();
  await loadRequirements();
}

async function resetFilters() {
  selectedRequirementKeys.value = [];
  Object.assign(filters, {
    endpointId: '',
    requirementInfo: '',
    status: '',
  });
  syncCurrentEndpoint();
  await loadRequirements();
}

function syncCurrentEndpoint() {
  const currentEndpointIds = currentProjectEndpointOptions.value.map((endpoint) => endpoint.value);
  if (filters.endpointId && currentEndpointIds.includes(filters.endpointId)) return;
  filters.endpointId = currentEndpointIds[0] || '';
}

function handleLocalFilterChange() {
  selectedRequirementKeys.value = [];
  pagination.current = 1;
}

async function handleEndpointFilterChange() {
  handleLocalFilterChange();
  await loadRequirements();
}

function getSelectedRequirementOrWarn() {
  if (!selectedRequirementForAction.value) {
    MessagePlugin.warning('请先选择一条需求');
    return undefined;
  }
  return selectedRequirementForAction.value;
}

function openSelectedDecompose() {
  const requirement = getSelectedRequirementOrWarn();
  if (!requirement || !canDecomposeSelectedRequirement.value) return;
  openDecompose(requirement);
}

function openSelectedAssignDialog() {
  const requirement = getSelectedRequirementOrWarn();
  if (!requirement || !canAssignSelectedRequirement.value) return;
  assigningRequirement.value = requirement;
  assignForm.assignmentMode = 'auto';
  assignForm.assigneeType = 0;
  assignForm.assigneeId = '';
  assignVisible.value = true;
}

function handleAssignAssigneeTypeChange() {
  assignForm.assigneeId = '';
}

async function submitAssignRequirement() {
  const requirement = assigningRequirement.value;
  if (!requirement || !canAssignSelectedRequirement.value) return;
  if (assignForm.assignmentMode === 'manual' && !assignForm.assigneeId) {
    MessagePlugin.warning(assignForm.assigneeType === 1 ? '请选择数字员工' : '请选择员工');
    return;
  }
  if (decomposing.value || isRequirementActionPending(requirement.id)) return;
  setRequirementActionPending(requirement.id, true);
  decomposing.value = true;
  try {
    await decomposeRequirementApi(requirement.id, {
      assignmentMode: assignForm.assignmentMode,
      assigneeId: assignForm.assignmentMode === 'manual' ? assignForm.assigneeId : undefined,
      assigneeType: assignForm.assignmentMode === 'manual' ? assignForm.assigneeType : undefined,
    });
    MessagePlugin.success('任务已指派');
    assignVisible.value = false;
    assigningRequirement.value = undefined;
    await loadRequirements();
  } finally {
    decomposing.value = false;
    setRequirementActionPending(requirement.id, false);
  }
}

function openSelectedFeedback() {
  const requirement = getSelectedRequirementOrWarn();
  if (!requirement || !canCreateSelectedFeedback.value) return;
  openFeedback(requirement);
}

function openSelectedReview() {
  const requirement = getSelectedRequirementOrWarn();
  if (!requirement || !canSubmitSelectedRequirement.value) return;
  openReview(requirement);
}

function deleteSelectedDraftRequirement() {
  const requirement = getSelectedRequirementOrWarn();
  if (!requirement || !canDeleteDraftSelectedRequirement.value) return;
  deleteDraftRequirement(requirement);
}

function voidSelectedRequirement() {
  const requirement = getSelectedRequirementOrWarn();
  if (!requirement || !canVoidSelectedRequirement.value) return;
  voidRequirement(requirement);
}

function closeSelectedRequirement() {
  const requirement = getSelectedRequirementOrWarn();
  if (!requirement || !canCloseSelectedRequirement.value) return;
  closeRequirement(requirement);
}

async function saveRequirement() {
  if (requirementSaving.value) return;
  if (!(await validateForm(requirementFormRef.value))) return;
  if (!requirementForm.projectId) {
    MessagePlugin.warning('请先选择项目');
    return;
  }
  if (!requirementForm.title.trim()) {
    MessagePlugin.warning('需求标题不能为空');
    return;
  }
  if (!requirementForm.endpointId || !requirementForm.moduleId) {
    MessagePlugin.warning('请选择端和功能模块');
    return;
  }

  requirementSaving.value = true;
  try {
    if (selectedRequirement.value) {
      if (!canEditRequirement(selectedRequirement.value)) {
        MessagePlugin.warning('需求提交评审后不支持编辑');
        editorVisible.value = false;
        return;
      }
      await updateRequirementApi(selectedRequirement.value.id, {
        description: requirementForm.description,
        priority: requirementForm.priority,
        stakeholders: serializeStakeholders(requirementForm.stakeholderIds),
        skillIds: [...requirementForm.skillIds],
        title: requirementForm.title,
      });
    } else {
      await createRequirementApi({
        description: requirementForm.description,
        endpointId: requirementForm.endpointId,
        moduleId: requirementForm.moduleId,
        priority: requirementForm.priority,
        projectId: requirementForm.projectId,
        requiresReview: requirementForm.requiresReview,
        skillIds: [...requirementForm.skillIds],
        stakeholders: serializeStakeholders(requirementForm.stakeholderIds),
        title: requirementForm.title,
      });
    }

    MessagePlugin.success('需求已保存');
    editorVisible.value = false;
    await loadRequirements();
  } finally {
    requirementSaving.value = false;
  }
}
async function submitReview() {
  if (reviewSubmitting.value) return;
  if (!selectedRequirement.value) return;
  if (!(await validateForm(reviewFormRef.value))) return;
  const reviewerIds = [...reviewForm.reviewerIds];
  if (reviewerIds.length === 0) {
    MessagePlugin.warning('请选择评审人');
    return;
  }

  reviewSubmitting.value = true;
  try {
    await submitRequirementReviewApi(selectedRequirement.value.id, {
      reason: reviewForm.reason,
      reviewerIds,
    });
    MessagePlugin.success('已提交需求评审');
    reviewVisible.value = false;
    await loadRequirements();
  } finally {
    reviewSubmitting.value = false;
  }
}
async function streamPreviewDecomposition() {
  if (decomposing.value) return;
  if (!selectedRequirement.value) return;
  decomposing.value = true;
  selectedRequirement.value.status = 'ai_decomposing';
  decompositionPhase.value = 'decompose';
  decompositionSseBuffer = '';
  decompositionPreviewMessage.value = '';
  try {
    await streamRequirementDecompositionPreviewApi(selectedRequirement.value.id, {
      instruction: decomposeForm.instruction,
    }, {
      onEnd: flushDecompositionPreviewBuffer,
      onMessage: handleDecompositionPreviewChunk,
    });
    await loadDecompositionPreviews(selectedRequirement.value.id);
    selectedRequirement.value.status = 'ai_decomposed';
    await loadRequirements();
  } finally {
    decomposing.value = false;
  }
}

async function saveDecompositionDraftsAndContinue() {
  if (decomposing.value) return;
  if (!selectedRequirement.value) return;
  const requirementPriority = selectedRequirement.value.priority || 3;
  const drafts = decompositionDrafts.value
    .map((task) => ({
      description: task.description?.trim() || '',
      id: task.id,
      priority: requirementPriority,
      title: task.title?.trim() || '',
    }));
  if (drafts.length === 0) {
    MessagePlugin.warning('请至少保留一条任务草案');
    return;
  }
  if (drafts.some((task) => !task.title || !task.description)) {
    MessagePlugin.warning('任务标题与任务内容不允许为空');
    return;
  }

  decomposing.value = true;
  try {
    const preview = await saveRequirementDecompositionPreviewApi(selectedRequirement.value.id, {
      instruction: decomposeForm.instruction,
      previewId: selectedPreview.value?.id,
      tasks: drafts,
    });
    MessagePlugin.success('任务草案已保存');
    selectedPreview.value = preview;
    decompositionPreviews.value = [
      preview,
      ...decompositionPreviews.value.filter((item) => item.id !== preview.id),
    ];
    decompositionDrafts.value = preview.tasks.map((task) => ({
      description: task.description || '',
      id: task.id,
      priority: requirementPriority,
      title: task.title,
    }));
    selectedRequirement.value.status = 'ai_decomposed';
    decompositionPhase.value = 'assign';
  } finally {
    decomposing.value = false;
  }
}

async function confirmDecompositionDrafts() {
  if (decomposing.value) return;
  if (!selectedRequirement.value) return;
  const requirementPriority = selectedRequirement.value.priority || 3;
  const drafts = decompositionDrafts.value
    .map((task) => ({
      description: task.description?.trim() || '',
      id: task.id,
      priority: requirementPriority,
      title: task.title?.trim() || '',
    }));
  if (drafts.length === 0) {
    MessagePlugin.warning('请至少保留一条任务草案');
    return;
  }
  if (drafts.some((task) => !task.title || !task.description)) {
    MessagePlugin.warning('任务标题与任务内容不允许为空');
    return;
  }
  if (assignForm.assignmentMode === 'manual' && !assignForm.assigneeId) {
    MessagePlugin.warning(assignForm.assigneeType === 1 ? '请选择数字员工' : '请选择员工');
    return;
  }

  decomposing.value = true;
  try {
    await confirmRequirementDecompositionApi(selectedRequirement.value.id, {
      assigneeId: assignForm.assignmentMode === 'manual' ? assignForm.assigneeId : undefined,
      assigneeType: assignForm.assignmentMode === 'manual' ? assignForm.assigneeType : undefined,
      assignmentMode: assignForm.assignmentMode,
      instruction: decomposeForm.instruction,
      previewId: selectedPreview.value?.id,
      tasks: drafts,
    });
    MessagePlugin.success('任务已创建');
    decomposeVisible.value = false;
    await loadRequirements();
  } finally {
    decomposing.value = false;
  }
}

function handleDecompositionPreviewChunk(chunk: string) {
  decompositionSseBuffer += chunk;
  const completeBlocks = decompositionSseBuffer.split(/\n\n+/);
  decompositionSseBuffer = completeBlocks.pop() || '';
  parseSseBlocks(completeBlocks).forEach(({ event, data }) => {
    if (event === 'preview') {
      const preview = JSON.parse(data) as SprintMvpApi.RequirementDecompositionPreview;
      decompositionPreviews.value = [preview, ...decompositionPreviews.value.filter((item) => item.id !== preview.id)];
      applyDecompositionPreview(preview);
    } else if (event === 'phase') {
      decompositionPreviewMessage.value = '';
      decompositionPhase.value = 'decompose';
    } else if (event === 'error') {
      const payload = JSON.parse(data) as { message?: string };
      decompositionPreviewMessage.value = payload.message || '预拆解失败';
      MessagePlugin.error(decompositionPreviewMessage.value);
    }
  });
}

function flushDecompositionPreviewBuffer() {
  if (!decompositionSseBuffer.trim()) return;
  parseSseBlocks([decompositionSseBuffer]).forEach(({ event, data }) => {
    if (event === 'preview') {
      const preview = JSON.parse(data) as SprintMvpApi.RequirementDecompositionPreview;
      decompositionPreviews.value = [preview, ...decompositionPreviews.value.filter((item) => item.id !== preview.id)];
      applyDecompositionPreview(preview);
    }
  });
  decompositionSseBuffer = '';
}

function parseSseBlocks(blocks: string[]) {
  return blocks
    .map((block) => {
      const event = block.match(/^event:\s*(.+)$/m)?.[1]?.trim() || 'message';
      const data = block
        .split(/\n/)
        .filter((line) => line.startsWith('data:'))
        .map((line) => line.slice(5).trimStart())
        .join('\n');
      return { data, event };
    })
    .filter((item) => item.data);
}

async function voidRequirement(requirement: SprintMvpApi.Requirement) {
  if (isRequirementActionPending(requirement.id)) return;
  setRequirementActionPending(requirement.id, true);
  try {
    await voidRequirementApi(requirement.id);
    MessagePlugin.success('闂団偓濮瑰倸鍑℃担婊冪熬');
    detailVisible.value = false;
    await loadRequirements();
  } finally {
    setRequirementActionPending(requirement.id, false);
  }
}

function deleteDraftRequirement(requirement: SprintMvpApi.Requirement) {
  if (isRequirementActionPending(requirement.id)) return;
  setRequirementActionPending(requirement.id, true);
  confirmAndClose({
    body: `确认删除草稿需求「${requirement.title}」？`,
    confirmBtn: '删除',
    header: '删除草稿需求',
    onClose: () => setRequirementActionPending(requirement.id, false),
    onConfirm: async () => {
      try {
        await deleteDraftRequirementApi(requirement.id);
        MessagePlugin.success('草稿需求已删除');
        detailVisible.value = false;
        await loadRequirements();
      } finally {
        setRequirementActionPending(requirement.id, false);
      }
    },
  });
}

async function closeRequirement(requirement: SprintMvpApi.Requirement) {
  if (isRequirementActionPending(requirement.id)) return;
  setRequirementActionPending(requirement.id, true);
  try {
    await closeRequirementApi(requirement.id);
    MessagePlugin.success('需求已作废');
    detailVisible.value = false;
    await loadRequirements();
  } finally {
    setRequirementActionPending(requirement.id, false);
  }
}

async function completeRequirementDevelopment(requirement: SprintMvpApi.Requirement) {
  if (isRequirementActionPending(requirement.id)) return;
  setRequirementActionPending(requirement.id, true);
  try {
    await completeRequirementDevelopmentApi(requirement.id, {});
    MessagePlugin.success('需求已确认开发完成，进入待测试');
    detailVisible.value = false;
    await loadRequirements();
  } finally {
    setRequirementActionPending(requirement.id, false);
  }
}

async function saveFeedback() {
  if (feedbackSaving.value) return;
  if (!selectedRequirement.value) return;
  if (!(await validateForm(feedbackFormRef.value))) return;
  if (!feedbackForm.title.trim()) {
    MessagePlugin.warning('回馈标题不能为空');
    return;
  }

  feedbackSaving.value = true;
  try {
    await createRequirementFeedbackApi(selectedRequirement.value.id, {
      content: feedbackForm.content,
      developmentTaskId: selectedFeedbackTaskId.value || undefined,
      title: feedbackForm.title,
    });
    MessagePlugin.success('反馈已记录');
    feedbackVisible.value = false;
    requirementFeedback.value = await listRequirementFeedbackApi(selectedRequirement.value.id);
    requirementFeedbackMap.value = {
      ...requirementFeedbackMap.value,
      [selectedRequirement.value.id]: requirementFeedback.value,
    };
  } finally {
    feedbackSaving.value = false;
  }
}
async function convertFeedback() {
  if (convertingFeedback.value) return;
  if (!selectedRequirement.value) return;
  if (!(await validateForm(convertFeedbackFormRef.value))) return;
  if (!convertFeedbackForm.title.trim()) {
    MessagePlugin.warning('后续需求标题不能为空');
    return;
  }

  if (convertFeedbackForm.feedbackIds.length === 0 && convertFeedbackForm.suggestionIds.length === 0) {
    MessagePlugin.warning('请选择至少一个回馈或优化建议');
    return;
  }

  convertingFeedback.value = true;
  try {
    const feedback = selectedFeedback.value;
    if (
      feedback &&
      convertFeedbackForm.feedbackIds.length === 1 &&
      convertFeedbackForm.suggestionIds.length === 0 &&
      feedback.id === convertFeedbackForm.feedbackIds[0]
    ) {
      await convertRequirementFeedbackApi(
        selectedRequirement.value.id,
        feedback.id,
        {
          description: convertFeedbackForm.description,
          priority: convertFeedbackForm.priority,
          remark: convertFeedbackForm.remark,
          stakeholders: serializeStakeholders(convertFeedbackForm.stakeholderIds),
          title: convertFeedbackForm.title,
        },
      );
    } else {
      await convertRequirementSourcesApi(selectedRequirement.value.id, {
        description: convertFeedbackForm.description,
        feedbackIds: [...convertFeedbackForm.feedbackIds],
        priority: convertFeedbackForm.priority,
        remark: convertFeedbackForm.remark,
        stakeholders: serializeStakeholders(convertFeedbackForm.stakeholderIds),
        suggestionIds: [...convertFeedbackForm.suggestionIds],
        title: convertFeedbackForm.title,
      });
    }
    MessagePlugin.success('反馈已转为后续需求');
    convertFeedbackVisible.value = false;
    requirementFeedback.value = await listRequirementFeedbackApi(selectedRequirement.value.id);
    const suggestions = await listFeatureSuggestionsApi({
      projectId: selectedRequirement.value.projectId,
      requirementId: selectedRequirement.value.id,
    });
    requirementFeedbackMap.value = {
      ...requirementFeedbackMap.value,
      [selectedRequirement.value.id]: requirementFeedback.value,
    };
    requirementSuggestionMap.value = {
      ...requirementSuggestionMap.value,
      [selectedRequirement.value.id]: suggestions,
    };
    await loadRequirements();
  } finally {
    convertingFeedback.value = false;
  }
}
onMounted(async () => {
  [users.value, digitalWorkers.value] = await Promise.all([
    listUserOptionsApi(),
    listDigitalWorkersApi(),
  ]);
  await loadProjects();
  await loadRequirements();
});

watch(
  () => requirementForm.projectId,
  (projectId) => {
    if (selectedRequirement.value) return;
    const endpointId = endpoints.value.find((endpoint) => endpoint.projectId === projectId)?.id || '';
    const endpoint = endpoints.value.find((item) => item.id === endpointId);
    requirementForm.endpointId = endpointId;
    requirementForm.skillIds = [...(endpoint?.skillIds || [])];
    requirementForm.moduleId =
      modules.value.find(
        (module) => module.projectId === projectId && module.endpointId === endpointId,
      )?.id || '';
  },
);

watch(
  () => requirementForm.endpointId,
  (endpointId) => {
    if (selectedRequirement.value) return;
    const endpoint = endpoints.value.find((item) => item.id === endpointId);
    requirementForm.skillIds = [...(endpoint?.skillIds || [])];
    requirementForm.moduleId =
      modules.value.find(
        (module) => module.projectId === requirementForm.projectId && module.endpointId === endpointId,
      )?.id || '';
  },
);

watch(selectedProjectId, async () => {
  syncCurrentEndpoint();
  await loadRequirements();
});

watch(requirements, syncExpandedRequirementIds);

onActivated(async () => {
  await loadRequirements();
});
</script>

<template>
  <div class="requirements-page sprint-list-page">
    <section class="sprint-page-title">
      <div>
        <h2 class="requirement-page-heading">
          <span>{{ requirementPageTitle }}</span>
          <TButton
            class="requirement-project-switch"
            shape="circle"
            size="small"
            theme="default"
            title="切换项目"
            variant="outline"
            @click="openProjectSelectDrawer"
          >
            <IconifyIcon icon="ant-design:swap-outlined" />
          </TButton>
        </h2>
        <p>维护需求、评审、拆解、测试闭环和验收后的产品反馈。</p>
      </div>
      <TButton theme="primary" :disabled="!selectedProjectId" @click="openCreate">
        <template #icon>
          <IconifyIcon icon="lucide:plus" />
        </template>
        新增需求
      </TButton>
    </section>
    <HeaderProjectSelect ref="pageProjectSelectRef" hide-trigger />

    <div class="sprint-project-workspace requirements-workspace">
    <section class="sprint-filter-panel">
      <div class="sprint-filter-grid">
        <div class="sprint-filter-field">
          <span>状态</span>
          <TSelect
            v-model="filters.status"
            :options="statusOptions"
            clearable
            placeholder="全部状态"
            @change="handleLocalFilterChange"
          />
        </div>
        <div class="sprint-filter-field">
          <span>需求信息</span>
          <TInput
            v-model="filters.requirementInfo"
            clearable
            placeholder="需求名、内容、干系人"
            @change="handleLocalFilterChange"
          />
        </div>
        <div class="sprint-filter-actions">
          <TButton theme="primary" :loading="loading" @click="queryRequirements">
            <template #icon>
              <IconifyIcon icon="lucide:search" />
            </template>
            查询
          </TButton>
          <TButton theme="default" :disabled="loading" @click="resetFilters">
            <template #icon>
              <IconifyIcon icon="lucide:rotate-ccw" />
            </template>
            重置
          </TButton>
        </div>
      </div>
    </section>

    <section class="sprint-table-panel">
      <div class="sprint-table-header">
        <div class="endpoint-toolbar-filter">
          <span>当前端:</span>
          <TSelect
            v-model="filters.endpointId"
            class="endpoint-toolbar-select"
            :disabled="currentProjectEndpointOptions.length === 0"
            :options="currentProjectEndpointOptions"
            placeholder="暂无端"
            @change="handleEndpointFilterChange"
          />
        </div>
        <div class="sprint-table-actions">
          <TButton
            theme="primary"
            :disabled="!canSubmitSelectedRequirement"
            @click="openSelectedReview"
          >
            <template #icon>
              <IconifyIcon icon="lucide:clipboard-check" />
            </template>
            立项推进
          </TButton>
          <TButton
            theme="primary"
            :disabled="!canDecomposeSelectedRequirement"
            @click="openSelectedDecompose"
          >
            <template #icon>
              <IconifyIcon icon="lucide:list-tree" />
            </template>
            任务拆解
          </TButton>
          <TButton
            theme="success"
            :disabled="!canAssignSelectedRequirement || isRequirementActionPending(selectedRequirementForAction?.id || '')"
            :loading="isRequirementActionPending(selectedRequirementForAction?.id || '')"
            @click="openSelectedAssignDialog"
          >
            <template #icon>
              <IconifyIcon icon="lucide:user-check" />
            </template>
            指派
          </TButton>
          <TButton
            theme="success"
            :disabled="!canCloseSelectedRequirement"
            @click="closeSelectedRequirement"
          >
            <template #icon>
              <IconifyIcon icon="lucide:check-circle" />
            </template>
            验收关闭
          </TButton>
          <TButton
            theme="warning"
            :disabled="!canCreateSelectedFeedback"
            @click="openSelectedFeedback"
          >
            <template #icon>
              <IconifyIcon icon="lucide:message-square" />
            </template>
            记录回馈
          </TButton>
          <TButton
            theme="danger"
            variant="outline"
            :disabled="!canDeleteDraftSelectedRequirement || isRequirementActionPending(selectedRequirementForAction?.id || '')"
            :loading="isRequirementActionPending(selectedRequirementForAction?.id || '')"
            @click="deleteSelectedDraftRequirement"
          >
            <template #icon>
              <IconifyIcon icon="lucide:trash-2" />
            </template>
            删除
          </TButton>
          <TButton
            theme="danger"
            :disabled="!canVoidSelectedRequirement || isRequirementActionPending(selectedRequirementForAction?.id || '')"
            :loading="isRequirementActionPending(selectedRequirementForAction?.id || '')"
            @click="voidSelectedRequirement"
          >
            <template #icon>
              <IconifyIcon icon="lucide:ban" />
            </template>
            作废
          </TButton>
          <TButton shape="circle" variant="outline" title="刷新" :loading="loading" @click="loadRequirements">
            <IconifyIcon icon="lucide:refresh-cw" />
          </TButton>
        </div>
      </div>

      <TTable
        row-key="id"
        class="sprint-compact-table"
        :columns="withSerialColumn(columns, { offset: () => (pagination.current - 1) * pagination.pageSize })"
        :data="requirements"
        :expand-on-row-click="false"
        :expanded-row-keys="expandedRequirementIds"
        :loading="loading"
        :pagination="tablePagination"
        :selected-row-keys="selectedRequirementKeys"
        size="small"
        row-selection-type="single"
        select-on-row-click
        hover
        stripe
        @expand-change="handleExpandedRowKeysChange"
        @page-change="handlePageChange"
        @select-change="selectedRequirementKeys = $event"
      >
        <template #expandedRow="{ row }">
          <div class="requirement-expanded">
            <section class="expanded-section">
              <h4>任务拆解</h4>
              <div v-if="getRequirementTasks(row.id).length === 0" class="expanded-empty">
                暂无拆解任务
              </div>
              <div
                v-for="task in getRequirementTasks(row.id)"
                :key="task.id"
                class="expanded-task-row"
              >
                <TTag variant="light">{{ taskStatusText[task.status] || task.status }}</TTag>
                <strong>{{ task.title }}</strong>
                <span>指派人: {{ resolveTaskAssigneeName(task) }}</span>
                <span>{{ resolvePriorityText(task.priority) }}</span>
                <TSpace class="sprint-row-actions expanded-actions">
                  <TLink v-if="task.status !== 'completed'" theme="primary" @click="goTaskAdvance(task)">
                    <IconifyIcon icon="lucide:play" />
                    任务推进
                  </TLink>
                </TSpace>
                <p>{{ task.description || '暂无任务说明' }}</p>
              </div>
            </section>

            <section class="expanded-section">
              <h4>反馈与子需求</h4>
              <div v-if="getRequirementFollowUpItems(row).length === 0" class="expanded-empty">
                暂无回馈与子需求
              </div>
              <div
                v-for="item in getRequirementFollowUpItems(row)"
                :key="item.id"
                class="expanded-item"
              >
                <TTag :theme="item.type === 'feedback' ? 'warning' : 'primary'" variant="light">
                  {{ item.type === 'feedback' ? '反馈' : '需求' }}
                </TTag>
                <TTag variant="light">
                  {{
                    item.type === 'feedback'
                      ? feedbackStatusText[item.status] || item.status
                      : statusText[item.status] || item.status
                  }}
                </TTag>
                <strong>{{ item.title }}</strong>
                <span>{{ formatDateTime(item.createdAt) }}</span>
                <TSpace class="sprint-row-actions expanded-actions">
                  <template v-if="item.type === 'feedback'">
                    <TLink
                      v-if="item.feedback.status === 'open'"
                      theme="primary"
                      @click="openConvertFeedbackFromRequirement(row, item.feedback)"
                    >
                      <IconifyIcon icon="lucide:arrow-right" />
                      转需求
                    </TLink>
                  </template>
                  <template v-else>
                    <TLink
                      v-if="canSubmitReview(item.child)"
                      theme="primary"
                      @click="openReview(item.child)"
                    >
                      <IconifyIcon icon="lucide:clipboard-check" />
                      提交评审
                    </TLink>
                    <TLink
                      v-if="decomposeAllowedStatuses.has(item.child.status)"
                      theme="primary"
                      @click="openDecompose(item.child)"
                    >
                      <IconifyIcon icon="lucide:list-tree" />
                      任务拆解
                    </TLink>
                    <TLink
                      v-if="item.child.status === 'developing' || item.child.status === 'pending_fix'"
                      theme="success"
                      @click="completeRequirementDevelopment(item.child)"
                    >
                      <IconifyIcon icon="lucide:check" />
                      完成开发
                    </TLink>
                    <TLink
                      v-if="item.child.status === 'tested'"
                      theme="success"
                      @click="closeRequirement(item.child)"
                    >
                      <IconifyIcon icon="lucide:check-circle" />
                      验收关闭
                    </TLink>
                    <TLink theme="primary" @click="openDetail(item.child)">
                      <IconifyIcon icon="lucide:eye" />
                      详情
                    </TLink>
                  </template>
                </TSpace>
                <p>{{ item.content }}</p>
              </div>
            </section>
          </div>
        </template>
        <template #endpointId="{ row }">
          {{ resolveEndpointName(row.endpointId) }}
        </template>
        <template #status="{ row }">
          <TTag variant="light">{{ statusText[row.status] || row.status }}</TTag>
        </template>
        <template #priority="{ row }">
          <TTag variant="light">{{ resolvePriorityText(row.priority) }}</TTag>
        </template>
        <template #createdBy="{ row }">
          {{ resolveUserName(row.createdBy) }}
        </template>
        <template #createTime="{ row }">
          {{ formatDateTime(row.createTime) }}
        </template>
        <template #stakeholders="{ row }">
          {{ resolveStakeholderNames(row.stakeholders) }}
        </template>
        <template #actions="{ row }">
          <TSpace class="sprint-row-actions">
            <TLink v-if="canEditRequirement(row)" theme="primary" @click="openEdit(row)">
              <IconifyIcon icon="lucide:pencil" />
              编辑
            </TLink>
            <TLink v-else theme="primary" @click="openDetail(row)">
              <IconifyIcon icon="lucide:eye" />
              详情
            </TLink>
          </TSpace>
        </template>
      </TTable>
    </section>

    <TDrawer
      v-model:visible="editorVisible"
      :size="'72%'"
      :header="selectedRequirement ? '编辑需求' : '新增需求'"
      :confirm-btn="{ content: '保存', loading: requirementSaving }"
      @confirm="saveRequirement"
    >
      <TForm ref="requirementFormRef" :data="requirementForm" :rules="requirementRules" label-width="90px">
        <div class="requirement-relation-row">
          <TFormItem label="端" name="endpointId">
            <TSelect
              v-model="requirementForm.endpointId"
              :disabled="!!selectedRequirement"
              :options="endpointOptions"
              placeholder="请选择端"
            />
          </TFormItem>
          <TFormItem label="功能模块" name="moduleId">
            <TSelect
              v-model="requirementForm.moduleId"
              :disabled="!!selectedRequirement"
              :options="moduleOptions"
              placeholder="请选择功能模块"
            />
          </TFormItem>
        </div>
        <TFormItem label="需求标题" name="title">
          <TInput v-model="requirementForm.title" />
        </TFormItem>
        <TFormItem label="优先级" name="priority">
          <div class="priority-options">
            <TButton
              v-for="item in priorityOptions"
              :key="item.value"
              :class="getPriorityButtonClass(requirementForm.priority, item.value)"
              :theme="getPriorityTheme(requirementForm.priority, item.value, item.theme)"
              :variant="requirementForm.priority === item.value ? 'base' : 'outline'"
              @click="requirementForm.priority = item.value"
            >
              {{ item.label }}
            </TButton>
          </div>
        </TFormItem>
        <TFormItem label="干系人">
          <TSelect
            v-model="requirementForm.stakeholderIds"
            multiple
            filterable
            :options="userOptions"
            placeholder="选择干系人"
          />
        </TFormItem>
        <TFormItem label="Skill">
          <TSelect v-model="requirementForm.skillIds" multiple filterable :options="skillOptions">
            <template #option="{ option }">
              <SkillSelectOption :skill="option.skill" />
            </template>
          </TSelect>
        </TFormItem>
        <TFormItem v-if="!selectedRequirement" label="需要评审">
          <TSwitch v-model="requirementForm.requiresReview" />
        </TFormItem>
        <TFormItem label="需求内容" class="markdown-form-item">
          <MarkdownEditor
            v-model="requirementForm.description"
            :height="560"
            placeholder="填写需求背景、功能范围、验收标准和约束。"
          />
        </TFormItem>
      </TForm>
    </TDrawer>

    <TDrawer
      v-model:visible="detailVisible"
      :size="'60%'"
      :header="selectedRequirement?.title || '需求详情'"
    >
      <article v-if="selectedRequirement" class="detail">
        <h3>{{ selectedRequirement.title }}</h3>
        <article
          class="markdown-preview detail-markdown"
          v-html="renderMarkdown(selectedRequirement.description || '暂无需求内容')"
        ></article>
        <dl>
          <dt>状态</dt>
          <dd>{{ statusText[selectedRequirement.status] || selectedRequirement.status }}</dd>
          <dt>产品经理</dt>
          <dd>{{ resolveUserName(selectedRequirement.createdBy) }}</dd>
          <dt>干系人</dt>
          <dd>{{ resolveStakeholderNames(selectedRequirement.stakeholders) }}</dd>
        </dl>
        <section v-if="requirementReviews.length > 0" class="review-history">
          <h4>评审记录</h4>
          <div
            v-for="review in requirementReviews"
            :key="review.id"
            class="review-history__item"
          >
            <TTag variant="light">{{ reviewStatusText[review.status] || review.status }}</TTag>
            <strong>
              {{ userMap[review.reviewerId]?.displayName || review.reviewerId }}
            </strong>
            <span>{{ formatDateTime(review.reviewedAt || review.createTime) }}</span>
            <p>{{ review.comment || '暂无意见' }}</p>
          </div>
        </section>
        <section class="feedback-history">
          <h4>产品回馈</h4>
          <div v-if="requirementFeedback.length === 0" class="feedback-empty">
            暂无回馈
          </div>
          <div
            v-for="feedback in requirementFeedback"
            :key="feedback.id"
            class="feedback-history__item"
          >
            <TTag variant="light">
              {{ feedbackStatusText[feedback.status] || feedback.status }}
            </TTag>
            <strong>{{ feedback.title }}</strong>
            <span>{{ formatDateTime(feedback.createTime) }}</span>
            <p>{{ feedback.content || '暂无内容' }}</p>
            <TButton
              v-if="feedback.status === 'open'"
              size="small"
              theme="primary"
              variant="outline"
              @click="openConvertFeedback(feedback)"
            >
              <template #icon>
                <IconifyIcon icon="lucide:arrow-right" />
              </template>
              转后续需求
            </TButton>
          </div>
        </section>
        <div class="detail-actions">
          <TSpace>
            <TButton
              v-if="canEditRequirement(selectedRequirement)"
              theme="primary"
              @click="openEdit(selectedRequirement)"
            >
              <template #icon>
                <IconifyIcon icon="lucide:pencil" />
              </template>
              编辑
            </TButton>
            <TButton
              v-if="canSubmitReview(selectedRequirement)"
              theme="primary"
              @click="openReview(selectedRequirement)"
            >
              <template #icon>
                <IconifyIcon icon="lucide:clipboard-check" />
              </template>
              立项推进
            </TButton>
            <TButton
              v-if="selectedRequirement.status === 'draft'"
              theme="danger"
              variant="outline"
              :disabled="isRequirementActionPending(selectedRequirement.id)"
              :loading="isRequirementActionPending(selectedRequirement.id)"
              @click="deleteDraftRequirement(selectedRequirement)"
            >
              <template #icon>
                <IconifyIcon icon="lucide:trash-2" />
              </template>
              删除草稿
            </TButton>
            <TButton
              v-if="selectedRequirement.status === 'rejected'"
              theme="danger"
              :disabled="isRequirementActionPending(selectedRequirement.id)"
              :loading="isRequirementActionPending(selectedRequirement.id)"
              @click="voidRequirement(selectedRequirement)"
            >
              <template #icon>
                <IconifyIcon icon="lucide:ban" />
              </template>
              作废需求
            </TButton>
            <TButton
              v-if="selectedRequirement.status === 'tested'"
              theme="success"
              :disabled="isRequirementActionPending(selectedRequirement.id)"
              :loading="isRequirementActionPending(selectedRequirement.id)"
              @click="closeRequirement(selectedRequirement)"
            >
              <template #icon>
                <IconifyIcon icon="lucide:check-circle" />
              </template>
              验收关闭
            </TButton>
            <TButton
              v-if="canCreateFeedback(selectedRequirement)"
              theme="primary"
              variant="outline"
              @click="openFeedback(selectedRequirement)"
            >
              <template #icon>
                <IconifyIcon icon="lucide:message-square" />
              </template>
              记录回馈
            </TButton>
          </TSpace>
        </div>
      </article>
      <template #footer>
        <TButton theme="default" @click="detailVisible = false">
          关闭
        </TButton>
      </template>
    </TDrawer>

    <TDialog
      v-model:visible="reviewVisible"
      header="提交需求评审"
      :confirm-btn="{ content: '提交', loading: reviewSubmitting }"
      @confirm="submitReview"
    >
      <TForm ref="reviewFormRef" :data="reviewForm" :rules="reviewRules" label-width="90px">
        <TFormItem label="评审人" name="reviewerIds">
          <TSelect
            v-model="reviewForm.reviewerIds"
            multiple
            :options="userOptions"
            placeholder="选择评审人"
          />
        </TFormItem>
        <TFormItem label="提交缘由" name="reason">
          <TTextarea
            v-model="reviewForm.reason"
            placeholder="填写本次提交评审的背景、变更点或需要重点关注的问题"
            :autosize="{ minRows: 4, maxRows: 8 }"
          />
        </TFormItem>
      </TForm>
    </TDialog>

    <TDrawer
      v-model:visible="decomposeVisible"
      :size="'40%'"
      header="AI 任务预拆解"
    >
      <TSteps class="decomposition-steps" :current="decompositionStepCurrent" readonly>
        <TStepItem title="拆解" content="查看需求并生成预拆解草案" />
        <TStepItem title="确认" content="检查并调整任务草案" />
        <TStepItem title="任务分派" content="确认指派方式并写入开发任务" />
      </TSteps>
      <section v-if="isDecompositionDecomposeStep" class="decomposition-requirement">
        <TDivider align="left">需求内容</TDivider>
        <MarkdownEditor
          class="decomposition-requirement-preview"
          :model-value="selectedRequirement?.description || '暂无需求内容'"
          height="260px"
          preview
          preview-only
          read-only
          placeholder="暂无需求内容"
        />
      </section>
      <section class="decomposition-workspace">
        <div v-if="isDecompositionDecomposeStep" class="decomposition-workspace__settings">
          <TDivider align="left">拆解补充要求</TDivider>
          <MarkdownEditor
            class="decomposition-instruction-editor"
            v-model="decomposeForm.instruction"
            :height="380"
            placeholder="填写拆解补充要求，留空则按需求内容生成预拆解草案。"
            :read-only="isDecompositionInputLocked"
          />
          <div v-if="decompositionPreviewMessage" class="decomposition-preview-toolbar">
            {{ decompositionPreviewMessage }}
          </div>
        </div>
        <div v-if="isDecompositionConfirmStep" class="decomposition-drafts">
          <TDivider align="left">{{ decompositionDraftTitle }}</TDivider>
          <TCollapse class="decomposition-draft-collapse" expand-mutex>
            <TCollapsePanel
              v-for="(task, index) in decompositionDrafts"
              :key="index"
              :value="String(index)"
            >
              <template #header>
                <div class="decomposition-draft-header">
                  <div class="decomposition-draft-title">
                    <TInput
                      v-if="editingDecompositionDraftTitles[index]"
                      v-model="task.title"
                      placeholder="任务标题"
                      @blur="toggleDecompositionDraftTitleEdit(index, false)"
                      @click.stop
                    />
                    <template v-else>
                      <span class="decomposition-draft-title__text">{{ task.title || '未命名任务' }}</span>
                      <TButton
                        shape="circle"
                        size="small"
                        theme="default"
                        variant="text"
                        @click.stop="toggleDecompositionDraftTitleEdit(index, true)"
                      >
                        <IconifyIcon icon="lucide:pencil" />
                      </TButton>
                    </template>
                  </div>
                  <div class="decomposition-draft-priority" @click.stop>
                    <TButton
                      shape="circle"
                      size="small"
                      theme="danger"
                      variant="text"
                      @click.stop="removeDecompositionDraft(index)"
                    >
                      <IconifyIcon icon="lucide:trash-2" />
                    </TButton>
                  </div>
                </div>
              </template>
              <div class="decomposition-draft-editor">
                <MarkdownEditor
                  v-model="task.description"
                  :height="260"
                  placeholder="使用 Markdown 编写任务描述、验收点和补充说明。"
                />
              </div>
            </TCollapsePanel>
          </TCollapse>
          <TButton
            block
            class="decomposition-draft-add"
            theme="default"
            variant="dashed"
            @click="addDecompositionDraft"
          >
            <template #icon>
              <IconifyIcon icon="lucide:plus" />
            </template>
            添加
          </TButton>
        </div>
        <section v-if="isDecompositionAssignStep" class="decomposition-assignment">
          <TDivider align="left">任务分派</TDivider>
          <TForm :data="assignForm" label-width="90px">
            <TFormItem label="指派方式">
              <TSelect
                v-model="assignForm.assignmentMode"
                :options="[
                  { label: '自动指派', value: 'auto' },
                  { label: '手动指派', value: 'manual' },
                ]"
              />
            </TFormItem>
            <div v-if="assignForm.assignmentMode === 'auto'" class="assignment-auto-summary">
              {{ autoAssignmentDescription }}
            </div>
            <template v-else>
              <TFormItem label="指派对象">
                <TSelect
                  v-model="assignForm.assigneeType"
                  :options="[
                    { label: '员工', value: 0 },
                    { label: '数字员工', value: 1 },
                  ]"
                  @change="handleAssignAssigneeTypeChange"
                />
              </TFormItem>
              <TFormItem :label="assignForm.assigneeType === 1 ? '数字员工' : '员工'">
                <TSelect
                  v-model="assignForm.assigneeId"
                  filterable
                  :options="assignAssigneeOptions"
                  :placeholder="assignForm.assigneeType === 1 ? '选择数字员工' : '选择员工'"
                />
              </TFormItem>
            </template>
          </TForm>
        </section>
      </section>
      <template #footer>
        <TSpace class="decomposition-footer" :size="8">
          <TButton
            v-if="isDecompositionConfirmStep && decompositionDrafts.length > 0"
            theme="primary"
            :loading="decomposing"
            @click="saveDecompositionDraftsAndContinue"
          >
            <template #icon>
              <IconifyIcon icon="lucide:play" />
            </template>
            开始创建
          </TButton>
          <TButton
            v-if="isDecompositionAssignStep"
            theme="primary"
            :loading="decomposing"
            @click="confirmDecompositionDrafts"
          >
            <template #icon>
              <IconifyIcon icon="lucide:check" />
            </template>
            确认创建任务
          </TButton>
          <TButton
            v-if="isDecompositionAssignStep"
            theme="default"
            :disabled="decomposing"
            @click="decompositionPhase = 'confirm'"
          >
            上一步
          </TButton>
          <TButton
            v-if="showDecompositionActionButtons"
            theme="primary"
            variant="outline"
            :disabled="isDecompositionInputLocked"
            :loading="decomposing"
            @click="streamPreviewDecomposition"
          >
            <template #icon>
              <IconifyIcon icon="lucide:sparkles" />
            </template>
            AI预拆解
          </TButton>
          <TButton theme="default" :disabled="decomposing" @click="decomposeVisible = false">
            取消
          </TButton>
        </TSpace>
      </template>
    </TDrawer>

    <TDialog
      v-model:visible="assignVisible"
      header="任务指派"
      width="560px"
      :confirm-btn="{ content: '确认指派', loading: decomposing }"
      @confirm="submitAssignRequirement"
    >
      <TForm :data="assignForm" label-width="90px">
        <TFormItem label="指派方式">
          <TSelect
            v-model="assignForm.assignmentMode"
            :options="[
              { label: '自动指派', value: 'auto' },
              { label: '手动指派', value: 'manual' },
            ]"
          />
        </TFormItem>
        <div v-if="assignForm.assignmentMode === 'auto'" class="assignment-auto-summary">
          {{ autoAssignmentDescription }}
        </div>
        <template v-else>
          <TFormItem label="指派对象">
            <TSelect
              v-model="assignForm.assigneeType"
              :options="[
                { label: '员工', value: 0 },
                { label: '数字员工', value: 1 },
              ]"
              @change="handleAssignAssigneeTypeChange"
            />
          </TFormItem>
          <TFormItem :label="assignForm.assigneeType === 1 ? '数字员工' : '员工'">
            <TSelect
              v-model="assignForm.assigneeId"
              filterable
              :options="assignAssigneeOptions"
              :placeholder="assignForm.assigneeType === 1 ? '选择数字员工' : '选择员工'"
            />
          </TFormItem>
        </template>
      </TForm>
    </TDialog>

    <TDrawer
      v-model:visible="feedbackVisible"
      :size="'60%'"
      header="记录产品回馈"
      :confirm-btn="{ content: '保存', loading: feedbackSaving }"
      @confirm="saveFeedback"
    >
      <TForm ref="feedbackFormRef" :data="feedbackForm" :rules="feedbackRules" label-width="90px">
        <TFormItem label="标题" name="title">
          <TInput v-model="feedbackForm.title" />
        </TFormItem>
        <TFormItem label="内容" class="markdown-form-item">
          <MarkdownEditor
            v-model="feedbackForm.content"
            :height="420"
            placeholder="记录验收后的新想法、补充范围或优化建议"
          />
        </TFormItem>
      </TForm>
    </TDrawer>

    <TDrawer
      v-model:visible="convertFeedbackVisible"
      :size="'60%'"
      header="转为后续需求"
      :confirm-btn="{ content: '创建草稿', loading: convertingFeedback }"
      @confirm="convertFeedback"
    >
      <TForm
        ref="convertFeedbackFormRef"
        :data="convertFeedbackForm"
        :rules="convertFeedbackRules"
        label-width="90px"
      >
        <TFormItem label="标题" name="title">
          <TInput v-model="convertFeedbackForm.title" />
        </TFormItem>
        <TFormItem label="回馈来源">
          <TSelect
            v-model="convertFeedbackForm.feedbackIds"
            :options="convertFeedbackOptions"
            clearable
            multiple
          />
        </TFormItem>
        <TFormItem label="建议来源">
          <TSelect
            v-model="convertFeedbackForm.suggestionIds"
            :options="convertSuggestionOptions"
            clearable
            multiple
          />
        </TFormItem>
        <TFormItem label="优先级" name="priority">
          <div class="priority-options">
            <TButton
              v-for="item in priorityOptions"
              :key="item.value"
              :class="getPriorityButtonClass(convertFeedbackForm.priority, item.value)"
              :theme="getPriorityTheme(convertFeedbackForm.priority, item.value, item.theme)"
              :variant="convertFeedbackForm.priority === item.value ? 'base' : 'outline'"
              @click="convertFeedbackForm.priority = item.value"
            >
              {{ item.label }}
            </TButton>
          </div>
        </TFormItem>
        <TFormItem label="干系人">
          <TSelect
            v-model="convertFeedbackForm.stakeholderIds"
            multiple
            filterable
            :options="userOptions"
            placeholder="选择干系人"
          />
        </TFormItem>
        <TFormItem label="备注">
          <TTextarea
            v-model="convertFeedbackForm.remark"
            class="drawer-textarea drawer-textarea--short"
            placeholder="填写转需求备注"
          />
        </TFormItem>
        <TFormItem label="需求内容" class="markdown-form-item">
          <MarkdownEditor
            v-model="convertFeedbackForm.description"
            :height="360"
            placeholder="后续需求会保留来源需求和来源回馈"
          />
        </TFormItem>
      </TForm>
    </TDrawer>
    </div>
  </div>
</template>
<style scoped>
.requirements-page {
  display: flex;
  flex-direction: column;
}

.requirements-page .sprint-page-title {
  display: flex;
  gap: 16px;
  align-items: center;
  justify-content: space-between;
}

.requirement-page-heading {
  display: flex;
  max-width: 100%;
  align-items: center;
  gap: 8px;
  margin: 0;
}

.requirement-page-heading > span {
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.requirement-page-heading .t-button {
  flex: 0 0 auto;
}

.requirement-project-switch {
  width: 20px;
  height: 20px;
  min-width: 20px;
  padding: 0;
}

.requirement-project-switch .iconify {
  width: 11px;
  height: 11px;
}

.requirements-workspace {
  gap: 12px;
}

.endpoint-toolbar-filter {
  display: flex;
  min-width: 0;
  align-items: center;
  gap: 8px;
  color: var(--td-text-color-secondary);
}

.endpoint-toolbar-filter span {
  flex: 0 0 auto;
}

.endpoint-toolbar-select {
  width: 260px;
  max-width: 34vw;
}

.requirement-relation-row {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
}

.requirement-expanded {
  display: grid;
  gap: 14px;
  padding: 12px 16px;
  background: var(--td-bg-color-page);
}

.expanded-section h4 {
  margin: 0 0 10px;
  font-size: 14px;
  line-height: 20px;
}

.expanded-item,
.expanded-task-row {
  display: grid;
  grid-template-columns: auto auto minmax(180px, 1fr) auto auto;
  gap: 8px 10px;
  align-items: center;
  padding: 8px 0;
  border-top: 1px solid var(--td-component-border);
}

.expanded-task-row {
  grid-template-columns: auto minmax(220px, 1fr) 120px 90px auto;
}

.expanded-item p,
.expanded-task-row p {
  grid-column: 1 / -1;
  margin: 0;
  color: var(--td-text-color-secondary);
  line-height: 20px;
}

.expanded-actions {
  justify-content: flex-end;
}

.expanded-empty {
  color: var(--td-text-color-secondary);
}

.markdown-form-item :deep(.t-form__controls-content) {
  display: block;
}

.markdown-form-item :deep(.md-editor-content) {
  min-height: 480px;
}

.priority-options {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.priority-option-button--danger {
  color: var(--td-error-color);
}

.priority-option-button--success {
  color: var(--td-success-color);
}

.markdown-preview {
  min-height: 320px;
}

.drawer-textarea {
  min-height: 180px;
}

.drawer-textarea--short {
  min-height: 88px;
}

.markdown-preview {
  max-height: 520px;
  padding: 12px;
  overflow: auto;
  background: var(--td-bg-color-page);
  border: 1px solid var(--td-component-border);
  border-radius: 6px;
  word-break: break-word;
}

.detail h3 {
  margin: 14px 0;
}

.detail-markdown {
  min-height: 120px;
  margin-bottom: 14px;
}

.markdown-preview :deep(h1),
.markdown-preview :deep(h2),
.markdown-preview :deep(h3),
.markdown-preview :deep(p),
.markdown-preview :deep(ul) {
  margin-top: 0;
}

.markdown-preview :deep(code) {
  padding: 1px 4px;
  background: var(--td-bg-color-container-hover);
  border-radius: 4px;
}

.markdown-preview :deep(pre) {
  padding: 10px;
  overflow: auto;
  background: var(--td-bg-color-container-hover);
  border-radius: 6px;
}

.detail dl {
  display: grid;
  grid-template-columns: 90px minmax(0, 1fr);
  gap: 10px;
}

.detail-actions {
  margin-top: 20px;
}

.decomposition-steps {
  max-width: 860px;
  margin: 0 auto 24px;
}

.decomposition-requirement,
.decomposition-assignment {
  margin-bottom: 18px;
}

.decomposition-requirement-preview {
  border: 1px solid var(--td-component-border);
  border-radius: 6px;
}

.decomposition-instruction-editor {
  width: 100%;
  border: 1px solid var(--td-component-border);
  border-radius: 6px;
}

.decomposition-workspace {
  display: grid;
  grid-template-columns: minmax(0, 1fr);
  gap: 18px;
  align-items: start;
}

.decomposition-workspace__settings {
  min-width: 0;
}

.decomposition-preview-toolbar {
  color: var(--td-error-color);
  font-size: 13px;
  margin-top: 16px;
}

.decomposition-drafts {
  display: grid;
  grid-template-columns: 1fr;
  gap: 14px;
  min-width: 0;
}

.decomposition-drafts :deep(.t-divider) {
  margin: 0 0 2px;
}

.decomposition-draft-collapse {
  width: 100%;
  max-width: 100%;
  min-width: 0;
  border: 1px solid var(--td-component-border);
  border-radius: 6px;
  overflow: hidden;
}

.decomposition-draft-header {
  display: grid;
  grid-template-columns: minmax(0, 1fr) max-content;
  gap: 10px;
  align-items: center;
  width: 100%;
  min-width: 0;
  overflow: hidden;
}

.decomposition-draft-title {
  display: flex;
  gap: 6px;
  align-items: center;
  min-width: 0;
}

.decomposition-draft-title__text {
  overflow: hidden;
  font-weight: 500;
  color: var(--td-text-color-primary);
  text-overflow: ellipsis;
  white-space: nowrap;
}

.decomposition-draft-priority {
  display: flex;
  gap: 10px;
  align-items: center;
  justify-content: flex-end;
  width: 150px;
  min-width: 150px;
}

.decomposition-draft-priority :deep(.t-select) {
  width: 108px;
}

.decomposition-draft-priority :deep(.t-button) {
  flex: 0 0 auto;
}

.decomposition-draft-add {
  width: 100%;
}

.decomposition-draft-collapse :deep(.t-collapse-panel),
.decomposition-draft-collapse :deep(.t-collapse-panel__wrapper),
.decomposition-draft-collapse :deep(.t-collapse-panel__body),
.decomposition-draft-collapse :deep(.t-collapse-panel__content) {
  width: 100%;
  max-width: 100%;
  min-width: 0;
  box-sizing: border-box;
  overflow-x: hidden;
}

.decomposition-draft-collapse :deep(.t-collapse-panel__content) {
  padding: 12px 16px 16px;
}

.decomposition-draft-editor {
  width: 100%;
  max-width: 100%;
  min-width: 0;
  overflow: hidden;
}

.decomposition-draft-editor :deep(.sprint-markdown-editor),
.decomposition-draft-editor :deep(.md-editor) {
  width: 100%;
  max-width: 100%;
  min-width: 0;
  box-sizing: border-box;
  overflow: hidden;
}

.decomposition-draft-editor :deep(.md-editor-content),
.decomposition-draft-editor :deep(.md-editor-content-wrapper),
.decomposition-draft-editor :deep(.md-editor-input-wrapper),
.decomposition-draft-editor :deep(.md-editor-toolbar),
.decomposition-draft-editor :deep(.md-editor-toolbar-left),
.decomposition-draft-editor :deep(.md-editor-toolbar-right),
.decomposition-draft-editor :deep(.cm-editor) {
  width: 100%;
  max-width: 100%;
  min-width: 0;
  box-sizing: border-box;
}

.decomposition-draft-editor :deep(.md-editor-toolbar) {
  overflow: hidden;
}

.decomposition-draft-editor :deep(.md-editor-toolbar-wrapper) {
  width: 100%;
  max-width: 100%;
  min-width: 0;
  overflow-x: auto;
  overflow-y: hidden;
}

.decomposition-draft-editor :deep(.cm-scroller) {
  overflow-x: auto;
}

.decomposition-footer {
  gap: 8px;
  justify-content: flex-end;
  width: auto;
  margin-left: auto;
}

.assignment-auto-summary {
  padding: 10px 12px;
  margin-bottom: 16px;
  color: var(--td-text-color-secondary);
  background: var(--td-bg-color-container-hover);
  border: 1px solid var(--td-component-border);
  border-radius: 6px;
}

.feedback-history,
.review-history {
  margin-top: 20px;
}

.feedback-history h4,
.review-history h4 {
  margin: 0 0 12px;
}

.feedback-history__item,
.review-history__item {
  display: grid;
  grid-template-columns: auto 120px minmax(160px, 1fr);
  gap: 8px 12px;
  align-items: center;
  padding: 12px 0;
  border-top: 1px solid var(--td-component-border);
}

.feedback-history__item p,
.review-history__item p {
  grid-column: 1 / -1;
  margin: 0;
  color: var(--td-text-color-secondary);
}

.feedback-empty {
  color: var(--td-text-color-secondary);
}

.detail dt {
  color: var(--td-text-color-secondary);
}

.detail dd {
  margin: 0;
}

@media (max-width: 960px) {
  .requirement-relation-row {
    grid-template-columns: 1fr;
  }

  .expanded-item {
    grid-template-columns: 1fr;
  }

  .expanded-task-row {
    grid-template-columns: 1fr;
  }

  .expanded-actions {
    justify-content: flex-start;
  }

  .decomposition-drafts {
    grid-template-columns: 1fr;
  }

  .decomposition-workspace {
    grid-template-columns: 1fr;
  }
}
</style>
