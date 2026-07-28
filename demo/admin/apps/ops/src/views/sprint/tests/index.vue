<script lang="ts" setup>
import type { SprintMvpApi, SprintTestApi, SprintUserApi } from '#/api/sprint/mvp';
import type { FormInstanceFunctions, FormRules } from 'tdesign-vue-next';

import { IconifyIcon } from '@vben/icons';
import { computed, onMounted, reactive, ref } from 'vue';

import {
  Button as TButton,
  Drawer as TDrawer,
  Form as TForm,
  FormItem as TFormItem,
  Input as TInput,
  Link as TLink,
  MessagePlugin,
  Select as TSelect,
  Space as TSpace,
  Table as TTable,
  Tag as TTag,
  Textarea as TTextarea,
} from 'tdesign-vue-next';

import {
  createBugApi,
  createTestPlanApi,
  listBugsApi,
  listProjectsApi,
  listRequirementsApi,
  listTestExecutionsApi,
  listTestPlansApi,
  listUserOptionsApi,
  startTestPlanApi,
  submitTestExecutionApi,
  updateTestExecutionBugApi,
} from '#/api/sprint/mvp';
import { requiredRule, validateForm } from '#/views/_shared/form-rules';
import { formatDateTime } from '#/views/_shared/date-format';
import { withSerialColumn } from '#/views/_shared/table-columns';
import ProjectContextListShell from '#/components/project-context-list-shell/project-context-list-shell.vue';

import '../_shared/table-layout.css';

defineOptions({ name: 'SprintTests' });

const creating = ref(false);
const loading = ref(false);
const executionLoading = ref(false);
const createVisible = ref(false);
const planFormRef = ref<FormInstanceFunctions>();
const executionVisible = ref(false);
const executionFormRef = ref<FormInstanceFunctions>();
const detailVisible = ref(false);
const currentPlan = ref<SprintTestApi.TestPlan>();
const projects = ref<SprintMvpApi.Project[]>([]);
const requirements = ref<SprintMvpApi.Requirement[]>([]);
const plans = ref<SprintTestApi.TestPlan[]>([]);
const executions = ref<SprintTestApi.TestExecution[]>([]);
const bugs = ref<SprintMvpApi.Bug[]>([]);
const users = ref<SprintUserApi.UserOption[]>([]);

const filters = reactive({
  projectId: '',
  requirementKeyword: '',
});
const planForm = reactive({
  environment: 'test',
  name: '',
  projectId: '',
  requirementId: '',
  testUrl: '',
});
const executionForm = reactive({
  actualResult: '',
  bugHandling: 'create',
  bugId: '',
  evidence: '',
  result: 'passed',
});
const planRules: FormRules<typeof planForm> = {
  name: requiredRule('请输入测试计划名称'),
  projectId: requiredRule('请选择项目', 'change'),
  requirementId: requiredRule('请选择需求', 'change'),
};
const executionRules = computed<FormRules<typeof executionForm>>(() => ({
  actualResult: requiredRule('请输入实际结果'),
  bugId:
    executionForm.result === 'failed' && executionForm.bugHandling === 'existing'
      ? requiredRule('请选择已有缺陷', 'change')
      : [],
  result: requiredRule('请选择执行结果', 'change'),
}));
const pagination = reactive({
  current: 1,
  pageSize: 30,
});
const testableRequirementStatuses = new Set(['ready_test', 'testing', 'tested', 'pending_fix']);

const projectOptions = computed(() =>
  projects.value.map((item) => ({ label: `${item.code} / ${item.name}`, value: item.id })),
);
const planRequirementOptions = computed(() =>
  requirements.value
    .filter(
      (item) =>
        (!planForm.projectId || item.projectId === planForm.projectId) &&
        testableRequirementStatuses.has(item.status),
    )
    .map((item) => ({ label: item.title, value: item.id })),
);
const projectMap = computed(() => Object.fromEntries(projects.value.map((item) => [item.id, item])));
const requirementMap = computed(() =>
  Object.fromEntries(requirements.value.map((item) => [item.id, item])),
);
const userMap = computed(() => Object.fromEntries(users.value.map((item) => [item.id, item])));
const userNameMap = computed(() => Object.fromEntries(users.value.map((item) => [item.username, item])));
const bugOptions = computed(() =>
  bugs.value
    .filter((item) => item.status !== 'closed')
    .map((item) => ({
      label: `${item.title} / ${bugStatusText[item.status] || item.status}`,
      value: item.id,
    })),
);

