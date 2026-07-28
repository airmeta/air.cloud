<script lang="ts" setup>
import type { SystemApi } from '#/api';
import type { FormInstanceFunctions, FormRules, PrimaryTableCol } from 'tdesign-vue-next';

import { computed, onMounted, reactive, ref } from 'vue';

import {
  deleteAiPlatformApi,
  listAiPlatformsApi,
  saveAiPlatformApi,
} from '#/api';
import AdminListPage from '#/components/admin-list-page/admin-list-page.vue';
import { confirmAndClose } from '#/views/_shared/dialog-confirm';
import { requiredRule, validateForm } from '#/views/_shared/form-rules';
import RowAction from '#/views/system/_shared/row-action.vue';
import {
  Dialog as TDialog,
  Form as TForm,
  FormItem as TFormItem,
  Input as TInput,
  MessagePlugin,
  Select as TSelect,
  Space as TSpace,
  Tag as TTag,
  Textarea as TTextarea,
} from 'tdesign-vue-next';

defineOptions({ name: 'GlobalConfigAiPlatforms' });

const loading = ref(false);
const saving = ref(false);
const visible = ref(false);
const formRef = ref<FormInstanceFunctions>();
const rows = ref<SystemApi.AiPlatform[]>([]);
const filters = reactive({
  keyword: '',
  status: undefined as number | undefined,
});
const query = reactive({ ...filters });
const pagination = reactive({
  current: 1,
  pageSize: 30,
  pageSizeOptions: [30, 50, 100, 200],
});
const form = reactive<Partial<SystemApi.AiPlatform>>({
  apiKey: '',
  code: 'openai',
  description: '',
  id: undefined,
  model: 'gpt-5.4',
  name: 'OpenAI',
  openAiBaseUrl: 'https://api.openai.com/v1',
  provider: 'openai',
  sort: 10,
  status: 1,
});
const rules: FormRules<typeof form> = {
  code: requiredRule('请输入平台编码'),
  model: requiredRule('请输入模型'),
  name: requiredRule('请输入平台名称'),
  provider: requiredRule('请输入 Provider'),
};
const statusOptions = [
  { label: '启用', value: 1 },
  { label: '停用', value: 0 },
];
const columns: PrimaryTableCol[] = [
  { colKey: 'code', title: '平台编码', width: 150 },
  { colKey: 'name', title: '平台名称', width: 160 },
  { colKey: 'provider', title: 'Provider', width: 150 },
  { colKey: 'model', title: '模型', width: 170 },
  { colKey: 'hasApiKey', title: 'API Key', cell: 'hasApiKey', width: 110 },
  { colKey: 'openAiBaseUrl', title: 'OpenAI Base URL', ellipsis: true },
  { colKey: 'status', title: '状态', cell: 'status', width: 90 },
  { colKey: 'actions', title: '操作', cell: 'actions', width: 150 },
];
const tablePagination = computed(() => ({
  current: pagination.current,
  pageSize: pagination.pageSize,
  pageSizeOptions: pagination.pageSizeOptions,
  total: rows.value.length,
}));

function resetForm() {
  Object.assign(form, {
    apiKey: '',
    code: 'openai',
    description: '',
    id: undefined,
    model: 'gpt-5.4',
    name: 'OpenAI',
    openAiBaseUrl: 'https://api.openai.com/v1',
    provider: 'openai',
    sort: 10,
    status: 1,
  });
}

function openCreate() {
  resetForm();
  visible.value = true;
}

function openEdit(row: SystemApi.AiPlatform) {
  Object.assign(form, row, { apiKey: '' });
  visible.value = true;
}

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

async function loadRows() {
  loading.value = true;
  try {
    rows.value = await listAiPlatformsApi(query);
  } finally {
    loading.value = false;
  }
}

async function save() {
  if (saving.value) return;
  if (!(await validateForm(formRef.value))) return;

  saving.value = true;
  try {
    await saveAiPlatformApi({
      ...form,
      apiKey: form.apiKey?.trim() || undefined,
      code: form.code?.trim(),
      model: form.model?.trim(),
      name: form.name?.trim(),
      openAiBaseUrl: form.openAiBaseUrl?.trim() || undefined,
      provider: form.provider?.trim(),
      sort: Number(form.sort || 0),
      status: Number(form.status ?? 1),
    });
    MessagePlugin.success('AI平台已保存');
    visible.value = false;
    await loadRows();
  } finally {
    saving.value = false;
  }
}

