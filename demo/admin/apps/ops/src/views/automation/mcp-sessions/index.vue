<script lang="ts" setup>
import type { AutomationApi } from '#/api';
import type { PrimaryTableCol } from 'tdesign-vue-next';

import { computed, onMounted, reactive, ref } from 'vue';
import { useRouter } from 'vue-router';

import {
  listDigitalWorkersApi,
  listWorkerSessionsApi,
} from '#/api';
import AdminListPage from '#/components/admin-list-page/admin-list-page.vue';
import { formatDateTime } from '#/views/_shared/date-format';
import RowAction from '#/views/system/_shared/row-action.vue';
import {
  Input as TInput,
  Select as TSelect,
  Space as TSpace,
  Tag as TTag,
} from 'tdesign-vue-next';

defineOptions({ name: 'AutomationMcpSessions' });

const router = useRouter();

type SessionRow = AutomationApi.WorkerSession & {
  worker?: AutomationApi.DigitalWorker;
};

const loading = ref(false);
const sessions = ref<AutomationApi.WorkerSession[]>([]);
const workers = ref<AutomationApi.DigitalWorker[]>([]);
const defaultFilters = {
  keyword: '',
  status: 'idle',
  workerId: '',
};
const filters = reactive({ ...defaultFilters });
const query = reactive({ ...filters });
const pagination = reactive({
  current: 1,
  pageSize: 30,
  pageSizeOptions: [30, 50, 100, 200],
});

const statusOptions = [
  { label: '启动中', value: 'starting' },
  { label: '空闲', value: 'idle' },
  { label: '运行中', value: 'busy' },
  { label: '待认证', value: 'auth_required' },
  { label: '异常', value: 'error' },
  { label: '离线', value: 'offline' },
  { label: '已过期', value: 'expired' },
];
const columns: PrimaryTableCol[] = [
  { colKey: 'worker', title: '数字员工', cell: 'worker', width: 190 },
  { colKey: 'instance', title: '会话实例', cell: 'instance', minWidth: 230 },
  { colKey: 'status', title: '状态', cell: 'status', width: 100 },
  { colKey: 'versions', title: '环境版本', cell: 'versions', width: 210 },
  { colKey: 'lastHeartbeatAt', title: '最后心跳', cell: 'lastHeartbeatAt', width: 170 },
  { colKey: 'actions', title: '操作', cell: 'actions', width: 120 },
];
const workerMap = computed(() => Object.fromEntries(workers.value.map((item) => [item.id, item])));
const sessionRows = computed<SessionRow[]>(() =>
  sessions.value.map((session) => ({ ...session, worker: workerMap.value[session.workerId] })),
);
const filteredRows = computed(() => {
  const keyword = query.keyword.trim().toLowerCase();
  return sessionRows.value.filter((row) => {
    if (!keyword) {
      return true;
    }

    return [
      row.id,
      row.instanceId,
      row.hostName,
      row.containerId,
      row.workspaceRoot,
      row.errorSummary,
      row.worker?.code,
      row.worker?.name,
    ]
      .filter(Boolean)
      .join('\n')
      .toLowerCase()
      .includes(keyword);
  });
});
const workerOptions = computed(() =>
  workers.value.map((item) => ({ label: `${item.name} (${item.code})`, value: item.id })),
);
const tablePagination = computed(() => ({
  current: pagination.current,
  pageSize: pagination.pageSize,
  pageSizeOptions: pagination.pageSizeOptions,
  total: filteredRows.value.length,
}));

function statusTheme(status?: string) {
  if (status === 'idle') return 'success';
  if (status === 'busy' || status === 'starting') return 'primary';
  if (status === 'auth_required') return 'warning';
  if (status === 'error') return 'danger';
  return 'default';
}

function statusText(status?: string) {
  return statusOptions.find((item) => item.value === status)?.label || status || '-';
}

function resolveWorker(row: SessionRow) {
  return row.worker ? `${row.worker.name} (${row.worker.code})` : row.workerId;
}

function handlePageChange(pageInfo: { current: number; pageSize: number }) {
  pagination.current = pageInfo.current;
  pagination.pageSize = pageInfo.pageSize;
}

async function applyFilters() {
  Object.assign(query, filters);
  pagination.current = 1;
  await loadSessions();
}

async function resetFilters() {
  Object.assign(filters, defaultFilters);
  await applyFilters();
}

async function openDetail(row: SessionRow) {
  await router.push({
    path: `/automation/digital-workers/${row.workerId}/command-audit`,
    query: { sessionId: row.id },
  });
}

async function loadReferences() {
  workers.value = await listDigitalWorkersApi();
}

async function loadSessions() {
  loading.value = true;
  try {
    sessions.value = await listWorkerSessionsApi({
      status: query.status,
      workerId: query.workerId,
    });
  } finally {
    loading.value = false;
  }
}

onMounted(async () => {
  await loadReferences();
  await loadSessions();
});
</script>

<template>
  <AdminListPage
    title="Worker会话管理"
    description="查看 AgentSprint.Worker 受控端注册、心跳、运行记录和事件审计，用于判断远程 Codex 会话是否健康。"
    table-title="Worker会话列表"
    :addable="false"
    :columns="columns"
    :data="filteredRows"
    :loading="loading"
    :pagination="tablePagination"
    @page-change="handlePageChange"
    @refresh="loadSessions"
    @reset="resetFilters"
    @search="applyFilters"
  >
    <template #filters>
      <label class="filter-field">
        <span>数字员工</span>
        <TSelect v-model="filters.workerId" clearable filterable placeholder="全部员工" :options="workerOptions" />
      </label>
      <label class="filter-field">
        <span>状态</span>
        <TSelect v-model="filters.status" clearable placeholder="全部状态" :options="statusOptions" />
      </label>
      <label class="filter-field">
        <span>会话信息</span>
        <TInput v-model="filters.keyword" clearable placeholder="实例 / 主机 / 工作区 / 错误" />
      </label>
    </template>

    <template #worker="{ row }">
      <div class="main-cell">
        <strong>{{ resolveWorker(row) }}</strong>
        <span>{{ row.workerId }}</span>
      </div>
    </template>
    <template #instance="{ row }">
      <div class="main-cell">
        <strong>{{ row.instanceId }}</strong>
        <span>{{ row.hostName || '-' }} / {{ row.containerId || '-' }}</span>
      </div>
    </template>
    <template #status="{ row }">
      <TTag :theme="statusTheme(row.status)" variant="light">{{ statusText(row.status) }}</TTag>
    </template>
    <template #versions="{ row }">
      <div class="version-cell">
        <span>Codex {{ row.codexVersion || '-' }}</span>
        <span>Git {{ row.gitVersion || '-' }} / Node {{ row.nodeVersion || '-' }}</span>
      </div>
    </template>
    <template #lastHeartbeatAt="{ row }">
      {{ formatDateTime(row.lastHeartbeatAt || row.startedAt) }}
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
  grid-template-columns: auto minmax(180px, 280px);
  gap: 8px;
  align-items: center;
  color: var(--td-text-color-secondary);
}

.main-cell,
.version-cell {
  display: grid;
  gap: 2px;
  min-width: 0;
}

.main-cell strong {
  overflow: hidden;
  color: var(--td-text-color-primary);
  text-overflow: ellipsis;
  white-space: nowrap;
}

.main-cell span,
.version-cell span {
  overflow: hidden;
  color: var(--td-text-color-secondary);
  font-size: 12px;
  text-overflow: ellipsis;
  white-space: nowrap;
}

@media (max-width: 760px) {
  .filter-field {
    grid-template-columns: 1fr;
    width: 100%;
  }
}
</style>