const planColumns = [
  { colKey: 'name', ellipsis: true, title: '测试计划' },
  { colKey: 'requirementId', title: '需求', width: 220 },
  { colKey: 'environment', title: '环境', width: 100 },
  { colKey: 'status', title: '状态', width: 110 },
  { colKey: 'testability', title: '可测状态', width: 130 },
  { colKey: 'createdBy', title: '测试负责人', width: 130 },
  { colKey: 'actions', title: '操作', width: 260 },
];

const executionColumns = [
  { colKey: 'result', title: '结果', width: 100 },
  { colKey: 'testerId', title: '执行人', width: 130 },
  { colKey: 'actualResult', title: '实际结果' },
  { colKey: 'evidence', title: '证据', width: 180 },
  { colKey: 'createdBugId', title: '关联缺陷', width: 170 },
  { colKey: 'executedAt', title: '执行时间', width: 190 },
];

const planStatusText: Record<string, string> = {
  blocked: '已阻塞',
  closed: '已关闭',
  failed: '未通过',
  passed: '已通过',
  pending: '待执行',
  testing: '测试中',
};
const requirementStatusText: Record<string, string> = {
  approved: '评审通过',
  completed: '已完成',
  decomposed: '已拆解',
  developing: '进行中',
  draft: '草稿',
  pending_fix: '待修复',
  pending_review: '待评审',
  ready_development: '待开发',
  ready_test: '待测试',
  rejected: '评审驳回',
  tested: '已测试',
  testing: '测试中',
  voided: '已作废',
};
const executionResultText: Record<string, string> = {
  blocked: '阻塞',
  failed: '失败',
  passed: '通过',
};
const bugStatusText: Record<string, string> = {
  closed: '已关闭',
  fixed_ready_regression: '已修复待回归',
  fixing: '修复中',
  open: '未修复',
};
const statusTheme: Record<string, 'danger' | 'default' | 'primary' | 'success' | 'warning'> = {
  blocked: 'warning',
  closed: 'default',
  failed: 'danger',
  passed: 'success',
  pending: 'default',
  testing: 'primary',
};
const tablePagination = computed(() => ({
  current: pagination.current,
  pageSize: pagination.pageSize,
  pageSizeOptions: [30, 50, 100, 200],
  showJumper: true,
  showPageSize: true,
  size: 'small' as const,
  total: plans.value.length,
}));

function handlePageChange(pageInfo: { current: number; pageSize: number }) {
  pagination.current = pageInfo.current;
  pagination.pageSize = pageInfo.pageSize;
}

function filterPlansByRequirementKeyword(items: SprintTestApi.TestPlan[]) {
  const keyword = filters.requirementKeyword.trim().toLowerCase();
  if (!keyword) return items;

  return items.filter((plan) => {
    const requirement = requirementMap.value[plan.requirementId];
    return [requirement?.title, requirement?.id, plan.requirementId]
      .filter(Boolean)
      .some((value) => String(value).toLowerCase().includes(keyword));
  });
}

async function loadBase() {
  [projects.value, requirements.value, users.value] = await Promise.all([
    listProjectsApi(),
    listRequirementsApi(),
    listUserOptionsApi(),
  ]);
  filters.projectId ||= projects.value[0]?.id || '';
  if (!planForm.testUrl) {
    planForm.testUrl = projects.value.find((item) => item.id === filters.projectId)?.testEnvironmentUrl || '';
  }
}

async function loadPlans() {
  loading.value = true;
  try {
    const planItems = await listTestPlansApi(filters.projectId || undefined);
    plans.value = filterPlansByRequirementKeyword(planItems);
    pagination.current = 1;
  } finally {
    loading.value = false;
  }
}

