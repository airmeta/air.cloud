<script lang="ts" setup>
import { computed, onMounted, reactive, ref } from 'vue';
import type { FormInstanceFunctions, FormRules } from 'tdesign-vue-next';

import {
  deleteSystemPermissionApi,
  listSystemMenusApi,
  listSystemPermissionsApi,
  saveSystemPermissionApi,
  type SystemApi,
} from '#/api';
import SystemPage from '#/views/system/_shared/system-page.vue';
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
const permissions = ref<SystemApi.Permission[]>([]);
const menus = ref<SystemApi.Menu[]>([]);
const form = reactive<Partial<SystemApi.Permission>>({
  code: '',
  id: undefined,
  menuId: '',
  name: '',
});
const rules: FormRules<typeof form> = {
  code: requiredRule('请输入权限码'),
  menuId: requiredRule('请选择关联菜单', 'change'),
  name: requiredRule('请输入名称'),
};
const filters = reactive({
  keyword: '',
  menuId: '',
});
const query = reactive({ ...filters });

const menuOptions = computed(() =>
  menus.value.map((menu) => ({
    label: `${menu.name} (${menu.path})`,
    value: menu.id,
  })),
);
const columns = [
  { colKey: 'code', title: '权限码', width: 220 },
  { colKey: 'name', title: '名称', width: 180 },
  {
    colKey: 'menuId',
    title: '关联菜单',
    cell: (...args: any[]) => {
      const row = getCellRow(args[0], args[1]);
      return row?.menuId ? resolveMenuLabel(row.menuId) : '-';
    },
  },
  { colKey: 'actions', title: '操作', width: 140, cell: 'actions' },
];

function resolveMenuLabel(menuId: string) {
  const menu = menus.value.find((item) => item.id === menuId);
  return menu ? `${menu.name} (${menu.path})` : menuId;
}

async function search() {
  Object.assign(query, filters);
  await load();
}

async function reset() {
  Object.assign(filters, { keyword: '', menuId: '' });
  await search();
}

function open(row?: SystemApi.Permission) {
  Object.assign(form, {
    code: row?.code || '',
    id: row?.id,
    menuId: row?.menuId || '',
    name: row?.name || '',
  });
  visible.value = true;
}

async function load() {
  loading.value = true;
  try {
    const [permissionRows, menuRows] = await Promise.all([listSystemPermissionsApi(query), listSystemMenusApi()]);
    permissions.value = permissionRows;
    menus.value = menuRows;
  } finally {
    loading.value = false;
  }
}

async function save() {
  if (saving.value) return;
  if (!(await validateForm(formRef.value))) return;
  saving.value = true;
  try {
    await saveSystemPermissionApi(form);
    MessagePlugin.success('权限码已保存');
    visible.value = false;
    await load();
  } finally {
    saving.value = false;
  }
}

function remove(row: SystemApi.Permission) {
  confirmAndClose({
    body: `确认删除权限码 ${row.code}？`,
    confirmBtn: '删除',
    header: '删除权限码',
    onConfirm: async () => {
      await deleteSystemPermissionApi(row.id);
      MessagePlugin.success('权限码已删除');
      await load();
    },
  });
}

onMounted(load);
</script>

<template>
  <SystemPage
    title="权限码管理"
    description="维护 RBAC 权限码，供角色授权和后续业务操作校验使用。"
    :columns="columns"
    :data="permissions"
    :loading="loading"
    @add="open()"
    @refresh="load"
    @reset="reset"
    @search="search"
  >
    <template #filters>
      <TInput v-model="filters.keyword" clearable placeholder="权限码 / 名称 / 菜单" class="filter-control" />
      <TSelect v-model="filters.menuId" clearable filterable placeholder="关联菜单" :options="menuOptions" class="filter-control" />
    </template>
    <template #action>新增权限码</template>
    <template #actions="{ row }">
      <TSpace>
        <RowAction v-if="row" label="编辑" @click="open(row)" />
        <RowAction v-if="row" label="删除" theme="danger" @click="remove(row)" />
      </TSpace>
    </template>
  </SystemPage>

  <TDialog v-model:visible="visible" header="权限码维护" width="580px" :confirm-btn="{ content: '保存', loading: saving }" @confirm="save">
    <TForm ref="formRef" :data="form" :rules="rules" label-width="96px">
      <TFormItem label="权限码" name="code"><TInput v-model="form.code" placeholder="System:User:Manage" /></TFormItem>
      <TFormItem label="名称" name="name"><TInput v-model="form.name" /></TFormItem>
      <TFormItem label="关联菜单" name="menuId">
        <TSelect v-model="form.menuId" clearable filterable :options="menuOptions" />
      </TFormItem>
    </TForm>
  </TDialog>
</template>

<style scoped>
.filter-control {
  width: 240px;
}
</style>