function remove(row: SystemApi.AiPlatform) {
  confirmAndClose({
    body: `确认删除 AI 平台 ${row.name}？数字员工已保存的运行快照不会被自动修改。`,
    confirmBtn: '删除',
    header: '删除AI平台',
    onConfirm: async () => {
      await deleteAiPlatformApi(row.id);
      MessagePlugin.success('AI平台已删除');
      await loadRows();
    },
  });
}

onMounted(loadRows);
</script>

<template>
  <AdminListPage
    title="AI平台维护"
    description="维护数字员工可选择的 AI Provider、模型和 OpenAI Base URL。"
    table-title="AI平台列表"
    add-button-text="新增AI平台"
    :columns="columns"
    :data="rows"
    :loading="loading"
    :pagination="tablePagination"
    @add="openCreate"
    @page-change="handlePageChange"
    @refresh="loadRows"
    @reset="resetFilters"
    @search="applyFilters"
  >
    <template #filters>
      <label class="filter-field">
        <span>平台信息</span>
        <TInput v-model="filters.keyword" clearable placeholder="编码 / 名称 / Provider / 模型" />
      </label>
      <label class="filter-field">
        <span>状态</span>
        <TSelect v-model="filters.status" clearable placeholder="全部状态" :options="statusOptions" />
      </label>
    </template>

    <template #status="{ row }">
      <TTag :theme="row.status === 1 ? 'success' : 'default'" variant="light">
        {{ row.status === 1 ? '启用' : '停用' }}
      </TTag>
    </template>

    <template #hasApiKey="{ row }">
      <TTag :theme="row.hasApiKey ? 'success' : 'warning'" variant="light">
        {{ row.hasApiKey ? '已配置' : '未配置' }}
      </TTag>
    </template>

    <template #actions="{ row }">
      <TSpace>
        <RowAction label="编辑" @click="openEdit(row)" />
        <RowAction label="删除" theme="danger" @click="remove(row)" />
      </TSpace>
    </template>
  </AdminListPage>

  <TDialog
    v-model:visible="visible"
    header="AI平台"
    width="680px"
    :confirm-btn="{ content: '保存', loading: saving }"
    @confirm="save"
  >
    <TForm ref="formRef" :data="form" :rules="rules" label-width="130px">
      <TFormItem label="平台编码" name="code">
        <TInput v-model="form.code" :disabled="Boolean(form.id)" placeholder="openai" />
      </TFormItem>
      <TFormItem label="平台名称" name="name">
        <TInput v-model="form.name" placeholder="OpenAI" />
      </TFormItem>
      <TFormItem label="Provider" name="provider">
        <TInput v-model="form.provider" placeholder="openai" />
      </TFormItem>
      <TFormItem label="模型" name="model">
        <TInput v-model="form.model" placeholder="gpt-5.4" />
      </TFormItem>
      <TFormItem label="API Key">
        <TInput v-model="form.apiKey" type="password" placeholder="留空则保持原密钥" />
      </TFormItem>
      <TFormItem label="OpenAI Base URL">
        <TInput v-model="form.openAiBaseUrl" placeholder="https://api.openai.com/v1" />
      </TFormItem>
      <TFormItem label="排序">
        <TInput v-model="form.sort" type="number" placeholder="10" />
      </TFormItem>
      <TFormItem label="状态">
        <TSelect v-model="form.status" :options="statusOptions" />
      </TFormItem>
      <TFormItem label="说明">
        <TTextarea v-model="form.description" :autosize="{ minRows: 2, maxRows: 4 }" />
      </TFormItem>
    </TForm>
  </TDialog>
</template>

<style scoped>
.filter-field {
  display: grid;
  grid-template-columns: auto minmax(180px, 260px);
  gap: 8px;
  align-items: center;
  color: var(--td-text-color-secondary);
}

@media (max-width: 760px) {
  .filter-field {
    grid-template-columns: 1fr;
    width: 100%;
  }
}
</style>
