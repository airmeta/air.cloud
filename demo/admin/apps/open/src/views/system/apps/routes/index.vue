<script lang="ts" setup>
import type { FormInstanceFunctions, FormRules } from 'tdesign-vue-next';
import { computed, onMounted, reactive, ref } from 'vue';
import { listApplicationsApi, OpenManagementApi, type SystemApi } from '#/api';
import AdminListPage from '@root/base/components/admin-page/admin-list-page.vue';
import { confirmAndClose } from '@root/base/components/admin-page/dialog-confirm';
import {
  requiredRule,
  validateForm,
} from '@root/base/components/admin-page/form-rules';
import RowAction from '@root/base/components/admin-page/row-action.vue';
import {
  Button as TButton,
  Drawer as TDrawer,
  Form as TForm,
  FormItem as TFormItem,
  Input as TInput,
  MessagePlugin,
  Select as TSelect,
  Space as TSpace,
  Textarea as TTextarea,
} from 'tdesign-vue-next';
defineOptions({ name: 'OpenApplicationRoutes' });
type Item = OpenManagementApi.AppRoute;
const rows = ref<Item[]>([]);
const apps = ref<SystemApi.Application[]>([]);
const loading = ref(false);
const saving = ref(false);
const visible = ref(false);
const formRef = ref<FormInstanceFunctions>();
const filters = reactive({ keyword: '' });
const query = reactive({ keyword: '' });
const pagination = reactive({ current: 1, pageSize: 30 });
const total = ref(0);
const form = reactive<OpenManagementApi.AppRouteForm>({
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
const appName = (id: string) =>
  apps.value.find((app) => app.appId === id)?.appName || id;
const columns = [
  { colKey: 'appId', title: '所属应用', width: 200, cell: 'appId' },
  { colKey: 'route', title: '路由地址', minWidth: 240 },
  { colKey: 'method', title: '请求方法', width: 120 },
  { colKey: 'description', title: '描述', minWidth: 180, ellipsis: true },
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
    const result = await OpenManagementApi.routes.query({
      ...query,
      ...pagination,
    });
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
function open(row?: Item) {
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
async function edit(row: Item) {
  open(await OpenManagementApi.routes.detail(row.id));
}
function addMeta() {
  form.authorizationMetas ||= [];
  form.authorizationMetas.push({
    authenticationSchemes: '',
    policy: '',
    roles: '',
  });
}
function removeMeta(index: number) {
  form.authorizationMetas?.splice(index, 1);
}
async function save() {
  if (saving.value || !(await validateForm(formRef.value))) return;
  saving.value = true;
  try {
    const payload = {
      ...form,
      authorizationMetas:
        form.allowAnonymous === 1 || form.requiresAuthorization !== 1
          ? []
          : form.authorizationMetas || [],
      requiresAuthorization:
        form.allowAnonymous === 1 ? 0 : form.requiresAuthorization,
    };
    await OpenManagementApi.routes.save(payload);
    MessagePlugin.success('路由已保存');
    visible.value = false;
    await load();
  } finally {
    saving.value = false;
  }
}
function remove(row: Item) {
  confirmAndClose({
    body: `确认删除路由 ${row.route}？`,
    confirmBtn: '删除',
    header: '删除路由',
    onConfirm: async () => {
      await OpenManagementApi.routes.remove(row.id);
      MessagePlugin.success('路由已删除');
      await load();
    },
  });
}
async function changePage(page: { current: number; pageSize: number }) {
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
    description="维护应用路由、访问方式和授权元数据。"
    table-title="路由列表"
    add-button-text="新增路由"
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
        ><span>路由地址</span
        ><TInput
          v-model="filters.keyword"
          clearable
          placeholder="路由地址 / 描述" /></label></template
    ><template #appId="{ row }">{{ appName(row.appId) }}</template
    ><template #actions="{ row }"
      ><TSpace
        ><RowAction label="编辑" @click="edit(row)" /><RowAction
          label="删除"
          theme="danger"
          @click="remove(row)" /></TSpace></template></AdminListPage
  ><TDrawer
    v-model:visible="visible"
    size="840px"
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
          ]" /></TFormItem
      ><template
        v-if="form.allowAnonymous === 0 && form.requiresAuthorization === 1"
        ><TFormItem label="授权元数据"
          ><TButton size="small" @click="addMeta"
            ><template #icon><IconifyIcon icon="lucide:plus" /></template
            >新增授权元数据</TButton
          ></TFormItem
        >
        <section
          v-for="(meta, index) in form.authorizationMetas"
          :key="index"
          class="meta"
        >
          <TInput
            v-model="meta.authenticationSchemes"
            placeholder="AuthenticationSchemes"
          /><TInput v-model="meta.policy" placeholder="Policy" /><TInput
            v-model="meta.roles"
            placeholder="Roles"
          /><TButton
            shape="circle"
            theme="danger"
            variant="text"
            @click="removeMeta(index)"
            ><template #icon><IconifyIcon icon="lucide:trash-2" /></template
          ></TButton></section></template></TForm
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
.meta {
  display: grid;
  grid-template-columns: 1fr 1fr 1fr auto;
  gap: 8px;
  margin: 0 0 12px 112px;
}
@media (max-width: 760px) {
  .filter {
    grid-template-columns: 1fr;
    width: 100%;
  }
  .meta {
    grid-template-columns: 1fr;
    margin-left: 0;
  }
}
</style>
