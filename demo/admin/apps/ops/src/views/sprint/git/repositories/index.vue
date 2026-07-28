<script lang="ts" setup>
import type { SprintGitApi } from '#/api/sprint/git';
import type { FormInstanceFunctions, FormRules } from 'tdesign-vue-next';

import { computed, onMounted, reactive, ref } from 'vue';

import AdminListPage from '#/components/admin-list-page/admin-list-page.vue';
import {
  createGitBranchApi,
  createGitRepositoryApi,
  deleteGitBranchApi,
  listGitAccountsApi,
  listGitRepositoriesApi,
  readGitPushRecordsApi,
  updateGitRepositoryApi,
} from '#/api/sprint/git';
import { formatDateTime } from '#/views/_shared/date-format';
import { requiredHttpUrlRule, requiredRule, validateForm } from '#/views/_shared/form-rules';
import RowAction from '#/views/system/_shared/row-action.vue';
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
  Table as TTable,
  Tag as TTag,
  Textarea as TTextarea,
} from 'tdesign-vue-next';

defineOptions({ name: 'SprintGitRepositories' });

const loading = ref(false);
const saving = ref(false);
const branchSaving = ref(false);
const recordsLoading = ref(false);
const drawerVisible = ref(false);
const branchDrawerVisible = ref(false);
const recordsVisible = ref(false);
const editingId = ref('');
const branchMode = ref<'create' | 'delete'>('create');
const selectedRepository = ref<SprintGitApi.GitRepository>();
const formRef = ref<FormInstanceFunctions>();
const branchFormRef = ref<FormInstanceFunctions>();
const repositories = ref<SprintGitApi.GitRepository[]>([]);
const accounts = ref<SprintGitApi.GitAccount[]>([]);
const records = ref<SprintGitApi.BranchOperation[]>([]);
const filters = reactive({
  gitAccountId: '',
  keyword: '',
  status: '',
});
const query = reactive({
  gitAccountId: '',
  keyword: '',
  status: '',
});
const pagination = reactive({
  current: 1,
  pageSize: 30,
});
const form = reactive<SprintGitApi.SaveGitRepositoryRequest>({
  defaultBranch: 'main',
  description: '',
  gitAccountId: '',
  localPath: '',
  name: '',
  repositoryUrl: '',
  status: 'active',
});
const branchForm = reactive({
  backupBranch: '',
  branchName: '',
  sourceBranch: '',
});
const recordQuery = reactive({
  branch: '',
});
const rules: FormRules<typeof form> = {
  name: requiredRule('请输入仓库名称'),
  repositoryUrl: requiredHttpUrlRule('请输入http或https仓库地址'),
};
const branchRules: FormRules<typeof branchForm> = {
  branchName: requiredRule('请输入分支名称'),
};
const statusOptions = [
  { label: '启用', value: 'active' },
  { label: '停用', value: 'disabled' },
];
const accountOptions = computed(() =>
  accounts.value
    .filter((item) => item.status === 'active')
    .map((item) => ({ label: `${item.name} (${item.username})`, value: item.id })),
);
const accountMap = computed(() => Object.fromEntries(accounts.value.map((item) => [item.id, item])));
const tablePagination = computed(() => ({
  current: pagination.current,
  pageSize: pagination.pageSize,
  pageSizeOptions: [30, 50, 100, 200],
  total: repositories.value.length,
}));
const columns = [
  { colKey: 'code', title: '仓库编码', width: 180, ellipsis: true },
  { colKey: 'name', title: '仓库名称', minWidth: 150, ellipsis: true },
  { colKey: 'repositoryUrl', title: '仓库地址', minWidth: 260, ellipsis: true },
  { colKey: 'defaultBranch', title: '默认分支', width: 120, ellipsis: true },
  { colKey: 'gitAccountId', title: 'Git账户', width: 160 },
  { colKey: 'status', title: '状态', width: 90 },
  { colKey: 'actions', title: '操作', width: 360, fixed: 'right' as const },
];
const recordColumns = [
  { colKey: 'serial-number', title: '序号', width: 70 },
  { colKey: 'branchName', title: '分支', width: 140, ellipsis: true },
  { colKey: 'commitHash', title: '提交', width: 120, ellipsis: true },
  { colKey: 'commitMessage', title: '提交说明', minWidth: 220, ellipsis: true },
  { colKey: 'pushedAt', title: '推送时间', width: 180, cell: 'pushedAt' },
  { colKey: 'status', title: '状态', width: 90, cell: 'status' },
];

function resolveAccountName(id?: string) {
  return id ? accountMap.value[id]?.name || id : '未绑定';
}

function handlePageChange(pageInfo: { current: number; pageSize: number }) {
  pagination.current = pageInfo.current;
  pagination.pageSize = pageInfo.pageSize;
}

