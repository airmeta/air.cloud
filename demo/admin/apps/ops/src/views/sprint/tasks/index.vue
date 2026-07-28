<script lang="ts" setup>
import type { SprintMvpApi, SprintUserApi } from '#/api/sprint/mvp';
import type { FormInstanceFunctions, FormRules } from 'tdesign-vue-next';

import { computed, onActivated, onMounted, reactive, ref } from 'vue';
import { useRouter } from 'vue-router';

import { IconifyIcon } from '@vben/icons';
import { useUserStore } from '@vben/stores';

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
  Tooltip as TTooltip,
} from 'tdesign-vue-next';

import {
  assignDevelopmentTaskApi,
  listDevelopmentTasksApi,
  listProjectsApi,
  listRequirementsApi,
  listUserOptionsApi,
} from '#/api/sprint/mvp';
import { listDigitalWorkersApi, type AutomationApi } from '#/api/automation/workers';
import { formatDateTime } from '#/views/_shared/date-format';
import { requiredRule, validateForm } from '#/views/_shared/form-rules';
import { withSerialColumn } from '#/views/_shared/table-columns';
import ProjectContextListShell from '#/components/project-context-list-shell/project-context-list-shell.vue';

import '../_shared/table-layout.css';

defineOptions({ name: 'SprintTasks' });

const assigning = ref(false);
const loading = ref(false);
const assignVisible = ref(false);
const assignFormRef = ref<FormInstanceFunctions>();
const projects = ref<SprintMvpApi.Project[]>([]);
const requirements = ref<SprintMvpApi.Requirement[]>([]);
const tasks = ref<SprintMvpApi.DevelopmentTask[]>([]);
const users = ref<SprintUserApi.UserOption[]>([]);
const digitalWorkers = ref<AutomationApi.DigitalWorker[]>([]);
const currentTask = ref<SprintMvpApi.DevelopmentTask>();
const initialized = ref(false);
const userStore = useUserStore();
const router = useRouter();

const filters = reactive({
  projectId: '',
  relatedUserId: '',
  requirementKeyword: '',
  status: '',
});
const assignForm = reactive({
  assigneeId: '',
  assigneeType: 0 as 0 | 1,
});
const assignRules: FormRules<typeof assignForm> = {
  assigneeId: requiredRule('请选择研发人员', 'change'),
};
const pagination = reactive({
  current: 1,
  pageSize: 30,
});

const requirementMap = computed(() =>
  Object.fromEntries(requirements.value.map((item) => [item.id, item])),
);
const userOptions = computed(() =>
  users.value.map((user) => ({
    label: `${user.displayName} (${user.username})`,
    value: user.id,
  })),
);
const userMap = computed(() => Object.fromEntries(users.value.map((item) => [item.id, item])));
const digitalWorkerMap = computed(() =>
  Object.fromEntries(digitalWorkers.value.map((item) => [item.agentUserId, item])),
);
const workerOptions = computed(() =>
  digitalWorkers.value
    .filter((worker) => ['active', 'idle', 'working'].includes(worker.status))
    .map((worker) => ({
      label: `${worker.name} (${worker.code})`,
      value: worker.agentUserId,
    })),
);
const assignAssigneeOptions = computed(() =>
  assignForm.assigneeType === 1 ? workerOptions.value : userOptions.value,
);
const relatedUserOptions = computed(() => {
  const optionMap = new Map<string, { label: string; value: string }>();
  for (const user of users.value) {
    optionMap.set(user.id, {
      label: `员工：${user.displayName} (${user.username})`,
      value: user.id,
    });
  }
  for (const worker of digitalWorkers.value) {
    optionMap.set(worker.agentUserId, {
      label: `数字员工：${worker.name} (${worker.code})`,
      value: worker.agentUserId,
    });
  }

  return [...optionMap.values()].sort((left, right) => left.label.localeCompare(right.label, 'zh-CN'));
});
const canAssignTask = computed(() =>
  userStore.userRoles.some((role) =>
    ['architect', 'pm', 'project_manager', 'super'].includes(role),
  ),
);

