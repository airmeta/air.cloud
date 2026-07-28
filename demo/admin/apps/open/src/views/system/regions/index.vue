<script lang="ts" setup>
import type { FormInstanceFunctions, FormRules } from 'tdesign-vue-next';

import { computed, onMounted, reactive, ref, watch } from 'vue';

import {
  deleteRegionApi,
  listApplicationsApi,
  listRegionsApi,
  saveRegionApi,
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
  Radio as TRadio,
  RadioGroup as TRadioGroup,
  Select as TSelect,
  Space as TSpace,
  Tag as TTag,
  Textarea as TTextarea,
  TreeSelect as TTreeSelect,
} from 'tdesign-vue-next';
import { collectRegionIds, countRegions, filterRegionTree } from './region-tree';

defineOptions({ name: 'SysRegion' });

enum RegionTypeEnum {
  District = 0,
  County = 1,
  Town = 2,
  Village = 3,
}

const appOptions = ref<{ label: string; value: string }[]>([]);
const expandedTreeNodes = ref<string[]>([]);
const filters = reactive({ name: '' });
const form = reactive<Partial<SystemApi.Region>>({
  appId: '',
  code: '',
  description: '',
  id: undefined,
  lngAndSat: '',
  name: '',
  parentId: '',
  parentRegionId: '',
  type: RegionTypeEnum.District,
});
const formRef = ref<FormInstanceFunctions>();
const formVisible = ref(false);
const loading = ref(false);
const pagination = reactive({ current: 1, pageSize: 30 });
const regions = ref<SystemApi.Region[]>([]);
const saving = ref(false);

const rules: FormRules<typeof form> = {
  appId: requiredRule('请选择所属应用'),
  code: requiredRule('请输入区域编码'),
  name: requiredRule('请输入区域名称'),
  type: requiredRule('请选择区域类型'),
};

const regionTypeOptions = [
  { label: '市区', theme: 'warning', value: RegionTypeEnum.District },
  { label: '县区', theme: 'success', value: RegionTypeEnum.County },
  { label: '乡镇/街道', theme: 'primary', value: RegionTypeEnum.Town },
  { label: '村/社居委', theme: 'default', value: RegionTypeEnum.Village },
] as const;

const appNameMap = computed(() => new Map(appOptions.value.map((app) => [app.value, app.label])));
const parentRegionOptions = computed(() => {
  const excludeId = form.id || '';
  const filterTree = (nodes: SystemApi.Region[] = []): SystemApi.Region[] => {
    return nodes.reduce<SystemApi.Region[]>((result, node) => {
      if (node.id === excludeId) {
        return result;
      }
      result.push({ ...node, children: filterTree(node.children || []) });
      return result;
    }, []);
  };
  return filterTree(regions.value);
});

const columns = [
  { colKey: 'name', title: '区域名称', width: 220 },
  { colKey: 'type', title: '区域类型', width: 130, cell: 'type' },
  { colKey: 'code', title: '区域编码', width: 150 },
  { colKey: 'appId', title: '所属应用', width: 180, cell: 'appId' },
  { colKey: 'description', title: '描述', ellipsis: true },
  { colKey: 'lngAndSat', title: '坐标', width: 180 },
  { colKey: 'actions', title: '操作', width: 180, cell: 'actions' },
];

const filteredRegions = computed(() => filterRegionTree(regions.value, filters.name));
const tablePagination = computed(() => ({
  current: pagination.current,
  pageSize: pagination.pageSize,
  pageSizeOptions: [30, 50, 100, 200],
  total: countRegions(filteredRegions.value),
}));

watch(
  filteredRegions,
  (tree) => {
    expandedTreeNodes.value = collectRegionIds(tree);
  },
  { immediate: true },
);

function getRegionType(type?: number) {
  return regionTypeOptions.find((item) => item.value === type) || regionTypeOptions[0];
}

async function loadApps() {
  const result = await listApplicationsApi({ current: 1, pageSize: 9999 });
  appOptions.value = result.list.map((app) => ({ label: app.appName, value: app.appId }));
}

async function load() {
  loading.value = true;
  try {
    const [regionTree] = await Promise.all([
      listRegionsApi({ keyword: filters.name }),
      loadApps(),
    ]);
    regions.value = regionTree;
  } finally {
    loading.value = false;
  }
}

async function search() {
  pagination.current = 1;
  await load();
}

async function reset() {
  filters.name = '';
  pagination.current = 1;
  await load();
}

