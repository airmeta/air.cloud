<script lang="ts" setup>
import { computed, nextTick, onMounted, reactive, ref } from 'vue';
import type { FormInstanceFunctions, FormRules } from 'tdesign-vue-next';

import {
  deleteSystemMenuApi,
  deleteSystemPermissionApi,
  listSystemMenusApi,
  listSystemPermissionsApi,
  saveSystemMenuApi,
  saveSystemPermissionApi,
  type SystemApi,
} from '#/api';
import { useAccessStore } from '@vben/stores';
import AdminListPage from '#/components/admin-list-page/admin-list-page.vue';
import RowAction from '#/views/system/_shared/row-action.vue';
import { getCellRow } from '#/views/system/_shared/table-cell';
import { withSerialColumn } from '#/views/_shared/table-columns';
import {
  Button as TButton,
  Dialog as TDialog,
  Form as TForm,
  FormItem as TFormItem,
  Input as TInput,
  MessagePlugin,
  Select as TSelect,
  Space as TSpace,
  Table as TTable,
} from 'tdesign-vue-next';
import { IconifyIcon } from '@vben/icons';
import { optionalNumberRule, requiredRule, validateForm } from '#/views/_shared/form-rules';
import { confirmAndClose } from '#/views/_shared/dialog-confirm';

type MenuTreeNode = SystemApi.Menu & {
  buttonPermissionCount: number;
  children?: MenuTreeNode[];
};

const loading = ref(false);
const saving = ref(false);
const permissionSaving = ref(false);
const visible = ref(false);
const formRef = ref<FormInstanceFunctions>();
const permissionVisible = ref(false);
const permissionFormRef = ref<FormInstanceFunctions>();
const menus = ref<SystemApi.Menu[]>([]);
const permissions = ref<SystemApi.Permission[]>([]);
const accessStore = useAccessStore();
const treeVersion = ref(0);
const expandedTreeNodes = ref<Array<number | string>>([]);
const selectedMenu = ref<SystemApi.Menu>();
const form = reactive<Partial<SystemApi.Menu>>({
  component: '',
  icon: '',
  id: undefined,
  name: '',
  parentId: '',
  path: '',
  sort: 0,
  status: 1,
  type: 1,
});
const permissionForm = reactive<Partial<SystemApi.Permission>>({
  code: '',
  id: undefined,
  menuId: '',
  name: '',
});
const rules: FormRules<typeof form> = {
  name: requiredRule('请输入菜单标识'),
  path: requiredRule('请输入路由路径'),
  sort: optionalNumberRule('排序必须是数字'),
};
const permissionRules: FormRules<typeof permissionForm> = {
  code: requiredRule('请输入权限码'),
  name: requiredRule('请输入按钮名称'),
};
const filters = reactive({
  keyword: '',
  status: undefined as number | undefined,
  type: undefined as number | undefined,
});
const query = reactive({ ...filters });
const pagination = reactive({
  current: 1,
  pageSize: 30,
});

const columns = [
  { colKey: 'name', title: '菜单标识', treeNode: true, width: 220 },
  { colKey: 'path', title: '路由路径', width: 220 },
  { colKey: 'component', title: '组件路径' },
  {
    colKey: 'buttonPermissionCount',
    title: '按钮权限',
    width: 100,
  },
  {
    colKey: 'type',
    title: '类型',
    width: 120,
    cell: (...args: any[]) => {
      const row = getCellRow(args[0], args[1]);
      return row?.type === 0 ? '组菜单' : row?.type === 2 ? '隐藏路由' : '页面菜单';
    },
  },
  { colKey: 'sort', title: '排序', width: 90 },
  {
    colKey: 'status',
    title: '状态',
    width: 100,
    cell: (...args: any[]) => (getCellRow(args[0], args[1])?.status === 1 ? '启用' : '停用'),
  },
  { colKey: 'actions', title: '操作', width: 200, cell: 'actions' },
];
const permissionColumns = [
  { colKey: 'code', title: '权限码', width: 220 },
  { colKey: 'name', title: '按钮名称' },
  { colKey: 'actions', title: '操作', width: 140, cell: 'actions' },
];
const treeConfig = {
  childrenKey: 'children',
  defaultExpandAll: true,
  treeNodeColumnIndex: 0,
};

const parentOptions = computed(() =>
  menus.value
    .filter((menu) => menu.type === 0 || menu.type === 1)
    .map((menu) => ({
      label: `${menu.name} (${menu.path})`,
      value: menu.id,
    })),
);

