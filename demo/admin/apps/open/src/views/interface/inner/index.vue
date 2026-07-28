<script lang="ts" setup>
import type { FormInstanceFunctions, FormRules } from 'tdesign-vue-next';
import { computed, onMounted, reactive, ref } from 'vue';
import { OpenManagementApi } from '#/api';
import AdminListPage from '@root/base/components/admin-page/admin-list-page.vue';
import { confirmAndClose } from '@root/base/components/admin-page/dialog-confirm';
import {
  requiredRule,
  validateForm,
} from '@root/base/components/admin-page/form-rules';
import RowAction from '@root/base/components/admin-page/row-action.vue';
import {
  Button as TButton,
  Dialog as TDialog,
  Drawer as TDrawer,
  Form as TForm,
  FormItem as TFormItem,
  Input as TInput,
  MessagePlugin,
  Select as TSelect,
  Space as TSpace,
  Tag as TTag,
  Textarea as TTextarea,
} from 'tdesign-vue-next';
defineOptions({ name: 'OpenInternalInterfaceMapping' });
type Item = OpenManagementApi.InternalInterfaceMapping;
const rows = ref<Item[]>([]);
const routeOptions = ref<OpenManagementApi.AppRoute[]>([]);
const loading = ref(false);
const saving = ref(false);
const visible = ref(false);
const paramsVisible = ref(false);
const paramsContent = ref('');
const formRef = ref<FormInstanceFunctions>();
const filters = reactive({ keyword: '' });
const query = reactive({ keyword: '' });
const pagination = reactive({ current: 1, pageSize: 30 });
const total = ref(0);
const form = reactive({
  description: '',
  id: '',
  name: '',
  requestParametersText: '[]',
  responseParametersText: '[]',
  routeId: '',
});
const rules: FormRules<typeof form> = {
  name: requiredRule('请输入名称'),
  routeId: requiredRule('请选择接口路由', 'change'),
};
const routeSelectOptions = computed(() =>
  routeOptions.value.map((item) => ({
    label: `${item.route}${item.description ? ` - ${item.description}` : ''}`,
    value: item.id,
  })),
);
const columns = [
  { colKey: 'name', title: '名称', width: 180, cell: 'name' },
  { colKey: 'route', title: '路由', minWidth: 240, cell: 'route' },
  { colKey: 'description', title: '描述', minWidth: 200, ellipsis: true },
  { colKey: 'params', title: '请求/响应参数', width: 180, cell: 'params' },
  { colKey: 'actions', title: '操作', width: 150, cell: 'actions' },
];
const tablePagination = computed(() => ({
  ...pagination,
  pageSizeOptions: [30, 50, 100, 200],
  total: total.value,
}));
function parseParameters(text: string, label: string) {
  try {
    const result = JSON.parse(text || '[]');
    if (!Array.isArray(result)) throw new Error();
    return result;
  } catch {
    MessagePlugin.error(`${label}必须是 JSON 数组`);
    return undefined;
  }
}
function stringify(value: unknown) {
  return JSON.stringify(value || [], null, 2);
}
async function load() {
  loading.value = true;
  try {
    const result = await OpenManagementApi.internalInterfaces.query({
      ...query,
      ...pagination,
    });
    rows.value = result.list;
    total.value = result.total;
  } finally {
    loading.value = false;
  }
}
async function loadRoutes() {
  const result = await OpenManagementApi.routes.query({
    current: 1,
    pageSize: 9999,
  });
  routeOptions.value = result.list;
}
async function search() {
  Object.assign(query, filters);
  pagination.current = 1;
  await load();
}
async function reset() {
  filters.keyword = '';
  await search();
}
function open(row?: Item) {
  Object.assign(form, {
    description: row?.description || '',
    id: row?.id || '',
    name: row?.name || '',
    requestParametersText: stringify(row?.requestParameters),
    responseParametersText: stringify(row?.responseParameters),
    routeId: row?.routeId || '',
  });
  visible.value = true;
}
async function edit(row: Item) {
  open(await OpenManagementApi.internalInterfaces.detail(row.id));
}
async function save() {
  if (saving.value || !(await validateForm(formRef.value))) return;
  const requestParameters = parseParameters(
    form.requestParametersText,
    '请求参数',
  );
  const responseParameters = parseParameters(
    form.responseParametersText,
    '响应参数',
  );
  if (!requestParameters || !responseParameters) return;
  saving.value = true;
  try {
    await OpenManagementApi.internalInterfaces.save({
      description: form.description,
      id: form.id,
      name: form.name,
      requestParameters,
      responseParameters,
      routeId: form.routeId,
    });
    MessagePlugin.success('内网接口已保存');
    visible.value = false;
    await load();
  } finally {
    saving.value = false;
  }
}
function remove(row: Item) {
  confirmAndClose({
    body: `确认删除内网接口 ${row.name}？`,
    confirmBtn: '删除',
    header: '删除内网接口',
    onConfirm: async () => {
      await OpenManagementApi.internalInterfaces.remove(row.id);
      MessagePlugin.success('内网接口已删除');
      await load();
    },
  });
}
async function changePage(page: { current: number; pageSize: number }) {
  Object.assign(pagination, page);
  await load();
}
function showParams(row: Item) {
  paramsContent.value = JSON.stringify(
    {
      requestParameters: row.requestParameters || [],
      responseParameters: row.responseParameters || [],
    },
    null,
    2,
  );
  paramsVisible.value = true;
}
onMounted(async () => {
  await Promise.all([loadRoutes(), load()]);
});
</script>
<template>
  <AdminListPage
    title="内网路由映射管理"
    description="维护对内接口与应用路由的映射及完整请求、响应参数。"
    table-title="内网接口列表"
    add-button-text="新增内网接口"
    :columns="columns"
    :data="rows"
    :loading="loading"
    :pagination="tablePagination"
    server-pagination
    @add="open()"
    @page-change="changePage"
    @refresh="load"
    @reset="reset"
    @search="search"
    ><template #filters
      ><label class="filter"
        ><span>关键字</span
        ><TInput
          v-model="filters.keyword"
          clearable
          placeholder="名称 / 路由 ID / 描述" /></label></template
    ><template #name="{ row }"
      ><TSpace
        ><span>{{ row.name || '-' }}</span
        ><IconifyIcon
          v-if="!row.routeAppId"
          class="warn"
          icon="lucide:triangle-alert" /></TSpace></template
    ><template #route="{ row }"
      ><TSpace
        ><TTag :theme="row.routeAppId ? 'primary' : 'danger'" variant="light">{{
          row.routeAppName || '未关联应用'
        }}</TTag
        ><span>{{ row.route || '-' }}</span></TSpace
      ></template
    ><template #params="{ row }"
      ><TButton size="small" variant="text" @click="showParams(row)"
        >请求 {{ row.requestParameters?.length || 0 }} / 响应
        {{ row.responseParameters?.length || 0
        }}<template #icon
          ><IconifyIcon icon="lucide:eye" /></template></TButton></template
    ><template #actions="{ row }"
      ><TSpace
        ><RowAction label="编辑" @click="edit(row)" /><RowAction
          label="删除"
          theme="danger"
          @click="remove(row)" /></TSpace></template></AdminListPage
  ><TDrawer
    v-model:visible="visible"
    size="70%"
    :header="form.id ? '编辑内网接口' : '新增内网接口'"
    :confirm-btn="{ content: '保存', loading: saving }"
    @confirm="save"
    ><TForm ref="formRef" :data="form" :rules="rules" label-width="112px"
      ><TFormItem label="名称" name="name"
        ><TInput v-model="form.name" /></TFormItem
      ><TFormItem label="接口路由" name="routeId"
        ><TSelect
          v-model="form.routeId"
          clearable
          filterable
          :options="routeSelectOptions" /></TFormItem
      ><TFormItem label="描述"
        ><TTextarea v-model="form.description" autosize /></TFormItem
      ><TFormItem label="请求参数"
        ><TTextarea
          v-model="form.requestParametersText"
          autosize
          placeholder="JSON 数组" /></TFormItem
      ><TFormItem label="响应参数"
        ><TTextarea
          v-model="form.responseParametersText"
          autosize
          placeholder="JSON 数组" /></TFormItem></TForm></TDrawer
  ><TDialog
    v-model:visible="paramsVisible"
    header="参数详情"
    :footer="false"
    width="760px"
  >
    <pre>{{ paramsContent }}</pre>
  </TDialog>
</template>
<style scoped>
.filter {
  display: grid;
  grid-template-columns: auto minmax(220px, 320px);
  gap: 8px;
  align-items: center;
  color: var(--td-text-color-secondary);
}
.warn {
  color: var(--td-warning-color);
}
pre {
  max-height: 60vh;
  margin: 0;
  overflow: auto;
  padding: 12px;
  background: var(--td-bg-color-container-hover);
  border-radius: 6px;
}
@media (max-width: 760px) {
  .filter {
    grid-template-columns: 1fr;
    width: 100%;
  }
}
</style>