async function loadExecutions(planId: string) {
  executionLoading.value = true;
  try {
    executions.value = await listTestExecutionsApi(planId);
  } finally {
    executionLoading.value = false;
  }
}

async function loadBugs(projectId?: string, requirementId?: string) {
  bugs.value = await listBugsApi(projectId, requirementId);
}

async function handleProjectChange() {
  filters.requirementKeyword = '';
  planForm.testUrl = projects.value.find((item) => item.id === filters.projectId)?.testEnvironmentUrl || '';
  await loadPlans();
}

async function queryPlans() {
  await loadPlans();
}

async function resetFilters() {
  Object.assign(filters, {
    projectId: projects.value[0]?.id || '',
    requirementKeyword: '',
  });
  await loadPlans();
}

function handlePlanProjectChange() {
  planForm.requirementId = planRequirementOptions.value[0]?.value || '';
  planForm.testUrl = projects.value.find((item) => item.id === planForm.projectId)?.testEnvironmentUrl || '';
  planForm.name = `${requirementMap.value[planForm.requirementId]?.title || '需求'} 测试计划`;
}

function openCreate() {
  Object.assign(planForm, {
    environment: 'test',
    projectId: filters.projectId || projects.value[0]?.id || '',
    requirementId: '',
    testUrl: '',
  });
  if (
    !planForm.requirementId ||
    !planRequirementOptions.value.some((item) => item.value === planForm.requirementId)
  ) {
    planForm.requirementId = planRequirementOptions.value[0]?.value || '';
  }
  planForm.name = `${requirementMap.value[planForm.requirementId]?.title || '需求'} 测试计划`;
  planForm.testUrl = projects.value.find((item) => item.id === planForm.projectId)?.testEnvironmentUrl || '';
  createVisible.value = true;
}

async function createPlan() {
  if (creating.value) return;
  if (!(await validateForm(planFormRef.value))) return;
  if (!planForm.projectId || !planForm.requirementId || !planForm.name.trim()) {
    MessagePlugin.warning('项目、需求和测试计划名称不能为空');
    return;
  }

  creating.value = true;
  try {
    await createTestPlanApi({
      environment: planForm.environment,
      name: planForm.name.trim(),
      projectId: planForm.projectId,
      requirementId: planForm.requirementId,
      testUrl: planForm.testUrl.trim() || undefined,
    });
    MessagePlugin.success('测试计划已创建');
    createVisible.value = false;
    await loadPlans();
  } finally {
    creating.value = false;
  }
}

async function startPlan(plan: SprintTestApi.TestPlan) {
  if (!canStartPlan(plan)) {
    MessagePlugin.warning('关联需求当前不可开始测试');
    return;
  }

  try {
    await startTestPlanApi(plan.id);
    MessagePlugin.success('测试计划已进入测试中');
    await loadPlans();
  } catch (error: any) {
    MessagePlugin.warning(error?.response?.data?.message || error?.message || 'Requirement is not ready for testing.');
  }
}

function canStartPlan(plan: SprintTestApi.TestPlan) {
  const requirement = requirementMap.value[plan.requirementId];
  return plan.status === 'pending' && !!requirement && testableRequirementStatuses.has(requirement.status);
}

function planTestabilityText(plan: SprintTestApi.TestPlan) {
  const requirement = requirementMap.value[plan.requirementId];
  if (canStartPlan(plan)) return '可开始测试';
  if (plan.status !== 'pending') return planStatusText[plan.status] || plan.status;
  if (!requirement) return '需求不存在';
  return requirementStatusText[requirement.status] || requirement.status;
}

function resolveUserName(userId?: string) {
  if (!userId) return '未指定';
  const user = userMap.value[userId] || userNameMap.value[userId];
  return user?.displayName || user?.username || userId;
}

