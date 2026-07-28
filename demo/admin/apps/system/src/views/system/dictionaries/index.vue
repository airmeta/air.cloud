<script lang="ts" setup>
import type { FormInstanceFunctions, FormRules } from 'tdesign-vue-next';

import { IconifyIcon } from '@vben/icons';
import { computed, onMounted, reactive, ref, watch } from 'vue';

import {
  deleteDictionaryItemApi,
  deleteDictionaryTypeApi,
  listDictionaryItemsApi,
  listDictionaryTypesApi,
  saveDictionaryItemApi,
  saveDictionaryTypeApi,
  type SystemApi,
} from '#/api';
import AdminListPage from '@root/base/components/admin-page/admin-list-page.vue';
import { confirmAndClose } from '@root/base/components/admin-page/dialog-confirm';
import { requiredRule, validateForm } from '@root/base/components/admin-page/form-rules';
import RowAction from '@root/base/components/admin-page/row-action.vue';
import { getCellRow } from '@root/base/components/admin-page/table-cell';
import {
  Button as TButton,
  Dialog as TDialog,
  Form as TForm,
  FormItem as TFormItem,
  Input as TInput,
  MessagePlugin,
  Select as TSelect,
  Space as TSpace,
  Textarea as TTextarea,
} from 'tdesign-vue-next';

defineOptions({ name: 'SystemDictionaries' });

const dictionaryItems = ref<SystemApi.DictionaryItem[]>([]);
const dictionaryTypes = ref<SystemApi.DictionaryType[]>([]);
const filters = reactive({ keyword: '' });
const itemFormRef = ref<FormInstanceFunctions>();
const itemLoading = ref(false);
const itemPagination = reactive({ current: 1, pageSize: 30, pageSizeOptions: [30, 50, 100, 200] });
const itemSaving = ref(false);
const itemTotal = ref(0);
const itemVisible = ref(false);
const loading = ref(false);
const query = reactive({ keyword: '' });
const selectedTypeId = ref('');
const typeFormRef = ref<FormInstanceFunctions>();
const typeSaving = ref(false);
const typeVisible = ref(false);

const itemForm = reactive<Partial<SystemApi.DictionaryItem>>({
  code: '',
  description: '',
  dictionaryTypeId: '',
  id: undefined,
  meta: '',
  name: '',
  value: '',
});

const typeForm = reactive<Partial<SystemApi.DictionaryType>>({
  code: '',
  description: '',
  id: undefined,
  meta: '',
  name: '',
  value: '',
});

const itemRules: FormRules<typeof itemForm> = {
  code: requiredRule('请输入字典项编码'),
  dictionaryTypeId: requiredRule('请选择字典类型'),
  name: requiredRule('请输入字典项标签'),
  value: requiredRule('请输入字典项值'),
};

const typeRules: FormRules<typeof typeForm> = {
  code: requiredRule('请输入字典编码'),
  name: requiredRule('请输入字典标签'),
  value: requiredRule('请输入字典值'),
};

const itemColumns = [
  { colKey: 'code', title: '编码', width: 180 },
  { colKey: 'name', title: '标签', width: 160 },
  { colKey: 'value', title: '值', width: 180 },
  {
    cell: (...args: any[]) => getCellRow<SystemApi.DictionaryItem>(args[0], args[1])?.description || '-',
    colKey: 'description',
    ellipsis: true,
    title: '说明',
  },
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
  total: itemTotal.value,
}));

watch(selectedTypeId, async (value) => {
  itemPagination.current = 1;
  if (!value) {
    dictionaryItems.value = [];
    itemTotal.value = 0;
    return;
  }

  await loadItems(value);
});

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
    meta: row?.meta || '',
    name: row?.name || '',
    value: row?.value || row?.code || '',
  });
  itemVisible.value = true;
}

function openType(row?: SystemApi.DictionaryType) {
  Object.assign(typeForm, {
    code: row?.code || '',
    description: row?.description || '',
    id: row?.id,
    meta: row?.meta || '',
    name: row?.name || '',
    value: row?.value || row?.code || '',
  });
  typeVisible.value = true;
}

async function loadItems(dictionaryTypeId = selectedTypeId.value) {
  if (!dictionaryTypeId) return;
  itemLoading.value = true;
  try {
    const result = await listDictionaryItemsApi(dictionaryTypeId, {
      ...query,
      current: itemPagination.current,
      pageSize: itemPagination.pageSize,
    });
    dictionaryItems.value = result.list;
    itemTotal.value = result.total;
  } finally {
    itemLoading.value = false;
  }
}

async function loadTypes() {
  loading.value = true;
  try {
    const result = await listDictionaryTypesApi({ current: 1, pageSize: 9999 });
    dictionaryTypes.value = result.list;
    if (!selectedTypeId.value || !dictionaryTypes.value.some((item) => item.id === selectedTypeId.value)) {
      selectedTypeId.value = dictionaryTypes.value[0]?.id || '';
    }
  } finally {
    loading.value = false;
  }
}

async function reset() {
  Object.assign(filters, { keyword: '' });
  await search();
}

async function search() {
  Object.assign(query, filters);
  itemPagination.current = 1;
  await loadItems();
}

function selectType(row: SystemApi.DictionaryType) {
  selectedTypeId.value = row.id;
}

