<script lang="ts" setup>
import type { SprintMvpApi, SprintUserApi } from '#/api/sprint/mvp';

import { IconifyIcon } from '@vben/icons';
import { computed, onActivated, onMounted, reactive, ref } from 'vue';

import {
  Button as TButton,
  Descriptions as TDescriptions,
  DescriptionsItem as TDescriptionsItem,
  Divider as TDivider,
  Drawer as TDrawer,
  Input as TInput,
  Link as TLink,
  MessagePlugin,
  Select as TSelect,
  Space as TSpace,
  Table as TTable,
  Tabs as TTabs,
  TabPanel as TTabPanel,
  Tag as TTag,
  Textarea as TTextarea,
} from 'tdesign-vue-next';

import {
  approveRequirementReviewApi,
  listProjectsApi,
  listMyPendingReviewsApi,
  listUserOptionsApi,
  rejectRequirementReviewApi,
} from '#/api/sprint/mvp';
import { formatDateTime } from '#/views/_shared/date-format';
import { confirmAndClose } from '#/views/_shared/dialog-confirm';
import { withSerialColumn } from '#/views/_shared/table-columns';

import ProjectContextListShell from '#/components/project-context-list-shell/project-context-list-shell.vue';
import MarkdownEditor from '../_shared/markdown-editor.vue';
import '../_shared/table-layout.css';

defineOptions({ name: 'SprintRequirementReviews' });

const approving = ref(false);
const loading = ref(false);
const rejecting = ref(false);
const reviewVisible = ref(false);
const current = ref<SprintMvpApi.RequirementReviewItem>();
const items = ref<SprintMvpApi.RequirementReviewItem[]>([]);
const projects = ref<SprintMvpApi.Project[]>([]);
const users = ref<SprintUserApi.UserOption[]>([]);
const userMap = computed(() =>
  Object.fromEntries(users.value.map((item) => [item.id, item])),
);
const userNameMap = computed(() =>
  Object.fromEntries(users.value.map((item) => [item.username, item])),
);
function resolveUserName(userId?: string) {
  if (!userId) return '未指定';
  const user = userMap.value[userId] || userNameMap.value[userId];
  return user?.displayName || user?.username || userId;
}
const filters = reactive({
  keyword: '',
  projectId: '',
  status: '',
});
const reviewForm = reactive({
  comment: '',
});
const currentSubmitReason = computed(
  () =>
    current.value?.reviews.find((review) => !!review.submitReason)?.submitReason ||
    '暂无提交缘由',
);
const activeReviewTab = ref<string>('content');
const pagination = reactive({
  current: 1,
  pageSize: 30,
});
const reviewStatusText: Record<string, string> = {
  approved: '已通过',
  pending: '待我评审',
  rejected: '已驳回',
};
const reviewStatusTheme: Record<string, 'danger' | 'primary' | 'success' | 'warning'> = {
  approved: 'success',
  pending: 'warning',
  rejected: 'danger',
};
const columns = [
  { colKey: 'requirement.title', title: '需求名' },
  { colKey: 'project.name', title: '项目名', width: 180 },
  { colKey: 'requirement.createdBy', title: '产品经理', width: 140 },
  { colKey: 'requirement.stakeholders', title: '干系人', width: 180 },
  { colKey: 'requirement.submittedAt', title: '提交时间', width: 180 },
  { colKey: 'currentStatus', title: '当前评审状态', width: 130 },
  { colKey: 'actions', title: '操作', width: 88 },
];
const statusOptions = [
  { label: '待我评审', value: 'pending' },
  { label: '已通过', value: 'approved' },
  { label: '已驳回', value: 'rejected' },
];
const tablePagination = computed(() => ({
  current: pagination.current,
  pageSize: pagination.pageSize,
  pageSizeOptions: [30, 50, 100, 200],
  showJumper: true,
  showPageSize: true,
  size: 'small' as const,
  total: items.value.length,
}));

function handlePageChange(pageInfo: { current: number; pageSize: number }) {
  pagination.current = pageInfo.current;
  pagination.pageSize = pageInfo.pageSize;
}