const columns = [
  { colKey: 'title', title: '任务标题', minWidth: 220 },
  { colKey: 'requirementId', title: '需求', width: 200, cell: 'requirementId' },
  { colKey: 'priority', title: '优先级', cell: 'priority', width: 110 },
  { colKey: 'status', title: '状态', cell: 'status', width: 110 },
  { colKey: 'executorType', title: '执行人类型', cell: 'executorType', width: 120 },
  { colKey: 'executor', title: '执行人', cell: 'executor', width: 150 },
  { colKey: 'createdBy', title: '创建人', cell: 'createdBy', width: 130 },
  { colKey: 'createTime', title: '创建时间', cell: 'createTime', width: 180 },
  { colKey: 'actions', title: '操作', cell: 'actions', width: 160 },
];

const priorityText: Record<number, string> = {
  1: '加急',
  2: '正常',
  3: '可延后',
  4: '低优先级',
  5: '最低优先级',
};
const priorityTheme: Record<number, 'danger' | 'default' | 'primary' | 'success' | 'warning'> = {
  1: 'danger',
  2: 'primary',
  3: 'success',
  4: 'warning',
  5: 'default',
};
const statusText: Record<string, string> = {
  assigned: '已指派',
  completed: '已完成',
  in_progress: '推进中',
  pending_assign: '待指派',
};
const statusOptions = [
  { label: '待指派', value: 'pending_assign' },
  { label: '已指派', value: 'assigned' },
  { label: '推进中', value: 'in_progress' },
  { label: '已完成', value: 'completed' },
];
const tablePagination = computed(() => ({
  current: pagination.current,
  pageSize: pagination.pageSize,
  pageSizeOptions: [30, 50, 100, 200],
  showJumper: true,
  showPageSize: true,
  size: 'small' as const,
  total: tasks.value.length,
}));

function handlePageChange(pageInfo: { current: number; pageSize: number }) {
  pagination.current = pageInfo.current;
  pagination.pageSize = pageInfo.pageSize;
}

function filterTasksByRequirementKeyword(items: SprintMvpApi.DevelopmentTask[]) {
  const keyword = filters.requirementKeyword.trim().toLowerCase();
  if (!keyword) return items;

  return items.filter((task) => {
    const requirement = requirementMap.value[task.requirementId];
    return [requirement?.title, requirement?.id, task.requirementId]
      .filter(Boolean)
      .some((value) => String(value).toLowerCase().includes(keyword));
  });
}

function resolveRequirementTitle(requirementId?: string) {
  return requirementId ? requirementMap.value[requirementId]?.title || requirementId : '-';
}

async function loadBase() {
  [projects.value, requirements.value, users.value, digitalWorkers.value] = await Promise.all([
    listProjectsApi(),
    listRequirementsApi(),
    listUserOptionsApi(),
    listDigitalWorkersApi(),
  ]);
  filters.projectId ||= projects.value[0]?.id || '';
}

async function loadTasks() {
  loading.value = true;
  try {
    const taskItems = await listDevelopmentTasksApi({
      projectId: filters.projectId || undefined,
      relatedUserId: canAssignTask.value ? filters.relatedUserId || undefined : undefined,
      status: filters.status || undefined,
    });
    tasks.value = filterTasksByRequirementKeyword(taskItems);
    pagination.current = 1;
  } finally {
    loading.value = false;
  }
}

async function queryTasks() {
  await loadTasks();
}

async function resetFilters() {
  Object.assign(filters, {
    projectId: projects.value[0]?.id || '',
    relatedUserId: '',
    requirementKeyword: '',
    status: '',
  });
  await loadTasks();
}

async function handleProjectChange() {
  filters.requirementKeyword = '';
  await loadTasks();
}

