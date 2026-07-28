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
  listProjectsApi,
  listRequirementsApi,
  listTestExecutionsApi,
  listTestPlansApi,
  listUserOptionsApi,
} from '#/api/sprint/mvp';
import { formatDateTime } from '#/views/_shared/date-format';
import { withSerialColumn } from '#/views/_shared/table-columns';
import { buildUserLookup, resolveUserName } from '../_shared/user-display';

import '../_shared/table-layout.css';

const route = useRoute();
const loading = ref(false);
const bug = ref<SprintMvpApi.Bug>();
const project = ref<SprintMvpApi.Project>();
const requirement = ref<SprintMvpApi.Requirement>();
const testPlan = ref<SprintTestApi.TestPlan>();
const executions = ref<SprintTestApi.TestExecution[]>([]);
const users = ref<SprintUserApi.UserOption[]>([]);
const userLookup = computed(() => buildUserLookup(users.value));

const statusText: Record<string, string> = {
  closed: '已关闭',
  fixed_ready_regression: '已修复待回归',
  fixing: '修复中',
  open: '未修复',
};
const severityText: Record<string, string> = {
  critical: '严重',
  major: '主要',
  minor: '次要',
  trivial: '轻微',
};
const statusTheme: Record<string, 'danger' | 'default' | 'primary' | 'success' | 'warning'> = {
  closed: 'success',
  fixed_ready_regression: 'primary',
  fixing: 'warning',
  open: 'danger',
};
const executionColumns = [
  { colKey: 'result', title: '结果', width: 100 },
  { colKey: 'testerId', title: '执行人', width: 130 },
  { colKey: 'actualResult', title: '实际结果' },
  { colKey: 'evidence', title: '证据', width: 180 },
  { colKey: 'executedAt', title: '执行时间', width: 190 },
];

async function loadDetail() {
  loading.value = true;
  try {
    const bugId = String(route.params.id || '');
    const bugs = await listBugsApi();
    bug.value = bugs.find((item) => item.id === bugId);
    if (!bug.value) return;

    const [projects, requirements, plans, userOptions] = await Promise.all([
      listProjectsApi(),
      listRequirementsApi(bug.value.projectId),
      listTestPlansApi(bug.value.projectId, bug.value.requirementId),
      listUserOptionsApi(),
    ]);
    users.value = userOptions;
    project.value = projects.find((item) => item.id === bug.value?.projectId);
    requirement.value = requirements.find((item) => item.id === bug.value?.requirementId);
    testPlan.value = plans.find((item) => item.id === bug.value?.testPlanId);
    executions.value = bug.value.testPlanId ? await listTestExecutionsApi(bug.value.testPlanId) : [];
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
        <h2>{{ bug?.title || '缺陷详情' }}</h2>
        <p>{{ project?.name || '未找到缺陷' }}</p>
      </div>
      <TButton @click="loadDetail">
        <template #icon>
          <IconifyIcon icon="lucide:refresh-cw" />
        </template>
        刷新
      </TButton>
    </section>

    <TEmpty v-if="!loading && !bug" description="缺陷不存在或已被删除" />

    <template v-else-if="bug">
      <section class="panel">
        <TDescriptions bordered :column="2">
          <TDescriptionsItem label="状态">
            <TTag :theme="statusTheme[bug.status] || 'default'" variant="light">
              {{ statusText[bug.status] || bug.status }}
            </TTag>
          </TDescriptionsItem>
          <TDescriptionsItem label="严重级别">
            <TTag variant="light">{{ severityText[bug.severity] || bug.severity }}</TTag>
          </TDescriptionsItem>
          <TDescriptionsItem label="环境">{{ bug.environment }}</TDescriptionsItem>
          <TDescriptionsItem label="项目">{{ project?.name || bug.projectId }}</TDescriptionsItem>
          <TDescriptionsItem label="需求">
            {{ requirement?.title || bug.requirementId }}
          </TDescriptionsItem>
          <TDescriptionsItem label="提交人">{{ resolveUserName(bug.createdBy, userLookup) }}</TDescriptionsItem>
          <TDescriptionsItem label="处理人">{{ resolveUserName(bug.developerId, userLookup, '未指派') }}</TDescriptionsItem>
          <TDescriptionsItem label="测试计划">{{ testPlan?.name || bug.testPlanId || '未绑定' }}</TDescriptionsItem>
          <TDescriptionsItem label="测试执行">{{ bug.testExecutionId || '未绑定' }}</TDescriptionsItem>
          <TDescriptionsItem label="修复时间">{{ formatDateTime(bug.fixedAt) }}</TDescriptionsItem>
          <TDescriptionsItem label="创建时间">{{ formatDateTime(bug.createTime) }}</TDescriptionsItem>
        </TDescriptions>
      </section>

      <section class="panel">
        <h3>缺陷描述</h3>
        <article>{{ bug.description || '暂无缺陷描述' }}</article>
      </section>

      <section class="panel">
        <h3>测试执行记录</h3>
        <TTable row-key="id" class="sprint-compact-table" :columns="withSerialColumn(executionColumns)" :data="executions" hover stripe>
          <template #testerId="{ row }">
            {{ resolveUserName(row.testerId, userLookup) }}
          </template>
          <template #executedAt="{ row }">
            {{ formatDateTime(row.executedAt) }}
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

.panel article {
  min-height: 100px;
  white-space: pre-wrap;
}
</style>
