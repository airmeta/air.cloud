<script lang="ts" setup>
import { computed, onMounted, reactive, ref } from 'vue';
import type { FormInstanceFunctions, FormRules } from 'tdesign-vue-next';

import { deleteAssignmentApi, listAssignmentsApi, saveAssignmentApi, type SystemApi } from '#/api';
import AdminListPage from '#/components/admin-list-page/admin-list-page.vue';
import RowAction from '#/views/system/_shared/row-action.vue';
import { getCellRow } from '#/views/system/_shared/table-cell';
import {
  Dialog as TDialog,
  Form as TForm,
  FormItem as TFormItem,
  Input as TInput,
  MessagePlugin,
  Select as TSelect,
  Space as TSpace,
} from 'tdesign-vue-next';
import { requiredRule, validateForm } from '#/views/_shared/form-rules';
import { confirmAndClose } from '#/views/_shared/dialog-confirm';

const loading = ref(false);
const saving = ref(false);
const visible = ref(false);
const formRef = ref<FormInstanceFunctions>();
const assignments = ref<SystemApi.CodeName[]>([]);
const form = reactive<Partial<SystemApi.CodeName>>({
  code: '',
  description: '',
  id: undefined,
  name: '',
  status: 1,
});
const rules: FormRules<typeof form> = {
  code: requiredRule('请输入岗位编码'),
  name: requiredRule('请输入岗位名称'),
};
const filters = reactive({
  keyword: '',
  status: undefined as number | undefined,
});
const query = reactive({
  keyword: '',
  status: undefined as number | undefined,
});
const pagination = reactive({
  current: 1,
  pageSize: 30,
});
const columns = [
  { colKey: 'code', title: '岗位编码' },
  { colKey: 'name', title: '岗位名称' },
  { colKey: 'description', title: '说明', cell: (...args: any[]) => getCellRow(args[0], args[1])?.description || '-' },
  { colKey: 'status', title: '状态', cell: (...args: any[]) => (getCellRow(args[0], args[1])?.status === 1 ? '启用' : '停用') },
  { colKey: 'actions', title: '操作', cell: 'actions' },
];
const tablePagination = computed(() => ({
  current: pagination.current,
  pageSize: pagination.pageSize,
  pageSizeOptions: [30, 50, 100, 200],
  total: assignments.value.length,
}));

function handlePageChange(pageInfo: { current: number; pageSize: number }) {
  pagination.current = pageInfo.current;
  pagination.pageSize = pageInfo.pageSize;
}

function open(row?: SystemApi.CodeName) {
  Object.assign(form, {
    code: row?.code || '',
    description: row?.description || '',
    id: row?.id,
    name: row?.name || '',
    status: row?.status ?? 1,
  });
  visible.value = true;
}

async function load() {
  loading.value = true;
  try {
    assignments.value = await listAssignmentsApi(query);
  } finally {
    loading.value = false;
  }
}

async function search() {
  Object.assign(query, filters);
  pagination.current = 1;
  await load();
}

async function reset() {
  Object.assign(filters, { keyword: '', status: undefined });
  await search();
}

async function save() {
  if (saving.value) return;
  if (!(await validateForm(formRef.value))) return;
  saving.value = true;
  try {
    await saveAssignmentApi(form);
    MessagePlugin.success('岗位已保存');
    visible.value = false;
    await load();
  } finally {
    saving.value = false;
  }
}

function remove(row: SystemApi.CodeName) {
  confirmAndClose({
    body: `确认删除岗位 ${row.code}？`,
    confirmBtn: '删除',
    header: '删除岗位',
    onConfirm: async () => {
      await deleteAssignmentApi(row.id);
      MessagePlugin.success('岗位已删除');
      await load();
    },
  });
}

onMounted(load);
</script>

<template>
  <div>
    <AdminListPage
      title="岗位管理"
      table-title="岗位列表"
      add-button-text="新增岗位"
      :columns="columns"
      :data="assignments"
      :loading="loading"
      :pagination="tablePagination"
      :refreshable="false"
      @add="open()"
      @page-change="handlePageChange"
      @reset="reset"
      @search="search"
    >
      <template #filters>
        <label class="filter-field">
          <span>岗位信息</span>
          <TInput v-model="filters.keyword" clearable placeholder="岗位编码 / 名称 / 说明" />
        </label>
        <label class="filter-field">
          <span>状态</span>
          <TSelect
            v-model="filters.status"
            clearable
            placeholder="全部状态"
            :options="[{ label: '启用', value: 1 }, { label: '停用', value: 0 }]"
          />
        </label>
      </template>
      <template #actions="{ row }">
        <TSpace>
          <RowAction label="编辑" @click="open(row)" />
          <RowAction label="删除" theme="danger" @click="remove(row)" />
        </TSpace>
      </template>
    </AdminListPage>
    <TDialog v-model:visible="visible" header="岗位维护" width="560px" :confirm-btn="{ content: '保存', loading: saving }" @confirm="save">
      <TForm ref="formRef" :data="form" :rules="rules" label-width="96px">
        <TFormItem label="岗位编码" name="code"><TInput v-model="form.code" /></TFormItem>
        <TFormItem label="岗位名称" name="name"><TInput v-model="form.name" /></TFormItem>
        <TFormItem label="说明"><TInput v-model="form.description" /></TFormItem>
        <TFormItem label="状态"><TSelect v-model="form.status" :options="[{ label: '启用', value: 1 }, { label: '停用', value: 0 }]" /></TFormItem>
      </TForm>
    </TDialog>
  </div>
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