const filteredMenus = computed(() => {
  return buildMenuTree(menus.value);
});
const tablePagination = computed(() => ({
  current: pagination.current,
  pageSize: pagination.pageSize,
  pageSizeOptions: [30, 50, 100, 200],
  total: filteredMenus.value.length,
}));

const selectedMenuPermissions = computed(() =>
  selectedMenu.value
    ? getMenuPermissions(selectedMenu.value.id).sort((left, right) => left.code.localeCompare(right.code))
    : [],
);

function buildMenuTree(source: SystemApi.Menu[]) {
  const nodeMap = new Map<string, MenuTreeNode>();
  source.forEach((menu) =>
    nodeMap.set(menu.id, {
      ...menu,
      buttonPermissionCount: getMenuPermissions(menu.id).length,
      children: [],
    }),
  );

  const roots: MenuTreeNode[] = [];
  nodeMap.forEach((node) => {
    if (node.parentId && nodeMap.has(node.parentId)) {
      nodeMap.get(node.parentId)?.children?.push(node);
    } else {
      roots.push(node);
    }
  });

  const sortNodes = (nodes: MenuTreeNode[]) => {
    nodes.sort((left, right) => left.sort - right.sort || left.name.localeCompare(right.name));
    nodes.forEach((node) => sortNodes(node.children || []));
  };
  sortNodes(roots);

  return roots;
}

function getMenuPermissions(menuId: string) {
  return permissions.value.filter((permission) => permission.menuId === menuId);
}

async function search() {
  Object.assign(query, filters);
  pagination.current = 1;
  await load();
}

function handlePageChange(pageInfo: { current: number; pageSize: number }) {
  pagination.current = pageInfo.current;
  pagination.pageSize = pageInfo.pageSize;
}

async function reset() {
  Object.assign(filters, { keyword: '', status: undefined, type: undefined });
  await search();
}

function open(row?: SystemApi.Menu) {
  Object.assign(form, {
    component: row?.component || '',
    icon: row?.icon || '',
    id: row?.id,
    name: row?.name || '',
    parentId: row?.parentId || '',
    path: row?.path || '',
    sort: row?.sort ?? 0,
    status: row?.status ?? 1,
    type: row?.type ?? 1,
  });
  visible.value = true;
}

async function load() {
  loading.value = true;
  try {
    const [menuRows, permissionRows] = await Promise.all([
      listSystemMenusApi(query),
      listSystemPermissionsApi(query.keyword ? { keyword: query.keyword } : undefined),
    ]);
    menus.value = menuRows;
    permissions.value = permissionRows;
    accessStore.syncAccessMenusFromSystemMenus(menuRows);
    expandedTreeNodes.value = menus.value.filter((menu) => menu.type === 0).map((menu) => menu.id);
    await nextTick();
    treeVersion.value += 1;
  } finally {
    loading.value = false;
  }
}

function openPermissions(row: SystemApi.Menu) {
  selectedMenu.value = row;
  openPermission();
  permissionVisible.value = true;
}

function openPermission(row?: SystemApi.Permission) {
  if (!selectedMenu.value) {
    return;
  }

  Object.assign(permissionForm, {
    code: row?.code || '',
    id: row?.id,
    menuId: selectedMenu.value.id,
    name: row?.name || '',
  });
}

async function savePermission() {
  if (permissionSaving.value) return;
  if (!selectedMenu.value) {
    return;
  }
  if (!(await validateForm(permissionFormRef.value))) return;

  permissionSaving.value = true;
  try {
    await saveSystemPermissionApi({ ...permissionForm, menuId: selectedMenu.value.id });
    MessagePlugin.success('按钮权限已保存');
    Object.assign(permissionForm, { code: '', id: undefined, menuId: selectedMenu.value.id, name: '' });
    await load();
  } finally {
    permissionSaving.value = false;
  }
}

function removePermission(row: SystemApi.Permission) {
  confirmAndClose({
    body: `确认删除按钮权限 ${row.code}？`,
    confirmBtn: '删除',
    header: '删除按钮权限',
    onConfirm: async () => {
      await deleteSystemPermissionApi(row.id);
      MessagePlugin.success('按钮权限已删除');
      await load();
    },
  });
}

async function save() {
  if (saving.value) return;
  if (!(await validateForm(formRef.value))) return;
  saving.value = true;
  try {
    await saveSystemMenuApi(form);
    MessagePlugin.success('菜单已保存');
    visible.value = false;
    await load();
  } finally {
    saving.value = false;
  }
}

function remove(row: SystemApi.Menu) {
  confirmAndClose({
    body: `确认删除菜单 ${row.name}？`,
    confirmBtn: '删除',
    header: '删除菜单',
    onConfirm: async () => {
      await deleteSystemMenuApi(row.id);
      MessagePlugin.success('菜单已删除');
      await load();
    },
  });
}