async function handleFilterChange() {
  pagination.current = 1;
  await loadReviews();
}

async function handleProjectChange() {
  pagination.current = 1;
  await loadReviews();
}

async function queryReviews() {
  pagination.current = 1;
  await loadReviews();
}

async function resetFilters() {
  Object.assign(filters, {
    keyword: '',
    projectId: projects.value[0]?.id || '',
    status: '',
  });
  pagination.current = 1;
  await loadReviews();
}

async function loadProjects() {
  projects.value = await listProjectsApi();
  if (filters.projectId && projects.value.some((project) => project.id === filters.projectId)) return;
  filters.projectId = projects.value[0]?.id || '';
}

async function loadReviews(options: { refreshProjects?: boolean } = {}) {
  loading.value = true;
  try {
    if (users.value.length === 0) {
      try {
        users.value = await listUserOptionsApi();
      } catch {
        users.value = [];
      }
    }
    if (options.refreshProjects || projects.value.length === 0) {
      await loadProjects();
    }
    items.value = await listMyPendingReviewsApi({
      keyword: filters.keyword || undefined,
      projectId: filters.projectId || undefined,
      status: filters.status || undefined,
    });
    pagination.current = 1;
  } finally {
    loading.value = false;
  }
}

async function refreshReviews() {
  await loadReviews({ refreshProjects: true });
}

function openReview(item: SprintMvpApi.RequirementReviewItem) {
  current.value = item;
  reviewForm.comment = '';
  reviewVisible.value = true;
}

function resolveCurrentReviewStatus(item: SprintMvpApi.RequirementReviewItem) {
  return item.reviews.find((review: SprintMvpApi.RequirementReview) => review.status === 'pending')
    ?.status || 'pending';
}

async function approve() {
  if (approving.value) return;
  if (!current.value) return;
  approving.value = true;
  try {
    await approveRequirementReviewApi(current.value.requirement.id, {
      comment: reviewForm.comment,
    });
    MessagePlugin.success('评审已通过');
    reviewVisible.value = false;
    await loadReviews();
  } finally {
    approving.value = false;
  }
}

function confirmApprove() {
  if (approving.value) return;
  if (!current.value) return;
  confirmAndClose({
    body: `是否确认通过「${current.value.requirement.title}」？`,
    confirmBtn: '通过',
    header: '确认通过需求',
    onConfirm: approve,
  });
}

async function reject() {
  if (rejecting.value) return;
  if (!current.value) return;
  rejecting.value = true;
  try {
    await rejectRequirementReviewApi(current.value.requirement.id, {
      comment: reviewForm.comment,
    });
    MessagePlugin.success('评审已驳回');
    reviewVisible.value = false;
    await loadReviews();
  } finally {
    rejecting.value = false;
  }
}

function confirmReject() {
  if (rejecting.value) return;
  if (!current.value) return;
  confirmAndClose({
    body: `是否确认驳回「${current.value.requirement.title}」？`,
    confirmBtn: '驳回',
    header: '确认驳回需求',
    onConfirm: reject,
    theme: 'warning',
  });
}

onMounted(refreshReviews);
onActivated(loadReviews);
</script>

