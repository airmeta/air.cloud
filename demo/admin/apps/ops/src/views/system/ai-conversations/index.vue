<script lang="ts" setup>
import type { SystemApi } from '#/api';
import type { PrimaryTableCol } from 'tdesign-vue-next';

import { computed, onMounted, reactive, ref } from 'vue';

import { listAiConversationsApi } from '#/api';
import AdminListPage from '#/components/admin-list-page/admin-list-page.vue';
import RowAction from '#/views/system/_shared/row-action.vue';
import {
  Dialog as TDialog,
  Input as TInput,
  Select as TSelect,
  Space as TSpace,
  Tag as TTag,
  Textarea as TTextarea,
} from 'tdesign-vue-next';

defineOptions({ name: 'GlobalConfigAiConversations' });

const loading = ref(false);
const detailVisible = ref(false);
const rows = ref<SystemApi.AiConversation[]>([]);
const current = ref<SystemApi.AiConversation>();
const filters = reactive({
  keyword: '',
  status: undefined as string | undefined,
});
const query = reactive({ ...filters });
const pagination = reactive({
  current: 1,
  pageSize: 30,
  pageSizeOptions: [30, 50, 100, 200],
});
const statusOptions = [
  { label: '成功', value: 'completed' },
  { label: '失败', value: 'failed' },
];
const columns: PrimaryTableCol[] = [
  { colKey: 'title', title: '对话标题', ellipsis: true, width: 220 },
  { colKey: 'aiPlatformCode', title: 'AI平台', width: 120 },
  { colKey: 'model', title: '模型', width: 150 },
  { colKey: 'target', title: '关联数据', cell: 'target', ellipsis: true },
  { colKey: 'status', title: '状态', cell: 'status', width: 90 },
  { colKey: 'startedAt', title: '发起时间', width: 180 },
  { colKey: 'actions', title: '操作', cell: 'actions', width: 120 },
];
const tablePagination = computed(() => ({
  current: pagination.current,
  pageSize: pagination.pageSize,
  pageSizeOptions: pagination.pageSizeOptions,
  total: rows.value.length,
}));
const detailText = computed(() => current.value
  ? [
      `用户消息:\n${current.value.userMessage}`,
      `上下文快照:\n${current.value.contextSnapshot}`,
      `AI回复:\n${current.value.assistantMessage || current.value.errorMessage || ''}`,
    ].join('\n\n')
  : '');

async function applyFilters() {
  Object.assign(query, filters);
  pagination.current = 1;
  await loadRows();
}

async function resetFilters() {
  Object.assign(filters, { keyword: '', status: undefined });
  await applyFilters();
}

function handlePageChange(pageInfo: { current: number; pageSize: number }) {
  pagination.current = pageInfo.current;
  pagination.pageSize = pageInfo.pageSize;
}

function openDetail(row: SystemApi.AiConversation) {
  current.value = row;
  detailVisible.value = true;
}

function targetText(row: SystemApi.AiConversation) {
  return [
    row.requirementId && `需求:${row.requirementId}`,
    row.taskId && `任务:${row.taskId}`,
    row.testPlanId && `测试计划:${row.testPlanId}`,
    row.bugId && `缺陷:${row.bugId}`,
  ].filter(Boolean).join(' / ');
}

async function loadRows() {
  loading.value = true;
  try {
    rows.value = await listAiConversationsApi(query);
  } finally {
    loading.value = false;
  }
}

onMounted(loadRows);
</script>

<template>
  <AdminListPage
    title="AI对话管理"
    description="查询平台内发起的 AI 对话、关联业务数据、上下文快照和回复结果。"
    table-title="AI对话列表"
    :addable="false"
    :columns="columns"
    :data="rows"
    :loading="loading"
    :pagination="tablePagination"
    @page-change="handlePageChange"
    @refresh="loadRows"
    @reset="resetFilters"
    @search="applyFilters"
  >
    <template #filters>
      <label class="filter-field">
        <span>对话信息</span>
        <TInput v-model="filters.keyword" clearable placeholder="标题 / 平台 / 模型 / 内容" />
      </label>
      <label class="filter-field">
        <span>状态</span>
        <TSelect v-model="filters.status" clearable placeholder="全部状态" :options="statusOptions" />
      </label>
    </template>

    <template #target="{ row }">
      {{ targetText(row) || '-' }}
    </template>

    <template #status="{ row }">
      <TTag :theme="row.status === 'completed' ? 'success' : 'danger'" variant="light">
        {{ row.status === 'completed' ? '成功' : '失败' }}
      </TTag>
    </template>

    <template #actions="{ row }">
      <TSpace>
        <RowAction label="详情" @click="openDetail(row)" />
      </TSpace>
    </template>
  </AdminListPage>

  <TDialog v-model:visible="detailVisible" header="AI对话详情" width="920px" :footer="false">
    <TTextarea readonly :model-value="detailText" :autosize="{ minRows: 18, maxRows: 26 }" />
  </TDialog>
</template>
