<script lang="ts" setup>
import type { FormInstanceFunctions, FormRules } from 'tdesign-vue-next';

import { computed, onMounted, reactive, ref } from 'vue';

import {
  deleteApplicationApi,
  listApplicationsApi,
  saveApplicationApi,
  type SystemApi,
} from '#/api';
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
  Dialog as TDialog,
  Form as TForm,
  FormItem as TFormItem,
  Input as TInput,
  MessagePlugin,
  Select as TSelect,
  Space as TSpace,
  Textarea as TTextarea,
} from 'tdesign-vue-next';

defineOptions({ name: 'SystemApplications' });

const applications = ref<SystemApi.Application[]>([]);
const detail = ref<SystemApi.Application>();
const detailVisible = ref(false);
const filters = reactive({ keyword: '' });
const form = reactive<Partial<SystemApi.Application>>({
  appEncryptType: undefined,
  appId: '',
  appName: '',
  appPrivateKey: '',
  appRedirectUrl: '',
  description: '',
  enableMFA: 0,
  id: undefined,
  isCommonApp: 0,
  isEnable: 1,
  logo: '',
});
const logoFileInputRef = ref<HTMLInputElement>();
const formRef = ref<FormInstanceFunctions>();
const formVisible = ref(false);
const loading = ref(false);
const pagination = reactive({ current: 1, pageSize: 30 });
const query = reactive({ keyword: '' });
const saving = ref(false);
const total = ref(0);

const rules: FormRules<typeof form> = {
  appId: requiredRule('请输入应用 ID'),
  appName: requiredRule('请输入应用名称'),
  appEncryptType: requiredRule('请选择加密方式', 'change'),
  appPrivateKey: requiredRule('请输入应用私钥'),
};

const yesNoOptions = [
  { label: '否', value: 0 },
  { label: '是', value: 1 },
];

const encryptOptions = [
  { label: 'RSA', value: 0 },
  { label: 'SM2', value: 1 },
];

const columns = [
  { colKey: 'appName', title: '应用名称', width: 200 },
  { colKey: 'appId', title: '应用 ID', width: 280 },
  { colKey: 'appRedirectUrl', title: '回调地址', ellipsis: true },
  {
    colKey: 'isEnable',
    title: '状态',
    width: 100,
    cell: (...args: any[]) =>
      getCellRow<SystemApi.Application>(args[0], args[1])?.isEnable === 1
        ? '启用'
        : '停用',
  },
  { colKey: 'actions', title: '操作', width: 190, cell: 'actions' },
];

const tablePagination = computed(() => ({
  current: pagination.current,
  pageSize: pagination.pageSize,
  pageSizeOptions: [30, 50, 100, 200],
  total: total.value,
}));

