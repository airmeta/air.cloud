<script lang="ts" setup>
import type { SprintMvpApi, SprintUserApi } from '#/api/sprint/mvp';
import type { FormInstanceFunctions, FormRules } from 'tdesign-vue-next';

import { computed, onMounted, reactive, ref, watch } from 'vue';

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
} from 'tdesign-vue-next';

import AdminListPage from '#/components/admin-list-page/admin-list-page.vue';
import {
  confirmProposalApi,
  createProposalApi,
  getProposalApi,
  listProjectMaterialsApi,
  listProposalsApi,
  listUserOptionsApi,
  updateProposalApi,
  voidProposalApi,
} from '#/api/sprint/mvp';
import { useProjectContextStore } from '#/store/project-context';
import { formatDateTime } from '#/views/_shared/date-format';
import { confirmAndClose } from '#/views/_shared/dialog-confirm';
import { requiredRule, validateForm } from '#/views/_shared/form-rules';
import RowAction from '#/views/system/_shared/row-action.vue';

defineOptions({ name: 'SprintProposals' });

type ProposalStatus = 'confirmed' | 'converted' | 'draft' | 'generated' | 'generating' | 'voided' | string;

const projectContext = useProjectContextStore();
const loading = ref(false);
const saving = ref(false);
const actionLoading = ref(false);
const editorVisible = ref(false);
const detailVisible = ref(false);
const formRef = ref<FormInstanceFunctions>();
const rows = ref<SprintMvpApi.Proposal[]>([]);
const selected = ref<SprintMvpApi.Proposal>();
const users = ref<SprintUserApi.UserOption[]>([]);
const materialOptions = ref<Array<{ label: string; value: string }>>([]);

const filters = reactive({
  createdBy: '',
  keyword: '',
  projectId: '',
  status: '',
});
const query = reactive({ ...filters });
const pagination = reactive({
  current: 1,
  pageSize: 30,
});
const form = reactive({
  content: '',
  instruction: '',
  materialIds: [] as string[],
  summary: '',
  title: '',
});

const rules: FormRules<typeof form> = {
  title: requiredRule('请输入提案标题'),
};

const columns = [
  { colKey: 'title', ellipsis: true, title: '提案标题', width: 260 },
  { colKey: 'status', title: '状态', width: 110 },
  { colKey: 'sourceType', title: '来源', width: 130 },
  { colKey: 'materials', title: '材料', width: 90 },
  { colKey: 'createdBy', title: '创建人', width: 140 },
  { colKey: 'updateTime', title: '更新时间', width: 170 },
  { colKey: 'actions', fixed: 'right', title: '操作', width: 260 },
];

const statusOptions = [
  { label: '草稿', value: 'draft' },
  { label: '生成中', value: 'generating' },
  { label: '已生成', value: 'generated' },
  { label: '已确认', value: 'confirmed' },
  { label: '已转需求', value: 'converted' },
  { label: '已作废', value: 'voided' },
];

const sourceTypeOptions = [
  { label: '手工录入', value: 'manual' },
  { label: '项目材料', value: 'project_materials' },
  { label: 'AI 对话', value: 'ai_chat' },
];

const projectOptions = computed(() =>
  projectContext.projects.map((project) => ({
    label: `${project.name} (${project.code})`,
    value: project.id,
  })),
);
const userOptions = computed(() =>
  users.value.map((user) => ({
    label: `${user.displayName} (${user.username})`,
    value: user.id,
  })),
);
const userMap = computed(() => Object.fromEntries(users.value.map((user) => [user.id, user])));
const tablePagination = computed(() => ({
  current: pagination.current,
  pageSize: pagination.pageSize,
  pageSizeOptions: [30, 50, 100, 200],
  total: rows.value.length,
}));
const selectedProjectName = computed(
  () => projectOptions.value.find((item) => item.value === query.projectId)?.label || '未选择项目',
);
const selectedMaterials = computed(() =>
  (selected.value?.materials || []).filter((relation) => relation.material),
);

watch(
  () => filters.projectId,
  async (projectId, previousProjectId) => {
    if (projectId && projectId !== previousProjectId) {
      await loadMaterialOptions(projectId);
    }
  },
);

function statusText(status?: ProposalStatus) {
  return statusOptions.find((item) => item.value === status)?.label || status || '-';
}

