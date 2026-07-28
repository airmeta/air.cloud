<script lang="ts" setup>
import type { FormInstanceFunctions, FormRules } from 'tdesign-vue-next';

import { computed, onMounted, reactive, ref } from 'vue';

import {
  deleteSystemUserApi,
  listAssignmentsApi,
  listDepartmentsApi,
  listSystemRolesApi,
  listSystemUsersApi,
  saveSystemUserApi,
  type SystemApi,
} from '#/api';
import AdminListPage from '@root/base/components/admin-page/admin-list-page.vue';
import { confirmAndClose } from '@root/base/components/admin-page/dialog-confirm';
import { requiredArrayRule, requiredRule, validateForm } from '@root/base/components/admin-page/form-rules';
import RowAction from '@root/base/components/admin-page/row-action.vue';
import {
  Dialog as TDialog,
  Form as TForm,
  FormItem as TFormItem,
  Input as TInput,
  MessagePlugin,
  Select as TSelect,
  Space as TSpace,
} from 'tdesign-vue-next';

defineOptions({ name: 'SystemUsers' });

const assignmentOptions = ref<{ label: string; value: string }[]>([]);
const departmentOptions = ref<{ label: string; value: string }[]>([]);
const formRef = ref<FormInstanceFunctions>();
const loading = ref(false);
const roleOptions = ref<{ label: string; value: string }[]>([]);
const roles = ref<SystemApi.Role[]>([]);
const saving = ref(false);
const total = ref(0);
const users = ref<SystemApi.User[]>([]);
const visible = ref(false);

const form = reactive<Partial<SystemApi.User> & { password?: string }>({
  account: '',
  appUserId: '',
  assignmentIds: [],
  departmentIds: [],
  email: '',
  id: undefined,
  idCardNo: '',
  password: '',
  phoneNumber: '',
  roleIds: [],
  userName: '',
});

const rules = computed<FormRules<typeof form>>(() => ({
  account: requiredRule('请输入账号'),
  password: form.id ? [] : requiredRule('请输入密码'),
  roleIds: requiredArrayRule('请选择角色'),
  userName: requiredRule('请输入用户名'),
}));

const filters = reactive({
  departmentId: '',
  keyword: '',
  roleId: '',
});
const query = reactive({ ...filters });
const pagination = reactive({ current: 1, pageSize: 30 });

const roleNameMap = computed(() => new Map(roles.value.map((role) => [role.id, role.roleName])));
const columns = [
  { colKey: 'account', title: '账号', width: 160 },
  { colKey: 'userName', title: '用户名', width: 160 },
  { colKey: 'email', title: '邮箱', width: 220 },
  { colKey: 'phoneNumber', title: '手机号', width: 150 },
  { colKey: 'roleIds', title: '角色' },
  { colKey: 'actions', title: '操作', width: 150, cell: 'actions' },
];

const tablePagination = computed(() => ({
  current: pagination.current,
  pageSize: pagination.pageSize,
  pageSizeOptions: [30, 50, 100, 200],
  total: total.value,
}));

function asArray(value: string[] | null | undefined) {
  return Array.isArray(value) ? value : [];
}

function resolveRoleNames(roleIds?: string[] | null) {
  const names = asArray(roleIds).map((id) => roleNameMap.value.get(id) || id);
  return names.length > 0 ? names.join(', ') : '-';
}

async function handlePageChange(pageInfo: { current: number; pageSize: number }) {
  Object.assign(pagination, pageInfo);
  await load();
}

async function search() {
  Object.assign(query, filters);
  pagination.current = 1;
  await load();
}

async function reset() {
  Object.assign(filters, { departmentId: '', keyword: '', roleId: '' });
  await search();
}

function open(row?: SystemApi.User) {
  Object.assign(form, {
    account: row?.account || '',
    appUserId: row?.appUserId || '',
    assignmentIds: [...asArray(row?.assignmentIds)],
    departmentIds: [...asArray(row?.departmentIds)],
    email: row?.email || '',
    id: row?.id,
    idCardNo: row?.idCardNo || '',
    password: '',
    phoneNumber: row?.phoneNumber || '',
    roleIds: [...asArray(row?.roleIds)],
    userName: row?.userName || '',
  });
  visible.value = true;
}