async function load() {
  loading.value = true;
  try {
    const result = await listApplicationsApi({
      ...query,
      current: pagination.current,
      pageSize: pagination.pageSize,
    });
    applications.value = result.list;
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
  Object.assign(filters, { keyword: '' });
  await search();
}

function open(row?: SystemApi.Application) {
  Object.assign(form, {
    appEncryptType: row?.appEncryptType,
    appId: row?.appId || '',
    appName: row?.appName || '',
    appPrivateKey: row?.appPrivateKey || '',
    appRedirectUrl: row?.appRedirectUrl || '',
    description: row?.description || '',
    enableMFA: row?.enableMFA ?? 0,
    id: row?.id,
    isCommonApp: row?.isCommonApp ?? 0,
    isEnable: row?.isEnable ?? 1,
    logo: row?.logo || '',
  });
  formVisible.value = true;
}

function chooseLogo() {
  logoFileInputRef.value?.click();
}
function clearLogo() {
  form.logo = '';
}
function readLogoFile(event: Event) {
  const input = event.target as HTMLInputElement;
  const file = input.files?.[0];
  input.value = '';
  if (!file) return;
  if (!file.type.startsWith('image/')) {
    MessagePlugin.warning('请选择图片文件');
    return;
  }
  if (file.size > 2 * 1024 * 1024) {
    MessagePlugin.warning('Logo 图片不能超过 2 MB');
    return;
  }
  const reader = new FileReader();
  reader.onerror = () => MessagePlugin.error('Logo 图片读取失败');
  reader.onload = () => {
    if (
      typeof reader.result !== 'string' ||
      !reader.result.startsWith('data:image/')
    ) {
      MessagePlugin.error('Logo 图片格式无效');
      return;
    }
    form.logo = reader.result;
  };
  reader.readAsDataURL(file);
}

function openDetail(row: SystemApi.Application) {
  detail.value = row;
  detailVisible.value = true;
}

async function save() {
  if (saving.value || !(await validateForm(formRef.value))) return;
  saving.value = true;
  try {
    await saveApplicationApi(form);
    MessagePlugin.success('应用已保存');
    formVisible.value = false;
    await load();
  } finally {
    saving.value = false;
  }
}

function remove(row: SystemApi.Application) {
  confirmAndClose({
    body: `确认删除应用 ${row.appName}？`,
    confirmBtn: '删除',
    header: '删除应用',
    onConfirm: async () => {
      await deleteApplicationApi(row.appId);
      MessagePlugin.success('应用已删除');
      await load();
    },
  });
}

async function handlePageChange(pageInfo: {
  current: number;
  pageSize: number;
}) {
  Object.assign(pagination, pageInfo);
  await load();
}

onMounted(load);
</script>

<template>
  <AdminListPage
    title="应用管理"
    description="维护后端 SYS_APP 应用注册信息。"
    table-title="应用列表"
    add-button-text="新增应用"
    :columns="columns"
    :data="applications"
    :loading="loading"
    :pagination="tablePagination"
    server-pagination
    @add="open()"
    @page-change="handlePageChange"
    @refresh="load"
    @reset="reset"
    @search="search"
  >
    <template #filters>
      <label class="filter-field">
        <span>应用名称 / 应用 ID</span>
        <TInput
          v-model="filters.keyword"
          clearable
          placeholder="输入名称或应用 ID"
        />
      </label>
    </template>
    <template #actions="{ row }">
      <TSpace>
        <RowAction icon="lucide:eye" label="查看" @click="openDetail(row)" />
        <RowAction label="编辑" @click="open(row)" />
        <RowAction label="删除" theme="danger" @click="remove(row)" />
      </TSpace>
    </template>
  </AdminListPage>

  <TDialog
    v-model:visible="detailVisible"
    header="应用详情"
    :footer="false"
    width="640px"
  >
    <dl v-if="detail" class="detail-list">
      <dt>应用名称</dt>
      <dd>{{ detail.appName }}</dd>
      <dt>应用 ID</dt>
      <dd>{{ detail.appId }}</dd>
      <dt>回调地址</dt>
      <dd>{{ detail.appRedirectUrl || '-' }}</dd>
      <dt>描述</dt>
      <dd>{{ detail.description || '-' }}</dd>
    </dl>
  </TDialog>

  <TDialog
    v-model:visible="formVisible"
    header="应用维护"
    width="720px"
    :confirm-btn="{ content: '保存', loading: saving }"
    @confirm="save"
  >
    <TForm ref="formRef" :data="form" :rules="rules" label-width="120px">
      <TFormItem label="应用 ID" name="appId"
        ><TInput v-model="form.appId"
      /></TFormItem>
      <TFormItem label="应用名称" name="appName"
        ><TInput v-model="form.appName"
      /></TFormItem>
      <TFormItem label="回调地址"
        ><TInput v-model="form.appRedirectUrl"
      /></TFormItem>
      <TFormItem label="加密方式" name="appEncryptType"
        ><TSelect
          v-model="form.appEncryptType"
          :options="encryptOptions"
          placeholder="请选择加密方式"
      /></TFormItem>
      <TFormItem label="应用私钥" name="appPrivateKey">
        <TTextarea
          v-model="form.appPrivateKey"
          placeholder="填写接入应用的私钥，用于平台解密该应用请求"
        />
      </TFormItem>
      <TFormItem label="Logo">
        <input
          ref="logoFileInputRef"
          accept="image/*"
          class="logo-file-input"
          type="file"
          @change="readLogoFile"
        />
        <div class="logo-picker">
          <img
            v-if="form.logo"
            :src="form.logo"
            alt="应用 Logo"
            class="logo-preview"
          />
          <div v-else class="logo-placeholder">
            <IconifyIcon icon="lucide:image" />
          </div>
          <TSpace
            ><TButton variant="outline" @click="chooseLogo"
              ><template #icon><IconifyIcon icon="lucide:image-up" /></template
              >选择图片</TButton
            ><TButton
              v-if="form.logo"
              theme="danger"
              variant="text"
              @click="clearLogo"
              ><template #icon><IconifyIcon icon="lucide:x" /></template
              >清除</TButton
            ></TSpace
          >
        </div>
      </TFormItem>
      <TFormItem label="公共应用"
        ><TSelect v-model="form.isCommonApp" :options="yesNoOptions"
      /></TFormItem>
      <TFormItem label="多因素认证"
        ><TSelect v-model="form.enableMFA" :options="yesNoOptions"
      /></TFormItem>
      <TFormItem label="是否启用"
        ><TSelect v-model="form.isEnable" :options="yesNoOptions"
      /></TFormItem>
      <TFormItem label="描述"
        ><TTextarea v-model="form.description"
      /></TFormItem>
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

.detail-list {
  display: grid;
  grid-template-columns: 120px minmax(0, 1fr);
  gap: 12px 16px;
  margin: 0;
}
.detail-list dt {
  color: var(--td-text-color-secondary);
}
.detail-list dd {
  margin: 0;
  overflow-wrap: anywhere;
}
.logo-file-input {
  display: none;
}
.logo-picker {
  display: flex;
  gap: 12px;
  align-items: center;
  min-height: 64px;
}
.logo-preview,
.logo-placeholder {
  display: grid;
  flex: 0 0 64px;
  width: 64px;
  height: 64px;
  place-items: center;
  overflow: hidden;
  color: var(--td-text-color-placeholder);
  background: var(--td-bg-color-container-hover);
  border: 1px solid var(--td-component-stroke);
  border-radius: 6px;
}
.logo-preview {
  object-fit: cover;
}

@media (max-width: 760px) {
  .filter-field {
    grid-template-columns: 1fr;
    width: 100%;
  }
}
</style>
