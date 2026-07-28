<script lang="ts" setup>
import type { FormInstanceFunctions, FormRules } from 'tdesign-vue-next';

import { computed, onMounted, reactive, ref } from 'vue';
import { useRouter } from 'vue-router';

import {
  deleteSystemRoleApi,
  listSystemRolesApi,
  saveSystemRoleApi,
  type SystemApi,
} from '#/api';
import AdminListPage from '#/components/admin-list-page/admin-list-page.vue';
import { requiredRule, validateForm } from '#/views/_shared/form-rules';
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
  Textarea as TTextarea,
} from 'tdesign-vue-next';

const loading = ref(false);
const saving = ref(false);
const visible = ref(false);
const formRef = ref<FormInstanceFunctions>();
const router = useRouter();
const roles = ref<SystemApi.Role[]>([]);
const form = reactive<Partial<SystemApi.Role>>({
  code: '',
  description: '',
  id: undefined,
  menuIds: [],
  name: '',
  permissionIds: [],
  status: 1,
});
const rules: FormRules<typeof form> = {
  code: requiredRule('请输入角色编码'),
  name: requiredRule('请输入角色名称'),
};
const filters = reactive({
  grantState: undefined as number | undefined,
  keyword: '',
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
const grantStateOptions = [
  { label: '已授权', value: 1 },
  { label: '未授权', value: 0 },
];
const columns = [
  { colKey: 'code', title: '角色编码', width: 160 },
  { colKey: 'name', title: '角色名称', width: 160 },
  { colKey: 'description', title: '说明' },
  { colKey: 'menuIds', title: '菜单数', width: 100 },
  { colKey: 'permissionIds', title: '按钮权限数', width: 120 },
  { colKey: 'status', title: '状态', width: 100 },
  { colKey: 'actions', title: '操作', width: 220 },
];

const tablePagination = computed(() => ({
  current: pagination.current,
  pageSize: pagination.pageSize,
  pageSizeOptions: [30, 50, 100, 200],
  total: roles.value.length,
}));

function asArray<T>(value: T[] | null | undefined) {
  return Array.isArray(value) ? value : [];
}

function normalizeRole(role: SystemApi.Role): SystemApi.Role {
  return {
    ...role,
    menuIds: asArray(role.menuIds),
    permissionIds: asArray(role.permissionIds),
  };
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
  Object.assign(filters, { grantState: undefined, keyword: '', status: undefined });
  await search();
}

function openEdit(row?: SystemApi.Role) {
  Object.assign(form, {
    code: row?.code || '',
    description: row?.description || '',
    id: row?.id,
    menuIds: [...asArray(row?.menuIds)],
    name: row?.name || '',
    permissionIds: [...asArray(row?.permissionIds)],
    status: row?.status ?? 1,
  });
  visible.value = true;
}

function openAuthorize(row: SystemApi.Role) {
  router.push(`/system/roles/authorize/${row.id}`);
}

async function load() {
  loading.value = true;
  try {
    const roleRows = await listSystemRolesApi(query);
    roles.value = roleRows.map(normalizeRole);
  } finally {
    loading.value = false;
  }
}

async function save() {
  if (saving.value) return;
  if (!(await validateForm(formRef.value))) return;
  saving.value = true;
  try {
    await saveSystemRoleApi(form);
    MessagePlugin.success('角色已保存');
    visible.value = false;
    await load();
  } finally {
    saving.value = false;
  }
}

function remove(row: SystemApi.Role) {
  confirmAndClose({
    body: `确认删除角色 ${row.code}？`,
    confirmBtn: '删除',
    header: '删除角色',
    onConfirm: async () => {
      await deleteSystemRoleApi(row.id);
      MessagePlugin.success('角色已删除');
      await load();
    },
  });
}

onMounted(load);
</script>

<template>
  <AdminListPage
    title="角色管理"
    description="维护角色基础信息，并为角色分配菜单和菜单下的按钮权限。"
    table-title="角色列表"
    add-button-text="新增角色"
    :columns="columns"
    :data="roles"
    :loading="loading"
    :pagination="tablePagination"
    :refreshable="false"
    @add="openEdit()"
    @page-change="handlePageChange"
    @reset="reset"
    @search="search"
  >
    <template #filters>
      <label class="filter-field">
        <span>角色信息</span>
        <TInput v-model="filters.keyword" clearable placeholder="角色编码 / 名称 / 说明" />
      </label>
      <label class="filter-field">
        <span>状态</span>
        <TSelect v-model="filters.status" clearable placeholder="全部状态" :options="statusOptions" />
      </label>
      <label class="filter-field">
        <span>授权状态</span>
        <TSelect v-model="filters.grantState" clearable placeholder="全部授权状态" :options="grantStateOptions" />
      </label>
    </template>
    <template #menuIds="{ row }">{{ asArray(row.menuIds).length }}</template>
    <template #permissionIds="{ row }">{{ asArray(row.permissionIds).length }}</template>
    <template #status="{ row }">
      <TTag :theme="row.status === 1 ? 'success' : 'default'" variant="light">
        {{ row.status === 1 ? '启用' : '停用' }}
      </TTag>
    </template>
    <template #actions="{ row }">
      <TSpace>
        <RowAction v-if="row" label="编辑" @click="openEdit(row)" />
        <RowAction v-if="row" icon="lucide:key-round" label="授权" @click="openAuthorize(row)" />
        <RowAction v-if="row" label="删除" theme="danger" @click="remove(row)" />
      </TSpace>
    </template>
  </AdminListPage>

  <TDialog v-model:visible="visible" header="角色维护" width="720px" :confirm-btn="{ content: '保存', loading: saving }" @confirm="save">
    <TForm ref="formRef" :data="form" :rules="rules" label-width="96px">
      <TFormItem label="角色编码" name="code"><TInput v-model="form.code" /></TFormItem>
      <TFormItem label="角色名称" name="name"><TInput v-model="form.name" /></TFormItem>
      <TFormItem label="说明"><TTextarea v-model="form.description" /></TFormItem>
      <TFormItem label="状态"><TSelect v-model="form.status" :options="statusOptions" /></TFormItem>
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
