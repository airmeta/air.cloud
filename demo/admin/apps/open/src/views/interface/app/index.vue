<script lang="ts" setup>
import type { FormInstanceFunctions, FormRules } from 'tdesign-vue-next';

import { computed, onMounted, reactive, ref } from 'vue';

import { OpenManagementApi, type SystemApi, listApplicationsApi } from '#/api';
import AdminListPage from '@root/base/components/admin-page/admin-list-page.vue';
import { confirmAndClose } from '@root/base/components/admin-page/dialog-confirm';
import {
  requiredRule,
  validateForm,
} from '@root/base/components/admin-page/form-rules';
import RowAction from '@root/base/components/admin-page/row-action.vue';
import { getCellRow } from '@root/base/components/admin-page/table-cell';
import {
  Button as TButton,
  DatePicker as TDatePicker,
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

defineOptions({ name: 'OpenAppInterfaceAuthorization' });

type Authorization = OpenManagementApi.AppInterfaceAuthorizationForm;
const rows = ref<Authorization[]>([]);
const applications = ref<SystemApi.Application[]>([]);
const externals = ref<OpenManagementApi.ExternalInterfaceMapping[]>([]);
const loading = ref(false);
const saving = ref(false);
const visible = ref(false);
const infoVisible = ref(false);
const info = ref<Authorization>();
const formRef = ref<FormInstanceFunctions>();
const filters = reactive({ appId: '', externalInterfaceId: '', keyword: '' });
const query = reactive({ ...filters });
const pagination = reactive({ current: 1, pageSize: 30 });
const total = ref(0);
const form = reactive<Authorization>({
  actionId: '',
  actionSecret: '',
  appId: '',
  deleteRemark: '',
  description: '',
  expiredTime: '',
  externalInterfaceId: '',
  id: '',
});
const rules: FormRules<typeof form> = {
  appId: requiredRule('请选择授权应用', 'change'),
  expiredTime: requiredRule('请选择到期时间', 'change'),
  externalInterfaceId: requiredRule('请选择接口信息', 'change'),
};
const applicationOptions = computed(() =>
  applications.value.map((item) => ({
    label: item.appName || item.appId,
    value: item.appId,
  })),
);
const externalOptions = computed(() =>
  externals.value.map((item) => ({
    label: item.name || item.id,
    value: item.id,
  })),
);
const appName = (id: string) =>
  applications.value.find((item) => item.appId === id)?.appName || id;
const externalName = (id: string) =>
  externals.value.find((item) => item.id === id)?.name || id;
const expires = (value: string) =>
  !value
    ? 'default'
    : new Date(value).getTime() < Date.now()
      ? 'danger'
      : 'success';
const columns = [
  { colKey: 'appId', title: '应用 ID', width: 190, cell: 'appId' },
  {
    colKey: 'externalInterfaceId',
    title: '对外接口',
    minWidth: 190,
    cell: 'externalInterfaceId',
  },
  { colKey: 'actionId', title: '访问信息', minWidth: 180, cell: 'actionId' },
  { colKey: 'expiredTime', title: '到期时间', width: 180, cell: 'expiredTime' },
  { colKey: 'description', title: '描述', minWidth: 200, ellipsis: true },
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
    const result = await OpenManagementApi.appAuthorization.query({
      ...query,
      ...pagination,
    });
    rows.value = result.list;
    total.value = result.total;
  } finally {
    loading.value = false;
  }
}
async function loadOptions() {
  const [apps, mappings] = await Promise.all([
    listApplicationsApi({ current: 1, pageSize: 9999 }),
    OpenManagementApi.externalInterfaces.select({ current: 1, pageSize: 9999 }),
  ]);
  applications.value = apps.list;
  externals.value = mappings;
}
async function search() {
  Object.assign(query, filters);
  pagination.current = 1;
  await load();
}
async function reset() {
  Object.assign(filters, { appId: '', externalInterfaceId: '', keyword: '' });
  await search();
}
function open(row?: Authorization) {
  Object.assign(form, {
    actionId: '',
    actionSecret: '',
    appId: '',
    deleteRemark: '',
    description: '',
    expiredTime: '',
    externalInterfaceId: '',
    id: '',
    ...row,
  });
  visible.value = true;
}
async function edit(row: Authorization) {
  const detail = await OpenManagementApi.appAuthorization.detail(
    row.appId,
    row.actionId,
  );
  open(detail || row);
}
async function save() {
  if (saving.value || !(await validateForm(formRef.value))) return;
  saving.value = true;
  try {
    await OpenManagementApi.appAuthorization.save({ ...form });
    MessagePlugin.success('授权已保存');
    visible.value = false;
    await load();
  } finally {
    saving.value = false;
  }
}
function remove(row: Authorization) {
  confirmAndClose({
    body: `确认删除应用 ${appName(row.appId)} 的接口授权？`,
    confirmBtn: '删除',
    header: '删除授权',
    onConfirm: async () => {
      await OpenManagementApi.appAuthorization.remove({
        actionId: row.actionId,
        appId: row.appId,
        remark: row.deleteRemark,
      });
      MessagePlugin.success('授权已删除');
      await load();
    },
  });
}
async function changePage(page: { current: number; pageSize: number }) {
  Object.assign(pagination, page);
  await load();
}
onMounted(async () => {
  await loadOptions();
  await load();
});
</script>