function resetForm(row?: SprintGitApi.GitRepository) {
  editingId.value = row?.id || '';
  Object.assign(form, {
    defaultBranch: row?.defaultBranch || 'main',
    description: row?.description || '',
    gitAccountId: row?.gitAccountId || '',
    localPath: row?.localPath || '',
    name: row?.name || '',
    repositoryUrl: row?.repositoryUrl || '',
    status: row?.status || 'active',
  });
}

function openCreate() {
  resetForm();
  drawerVisible.value = true;
}

function openEdit(row: SprintGitApi.GitRepository) {
  resetForm(row);
  drawerVisible.value = true;
}

function openBranch(row: SprintGitApi.GitRepository, mode: typeof branchMode.value) {
  selectedRepository.value = row;
  branchMode.value = mode;
  Object.assign(branchForm, {
    backupBranch: '',
    branchName: '',
    sourceBranch: row.defaultBranch || 'main',
  });
  branchDrawerVisible.value = true;
}

async function openRecords(row: SprintGitApi.GitRepository) {
  selectedRepository.value = row;
  records.value = [];
  recordQuery.branch = row.defaultBranch || '';
  recordsVisible.value = true;
  await loadRecords();
}

async function search() {
  Object.assign(query, filters);
  pagination.current = 1;
  await loadRepositories();
}

async function reset() {
  Object.assign(filters, { gitAccountId: '', keyword: '', status: '' });
  await search();
}

async function loadRepositories() {
  loading.value = true;
  try {
    [repositories.value, accounts.value] = await Promise.all([
      listGitRepositoriesApi({
        gitAccountId: query.gitAccountId || undefined,
        keyword: query.keyword || undefined,
        status: query.status || undefined,
      }),
      listGitAccountsApi(),
    ]);
  } finally {
    loading.value = false;
  }
}

async function saveRepository() {
  if (saving.value) return;
  if (!(await validateForm(formRef.value))) return;

  saving.value = true;
  try {
    const payload: SprintGitApi.SaveGitRepositoryRequest = {
      defaultBranch: form.defaultBranch?.trim() || undefined,
      description: form.description?.trim() || undefined,
      gitAccountId: form.gitAccountId || undefined,
      localPath: form.localPath?.trim() || undefined,
      name: form.name.trim(),
      repositoryUrl: form.repositoryUrl.trim(),
      status: form.status,
    };
    if (editingId.value) {
      await updateGitRepositoryApi(editingId.value, payload);
    } else {
      await createGitRepositoryApi(payload);
    }
    MessagePlugin.success('Git仓库已保存');
    drawerVisible.value = false;
    await loadRepositories();
  } finally {
    saving.value = false;
  }
}

async function submitBranchOperation() {
  if (!selectedRepository.value || branchSaving.value) return;
  if (!(await validateForm(branchFormRef.value))) return;
  branchSaving.value = true;
  try {
    const payload = {
      backupBranch: branchForm.backupBranch.trim() || undefined,
      branchName: branchForm.branchName.trim(),
      sourceBranch: branchForm.sourceBranch.trim() || undefined,
    };
    const result =
      branchMode.value === 'create'
        ? await createGitBranchApi(selectedRepository.value.id, {
            branchName: payload.branchName,
            sourceBranch: payload.sourceBranch,
          })
        : await deleteGitBranchApi(selectedRepository.value.id, {
            backupBranch: payload.backupBranch,
            branchName: payload.branchName,
          });
    if (result.status === 'success') {
      MessagePlugin.success(branchMode.value === 'create' ? '分支已新增' : '分支已备份并删除');
      branchDrawerVisible.value = false;
    } else {
      MessagePlugin.error(result.message || 'Git分支操作失败');
    }
  } finally {
    branchSaving.value = false;
  }
}

async function loadRecords() {
  if (!selectedRepository.value || recordsLoading.value) return;
  recordsLoading.value = true;
  try {
    records.value = await readGitPushRecordsApi(
      selectedRepository.value.id,
      recordQuery.branch.trim() || undefined,
    );
  } finally {
    recordsLoading.value = false;
  }
}

onMounted(loadRepositories);
</script>

