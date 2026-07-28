<script lang="ts" setup>
import type { FormInstanceFunctions, FormRules } from 'tdesign-vue-next';

import { computed, onMounted, reactive, ref } from 'vue';

import {
  deleteSystemUserApi,
  listSystemRolesApi,
  listSystemUsersApi,
  saveSystemUserApi,
  type SystemApi,
} from '#/api';
import AdminListPage from '#/components/admin-list-page/admin-list-page.vue';
import { requiredArrayRule, requiredRule, validateForm } from '#/views/_shared/form-rules';
import { confirmAndClose } from '#/views/_shared/dialog-confirm';
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
} from 'tdesign-vue-next';

const loading = ref(false);
const saving = ref(false);
const visible = ref(false);
const formRef = ref<FormInstanceFunctions>();
const users = ref<SystemApi.User[]>([]);
const roles = ref<SystemApi.Role[]>([]);
const roleOptions = ref<{ label: string; value: string }[]>([]);
const form = reactive<Partial<SystemApi.User> & { password?: string }>({
  displayName: '',
  email: '',
  id: undefined,
  password: '',
  phoneNumber: '',
  roleIds: [],
  status: 1,
  username: '',
});
const rules = computed<FormRules<typeof form>>(() => ({
  displayName: requiredRule('请输入显示名称'),
  password: form.id ? [] : requiredRule('请输入密码'),
  roleIds: requiredArrayRule('请选择角色'),
  username: requiredRule('请输入用户名'),
}));
const filters = reactive({
  keyword: '',
  roleId: '',
  status: undefined as number | undefined,
});
const query = reactive({ ...filters });
const pagination = reactive({
  current: 1,
  pageSize: 30,
});

const statusOptions = [
  { label: '启用', value: 1 },
  { label: '停用', value: 0 },
];
const roleNameMap = computed(() => new Map(roles.value.map((role) => [role.id, role.name])));
const columns = [
  { colKey: 'username', title: '用户名', width: 160 },
  { colKey: 'displayName', title: '显示名称', width: 160 },
  { colKey: 'email', title: '邮箱', width: 220 },
  { colKey: 'phoneNumber', title: '手机号', width: 150 },
  { colKey: 'roleIds', title: '角色' },
  { colKey: 'status', title: '状态', width: 100 },
  { colKey: 'actions', title: '操作', width: 150 },
];

const tablePagination = computed(() => ({
  current: pagination.current,
  pageSize: pagination.pageSize,
  pageSizeOptions: [30, 50, 100, 200],
  total: users.value.length,
}));

function asArray(value: string[] | null | undefined) {
  return Array.isArray(value) ? value : [];
}

function normalizeUser(user: SystemApi.User): SystemApi.User {
  return {
    ...user,
    roleIds: asArray(user.roleIds),
  };
}

function resolveRoleNames(roleIds?: string[] | null) {
  const names = asArray(roleIds).map((id) => roleNameMap.value.get(id) || id);
  return names.length > 0 ? names.join(', ') : '-';
}

function handlePageChange(pageInfo: { current: number; pageSize: number }) {
  pagination.current = pageInfo.current;
  pagination.pageSize = pageInfo.pageSize;
}

async function search() {
  Object.assign(query, filters);
  pagination.current = 1;
  await load();
}

async function reset() {
  Object.assign(filters, { keyword: '', roleId: '', status: undefined });
  await search();
}

function open(row?: SystemApi.User) {
  Object.assign(form, {
    displayName: row?.displayName || '',
    email: row?.email || '',
    id: row?.id,
    password: '',
    phoneNumber: row?.phoneNumber || '',
    roleIds: [...asArray(row?.roleIds)],
    status: row?.status ?? 1,
    username: row?.username || '',
  });
  visible.value = true;
}

async function load() {
  loading.value = true;
  try {
    const [userRows, roleRows] = await Promise.all([listSystemUsersApi(query), listSystemRolesApi()]);
    users.value = userRows.map(normalizeUser);
    roles.value = roleRows;
    roleOptions.value = roleRows.map((role) => ({
      label: `${role.name} (${role.code})`,
      value: role.id,
    }));
  } finally {
    loading.value = false;
  }
}

async function save() {
  if (saving.value) return;
  if (!(await validateForm(formRef.value))) return;
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
    body: `确认删除用户 ${row.username}？`,
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
    description="维护人员、项目成员和系统账号，完成角色授权后可参与对应业务操作。"
    table-title="用户列表"
    add-button-text="新增用户"
    :columns="columns"
    :data="users"
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
        <span>用户信息</span>
        <TInput v-model="filters.keyword" clearable placeholder="用户名 / 显示名称 / 邮箱 / 手机号" />
      </label>
      <label class="filter-field">
        <span>角色</span>
        <TSelect v-model="filters.roleId" clearable placeholder="全部角色" :options="roleOptions" />
      </label>
      <label class="filter-field">
        <span>状态</span>
        <TSelect v-model="filters.status" clearable placeholder="全部状态" :options="statusOptions" />
      </label>
    </template>

    <template #roleIds="{ row }">
      {{ resolveRoleNames(row.roleIds) }}
    </template>
    <template #status="{ row }">
      <TTag :theme="row.status === 1 ? 'success' : 'default'" variant="light">
        {{ row.status === 1 ? '启用' : '停用' }}
      </TTag>
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
    width="640px"
    :confirm-btn="{ content: '保存', loading: saving }"
    @confirm="save"
  >
    <TForm ref="formRef" :data="form" :rules="rules" label-width="96px">
      <TFormItem label="用户名" name="username"><TInput v-model="form.username" /></TFormItem>
      <TFormItem label="显示名称" name="displayName"><TInput v-model="form.displayName" /></TFormItem>
      <TFormItem v-if="!form.id" label="密码" name="password">
        <TInput v-model="form.password" type="password" placeholder="新增必填，编辑留空则不修改" />
      </TFormItem>
      <TFormItem label="邮箱"><TInput v-model="form.email" /></TFormItem>
      <TFormItem label="手机号"><TInput v-model="form.phoneNumber" /></TFormItem>
      <TFormItem label="状态">
        <TSelect v-model="form.status" :options="statusOptions" />
      </TFormItem>
      <TFormItem label="角色" name="roleIds">
        <TSelect v-model="form.roleIds" multiple filterable clearable placeholder="请选择角色" :options="roleOptions" />
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