function openAssign(task: SprintMvpApi.DevelopmentTask) {
  currentTask.value = task;
  assignForm.assigneeId = task.assigneeId || '';
  assignForm.assigneeType = task.assigneeType === 1 ? 1 : 0;
  assignVisible.value = true;
}

function handleAssignTypeChange() {
  assignForm.assigneeId = '';
}

function openDetail(task: SprintMvpApi.DevelopmentTask) {
  router.push(`/sprint/tasks/detail/${task.id}`);
}

function resolveUserName(userId?: string, emptyText = '-') {
  if (!userId) return emptyText;
  const user = userMap.value[userId];
  return user?.displayName || user?.username || userId;
}

function resolveTaskExecutorName(task: SprintMvpApi.DevelopmentTask) {
  if (!task.assigneeId) return '-';
  if (task.assigneeType === 1) {
    const worker = digitalWorkerMap.value[task.assigneeId];
    return worker?.name || task.assigneeId;
  }

  return resolveUserName(task.assigneeId, task.assigneeId);
}

function resolveTaskExecutorType(task: SprintMvpApi.DevelopmentTask) {
  if (!task.assigneeId) return '-';
  return task.assigneeType === 1 ? '数字员工' : '员工';
}

function resolveTaskExecutorTypeTheme(task: SprintMvpApi.DevelopmentTask) {
  if (!task.assigneeId) return 'default';
  return task.assigneeType === 1 ? 'primary' : 'success';
}

function resolvePriorityText(priority: number) {
  return priorityText[priority] || `优先级 ${priority}`;
}

function resolvePriorityTheme(priority: number) {
  return priorityTheme[priority] || 'default';
}

async function assignTask() {
  if (assigning.value) return;
  if (!canAssignTask.value) {
    MessagePlugin.warning('当前角色不能指派任务');
    assignVisible.value = false;
    return;
  }
  if (!currentTask.value || !assignForm.assigneeId.trim()) {
    await validateForm(assignFormRef.value);
    MessagePlugin.warning('负责人不能为空');
    return;
  }
  if (!(await validateForm(assignFormRef.value))) return;

  assigning.value = true;
  try {
    await assignDevelopmentTaskApi(currentTask.value.id, {
      assigneeId: assignForm.assigneeId.trim(),
      assigneeType: assignForm.assigneeType,
    });
    MessagePlugin.success('任务已指派');
    assignVisible.value = false;
    await loadTasks();
  } finally {
    assigning.value = false;
  }
}

async function refreshPage() {
  await loadBase();
  await loadTasks();
}

onMounted(async () => {
  await refreshPage();
  initialized.value = true;
});

onActivated(async () => {
  if (initialized.value) {
    await refreshPage();
  }
});
</script>