async function load() {
  loading.value = true;
  try {
    const [userResult, roleResult, departmentResult, assignmentResult] = await Promise.all([
      listSystemUsersApi({ ...query, current: pagination.current, pageSize: pagination.pageSize }),
      listSystemRolesApi({ current: 1, pageSize: 9999 }),
      listDepartmentsApi({ current: 1, pageSize: 9999 }),
      listAssignmentsApi({ current: 1, pageSize: 9999 }),
    ]);
    users.value = userResult.list;
    total.value = userResult.total;
    roles.value = roleResult.list;
    roleOptions.value = roleResult.list.map((role) => ({ label: role.roleName, value: role.id }));
    departmentOptions.value = departmentResult.list.map((department) => ({ label: department.name, value: department.id }));
    assignmentOptions.value = assignmentResult.list.map((assignment) => ({ label: assignment.name, value: assignment.id }));
  } finally {
    loading.value = false;
  }
}

async function save() {
  if (saving.value || !(await validateForm(formRef.value))) return;
  saving.value = true;
  try {
    await saveSystemUserApi(form);
    MessagePlugin.success('用户已保存');
    visible.value = false;
    await load();
  } finally {
    saving.value = false;
  }
}

function remove(row: SystemApi.User) {
  confirmAndClose({
    body: `确认删除用户 ${row.account}？`,
    confirmBtn: '删除',
    header: '删除用户',
    onConfirm: async () => {
      await deleteSystemUserApi(row.id);
      MessagePlugin.success('用户已删除');
      await load();
    },
  });
}

onMounted(load);
</script>

<template>
  <AdminListPage
    title="用户管理"
    description="维护后端 SYS_USER 用户基础信息，并提交角色、部门和岗位关联。"
    table-title="用户列表"
    add-button-text="新增用户"
    :columns="columns"
    :data="users"
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
        <span>用户信息</span>
        <TInput v-model="filters.keyword" clearable placeholder="账号 / 用户名 / 邮箱 / 证件号 / 手机号" />
      </label>
      <label class="filter-field">
        <span>角色</span>
        <TSelect v-model="filters.roleId" clearable placeholder="全部角色" :options="roleOptions" />
      </label>
      <label class="filter-field">
        <span>部门</span>
        <TSelect v-model="filters.departmentId" clearable placeholder="全部部门" :options="departmentOptions" />
      </label>
    </template>

    <template #roleIds="{ row }">
      {{ resolveRoleNames(row.roleIds) }}
    </template>
    <template #actions="{ row }">
      <TSpace>
        <RowAction label="编辑" @click="open(row)" />
        <RowAction label="删除" theme="danger" @click="remove(row)" />
      </TSpace>
    </template>
  </AdminListPage>

  <TDialog
    v-model:visible="visible"
    header="用户维护"
    width="720px"
    :confirm-btn="{ content: '保存', loading: saving }"
    @confirm="save"
  >
    <TForm ref="formRef" :data="form" :rules="rules" label-width="96px">
      <TFormItem label="账号" name="account"><TInput v-model="form.account" /></TFormItem>
      <TFormItem label="用户名" name="userName"><TInput v-model="form.userName" /></TFormItem>
      <TFormItem v-if="!form.id" label="密码" name="password">
        <TInput v-model="form.password" type="password" placeholder="新增用户必填" />
      </TFormItem>
      <TFormItem label="应用用户ID"><TInput v-model="form.appUserId" placeholder="新增时可留空" /></TFormItem>
      <TFormItem label="证件号码"><TInput v-model="form.idCardNo" /></TFormItem>
      <TFormItem label="邮箱"><TInput v-model="form.email" /></TFormItem>
      <TFormItem label="手机号"><TInput v-model="form.phoneNumber" /></TFormItem>
      <TFormItem label="角色" name="roleIds">
        <TSelect v-model="form.roleIds" multiple filterable clearable placeholder="请选择角色" :options="roleOptions" />
      </TFormItem>
      <TFormItem label="部门">
        <TSelect v-model="form.departmentIds" multiple filterable clearable placeholder="请选择部门" :options="departmentOptions" />
      </TFormItem>
      <TFormItem label="岗位">
        <TSelect v-model="form.assignmentIds" multiple filterable clearable placeholder="请选择岗位" :options="assignmentOptions" />
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
