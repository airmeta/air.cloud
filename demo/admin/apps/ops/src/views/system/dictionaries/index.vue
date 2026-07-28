<script lang="ts" setup>
import { computed, onMounted, reactive, ref, watch } from 'vue';
import type { FormInstanceFunctions, FormRules } from 'tdesign-vue-next';

import { IconifyIcon } from '@vben/icons';
import {
  deleteDictionaryItemApi,
  deleteDictionaryTypeApi,
  listDictionaryItemsApi,
  listDictionaryTypesApi,
  saveDictionaryItemApi,
  saveDictionaryTypeApi,
  type SystemApi,
} from '#/api';
import AdminListPage from '#/components/admin-list-page/admin-list-page.vue';
import { optionalNumberRule, requiredRule, validateForm } from '#/views/_shared/form-rules';
import { confirmAndClose } from '#/views/_shared/dialog-confirm';
import RowAction from '#/views/system/_shared/row-action.vue';
import { getCellRow } from '#/views/system/_shared/table-cell';
import {
  Button as TButton,
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

const itemLoading = ref(false);
const itemSaving = ref(false);
const loading = ref(false);
const typeSaving = ref(false);
const itemVisible = ref(false);
const typeVisible = ref(false);
const itemFormRef = ref<FormInstanceFunctions>();
const typeFormRef = ref<FormInstanceFunctions>();
const dictionaryItems = ref<SystemApi.DictionaryItem[]>([]);
const dictionaryTypes = ref<SystemApi.DictionaryType[]>([]);
const selectedTypeId = ref('');
const itemPagination = reactive({
  current: 1,
  pageSize: 30,
  pageSizeOptions: [30, 50, 100, 200],
});

const filters = reactive({
  keyword: '',
  status: undefined as number | undefined,
});
const query = reactive({
  keyword: '',
  status: undefined as number | undefined,
});
const itemForm = reactive<Partial<SystemApi.DictionaryItem>>({
  code: '',
  description: '',
  dictionaryTypeId: '',
  id: undefined,
  name: '',
  sort: 0,
  status: 1,
});
const typeForm = reactive<Partial<SystemApi.DictionaryType>>({
  code: '',
  description: '',
  id: undefined,
  name: '',
  sort: 0,
  status: 1,
});

const itemRules: FormRules<typeof itemForm> = {
  code: requiredRule('请输入字典项编码'),
  dictionaryTypeId: requiredRule('请选择字典类型'),
  name: requiredRule('请输入字典项名称'),
  sort: optionalNumberRule('排序必须是数字'),
};
const typeRules: FormRules<typeof typeForm> = {
  code: requiredRule('请输入字典编码'),
  name: requiredRule('请输入字典名称'),
  sort: optionalNumberRule('排序必须是数字'),
};

const statusOptions = [
  { label: '启用', value: 1 },
  { label: '停用', value: 0 },
];
const itemColumns = [
  { colKey: 'code', title: '字典项编码', width: 180 },
  { colKey: 'name', title: '字典项名称', width: 160 },
  {
    cell: (...args: any[]) => getCellRow<SystemApi.DictionaryItem>(args[0], args[1])?.description || '-',
    colKey: 'description',
    ellipsis: true,
    title: '说明',
    width: 280,
  },
  { colKey: 'sort', title: '排序', width: 80 },
  { colKey: 'status', title: '状态', width: 90 },
  { colKey: 'actions', title: '操作', width: 150, cell: 'actions' },
];

const selectedType = computed(() => dictionaryTypes.value.find((item) => item.id === selectedTypeId.value));
const typeOptions = computed(() =>
  dictionaryTypes.value.map((item) => ({
    label: `${item.name} (${item.code})`,
    value: item.id,
  })),
);
const itemTablePagination = computed(() => ({
  current: itemPagination.current,
  pageSize: itemPagination.pageSize,
  pageSizeOptions: itemPagination.pageSizeOptions,
  total: dictionaryItems.value.length,
}));

watch(selectedTypeId, async (value) => {
  if (!value) {
    dictionaryItems.value = [];
    return;
  }

  await loadItems(value);
});

watch(
  () => dictionaryItems.value.length,
  (total) => {
    const maxPage = Math.max(1, Math.ceil(total / itemPagination.pageSize));
    if (itemPagination.current > maxPage) {
      itemPagination.current = maxPage;
    }
  },
);

function openItem(row?: SystemApi.DictionaryItem) {
  if (!selectedTypeId.value) {
    MessagePlugin.warning('请先选择字典类型');
    return;
  }

  Object.assign(itemForm, {
    code: row?.code || '',
    description: row?.description || '',
    dictionaryTypeId: row?.dictionaryTypeId || selectedTypeId.value,
    id: row?.id,
    name: row?.name || '',
    sort: row?.sort ?? 0,
    status: row?.status ?? 1,
  });
  itemVisible.value = true;
}

function openType(row?: SystemApi.DictionaryType) {
  Object.assign(typeForm, {
    code: row?.code || '',
    description: row?.description || '',
    id: row?.id,
    name: row?.name || '',
    sort: row?.sort ?? 0,
    status: row?.status ?? 1,
  });
  typeVisible.value = true;
}

async function loadItems(dictionaryTypeId = selectedTypeId.value) {
  if (!dictionaryTypeId) return;
  itemLoading.value = true;
  try {
    dictionaryItems.value = await listDictionaryItemsApi(dictionaryTypeId, query);
  } finally {
    itemLoading.value = false;
  }
}

async function loadTypes() {
  loading.value = true;
  try {
    dictionaryTypes.value = await listDictionaryTypesApi();
    if (!selectedTypeId.value || !dictionaryTypes.value.some((item) => item.id === selectedTypeId.value)) {
      selectedTypeId.value = dictionaryTypes.value[0]?.id || '';
    }
  } finally {
    loading.value = false;
  }
}

async function reset() {
  Object.assign(filters, { keyword: '', status: undefined });
  itemPagination.current = 1;
  await search();
}

async function search() {
  Object.assign(query, filters);
  itemPagination.current = 1;
  await loadItems();
}

function selectType(row: SystemApi.DictionaryType) {
  selectedTypeId.value = row.id;
  itemPagination.current = 1;
}

async function saveItem() {
  if (itemSaving.value) return;
  if (!(await validateForm(itemFormRef.value))) return;
  itemSaving.value = true;
  try {
    await saveDictionaryItemApi(itemForm);
    MessagePlugin.success('字典项已保存');
    itemVisible.value = false;
    await loadItems();
  } finally {
    itemSaving.value = false;
  }
}

async function saveType() {
  if (typeSaving.value) return;
  if (!(await validateForm(typeFormRef.value))) return;
  typeSaving.value = true;
  try {
    await saveDictionaryTypeApi(typeForm);
    MessagePlugin.success('字典类型已保存');
    typeVisible.value = false;
    await loadTypes();
  } finally {
    typeSaving.value = false;
  }
}

function handleItemPageChange(pageInfo: { current: number; pageSize: number }) {
  itemPagination.current = pageInfo.current;
  itemPagination.pageSize = pageInfo.pageSize;
}

function removeItem(row: SystemApi.DictionaryItem) {
  confirmAndClose({
    body: `确认删除字典项 ${row.code}？`,
    confirmBtn: '删除',
    header: '删除字典项',
    onConfirm: async () => {
      await deleteDictionaryItemApi(row.id);
      MessagePlugin.success('字典项已删除');
      await loadItems();
    },
  });
}

function removeType(row: SystemApi.DictionaryType) {
  confirmAndClose({
    body: `确认删除字典类型 ${row.code}？该类型下的字典项也会被删除。`,
    confirmBtn: '删除',
    header: '删除字典类型',
    onConfirm: async () => {
      await deleteDictionaryTypeApi(row.id);
      MessagePlugin.success('字典类型已删除');
      await loadTypes();
    },
  });
}

onMounted(loadTypes);
</script>

<template>
  <div class="dictionary-page">
    <header class="dictionary-page-header">
      <h2>字典管理</h2>
      <p>维护系统字典类型和字典项，统一业务枚举、筛选选项和状态展示的基础数据。</p>
    </header>

    <div class="dictionary-page-content">
      <aside class="dictionary-type-panel">
      <header class="dictionary-type-head">
        <div class="dictionary-type-head__top">
          <h2>字典类型</h2>
          <TSpace size="small">
            <TButton shape="circle" theme="primary" title="新增" @click="openType()">
              <template #icon>
                <IconifyIcon icon="lucide:plus" />
              </template>
            </TButton>
            <TButton shape="circle" title="刷新" variant="outline" :loading="loading" @click="loadTypes">
              <template #icon>
                <IconifyIcon icon="lucide:refresh-cw" />
              </template>
            </TButton>
          </TSpace>
        </div>
        <p>维护可复用的业务枚举分类。</p>
      </header>

      <div class="dictionary-type-list">
        <button
          v-for="type in dictionaryTypes"
          :key="type.id"
          class="dictionary-type-card"
          :class="{ active: type.id === selectedTypeId }"
          type="button"
          @click="selectType(type)"
        >
          <span class="dictionary-type-card__head">
            <strong>{{ type.name }}</strong>
            <TTag :theme="type.status === 1 ? 'success' : 'default'" variant="light">
              {{ type.status === 1 ? '启用' : '停用' }}
            </TTag>
          </span>
          <span class="dictionary-type-card__code">{{ type.code }}</span>
          <span class="dictionary-type-card__meta">
            <span>排序 {{ type.sort }}</span>
            <span>{{ type.description || '暂无说明' }}</span>
          </span>
          <span class="dictionary-type-card__actions" @click.stop>
            <RowAction label="编辑" @click="openType(type)" />
            <RowAction label="删除" theme="danger" @click="removeType(type)" />
          </span>
        </button>

        <div v-if="dictionaryTypes.length === 0 && !loading" class="dictionary-empty">
          暂无字典类型
        </div>
      </div>
      </aside>

      <section class="dictionary-item-panel">
      <AdminListPage
        title="字典项"
        :description="selectedType ? `当前类型：${selectedType.name} (${selectedType.code})` : '请先创建或选择字典类型。'"
        table-title="字典项列表"
        add-button-text="新增字典项"
        :addable="!!selectedTypeId"
        :columns="itemColumns"
        :data="dictionaryItems"
        :loading="itemLoading"
        :pagination="itemTablePagination"
        :refreshable="!!selectedTypeId"
        @add="openItem()"
        @page-change="handleItemPageChange"
        @refresh="loadItems()"
        @reset="reset"
        @search="search"
      >
        <template #filters>
          <label class="filter-field">
            <span>关键字</span>
            <TInput v-model="filters.keyword" clearable placeholder="字典项编码 / 名称 / 说明" class="filter-control" />
          </label>
          <label class="filter-field">
            <span>状态</span>
            <TSelect v-model="filters.status" clearable placeholder="状态" :options="statusOptions" class="filter-control" />
          </label>
        </template>
        <template #status="{ row }">
          <TTag :theme="row.status === 1 ? 'success' : 'default'" variant="light">
            {{ row.status === 1 ? '启用' : '停用' }}
          </TTag>
        </template>
        <template #actions="{ row }">
          <TSpace>
            <RowAction label="编辑" @click="openItem(row)" />
            <RowAction label="删除" theme="danger" @click="removeItem(row)" />
          </TSpace>
        </template>
      </AdminListPage>
      </section>
    </div>

    <TDialog v-model:visible="typeVisible" header="字典类型维护" width="560px" :confirm-btn="{ content: '保存', loading: typeSaving }" @confirm="saveType">
      <TForm ref="typeFormRef" :data="typeForm" :rules="typeRules" label-width="96px">
        <TFormItem label="字典编码" name="code"><TInput v-model="typeForm.code" placeholder="requirement_priority" /></TFormItem>
        <TFormItem label="字典名称" name="name"><TInput v-model="typeForm.name" placeholder="需求优先级" /></TFormItem>
        <TFormItem label="说明"><TTextarea v-model="typeForm.description" /></TFormItem>
        <TFormItem label="排序" name="sort"><TInput v-model="typeForm.sort" type="number" /></TFormItem>
        <TFormItem label="状态"><TSelect v-model="typeForm.status" :options="statusOptions" /></TFormItem>
      </TForm>
    </TDialog>

    <TDialog v-model:visible="itemVisible" header="字典项维护" width="560px" :confirm-btn="{ content: '保存', loading: itemSaving }" @confirm="saveItem">
      <TForm ref="itemFormRef" :data="itemForm" :rules="itemRules" label-width="96px">
        <TFormItem label="字典类型" name="dictionaryTypeId"><TSelect v-model="itemForm.dictionaryTypeId" :options="typeOptions" /></TFormItem>
        <TFormItem label="字典项编码" name="code"><TInput v-model="itemForm.code" placeholder="high" /></TFormItem>
        <TFormItem label="字典项名称" name="name"><TInput v-model="itemForm.name" placeholder="高" /></TFormItem>
        <TFormItem label="说明"><TTextarea v-model="itemForm.description" /></TFormItem>
        <TFormItem label="排序" name="sort"><TInput v-model="itemForm.sort" type="number" /></TFormItem>
        <TFormItem label="状态"><TSelect v-model="itemForm.status" :options="statusOptions" /></TFormItem>
      </TForm>
    </TDialog>
  </div>
</template>

<style scoped>
.dictionary-page {
  display: grid;
  height: 100%;
  min-height: 0;
  grid-template-rows: auto minmax(0, 1fr);
  gap: 12px;
  padding: 12px;
}

.dictionary-page-header {
  padding: 14px 16px;
  background: var(--td-bg-color-container);
  border: 1px solid var(--td-component-border);
  border-radius: 6px;
}

.dictionary-page-header h2 {
  margin: 0;
  color: var(--td-text-color-primary);
  font-size: 18px;
  line-height: 24px;
}

.dictionary-page-header p {
  margin: 4px 0 0;
  color: var(--td-text-color-secondary);
  line-height: 20px;
}

.dictionary-page-content {
  display: flex;
  min-height: 0;
  gap: 12px;
}

.dictionary-type-panel {
  --dictionary-type-card-gap: 10px;
  --dictionary-type-card-height: 165px;
  --dictionary-type-description-height: 33px;

  display: flex;
  flex: 0 0 300px;
  width: 300px;
  min-height: 0;
  flex-direction: column;
  background: var(--td-bg-color-container);
  border: 1px solid var(--td-component-border);
  border-radius: 6px;
}

.dictionary-item-panel {
  width: calc(100% - 312px);
  min-width: 0;
}

.dictionary-item-panel :deep(.admin-list-page) {
  padding: 0;
}

.dictionary-type-head {
  display: flex;
  flex-direction: column;
  gap: 6px;
  padding: 14px;
  border-bottom: 1px solid var(--td-component-border);
}

.dictionary-type-head__top {
  display: flex;
  min-width: 0;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
}

.dictionary-type-head h2 {
  margin: 0;
  min-width: 0;
  overflow: hidden;
  font-size: 18px;
  line-height: 24px;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.dictionary-type-head p {
  margin: 0;
  color: var(--td-text-color-secondary);
  line-height: 20px;
}

.dictionary-type-list {
  display: flex;
  min-height: 0;
  flex: 1 1 auto;
  flex-direction: column;
  gap: var(--dictionary-type-card-gap);
  overflow-x: hidden;
  overflow-y: auto;
  padding: 12px;
}

.dictionary-type-card {
  display: flex;
  width: 100%;
  min-height: var(--dictionary-type-card-height);
  flex: 0 0 var(--dictionary-type-card-height);
  flex-direction: column;
  gap: 8px;
  padding: 12px;
  color: var(--td-text-color-primary);
  text-align: left;
  background: var(--td-bg-color-container);
  border: 1px solid var(--td-component-border);
  border-radius: 6px;
  cursor: pointer;
  transition:
    background-color 0.2s ease,
    border-color 0.2s ease,
    box-shadow 0.2s ease;
}

.dictionary-type-card:hover,
.dictionary-type-card.active {
  background: var(--td-brand-color-light);
  border-color: var(--td-brand-color);
  box-shadow: 0 2px 8px rgb(0 0 0 / 6%);
}

.dictionary-type-card__head {
  display: flex;
  gap: 8px;
  align-items: center;
  justify-content: space-between;
}

.dictionary-type-card__head strong {
  min-width: 0;
  overflow: hidden;
  font-size: 14px;
  line-height: 20px;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.dictionary-type-card__code,
.dictionary-type-card__meta,
.dictionary-empty {
  color: var(--td-text-color-secondary);
  font-size: 12px;
  line-height: 18px;
}

.dictionary-type-card__meta {
  display: grid;
  width: 100%;
  max-width: 100%;
  min-width: 0;
  grid-template-columns: minmax(0, 1fr);
  gap: 4px;
  overflow: hidden;
}

.dictionary-type-card__meta > span:nth-child(2) {
  display: -webkit-box;
  width: 100%;
  max-width: 100%;
  height: var(--dictionary-type-description-height);
  min-width: 0;
  overflow: hidden;
  -webkit-box-orient: vertical;
  -webkit-line-clamp: 2;
}

.dictionary-type-card__actions {
  display: flex;
  gap: 10px;
  align-items: center;
  padding-top: 6px;
  border-top: 1px solid var(--td-component-border);
}

.filter-field {
  display: inline-flex;
  gap: 8px;
  align-items: center;
  color: var(--td-text-color-primary);
  font-size: 14px;
  line-height: 22px;
}

.filter-field > span {
  flex: 0 0 auto;
  white-space: nowrap;
}

.filter-control {
  width: 220px;
}

@media (max-width: 1200px) {
  .dictionary-page-content {
    flex-direction: column;
  }

  .dictionary-type-panel,
  .dictionary-item-panel {
    width: 100%;
    flex-basis: auto;
  }

  .dictionary-type-panel {
    min-height: auto;
  }
}
</style>
