<script lang="ts" setup>
import type { PrimaryTableCol } from 'tdesign-vue-next';

import { computed, reactive, ref } from 'vue';

import {
  createCodeAuditIndexSyncCommandApi,
  createCodeAuditTaskApi,
  listCodeAuditFilesApi,
  type CodeReviewApi,
} from '#/api/code-review';
import { listDigitalWorkersApi, type AutomationApi } from '#/api/automation/workers';
import { listProjectsApi, type SprintMvpApi } from '#/api/sprint/mvp';
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
} from 'tdesign-vue-next';

defineOptions({ name: 'CodeReviewFiles' });

type AuditStatus = CodeReviewApi.CodeAuditFile['auditStatus'];

const defaultFilters = {
  auditStatus: undefined as AuditStatus | undefined,
  branch: '',
  fileType: '',
  keyword: '',
  projectId: '',
};
const filters = reactive({ ...defaultFilters });
const query = reactive({ ...defaultFilters });
const pagination = reactive({
  current: 1,
  pageSize: 30,
  pageSizeOptions: [30, 50, 100, 200],
});
const rows = ref<CodeReviewApi.CodeAuditFile[]>([]);
const selectedRowKeys = ref<Array<number | string>>([]);
const projects = ref<SprintMvpApi.Project[]>([]);
const workers = ref<AutomationApi.DigitalWorker[]>([]);
const syncVisible = ref(false);
const auditVisible = ref(false);
const syncing = ref(false);
const creatingAudit = ref(false);
const syncForm = reactive({
  branch: '',
  projectId: '',
  workerId: '',
});
const auditForm = reactive({
  branch: '',
  instruction: '',
  projectId: '',
  workerId: '',
});

const statusOptions = [
  { label: '未审计', value: 'not_audited' },
  { label: '异常', value: 'abnormal' },
  { label: '正常', value: 'normal' },
  { label: '已删除', value: 'deleted' },
];
const columns: PrimaryTableCol[] = [
  { colKey: 'filePath', title: '文件路径', minWidth: 320 },
  { colKey: 'projectId', title: '项目', minWidth: 150 },
  { colKey: 'branch', title: '分支', width: 140 },
  { colKey: 'fileType', title: '类型', width: 90 },
  { colKey: 'auditStatus', title: '状态', cell: 'auditStatus', width: 110 },
  { colKey: 'issueCount', title: '问题数', width: 90 },
  { colKey: 'lastCommitId', title: '最后提交', minWidth: 180 },
  { colKey: 'actions', title: '操作', cell: 'actions', width: 120 },
];

const tablePagination = computed(() => ({
  current: pagination.current,
  pageSize: pagination.pageSize,
  pageSizeOptions: pagination.pageSizeOptions,
  total: rows.value.length,
}));
const projectOptions = computed(() =>
  projects.value.map((item) => ({
    label: `${item.name} (${item.code})`,
    value: item.id,
  })),
);
const workerOptions = computed(() =>
  workers.value.map((item) => ({
    label: `${item.name} (${item.code})`,
    value: item.id,
  })),
);
const selectedFiles = computed(() =>
  rows.value.filter((item) => selectedRowKeys.value.includes(item.id)),
);

function statusTheme(status?: AuditStatus) {
  if (status === 'normal') return 'success';
  if (status === 'not_audited') return 'warning';
  if (status === 'abnormal') return 'danger';
  return 'default';
}

function statusText(status?: AuditStatus) {
  return statusOptions.find((item) => item.value === status)?.label || '-';
}

function handlePageChange(pageInfo: { current: number; pageSize: number }) {
  pagination.current = pageInfo.current;
  pagination.pageSize = pageInfo.pageSize;
}

function applyFilters() {
  Object.assign(query, filters);
  pagination.current = 1;
  selectedRowKeys.value = [];
}

function resetFilters() {
  Object.assign(filters, defaultFilters);
  applyFilters();
}

async function loadRows() {
  rows.value = await listCodeAuditFilesApi({
    auditStatus: query.auditStatus,
    branch: query.branch,
    fileType: query.fileType,
    keyword: query.keyword,
    projectId: query.projectId,
  });
}

async function loadOptions() {
  const [projectRows, workerRows] = await Promise.all([
    listProjectsApi(),
    listDigitalWorkersApi(),
  ]);
  projects.value = projectRows;
  workers.value = workerRows.filter(
    (item) =>
      (item.employeeType === 'audit' || item.workerType === 'codex') &&
      ['active', 'idle', 'working'].includes(item.status),
  );
}

async function openSync() {
  await loadOptions();
  syncForm.projectId = query.projectId || projects.value[0]?.id || '';
  syncForm.workerId = workers.value[0]?.id || '';
  syncForm.branch = query.branch || '';
  syncVisible.value = true;
}

async function submitSync() {
  if (syncing.value) return;
  if (!syncForm.projectId || !syncForm.workerId) {
    MessagePlugin.warning('项目和 Worker 不能为空');
    return;
  }

  syncing.value = true;
  try {
    const command = await createCodeAuditIndexSyncCommandApi({
      branch: syncForm.branch || undefined,
      projectId: syncForm.projectId,
      workerId: syncForm.workerId,
    });
    MessagePlugin.success(`索引同步命令已创建：${command.id}`);
    syncVisible.value = false;
  } finally {
    syncing.value = false;
  }
}