<template>
  <ProjectContextListShell
    v-model:selected-project-id="filters.projectId"
    class="tasks-page"
    @project-change="handleProjectChange"
  >
    <template #title>
      任务大厅
    </template>
    <template #description>
      统一管理需求拆解后的任务，并指派给具体研发人员。
    </template>

    <section class="sprint-filter-panel">
      <div class="sprint-filter-grid">
        <label class="sprint-filter-field">
          <span>需求</span>
          <TInput
            v-model="filters.requirementKeyword"
            clearable
            placeholder="输入需求标题或ID"
            @enter="queryTasks"
          />
        </label>
        <label v-if="canAssignTask" class="sprint-filter-field">
          <span>关联人员</span>
          <TSelect
            v-model="filters.relatedUserId"
            clearable
            filterable
            :options="relatedUserOptions"
            placeholder="负责人 / 指派人"
          />
        </label>
        <div v-else class="sprint-filter-field">
          <span>范围</span>
          <TTag theme="primary" variant="light">仅显示我的任务</TTag>
        </div>
        <label class="sprint-filter-field">
          <span>状态</span>
          <TSelect
            v-model="filters.status"
            clearable
            :options="statusOptions"
            placeholder="全部状态"
          />
        </label>
        <div class="sprint-filter-actions">
          <TButton theme="primary" :loading="loading" @click="queryTasks">
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
        <h3>任务列表</h3>
        <div class="sprint-table-actions">
          <TButton shape="circle" variant="outline" title="刷新" :loading="loading" @click="loadTasks">
            <IconifyIcon icon="lucide:refresh-cw" />
          </TButton>
        </div>
      </div>

      <TTable
        row-key="id"
        class="sprint-compact-table"
        :columns="withSerialColumn(columns, { offset: () => (pagination.current - 1) * pagination.pageSize })"
        :data="tasks"
        :loading="loading"
        :pagination="tablePagination"
        size="small"
        hover
        stripe
        @page-change="handlePageChange"
      >
        <template #title="{ row }">
          <div class="task-title-cell">
            <span class="task-title-text">{{ row.title }}</span>
          </div>
        </template>
        <template #requirementId="{ row }">
          <TTooltip
            v-if="resolveRequirementTitle(row.requirementId) !== row.requirementId"
            placement="top"
            theme="light"
          >
            <template #content>{{ resolveRequirementTitle(row.requirementId) }}</template>
            <span class="requirement-text">{{ resolveRequirementTitle(row.requirementId) }}</span>
          </TTooltip>
          <span v-else class="requirement-text">{{ row.requirementId }}</span>
        </template>
        <template #priority="{ row }">
          <TTag size="small" :theme="resolvePriorityTheme(row.priority)" variant="light">
            {{ resolvePriorityText(row.priority) }}
          </TTag>
        </template>
        <template #status="{ row }">
          <TTag variant="light">{{ statusText[row.status] || row.status }}</TTag>
        </template>
        <template #executorType="{ row }">
          <TTag size="small" :theme="resolveTaskExecutorTypeTheme(row)" variant="light">
            {{ resolveTaskExecutorType(row) }}
          </TTag>
        </template>
        <template #executor="{ row }">
          {{ resolveTaskExecutorName(row) }}
        </template>
        <template #createdBy="{ row }">
          {{ resolveUserName(row.createdBy) }}
        </template>
        <template #createTime="{ row }">
          {{ formatDateTime(row.createTime) }}
        </template>
        <template #actions="{ row }">
          <TSpace class="sprint-row-actions">
            <TLink theme="primary" @click="openDetail(row)">
              <IconifyIcon icon="lucide:eye" />
              <span>详情</span>
            </TLink>
            <TLink v-if="canAssignTask" theme="primary" @click="openAssign(row)">
              <IconifyIcon icon="lucide:user-plus" />
              <span>指派</span>
            </TLink>
          </TSpace>
        </template>
      </TTable>
    </section>

    <TDrawer
      v-model:visible="assignVisible"
      :size="'40%'"
      header="任务指派"
      :confirm-btn="{ content: '保存', loading: assigning }"
      @confirm="assignTask"
    >
      <TForm ref="assignFormRef" :data="assignForm" :rules="assignRules" label-width="80px">
        <TFormItem label="指派类型">
          <TSelect
            v-model="assignForm.assigneeType"
            :options="[
              { label: '员工', value: 0 },
              { label: '数字员工', value: 1 },
            ]"
            @change="handleAssignTypeChange"
          />
        </TFormItem>
        <TFormItem :label="assignForm.assigneeType === 1 ? '数字员工' : '研发人员'" name="assigneeId">
          <TSelect
            v-model="assignForm.assigneeId"
            :options="assignAssigneeOptions"
            filterable
            :placeholder="assignForm.assigneeType === 1 ? '选择数字员工' : '选择研发人员'"
          />
        </TFormItem>
      </TForm>
    </TDrawer>
  </ProjectContextListShell>
</template>

<style scoped>
.task-title-cell {
  display: flex;
  min-width: 0;
  align-items: center;
  gap: 6px;
}

.task-title-text {
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.requirement-text {
  display: inline-block;
  max-width: 100%;
  overflow: hidden;
  text-overflow: ellipsis;
  vertical-align: bottom;
  white-space: nowrap;
}
</style>
