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
import AdminListPage from '@root/base/components/admin-page/admin-list-page.vue';
import { confirmAndClose } from '@root/base/components/admin-page/dialog-confirm';
import { requiredRule, validateForm } from '@root/base/components/admin-page/form-rules';
import RowAction from '@root/base/components/admin-page/row-action.vue';
import {
  Dialog as TDialog,
  Form as TForm,
  FormItem as TFormItem,
  Input as TInput,
  MessagePlugin,
  Space as TSpace,
  Textarea as TTextarea,
} from 'tdesign-vue-next';

defineOptions({ name: 'SystemRoles' });

const formRef = ref<FormInstanceFunctions>();
const loading = ref(false);
const router = useRouter();
const roles = ref<SystemApi.Role[]>([]);
const saving = ref(false);
const total = ref(0);
const visible = ref(false);

const form = reactive<Partial<SystemApi.Role>>({
  description: '',
  id: undefined,
  roleName: '',
  targetId: '',
});

const rules: FormRules<typeof form> = {
  roleName: requiredRule('请输入角色名称'),
};

const filters = reactive({ keyword: '' });
const query = reactive({ keyword: '' });
const pagination = reactive({ current: 1, pageSize: 30 });

const columns = [
  { colKey: 'roleName', title: '角色名称', width: 200 },
  { colKey: 'description', title: '说明' },
  { colKey: 'actions', title: '操作', width: 220, cell: 'actions' },
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
    const result = await listSystemRolesApi({
      ...query,
      current: pagination.current,
      pageSize: pagination.pageSize,
    });
    roles.value = result.list;
    total.value = result.total;
  } finally {
    loading.value = false;
  }
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
  Object.assign(filters, { keyword: '' });
  await search();
}

function openEdit(row?: SystemApi.Role) {
  Object.assign(form, {
    description: row?.description || '',
    id: row?.id,
    roleName: row?.roleName || '',
    targetId: row?.targetId || '',
  });
  visible.value = true;
}

function openAuthorize(row: SystemApi.Role) {
  router.push(`/system/roles/authorize/${row.id}`);
}

async function save() {
  if (saving.value || !(await validateForm(formRef.value))) return;
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
    body: `确认删除角色 ${row.roleName}？`,
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
    description="维护后端 SYS_ROLE 角色基础信息，并通过授权页维护角色菜单权限。"
    table-title="角色列表"
    add-button-text="新增角色"
    :columns="columns"
    :data="roles"
    :loading="loading"
    :pagination="tablePagination"
    server-pagination
    :refreshable="false"
    @add="openEdit()"
    @page-change="handlePageChange"
    @reset="reset"
    @search="search"
  >
    <template #filters>
      <label class="filter-field">
        <span>角色信息</span>
        <TInput v-model="filters.keyword" clearable placeholder="角色名称" />
      </label>
    </template>
    <template #actions="{ row }">
      <TSpace>
        <RowAction v-if="row" label="编辑" @click="openEdit(row)" />
        <RowAction v-if="row" icon="lucide:key-round" label="授权" @click="openAuthorize(row)" />
        <RowAction v-if="row" label="删除" theme="danger" @click="remove(row)" />
      </TSpace>
    </template>
  </AdminListPage>

  <TDialog v-model:visible="visible" header="角色维护" width="640px" :confirm-btn="{ content: '保存', loading: saving }" @confirm="save">
    <TForm ref="formRef" :data="form" :rules="rules" label-width="96px">
      <TFormItem label="角色名称" name="roleName"><TInput v-model="form.roleName" /></TFormItem>
      <TFormItem label="说明"><TTextarea v-model="form.description" /></TFormItem>
      <TFormItem label="复制来源"><TInput v-model="form.targetId" placeholder="可选：复制指定角色的菜单/动作权限" /></TFormItem>
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
