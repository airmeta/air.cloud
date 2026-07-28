<script lang="ts" setup>
import { computed, useSlots, watch } from 'vue';

import { IconifyIcon } from '@vben/icons';

import { withSerialColumn } from '#/views/_shared/table-columns';
import {
  Button as TButton,
  EnhancedTable as TEnhancedTable,
  Pagination as TPagination,
  Space as TSpace,
  Table as TTable,
  Tag as TTag,
} from 'tdesign-vue-next';

type ListPagination = {
  current: number;
  pageSize: number;
  pageSizeOptions?: number[];
  total?: number;
};

const props = withDefaults(
  defineProps<{
    addButtonText?: string;
    addable?: boolean;
    bordered?: boolean;
    columns: any[];
    data: any[];
    description?: string;
    expandedRowKeys?: Array<number | string>;
    expandedTreeNodes?: Array<number | string>;
    headerTags?: Array<{ label: string; theme?: 'default' | 'primary' | 'success' | 'warning' | 'danger' }>;
    loading?: boolean;
    pagination: ListPagination;
    queryable?: boolean;
    queryButtonText?: string;
    queryDisabled?: boolean;
    refreshable?: boolean;
    resetButtonText?: string;
    resetDisabled?: boolean;
    rowKey?: string;
    rowSelection?: Record<string, any>;
    serial?: boolean;
    selectedRowKeys?: Array<number | string>;
    showFilterForm?: boolean;
    stripe?: boolean;
    tableKey?: number | string;
    tableTitle: string;
    title: string;
    tree?: Record<string, any>;
  }>(),
  {
    addButtonText: '新增',
    addable: true,
    bordered: false,
    queryButtonText: '查询',
    queryable: true,
    refreshable: true,
    resetButtonText: '重置',
    rowKey: 'id',
    serial: true,
    showFilterForm: true,
    stripe: true,
  },
);

const emit = defineEmits<{
  add: [];
  expandChange: [keys: Array<number | string>];
  expandedTreeNodesChange: [nodes: Array<number | string>];
  pageChange: [pageInfo: { current: number; pageSize: number }];
  refresh: [];
  reset: [];
  selectChange: [keys: Array<number | string>, context: any];
  search: [];
}>();

const slots = useSlots();
const tableSlots = computed(() =>
  Object.keys(slots).filter((name) => !['action', 'filters', 'toolbar'].includes(name)),
);
const safeData = computed(() => (Array.isArray(props.data) ? props.data.filter(Boolean) : []));
const total = computed(() => props.pagination.total ?? safeData.value.length);
const pageSizeOptions = computed(() => props.pagination.pageSizeOptions ?? [30, 50, 100, 200]);
const rowSelectionType = computed(() => {
  const type = props.rowSelection?.type;
  return type === 'single' || type === 'multiple' ? type : undefined;
});
const tableColumns = computed(() => {
  const columns = props.serial
    ? withSerialColumn(props.columns, {
        offset: () => (props.pagination.current - 1) * props.pagination.pageSize,
      })
    : props.columns;

  if (!rowSelectionType.value || columns.some((column) => column?.colKey === 'row-select')) {
    return columns;
  }

  return [
    {
      align: 'center' as const,
      colKey: 'row-select',
      type: rowSelectionType.value,
      width: 48,
    },
    ...columns,
  ];
});
const pageData = computed(() => {
  const start = (props.pagination.current - 1) * props.pagination.pageSize;
  return safeData.value.slice(start, start + props.pagination.pageSize);
});
const tableTree = computed(() =>
  props.tree
    ? {
        ...props.tree,
        treeNodeColumnIndex: Number(props.tree.treeNodeColumnIndex || 0) + (props.serial ? 1 : 0),
      }
    : undefined,
);
watch(
  () => [total.value, props.pagination.current, props.pagination.pageSize] as const,
  ([currentTotal, current, pageSize]) => {
    const maxPage = Math.max(1, Math.ceil(currentTotal / pageSize));
    if (current > maxPage) {
      emit('pageChange', { current: maxPage, pageSize });
    }
  },
);

function handlePageChange(pageInfo: { current: number; pageSize: number }) {
  emit('pageChange', pageInfo);
}

function handleSearch(event?: MouseEvent) {
  event?.stopPropagation();
  emit('search');
}

function handleReset(event?: MouseEvent) {
  event?.stopPropagation();
  emit('reset');
}

function normalizeSlotProps(slotProps: any) {
  return {
    ...slotProps,
    row: slotProps?.row ?? slotProps?.data?.row,
  };
}
</script>

