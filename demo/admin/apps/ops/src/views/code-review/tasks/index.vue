<script lang="ts" setup>
import type { PrimaryTableCol } from 'tdesign-vue-next';

import { computed, reactive, ref } from 'vue';
import { useRouter } from 'vue-router';

import {
  cancelCodeAuditTaskApi,
  createCodeAuditTaskApi,
  listCodeAuditTasksApi,
  retryCodeAuditTaskApi,
  type CodeReviewApi,
} from '#/api/code-review';
import { listDigitalWorkersApi, type AutomationApi } from '#/api/automation/workers';
import {
  listDevelopmentTasksApi,
  listFeatureModulesApi,
  listProjectsApi,
  type SprintMvpApi,
} from '#/api/sprint/mvp';
import AdminListPage from '#/components/admin-list-page/admin-list-page.vue';
import RowAction from '#/views/system/_shared/row-action.vue';
import {
  Button as TButton,
  Dialog as TDialog,
  Input as TInput,
  MessagePlugin,
  Select as TSelect,
  Space as TSpace,
  Tag as TTag,
  Textarea as TTextarea,
} from 'tdesign-vue-next';

defineOptions({ name: 'CodeReviewTasks' });

const router = useRouter();

type CodeReviewTaskRow = {
  auditTargetType: CodeReviewApi.AuditTargetType;
  branch?: string;
  id: string;
  projectId: string;
  status: CodeReviewApi.TaskStatus;
  targetId?: string;
  taskName: string;
  workerId: string;
};

const defaultFilters = {
  keyword: '',
  status: undefined as CodeReviewTaskRow['status'] | undefined,
};
const filters = reactive({ ...defaultFilters });
const query = reactive({ ...defaultFilters });
const pagination = reactive({
  current: 1,
  pageSize: 30,
  pageSizeOptions: [30, 50, 100, 200],
});
const rows = reactive<CodeReviewTaskRow[]>([]);
const createVisible = ref(false);
const creating = ref(false);
const projects = ref<SprintMvpApi.Project[]>([]);
const workers = ref<AutomationApi.DigitalWorker[]>([]);
const developmentTasks = ref<SprintMvpApi.DevelopmentTask[]>([]);
const modules = ref<SprintMvpApi.FeatureModule[]>([]);
const scopeText = ref('');
const createForm = reactive<CodeReviewApi.CreateCodeAuditTaskRequest>({
  auditTargetType: 'development_task',
  projectId: '',
  targetId: '',
  workerId: '',
  branch: '',
  instruction: '',
  scopeJson: '',
});

const statusOptions = [
  { label: '待审计', value: 'pending' },
  { label: '审计中', value: 'running' },
  { label: '通过', value: 'passed' },
  { label: '需修改', value: 'needs_changes' },
  { label: '阻断', value: 'blocked' },
  { label: '失败', value: 'failed' },
  { label: '已取消', value: 'cancelled' },
];
const auditTargetOptions = [
  { label: '任务 ID 审计', value: 'development_task' },
  { label: '文件审计', value: 'files' },
  { label: '文件夹审计', value: 'folders' },
  { label: '需求模块审计', value: 'requirement_module' },
  { label: '功能描述审计', value: 'feature_description' },
  { label: '发布前综合审计', value: 'release_preflight' },
];
const columns: PrimaryTableCol[] = [
  { colKey: 'taskName', title: '审计任务', minWidth: 220 },
  { colKey: 'projectId', title: '项目名称', minWidth: 160, cell: 'projectId' },
  { colKey: 'branch', title: '分支', minWidth: 150 },
  { colKey: 'workerId', title: '数字员工', minWidth: 180, cell: 'workerId' },
  { colKey: 'auditTargetType', title: '范围', cell: 'auditTargetType', width: 150 },
  { colKey: 'status', title: '状态', cell: 'status', width: 110 },
  { colKey: 'actions', title: '操作', cell: 'actions', width: 160 },
];