async function openExecution(plan: SprintTestApi.TestPlan) {
  currentPlan.value = plan;
  Object.assign(executionForm, {
    actualResult: '',
    bugHandling: 'create',
    bugId: '',
    evidence: '',
    result: 'passed',
  });
  await Promise.all([loadExecutions(plan.id), loadBugs(plan.projectId, plan.requirementId)]);
  executionVisible.value = true;
}

async function openDetail(plan: SprintTestApi.TestPlan) {
  currentPlan.value = plan;
  await loadExecutions(plan.id);
  detailVisible.value = true;
}

async function submitExecution() {
  if (!currentPlan.value) return;
  if (!(await validateForm(executionFormRef.value))) return;
  if (!executionForm.actualResult.trim()) {
    MessagePlugin.warning('实际结果不能为空');
    return;
  }
  if (
    executionForm.result === 'failed' &&
    executionForm.bugHandling === 'existing' &&
    !executionForm.bugId
  ) {
    MessagePlugin.warning('请选择要关联的已有缺陷');
    return;
  }

  if (executionForm.result === 'failed' && executionForm.bugHandling === 'existing') {
    await submitTestExecutionApi(currentPlan.value.id, {
      actualResult: executionForm.actualResult,
      bugId: executionForm.bugId,
      evidence: executionForm.evidence,
      result: executionForm.result,
    });
    MessagePlugin.success('测试执行已提交，并关联已有缺陷');
  } else if (executionForm.result === 'failed' && executionForm.bugHandling === 'create') {
    const bug = await createBugApi({
      description: `${executionForm.actualResult}\n\n证据：${executionForm.evidence || '无'}`,
      environment: currentPlan.value.environment,
      projectId: currentPlan.value.projectId,
      requirementId: currentPlan.value.requirementId,
      severity: 'major',
      testPlanId: currentPlan.value.id,
      title: `${requirementMap.value[currentPlan.value.requirementId]?.title || '需求'} 测试失败`,
    });
    const execution = await submitTestExecutionApi(currentPlan.value.id, {
      actualResult: executionForm.actualResult,
      createdBugId: bug.id,
      evidence: executionForm.evidence,
      result: executionForm.result,
    });
    await updateTestExecutionBugApi(currentPlan.value.id, execution.id, {
      createdBugId: bug.id,
    });
    await loadBugs(currentPlan.value.projectId, currentPlan.value.requirementId);
    MessagePlugin.success(`测试执行已提交，并创建缺陷 ${bug.title}`);
  } else {
    await submitTestExecutionApi(currentPlan.value.id, {
      actualResult: executionForm.actualResult,
      evidence: executionForm.evidence,
      result: executionForm.result,
    });
    MessagePlugin.success('测试执行已提交');
  }

  executionVisible.value = false;
  await loadPlans();
}

onMounted(async () => {
  await loadBase();
  await loadPlans();
});
</script>

