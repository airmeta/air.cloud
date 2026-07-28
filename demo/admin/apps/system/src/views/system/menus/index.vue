<script lang="ts" setup>
import type { FormInstanceFunctions, FormRules } from 'tdesign-vue-next';

import { computed, onMounted, reactive, ref } from 'vue';

import {
  deleteSystemMenuApi,
  listSystemMenusApi,
  saveSystemMenuApi,
  type SystemApi,
} from '#/api';
import { useAccessStore } from '@vben/stores';
import AdminListPage from '@root/base/components/admin-page/admin-list-page.vue';
import { confirmAndClose } from '@root/base/components/admin-page/dialog-confirm';
import { optionalNumberRule, requiredRule, validateForm } from '@root/base/components/admin-page/form-rules';
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

defineOptions({ name: 'SystemMenus' });

type MenuTreeNode = SystemApi.Menu & {
  children?: MenuTreeNode[];
};

const accessStore = useAccessStore();
const expandedTreeNodes = ref<Array<number | string>>([]);
const formRef = ref<FormInstanceFunctions>();
const loading = ref(false);
const menus = ref<SystemApi.Menu[]>([]);
const saving = ref(false);
const total = ref(0);
const treeVersion = ref(0);
const visible = ref(false);
const filters = reactive({ keyword: '' });
const query = reactive({ keyword: '' });
const pagination = reactive({ current: 1, pageSize: 30 });
const form = reactive<Partial<SystemApi.Menu>>({
  authority: '',
  component: '',
  hide: 0,
  icon: '',
  id: undefined,
  parentId: '',
  path: '',
  sortNumber: 0,
  title: '',
  type: 1,
});

const rules: FormRules<typeof form> = {
  path: requiredRule('请输入路由路径'),
  sortNumber: optionalNumberRule('排序必须是数字'),
  title: requiredRule('请输入菜单标题'),
};

const menuTypeOptions = [
  { label: '组菜单', value: 0 },
  { label: '页面菜单', value: 1 },
  { label: '隐藏路由', value: 2 },
];

const hideOptions = [
  { label: '显示', value: 0 },
  { label: '隐藏', value: 1 },
];

const columns = [
  { colKey: 'title', title: '菜单标题', treeNode: true, width: 220 },
  { colKey: 'path', title: '路由路径', width: 220 },
  { colKey: 'component', title: '组件路径' },
  {
    colKey: 'type',
    title: '类型',
    width: 120,
    cell: (...args: any[]) => menuTypeOptions.find((item) => item.value === getCellRow<SystemApi.Menu>(args[0], args[1])?.type)?.label || '页面菜单',
  },
  { colKey: 'sortNumber', title: '排序', width: 90 },
  {
    colKey: 'hide',
    title: '显示状态',
    width: 100,
    cell: (...args: any[]) => (getCellRow<SystemApi.Menu>(args[0], args[1])?.hide === 1 ? '隐藏' : '显示'),
  },
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
      label: `${menu.title} (${menu.path})`,
      value: menu.id,
    })),
);

const filteredMenus = computed(() => buildMenuTree(menus.value));
const tablePagination = computed(() => ({
  current: pagination.current,
  pageSize: pagination.pageSize,
  pageSizeOptions: [30, 50, 100, 200],
  total: total.value,
}));

function buildMenuTree(source: SystemApi.Menu[]) {
  const nodeMap = new Map<string, MenuTreeNode>();
  source.forEach((menu) => nodeMap.set(menu.id, { ...menu, children: [] }));

  const roots: MenuTreeNode[] = [];
  nodeMap.forEach((node) => {
    if (node.parentId && nodeMap.has(node.parentId)) {
      nodeMap.get(node.parentId)?.children?.push(node);
    } else {
      roots.push(node);
    }
  });

  const sortNodes = (nodes: MenuTreeNode[]) => {
    nodes.sort(
      (left, right) =>
        (left.sortNumber ?? 0) - (right.sortNumber ?? 0) ||
        String(left.title || '').localeCompare(String(right.title || '')),
    );
    nodes.forEach((node) => sortNodes(node.children || []));
  };

  sortNodes(roots);
  return roots;
}