async function saveItem() {
  if (itemSaving.value || !(await validateForm(itemFormRef.value))) return;
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
  if (typeSaving.value || !(await validateForm(typeFormRef.value))) return;
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

async function handleItemPageChange(pageInfo: { current: number; pageSize: number }) {
  itemPagination.current = pageInfo.current;
  itemPagination.pageSize = pageInfo.pageSize;
  await loadItems();
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
    body: `确认删除字典类型 ${row.code}？`,
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
      <p>维护后端字典配置，保存字段严格对应 Code、Label、Value、Description、Meta 和 ParentId。</p>
    </header>

    <div class="dictionary-page-content">
      <aside class="dictionary-type-panel">
        <header class="dictionary-type-head">
          <div class="dictionary-type-head__top">
            <h2>字典类型</h2>
            <TSpace size="small">
              <TButton shape="circle" theme="primary" title="新增" @click="openType()">
                <template #icon><IconifyIcon icon="lucide:plus" /></template>
              </TButton>
              <TButton shape="circle" title="刷新" variant="outline" :loading="loading" @click="loadTypes">
                <template #icon><IconifyIcon icon="lucide:refresh-cw" /></template>
              </TButton>
            </TSpace>
          </div>
          <p>ParentId 为空的字典配置。</p>
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
            <strong>{{ type.name }}</strong>
            <span>{{ type.code }} / {{ type.value }}</span>
            <span>{{ type.description || '暂无说明' }}</span>
            <span class="dictionary-type-card__actions" @click.stop>
              <RowAction label="编辑" @click="openType(type)" />
              <RowAction label="删除" theme="danger" @click="removeType(type)" />
            </span>
          </button>

          <div v-if="dictionaryTypes.length === 0 && !loading" class="dictionary-empty">暂无字典类型</div>
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
          server-pagination
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
              <TInput v-model="filters.keyword" clearable placeholder="编码 / 标签" class="filter-control" />
            </label>
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
        <TFormItem label="编码" name="code"><TInput v-model="typeForm.code" placeholder="requirement_priority" /></TFormItem>
        <TFormItem label="标签" name="name"><TInput v-model="typeForm.name" placeholder="需求优先级" /></TFormItem>
        <TFormItem label="值" name="value"><TInput v-model="typeForm.value" placeholder="requirement_priority" /></TFormItem>
        <TFormItem label="说明"><TTextarea v-model="typeForm.description" /></TFormItem>
        <TFormItem label="扩展信息"><TTextarea v-model="typeForm.meta" /></TFormItem>
      </TForm>
    </TDialog>

    <TDialog v-model:visible="itemVisible" header="字典项维护" width="560px" :confirm-btn="{ content: '保存', loading: itemSaving }" @confirm="saveItem">
      <TForm ref="itemFormRef" :data="itemForm" :rules="itemRules" label-width="96px">
        <TFormItem label="字典类型" name="dictionaryTypeId"><TSelect v-model="itemForm.dictionaryTypeId" :options="typeOptions" /></TFormItem>
        <TFormItem label="编码" name="code"><TInput v-model="itemForm.code" placeholder="high" /></TFormItem>
        <TFormItem label="标签" name="name"><TInput v-model="itemForm.name" placeholder="高" /></TFormItem>
        <TFormItem label="值" name="value"><TInput v-model="itemForm.value" placeholder="high" /></TFormItem>
        <TFormItem label="说明"><TTextarea v-model="itemForm.description" /></TFormItem>
        <TFormItem label="扩展信息"><TTextarea v-model="itemForm.meta" /></TFormItem>
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

.dictionary-page-header,
.dictionary-type-panel {
  background: var(--td-bg-color-container);
  border: 1px solid var(--td-component-border);
  border-radius: 6px;
}

.dictionary-page-header {
  padding: 14px 16px;
}

.dictionary-page-header h2,
.dictionary-type-head h2 {
  margin: 0;
}

.dictionary-page-header p,
.dictionary-type-head p {
  margin: 4px 0 0;
  color: var(--td-text-color-secondary);
}

.dictionary-page-content {
  display: flex;
  min-height: 0;
  gap: 12px;
}

.dictionary-type-panel {
  display: flex;
  flex: 0 0 300px;
  width: 300px;
  min-height: 0;
  flex-direction: column;
}

.dictionary-item-panel {
  width: calc(100% - 312px);
  min-width: 0;
}

.dictionary-item-panel :deep(.admin-list-page) {
  padding: 0;
}

.dictionary-type-head {
  padding: 14px;
  border-bottom: 1px solid var(--td-component-border);
}

.dictionary-type-head__top {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
}

.dictionary-type-list {
  display: flex;
  min-height: 0;
  flex: 1 1 auto;
  flex-direction: column;
  gap: 10px;
  overflow-y: auto;
  padding: 12px;
}

.dictionary-type-card {
  display: grid;
  width: 100%;
  gap: 6px;
  padding: 12px;
  color: var(--td-text-color-primary);
  text-align: left;
  background: var(--td-bg-color-container);
  border: 1px solid var(--td-component-border);
  border-radius: 6px;
  cursor: pointer;
}

.dictionary-type-card:hover,
.dictionary-type-card.active {
  background: var(--td-brand-color-light);
  border-color: var(--td-brand-color);
}

.dictionary-type-card span,
.dictionary-empty {
  color: var(--td-text-color-secondary);
  font-size: 12px;
}

.dictionary-type-card__actions {
  display: flex;
  gap: 10px;
  padding-top: 6px;
  border-top: 1px solid var(--td-component-border);
}

.filter-field {
  display: inline-flex;
  gap: 8px;
  align-items: center;
  color: var(--td-text-color-primary);
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
}
</style>
