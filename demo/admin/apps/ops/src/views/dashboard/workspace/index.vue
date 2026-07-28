<script lang="ts" setup>
import type { SprintMvpApi, SprintTestApi, SprintUserApi } from '#/api/sprint/mvp';

import { computed, onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';

import { preferences } from '@vben/preferences';
import { useUserStore } from '@vben/stores';
import { IconifyIcon } from '@vben/icons';

import {
  Avatar as TAvatar,
  Button as TButton,
  Card as TCard,
  Col as TCol,
  Link as TLink,
  Loading as TLoading,
  MessagePlugin,
  Row as TRow,
  Space as TSpace,
  Table as TTable,
  Tag as TTag,
} from 'tdesign-vue-next';

import {
  claimRequirementApi,
  getMvpSummaryApi,
  listActiveLeasesApi,
  listBugsApi,
  listDevelopmentTasksApi,
  listMyDevelopmentTasksApi,
  listProjectsApi,
  listRequirementsApi,
  listTestPlansApi,
  listUserOptionsApi,
} from '#/api/sprint/mvp';
import { formatDateTime } from '#/views/_shared/date-format';
import { withSerialColumn } from '#/views/_shared/table-columns';

import '../../sprint/_shared/table-layout.css';

type ActivityKind = 'bug' | 'lease' | 'requirement' | 'task' | 'test';

interface ActivityItem {
  actor: string;
  description: string;
  kind: ActivityKind;
  time: string;
  title: string;
}

const router = useRouter();
const userStore = useUserStore();

const loading = ref(false);
const summary = ref<SprintMvpApi.Summary>();
const projects = ref<SprintMvpApi.Project[]>([]);
const requirements = ref<SprintMvpApi.Requirement[]>([]);
const tasks = ref<SprintMvpApi.DevelopmentTask[]>([]);
const myTasks = ref<SprintMvpApi.DevelopmentTask[]>([]);
const bugs = ref<SprintMvpApi.Bug[]>([]);
const leases = ref<SprintMvpApi.Lease[]>([]);
const testPlans = ref<SprintTestApi.TestPlan[]>([]);
const users = ref<SprintUserApi.UserOption[]>([]);

const userMap = computed(() => Object.fromEntries(users.value.map((item) => [item.id, item])));
const projectMap = computed(() => Object.fromEntries(projects.value.map((item) => [item.id, item])));
const requirementMap = computed(() =>
  Object.fromEntries(requirements.value.map((item) => [item.id, item])),
);

const statusText: Record<string, string> = {
  active: '进行中',
  approved: '已评审',
  assigned: '已指派',
  blocked: '阻塞',
  closed: '已关闭',
  completed: '已完成',
  decomposed: '已拆解',
  developing: '开发中',
  failed: '失败',
  fixing: '修复中',
  in_progress: '推进中',
  open: '打开',
  passed: '通过',
  pending_assign: '待指派',
  pending_fix: '待修复',
  pending_review: '待评审',
  ready_for_development: '待开发',
  ready_for_test: '待测试',
  rejected: '已驳回',
  testing: '测试中',
  tested: '已测试',
};

const statusTheme: Record<string, 'danger' | 'default' | 'primary' | 'success' | 'warning'> = {
  assigned: 'primary',
  blocked: 'warning',
  closed: 'default',
  completed: 'success',
  developing: 'primary',
  failed: 'danger',
  fixing: 'warning',
  in_progress: 'primary',
  open: 'danger',
  passed: 'success',
  pending_assign: 'warning',
  pending_fix: 'warning',
  ready_for_development: 'primary',
  ready_for_test: 'warning',
  testing: 'primary',
  tested: 'success',
};

const metricCards = computed(() => [
  {
    label: '项目总数',
    value: summary.value?.projectCount ?? projects.value.length,
    tone: 'blue',
  },
  {
    label: '需求总数',
    value: summary.value?.requirementCount ?? requirements.value.length,
    tone: 'green',
  },
  {
    label: '开发中',
    value: summary.value?.developingRequirementCount ?? countRequirements('developing'),
    tone: 'indigo',
  },
  {
    label: '待测试',
    value: summary.value?.readyTestRequirementCount ?? countRequirements('ready_for_test'),
    tone: 'amber',
  },
  {
    label: '开放缺陷',
    value: summary.value?.openBugCount ?? bugs.value.filter((item) => item.status !== 'closed').length,
    tone: 'red',
  },
  {
    label: '活跃租约',
    value: summary.value?.activeLeaseCount ?? leases.value.length,
    tone: 'slate',
  },
]);

const activeMyTasks = computed(() =>
  myTasks.value
    .filter((item) => item.status !== 'completed')
    .sort(byRecentTask)
    .slice(0, 6),
);

const quickRequirements = computed(() =>
  requirements.value
    .filter((item) => ['approved', 'decomposed', 'ready_for_development'].includes(item.status))
    .sort((left, right) => right.priority - left.priority || right.createTime.localeCompare(left.createTime))
    .slice(0, 6),
);

const hallTasks = computed(() =>
  tasks.value
    .filter((item) => item.status === 'pending_assign')
    .sort((left, right) => right.priority - left.priority || right.createTime.localeCompare(left.createTime))
    .slice(0, 6),
);

const auditItems = computed<ActivityItem[]>(() => {
  const entries: ActivityItem[] = [
    ...tasks.value.map((task) => ({
      actor: displayUser(task.assigneeId || task.createdBy),
      description: `${projectName(task.projectId)} / ${requirementName(task.requirementId)}`,
      kind: 'task' as const,
      time: task.updateTime || task.completedAt || task.assignedAt || task.createTime,
      title: `${statusLabel(task.status)}：${task.title}`,
    })),
    ...requirements.value.map((requirement) => ({
      actor: displayUser(requirement.developerId || requirement.createdBy),
      description: projectName(requirement.projectId),
      kind: 'requirement' as const,
      time:
        requirement.closedAt ||
        requirement.testedAt ||
        requirement.developmentCompletedAt ||
        requirement.submittedAt ||
        requirement.createTime,
      title: `${statusLabel(requirement.status)}：${requirement.title}`,
    })),
    ...bugs.value.map((bug) => ({
      actor: displayUser(bug.developerId || bug.createdBy),
      description: `${projectName(bug.projectId)} / ${requirementName(bug.requirementId)}`,
      kind: 'bug' as const,
      time: bug.fixedAt || bug.createTime,
      title: `${statusLabel(bug.status)}：${bug.title}`,
    })),
    ...testPlans.value.map((plan) => ({
      actor: displayUser(plan.testerId || plan.createdBy),
      description: `${projectName(plan.projectId)} / ${requirementName(plan.requirementId)}`,
      kind: 'test' as const,
      time: plan.completedAt || plan.startedAt || plan.createTime,
      title: `${statusLabel(plan.status)}：${plan.name}`,
    })),
    ...leases.value.map((lease) => ({
      actor: displayUser(lease.ownerId),
      description: `${projectName(lease.projectId)} / ${lease.targetType}`,
      kind: 'lease' as const,
      time: lease.completedAt || lease.createTime,
      title: `${statusLabel(lease.status)}：${lease.targetId}`,
    })),
  ];

  return entries
    .filter((item) => item.time)
    .sort((left, right) => right.time.localeCompare(left.time))
    .slice(0, 10);
});

const logItems = computed(() => [
  `当前用户：${userStore.userInfo?.realName || userStore.userInfo?.username || '未识别'}`,
  `项目：${projects.value.length} 个，需求：${requirements.value.length} 条，任务：${tasks.value.length} 条`,
  `我的未完成任务：${activeMyTasks.value.length} 条，待接取需求：${quickRequirements.value.length} 条`,
  `开放缺陷：${bugs.value.filter((item) => item.status !== 'closed').length} 条，测试计划：${testPlans.value.length} 条`,
]);

const taskColumns = [
  { colKey: 'title', title: '任务', ellipsis: true },
  { colKey: 'status', title: '状态', width: 96 },
  { colKey: 'projectId', title: '项目', width: 140, ellipsis: true },
  { colKey: 'actions', title: '操作', width: 80 },
];

const requirementColumns = [
  { colKey: 'title', title: '需求', ellipsis: true },
  { colKey: 'status', title: '状态', width: 96 },
  { colKey: 'priority', title: '优先级', width: 76 },
  { colKey: 'actions', title: '操作', width: 104 },
];

async function loadDashboard() {
  loading.value = true;
  try {
    const [
      summaryResult,
      projectResult,
      requirementResult,
      taskResult,
      myTaskResult,
      bugResult,
      leaseResult,
      testPlanResult,
      userResult,
    ] = await Promise.all([
      getMvpSummaryApi(),
      listProjectsApi(),
      listRequirementsApi(),
      listDevelopmentTasksApi(),
      listMyDevelopmentTasksApi(),
      listBugsApi(),
      listActiveLeasesApi(),
      listTestPlansApi(),
      listUserOptionsApi(),
    ]);

    summary.value = summaryResult;
    projects.value = projectResult;
    requirements.value = requirementResult;
    tasks.value = taskResult;
    myTasks.value = myTaskResult;
    bugs.value = bugResult;
    leases.value = leaseResult;
    testPlans.value = testPlanResult;
    users.value = userResult;
  } finally {
    loading.value = false;
  }
}

async function claimRequirement(requirement: SprintMvpApi.Requirement) {
  await claimRequirementApi(requirement.id, {});
  MessagePlugin.success('接取成功');
  await loadDashboard();
}

function byRecentTask(left: SprintMvpApi.DevelopmentTask, right: SprintMvpApi.DevelopmentTask) {
  const leftTime = left.updateTime || left.assignedAt || left.createTime;
  const rightTime = right.updateTime || right.assignedAt || right.createTime;
  return rightTime.localeCompare(leftTime);
}

function countRequirements(status: string) {
  return requirements.value.filter((item) => item.status === status).length;
}

function displayUser(id?: string) {
  if (!id) return '-';
  const user = userMap.value[id];
  return user ? `${user.displayName || user.username}` : id;
}

function go(path: string) {
  router.push(path);
}

function projectName(id: string) {
  return projectMap.value[id]?.name || id;
}

function requirementName(id: string) {
  return requirementMap.value[id]?.title || id;
}

function statusLabel(status: string) {
  return statusText[status] || status;
}

function statusTagTheme(status: string) {
  return statusTheme[status] || 'default';
}

onMounted(loadDashboard);
</script>

<template>
  <TLoading :loading="loading" show-overlay>
    <div class="agent-dashboard">
      <section class="dashboard-hero">
        <div class="profile-block">
          <TAvatar
            class="profile-avatar"
            :image="userStore.userInfo?.avatar || preferences.app.defaultAvatar"
            size="64px"
          />
          <div>
            <h2>{{ userStore.userInfo?.realName || userStore.userInfo?.username }}</h2>
            <p>AgentSprint 综合工作台</p>
          </div>
        </div>
        <TSpace>
          <TButton variant="outline" @click="loadDashboard">
            <template #icon>
              <IconifyIcon icon="lucide:refresh-cw" />
            </template>
            刷新
          </TButton>
          <TButton theme="primary" @click="go('/sprint/my-tasks')">
            <template #icon>
              <IconifyIcon icon="lucide:list-checks" />
            </template>
            我的任务
          </TButton>
        </TSpace>
      </section>

      <section class="metric-grid">
        <div v-for="item in metricCards" :key="item.label" class="metric-card" :data-tone="item.tone">
          <span>{{ item.label }}</span>
          <strong>{{ item.value }}</strong>
        </div>
      </section>

      <TRow :gutter="[12, 12]">
        <TCol :lg="14" :xs="12">
          <TCard bordered title="我的工作推进">
            <TTable
              row-key="id"
              class="sprint-compact-table"
              :columns="withSerialColumn(taskColumns)"
              :data="activeMyTasks"
              size="small"
              hover
              stripe
            >
              <template #status="{ row }">
                <TTag :theme="statusTagTheme(row.status)" variant="light">
                  {{ statusLabel(row.status) }}
                </TTag>
              </template>
              <template #projectId="{ row }">
                {{ projectName(row.projectId) }}
              </template>
              <template #actions="{ row }">
                <TLink theme="primary" class="sprint-action-link" @click="go(`/sprint/tasks/detail/${row.id}`)">
                  <IconifyIcon icon="lucide:eye" />
                  <span>详情</span>
                </TLink>
              </template>
            </TTable>
          </TCard>
        </TCol>

        <TCol :lg="10" :xs="12">
          <TCard bordered title="日志信息">
            <ul class="log-list">
              <li v-for="item in logItems" :key="item">{{ item }}</li>
            </ul>
          </TCard>
        </TCol>

        <TCol :lg="14" :xs="12">
          <TCard bordered title="任务大厅简版">
            <TTable
              row-key="id"
              class="sprint-compact-table"
              :columns="withSerialColumn(requirementColumns)"
              :data="quickRequirements"
              size="small"
              hover
              stripe
            >
              <template #status="{ row }">
                <TTag :theme="statusTagTheme(row.status)" variant="light">
                  {{ statusLabel(row.status) }}
                </TTag>
              </template>
              <template #actions="{ row }">
                <TSpace size="small">
                  <TLink theme="primary" class="sprint-action-link" @click="claimRequirement(row)">
                    <IconifyIcon icon="lucide:handshake" />
                    <span>接取</span>
                  </TLink>
                  <TLink class="sprint-action-link" @click="go(`/sprint/requirements/detail/${row.id}`)">
                    <IconifyIcon icon="lucide:eye" />
                    <span>详情</span>
                  </TLink>
                </TSpace>
              </template>
            </TTable>
          </TCard>
        </TCol>

        <TCol :lg="10" :xs="12">
          <TCard bordered title="待指派任务">
            <div class="hall-list">
              <div v-for="task in hallTasks" :key="task.id" class="hall-item">
                <div>
                  <strong>{{ task.title }}</strong>
                  <span>{{ projectName(task.projectId) }}</span>
                </div>
                <TTag theme="warning" variant="light">P{{ task.priority }}</TTag>
              </div>
              <div v-if="hallTasks.length === 0" class="empty-line">暂无待指派任务</div>
            </div>
          </TCard>
        </TCol>

        <TCol :xs="12">
          <TCard bordered title="工作审计">
            <div class="audit-list">
              <div v-for="item in auditItems" :key="`${item.kind}-${item.title}-${item.time}`" class="audit-item">
                <div class="audit-dot" :data-kind="item.kind"></div>
                <div>
                  <strong>{{ item.title }}</strong>
                  <p>{{ item.description }} · {{ item.actor }}</p>
                </div>
                <time>{{ formatDateTime(item.time) }}</time>
              </div>
            </div>
          </TCard>
        </TCol>
      </TRow>
    </div>
  </TLoading>
</template>

<style scoped>
.agent-dashboard {
  display: grid;
  gap: 12px;
  padding: 12px;
}

.dashboard-hero {
  display: flex;
  gap: 16px;
  align-items: center;
  justify-content: space-between;
  padding: 16px;
  background: var(--td-bg-color-container);
  border: 1px solid var(--td-component-border);
  border-radius: 6px;
}

.profile-block {
  display: flex;
  gap: 14px;
  align-items: center;
}

.profile-avatar {
  flex: 0 0 auto;
}

.profile-block h2 {
  margin: 0;
  font-size: 20px;
  line-height: 28px;
}

.profile-block p {
  margin: 4px 0 0;
  color: var(--td-text-color-secondary);
}

.metric-grid {
  display: grid;
  grid-template-columns: repeat(6, minmax(0, 1fr));
  gap: 12px;
}

.metric-card {
  display: grid;
  gap: 8px;
  min-height: 86px;
  padding: 14px;
  background: var(--td-bg-color-container);
  border: 1px solid var(--td-component-border);
  border-radius: 6px;
}

.metric-card span {
  color: var(--td-text-color-secondary);
}

.metric-card strong {
  font-size: 28px;
  line-height: 34px;
}

.metric-card[data-tone='amber'] {
  border-left: 4px solid var(--td-warning-color);
}

.metric-card[data-tone='blue'],
.metric-card[data-tone='indigo'] {
  border-left: 4px solid var(--td-brand-color);
}

.metric-card[data-tone='green'] {
  border-left: 4px solid var(--td-success-color);
}

.metric-card[data-tone='red'] {
  border-left: 4px solid var(--td-error-color);
}

.metric-card[data-tone='slate'] {
  border-left: 4px solid var(--td-gray-color-7);
}

.log-list,
.hall-list,
.audit-list {
  display: grid;
  gap: 10px;
  padding: 0;
  margin: 0;
}

.log-list {
  list-style: none;
}

.log-list li,
.hall-item,
.audit-item {
  padding: 10px 12px;
  background: var(--td-bg-color-secondarycontainer);
  border-radius: 6px;
}

.hall-item,
.audit-item {
  display: flex;
  gap: 10px;
  align-items: center;
  justify-content: space-between;
}

.hall-item div,
.audit-item div:nth-child(2) {
  min-width: 0;
}

.hall-item strong,
.audit-item strong {
  display: block;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.hall-item span,
.audit-item p,
.audit-item time {
  margin: 4px 0 0;
  color: var(--td-text-color-secondary);
  font-size: 12px;
}

.audit-dot {
  width: 10px;
  height: 10px;
  flex: 0 0 auto;
  border-radius: 50%;
  background: var(--td-brand-color);
}

.audit-dot[data-kind='bug'] {
  background: var(--td-error-color);
}

.audit-dot[data-kind='test'] {
  background: var(--td-success-color);
}

.audit-dot[data-kind='lease'] {
  background: var(--td-warning-color);
}

.audit-item time {
  flex: 0 0 auto;
  margin: 0;
}

.empty-line {
  padding: 20px;
  color: var(--td-text-color-secondary);
  text-align: center;
}

@media (max-width: 1100px) {
  .metric-grid {
    grid-template-columns: repeat(3, minmax(0, 1fr));
  }
}

@media (max-width: 760px) {
  .dashboard-hero {
    align-items: flex-start;
    flex-direction: column;
  }

  .metric-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .audit-item {
    align-items: flex-start;
    flex-direction: column;
  }
}
</style>