function statusTheme(status?: ProposalStatus) {
  if (status === 'confirmed') return 'success';
  if (status === 'converted') return 'primary';
  if (status === 'generating' || status === 'generated') return 'warning';
  if (status === 'voided') return 'danger';
  return 'default';
}

function sourceTypeText(sourceType?: string) {
  return sourceTypeOptions.find((item) => item.value === sourceType)?.label || sourceType || '-';
}

function resolveUserName(userId?: string) {
  if (!userId) return '-';
  const user = userMap.value[userId];
  return user ? `${user.displayName} (${user.username})` : userId;
}

function canEdit(row: SprintMvpApi.Proposal) {
  return row.status === 'draft' || row.status === 'generated';
}

function canConfirm(row: SprintMvpApi.Proposal) {
  return row.status === 'draft' || row.status === 'generated';
}

function canVoid(row: SprintMvpApi.Proposal) {
  return row.status !== 'converted' && row.status !== 'voided';
}

function resetForm() {
  selected.value = undefined;
  Object.assign(form, {
    content: '',
    instruction: '',
    materialIds: [],
    summary: '',
    title: '',
  });
}

async function loadMaterialOptions(projectId: string) {
  if (!projectId) {
    materialOptions.value = [];
    return;
  }

  const result = await listProjectMaterialsApi(projectId, {
    itemType: 'file',
    pageIndex: 1,
    pageSize: 200,
  });
  materialOptions.value = result.items.map((material) => ({
    label: material.originalFileName || material.name,
    value: material.id,
  }));
}

async function loadRows() {
  if (!query.projectId) {
    rows.value = [];
    return;
  }

  loading.value = true;
  try {
    const result = await listProposalsApi(query.projectId, {
      createdBy: query.createdBy || undefined,
      keyword: query.keyword || undefined,
      pageIndex: 1,
      pageSize: 200,
      status: query.status || undefined,
    });
    rows.value = result.items;
  } finally {
    loading.value = false;
  }
}

async function search() {
  Object.assign(query, filters);
  pagination.current = 1;
  await loadRows();
}

async function reset() {
  Object.assign(filters, {
    createdBy: '',
    keyword: '',
    projectId: projectContext.selectedProjectId || projectContext.projects[0]?.id || '',
    status: '',
  });
  await search();
}

function handlePageChange(pageInfo: { current: number; pageSize: number }) {
  pagination.current = pageInfo.current;
  pagination.pageSize = pageInfo.pageSize;
}

async function openCreate() {
  if (!filters.projectId) {
    MessagePlugin.warning('请先选择项目');
    return;
  }

  resetForm();
  await loadMaterialOptions(filters.projectId);
  editorVisible.value = true;
}

async function loadDetail(row: SprintMvpApi.Proposal) {
  const detail = await getProposalApi(row.id);
  selected.value = detail;
  return detail;
}

async function openEdit(row: SprintMvpApi.Proposal) {
  const detail = await loadDetail(row);
  if (!canEdit(detail)) {
    MessagePlugin.warning('当前提案状态不允许编辑');
    return;
  }

  await loadMaterialOptions(detail.projectId);
  Object.assign(form, {
    content: detail.content || '',
    instruction: detail.instruction || '',
    materialIds: detail.materials.map((relation) => relation.materialId),
    summary: detail.summary || '',
    title: detail.title,
  });
  editorVisible.value = true;
}

async function openDetail(row: SprintMvpApi.Proposal) {
  await loadDetail(row);
  detailVisible.value = true;
}

async function save() {
  if (saving.value) return;
  if (!(await validateForm(formRef.value))) return;
  const projectId = selected.value?.projectId || filters.projectId;
  if (!projectId) {
    MessagePlugin.warning('请先选择项目');
    return;
  }

  saving.value = true;
  try {
    const payload = {
      content: form.content.trim() || undefined,
      instruction: form.instruction.trim() || undefined,
      materialIds: form.materialIds,
      summary: form.summary.trim() || undefined,
      title: form.title.trim(),
    };

    if (selected.value) {
      await updateProposalApi(selected.value.id, payload);
      MessagePlugin.success('提案已保存');
    } else {
      await createProposalApi(projectId, payload);
      MessagePlugin.success('提案已创建');
    }

    editorVisible.value = false;
    await search();
  } finally {
    saving.value = false;
  }
}