<template>
  <div class="tests-page">
    <ProjectContextListShell
      v-model:selected-project-id="filters.projectId"
      @project-change="handleProjectChange"
    >
      <template #title>
        测试验证
      </template>
      <template #description>
        围绕项目和需求管理测试计划、执行结果、失败缺陷和回归验证。
      </template>

      <template #actions>
        <TButton theme="primary" :disabled="!filters.projectId" @click="openCreate">
            <template #icon>
              <IconifyIcon icon="lucide:plus" />
            </template>
            新增测试计划
          </TButton>
      </template>

      <section class="sprint-filter-panel">
        <div class="sprint-filter-grid">
          <label class="sprint-filter-field">
            <span>需求</span>
            <TInput
              v-model="filters.requirementKeyword"
              clearable
              placeholder="输入需求标题或ID"
              @enter="queryPlans"
            />
          </label>
          <div class="sprint-filter-actions">
            <TButton theme="primary" :loading="loading" @click="queryPlans">
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
          <h3>测试计划列表</h3>
          <div class="sprint-table-actions">
            <TButton shape="circle" variant="outline" title="刷新" :loading="loading" @click="loadPlans">
              <IconifyIcon icon="lucide:refresh-cw" />
            </TButton>
          </div>
        </div>

        <TTable
          row-key="id"
          class="sprint-compact-table"
          :columns="withSerialColumn(planColumns, { offset: () => (pagination.current - 1) * pagination.pageSize })"
          :data="plans"
          :loading="loading"
          :pagination="tablePagination"
          size="small"
          hover
          stripe
          @page-change="handlePageChange"
        >
          <template #requirementId="{ row }">
            {{ requirementMap[row.requirementId]?.title || row.requirementId }}
          </template>
          <template #status="{ row }">
            <TTag :theme="statusTheme[row.status] || 'default'" variant="light">
              {{ planStatusText[row.status] || row.status }}
            </TTag>
          </template>
          <template #testability="{ row }">
            <TTag :theme="canStartPlan(row) ? 'success' : 'default'" variant="light">
              {{ planTestabilityText(row) }}
            </TTag>
          </template>
          <template #createdBy="{ row }">
            {{ resolveUserName(row.createdBy) }}
          </template>
          <template #actions="{ row }">
            <TSpace class="sprint-row-actions">
              <TLink theme="primary" @click="openDetail(row)">
                <IconifyIcon icon="lucide:search" />
                <span>详情</span>
              </TLink>
              <TLink v-if="canStartPlan(row)" theme="primary" @click="startPlan(row)">
                <IconifyIcon icon="lucide:play" />
                <span>开始测试</span>
              </TLink>
              <TLink v-if="row.status === 'testing'" theme="primary" @click="openExecution(row)">
                <IconifyIcon icon="lucide:clipboard-check" />
                <span>提交结果</span>
              </TLink>
            </TSpace>
          </template>
        </TTable>
      </section>
    </ProjectContextListShell>

    <TDrawer
      v-model:visible="createVisible"
      :size="'40%'"
      header="新增测试计划"
      :confirm-btn="{ content: '保存', loading: creating }"
      @confirm="createPlan"
    >
      <TForm ref="planFormRef" :data="planForm" :rules="planRules" label-width="90px">
        <TFormItem label="项目" name="projectId">
          <TSelect
            v-model="planForm.projectId"
            :options="projectOptions"
            @change="handlePlanProjectChange"
          />
        </TFormItem>
        <TFormItem label="需求" name="requirementId">
          <TSelect
            v-model="planForm.requirementId"
            :options="planRequirementOptions"
            filterable
          />
        </TFormItem>
        <TFormItem label="计划名称" name="name">
          <TInput v-model="planForm.name" />
        </TFormItem>
        <TFormItem label="测试环境">
          <TInput v-model="planForm.environment" />
        </TFormItem>
        <TFormItem label="测试地址">
          <TInput v-model="planForm.testUrl" placeholder="http://localhost:5999" />
        </TFormItem>
      </TForm>
    </TDrawer>

    <TDrawer
      v-model:visible="executionVisible"
      :size="'60%'"
      :header="currentPlan?.name || '提交测试结果'"
      @confirm="submitExecution"
    >
      <TForm ref="executionFormRef" :data="executionForm" :rules="executionRules" label-width="90px">
        <TFormItem label="执行结果" name="result">
          <TSelect
            v-model="executionForm.result"
            :options="[
              { label: '通过', value: 'passed' },
              { label: '失败', value: 'failed' },
              { label: '阻塞', value: 'blocked' },
            ]"
          />
        </TFormItem>
        <TFormItem label="实际结果" name="actualResult">
          <TTextarea
            v-model="executionForm.actualResult"
            placeholder="记录实际执行现象、结论和阻塞原因"
          />
        </TFormItem>
        <TFormItem label="证据">
          <TTextarea
            v-model="executionForm.evidence"
            placeholder="截图地址、日志摘要、测试环境地址或复现补充"
          />
        </TFormItem>
        <TFormItem v-if="executionForm.result === 'failed'" label="失败处理">
          <TSelect
            v-model="executionForm.bugHandling"
            :options="[
              { label: '提交后自动创建缺陷', value: 'create' },
              { label: '关联已有缺陷', value: 'existing' },
            ]"
          />
        </TFormItem>
        <TFormItem
          v-if="executionForm.result === 'failed' && executionForm.bugHandling === 'existing'"
          label="已有缺陷"
          name="bugId"
        >
          <TSelect
            v-model="executionForm.bugId"
            :options="bugOptions"
            filterable
            placeholder="选择当前需求下未关闭缺陷"
          />
        </TFormItem>
      </TForm>

      <section class="execution-history">
        <h3>执行记录</h3>
        <TTable
          row-key="id"
          class="sprint-compact-table"
          :columns="withSerialColumn(executionColumns)"
          :data="executions"
          :loading="executionLoading"
          size="small"
          hover
          stripe
        >
          <template #result="{ row }">
            <TTag :theme="statusTheme[row.result] || 'default'" variant="light">
              {{ executionResultText[row.result] || row.result }}
            </TTag>
          </template>
          <template #testerId="{ row }">
            {{ resolveUserName(row.testerId) }}
          </template>
          <template #createdBugId="{ row }">
            {{ row.createdBugId || row.bugId || '-' }}
          </template>
          <template #executedAt="{ row }">
            {{ formatDateTime(row.executedAt) }}
          </template>
        </TTable>
      </section>
    </TDrawer>

    <TDrawer
      v-model:visible="detailVisible"
      :footer="false"
      :size="'60%'"
      :header="currentPlan?.name || '测试计划详情'"
    >
      <article v-if="currentPlan" class="detail">
        <TTag :theme="statusTheme[currentPlan.status] || 'default'" variant="light">
          {{ planStatusText[currentPlan.status] || currentPlan.status }}
        </TTag>
        <h3>{{ currentPlan.name }}</h3>
        <dl>
          <dt>项目</dt>
          <dd>{{ projectMap[currentPlan.projectId]?.name || currentPlan.projectId }}</dd>
          <dt>需求</dt>
          <dd>{{ requirementMap[currentPlan.requirementId]?.title || currentPlan.requirementId }}</dd>
          <dt>测试环境</dt>
          <dd>{{ currentPlan.environment }}</dd>
          <dt>测试地址</dt>
          <dd>{{ currentPlan.testUrl || '未配置' }}</dd>
          <dt>测试负责人</dt>
          <dd>{{ resolveUserName(currentPlan.createdBy) }}</dd>
          <dt>总结</dt>
          <dd>{{ currentPlan.summary || '暂无' }}</dd>
        </dl>
      </article>

      <section class="execution-history">
        <h3>执行记录</h3>
        <TTable
          row-key="id"
          class="sprint-compact-table"
          :columns="withSerialColumn(executionColumns)"
          :data="executions"
          :loading="executionLoading"
          size="small"
          hover
          stripe
        >
          <template #result="{ row }">
            <TTag :theme="statusTheme[row.result] || 'default'" variant="light">
              {{ executionResultText[row.result] || row.result }}
            </TTag>
          </template>
          <template #testerId="{ row }">
            {{ resolveUserName(row.testerId) }}
          </template>
          <template #createdBugId="{ row }">
            {{ row.createdBugId || row.bugId || '-' }}
          </template>
          <template #executedAt="{ row }">
            {{ formatDateTime(row.executedAt) }}
          </template>
        </TTable>
      </section>
    </TDrawer>
  </div>
</template>

<style scoped>
.tests-page {
  display: flex;
  height: 100%;
  min-height: 0;
  flex-direction: column;
}

.tests-page :deep(.project-context-list-shell) {
  flex: 1;
  min-height: 0;
}

.detail h3,
.execution-history h3 {
  margin: 0;
}

.detail h3 {
  margin-top: 14px;
}

.detail dl {
  display: grid;
  grid-template-columns: 90px minmax(0, 1fr);
  gap: 10px;
  margin-top: 16px;
}

.detail dt {
  color: var(--td-text-color-secondary);
}

.detail dd {
  margin: 0;
}

.execution-history {
  margin-top: 24px;
}

.execution-history h3 {
  margin-bottom: 12px;
}
</style>