onMounted(load);
</script>

<template>
  <AdminListPage
    title="菜单管理"
    description="维护系统菜单树和菜单下的按钮权限，支持组菜单、页面菜单、隐藏详情路由和页面级按钮权限。"
    table-title="菜单列表"
    add-button-text="新增菜单"
    :columns="columns"
    :data="filteredMenus"
    :expanded-tree-nodes="expandedTreeNodes"
    :loading="loading"
    :pagination="tablePagination"
    :refreshable="false"
    :table-key="treeVersion"
    :tree="treeConfig"
    @add="open()"
    @expanded-tree-nodes-change="expandedTreeNodes = $event"
    @page-change="handlePageChange"
    @reset="reset"
    @search="search"
  >
    <template #filters>
      <label class="filter-field">
        <span>菜单信息</span>
        <TInput v-model="filters.keyword" clearable placeholder="名称 / 路径 / 组件 / 图标" />
      </label>
      <label class="filter-field">
        <span>类型</span>
        <TSelect
          v-model="filters.type"
          clearable
          placeholder="全部类型"
          :options="[
            { label: '组菜单', value: 0 },
            { label: '页面菜单', value: 1 },
            { label: '隐藏路由', value: 2 },
          ]"
        />
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
        <RowAction v-if="row" icon="lucide:key-round" label="按钮权限" @click="openPermissions(row)" />
        <RowAction v-if="row" label="编辑" @click="open(row)" />
        <RowAction v-if="row" label="删除" theme="danger" @click="remove(row)" />
      </TSpace>
    </template>
  </AdminListPage>

  <TDialog v-model:visible="visible" header="菜单维护" width="660px" :confirm-btn="{ content: '保存', loading: saving }" @confirm="save">
    <TForm ref="formRef" :data="form" :rules="rules" label-width="96px">
      <TFormItem label="上级菜单">
        <TSelect v-model="form.parentId" clearable filterable :options="parentOptions" placeholder="顶级菜单可留空" />
      </TFormItem>
      <TFormItem label="菜单标识" name="name"><TInput v-model="form.name" placeholder="如 SystemUsers" /></TFormItem>
      <TFormItem label="路由路径" name="path"><TInput v-model="form.path" placeholder="/system/users" /></TFormItem>
      <TFormItem label="组件路径"><TInput v-model="form.component" placeholder="/system/users/index" /></TFormItem>
      <TFormItem label="图标"><TInput v-model="form.icon" placeholder="lucide:users" /></TFormItem>
      <TFormItem label="类型">
        <TSelect
          v-model="form.type"
          :options="[
            { label: '组菜单', value: 0 },
            { label: '页面菜单', value: 1 },
            { label: '隐藏路由', value: 2 },
          ]"
        />
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
      <TFormItem label="排序" name="sort"><TInput v-model="form.sort" type="number" /></TFormItem>
    </TForm>
  </TDialog>

  <TDialog
    v-model:visible="permissionVisible"
    :footer="false"
    :header="`${selectedMenu ? selectedMenu.name : ''} - 按钮权限`"
    width="760px"
  >
    <TForm
      ref="permissionFormRef"
      :data="permissionForm"
      :rules="permissionRules"
      label-width="88px"
      class="permission-form"
    >
      <TFormItem label="权限码" name="code"><TInput v-model="permissionForm.code" placeholder="System:User:Create" /></TFormItem>
      <TFormItem label="按钮名称" name="name"><TInput v-model="permissionForm.name" placeholder="新增用户" /></TFormItem>
      <TFormItem>
        <TSpace>
          <TButton theme="primary" :loading="permissionSaving" @click="savePermission">
            <template #icon>
              <IconifyIcon icon="lucide:save" />
            </template>
            保存按钮权限
          </TButton>
          <TButton @click="openPermission()">
            <template #icon>
              <IconifyIcon icon="lucide:refresh-cw" />
            </template>
            清空
          </TButton>
        </TSpace>
      </TFormItem>
    </TForm>
    <TTable
      row-key="id"
      :columns="withSerialColumn(permissionColumns)"
      :data="selectedMenuPermissions"
      :loading="loading"
      bordered
      hover
      class="permission-table"
      stripe
    >
      <template #actions="{ row }">
        <TSpace>
          <RowAction v-if="row" label="编辑" @click="openPermission(row)" />
          <RowAction v-if="row" label="删除" theme="danger" @click="removePermission(row)" />
        </TSpace>
      </template>
    </TTable>
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

.permission-form {
  margin-bottom: 16px;
}

.permission-table {
  margin-top: 8px;
}
</style>