async function openBatchAudit(file?: CodeReviewApi.CodeAuditFile) {
  await loadOptions();
  const baseFile = file || selectedFiles.value[0];
  auditForm.projectId = baseFile?.projectId || query.projectId || projects.value[0]?.id || '';
  auditForm.branch = baseFile?.branch || query.branch || '';
  auditForm.workerId = workers.value[0]?.id || '';
  auditForm.instruction = '';
  if (file) {
    selectedRowKeys.value = [file.id];
  }

  auditVisible.value = true;
}

async function submitBatchAudit() {
  if (creatingAudit.value) return;
  const files = selectedFiles.value.filter((item) => item.auditStatus !== 'deleted');
  if (!auditForm.projectId || !auditForm.workerId || files.length === 0) {
    MessagePlugin.warning('项目、Worker 和审计文件不能为空');
    return;
  }

  creatingAudit.value = true;
  try {
    await createCodeAuditTaskApi({
      auditTargetType: 'files',
      branch: auditForm.branch || undefined,
      instruction: auditForm.instruction || undefined,
      projectId: auditForm.projectId,
      scopeJson: JSON.stringify({ mode: 'files', paths: files.map((item) => item.filePath) }),
      workerId: auditForm.workerId,
    });
    MessagePlugin.success('文件审计任务已发布');
    auditVisible.value = false;
    selectedRowKeys.value = [];
    await loadRows();
  } finally {
    creatingAudit.value = false;
  }
}

loadOptions();
loadRows();
</script>

<template>
  <AdminListPage
    title="代码审计文件"
    description="管理仓库文件审计状态，支持同步索引、筛选未审计或异常文件，并批量发布文件审计任务。"
    table-title="代码审计文件列表"
    add-button-text="同步索引"
    :columns="columns"
    :data="rows"
    :pagination="tablePagination"
    row-key="id"
    :row-selection="{ type: 'multiple' }"
    :selected-row-keys="selectedRowKeys"
    @add="openSync"
    @page-change="handlePageChange"
    @refresh="loadRows"
    @reset="resetFilters"
    @search="() => { applyFilters(); loadRows(); }"
    @select-change="(keys: Array<number | string>) => { selectedRowKeys = keys; }"
  >
    <template #toolbar>
      <TButton :disabled="selectedFiles.length === 0" theme="primary" @click="openBatchAudit()">
        批量发布审计
      </TButton>
    </template>
    <template #filters>
      <label class="filter-field">
        <span>项目</span>
        <TSelect v-model="filters.projectId" clearable filterable placeholder="全部项目" :options="projectOptions" />
      </label>
      <label class="filter-field">
        <span>状态</span>
        <TSelect v-model="filters.auditStatus" clearable placeholder="全部状态" :options="statusOptions" />
      </label>
      <label class="filter-field">
        <span>分支</span>
        <TInput v-model="filters.branch" clearable placeholder="main" />
      </label>
      <label class="filter-field">
        <span>文件</span>
        <TInput v-model="filters.keyword" clearable placeholder="路径 / 类型 / 摘要" />
      </label>
    </template>
    <template #auditStatus="{ row }">
      <TTag :theme="statusTheme(row.auditStatus)" variant="light">{{ statusText(row.auditStatus) }}</TTag>
    </template>
    <template #actions="{ row }">
      <TSpace>
        <RowAction icon="lucide:shield-check" label="审计" @click="openBatchAudit(row)" />
      </TSpace>
    </template>
  </AdminListPage>

  <TDialog
    v-model:visible="syncVisible"
    header="同步仓库文件索引"
    width="560px"
    :confirm-btn="{ content: '创建同步命令', loading: syncing }"
    @confirm="submitSync"
  >
    <div class="dialog-form">
      <label class="form-field">
        <span>项目</span>
        <TSelect v-model="syncForm.projectId" filterable :options="projectOptions" />
      </label>
      <label class="form-field">
        <span>Worker</span>
        <TSelect v-model="syncForm.workerId" filterable :options="workerOptions" />
      </label>
      <label class="form-field">
        <span>分支</span>
        <TInput v-model="syncForm.branch" clearable placeholder="留空使用仓库默认分支" />
      </label>
    </div>
  </TDialog>

  <TDialog
    v-model:visible="auditVisible"
    header="发布文件审计任务"
    width="560px"
    :confirm-btn="{ content: '发布审计', loading: creatingAudit }"
    @confirm="submitBatchAudit"
  >
    <div class="dialog-form">
      <label class="form-field">
        <span>项目</span>
        <TSelect v-model="auditForm.projectId" disabled :options="projectOptions" />
      </label>
      <label class="form-field">
        <span>Worker</span>
        <TSelect v-model="auditForm.workerId" filterable :options="workerOptions" />
      </label>
      <label class="form-field">
        <span>分支</span>
        <TInput v-model="auditForm.branch" clearable placeholder="留空使用仓库默认分支" />
      </label>
      <label class="form-field">
        <span>文件数</span>
        <TInput :model-value="String(selectedFiles.length)" readonly />
      </label>
      <label class="form-field">
        <span>要求</span>
        <TInput v-model="auditForm.instruction" clearable placeholder="补充审计重点" />
      </label>
    </div>
  </TDialog>
</template>

<style scoped>
.filter-field,
.form-field {
  display: grid;
  grid-template-columns: auto minmax(180px, 260px);
  gap: 8px;
  align-items: center;
}

.dialog-form {
  display: grid;
  gap: 14px;
}
</style>