<template>
  <ProjectContextListShell
    v-model:selected-project-id="filters.projectId"
    class="reviews-page"
    @project-change="handleProjectChange"
  >
    <template #title>
      需求评审
    </template>
    <template #description>
      按项目查看待评审需求，完成评审意见与审批流转。
    </template>

    <section class="sprint-filter-panel">
      <div class="sprint-filter-grid">
        <div class="sprint-filter-field">
          <span>状态</span>
          <TSelect
            v-model="filters.status"
            clearable
            :options="statusOptions"
            placeholder="全部状态"
            @change="handleFilterChange"
          />
        </div>
        <div class="sprint-filter-field">
          <span>需求信息</span>
          <TInput
            v-model="filters.keyword"
            clearable
            placeholder="需求、项目、干系人"
            @change="handleFilterChange"
          />
        </div>
        <div class="sprint-filter-actions">
          <TButton theme="primary" :loading="loading" @click="queryReviews">
            <template #icon>
              <IconifyIcon icon="lucide:search" />
            </template>
            查询
          </TButton>
          <TButton theme="default" :disabled="loading" @click="resetFilters">
            <template #icon>
              <IconifyIcon icon="lucide:rotate-ccw" />
            </template>
            重置
          </TButton>
        </div>
      </div>
    </section>

    <section class="sprint-table-panel">
      <div class="sprint-table-header">
        <h3>评审列表</h3>
        <div class="sprint-table-actions">
          <TButton shape="circle" variant="outline" title="刷新" :loading="loading" @click="loadReviews()">
            <IconifyIcon icon="lucide:refresh-cw" />
          </TButton>
        </div>
      </div>

      <TTable
        row-key="requirement.id"
        class="sprint-compact-table"
        :columns="withSerialColumn(columns, { offset: () => (pagination.current - 1) * pagination.pageSize })"
        :data="items"
        :loading="loading"
        :pagination="tablePagination"
        size="small"
        hover
        stripe
        @page-change="handlePageChange"
      >
        <template #requirement.title="{ row }">
          {{ row.requirement.title }}
        </template>
        <template #project.name="{ row }">
          {{ row.project.name }}
        </template>
        <template #requirement.createdBy="{ row }">
          {{ resolveUserName(row.requirement.createdBy) }}
        </template>
        <template #requirement.stakeholders="{ row }">
          {{ row.requirement.stakeholders || '未填写' }}
        </template>
        <template #requirement.submittedAt="{ row }">
          {{ formatDateTime(row.requirement.submittedAt) }}
        </template>
        <template #currentStatus="{ row }">
          <TTag variant="light">
            {{ reviewStatusText[resolveCurrentReviewStatus(row)] }}
          </TTag>
        </template>
        <template #actions="{ row }">
          <TSpace class="sprint-row-actions">
            <TLink theme="primary" @click="openReview(row)">
              <IconifyIcon icon="lucide:clipboard-check" />
              评审
            </TLink>
          </TSpace>
        </template>
      </TTable>
    </section>

    <TDrawer
      v-model:visible="reviewVisible"
      :footer="false"
      :size="'60%'"
      drawer-class-name="review-drawer"
    >
      <template #header>
        <div class="review-drawer-header">
          <div class="review-drawer-header__title">
            <span>需求评审</span>
            <span v-if="current" class="review-drawer-header__requirement">
              需求名称：{{ current.requirement.title }}
            </span>
          </div>
          <span v-if="current" class="review-drawer-header__time">
            提交时间：{{ formatDateTime(current.requirement.submittedAt) }}
          </span>
        </div>
      </template>
      <section v-if="current" class="review-preview compact">
        <div class="review-preview__body review-content-area">
          <section class="review-project-card">
            <div class="project-card-head">
              <strong>{{ current.project.name }}</strong>
              <TTag size="small" theme="success" variant="light">active</TTag>
            </div>
            <span class="project-card-code">{{ current.project.code }}</span>
            <span class="project-card-desc">
              {{ current.project.description || '暂无项目说明' }}
            </span>
            <div class="project-card-meta">
              <span v-if="current.project.frontendTechStack">
                前端：{{ current.project.frontendTechStack }}
              </span>
              <span v-if="current.project.backendTechStack">
                后端：{{ current.project.backendTechStack }}
              </span>
            </div>
          </section>
          <TTabs v-model="activeReviewTab" class="review-tabs" :destroy-on-hide="false">
            <TTabPanel value="content" label="评审内容">
              <div class="review-tab-panel">
          <TDescriptions class="review-descriptions" bordered :column="2">
            <TDescriptionsItem label="产品经理">
              {{ resolveUserName(current.requirement.createdBy) }}
            </TDescriptionsItem>
            <TDescriptionsItem label="干系人">
              {{ current.requirement.stakeholders || '未填写' }}
            </TDescriptionsItem>
            <TDescriptionsItem label="提交缘由" :span="2">
              {{ currentSubmitReason }}
            </TDescriptionsItem>
          </TDescriptions>
          <div class="review-markdown-area">
            <h4>需求内容</h4>
            <MarkdownEditor
              class="review-markdown-preview"
              :model-value="current.requirement.description || '暂无需求内容'"
              height="100%"
              preview
              preview-only
              read-only
              placeholder="暂无需求内容"
            />
          </div>
              </div>
            </TTabPanel>
            <TTabPanel value="progress" label="评审进度">
              <div class="review-tab-panel">
                <h4 class="review-tab-panel__title">评审进度</h4>
                <div class="review-list">
                  <div v-for="review in current.reviews" :key="review.id" class="review-list__item">
                    <TTag :theme="reviewStatusTheme[review.status]" variant="light">
                      {{ reviewStatusText[review.status] || review.status }}
                    </TTag>
                    <strong>{{ review.reviewerId }}</strong>
                    <span>{{ formatDateTime(review.reviewedAt || review.createTime) }}</span>
                    <p>{{ review.comment || '暂无意见' }}</p>
                  </div>
                </div>
              </div>
            </TTabPanel>
          </TTabs>
        </div>
        <TDivider class="review-divider" />
        <section class="review-audit-area">
          <TTabs class="review-audit-tabs" :default-value="'comment'">
            <TTabPanel value="comment" label="评审意见">
              <div class="review-audit-card">
                <TTextarea
                  v-model="reviewForm.comment"
                  class="review-audit-card__input"
                  placeholder="填写评审意见"
                  :autosize="{ minRows: 2, maxRows: 6 }"
                />
                <div class="review-audit-card__actions">
                  <TButton
                    aria-label="驳回"
                    shape="circle"
                    theme="danger"
                    title="驳回"
                    variant="outline"
                    :loading="rejecting"
                    @click="confirmReject"
                  >
                    <template #icon>
                      <IconifyIcon icon="lucide:x" />
                    </template>
                  </TButton>
                  <TButton
                    aria-label="通过"
                    shape="circle"
                    theme="primary"
                    title="通过"
                    :loading="approving"
                    @click="confirmApprove"
                  >
                    <template #icon>
                      <IconifyIcon icon="lucide:check" />
                    </template>
                  </TButton>
                </div>
              </div>
            </TTabPanel>
          </TTabs>
        </section>
      </section>
    </TDrawer>
  </ProjectContextListShell>
