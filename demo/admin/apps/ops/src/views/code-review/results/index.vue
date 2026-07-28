<script lang="ts" setup>
import type { PrimaryTableCol } from 'tdesign-vue-next';

import { computed, reactive, ref } from 'vue';
import { useRouter } from 'vue-router';

import {
  listCodeAuditResultsApi,
  type CodeReviewApi,
} from '#/api/code-review';
import { listDigitalWorkersApi, type AutomationApi } from '#/api/automation/workers';
import { listProjectsApi, type SprintMvpApi } from '#/api/sprint/mvp';
import AdminListPage from '#/components/admin-list-page/admin-list-page.vue';
import RowAction from '#/views/system/_shared/row-action.vue';
import {
  Input as TInput,
  Select as TSelect,
  Space as TSpace,
  Tag as TTag,
} from 'tdesign-vue-next';

defineOptions({ name: 'CodeReviewResults' });

const router = useRouter();

type CodeReviewResultRow = {
  auditTaskId: string;
  branch?: string;
  id: string;
  projectId: string;
  resultLevel: CodeReviewApi.TaskStatus;
  riskCount: number;
  taskName: string;
  workerId: string;
};

const defaultFilters = {
  keyword: '',
  resultLevel: undefined as CodeReviewResultRow['resultLevel'] | undefined,
};
const filters = reactive({ ...defaultFilters });
const query = reactive({ ...defaultFilters });
const pagination = reactive({
  current: 1,
  pageSize: 30,
  pageSizeOptions: [30, 50, 100, 200],
});
const rows = reactive<CodeReviewResultRow[]>([]);
const projects = ref<SprintMvpApi.Project[]>([]);
const workers = ref<AutomationApi.DigitalWorker[]>([]);

const levelOptions = [
  { label: '通过', value: 'passed' },
  { label: '需修改', value: 'needs_changes' },
  { label: '阻断', value: 'blocked' },
  { label: '失败', value: 'failed' },
  { label: '已取消', value: 'cancelled' },
  { label: '待审计', value: 'pending' },
  { label: '审计中', value: 'running' },
];
const columns: PrimaryTableCol[] = [
  { colKey: 'taskName', title: '审计任务', minWidth: 220 },
  { colKey: 'projectId', title: '项目名称', cell: 'projectId', minWidth: 160 },
  { colKey: 'branch', title: '分支', width: 140 },
  { colKey: 'resultLevel', title: '结果等级', cell: 'resultLevel', width: 120 },
  { colKey: 'riskCount', title: '风险数', width: 100 },
  { colKey: 'workerId', title: '数字员工', cell: 'workerId', minWidth: 180 },
  { colKey: 'actions', title: '操作', cell: 'actions', width: 130 },
];

const filteredRows = computed(() => {
  const keyword = query.keyword.trim().toLowerCase();
  return rows.filter((row) => {
    if (query.resultLevel && row.resultLevel !== query.resultLevel) {
      return false;
    }

    if (!keyword) {
      return true;
    }

    return [
      row.taskName,
      row.projectId,
      resolveProjectName(row.projectId),
      row.workerId,
      resolveWorkerName(row.workerId),
      row.branch,
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
const projectMap = computed(() =>
  Object.fromEntries(projects.value.map((item) => [item.id, item])),
);
const workerMap = computed(() =>
  Object.fromEntries(workers.value.map((item) => [item.id, item])),
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

function levelTheme(level?: CodeReviewResultRow['resultLevel']) {
  if (level === 'passed') return 'success';
  if (level === 'running') return 'primary';
  if (level === 'pending' || level === 'needs_changes') return 'warning';
  if (level === 'blocked' || level === 'failed') return 'danger';
  return 'default';
}

function levelText(level?: CodeReviewResultRow['resultLevel']) {
  return levelOptions.find((item) => item.value === level)?.label || '-';
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

  const data = await listCodeAuditResultsApi({
    keyword: query.keyword,
    status: query.resultLevel,
  });
  rows.splice(
    0,
    rows.length,
    ...data.map((item: CodeReviewApi.CodeAuditResultListItem) => ({
      auditTaskId: item.task.id,
      branch: item.result?.branch || item.task.branch,
      id: item.result?.id || item.task.id,
      projectId: item.task.projectId,
      resultLevel: item.task.status,
      riskCount:
        parseJsonArray(item.result?.issuesJson).length +
        parseJsonArray(item.result?.annotationIssuesJson).length,
      taskName: item.task.targetId
        ? `${item.task.auditTargetType} / ${item.task.targetId}`
        : item.task.id,
      workerId: item.task.workerId,
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

function parseJsonArray(value?: string) {
  if (!value) {
    return [];
  }

  try {
    const parsed = JSON.parse(value);
    return Array.isArray(parsed) ? parsed : [];
  } catch {
    return [];
  }
}

function openDetail(row: CodeReviewResultRow) {
  router.push(`/code-review/results/detail/${row.auditTaskId}`);
}

loadRows();
</script>

<template>
  <AdminListPage
    title="代码审计结果"
    description="查看代码审查输出结果、风险分级、整改建议、版本边界和审计追踪。"
    table-title="代码审计结果列表"
    :addable="false"
    :columns="columns"
    :data="filteredRows"
    :pagination="tablePagination"
    @page-change="handlePageChange"
    @refresh="loadRows"
    @reset="resetFilters"
    @search="() => { applyFilters(); loadRows(); }"
  >
    <template #filters>
      <label class="filter-field">
        <span>结果信息</span>
        <TInput v-model="filters.keyword" clearable placeholder="任务 / 项目 / 数字员工" />
      </label>
      <label class="filter-field">
        <span>等级</span>
        <TSelect v-model="filters.resultLevel" clearable placeholder="全部等级" :options="levelOptions" />
      </label>
    </template>
    <template #projectId="{ row }">
      {{ resolveProjectName(row.projectId) }}
    </template>
    <template #workerId="{ row }">
      {{ resolveWorkerName(row.workerId) }}
    </template>
    <template #resultLevel="{ row }">
      <TTag :theme="levelTheme(row.resultLevel)" variant="light">{{ levelText(row.resultLevel) }}</TTag>
    </template>
    <template #actions="{ row }">
      <TSpace>
        <RowAction icon="lucide:eye" label="详情" @click="openDetail(row)" />
      </TSpace>
    </template>
  </AdminListPage>
</template>

<style scoped>
.filter-field {
  display: grid;
  grid-template-columns: auto minmax(180px, 260px);
  gap: 8px;
  align-items: center;
}

</style>
