<script lang="ts" setup>
import type { SprintMvpApi, SprintUserApi } from '#/api/sprint/mvp';
import type { PrimaryTableCol } from 'tdesign-vue-next';
import type { TableRowData } from 'tdesign-vue-next';
import type { HTMLElementAttributes } from 'tdesign-vue-next';

import { CircleAlert, IconifyIcon } from '@vben/icons';

import { computed, onMounted, reactive, ref, watch } from 'vue';
import { useRouter } from 'vue-router';

import {
  Button as TButton,
  Dialog as TDialog,
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
  Tooltip as TTooltip,
} from 'tdesign-vue-next';

import { listAgentTokensApi, type SystemApi } from '#/api';
import { formatDateTime } from '#/views/_shared/date-format';
import { withSerialColumn } from '#/views/_shared/table-columns';
import ProjectContextListShell from '#/components/project-context-list-shell/project-context-list-shell.vue';
import {
  completeDevelopmentTaskApi,
  getDevelopmentTaskPromptApi,
  listMyDevelopmentTasksApi,
  listProjectsApi,
  listRequirementsApi,
  listUserOptionsApi,
} from '#/api/sprint/mvp';

import '../_shared/table-layout.css';
import {
  buildAgentsprintMcpSetupPrompt,
  buildAgentsprintMcpSetupPromptWithToken,
} from '../_shared/mcpSetupPrompt';

defineOptions({ name: 'SprintMyTasks' });

const loading = ref(false);
const router = useRouter();
const promptVisible = ref(false);
const mcpCopyVisible = ref(false);
const promptResult = ref<SprintMvpApi.TaskPrompt>();
const mcpSetupContent = ref('');
const agentTokens = ref<SystemApi.AgentToken[]>([]);
const selectedAgentTokenId = ref('');
const projects = ref<SprintMvpApi.Project[]>([]);
const requirements = ref<SprintMvpApi.Requirement[]>([]);
const tasks = ref<SprintMvpApi.DevelopmentTask[]>([]);
const users = ref<SprintUserApi.UserOption[]>([]);
const currentTask = ref<SprintMvpApi.DevelopmentTask>();
const selectedTaskKeys = ref<Array<number | string>>([]);
const filters = reactive({
  projectId: '',
  requirementKeyword: '',
  status: '',
});
const pagination = reactive({
  current: 1,
  pageSize: 30,
});

const projectMap = computed(() => Object.fromEntries(projects.value.map((item) => [item.id, item])));
const requirementMap = computed(() =>
  Object.fromEntries(requirements.value.map((item) => [item.id, item])),
);
const userMap = computed(() => Object.fromEntries(users.value.map((item) => [item.id, item])));
const validAgentTokens = computed(() =>
  agentTokens.value.filter(
    (token) =>
      token.status === 1 && !token.revokedAt && new Date(token.expiresAt).getTime() > Date.now(),
  ),
);
const agentTokenOptions = computed(() =>
  validAgentTokens.value.map((token) => ({
    label: `${token.name} (${token.maskedToken})`,
    value: token.id,
  })),
);
const columns: PrimaryTableCol[] = [
  {
    colKey: 'row-select',
    disabled: ({ row }: { row: TableRowData }) => isTaskCompleted(row as SprintMvpApi.DevelopmentTask),
    type: 'single',
    width: 48,
  },
  { colKey: 'title', title: '任务标题', minWidth: 220 },
  { colKey: 'requirementId', title: '需求', width: 200, cell: 'requirementId' },
  { colKey: 'priority', title: '优先级', cell: 'priority', width: 100 },
  { colKey: 'status', title: '状态', cell: 'status', width: 110 },
  { colKey: 'executorType', title: '执行人类型', cell: 'executorType', width: 120 },
  { colKey: 'executor', title: '执行人', cell: 'executor', width: 150 },
  { colKey: 'createdBy', title: '创建人', cell: 'createdBy', width: 130 },
  { colKey: 'createTime', title: '创建时间', cell: 'createTime', width: 180 },
  { colKey: 'actions', title: '操作', cell: 'actions', width: 110 },
];