</template>

<style scoped>
.review-preview {
  display: flex;
  height: calc(100vh - 96px);
  min-height: 0;
  flex-direction: column;
}

.dialog-actions {
  display: flex;
  gap: 10px;
}

.review-drawer-header {
  display: flex;
  width: 100%;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  padding-right: 20px;
}

.review-drawer-header__title {
  display: flex;
  min-width: 0;
  flex-direction: column;
  gap: 3px;
}

.review-drawer-header__requirement {
  max-width: 520px;
  overflow: hidden;
  color: var(--td-text-color-secondary);
  font-size: 13px;
  font-weight: 400;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.review-drawer-header__time {
  color: var(--td-text-color-secondary);
  font-size: 13px;
  font-weight: 400;
  white-space: nowrap;
}

.review-preview__header {
  display: grid;
  gap: 10px;
}

.review-preview__body {
  display: flex;
  height: 100%;
  min-height: 0;
  flex-direction: column;
  overflow: auto;
  padding-right: 4px;
}

.review-preview h3,
.review-preview h4 {
  margin: 0;
}

.review-preview dl {
  display: grid;
  grid-template-columns: 90px minmax(0, 1fr);
  gap: 10px;
  margin: 16px 0 0;
}

.review-preview dt {
  color: var(--td-text-color-secondary);
}

.review-preview dd {
  margin: 0;
}

.review-field-label {
  color: var(--td-text-color-secondary);
  font-size: 13px;
  white-space: nowrap;
}

.review-project-card {
  display: flex;
  width: 100%;
  flex-shrink: 0;
  flex-direction: column;
  gap: 8px;
  margin-bottom: 12px;
  padding: 12px;
  color: var(--td-text-color-primary);
  text-align: left;
  background: var(--td-bg-color-container);
  border: 1px solid var(--td-component-border);
  border-radius: 6px;
}

.review-project-card .project-card-head,
.review-project-card .project-card-meta {
  display: flex;
  gap: 8px;
  align-items: center;
  justify-content: space-between;
}

.review-project-card .project-card-head strong {
  min-width: 0;
  overflow: hidden;
  font-size: 14px;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.review-project-card .project-card-code,
.review-project-card .project-card-desc,
.review-project-card .project-card-meta {
  color: var(--td-text-color-secondary);
  font-size: 12px;
  line-height: 1.5;
}

.review-project-card .project-card-desc {
  display: -webkit-box;
  min-height: 18px;
  overflow: hidden;
  -webkit-box-orient: vertical;
  -webkit-line-clamp: 2;
}

.review-project-card .project-card-meta {
  flex-wrap: wrap;
  justify-content: flex-start;
  color: var(--td-text-color-placeholder);
}

.review-descriptions {
  flex-shrink: 0;
}

.review-markdown-area {
  display: flex;
  min-height: 280px;
  flex: 1;
  flex-direction: column;
  gap: 10px;
  margin-top: 18px;
}

.review-markdown-preview {
  min-height: 0;
  flex: 1;
}

.review-progress {
  height: 200px;
  min-height: 0;
  flex: 0 0 200px;
  padding-top: 12px;
  overflow: auto;
  border-top: 1px solid var(--td-component-border);
}

.review-list {
  display: grid;
  gap: 10px;
  margin-top: 10px;
}

.review-list__item {
  display: grid;
  grid-template-columns: auto 110px minmax(150px, 1fr);
  gap: 8px 10px;
  align-items: center;
  padding: 10px 0;
  border-top: 1px solid var(--td-component-border);
}

.review-list__item p {
  grid-column: 1 / -1;
  margin: 0;
  color: var(--td-text-color-secondary);
  white-space: pre-wrap;
}

.dialog-actions {
  justify-content: flex-end;
  margin-top: 16px;
}

.review-content-area {
  display: flex;
  min-height: 0;
  flex: 1;
  flex-direction: column;
}

.review-tabs {
  display: flex;
  min-height: 0;
  flex: 1;
  flex-direction: column;
}

.review-tabs :deep(.t-tabs__nav) {
  flex-shrink: 0;
}

.review-tabs :deep(.t-tabs__content) {
  display: flex;
  min-height: 0;
  flex: 1;
  flex-direction: column;
  padding-top: 8px;
}

.review-tabs :deep(.t-tab-panel) {
  display: flex;
  min-height: 0;
  flex: 1;
  flex-direction: column;
}

.review-tab-panel {
  display: flex;
  min-height: 0;
  flex: 1;
  flex-direction: column;
}

.review-tab-panel__title {
  margin-bottom: 8px;
  color: var(--td-text-color-secondary);
  font-size: 14px;
  font-weight: 600;
}

.review-divider {
  flex-shrink: 0;
  margin: 8px 0;
}

.review-audit-area {
  flex-shrink: 0;
  padding: 12px 0 4px;
  background: var(--td-bg-color-container, #fff);
}

.review-audit-card {
  display: flex;
  flex-direction: column;
  gap: 10px;
  padding: 12px;
  background: var(--td-bg-color-container, #fff);
  border: 1px solid var(--td-component-stroke, var(--td-component-border));
  border-radius: 6px;
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.04);
}

.review-audit-tabs :deep(.t-tabs__content) {
  padding-top: 10px;
}

.review-audit-card__input :deep(.t-textarea),
.review-audit-card__input :deep(textarea),
.review-audit-card__input :deep(.t-textarea__inner) {
  background: transparent;
  border: none;
  box-shadow: none;
}

.review-audit-card__actions {
  display: flex;
  gap: 12px;
  align-self: flex-end;
  justify-content: flex-end;
}
</style>