<template>
  <div class="admin-list-page">
    <header class="admin-list-page__header">
      <div>
        <h2>{{ title }}</h2>
        <p v-if="description">{{ description }}</p>
      </div>
      <div v-if="headerTags?.length" class="admin-list-page__header-tags">
        <TTag v-for="tag in headerTags" :key="tag.label" :theme="tag.theme || 'primary'" variant="light">
          {{ tag.label }}
        </TTag>
      </div>
    </header>

    <section v-if="showFilterForm && $slots.filters" class="admin-list-page__filters">
      <div class="admin-list-page__filter-content">
        <slot name="filters"></slot>
      </div>
      <TSpace v-if="queryable">
        <TButton theme="primary" :disabled="queryDisabled" :loading="loading" @click.stop="handleSearch">
          <template #icon>
            <IconifyIcon icon="lucide:search" />
          </template>
          {{ queryButtonText }}
        </TButton>
        <TButton theme="default" :disabled="resetDisabled || loading" @click.stop="handleReset">
          <template #icon>
            <IconifyIcon icon="lucide:rotate-ccw" />
          </template>
          {{ resetButtonText }}
        </TButton>
      </TSpace>
    </section>

    <section class="admin-list-page__table-panel">
      <div class="admin-list-page__table-toolbar">
        <div>
          <h3>{{ tableTitle }}</h3>
        </div>
        <TSpace>
          <slot name="toolbar"></slot>
          <TButton v-if="addable" theme="primary" @click="emit('add')">
            <template #icon>
              <IconifyIcon icon="lucide:plus" />
            </template>
            <slot name="action">{{ addButtonText }}</slot>
          </TButton>
          <TButton v-if="refreshable" shape="circle" title="刷新" variant="outline" :loading="loading" @click="emit('refresh')">
            <template #icon>
              <IconifyIcon icon="lucide:refresh-cw" />
            </template>
          </TButton>
        </TSpace>
      </div>

      <TEnhancedTable
        v-if="tree"
        :key="tableKey"
        :row-key="rowKey"
        class="admin-list-page__table"
        :columns="tableColumns"
        :data="pageData"
        :expanded-tree-nodes="expandedTreeNodes"
        :selected-row-keys="selectedRowKeys"
        :row-selection="rowSelection"
        :row-selection-type="rowSelectionType"
        :loading="loading"
        :bordered="bordered"
        hover
        :stripe="stripe"
        :tree="tableTree"
        @expanded-tree-nodes-change="emit('expandedTreeNodesChange', $event)"
        @select-change="(keys, context) => emit('selectChange', keys, context)"
      >
        <template v-for="name in tableSlots" :key="name" #[name]="slotProps">
          <slot :name="name" v-bind="normalizeSlotProps(slotProps)"></slot>
        </template>
      </TEnhancedTable>

      <TTable
        v-else
        :row-key="rowKey"
        class="admin-list-page__table"
        :columns="tableColumns"
        :data="pageData"
        :expanded-row-keys="expandedRowKeys"
        :selected-row-keys="selectedRowKeys"
        :row-selection="rowSelection"
        :row-selection-type="rowSelectionType"
        :loading="loading"
        :bordered="bordered"
        hover
        :stripe="stripe"
        @expand-change="emit('expandChange', $event)"
        @select-change="(keys, context) => emit('selectChange', keys, context)"
      >
        <template v-for="name in tableSlots" :key="name" #[name]="slotProps">
          <slot :name="name" v-bind="normalizeSlotProps(slotProps)"></slot>
        </template>
      </TTable>

      <div class="admin-list-page__pagination">
        <TPagination
          :current="pagination.current"
          :page-size="pagination.pageSize"
          :page-size-options="pageSizeOptions"
          :total="total"
          show-jumper
          show-page-size
          @change="handlePageChange"
        />
      </div>
    </section>
  </div>
</template>

<style scoped>
.admin-list-page {
  display: flex;
  flex-direction: column;
  gap: 12px;
  padding: 12px;
}

.admin-list-page__header,
.admin-list-page__filters,
.admin-list-page__table-panel {
  background: var(--td-bg-color-container);
  border: 1px solid var(--td-component-border);
  border-radius: 6px;
}

.admin-list-page__header {
  display: flex;
  gap: 16px;
  align-items: flex-start;
  justify-content: space-between;
  padding: 14px 16px;
}

.admin-list-page__header h2,
.admin-list-page__table-toolbar h3 {
  margin: 0;
  color: var(--td-text-color-primary);
}

.admin-list-page__header h2 {
  font-size: 18px;
  line-height: 24px;
}

.admin-list-page__header p,
.admin-list-page__table-toolbar span {
  color: var(--td-text-color-secondary);
}

.admin-list-page__header p {
  margin: 4px 0 0;
  line-height: 20px;
}

.admin-list-page__header-tags,
.admin-list-page__filters,
.admin-list-page__filter-content,
.admin-list-page__table-toolbar {
  display: flex;
  gap: 10px;
  align-items: center;
}

.admin-list-page__filters {
  flex-wrap: wrap;
  justify-content: space-between;
  padding: 12px 16px;
}

.admin-list-page__filter-content {
  flex-wrap: wrap;
  gap: 10px 16px;
}

.admin-list-page__table-panel {
  overflow: visible;
}

.admin-list-page__table-toolbar {
  justify-content: space-between;
  padding: 12px 16px;
  border-bottom: 1px solid var(--td-component-border);
}

.admin-list-page__table-toolbar h3 {
  font-size: 16px;
  line-height: 22px;
}

.admin-list-page__table-toolbar span {
  display: inline-block;
  margin-top: 4px;
}

.admin-list-page__table :deep(.t-table th),
.admin-list-page__table :deep(.t-table td) {
  padding-top: 6px;
  padding-bottom: 6px;
}

.admin-list-page__table :deep(.t-table__body tr:nth-child(2n)) {
  background: var(--td-bg-color-secondarycontainer);
}

.admin-list-page__table :deep(.t-table__body tr:hover) {
  background: var(--td-brand-color-light) !important;
}

.admin-list-page__pagination {
  display: flex;
  gap: 12px;
  align-items: center;
  justify-content: space-between;
  padding: 10px 16px;
  border-top: 1px solid var(--td-component-border);
}

@media (max-width: 760px) {
  .admin-list-page__header,
  .admin-list-page__filters,
  .admin-list-page__table-toolbar,
  .admin-list-page__pagination {
    align-items: stretch;
    flex-direction: column;
  }

  .admin-list-page__pagination {
    overflow-x: auto;
  }
}
</style>
