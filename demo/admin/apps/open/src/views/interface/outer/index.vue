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
  Textarea as TTextarea,
} from 'tdesign-vue-next';
defineOptions({ name: 'OpenExternalInterfaceMapping' });
type Item = OpenManagementApi.ExternalInterfaceMapping;
const rows = ref<Item[]>([]);
const internals = ref<OpenManagementApi.InternalInterfaceMapping[]>([]);
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
  enableAppEncrypt: 0,
  id: '',
  internalInterfaceId: '',
  name: '',
  requestParametersText: '[]',
  responseParametersText: '[]',
});
const rules: FormRules<typeof form> = {
  internalInterfaceId: requiredRule('请选择内部接口', 'change'),
  name: requiredRule('请输入名称'),
};
const options = computed(() =>
  internals.value.map((item) => ({
    label: `${item.name}${item.route ? ` - ${item.route}` : ''}`,
    value: item.id,
    disabled: !item.routeAppId,
  })),
);
const columns = [
  { colKey: 'name', title: '名称', width: 170 },
  { colKey: 'description', title: '描述', minWidth: 190, ellipsis: true },
  { colKey: 'internalInterfaceName', title: '内部接口名称', width: 180 },
  {
    colKey: 'internalInterfaceDescription',
    title: '内部接口描述',
    minWidth: 180,
    ellipsis: true,
  },
  {
    colKey: 'enableAppEncrypt',
    title: '启用应用加密',
    width: 130,
    cell: 'encrypt',
  },
  { colKey: 'params', title: '请求/响应参数', width: 170, cell: 'params' },
  { colKey: 'actions', title: '操作', width: 150, cell: 'actions' },
];
const tablePagination = computed(() => ({
  ...pagination,
  pageSizeOptions: [30, 50, 100, 200],
  total: total.value,
}));
function json(text: string, label: string) {
  try {
    const value = JSON.parse(text || '[]');
    if (!Array.isArray(value)) throw new Error();
    return value;
  } catch {
    MessagePlugin.error(`${label}必须是 JSON 数组`);
    return undefined;
  }
}
function text(value: unknown) {
  return JSON.stringify(value || [], null, 2);
}
async function load() {
  loading.value = true;
  try {
    const result = await OpenManagementApi.externalInterfaces.query({
      ...query,
      ...pagination,
    });
    rows.value = result.list;
    total.value = result.total;
  } finally {
    loading.value = false;
  }
}
async function loadInternals() {
  internals.value = await OpenManagementApi.internalInterfaces.select({
    current: 1,
    pageSize: 9999,
  });
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
    enableAppEncrypt: row?.enableAppEncrypt ?? 0,
    id: row?.id || '',
    internalInterfaceId: row?.internalInterfaceId || '',
    name: row?.name || '',
    requestParametersText: text(row?.requestParameters),
    responseParametersText: text(row?.responseParameters),
  });
  visible.value = true;
}
async function edit(row: Item) {
  open(await OpenManagementApi.externalInterfaces.detail(row.id));
}
async function save() {
  if (saving.value || !(await validateForm(formRef.value))) return;
  const requestParameters = json(form.requestParametersText, '请求参数');
  const responseParameters = json(form.responseParametersText, '响应参数');
  if (!requestParameters || !responseParameters) return;
  saving.value = true;
  try {
    await OpenManagementApi.externalInterfaces.save({
      description: form.description,
      enableAppEncrypt: form.enableAppEncrypt,
      id: form.id,
      internalInterfaceId: form.internalInterfaceId,
      name: form.name,
      requestParameters,
      responseParameters,
    });
    MessagePlugin.success('外部接口已保存');
    visible.value = false;
    await load();
  } finally {
    saving.value = false;
  }
}
function remove(row: Item) {
  confirmAndClose({
    body: `确认删除外部接口 ${row.name || row.id}？`,
    confirmBtn: '删除',
    header: '删除外部接口',
    onConfirm: async () => {
      await OpenManagementApi.externalInterfaces.remove(row.id);
      MessagePlugin.success('外部接口已删除');
      await load();
    },
  });
}
async function changePage(page: { current: number; pageSize: number }) {
  Object.assign(pagination, page);
  await load();
}
function show(row: Item) {
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
  await Promise.all([loadInternals(), load()]);
});
</script>
<template>
  <AdminListPage
    title="外网路由映射管理"
    description="维护外部接口、内部接口关联和应用加密要求。"
    table-title="外部接口列表"
    add-button-text="新增外部接口"
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
          placeholder="内部接口 ID / 描述" /></label></template
    ><template #encrypt="{ row }">{{
      row.enableAppEncrypt === 1 ? '是' : '否'
    }}</template
    ><template #params="{ row }"
      ><TButton size="small" variant="text" @click="show(row)"
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
    :header="form.id ? '编辑外部接口' : '新增外部接口'"
    :confirm-btn="{ content: '保存', loading: saving }"
    @confirm="save"
    ><TForm ref="formRef" :data="form" :rules="rules" label-width="126px"
      ><TFormItem label="名称" name="name"
        ><TInput v-model="form.name" /></TFormItem
      ><TFormItem label="内部接口" name="internalInterfaceId"
        ><TSelect
          v-model="form.internalInterfaceId"
          clearable
          filterable
          :options="options" /></TFormItem
      ><TFormItem label="描述"
        ><TTextarea v-model="form.description" autosize /></TFormItem
      ><TFormItem label="启用应用加密"
        ><TSelect
          v-model="form.enableAppEncrypt"
          :options="[
            { label: '是', value: 1 },
            { label: '否', value: 0 },
          ]" /></TFormItem
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
