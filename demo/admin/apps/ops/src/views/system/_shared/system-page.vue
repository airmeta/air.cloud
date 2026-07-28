<script lang="ts" setup>
import { computed, reactive, watch } from 'vue';

import AdminListPage from '#/components/admin-list-page/admin-list-page.vue';

const props = withDefaults(defineProps<{
  addable?: boolean;
  columns: any[];
  data: any[];
  description?: string;
  expandedTreeNodes?: Array<number | string>;
  loading?: boolean;
  showFilterForm?: boolean;
  tableKey?: number | string;
  title: string;
  tree?: Record<string, any>;
}>(), {
  addable: true,
  showFilterForm: true,
});

const emit = defineEmits<{
  add: [];
  expandedTreeNodesChange: [nodes: Array<number | string>];
  refresh: [];
  reset: [];
  search: [];
}>();

const safeData = computed(() => (Array.isArray(props.data) ? props.data.filter(Boolean) : []));
const pagination = reactive({
  current: 1,
  pageSize: 30,
  pageSizeOptions: [30, 50, 100, 200],
  total: 0,
});

watch(
  () => safeData.value.length,
  (total) => {
    pagination.total = total;
    const maxPage = Math.max(1, Math.ceil(total / pagination.pageSize));
    if (pagination.current > maxPage) {
      pagination.current = maxPage;
    }
  },
  { immediate: true },
);

function handlePageChange(pageInfo: { current: number; pageSize: number }) {
  pagination.current = pageInfo.current;
  pagination.pageSize = pageInfo.pageSize;
}
</script>

<template>
  <AdminListPage
    :addable="addable"
    :columns="columns"
    :data="safeData"
    :description="description || '维护当前项目的 RBAC 主体、菜单授权和通用组织关系。'"
    :expanded-tree-nodes="expandedTreeNodes"
    :loading="loading"
    :pagination="pagination"
    :show-filter-form="showFilterForm"
    :table-key="tableKey"
    :table-title="`${title}列表`"
    :title="title"
    :tree="tree"
    @add="emit('add')"
    @expanded-tree-nodes-change="emit('expandedTreeNodesChange', $event)"
    @page-change="handlePageChange"
    @refresh="emit('refresh')"
    @reset="emit('reset')"
    @search="emit('search')"
  >
    <template v-if="$slots.filters" #filters>
      <slot name="filters"></slot>
    </template>
    <template v-if="$slots.toolbar" #toolbar>
      <slot name="toolbar"></slot>
    </template>
    <template v-if="$slots.action" #action>
      <slot name="action"></slot>
    </template>
    <template v-for="(_, name) in $slots" :key="name" #[name]="slotProps">
      <slot v-if="!['filters', 'toolbar', 'action'].includes(String(name))" :name="name" v-bind="slotProps"></slot>
    </template>
  </AdminListPage>
</template>
