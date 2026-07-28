<script lang="ts" setup>
import type { FormInstanceFunctions, FormRules } from 'tdesign-vue-next';

import { computed, onMounted, reactive, ref } from 'vue';

import { deleteDepartmentApi, listDepartmentsApi, saveDepartmentApi, type SystemApi } from '#/api';
import AdminListPage from '@root/base/components/admin-page/admin-list-page.vue';
import { confirmAndClose } from '@root/base/components/admin-page/dialog-confirm';
import { requiredRule, validateForm } from '@root/base/components/admin-page/form-rules';
import RowAction from '@root/base/components/admin-page/row-action.vue';
import { getCellRow } from '@root/base/components/admin-page/table-cell';
import {
  Dialog as TDialog,
  Form as TForm,
  FormItem as TFormItem,
  Input as TInput,
  MessagePlugin,
  Space as TSpace,
  Textarea as TTextarea,
} from 'tdesign-vue-next';

defineOptions({ name: 'SystemDepartments' });

const departments = ref<SystemApi.Department[]>([]);
const formRef = ref<FormInstanceFunctions>();
const loading = ref(false);
const saving = ref(false);
const total = ref(0);
const visible = ref(false);

const form = reactive<Partial<SystemApi.Department>>({
  code: '',
  description: '',
  id: undefined,
  managedAppIds: '',
  managedRegions: '',
  name: '',
  parentId: '',
});

const rules: FormRules<typeof form> = {
  code: requiredRule('请输入部门编码'),
  name: requiredRule('请输入部门名称'),
};

const filters = reactive({ keyword: '' });
const query = reactive({ keyword: '' });
const pagination = reactive({ current: 1, pageSize: 30 });

const columns = [
  { colKey: 'code', title: '部门编码', width: 180 },
  { colKey: 'name', title: '部门名称', width: 180 },
  { colKey: 'parentId', title: '上级部门 ID', cell: (...args: any[]) => getCellRow<SystemApi.Department>(args[0], args[1])?.parentId || '-' },
  { colKey: 'description', title: '说明', cell: (...args: any[]) => getCellRow<SystemApi.Department>(args[0], args[1])?.description || '-' },
  { colKey: 'actions', title: '操作', width: 150, cell: 'actions' },
];

const tablePagination = computed(() => ({
  current: pagination.current,
  pageSize: pagination.pageSize,
  pageSizeOptions: [30, 50, 100, 200],
  total: total.value,
}));

async function load() {
  loading.value = true;
  try {
    const result = await listDepartmentsApi({
      ...query,
      current: pagination.current,
      pageSize: pagination.pageSize,
    });
    departments.value = result.list;
    total.value = result.total;
  } finally {
    loading.value = false;
  }
}

async function handlePageChange(pageInfo: { current: number; pageSize: number }) {
  Object.assign(pagination, pageInfo);
  await load();
}

function open(row?: SystemApi.Department) {
  Object.assign(form, {
    code: row?.code || '',
    description: row?.description || '',
    id: row?.id,
    managedAppIds: row?.managedAppIds || '',
    managedRegions: row?.managedRegions || '',
    name: row?.name || '',
    parentId: row?.parentId || '',
  });
  visible.value = true;
}

async function search() {
  Object.assign(query, filters);
  pagination.current = 1;
  await load();
}

async function reset() {
  Object.assign(filters, { keyword: '' });
  await search();
}

async function save() {
  if (saving.value || !(await validateForm(formRef.value))) return;
  saving.value = true;
  try {
    await saveDepartmentApi(form);
    MessagePlugin.success('部门已保存');
    visible.value = false;
    await load();
  } finally {
    saving.value = false;
  }
}

function remove(row: SystemApi.Department) {
  confirmAndClose({
    body: `确认删除部门 ${row.code}？`,
    confirmBtn: '删除',
    header: '删除部门',
    onConfirm: async () => {
      await deleteDepartmentApi(row.id);
      MessagePlugin.success('部门已删除');
      await load();
    },
  });
}

onMounted(load);
</script>

<template>
  <AdminListPage
    title="部门管理"
    description="维护后端 SYS_DEPARTMENT 部门树及部门管理范围。"
    table-title="部门列表"
    add-button-text="新增部门"
    :columns="columns"
    :data="departments"
    :loading="loading"
    :pagination="tablePagination"
    server-pagination
    :refreshable="false"
    @add="open()"
    @page-change="handlePageChange"
    @reset="reset"
    @search="search"
  >
    <template #filters>
      <label class="filter-field">
        <span>部门信息</span>
        <TInput v-model="filters.keyword" clearable placeholder="部门编码 / 部门名称" />
      </label>
    </template>
    <template #actions="{ row }">
      <TSpace>
        <RowAction label="编辑" @click="open(row)" />
        <RowAction label="删除" theme="danger" @click="remove(row)" />
      </TSpace>
    </template>
  </AdminListPage>

  <TDialog v-model:visible="visible" header="部门维护" width="640px" :confirm-btn="{ content: '保存', loading: saving }" @confirm="save">
    <TForm ref="formRef" :data="form" :rules="rules" label-width="120px">
      <TFormItem label="上级部门 ID"><TInput v-model="form.parentId" placeholder="顶级部门可留空" /></TFormItem>
      <TFormItem label="部门编码" name="code"><TInput v-model="form.code" /></TFormItem>
      <TFormItem label="部门名称" name="name"><TInput v-model="form.name" /></TFormItem>
      <TFormItem label="说明"><TTextarea v-model="form.description" /></TFormItem>
      <TFormItem label="管理应用"><TInput v-model="form.managedAppIds" placeholder="多个 AppId 使用英文逗号分隔" /></TFormItem>
      <TFormItem label="管理区域"><TInput v-model="form.managedRegions" placeholder="多个区域 ID 使用英文逗号分隔" /></TFormItem>
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
