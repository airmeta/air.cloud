<script lang="ts" setup>
import { computed, onMounted, reactive, ref } from 'vue';
import type { FormInstanceFunctions, FormRules } from 'tdesign-vue-next';

import {
  deleteSystemConfigurationApi,
  listSystemConfigurationsApi,
  saveSystemConfigurationApi,
  type SystemApi,
} from '#/api';
import AdminListPage from '#/components/admin-list-page/admin-list-page.vue';
import RowAction from '#/views/system/_shared/row-action.vue';
import {
  Dialog as TDialog,
  Form as TForm,
  FormItem as TFormItem,
  Input as TInput,
  MessagePlugin,
  Select as TSelect,
  Space as TSpace,
  Textarea as TTextarea,
} from 'tdesign-vue-next';
import { requiredRule, validateForm } from '#/views/_shared/form-rules';
import { confirmAndClose } from '#/views/_shared/dialog-confirm';

const loading = ref(false);
const saving = ref(false);
const visible = ref(false);
const formRef = ref<FormInstanceFunctions>();
const rows = ref<SystemApi.Configuration[]>([]);
const form = reactive<Partial<SystemApi.Configuration>>({
  description: '',
  id: undefined,
  key: 'Mcp:Endpoint',
  status: 1,
  value: '',
});
const rules: FormRules<typeof form> = {
  key: requiredRule('请输入配置键'),
  value: requiredRule('请输入配置值'),
};
const filters = reactive({
  keyword: '',
  status: undefined as number | undefined,
});
const query = reactive({ ...filters });
const pagination = reactive({
  current: 1,
  pageSize: 30,
});

const columns = [
  { colKey: 'key', title: '配置键', width: 220 },
  { colKey: 'value', title: '配置值', ellipsis: true },
  { colKey: 'description', title: '说明', ellipsis: true },
  { colKey: 'status', title: '状态', width: 100 },
  { colKey: 'actions', title: '操作', width: 140 },
];

const tablePagination = computed(() => ({
  current: pagination.current,
  pageSize: pagination.pageSize,
  pageSizeOptions: [30, 50, 100, 200],
  total: rows.value.length,
}));

function handlePageChange(pageInfo: { current: number; pageSize: number }) {
  pagination.current = pageInfo.current;
  pagination.pageSize = pageInfo.pageSize;
}

function resetForm() {
  Object.assign(form, {
    description: 'Streamable HTTP MCP 服务地址，用于任务提示词生成。',
    id: undefined,
    key: 'Mcp:Endpoint',
    status: 1,
    value: 'http://192.168.80.101:5010/mcp',
  });
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

async function loadRows() {
  loading.value = true;
  try {
    rows.value = await listSystemConfigurationsApi(query);
  } finally {
    loading.value = false;
  }
}

function openCreate() {
  resetForm();
  visible.value = true;
}

function openEdit(row: SystemApi.Configuration) {
  Object.assign(form, row);
  visible.value = true;
}

async function save() {
  if (saving.value) return;
  if (!(await validateForm(formRef.value))) return;
  if (!form.key?.trim() || !form.value?.trim()) {
    MessagePlugin.warning('配置键和配置值不能为空');
    return;
  }

  saving.value = true;
  try {
    await saveSystemConfigurationApi(form);
    MessagePlugin.success('配置已保存');
    visible.value = false;
    await loadRows();
  } finally {
    saving.value = false;
  }
}

function remove(row: SystemApi.Configuration) {
  confirmAndClose({
    body: `确认删除配置 ${row.key}？删除后业务会回退默认值。`,
    confirmBtn: '删除',
    header: '删除配置',
    onConfirm: async () => {
      await deleteSystemConfigurationApi(row.id);
      MessagePlugin.success('配置已删除');
      await loadRows();
    },
  });
}

onMounted(async () => {
  resetForm();
  await loadRows();
});
</script>

<template>
  <AdminListPage
    title="系统配置"
    description="维护运行时动态配置，当前用于控制任务提示词中的 Streamable HTTP MCP 服务地址。"
    table-title="系统配置列表"
    add-button-text="新增配置"
    :columns="columns"
    :data="rows"
    :loading="loading"
    :pagination="tablePagination"
    :refreshable="false"
    @add="openCreate"
    @page-change="handlePageChange"
    @reset="resetFilters"
    @search="applyFilters"
  >
    <template #filters>
      <label class="filter-field">
        <span>配置信息</span>
        <TInput v-model="filters.keyword" clearable placeholder="配置键 / 配置值 / 说明" />
      </label>
      <label class="filter-field">
        <span>状态</span>
        <TSelect
          v-model="filters.status"
          clearable
          placeholder="全部状态"
          :options="[
            { label: '启用', value: 1 },
            { label: '停用', value: 0 },
          ]"
        />
      </label>
    </template>
    <template #actions="{ row }">
      <TSpace>
        <RowAction label="编辑" @click="openEdit(row)" />
        <RowAction label="删除" theme="danger" @click="remove(row)" />
      </TSpace>
    </template>
  </AdminListPage>

  <TDialog v-model:visible="visible" header="系统配置" width="620" :confirm-on-enter="true" :confirm-btn="{ content: '保存', loading: saving }" @confirm="save">
    <TForm ref="formRef" :data="form" :rules="rules" label-width="96px">
      <TFormItem label="配置键" name="key">
        <TInput v-model="form.key" placeholder="Mcp:Endpoint" />
      </TFormItem>
      <TFormItem label="配置值" name="value">
        <TInput v-model="form.value" placeholder="http://192.168.80.101:5010/mcp" />
      </TFormItem>
      <TFormItem label="说明">
        <TTextarea v-model="form.description" placeholder="配置用途说明" />
      </TFormItem>
      <TFormItem label="状态">
        <TSelect
          v-model="form.status"
          :options="[
            { label: '启用', value: 1 },
            { label: '停用', value: 0 },
          ]"
        />
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
