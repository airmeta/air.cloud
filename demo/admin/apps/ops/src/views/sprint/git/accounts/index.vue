<script lang="ts" setup>
import type { SprintGitApi } from '#/api/sprint/git';
import type { FormInstanceFunctions, FormRules } from 'tdesign-vue-next';

import { computed, h, onMounted, reactive, ref } from 'vue';

import { IconifyIcon } from '@vben/icons';

import AdminListPage from '#/components/admin-list-page/admin-list-page.vue';
import {
  createGitAccountApi,
  listGitAccountsApi,
  updateGitAccountApi,
} from '#/api/sprint/git';
import { formatDateTime } from '#/views/_shared/date-format';
import { requiredRule, validateForm } from '#/views/_shared/form-rules';
import RowAction from '#/views/system/_shared/row-action.vue';
import {
  Drawer as TDrawer,
  Form as TForm,
  FormItem as TFormItem,
  Input as TInput,
  MessagePlugin,
  Select as TSelect,
  Space as TSpace,
  Tag as TTag,
  Textarea as TTextarea,
  Tooltip as TTooltip,
} from 'tdesign-vue-next';

defineOptions({ name: 'SprintGitAccounts' });

const loading = ref(false);
const saving = ref(false);
const drawerVisible = ref(false);
const editingId = ref('');
const formRef = ref<FormInstanceFunctions>();
const accounts = ref<SprintGitApi.GitAccount[]>([]);
const filters = reactive({
  keyword: '',
  status: '',
});
const query = reactive({
  keyword: '',
  status: '',
});
const pagination = reactive({
  current: 1,
  pageSize: 30,
});
const form = reactive<SprintGitApi.SaveGitAccountRequest>({
  accessToken: '',
  commitAuthorEmail: '',
  commitAuthorName: '',
  description: '',
  name: '',
  status: 'active',
  username: '',
});
const rules: FormRules<typeof form> = {
  name: requiredRule('请输入账户名称'),
  username: requiredRule('请输入Git用户名'),
};
const statusOptions = [
  { label: '启用', value: 'active' },
  { label: '停用', value: 'disabled' },
];
const accessTokenTips = [
  '访问令牌只在新增或重新填写时提交，列表和编辑页不会回显明文。',
  'GitHub 填 Personal Access Token，不是账号密码。',
  'GitLab 建议填 Personal Access Token、Project Access Token 或 Deploy Token 的 token 值。',
  'Deploy Token 需要用户名填写平台生成的 deploy token username，访问令牌填写 token/password。',
  '用于推送时，令牌必须具备写仓库权限。',
];

const tablePagination = computed(() => ({
  current: pagination.current,
  pageSize: pagination.pageSize,
  pageSizeOptions: [30, 50, 100, 200],
  total: accounts.value.length,
}));
const columns = [
  { colKey: 'code', title: '账户编码', width: 180, ellipsis: true },
  { colKey: 'name', title: '账户名称', minWidth: 160, ellipsis: true },
  { colKey: 'username', title: 'Git用户名', minWidth: 160, ellipsis: true },
  { colKey: 'hasAccessToken', title: renderAccessTokenTitle, minWidth: 220, width: 220 },
  { colKey: 'commitAuthorName', title: '提交作者', minWidth: 160, ellipsis: true },
  { colKey: 'commitAuthorEmail', title: '提交邮箱', minWidth: 200, ellipsis: true },
  { colKey: 'status', title: '状态', width: 100 },
  { colKey: 'createTime', title: '创建时间', width: 180 },
  { colKey: 'actions', title: '操作', width: 120, fixed: 'right' as const },
];

function renderAccessTokenTitle() {
  return h('span', { class: 'field-label-with-tip' }, [
    '访问令牌',
    h(
      TTooltip,
      { placement: 'top', theme: 'light' },
      {
        content: () =>
          h(
            'div',
            { class: 'access-token-tip' },
            accessTokenTips.map((item) => h('p', { key: item }, item)),
          ),
        default: () =>
          h(IconifyIcon, {
            class: 'access-token-warning-icon',
            icon: 'lucide:circle-alert',
          }),
      },
    ),
  ]);
}

function handlePageChange(pageInfo: { current: number; pageSize: number }) {
  pagination.current = pageInfo.current;
  pagination.pageSize = pageInfo.pageSize;
}

function resetForm(row?: SprintGitApi.GitAccount) {
  editingId.value = row?.id || '';
  Object.assign(form, {
    accessToken: '',
    commitAuthorEmail: row?.commitAuthorEmail || '',
    commitAuthorName: row?.commitAuthorName || '',
    description: row?.description || '',
    name: row?.name || '',
    status: row?.status || 'active',
    username: row?.username || '',
  });
}

function openCreate() {
  resetForm();
  drawerVisible.value = true;
}

function openEdit(row: SprintGitApi.GitAccount) {
  resetForm(row);
  drawerVisible.value = true;
}

async function search() {
  Object.assign(query, filters);
  pagination.current = 1;
  await loadAccounts();
}

async function reset() {
  Object.assign(filters, { keyword: '', status: '' });
  await search();
}

async function loadAccounts() {
  loading.value = true;
  try {
    accounts.value = await listGitAccountsApi({
      keyword: query.keyword || undefined,
      status: query.status || undefined,
    });
  } finally {
    loading.value = false;
  }
}