function open(row?: SystemApi.Region) {
  Object.assign(form, {
    appId: row?.appId || '',
    code: row?.code || '',
    description: row?.description || '',
    id: row?.id,
    lngAndSat: row?.lngAndSat || '',
    name: row?.name || '',
    parentId: row?.parentId || '',
    parentRegionId: row?.parentRegionId || '',
    type: row?.type ?? RegionTypeEnum.District,
  });
  formVisible.value = true;
}

async function save() {
  if (saving.value || !(await validateForm(formRef.value))) return;
  if (form.id && form.parentId === form.id) {
    MessagePlugin.error('父级区域不能为当前区域');
    return;
  }

  saving.value = true;
  try {
    await saveRegionApi(form);
    MessagePlugin.success(form.id ? '修改成功' : '新增成功');
    formVisible.value = false;
    await load();
  } finally {
    saving.value = false;
  }
}

function remove(row: SystemApi.Region) {
  confirmAndClose({
    body: '确认删除已选中的数据项?',
    confirmBtn: '确定',
    header: '警告',
    onConfirm: async () => {
      await deleteRegionApi(row.id);
      MessagePlugin.success('删除成功');
      await load();
    },
  });
}

function handlePageChange(pageInfo: { current: number; pageSize: number }) {
  Object.assign(pagination, pageInfo);
}

function handleExpandedTreeNodesChange(nodes: Array<number | string>) {
  expandedTreeNodes.value = nodes.map(String);
}

onMounted(load);
</script>

<template>
  <AdminListPage
    title="区域管理"
    description="维护区域树、区域编码、所属应用和区域坐标信息。"
    table-title="区域列表"
    add-button-text="添加"
    :columns="columns"
    :data="filteredRegions"
    :expanded-tree-nodes="expandedTreeNodes"
    :loading="loading"
    :pagination="tablePagination"
    :tree="{ childrenKey: 'children', treeNodeColumnIndex: 0 }"
    @add="open()"
    @expanded-tree-nodes-change="handleExpandedTreeNodesChange"
    @page-change="handlePageChange"
    @refresh="load"
    @reset="reset"
    @search="search"
  >
    <template #filters>
      <label class="filter-field">
        <span>区域名称</span>
        <TInput v-model="filters.name" clearable placeholder="区域名称" />
      </label>
    </template>

    <template #type="{ row }">
      <TTag :theme="getRegionType(row.type).theme" variant="light">
        {{ getRegionType(row.type).label }}
      </TTag>
    </template>

    <template #appId="{ row }">
      <TTag theme="primary" variant="light">{{ appNameMap.get(row.appId) || row.appId }}</TTag>
    </template>

    <template #actions="{ row }">
      <TSpace>
        <RowAction label="编辑" @click="open(row)" />
        <RowAction label="删除" theme="danger" @click="remove(row)" />
      </TSpace>
    </template>
  </AdminListPage>

  <TDialog
    v-model:visible="formVisible"
    :header="form.id ? '编辑区域' : '新增区域'"
    width="640px"
    :confirm-btn="{ content: '确定', loading: saving }"
    cancel-btn="取消"
    @confirm="save"
  >
    <TForm ref="formRef" :data="form" :rules="rules" label-width="110px">
      <TFormItem label="应用信息" name="appId">
        <TSelect
          v-model="form.appId"
          :disabled="!!form.id"
          filterable
          clearable
          placeholder="选择所属应用"
          :options="appOptions"
        />
      </TFormItem>

      <TFormItem label="上级区域" name="parentId">
        <TTreeSelect
          v-model="form.parentId"
          clearable
          filterable
          placeholder="选择上级区域"
          :data="parentRegionOptions"
          :keys="{ children: 'children', label: 'name', value: 'id' }"
        />
      </TFormItem>

      <TFormItem label="区域编码" name="code">
        <TInput v-model="form.code" placeholder="请输入区域编码" />
      </TFormItem>

      <TFormItem label="区域名称" name="name">
        <TInput v-model="form.name" placeholder="请输入区域名称" />
      </TFormItem>

      <TFormItem label="区域类型" name="type">
        <TRadioGroup v-model="form.type">
          <TRadio :value="RegionTypeEnum.District">市区</TRadio>
          <TRadio :value="RegionTypeEnum.County">县区</TRadio>
          <TRadio :value="RegionTypeEnum.Town">乡镇/街道</TRadio>
          <TRadio :value="RegionTypeEnum.Village">村/社居委</TRadio>
        </TRadioGroup>
      </TFormItem>

      <TFormItem label="描述" name="description">
        <TTextarea v-model="form.description" />
      </TFormItem>

      <TFormItem label="经纬度" name="lngAndSat">
        <TInput v-model="form.lngAndSat" />
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