async function load() {
  loading.value = true;
  try {
    const result = await listSystemMenusApi({
      ...query,
      current: pagination.current,
      pageSize: pagination.pageSize,
    });
    menus.value = result.list;
    total.value = result.total;
    accessStore.syncAccessMenusFromSystemMenus(result.list);
    expandedTreeNodes.value = result.list.filter((menu) => menu.type === 0).map((menu) => menu.id);
    treeVersion.value += 1;
  } finally {
    loading.value = false;
  }
}

function open(row?: SystemApi.Menu) {
  Object.assign(form, {
    authority: row?.authority || '',
    component: row?.component || '',
    hide: row?.hide ?? 0,
    icon: row?.icon || '',
    id: row?.id,
    parentId: row?.parentId || '',
    path: row?.path || '',
    sortNumber: row?.sortNumber ?? 0,
    title: row?.title || '',
    type: row?.type ?? 1,
  });
  visible.value = true;
}

async function save() {
  if (saving.value || !(await validateForm(formRef.value))) return;
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
    body: `确认删除菜单 ${row.title}？`,
    confirmBtn: '删除',
    header: '删除菜单',
    onConfirm: async () => {
      await deleteSystemMenuApi(row.id);
      MessagePlugin.success('菜单已删除');
      await load();
    },
  });
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

async function handlePageChange(pageInfo: { current: number; pageSize: number }) {
  Object.assign(pagination, pageInfo);
  await load();
}

onMounted(load);
</script>

<template>
  <AdminListPage
    title="菜单管理"
    description="维护后端 SYS_MENU 菜单树，支持组菜单、页面菜单和隐藏路由。"
    table-title="菜单列表"
    add-button-text="新增菜单"
    :columns="columns"
    :data="filteredMenus"
    :expanded-tree-nodes="expandedTreeNodes"
    :loading="loading"
    :pagination="tablePagination"
    server-pagination
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
        <TInput v-model="filters.keyword" clearable placeholder="标题 / 路径 / 组件 / 图标" />
      </label>
    </template>
    <template #actions="{ row }">
      <TSpace>
        <RowAction v-if="row" label="编辑" @click="open(row)" />
        <RowAction v-if="row" label="删除" theme="danger" @click="remove(row)" />
      </TSpace>
    </template>
  </AdminListPage>

  <TDialog v-model:visible="visible" header="菜单维护" width="700px" :confirm-btn="{ content: '保存', loading: saving }" @confirm="save">
    <TForm ref="formRef" :data="form" :rules="rules" label-width="96px">
      <TFormItem label="上级菜单">
        <TSelect v-model="form.parentId" clearable filterable :options="parentOptions" placeholder="顶级菜单可留空" />
      </TFormItem>
      <TFormItem label="菜单标题" name="title"><TInput v-model="form.title" placeholder="如 用户管理" /></TFormItem>
      <TFormItem label="路由路径" name="path"><TInput v-model="form.path" placeholder="/system/users" /></TFormItem>
      <TFormItem label="组件路径"><TInput v-model="form.component" placeholder="/system/users/index" /></TFormItem>
      <TFormItem label="图标"><TInput v-model="form.icon" placeholder="lucide:users" /></TFormItem>
      <TFormItem label="权限标识"><TInput v-model="form.authority" placeholder="可选，对应 SYS_MENU.AUTHORITY" /></TFormItem>
      <TFormItem label="类型"><TSelect v-model="form.type" :options="menuTypeOptions" /></TFormItem>
      <TFormItem label="显示状态"><TSelect v-model="form.hide" :options="hideOptions" /></TFormItem>
      <TFormItem label="排序" name="sortNumber"><TInput v-model="form.sortNumber" type="number" /></TFormItem>
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
