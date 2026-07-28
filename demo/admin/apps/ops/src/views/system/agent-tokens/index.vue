<script lang="ts" setup>
import { computed, onMounted, reactive, ref } from 'vue';
import type { FormInstanceFunctions, FormRules } from 'tdesign-vue-next';

import {
  createAgentTokenApi,
  listAgentTokensApi,
  revokeAgentTokenApi,
  type SystemApi,
} from '#/api';
import { listProjectsApi, type SprintMvpApi } from '#/api/sprint/mvp';
import AdminListPage from '#/components/admin-list-page/admin-list-page.vue';
import { formatDateTime } from '#/views/_shared/date-format';
import { requiredRule, validateForm } from '#/views/_shared/form-rules';
import { confirmAndClose } from '#/views/_shared/dialog-confirm';
import RowAction from '#/views/system/_shared/row-action.vue';
import {
  DatePicker as TDatePicker,
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

const loading = ref(false);
const creating = ref(false);
const createVisible = ref(false);
const formRef = ref<FormInstanceFunctions>();
const createdToken = ref('');
const tokens = ref<SystemApi.AgentToken[]>([]);
const projects = ref<SprintMvpApi.Project[]>([]);
const form = reactive({
  expiresAt: defaultExpiresAt(),
  name: '',
  projectId: '',
});
const rules: FormRules<typeof form> = {
  expiresAt: requiredRule('请选择到期时间', 'change'),
  name: requiredRule('请输入令牌名称'),
};
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

const columns = [
  { colKey: 'name', title: '名称', width: 160 },
  { colKey: 'maskedToken', title: '令牌', width: 220 },
  { colKey: 'ownerUsername', title: '归属用户', width: 140 },
  { colKey: 'projectId', title: '归属项目', width: 180 },
  { colKey: 'expiresAt', title: '到期时间', width: 170 },
  { colKey: 'lastUsedAt', title: '最后使用', width: 170 },
  { colKey: 'status', title: '状态', width: 100 },
  { colKey: 'actions', title: '操作', width: 120 },
];
const statusOptions = [
  { label: '有效', value: '1' },
  { label: '已撤销', value: '0' },
];

const projectOptions = computed(() =>
  projects.value.map((project) => ({
    label: `${project.code} · ${project.name}`,
    value: project.id,
  })),
);
const projectNameMap = computed(() =>
  Object.fromEntries(projects.value.map((project) => [project.id, `${project.code} · ${project.name}`])),
);
const tablePagination = computed(() => ({
  current: pagination.current,
  pageSize: pagination.pageSize,
  pageSizeOptions: [30, 50, 100, 200],
  total: tokens.value.length,
}));

function defaultExpiresAt() {
  const target = new Date();
  target.setMonth(target.getMonth() + 3);
  return toDateValue(target);
}

function toDateValue(value: Date) {
  const pad = (item: number) => String(item).padStart(2, '0');
  return `${value.getFullYear()}-${pad(value.getMonth() + 1)}-${pad(value.getDate())}`;
}

function resolveProjectName(projectId?: string) {
  return projectId ? projectNameMap.value[projectId] || projectId : '-';
}

function resolveExpiration(dateValue?: string) {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(dateValue || '');
  if (!match) {
    return new Date(Number.NaN);
  }

  return new Date(Number(match[1]), Number(match[2]) - 1, Number(match[3]), 23, 59, 59, 999);
}

function isExpired(row?: Partial<SystemApi.AgentToken>) {
  if (!row?.expiresAt) {
    return false;
  }
  const expiresAt = new Date(row.expiresAt).getTime();
  return Number.isFinite(expiresAt) && expiresAt <= Date.now();
}

function normalizeToken(token: SystemApi.AgentToken): SystemApi.AgentToken {
  return {
    ...token,
    maskedToken: token.maskedToken || '-',
    name: token.name || '-',
    ownerUsername: token.ownerUsername || '-',
  };
}

function statusTheme(row?: Partial<SystemApi.AgentToken>) {
  if (!row) {
    return 'default';
  }
  if (row.status !== 1 || row.revokedAt) {
    return 'danger';
  }
  if (isExpired(row)) {
    return 'warning';
  }
  return 'success';
}

function statusText(row?: Partial<SystemApi.AgentToken>) {
  if (!row) {
    return '-';
  }
  if (row.status !== 1 || row.revokedAt) {
    return '已撤销';
  }
  if (isExpired(row)) {
    return '已过期';
  }
  return '有效';
}

function openCreate() {
  Object.assign(form, {
    expiresAt: defaultExpiresAt(),
    name: '',
    projectId: '',
  });
  createdToken.value = '';
  createVisible.value = true;
}

function handlePageChange(pageInfo: { current: number; pageSize: number }) {
  pagination.current = pageInfo.current;
  pagination.pageSize = pageInfo.pageSize;
}

async function search() {
  Object.assign(query, filters);
  pagination.current = 1;
  await load();
}

async function reset() {
  Object.assign(filters, { keyword: '', status: '' });
  await search();
}

async function load() {
  loading.value = true;
  try {
    const [tokenResult, projectRows] = await Promise.all([listAgentTokensApi(query), listProjectsApi()]);
    tokens.value = (tokenResult || []).filter(Boolean).map(normalizeToken);
    projects.value = projectRows;
  } finally {
    loading.value = false;
  }
}

async function createToken() {
  if (!(await validateForm(formRef.value))) return;
  const name = form.name.trim();
  const expiresAt = resolveExpiration(form.expiresAt);
  if (!name) {
    MessagePlugin.warning('请填写令牌名称');
    return;
  }
  if (Number.isNaN(expiresAt.getTime()) || expiresAt.getTime() <= Date.now()) {
    MessagePlugin.warning('请选择晚于当前时间的到期时间');
    return;
  }

  creating.value = true;
  try {
    const result = await createAgentTokenApi({
      expiresAt: expiresAt.toISOString(),
      name,
      projectId: form.projectId.trim() || undefined,
    });
    createdToken.value = result.token;
    MessagePlugin.success('令牌已创建');
    await load();
  } catch (error: any) {
    MessagePlugin.error(error?.message || '令牌创建失败');
  } finally {
    creating.value = false;
  }
}

function revoke(row: SystemApi.AgentToken) {
  confirmAndClose({
    body: `确认撤销令牌 ${row.name}？撤销后已配置的 MCP 客户端将无法继续使用。`,
    confirmBtn: '撤销',
    header: '撤销令牌',
    onConfirm: async () => {
      await revokeAgentTokenApi(row.id);
      MessagePlugin.success('令牌已撤销');
      await load();
    },
  });
}

onMounted(load);
</script>

<template>
  <div>
    <AdminListPage
      title="令牌管理"
      description="维护 AgentSprint MCP 接入令牌，按名称、用户、项目与状态检索令牌记录。"
      table-title="令牌列表"
      add-button-text="新增令牌"
      :columns="columns"
      :data="tokens"
      :loading="loading"
      :pagination="tablePagination"
      :refreshable="false"
      @add="openCreate"
      @page-change="handlePageChange"
      @reset="reset"
      @search="search"
    >
      <template #filters>
        <label class="filter-field">
          <span>令牌信息</span>
          <TInput v-model="filters.keyword" clearable placeholder="名称 / 令牌 / 用户 / 项目" />
        </label>
        <label class="filter-field">
          <span>状态</span>
          <TSelect v-model="filters.status" clearable placeholder="全部状态" :options="statusOptions" />
        </label>
      </template>
      <template #expiresAt="{ row }">
        {{ formatDateTime(row?.expiresAt) }}
      </template>
      <template #lastUsedAt="{ row }">
        {{ formatDateTime(row?.lastUsedAt) }}
      </template>
      <template #status="{ row }">
        <TTag :theme="statusTheme(row)" variant="light">{{ statusText(row) }}</TTag>
      </template>
      <template #projectId="{ row }">
        {{ resolveProjectName(row?.projectId) }}
      </template>
      <template #actions="{ row }">
        <TSpace>
          <RowAction
            v-if="row?.status === 1 && !row?.revokedAt"
            icon="lucide:ban"
            label="撤销"
            theme="danger"
            @click="revoke(row)"
          />
        </TSpace>
      </template>
    </AdminListPage>

    <TDrawer
      v-model:visible="createVisible"
      :size="'42%'"
      header="新增 Agent 令牌"
      :confirm-btn="{ content: '创建令牌', loading: creating }"
      @confirm="createToken"
    >
      <section class="drawer-content">
        <TForm ref="formRef" :data="form" :rules="rules" label-width="96px">
          <TFormItem label="名称" name="name"><TInput v-model="form.name" placeholder="例如 本机 Codex" /></TFormItem>
          <TFormItem label="到期时间" name="expiresAt">
            <TDatePicker v-model="form.expiresAt" clearable format="YYYY-MM-DD" value-type="YYYY-MM-DD" />
          </TFormItem>
          <TFormItem label="归属项目">
            <TSelect v-model="form.projectId" clearable filterable :options="projectOptions" placeholder="可不选" />
          </TFormItem>
        </TForm>

        <div v-if="createdToken" class="token-result">
          <div class="token-result__head">
            <strong>完整令牌仅展示一次</strong>
            <span>复制后配置到 Codex MCP 的 Authorization Bearer。</span>
          </div>
          <TTextarea :model-value="createdToken" readonly autosize />
        </div>
      </section>
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

.drawer-content {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.token-result {
  padding: 14px;
  background: var(--td-bg-color-container-hover);
  border: 1px solid var(--td-component-border);
  border-radius: 6px;
}

.token-result__head {
  display: flex;
  flex-direction: column;
  gap: 4px;
  margin-bottom: 10px;
  color: var(--td-text-color-primary);
}

.token-result__head span {
  color: var(--td-text-color-secondary);
}

@media (max-width: 760px) {
  .filter-field {
    grid-template-columns: 1fr;
    width: 100%;
  }
}
</style>