async function saveAccount() {
  if (saving.value) return;
  if (!(await validateForm(formRef.value))) return;

  saving.value = true;
  try {
    const payload: SprintGitApi.SaveGitAccountRequest = {
      accessToken: form.accessToken?.trim() || undefined,
      commitAuthorEmail: form.commitAuthorEmail?.trim() || undefined,
      commitAuthorName: form.commitAuthorName?.trim() || undefined,
      description: form.description?.trim() || undefined,
      name: form.name.trim(),
      status: form.status,
      username: form.username.trim(),
    };
    if (editingId.value) {
      await updateGitAccountApi(editingId.value, payload);
    } else {
      await createGitAccountApi(payload);
    }
    MessagePlugin.success('Git账户已保存');
    drawerVisible.value = false;
    await loadAccounts();
  } finally {
    saving.value = false;
  }
}

onMounted(loadAccounts);
</script>

<template>
  <div>
    <AdminListPage
      title="Git账户管理"
      description="维护仓库访问使用的Git用户名和访问令牌，编码由后台自动生成。"
      table-title="Git账户列表"
      add-button-text="新增账户"
      :columns="columns"
      :data="accounts"
      :loading="loading"
      :pagination="tablePagination"
      @add="openCreate"
      @page-change="handlePageChange"
      @refresh="loadAccounts"
      @reset="reset"
      @search="search"
    >
      <template #filters>
        <label class="filter-field">
          <span>账户信息</span>
          <TInput v-model="filters.keyword" clearable placeholder="编码 / 名称 / 用户名" />
        </label>
        <label class="filter-field">
          <span>状态</span>
          <TSelect v-model="filters.status" clearable placeholder="全部状态" :options="statusOptions" />
        </label>
      </template>
      <template #hasAccessToken="{ row }">
        <TTag :theme="row?.hasAccessToken ? 'success' : 'default'" variant="light">
          {{ row?.hasAccessToken ? '已配置' : '未配置' }}
        </TTag>
      </template>
      <template #status="{ row }">
        <TTag :theme="row?.status === 'active' ? 'success' : 'default'" variant="light">
          {{ row?.status === 'active' ? '启用' : '停用' }}
        </TTag>
      </template>
      <template #createTime="{ row }">
        {{ formatDateTime(row?.createTime) }}
      </template>
      <template #actions="{ row }">
        <TSpace>
          <RowAction icon="lucide:pencil" label="编辑" theme="primary" @click="openEdit(row)" />
        </TSpace>
      </template>
    </AdminListPage>

    <TDrawer
      v-model:visible="drawerVisible"
      :confirm-btn="{ content: '保存', loading: saving }"
      :header="editingId ? '编辑Git账户' : '新增Git账户'"
      size="520px"
      @confirm="saveAccount"
    >
      <TForm ref="formRef" :data="form" :rules="rules" label-width="108px">
        <TFormItem label="账户名称" name="name">
          <TInput v-model="form.name" placeholder="GitHub主账户" />
        </TFormItem>
        <TFormItem label="Git用户名" name="username">
          <TInput v-model="form.username" placeholder="git username" />
        </TFormItem>
        <TFormItem>
          <template #label>
            <span class="field-label-with-tip">
              访问令牌
              <TTooltip placement="top" theme="light">
                <template #content>
                  <div class="access-token-tip">
                    <p v-for="item in accessTokenTips" :key="item">{{ item }}</p>
                  </div>
                </template>
                <IconifyIcon class="access-token-warning-icon" icon="lucide:circle-alert" />
              </TTooltip>
            </span>
          </template>
          <TInput
            v-model="form.accessToken"
            type="password"
            :placeholder="editingId ? '留空则保持原访问令牌' : '密码或Personal Access Token'"
          />
        </TFormItem>
        <TFormItem label="提交作者">
          <TInput v-model="form.commitAuthorName" placeholder="AgentSprint Bot" />
        </TFormItem>
        <TFormItem label="提交邮箱">
          <TInput v-model="form.commitAuthorEmail" placeholder="agentsprint-bot@example.com" />
        </TFormItem>
        <TFormItem label="状态">
          <TSelect v-model="form.status" :options="statusOptions" />
        </TFormItem>
        <TFormItem label="说明">
          <TTextarea v-model="form.description" :autosize="{ minRows: 3, maxRows: 5 }" />
        </TFormItem>
      </TForm>
    </TDrawer>
  </div>
</template>

<style scoped>
.filter-field {
  display: grid;
  grid-template-columns: auto minmax(180px, 260px);
  gap: 8px;
  align-items: center;
  color: var(--td-text-color-secondary);
}

.field-label-with-tip {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  white-space: nowrap;
}

.access-token-warning-icon {
  display: inline-flex;
  flex: 0 0 auto;
  color: var(--td-warning-color);
  cursor: help;
  font-size: 16px;
}

.access-token-tip {
  max-width: 420px;
  line-height: 1.6;
}

.access-token-tip p {
  margin: 0;
}

.access-token-tip p + p {
  margin-top: 6px;
}

@media (max-width: 760px) {
  .filter-field {
    grid-template-columns: 1fr;
    width: 100%;
  }
}
</style>