function confirmProposal(row: SprintMvpApi.Proposal) {
  confirmAndClose({
    body: `确认提案“${row.title}”后，将进入后续转需求准备状态。`,
    confirmBtn: { content: '确认提案', theme: 'primary' },
    header: '确认提案',
    onConfirm: async () => {
      actionLoading.value = true;
      try {
        await confirmProposalApi(row.id);
        MessagePlugin.success('提案已确认');
        await loadRows();
      } finally {
        actionLoading.value = false;
      }
    },
  });
}

function voidProposal(row: SprintMvpApi.Proposal) {
  confirmAndClose({
    body: `作废提案“${row.title}”后，将不再进入后续转需求流程。`,
    confirmBtn: { content: '作废', theme: 'danger' },
    header: '作废提案',
    onConfirm: async () => {
      actionLoading.value = true;
      try {
        await voidProposalApi(row.id);
        MessagePlugin.success('提案已作废');
        await loadRows();
      } finally {
        actionLoading.value = false;
      }
    },
  });
}

onMounted(async () => {
  loading.value = true;
  try {
    await Promise.all([projectContext.loadProjects(), listUserOptionsApi().then((items) => (users.value = items))]);
    const projectId = projectContext.selectedProjectId || projectContext.projects[0]?.id || '';
    Object.assign(filters, { projectId });
    Object.assign(query, { projectId });
    await loadMaterialOptions(projectId);
    await loadRows();
  } finally {
    loading.value = false;
  }
});
</script>

<template>
  <AdminListPage
    title="提案管理"
    :description="`按项目维护提案草稿、来源材料和确认状态，当前项目：${selectedProjectName}`"
    table-title="提案列表"
    add-button-text="新增提案"
    :columns="columns"
    :data="rows"
    :loading="loading || actionLoading"
    :pagination="tablePagination"
    @add="openCreate"
    @page-change="handlePageChange"
    @refresh="loadRows"
    @reset="reset"
    @search="search"
  >
    <template #filters>
      <label class="proposal-filter-field">
        <span>项目</span>
        <TSelect
          v-model="filters.projectId"
          filterable
          placeholder="选择项目"
          :options="projectOptions"
          style="width: 280px"
        />
      </label>
      <label class="proposal-filter-field">
        <span>提案信息</span>
        <TInput v-model="filters.keyword" clearable placeholder="标题 / 摘要 / 内容" style="width: 220px" />
      </label>
      <label class="proposal-filter-field">
        <span>状态</span>
        <TSelect v-model="filters.status" clearable placeholder="全部状态" :options="statusOptions" style="width: 150px" />
      </label>
      <label class="proposal-filter-field">
        <span>创建人</span>
        <TSelect
          v-model="filters.createdBy"
          clearable
          filterable
          placeholder="全部创建人"
          :options="userOptions"
          style="width: 190px"
        />
      </label>
    </template>

    <template #status="{ row }">
      <TTag :theme="statusTheme(row.status)" variant="light">{{ statusText(row.status) }}</TTag>
    </template>
    <template #sourceType="{ row }">
      <TTag variant="light">{{ sourceTypeText(row.sourceType) }}</TTag>
    </template>
    <template #materials="{ row }">
      {{ row.materials?.length || 0 }} 个
    </template>
    <template #createdBy="{ row }">
      {{ resolveUserName(row.createdBy) }}
    </template>
    <template #updateTime="{ row }">
      {{ formatDateTime(row.updateTime || row.createTime) }}
    </template>
    <template #actions="{ row }">
      <TSpace>
        <RowAction icon="lucide:eye" label="详情" @click="openDetail(row)" />
        <RowAction v-if="canEdit(row)" icon="lucide:pencil" label="编辑" @click="openEdit(row)" />
        <RowAction v-if="canConfirm(row)" icon="lucide:check" label="确认" theme="success" @click="confirmProposal(row)" />
        <RowAction v-if="canVoid(row)" icon="lucide:ban" label="作废" theme="danger" @click="voidProposal(row)" />
      </TSpace>
    </template>
  </AdminListPage>

  <TDrawer
    v-model:visible="editorVisible"
    size="720px"
    :header="selected ? '编辑提案' : '新增提案'"
    :confirm-btn="{ content: '保存', loading: saving }"
    @confirm="save"
  >
    <TForm ref="formRef" :data="form" :rules="rules" label-align="top">
      <TFormItem label="提案标题" name="title">
        <TInput v-model="form.title" clearable placeholder="请输入提案标题" />
      </TFormItem>
      <TFormItem label="来源材料" name="materialIds">
        <TSelect
          v-model="form.materialIds"
          clearable
          filterable
          multiple
          placeholder="选择项目材料"
          :options="materialOptions"
        />
      </TFormItem>
      <TFormItem label="生成要求" name="instruction">
        <TTextarea v-model="form.instruction" :autosize="{ minRows: 3, maxRows: 6 }" placeholder="可填写后续 AI 生成提案时的补充要求" />
      </TFormItem>
      <TFormItem label="提案摘要" name="summary">
        <TTextarea v-model="form.summary" :autosize="{ minRows: 3, maxRows: 6 }" placeholder="请输入提案摘要" />
      </TFormItem>
      <TFormItem label="提案内容" name="content">
        <TTextarea v-model="form.content" :autosize="{ minRows: 8, maxRows: 16 }" placeholder="请输入提案正文" />
      </TFormItem>
    </TForm>
  </TDrawer>

  <TDrawer v-model:visible="detailVisible" size="760px" header="提案详情" :footer="false">
    <div v-if="selected" class="proposal-detail">
      <header class="proposal-detail__header">
        <h3>{{ selected.title }}</h3>
        <TSpace>
          <TTag :theme="statusTheme(selected.status)" variant="light">{{ statusText(selected.status) }}</TTag>
          <TTag variant="light">{{ sourceTypeText(selected.sourceType) }}</TTag>
        </TSpace>
      </header>

      <dl class="proposal-detail__meta">
        <div>
          <dt>创建人</dt>
          <dd>{{ resolveUserName(selected.createdBy) }}</dd>
        </div>
        <div>
          <dt>创建时间</dt>
          <dd>{{ formatDateTime(selected.createTime) }}</dd>
        </div>
        <div>
          <dt>确认时间</dt>
          <dd>{{ formatDateTime(selected.confirmedAt) }}</dd>
        </div>
        <div>
          <dt>转需求记录</dt>
          <dd>{{ selected.requirements?.length || 0 }} 条</dd>
        </div>
      </dl>

      <section class="proposal-detail__section">
        <h4>来源材料</h4>
        <div v-if="selectedMaterials.length" class="proposal-material-list">
          <TTag v-for="relation in selectedMaterials" :key="relation.id" variant="light">
            {{ relation.material?.originalFileName || relation.material?.name || relation.materialId }}
          </TTag>
        </div>
        <p v-else>未绑定项目材料。</p>
      </section>

      <section class="proposal-detail__section">
        <h4>生成要求</h4>
        <p>{{ selected.instruction || '暂无' }}</p>
      </section>
      <section class="proposal-detail__section">
        <h4>提案摘要</h4>
        <p>{{ selected.summary || '暂无' }}</p>
      </section>
      <section class="proposal-detail__section">
        <h4>提案内容</h4>
        <pre>{{ selected.content || '暂无' }}</pre>
      </section>
    </div>
  </TDrawer>
