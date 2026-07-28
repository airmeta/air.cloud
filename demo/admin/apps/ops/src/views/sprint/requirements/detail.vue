<script lang="ts" setup>
import type { SprintMvpApi, SprintTestApi, SprintUserApi } from '#/api/sprint/mvp';

import { IconifyIcon } from '@vben/icons';
import { computed, onMounted, ref } from 'vue';
import { useRoute } from 'vue-router';

import {
  Button as TButton,
  Descriptions as TDescriptions,
  DescriptionsItem as TDescriptionsItem,
  Empty as TEmpty,
  Table as TTable,
  Tag as TTag,
} from 'tdesign-vue-next';

import {
  listBugsApi,
  listDevelopmentTasksApi,
  listProjectsApi,
  listRequirementsApi,
  listTestPlansApi,
  listUserOptionsApi,
} from '#/api/sprint/mvp';
import { formatDateTime } from '#/views/_shared/date-format';
import { withSerialColumn } from '#/views/_shared/table-columns';

import { renderMarkdown } from '../_shared/markdown';
import { buildUserLookup, resolveUserName } from '../_shared/user-display';
import '../_shared/table-layout.css';

const route = useRoute();
const loading = ref(false);
const project = ref<SprintMvpApi.Project>();
const requirement = ref<SprintMvpApi.Requirement>();
const tasks = ref<SprintMvpApi.DevelopmentTask[]>([]);
const bugs = ref<SprintMvpApi.Bug[]>([]);
const testPlans = ref<SprintTestApi.TestPlan[]>([]);
const users = ref<SprintUserApi.UserOption[]>([]);

const statusText: Record<string, string> = {
  approved: '待拆解',
  completed: '已完成',
  decomposed: '待推进',
  developing: '已推进',
  draft: '草稿',
  pending_fix: '待修复',
  pending_review: '待评审',
  ready_development: '待拆解',
  ready_test: '待测试',
  rejected: '评审驳回',
  tested: '已测试',
  testing: '测试中',
  voided: '已作废',
};
const taskColumns = [
  { colKey: 'title', title: '任务' },
  { colKey: 'status', title: '状态', width: 130 },
  { colKey: 'assigneeId', title: '负责人', width: 140 },
];
const bugColumns = [
  { colKey: 'title', title: '缺陷' },
  { colKey: 'status', title: '状态', width: 130 },
  { colKey: 'createdBy', title: '提交人', width: 140 },
];
const testColumns = [
  { colKey: 'name', title: '测试计划' },
  { colKey: 'status', title: '状态', width: 130 },
  { colKey: 'createdBy', title: '负责人', width: 140 },
];

const activeBugCount = computed(() => bugs.value.filter((item) => item.status !== 'closed').length);
const userLookup = computed(() => buildUserLookup(users.value));

async function loadDetail() {
  loading.value = true;
  try {
    const requirementId = String(route.params.id || '');
    const [requirements, userOptions] = await Promise.all([
      listRequirementsApi(),
      listUserOptionsApi(),
    ]);
    users.value = userOptions;
    requirement.value = requirements.find((item) => item.id === requirementId);
    if (!requirement.value) return;

    const projectId = requirement.value.projectId;
    const [projects, taskItems, bugItems, planItems] = await Promise.all([
      listProjectsApi(),
      listDevelopmentTasksApi({ projectId, requirementId }),
      listBugsApi(projectId, requirementId),
      listTestPlansApi(projectId, requirementId),
    ]);
    project.value = projects.find((item) => item.id === projectId);
    tasks.value = taskItems;
    bugs.value = bugItems;
    testPlans.value = planItems;
  } finally {
    loading.value = false;
  }
}

onMounted(loadDetail);
</script>

<template>
  <div class="detail-page">
    <section class="header">
      <div>
        <h2>{{ requirement?.title || '需求详情' }}</h2>
        <p>{{ project?.name || '未找到需求' }}</p>
      </div>
      <TButton @click="loadDetail">
        <template #icon>
          <IconifyIcon icon="lucide:refresh-cw" />
        </template>
        刷新
      </TButton>
    </section>

    <TEmpty v-if="!loading && !requirement" description="需求不存在或已被删除" />

    <template v-else-if="requirement">
      <section class="panel">
        <TDescriptions bordered :column="2">
          <TDescriptionsItem label="状态">
            <TTag variant="light">{{ statusText[requirement.status] || requirement.status }}</TTag>
          </TDescriptionsItem>
          <TDescriptionsItem label="产品经理">
            {{ resolveUserName(requirement.createdBy, userLookup) }}
          </TDescriptionsItem>
          <TDescriptionsItem label="干系人">{{ requirement.stakeholders || '未填写' }}</TDescriptionsItem>
          <TDescriptionsItem label="优先级">{{ requirement.priority }}</TDescriptionsItem>
          <TDescriptionsItem label="未关闭缺陷">{{ activeBugCount }}</TDescriptionsItem>
          <TDescriptionsItem label="测试地址">{{ requirement.testUrl || '未配置' }}</TDescriptionsItem>
          <TDescriptionsItem label="创建时间">{{ formatDateTime(requirement.createTime) }}</TDescriptionsItem>
        </TDescriptions>
      </section>

      <section class="panel">
        <h3>需求正文</h3>
        <article
          class="markdown-body"
          v-html="renderMarkdown(requirement.description || '暂无需求内容')"
        ></article>
      </section>

      <section class="panel">
        <h3>任务</h3>
        <TTable row-key="id" class="sprint-compact-table" :columns="withSerialColumn(taskColumns)" :data="tasks" hover stripe>
          <template #assigneeId="{ row }">
            {{ resolveUserName(row.assigneeId, userLookup, '未指派') }}
          </template>
        </TTable>
      </section>

      <section class="panel">
        <h3>测试计划</h3>
        <TTable row-key="id" class="sprint-compact-table" :columns="withSerialColumn(testColumns)" :data="testPlans" hover stripe />
      </section>

      <section class="panel">
        <h3>缺陷</h3>
        <TTable row-key="id" class="sprint-compact-table" :columns="withSerialColumn(bugColumns)" :data="bugs" hover stripe>
          <template #createdBy="{ row }">
            {{ resolveUserName(row.createdBy, userLookup) }}
          </template>
        </TTable>
      </section>
    </template>
  </div>
</template>

<style scoped>
.detail-page {
  padding: 16px;
}

.header,
.panel {
  margin-bottom: 16px;
  padding: 16px 20px;
  background: var(--td-bg-color-container);
  border: 1px solid var(--td-component-border);
  border-radius: 6px;
}

.header {
  display: flex;
  gap: 16px;
  align-items: center;
  flex-wrap: wrap;
  justify-content: space-between;
}

.header h2,
.panel h3 {
  margin: 0;
}

.header p {
  margin: 6px 0 0;
  color: var(--td-text-color-secondary);
}

.panel h3 {
  margin-bottom: 12px;
}

.markdown-body {
  min-height: 120px;
  word-break: break-word;
}

.markdown-body :deep(h1),
.markdown-body :deep(h2),
.markdown-body :deep(h3),
.markdown-body :deep(p),
.markdown-body :deep(ul) {
  margin-top: 0;
}

.markdown-body :deep(code) {
  padding: 1px 4px;
  background: var(--td-bg-color-container-hover);
  border-radius: 4px;
}

.markdown-body :deep(pre) {
  padding: 10px;
  overflow: auto;
  background: var(--td-bg-color-container-hover);
  border-radius: 6px;
}
</style>
