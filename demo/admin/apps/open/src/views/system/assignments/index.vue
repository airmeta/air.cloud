<script lang="ts" setup>
import type { FormInstanceFunctions, FormRules } from 'tdesign-vue-next';

import { computed, onMounted, reactive, ref } from 'vue';

import {
  deleteAssignmentApi,
  listAssignmentsApi,
  listDepartmentsApi,
  saveAssignmentApi,
  type SystemApi,
} from '#/api';
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
  Select as TSelect,
  Space as TSpace,
} from 'tdesign-vue-next';

defineOptions({ name: 'SystemAssignments' });

const assignments = ref<SystemApi.Assignment[]>([]);
const departmentOptions = ref<{ label: string; value: string }[]>([]);
const formRef = ref<FormInstanceFunctions>();
const loading = ref(false);
const saving = ref(false);
const total = ref(0);
const visible = ref(false);

const form = reactive<Partial<SystemApi.Assignment>>({
  departmentId: '',
  description: '',
  id: undefined,
  name: '',
});

const rules: FormRules<typeof form> = {
  departmentId: requiredRule('请选择所属部门'),
  name: requiredRule('请输入岗位名称'),
};

const filters = reactive({
  departmentId: '',
  keyword: '',
});
const query = reactive({ ...filters });
const pagination = reactive({ current: 1, pageSize: 30 });

const columns = [
  { colKey: 'name', title: '岗位名称', width: 180 },
  { colKey: 'departmentId', title: '所属部门', width: 220 },
  { colKey: 'description', title: '说明', cell: (...args: any[]) => getCellRow<SystemApi.Assignment>(args[0], args[1])?.description || '-' },
  { colKey: 'actions', title: '操作', width: 150, cell: 'actions' },
];

const tablePagination = computed(() => ({
  current: pagination.current,
  pageSize: pagination.pageSize,
  pageSizeOptions: [30, 50, 100, 200],
  total: total.value,
}));

function departmentName(id?: string) {
  return departmentOptions.value.find((item) => item.value === id)?.label || id || '-';
}

async function load() {
  loading.value = true;
  try {
    const [assignmentResult, departmentResult] = await Promise.all([
      listAssignmentsApi({ ...query, current: pagination.current, pageSize: pagination.pageSize }),
      listDepartmentsApi({ current: 1, pageSize: 9999 }),
    ]);
    assignments.value = assignmentResult.list;
    total.value = assignmentResult.total;
    departmentOptions.value = departmentResult.list.map((department) => ({
      label: department.name,
      value: department.id,
    }));
  } finally {
    loading.value = false;
  }
}

async function handlePageChange(pageInfo: { current: number; pageSize: number }) {
  Object.assign(pagination, pageInfo);
  await load();
}

function open(row?: SystemApi.Assignment) {
  Object.assign(form, {
    departmentId: row?.departmentId || '',
    description: row?.description || '',
    id: row?.id,
    name: row?.name || '',
  });
  visible.value = true;
}

async function search() {
  Object.assign(query, filters);
  pagination.current = 1;
  await load();
}

async function reset() {
  Object.assign(filters, { departmentId: '', keyword: '' });
  await search();
}

async function save() {
  if (saving.value || !(await validateForm(formRef.value))) return;
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

function remove(row: SystemApi.Assignment) {
  confirmAndClose({
    body: `确认删除岗位 ${row.name}？`,
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
  <AdminListPage
    title="岗位管理"
    description="维护后端 SYS_ASSIGNMENT 岗位信息，岗位必须归属一个部门。"
    table-title="岗位列表"
    add-button-text="新增岗位"
    :columns="columns"
    :data="assignments"
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
        <span>岗位信息</span>
        <TInput v-model="filters.keyword" clearable placeholder="岗位名称 / 说明" />
      </label>
      <label class="filter-field">
        <span>部门</span>
        <TSelect v-model="filters.departmentId" clearable filterable placeholder="全部部门" :options="departmentOptions" />
      </label>
    </template>
    <template #departmentId="{ row }">{{ departmentName(row.departmentId) }}</template>
    <template #actions="{ row }">
      <TSpace>
        <RowAction label="编辑" @click="open(row)" />
        <RowAction label="删除" theme="danger" @click="remove(row)" />
      </TSpace>
    </template>
  </AdminListPage>

  <TDialog v-model:visible="visible" header="岗位维护" width="560px" :confirm-btn="{ content: '保存', loading: saving }" @confirm="save">
    <TForm ref="formRef" :data="form" :rules="rules" label-width="96px">
      <TFormItem label="所属部门" name="departmentId">
        <TSelect v-model="form.departmentId" filterable clearable :options="departmentOptions" />
      </TFormItem>
      <TFormItem label="岗位名称" name="name"><TInput v-model="form.name" /></TFormItem>
      <TFormItem label="说明"><TInput v-model="form.description" /></TFormItem>
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