const filteredRows = computed(() => {
  const keyword = query.keyword.trim().toLowerCase();
  return rows.filter((row) => {
    if (query.status && row.status !== query.status) {
      return false;
    }

    if (!keyword) {
      return true;
    }

    return [
      row.taskName,
      row.projectId,
      resolveProjectName(row.projectId),
      row.branch,
      row.workerId,
      resolveWorkerName(row.workerId),
      row.targetId,
    ]
      .filter(Boolean)
      .join('\n')
      .toLowerCase()
      .includes(keyword);
  });
});
const tablePagination = computed(() => ({
  current: pagination.current,
  pageSize: pagination.pageSize,
  pageSizeOptions: pagination.pageSizeOptions,
  total: filteredRows.value.length,
}));
const projectOptions = computed(() =>
  projects.value.map((item) => ({
    label: `${item.name} (${item.code})`,
    value: item.id,
  })),
);
const projectMap = computed(() =>
  Object.fromEntries(projects.value.map((item) => [item.id, item])),
);
const workerOptions = computed(() =>
  auditWorkers.value.map((item) => ({
    label: `${item.name} (${item.code})`,
    value: item.id,
  })),
);
const workerMap = computed(() =>
  Object.fromEntries(workers.value.map((item) => [item.id, item])),
);
const auditWorkers = computed(() =>
  workers.value.filter(
    (item) =>
      (item.employeeType === 'audit' || item.workerType === 'codex') &&
      ['active', 'idle', 'working'].includes(item.status),
  ),
);
function resolveProjectName(projectId?: string) {
  if (!projectId) return '-';
  const project = projectMap.value[projectId];
  return project ? project.name : projectId;
}
function resolveWorkerName(workerId?: string) {
  if (!workerId) return '-';
  const worker = workerMap.value[workerId];
  return worker ? worker.name : workerId;
}
const taskOptions = computed(() =>
  developmentTasks.value
    .filter((item) => !createForm.projectId || item.projectId === createForm.projectId)
    .map((item) => ({
      label: `${item.title} (${item.status})`,
      value: item.id,
    })),
);
const moduleOptions = computed(() =>
  modules.value
    .filter((item) => !createForm.projectId || item.projectId === createForm.projectId)
    .map((item) => ({
      label: `${item.name} (${item.code})`,
      value: item.id,
    })),
);

function statusTheme(status?: CodeReviewTaskRow['status']) {
  if (status === 'passed') return 'success';
  if (status === 'running') return 'primary';
  if (status === 'pending' || status === 'needs_changes') return 'warning';
  if (status === 'blocked' || status === 'failed') return 'danger';
  return 'default';
}

function statusText(status?: CodeReviewTaskRow['status']) {
  return statusOptions.find((item) => item.value === status)?.label || '-';
}

function targetTypeText(value?: CodeReviewApi.AuditTargetType) {
  return auditTargetOptions.find((item) => item.value === value)?.label || value || '-';
}

function canCancel(row: CodeReviewTaskRow) {
  return row.status === 'pending';
}

function canRetry(row: CodeReviewTaskRow) {
  return ['pending', 'blocked', 'cancelled', 'failed'].includes(row.status);
}

function handlePageChange(pageInfo: { current: number; pageSize: number }) {
  pagination.current = pageInfo.current;
  pagination.pageSize = pageInfo.pageSize;
}

function applyFilters() {
  Object.assign(query, filters);
  pagination.current = 1;
}

function resetFilters() {
  Object.assign(filters, defaultFilters);
  applyFilters();
}

async function loadRows() {
  if (projects.value.length === 0 || workers.value.length === 0) {
    await loadLookupOptions();
  }

  const data = await listCodeAuditTasksApi({
    keyword: query.keyword,
    status: query.status,
  });
  rows.splice(
    0,
    rows.length,
    ...data.map((item) => ({
      auditTargetType: item.auditTargetType,
      branch: item.branch,
      id: item.id,
      projectId: item.projectId,
      status: item.status,
      targetId: item.targetId,
      taskName: item.targetId ? `${targetTypeText(item.auditTargetType)} / ${item.targetId}` : item.id,
      workerId: item.workerId,
    })),
  );
}

async function loadLookupOptions() {
  const [projectRows, workerRows] = await Promise.all([
    listProjectsApi(),
    listDigitalWorkersApi(),
  ]);
  projects.value = projectRows;
  workers.value = workerRows;
}

async function loadCreateOptions() {
  if (projects.value.length === 0 || workers.value.length === 0) {
    await loadLookupOptions();
  }

  createForm.projectId ||= projects.value[0]?.id || '';
  createForm.workerId ||= auditWorkers.value[0]?.id || '';
  await loadTargetOptionsForCreate();
}

