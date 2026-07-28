<script lang="ts" setup>
import type { FormInstanceFunctions, FormRules } from 'tdesign-vue-next';
import { computed, onMounted, reactive, ref } from 'vue';
import { AppRouteApi, listApplicationsApi, type SystemApi } from '#/api';
import AdminListPage from '@root/base/components/admin-page/admin-list-page.vue';
import { confirmAndClose } from '@root/base/components/admin-page/dialog-confirm';
import {
  requiredRule,
  validateForm,
} from '@root/base/components/admin-page/form-rules';
import RowAction from '@root/base/components/admin-page/row-action.vue';
import {
  Drawer as TDrawer,
  Form as TForm,
  FormItem as TFormItem,
  Input as TInput,
  MessagePlugin,
  Select as TSelect,
  Space as TSpace,
  Textarea as TTextarea,
} from 'tdesign-vue-next';

defineOptions({ name: 'SystemApplicationRoutes' });

const apps = ref<SystemApi.Application[]>([]);
const rows = ref<AppRouteApi.Route[]>([]);
const loading = ref(false);
const saving = ref(false);
const visible = ref(false);
const total = ref(0);
const formRef = ref<FormInstanceFunctions>();
const filters = reactive({ keyword: '' });
const query = reactive({ keyword: '' });
const pagination = reactive({ current: 1, pageSize: 30 });
const form = reactive<AppRouteApi.Route>({
  allowAnonymous: 0,
  appId: '',
  authorizationMetas: [],
  description: '',
  id: '',
  method: '',
  requiresAuthorization: 0,
  route: '',
});
const rules: FormRules<typeof form> = {
  appId: requiredRule('请选择所属应用', 'change'),
  route: requiredRule('请输入路由地址'),
};
const appOptions = computed(() =>
  apps.value.map((item) => ({
    label: item.appName || item.appId,
    value: item.appId,
  })),
);
const applicationName = (appId: string) =>
  apps.value.find((app) => app.appId === appId)?.appName || appId;
const columns = [
  { colKey: 'appId', title: '所属应用', width: 220, cell: 'appName' },
  { colKey: 'route', title: '路由地址', minWidth: 240 },
  { colKey: 'method', title: '请求方法', width: 120 },
  { colKey: 'description', title: '描述', minWidth: 200, ellipsis: true },
  { colKey: 'createTime', title: '创建时间', width: 180 },
  { colKey: 'actions', title: '操作', width: 150, cell: 'actions' },
];
const tablePagination = computed(() => ({
  ...pagination,
  pageSizeOptions: [30, 50, 100, 200],
  total: total.value,
}));
async function load() {
  loading.value = true;
  try {
    const result = await AppRouteApi.query({ ...query, ...pagination });
    rows.value = result.list;
    total.value = result.total;
  } finally {
    loading.value = false;
  }
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
function open(row?: AppRouteApi.Route) {
  Object.assign(form, {
    allowAnonymous: row?.allowAnonymous ?? 0,
    appId: row?.appId || '',
    authorizationMetas: row?.authorizationMetas || [],
    description: row?.description || '',
    id: row?.id || '',
    method: row?.method || '',
    requiresAuthorization: row?.requiresAuthorization ?? 0,
    route: row?.route || '',
  });
  visible.value = true;
}
async function edit(row: AppRouteApi.Route) {
  open(await AppRouteApi.detail(row.id));
}
async function save() {
  if (saving.value || !(await validateForm(formRef.value))) return;
  saving.value = true;
  try {
    await AppRouteApi.save({
      ...form,
      authorizationMetas:
        form.allowAnonymous === 1 || form.requiresAuthorization !== 1
          ? []
          : form.authorizationMetas,
      requiresAuthorization:
        form.allowAnonymous === 1 ? 0 : form.requiresAuthorization,
    });
    MessagePlugin.success('路由已保存');
    visible.value = false;
    await load();
  } finally {
    saving.value = false;
  }
}
function remove(row: AppRouteApi.Route) {
  confirmAndClose({
    body: `确认删除路由 ${row.route}？`,
    confirmBtn: '删除',
    header: '删除路由',
    onConfirm: async () => {
      await AppRouteApi.remove(row.id);
      MessagePlugin.success('路由已删除');
      await load();
    },
  });
}
async function pageChange(page: { current: number; pageSize: number }) {
  Object.assign(pagination, page);
  await load();
}
onMounted(async () => {
  const result = await listApplicationsApi({ current: 1, pageSize: 9999 });
  apps.value = result.list;
  await load();
});
</script>
<template>
  <AdminListPage
    title="路由管理"
    description="维护所有应用的服务路由和访问策略。"
    table-title="路由列表"
    add-button-text="新增路由"
    :columns="columns"
    :data="rows"
    :loading="loading"
    :pagination="tablePagination"
    server-pagination
    @add="open()"
    @page-change="pageChange"
    @refresh="load"
    @reset="reset"
    @search="search"
    ><template #filters
      ><label class="filter"
        ><span>路由地址</span
        ><TInput
          v-model="filters.keyword"
          clearable
          placeholder="路由地址 / 描述" /></label></template
    ><template #appName="{ row }">{{ applicationName(row.appId) }}</template
    ><template #actions="{ row }"
      ><TSpace
        ><RowAction label="编辑" @click="edit(row)" /><RowAction
          label="删除"
          theme="danger"
          @click="remove(row)" /></TSpace></template></AdminListPage
  ><TDrawer
    v-model:visible="visible"
    size="760px"
    header="路由维护"
    :confirm-btn="{ content: '保存', loading: saving }"
    @confirm="save"
    ><TForm ref="formRef" :data="form" :rules="rules" label-width="112px"
      ><TFormItem label="所属应用" name="appId"
        ><TSelect
          v-model="form.appId"
          clearable
          filterable
          :options="appOptions" /></TFormItem
      ><TFormItem label="路由地址" name="route"
        ><TInput v-model="form.route" /></TFormItem
      ><TFormItem label="请求方法"
        ><TInput
          v-model="form.method"
          placeholder="GET / POST / PUT ..." /></TFormItem
      ><TFormItem label="描述"
        ><TTextarea v-model="form.description" autosize /></TFormItem
      ><TFormItem label="允许匿名访问"
        ><TSelect
          v-model="form.allowAnonymous"
          :options="[
            { label: '是', value: 1 },
            { label: '否', value: 0 },
          ]" /></TFormItem
      ><TFormItem v-if="form.allowAnonymous === 0" label="需要授权"
        ><TSelect
          v-model="form.requiresAuthorization"
          :options="[
            { label: '是', value: 1 },
            { label: '否', value: 0 },
          ]" /></TFormItem></TForm
  ></TDrawer>
</template>
<style scoped>
.filter {
  display: grid;
  grid-template-columns: auto minmax(220px, 320px);
  gap: 8px;
  align-items: center;
  color: var(--td-text-color-secondary);
}
@media (max-width: 760px) {
  .filter {
    grid-template-columns: 1fr;
    width: 100%;
  }
}
</style>