<template>
  <AdminListPage
    title="应用授权管理"
    description="维护应用访问外部接口的动作密钥和有效期。"
    table-title="授权列表"
    add-button-text="新增授权"
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
  >
    <template #filters
      ><label class="filter"
        ><span>关键字</span
        ><TInput
          v-model="filters.keyword"
          clearable
          placeholder="应用 / 接口 / 动作编号" /></label
      ><label class="filter"
        ><span>授权应用</span
        ><TSelect
          v-model="filters.appId"
          clearable
          filterable
          :options="applicationOptions" /></label
      ><label class="filter"
        ><span>外部接口</span
        ><TSelect
          v-model="filters.externalInterfaceId"
          clearable
          filterable
          :options="externalOptions" /></label
    ></template>
    <template #appId="{ row }"
      ><TTag theme="primary" variant="light">{{
        appName(row.appId)
      }}</TTag></template
    ><template #externalInterfaceId="{ row }">{{
      externalName(row.externalInterfaceId)
    }}</template>
    <template #actionId="{ row }"
      ><TSpace
        ><span>{{ row.actionId || '-' }}</span
        ><TButton
          v-if="row.actionId || row.actionSecret"
          shape="circle"
          size="small"
          variant="text"
          @click="
            info = row;
            infoVisible = true;
          "
          ><template #icon
            ><IconifyIcon icon="lucide:eye" /></template></TButton></TSpace
    ></template>
    <template #expiredTime="{ row }"
      ><TTag :theme="expires(row.expiredTime)" variant="light">{{
        row.expiredTime || '-'
      }}</TTag></template
    ><template #actions="{ row }"
      ><TSpace
        ><RowAction label="编辑" @click="edit(row)" /><RowAction
          label="删除"
          theme="danger"
          @click="remove(row)" /></TSpace
    ></template>
  </AdminListPage>
  <TDrawer
    v-model:visible="visible"
    size="70%"
    :header="form.id ? '编辑应用接口授权' : '新增应用接口授权'"
    :confirm-btn="{ content: '保存', loading: saving }"
    @confirm="save"
    ><TForm ref="formRef" :data="form" :rules="rules" label-width="112px"
      ><TFormItem label="接口信息" name="externalInterfaceId"
        ><TSelect
          v-model="form.externalInterfaceId"
          clearable
          filterable
          :options="externalOptions" /></TFormItem
      ><TFormItem label="授权应用" name="appId"
        ><TSelect
          v-model="form.appId"
          clearable
          filterable
          :options="applicationOptions" /></TFormItem
      ><TFormItem label="到期时间" name="expiredTime"
        ><TDatePicker
          v-model="form.expiredTime"
          enable-time-picker
          format="YYYY-MM-DD HH:mm:ss"
          value-type="YYYY-MM-DD HH:mm:ss" /></TFormItem
      ><TFormItem label="动作编号"
        ><TInput
          v-model="form.actionId"
          placeholder="服务端生成的动作编号" /></TFormItem
      ><TFormItem label="动作密钥"
        ><TTextarea
          v-model="form.actionSecret"
          autosize
          placeholder="服务端生成的动作密钥" /></TFormItem
      ><TFormItem label="描述"
        ><TTextarea v-model="form.description" autosize /></TFormItem
      ><TFormItem label="删除备注"
        ><TTextarea v-model="form.deleteRemark" autosize /></TFormItem></TForm
  ></TDrawer>
  <TDialog
    v-model:visible="infoVisible"
    header="动作信息"
    :footer="false"
    width="460px"
    ><dl v-if="info" class="details">
      <dt>动作编号</dt>
      <dd>{{ info.actionId || '-' }}</dd>
      <dt>动作密钥</dt>
      <dd>{{ info.actionSecret || '-' }}</dd>
    </dl></TDialog
  >
</template>

<style scoped>
.filter {
  display: grid;
  grid-template-columns: auto minmax(180px, 260px);
  gap: 8px;
  align-items: center;
  color: var(--td-text-color-secondary);
}
.details {
  display: grid;
  grid-template-columns: 88px 1fr;
  gap: 12px;
  margin: 0;
}
.details dt {
  color: var(--td-text-color-secondary);
}
.details dd {
  margin: 0;
  overflow-wrap: anywhere;
}
@media (max-width: 760px) {
  .filter {
    grid-template-columns: 1fr;
    width: 100%;
  }
}
</style>