async function loadTargetOptionsForCreate() {
  const projectId = createForm.projectId || undefined;
  const [taskRows, moduleRows] = await Promise.all([
    listDevelopmentTasksApi({ projectId }),
    listFeatureModulesApi(projectId),
  ]);
  developmentTasks.value = taskRows;
  modules.value = moduleRows;
  if (createForm.auditTargetType === 'development_task' && !taskOptions.value.some((item) => item.value === createForm.targetId)) {
    createForm.targetId = '';
  }

  if (createForm.auditTargetType === 'requirement_module' && !moduleOptions.value.some((item) => item.value === createForm.targetId)) {
    createForm.targetId = '';
  }
}

function openDetail(row: CodeReviewTaskRow) {
  router.push(`/code-review/results/detail/${row.id}`);
}

async function openCreate() {
  createVisible.value = true;
  await loadCreateOptions();
}

async function handleCreateProjectChange() {
  createForm.targetId = '';
  await loadTargetOptionsForCreate();
}

function handleAuditTargetChange() {
  createForm.targetId = '';
  createForm.scopeJson = '';
  createForm.instruction = '';
  scopeText.value = '';
}

function buildScopeJson() {
  const paths = scopeText.value
    .split(/\r?\n/)
    .map((item) => item.trim())
    .filter(Boolean);
  if (paths.length === 0) {
    return createForm.auditTargetType === 'release_preflight'
      ? JSON.stringify({ mode: 'release_preflight', paths: [] })
      : undefined;
  }

  return JSON.stringify({
    mode: createForm.auditTargetType === 'release_preflight' ? 'release_preflight' : createForm.auditTargetType,
    paths,
  });
}

async function submitCreate() {
  if (creating.value) return;
  if (!createForm.projectId || !createForm.workerId) {
    MessagePlugin.warning('项目和审计 Worker 不能为空');
    return;
  }

  if (createForm.auditTargetType === 'development_task' && !createForm.targetId) {
    MessagePlugin.warning('任务 ID 不能为空');
    return;
  }

  if (createForm.auditTargetType === 'requirement_module' && !createForm.targetId) {
    MessagePlugin.warning('需求模块不能为空');
    return;
  }

  if ((createForm.auditTargetType === 'files' || createForm.auditTargetType === 'folders') && !buildScopeJson()) {
    MessagePlugin.warning('请输入至少一个文件或文件夹路径');
    return;
  }

  if (createForm.auditTargetType === 'feature_description' && !createForm.instruction?.trim()) {
    MessagePlugin.warning('功能描述不能为空');
    return;
  }

  creating.value = true;
  try {
    const scopeJson = createForm.auditTargetType === 'files' ||
      createForm.auditTargetType === 'folders' ||
      createForm.auditTargetType === 'release_preflight'
      ? buildScopeJson()
      : undefined;
    await createCodeAuditTaskApi({
      auditTargetType: createForm.auditTargetType,
      projectId: createForm.projectId,
      workerId: createForm.workerId,
      targetId: createForm.targetId || undefined,
      branch: createForm.branch || undefined,
      scopeJson,
      instruction: createForm.instruction || undefined,
    });
    MessagePlugin.success('审计任务已发布');
    createVisible.value = false;
    await loadRows();
  } finally {
    creating.value = false;
  }
}

async function cancelTask(row: CodeReviewTaskRow) {
  await cancelCodeAuditTaskApi(row.id);
  MessagePlugin.success('审计任务已取消');
  await loadRows();
}

async function retryTask(row: CodeReviewTaskRow) {
  const task = await retryCodeAuditTaskApi(row.id);
  MessagePlugin.success(`已创建重试任务：${task.id}`);
  await loadRows();
}

loadRows();
</script>