const priorityText: Record<number, string> = {
  1: '加急',
  2: '正常',
  3: '可延后',
  4: '低优先级',
  5: '最低优先级',
};
const priorityTheme: Record<number, 'default' | 'primary' | 'success' | 'warning'> = {
  1: 'warning',
  2: 'primary',
  3: 'success',
  4: 'default',
  5: 'default',
};
const statusText: Record<string, string> = {
  assigned: '已指派',
  completed: '已完成',
  in_progress: '推进中',
  pending_assign: '待指派',
};
const statusOptions = [
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
const displayedMcpSetupPrompt = computed(() =>
  buildAgentsprintMcpSetupPrompt(promptResult.value?.mcpSetupPrompt.content),
);
const selectedTask = computed(() =>
  tasks.value.find((task) => task.id === selectedTaskKeys.value[0]),
);
const canOperateSelectedTask = computed(() =>
  Boolean(selectedTask.value && selectedTask.value.status !== 'completed'),
);

watch(tasks, (items) => {
  if (
    selectedTaskKeys.value.length > 0 &&
    !items.some((task) => task.id === selectedTaskKeys.value[0])
  ) {
    selectedTaskKeys.value = [];
  }
});

function handlePageChange(pageInfo: { current: number; pageSize: number }) {
  pagination.current = pageInfo.current;
  pagination.pageSize = pageInfo.pageSize;
}

function handleSelectChange(keys: Array<number | string>) {
  selectedTaskKeys.value = keys.filter((id) => {
    const task = tasks.value.find((item) => item.id === id);
    return task && !isTaskCompleted(task);
  });
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

function isTaskCompleted(task: SprintMvpApi.DevelopmentTask) {
  return task.status === 'completed';
}

function resolveTaskRowAttributes({ row }: { row: TableRowData }): HTMLElementAttributes {
  return isTaskCompleted(row as SprintMvpApi.DevelopmentTask)
    ? { title: '当前任务已完成,无需处理' }
    : {};
}

function resolveTaskRowClassName({ row }: { row: TableRowData }) {
  return isTaskCompleted(row as SprintMvpApi.DevelopmentTask) ? 'task-row-completed' : '';
}

async function loadTasks() {
  loading.value = true;
  try {
    const [projectItems, requirementItems, userItems, taskItems, agentTokenItems] = await Promise.all([
      listProjectsApi(),
      listRequirementsApi(),
      listUserOptionsApi(),
      listMyDevelopmentTasksApi({
        projectId: filters.projectId || undefined,
        status: filters.status || undefined,
      }),
      listAgentTokensApi(),
    ]);
    projects.value = projectItems;
    requirements.value = requirementItems;
    users.value = userItems;
    tasks.value = filterTasksByRequirementKeyword(taskItems);
    agentTokens.value = agentTokenItems;
    filters.projectId ||= projects.value[0]?.id || '';
    selectedTaskKeys.value = selectedTaskKeys.value.filter((id) =>
      tasks.value.some((task) => task.id === id && !isTaskCompleted(task)),
    );
    pagination.current = 1;
  } finally {
    loading.value = false;
  }
}

async function handleProjectChange() {
  filters.requirementKeyword = '';
  selectedTaskKeys.value = [];
  pagination.current = 1;
  await loadTasks();
}

async function handleFilterChange() {
  selectedTaskKeys.value = [];
  pagination.current = 1;
  await loadTasks();
}

async function queryTasks() {
  pagination.current = 1;
  await loadTasks();
}

async function resetFilters() {
  Object.assign(filters, {
    projectId: projects.value[0]?.id || '',
    requirementKeyword: '',
    status: '',
  });
  selectedTaskKeys.value = [];
  pagination.current = 1;
  await loadTasks();
}

function goTaskHall() {
  router.push('/sprint/tasks');
}

async function advanceTask(task: SprintMvpApi.DevelopmentTask) {
  if (task.status === 'completed') return;
  const result = await getDevelopmentTaskPromptApi(task.id);
  promptResult.value = result;
  currentTask.value = task;
  promptVisible.value = true;
  await loadTasks();
}

function openDetail(task: SprintMvpApi.DevelopmentTask) {
  router.push(`/sprint/tasks/detail/${task.id}`);
}

async function completeTask(task: SprintMvpApi.DevelopmentTask) {
  if (task.status === 'completed') return;
  await completeDevelopmentTaskApi(task.id);
  MessagePlugin.success('任务已完成；同一需求下全部任务完成后需求进入待测试');
  await loadTasks();
}

async function advanceSelectedTask() {
  if (!selectedTask.value || !canOperateSelectedTask.value) return;
  await advanceTask(selectedTask.value);
}

async function completeSelectedTask() {
  if (!selectedTask.value || !canOperateSelectedTask.value) return;
  await completeTask(selectedTask.value);
}

function resolvePriorityText(priority: number) {
  return priorityText[priority] || `优先级 ${priority}`;
}

function resolvePriorityTheme(priority: number) {
  return priorityTheme[priority] || 'default';
}

function resolveExecutorName(task: SprintMvpApi.DevelopmentTask) {
  if (!task.assigneeId) return '-';
  const user = userMap.value[task.assigneeId];
  return user?.displayName || user?.username || task.assigneeId;
}

function resolveCreatedByName(task: SprintMvpApi.DevelopmentTask) {
  if (!task.createdBy) return '-';
  const user = userMap.value[task.createdBy];
  return user?.displayName || user?.username || task.createdBy;
}

function fallbackCopyText(content: string) {
  const textarea = document.createElement('textarea');
  textarea.value = content;
  textarea.style.position = 'fixed';
  textarea.style.left = '-9999px';
  textarea.style.top = '0';
  textarea.setAttribute('readonly', 'readonly');
  document.body.append(textarea);
  textarea.select();
  textarea.setSelectionRange(0, textarea.value.length);
  try {
    return document.execCommand('copy');
  } finally {
    textarea.remove();
  }
}

async function copyPrompt(content: string) {
  try {
    if (navigator.clipboard?.writeText) {
      await navigator.clipboard.writeText(content);
    } else if (!fallbackCopyText(content)) {
      throw new Error('Clipboard fallback failed');
    }
    MessagePlugin.success('写入剪切板成功');
    return true;
  } catch {
    if (fallbackCopyText(content)) {
      MessagePlugin.success('写入剪切板成功');
      return true;
    }
    MessagePlugin.error('写入剪切板失败，请手动复制文本框内容');
    return false;
  }
}

async function openMcpCopy() {
  if (!currentTask.value) return;
  selectedAgentTokenId.value = validAgentTokens.value[0]?.id || '';
  await refreshMcpSetupPrompt();
  mcpCopyVisible.value = true;
}

function selectedAgentToken() {
  return validAgentTokens.value.find((token) => token.id === selectedAgentTokenId.value);
}

async function refreshMcpSetupPrompt() {
  if (!currentTask.value || !selectedAgentTokenId.value) {
    mcpSetupContent.value = '';
    return;
  }

  const result = await getDevelopmentTaskPromptApi(currentTask.value.id);
  promptResult.value = result;
  const token = selectedAgentToken();
  const bearerToken = `Bearer ${token?.token || token?.maskedToken || ''}`;
  mcpSetupContent.value = buildAgentsprintMcpSetupPromptWithToken(
    result.mcpSetupPrompt.content,
    bearerToken,
  );
}

async function handleAgentTokenChange() {
  await refreshMcpSetupPrompt();
}

async function copyMcpSetupPrompt() {
  if (!selectedAgentTokenId.value) {
    MessagePlugin.warning('请选择一个有效令牌');
    return;
  }
  if (!mcpSetupContent.value) {
    await refreshMcpSetupPrompt();
  }
  const copied = await copyPrompt(mcpSetupContent.value);
  if (copied) {
    mcpCopyVisible.value = false;
  }
}

onMounted(loadTasks);
</script>

<template>
  <ProjectContextListShell
    v-model:selected-project-id="filters.projectId"
    class="my-tasks-page"
    @project-change="handleProjectChange"
  >
    <template #title>
      我的任务
    </template>
    <template #description>
      当前账号作为研发负责人被指派的需求拆解任务，不包含指派给数字员工的协作任务。
    </template>

    <section class="sprint-filter-panel">
      <div class="sprint-filter-grid">
        <div class="sprint-filter-field">
          <span>需求</span>
          <TInput
            v-model="filters.requirementKeyword"
            clearable
            placeholder="输入需求标题或ID"
            @enter="queryTasks"
          />
        </div>
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
          <TButton theme="primary" @click="goTaskHall">
            <template #icon>
              <IconifyIcon icon="lucide:handshake" />
            </template>
            接取任务
          </TButton>
          <TButton
            theme="primary"
            :disabled="!canOperateSelectedTask"
            @click="advanceSelectedTask"
          >
            <template #icon>
              <IconifyIcon icon="lucide:play" />
            </template>
            任务推进
          </TButton>
          <TButton
            theme="primary"
            :disabled="!canOperateSelectedTask"
            @click="completeSelectedTask"
          >
            <template #icon>
              <IconifyIcon icon="lucide:check" />
            </template>
            完成
          </TButton>
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
        :row-attributes="resolveTaskRowAttributes"
        :row-class-name="resolveTaskRowClassName"
        :selected-row-keys="selectedTaskKeys"
        row-selection-type="single"
        select-on-row-click
        size="small"
        hover
        stripe
        @page-change="handlePageChange"
        @select-change="handleSelectChange"
      >
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
          <TTag :theme="resolvePriorityTheme(row.priority)" variant="light">
            {{ resolvePriorityText(row.priority) }}
          </TTag>
        </template>
        <template #status="{ row }">
          <TTag variant="light">{{ statusText[row.status] || row.status }}</TTag>
        </template>
        <template #executorType>
          <TTag size="small" theme="success" variant="light">员工</TTag>
        </template>
        <template #executor="{ row }">
          {{ resolveExecutorName(row) }}
        </template>
        <template #createdBy="{ row }">
          {{ resolveCreatedByName(row) }}
        </template>
        <template #createTime="{ row }">
          {{ formatDateTime(row.createTime) }}
        </template>
        <template #actions="{ row }">
          <TSpace class="sprint-row-actions">
            <TLink theme="primary" class="task-detail-link" @click="openDetail(row)">
              <IconifyIcon icon="lucide:search" />
              <span>详情</span>
            </TLink>
          </TSpace>
        </template>
      </TTable>
    </section>

    <TDrawer v-model:visible="promptVisible" :size="'60%'" header="任务推进" :footer="false">
      <section v-if="promptResult" class="prompt-drawer">
        <header class="prompt-summary">
          <div>
            <h3>{{ currentTask?.title || '任务推进' }}</h3>
            <p>
              {{ projectMap[currentTask?.projectId || '']?.name || currentTask?.projectId }}
              · 任务 ID：{{ promptResult.taskId }}
            </p>
          </div>
        </header>

        <section class="prompt-section">
          <div class="prompt-section-header">
            <div>
              <h4>{{ promptResult.mcpSetupPrompt.title }}</h4>
            </div>
            <TButton theme="primary" @click="openMcpCopy">
              <template #icon>
                <IconifyIcon icon="lucide:settings" />
              </template>
              初次接入配置
            </TButton>
          </div>
          <p class="prompt-usage">
            <strong>首次接入</strong>或本地 Codex 未发现
            <strong>AgentSprint MCP</strong> 时复制执行；默认只配置 MCP endpoint 和
            <strong>Authorization</strong>，不要默认写入
            <strong>X-AgentSprint-Api-Base-Url</strong>。
          </p>
          <div class="prompt-content">
            {{ displayedMcpSetupPrompt }}
          </div>
        </section>

        <section class="prompt-section">
          <div class="prompt-section-header">
            <div>
              <div class="prompt-title-line">
                <h4>{{ promptResult.taskExecutionPrompt.title }}</h4>
                <TTooltip placement="top" theme="light">
                  <template #content>
                    <ul class="prompt-note-list">
                      <li v-for="item in promptResult.taskExecutionPrompt.notes" :key="item">
                        {{ item }}
                      </li>
                    </ul>
                  </template>
                  <CircleAlert class="prompt-note-icon" />
                </TTooltip>
              </div>
            </div>
            <TButton theme="primary" @click="copyPrompt(promptResult.taskExecutionPrompt.content)">
              <template #icon>
                <IconifyIcon icon="lucide:copy" />
              </template>
              任务推进提示词
            </TButton>
          </div>
          <p class="prompt-usage">
            <strong>日常推进</strong>只复制此段，Codex 会按
            <strong>任务 ID</strong> 通过 <strong>MCP</strong> 拉取任务、需求和 Skill 上下文，并按
            <strong>next_work</strong> 仅作为状态参考，完成当前任务后停止接取新任务。
          </p>
          <div class="prompt-content">
            {{ promptResult.taskExecutionPrompt.content }}
          </div>
        </section>
      </section>
    </TDrawer>

    <TDialog
      v-model:visible="mcpCopyVisible"
      header="初次接入配置"
      width="620px"
      confirm-btn="写入剪切板"
      @confirm="copyMcpSetupPrompt"
    >
      <section class="mcp-copy-dialog">
        <p>
          选择一个有效令牌后会重新拉取初次接入提示词，并只替换 Authorization
          令牌；默认不写入 X-AgentSprint-Api-Base-Url。只有用户明确提供远程 MCP
          服务可访问的 AgentSprint API 地址时，才追加该可选覆盖配置，不要固定写入
          http://localhost:5000。
        </p>
        <TForm label-width="112px">
          <TFormItem label="令牌记录">
            <TSelect
              v-model="selectedAgentTokenId"
              filterable
              :options="agentTokenOptions"
              placeholder="请选择令牌"
              @change="handleAgentTokenChange"
            />
          </TFormItem>
        </TForm>
        <TTextarea :model-value="mcpSetupContent" readonly autosize />
      </section>
    </TDialog>
  </ProjectContextListShell>
</template>

<style scoped>
.requirement-text {
  display: inline-block;
  max-width: 100%;
  overflow: hidden;
  text-overflow: ellipsis;
  vertical-align: bottom;
  white-space: nowrap;
}

.prompt-drawer {
  display: grid;
  gap: 16px;
}

.prompt-summary,
.prompt-section-header {
  display: flex;
  gap: 16px;
  align-items: flex-start;
  justify-content: space-between;
}

.prompt-summary {
  padding-bottom: 12px;
  border-bottom: 1px solid var(--td-component-border);
}

.prompt-summary h3,
.prompt-section h4 {
  margin: 0;
}

.prompt-summary p {
  margin: 6px 0 0;
  color: var(--td-text-color-secondary);
}

.prompt-section {
  display: grid;
  gap: 12px;
  padding: 16px;
  border: 1px solid var(--td-component-border);
  border-radius: 6px;
}

.prompt-title-line {
  display: flex;
  gap: 6px;
  align-items: center;
}

.prompt-note-icon {
  width: 16px;
  height: 16px;
  color: var(--td-warning-color);
  cursor: help;
}

.prompt-note-list {
  max-width: 320px;
  padding-left: 18px;
  margin: 0;
  line-height: 1.7;
}

.prompt-note-list li + li {
  margin-top: 4px;
}

.prompt-usage {
  margin: 0;
  color: var(--td-text-color-secondary);
  line-height: 1.7;
}

.prompt-content {
  max-height: 360px;
  min-height: 220px;
  padding: 12px 14px;
  overflow: auto;
  font-size: 13px;
  font-family: ui-monospace, SFMono-Regular, Consolas, 'Liberation Mono', monospace;
  line-height: 1.7;
  white-space: pre-wrap;
  word-break: break-word;
  background: var(--td-bg-color-secondarycontainer);
  border: 1px solid var(--td-component-border);
  border-radius: 6px;
}

.mcp-copy-dialog {
  display: grid;
  gap: 12px;
}

.mcp-copy-dialog p {
  margin: 0;
  color: var(--td-text-color-secondary);
  line-height: 1.7;
}

:deep(.task-row-completed) {
  color: var(--td-text-color-disabled);
  cursor: not-allowed;
}

:deep(.task-row-completed .t-checkbox),
:deep(.task-row-completed .t-radio) {
  cursor: not-allowed;
}

.task-detail-link {
  display: inline-flex;
  gap: 4px;
  align-items: center;
}

.task-detail-link .iconify {
  width: 14px;
  height: 14px;
}
</style>