</template>

<style scoped>
.proposal-filter-field {
  display: inline-flex;
  gap: 6px;
  align-items: center;
}

.proposal-filter-field span {
  color: var(--td-text-color-secondary);
  white-space: nowrap;
}

.proposal-detail {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.proposal-detail__header {
  display: flex;
  gap: 12px;
  align-items: flex-start;
  justify-content: space-between;
}

.proposal-detail__header h3 {
  margin: 0;
  color: var(--td-text-color-primary);
  font-size: 18px;
  line-height: 26px;
}

.proposal-detail__meta {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 10px 16px;
  margin: 0;
  padding: 12px;
  background: var(--td-bg-color-secondarycontainer);
  border-radius: 6px;
}

.proposal-detail__meta div {
  min-width: 0;
}

.proposal-detail__meta dt {
  color: var(--td-text-color-placeholder);
  font-size: 12px;
}

.proposal-detail__meta dd {
  margin: 4px 0 0;
  overflow: hidden;
  color: var(--td-text-color-primary);
  text-overflow: ellipsis;
  white-space: nowrap;
}

.proposal-detail__section {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.proposal-detail__section h4 {
  margin: 0;
  color: var(--td-text-color-primary);
  font-size: 15px;
}

.proposal-detail__section p,
.proposal-detail__section pre {
  margin: 0;
  color: var(--td-text-color-secondary);
  line-height: 22px;
  white-space: pre-wrap;
  word-break: break-word;
}

.proposal-material-list {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

@media (max-width: 760px) {
  .proposal-detail__header,
  .proposal-detail__meta {
    grid-template-columns: 1fr;
  }
}
</style>