<template>
  <AdminListPage
    title="代码审计任务"
    description="维护 AI 代码审查任务，按项目、分支、范围和执行状态跟踪审计进度。"
    table-title="代码审计任务列表"
    add-button-text="新增任务"
    :columns="columns"
    :data="filteredRows"
    :pagination="tablePagination"
    @add="openCreate"
    @page-change="handlePageChange"
    @refresh="loadRows"
    @reset="resetFilters"
    @search="() => { applyFilters(); loadRows(); }"
  >
    <template #filters>
      <label class="filter-field">
        <span>任务信息</span>
        <TInput v-model="filters.keyword" clearable placeholder="任务 / 项目 / 分支 / 数字员工" />
      </label>
      <label class="filter-field">
        <span>状态</span>
        <TSelect v-model="filters.status" clearable placeholder="全部状态" :options="statusOptions" />
      </label>
    </template>
    <template #projectId="{ row }">
      {{ resolveProjectName(row.projectId) }}
    </template>
    <template #workerId="{ row }">
      {{ resolveWorkerName(row.workerId) }}
    </template>
    <template #status="{ row }">
      <TTag :theme="statusTheme(row.status)" variant="light">{{ statusText(row.status) }}</TTag>
    </template>
    <template #auditTargetType="{ row }">
      <TTag variant="light">{{ targetTypeText(row.auditTargetType) }}</TTag>
    </template>
    <template #actions="{ row }">
      <TSpace>
        <RowAction icon="lucide:eye" label="详情" @click="openDetail(row)" />
        <RowAction v-if="canCancel(row)" icon="lucide:x" label="取消" @click="cancelTask(row)" />
        <RowAction v-if="canRetry(row)" icon="lucide:rotate-ccw" label="重试" @click="retryTask(row)" />
      </TSpace>
    </template>
  </AdminListPage>

  <TDialog
    v-model:visible="createVisible"
    header="发布代码审计任务"
    width="760px"
    :confirm-btn="{ content: '发布审计', loading: creating }"
    @confirm="submitCreate"
  >
    <div class="create-form">
      <label class="form-field">
        <span>项目</span>
        <TSelect
          v-model="createForm.projectId"
          filterable
          placeholder="选择项目"
          :options="projectOptions"
          @change="handleCreateProjectChange"
        />
      </label>
      <label class="form-field">
        <span>审计数字员工</span>
        <TSelect
          v-model="createForm.workerId"
          filterable
          placeholder="选择审计数字员工"
          :options="workerOptions"
        />
      </label>
      <label class="form-field">
        <span>审计范围</span>
        <TSelect
          v-model="createForm.auditTargetType"
          :options="auditTargetOptions"
          @change="handleAuditTargetChange"
        />
      </label>
      <label v-if="createForm.auditTargetType === 'development_task'" class="form-field">
        <span>任务 ID</span>
        <TSelect
          v-model="createForm.targetId"
          filterable
          placeholder="选择开发任务"
          :options="taskOptions"
        />
      </label>
      <label v-if="createForm.auditTargetType === 'requirement_module'" class="form-field">
        <span>需求模块</span>
        <TSelect
          v-model="createForm.targetId"
          filterable
          placeholder="选择需求模块"
          :options="moduleOptions"
        />
      </label>
      <label
        v-if="createForm.auditTargetType === 'files' || createForm.auditTargetType === 'folders' || createForm.auditTargetType === 'release_preflight'"
        class="form-field form-field-wide"
      >
        <span>{{ createForm.auditTargetType === 'files' ? '文件路径' : createForm.auditTargetType === 'folders' ? '文件夹路径' : '综合范围' }}</span>
        <TTextarea
          v-model="scopeText"
          :autosize="{ minRows: 4, maxRows: 8 }"
          placeholder="一行一个仓库相对路径；发布前综合审计可留空表示审计当前分支整体发布风险"
        />
      </label>
      <label class="form-field">
        <span>分支</span>
        <TInput v-model="createForm.branch" clearable placeholder="留空使用仓库默认分支" />
      </label>
      <label class="form-field form-field-wide">
        <span>{{ createForm.auditTargetType === 'feature_description' ? '功能描述' : '补充要求' }}</span>
        <TTextarea
          v-model="createForm.instruction"
          :autosize="{ minRows: 3, maxRows: 8 }"
          placeholder="补充审计重点、功能说明或人工关注点"
        />
      </label>
      <div class="form-hint">
        Worker 只读执行审计，不提交、不推送，也不改变开发任务状态。文件和文件夹审计会把路径列表写入审计范围快照。
      </div>
    </div>
    <template #footer>
      <TSpace>
        <TButton variant="outline" @click="createVisible = false">取消</TButton>
        <TButton theme="primary" :loading="creating" @click="submitCreate">发布审计</TButton>
      </TSpace>
    </template>
  </TDialog>

</template>

<style scoped>
.filter-field {
  display: grid;
  grid-template-columns: auto minmax(180px, 260px);
  gap: 8px;
  align-items: center;
}

.create-form {
  display: grid;
  gap: 14px;
}

.form-field {
  display: grid;
  grid-template-columns: 96px minmax(0, 1fr);
  gap: 10px;
  align-items: center;
}

.form-field-wide {
  align-items: start;
}

.form-hint {
  padding: 10px 12px;
  color: var(--td-text-color-secondary);
  background: var(--td-bg-color-container-hover);
  border-radius: 6px;
}

</style>