<template>
  <div>
    <AdminListPage
      title="Git仓库管理"
      description="维护可选仓库数据源，并执行新增分支、备份删除分支和读取推送记录。仓库编码由后台自动生成。"
      table-title="Git仓库列表"
      add-button-text="新增仓库"
      :columns="columns"
      :data="repositories"
      :loading="loading"
      :pagination="tablePagination"
      @add="openCreate"
      @page-change="handlePageChange"
      @refresh="loadRepositories"
      @reset="reset"
      @search="search"
    >
      <template #filters>
        <label class="filter-field">
          <span>仓库信息</span>
          <TInput v-model="filters.keyword" clearable placeholder="编码 / 名称 / 地址" />
        </label>
        <label class="filter-field">
          <span>Git账户</span>
          <TSelect v-model="filters.gitAccountId" clearable filterable placeholder="全部账户" :options="accountOptions" />
        </label>
        <label class="filter-field">
          <span>状态</span>
          <TSelect v-model="filters.status" clearable placeholder="全部状态" :options="statusOptions" />
        </label>
      </template>
      <template #gitAccountId="{ row }">
        {{ resolveAccountName(row?.gitAccountId) }}
      </template>
      <template #status="{ row }">
        <TTag :theme="row?.status === 'active' ? 'success' : 'default'" variant="light">
          {{ row?.status === 'active' ? '启用' : '停用' }}
        </TTag>
      </template>
      <template #actions="{ row }">
        <TSpace break-line>
          <RowAction icon="lucide:pencil" label="编辑" theme="primary" @click="openEdit(row)" />
          <RowAction icon="lucide:git-branch-plus" label="新增分支" theme="primary" @click="openBranch(row, 'create')" />
          <RowAction icon="lucide:trash-2" label="删除分支" theme="danger" @click="openBranch(row, 'delete')" />
          <RowAction icon="lucide:list-tree" label="推送记录" theme="primary" @click="openRecords(row)" />
        </TSpace>
      </template>
    </AdminListPage>

    <TDrawer
      v-model:visible="drawerVisible"
      :confirm-btn="{ content: '保存', loading: saving }"
      :header="editingId ? '编辑Git仓库' : '新增Git仓库'"
      size="620px"
      @confirm="saveRepository"
    >
      <TForm ref="formRef" :data="form" :rules="rules" label-width="100px">
        <TFormItem label="仓库名称" name="name">
          <TInput v-model="form.name" placeholder="AgentSprint主仓库" />
        </TFormItem>
        <TFormItem label="仓库地址" name="repositoryUrl">
          <TInput v-model="form.repositoryUrl" placeholder="https://github.com/org/repo.git" />
        </TFormItem>
        <TFormItem label="默认分支">
          <TInput v-model="form.defaultBranch" placeholder="main" />
        </TFormItem>
        <TFormItem label="Git账户">
          <TSelect v-model="form.gitAccountId" clearable filterable :options="accountOptions" />
        </TFormItem>
        <TFormItem label="本地路径">
          <TInput v-model="form.localPath" placeholder="可选，本地工作副本路径" />
        </TFormItem>
        <TFormItem label="状态">
          <TSelect v-model="form.status" :options="statusOptions" />
        </TFormItem>
        <TFormItem label="说明">
          <TTextarea v-model="form.description" :autosize="{ minRows: 3, maxRows: 5 }" />
        </TFormItem>
      </TForm>
    </TDrawer>

    <TDrawer
      v-model:visible="branchDrawerVisible"
      :confirm-btn="{ content: branchMode === 'create' ? '新增分支' : '备份并删除', loading: branchSaving }"
      :header="branchMode === 'create' ? '新增分支' : '删除分支'"
      size="520px"
      @confirm="submitBranchOperation"
    >
      <TForm ref="branchFormRef" :data="branchForm" :rules="branchRules" label-width="110px">
        <TFormItem label="目标仓库">
          <TInput :value="selectedRepository?.name" disabled />
        </TFormItem>
        <TFormItem label="分支名称" name="branchName">
          <TInput v-model="branchForm.branchName" placeholder="feature/demo" />
        </TFormItem>
        <TFormItem v-if="branchMode === 'create'" label="来源分支">
          <TInput v-model="branchForm.sourceBranch" placeholder="main" />
        </TFormItem>
        <TFormItem v-else label="备份分支">
          <TInput v-model="branchForm.backupBranch" placeholder="留空时自动生成 backup/分支-时间" />
        </TFormItem>
      </TForm>
    </TDrawer>

    <TDialog v-model:visible="recordsVisible" width="860px" header="分支推送记录" :footer="false">
      <div class="records-query">
        <TInput v-model="recordQuery.branch" clearable placeholder="分支名称，留空读取全部远端分支" />
        <TButton theme="primary" :loading="recordsLoading" @click="loadRecords">读取</TButton>
      </div>
      <TTable
        row-key="id"
        :columns="recordColumns"
        :data="records"
        :loading="recordsLoading"
        hover
      >
        <template #pushedAt="{ row }">
          {{ row.pushedAt ? formatDateTime(row.pushedAt) : '-' }}
        </template>
        <template #status="{ row }">
          <TTag :theme="row.status === 'success' ? 'success' : 'danger'" variant="light">
            {{ row.status === 'success' ? '成功' : '失败' }}
          </TTag>
        </template>
      </TTable>
    </TDialog>
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

.records-query {
  display: flex;
  gap: 12px;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 12px;
}

@media (max-width: 760px) {
  .filter-field {
    grid-template-columns: 1fr;
    width: 100%;
  }

  .records-query {
    align-items: stretch;
    flex-direction: column;
  }
}
</style>
