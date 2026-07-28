<script lang="ts" setup>
import type { AutomationApi } from '#/api';
import type { SprintUserApi } from '#/api/sprint/mvp';
import type { PrimaryTableCol } from 'tdesign-vue-next';

import { IconifyIcon } from '@vben/icons';
import { computed, onMounted, reactive, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';

import {
  getDigitalWorkerDetailApi,
  listDigitalWorkersApi,
  listWorkerCommandsApi,
  listWorkerSessionsApi,
  replayWorkerCommandApi,
} from '#/api';
import { listUserOptionsApi } from '#/api/sprint/mvp';
import { confirmAndClose } from '#/views/_shared/dialog-confirm';
import { formatDateTime } from '#/views/_shared/date-format';
import { withSerialColumn } from '#/views/_shared/table-columns';
import RowAction from '#/views/system/_shared/row-action.vue';
import {
  Button as TButton,
  Descriptions as TDescriptions,
  DescriptionsItem as TDescriptionsItem,
  Drawer as TDrawer,
  Empty as TEmpty,
  Input as TInput,
  MessagePlugin,
  Select as TSelect,
  Space as TSpace,
  Table as TTable,
  Tag as TTag,
  TabPanel as TTabPanel,
  Tabs as TTabs,
  Tooltip as TTooltip,
} from 'tdesign-vue-next';

defineOptions({ name: 'AutomationDigitalWorkerCommandAudit' });

const route = useRoute();
const router = useRouter();

const loading = ref(false);
const replayingCommandId = ref('');
const worker = ref<AutomationApi.DigitalWorker>();
const detail = ref<AutomationApi.DigitalWorkerDetail>();
const commands = ref<AutomationApi.WorkerCommand[]>([]);
const sessions = ref<AutomationApi.WorkerSession[]>([]);
const users = ref<SprintUserApi.UserOption[]>([]);
const selectedCommandId = ref('');
const detailVisible = ref(false);
const activeTab = ref('detail');
const filters = reactive({
  commandType: '',
  keyword: '',
  status: '',
});
const query = reactive({ ...filters });

const commandOptions = [
  { label: '烟测', value: 'smoke' },
  { label: '重载配置', value: 'reload_config' },
  { label: '当前任务后停止', value: 'stop_after_current' },
  { label: '取消当前运行', value: 'cancel_current_run' },
  { label: '开始任务', value: 'start_task' },
  { label: '开始缺陷', value: 'start_bug' },
];
const commandStatusOptions = [
  { label: '待领取', value: 'pending' },
  { label: '已确认', value: 'acked' },
  { label: '运行中', value: 'running' },
  { label: '已成功', value: 'succeeded' },
  { label: '已失败', value: 'failed' },
  { label: '已取消', value: 'cancelled' },
  { label: '已过期', value: 'expired' },
];
const sessionStatusOptions = [
  { label: '启动中', value: 'starting' },
  { label: '空闲', value: 'idle' },
  { label: '运行中', value: 'busy' },
  { label: '待认证', value: 'auth_required' },
  { label: '异常', value: 'error' },
  { label: '离线', value: 'offline' },
  { label: '已过期', value: 'expired' },
];
const columns: PrimaryTableCol[] = [
  { colKey: 'gitCommitId', title: 'Git Commit', cell: 'gitCommitId', ellipsis: true, minWidth: 160 },
  { colKey: 'createdBy', title: '创建人', cell: 'createdBy', width: 120 },
  { colKey: 'commandType', title: '命令', cell: 'commandType', width: 140 },
  { colKey: 'status', title: '状态', cell: 'commandStatus', width: 130 },
  { colKey: 'createTime', title: '创建时间', cell: 'createTime', width: 170 },
  { colKey: 'actions', title: '操作', cell: 'actions', fixed: 'right', width: 150 },
];
const orderedColumns: PrimaryTableCol[] = [
  { colKey: 'title', title: 'Title', cell: 'title', ellipsis: true, minWidth: 220 },
  columns.find((item) => item.colKey === 'commandType')!,
  columns.find((item) => item.colKey === 'status')!,
  columns.find((item) => item.colKey === 'gitCommitId')!,
  columns.find((item) => item.colKey === 'createdBy')!,
  columns.find((item) => item.colKey === 'createTime')!,
  columns.find((item) => item.colKey === 'actions')!,
];

const probeColumns: PrimaryTableCol[] = [
  { colKey: 'probeName', title: 'Probe', minWidth: 150 },
  { colKey: 'command', title: 'Command', ellipsis: true, minWidth: 180 },
  { colKey: 'exitCode', title: 'Exit Code', width: 110 },
  { colKey: 'passed', title: 'Result', cell: 'probePassed', width: 110 },
  { colKey: 'required', title: 'Required', cell: 'probeRequired', width: 90 },
  { colKey: 'workerDeployRenderId', title: 'Render ID', cell: 'workerDeployRenderId', ellipsis: true, minWidth: 180 },
  { colKey: 'reportedAt', title: 'Reported At', cell: 'reportedAt', width: 170 },
];

const workerId = computed(() => String(route.params.id || ''));
const routeSessionId = computed(() => String(route.query.sessionId || ''));
const selectedCommand = computed(() => commands.value.find((item) => item.id === selectedCommandId.value));
const selectedSession = computed(() =>
  sessions.value.find((item) => item.id === (selectedCommand.value?.sessionId || routeSessionId.value)),
);
const userMap = computed(() => Object.fromEntries(users.value.map((item) => [item.id, item])));
const userNameMap = computed(() => Object.fromEntries(users.value.map((item) => [item.username, item])));
const filteredCommands = computed(() => {
  const keyword = query.keyword.trim().toLowerCase();
  if (!keyword) {
    return commands.value;
  }

  return commands.value.filter((command) =>
    [
      command.id,
      command.title,
      command.sessionId,
      command.commandType,
      command.status,
      command.createdBy,
      command.payloadJson,
      command.resultJson,
      command.changedFilesJson,
      command.gitCommitId,
      command.error,
    ]
      .filter(Boolean)
      .join('\n')
      .toLowerCase()
      .includes(keyword),
  );
});
const stats = computed(() => ({
  acked: commands.value.filter((item) => item.status === 'acked').length,
  failed: commands.value.filter((item) => ['cancelled', 'expired', 'failed'].includes(item.status)).length,
  pending: commands.value.filter((item) => item.status === 'pending').length,
  succeeded: commands.value.filter((item) => item.status === 'succeeded').length,
  total: commands.value.length,
}));
const startupProbeResults = computed(() => detail.value?.startupProbeResults || []);

function commandText(commandType?: string) {
  return commandOptions.find((item) => item.value === commandType)?.label || commandType || '-';
}

function commandStatusText(status?: string) {
  return commandStatusOptions.find((item) => item.value === status)?.label || status || '-';
}

function commandStatusTheme(status?: string) {
  if (status === 'succeeded') return 'success';
  if (status === 'failed' || status === 'cancelled' || status === 'expired') return 'danger';
  if (status === 'running' || status === 'acked') return 'warning';
  if (status === 'pending') return 'primary';
  return 'default';
}

function sessionStatusText(status?: string) {
  return sessionStatusOptions.find((item) => item.value === status)?.label || status || '-';
}

function resolveUserName(userId?: string) {
  if (!userId) return '-';
  const user = userMap.value[userId] || userNameMap.value[userId];
  return user?.displayName || user?.username || userId;
}

function formatJson(value?: string) {
  if (!value) return '-';
  try {
    return JSON.stringify(JSON.parse(value), null, 2);
  } catch {
    return value;
  }
}

function shortCommit(value?: string) {
  return value ? value.slice(0, 12) : '-';
}

function commandTimeTips(command: AutomationApi.WorkerCommand) {
  return `开始时间: ${formatDateTime(command.startedAt)}\n结束时间: ${formatDateTime(command.completedAt)}`;
}

function probeOutputTips(row: AutomationApi.StartupProbeResult) {
  return [
    row.stdout ? `stdout: ${row.stdout}` : '',
    row.stderr ? `stderr: ${row.stderr}` : '',
    row.error ? `error: ${row.error}` : '',
  ]
    .filter(Boolean)
    .join('\n') || 'no output';
}

async function applyFilters() {
  Object.assign(query, filters);
  await loadCommands();
}

async function resetFilters() {
  Object.assign(filters, { commandType: '', keyword: '', status: '' });
  await applyFilters();
}

async function loadCommands() {
  const rows = await listWorkerCommandsApi({
    workerId: workerId.value,
    commandType: query.commandType,
    status: query.status,
  });
  commands.value = rows;
  if (!rows.some((item) => item.id === selectedCommandId.value)) {
    selectedCommandId.value = rows.find((item) => item.sessionId === routeSessionId.value)?.id || rows[0]?.id || '';
  }
}

async function loadPage() {
  loading.value = true;
  try {
    const [workerRows, detailResult, sessionRows, userRows] = await Promise.all([
      listDigitalWorkersApi(),
      getDigitalWorkerDetailApi(workerId.value),
      listWorkerSessionsApi({ workerId: workerId.value }),
      listUserOptionsApi(),
    ]);
    worker.value = workerRows.find((item) => item.id === workerId.value) || detailResult.worker;
    detail.value = detailResult;
    sessions.value = sessionRows;
    users.value = userRows;
    if (routeSessionId.value && !filters.keyword) {
      filters.keyword = routeSessionId.value;
      query.keyword = routeSessionId.value;
    }
    await loadCommands();
  } finally {
    loading.value = false;
  }
}

function selectCommand(command: AutomationApi.WorkerCommand) {
  selectedCommandId.value = command.id;
  detailVisible.value = true;
}

async function replayCommand(command: AutomationApi.WorkerCommand) {
  if (replayingCommandId.value || command.status === 'succeeded') return;

  confirmAndClose({
    body: `确认回放 ${commandText(command.commandType)} 命令？系统会复制原载荷并创建一条新的待领取命令。`,
    confirmBtn: '回放',
    header: '回放 Worker 命令',
    onConfirm: async () => {
      replayingCommandId.value = command.id;
      try {
        await replayWorkerCommandApi(command.id);
        MessagePlugin.success('命令回放已创建');
        await loadCommands();
      } finally {
        replayingCommandId.value = '';
      }
    },
  });
}

onMounted(loadPage);
</script>

<template>
  <div class="command-audit-page">
    <section class="header">
      <div>
        <h2>{{ worker ? `命令审计 - ${worker.name}` : '命令审计' }}</h2>
        <p>{{ worker ? `${worker.code} / ${worker.workerType}` : '查看数字员工历史命令、执行状态、载荷和回放结果。' }}</p>
      </div>
      <TSpace>
        <TButton variant="outline" @click="router.push('/automation/digital-workers')">
          <template #icon>
            <IconifyIcon icon="lucide:arrow-left" />
          </template>
          返回
        </TButton>
        <TButton :loading="loading" @click="loadPage">
          <template #icon>
            <IconifyIcon icon="lucide:refresh-cw" />
          </template>
          刷新
        </TButton>
      </TSpace>
    </section>

    <TEmpty v-if="!loading && !worker" description="数字员工不存在或已被删除" />

    <template v-else>
      <section class="summary">
        <div>
          <span>全部命令</span>
          <strong>{{ stats.total }}</strong>
        </div>
        <div>
          <span>待领取</span>
          <strong>{{ stats.pending }}</strong>
        </div>
        <div>
          <span>已确认</span>
          <strong>{{ stats.acked }}</strong>
        </div>
        <div>
          <span>已成功</span>
          <strong>{{ stats.succeeded }}</strong>
        </div>
        <div>
          <span>异常结束</span>
          <strong>{{ stats.failed }}</strong>
        </div>
      </section>

      <TTabs v-model="activeTab" class="audit-tabs">
        <TTabPanel value="detail" label="员工详情">
          <section v-if="worker" class="panel">
            <TDescriptions bordered :column="3">
              <TDescriptionsItem label="员工编码">{{ worker.code }}</TDescriptionsItem>
              <TDescriptionsItem label="员工名称">{{ worker.name }}</TDescriptionsItem>
              <TDescriptionsItem label="状态">
                <TTag variant="light">{{ worker.status }}</TTag>
              </TDescriptionsItem>
              <TDescriptionsItem label="模型">{{ worker.codexModel || '-' }}</TDescriptionsItem>
              <TDescriptionsItem label="工作区">{{ worker.workspaceRoot || '-' }}</TDescriptionsItem>
              <TDescriptionsItem label="运行目录">{{ worker.runsRoot || '-' }}</TDescriptionsItem>
              <TDescriptionsItem label="最近会话">
                {{ detail?.latestSession?.instanceId || '-' }}
              </TDescriptionsItem>
              <TDescriptionsItem label="最后心跳">
                {{ formatDateTime(detail?.latestSession?.lastHeartbeatAt) }}
              </TDescriptionsItem>
              <TDescriptionsItem label="待领取命令">{{ detail?.pendingCommands.length || 0 }}</TDescriptionsItem>
            </TDescriptions>
          </section>
        </TTabPanel>

        <TTabPanel value="probe" label="能力探针">
          <section class="panel">
            <div class="panel-title">
              <h3>Startup Probes</h3>
              <span>{{ startupProbeResults.length }}</span>
            </div>
            <TEmpty v-if="startupProbeResults.length === 0" description="No startup probe reports" />
            <TTable
              v-else
              row-key="id"
              :columns="withSerialColumn(probeColumns)"
              :data="startupProbeResults"
              hover
              stripe
            >
              <template #probePassed="{ row }">
                <TTooltip :content="probeOutputTips(row)" placement="top" theme="light">
                  <TTag :theme="row.passed ? 'success' : 'danger'" variant="light">
                    {{ row.passed ? 'Passed' : 'Failed' }}
                  </TTag>
                </TTooltip>
              </template>
              <template #probeRequired="{ row }">
                <TTag :theme="row.required ? 'warning' : 'default'" variant="light">
                  {{ row.required ? 'Required' : 'Optional' }}
                </TTag>
              </template>
              <template #workerDeployRenderId="{ row }">{{ row.workerDeployRenderId || '-' }}</template>
              <template #reportedAt="{ row }">{{ formatDateTime(row.reportedAt) }}</template>
            </TTable>
          </section>


          <section class="panel">
            <div class="filter-bar">
              <TSpace>
                <TSelect
                  v-model="filters.commandType"
                  clearable
                  placeholder="全部命令"
                  :options="commandOptions"
                  class="filter-control"
                />
                <TSelect
                  v-model="filters.status"
                  clearable
                  placeholder="全部状态"
                  :options="commandStatusOptions"
                  class="filter-control"
                />
                <TInput v-model="filters.keyword" clearable placeholder="命令 ID / 会话 / 载荷 / 错误" class="keyword" />
                <TButton theme="primary" :loading="loading" @click="applyFilters">
                  <template #icon>
                    <IconifyIcon icon="lucide:search" />
                  </template>
                  查询
                </TButton>
                <TButton variant="outline" :disabled="loading" @click="resetFilters">
                  <template #icon>
                    <IconifyIcon icon="lucide:rotate-ccw" />
                  </template>
                  重置
                </TButton>
              </TSpace>
            </div>

            <TTable row-key="id" :columns="withSerialColumn(orderedColumns)" :data="filteredCommands" :loading="loading" hover stripe>
              <template #title="{ row }">{{ row.title || '-' }}</template>
              <template #gitCommitId="{ row }">
                <TSpace size="small" align="center">
                  <span>{{ shortCommit(row.gitCommitId) }}</span>
                  <TButton shape="square" variant="text" size="small">
                    <template #icon>
                      <IconifyIcon icon="lucide:eye" />
                    </template>
                  </TButton>
                </TSpace>
              </template>
              <template #createdBy="{ row }">{{ resolveUserName(row.createdBy) }}</template>
              <template #commandType="{ row }">
                <TTag variant="light" theme="primary">{{ commandText(row.commandType) }}</TTag>
              </template>
              <template #commandStatus="{ row }">
                <TTooltip :content="commandTimeTips(row)" placement="top" theme="light">
                  <TTag :theme="commandStatusTheme(row.status)" variant="light">{{ commandStatusText(row.status) }}</TTag>
                </TTooltip>
              </template>
              <template #createTime="{ row }">{{ formatDateTime(row.createTime) }}</template>
              <template #actions="{ row }">
                <TSpace>
                  <RowAction icon="lucide:eye" label="详情" @click="selectCommand(row)" />
                  <TButton
                    variant="text"
                    theme="primary"
                    size="small"
                    :loading="replayingCommandId === row.id"
                    :disabled="row.status === 'succeeded'"
                    @click="replayCommand(row)"
                  >
                    <template #icon>
                      <IconifyIcon icon="lucide:rotate-cw" />
                    </template>
                    回放
                  </TButton>
                </TSpace>
              </template>
            </TTable>
          </section>
        </TTabPanel>
      </TTabs>

      <TDrawer v-model:visible="detailVisible" size="860px" header="命令详情" :footer="false">
        <div class="detail-drawer">
          <TEmpty v-if="!selectedCommand" description="请选择一条命令" />
          <template v-else>
            <TDescriptions bordered :column="2">
              <TDescriptionsItem label="命令 ID">{{ selectedCommand.id }}</TDescriptionsItem>
              <TDescriptionsItem label="Title">{{ selectedCommand.title || '-' }}</TDescriptionsItem>
              <TDescriptionsItem label="命令类型">{{ commandText(selectedCommand.commandType) }}</TDescriptionsItem>
              <TDescriptionsItem label="状态">
                <TTag :theme="commandStatusTheme(selectedCommand.status)" variant="light">
                  {{ commandStatusText(selectedCommand.status) }}
                </TTag>
              </TDescriptionsItem>
              <TDescriptionsItem label="会话 ID">{{ selectedCommand.sessionId || '-' }}</TDescriptionsItem>
              <TDescriptionsItem label="创建人">{{ resolveUserName(selectedCommand.createdBy) }}</TDescriptionsItem>
              <TDescriptionsItem label="创建时间">{{ formatDateTime(selectedCommand.createTime) }}</TDescriptionsItem>
              <TDescriptionsItem label="确认时间">{{ formatDateTime(selectedCommand.ackedAt) }}</TDescriptionsItem>
              <TDescriptionsItem label="过期时间">{{ formatDateTime(selectedCommand.expiresAt) }}</TDescriptionsItem>
              <TDescriptionsItem label="开始时间">{{ formatDateTime(selectedCommand.startedAt) }}</TDescriptionsItem>
              <TDescriptionsItem label="完成时间">{{ formatDateTime(selectedCommand.completedAt) }}</TDescriptionsItem>
            </TDescriptions>

            <div class="text-block">
              <h4>Payload JSON</h4>
              <pre>{{ formatJson(selectedCommand.payloadJson) }}</pre>
            </div>
            <div class="text-block">
              <h4>Result JSON</h4>
              <pre>{{ formatJson(selectedCommand.resultJson) }}</pre>
            </div>
            <div class="text-block">
              <h4>Git Commit</h4>
              <pre>{{ selectedCommand.gitCommitId || '-' }}</pre>
            </div>
            <div class="text-block">
              <h4>Changed Files</h4>
              <pre>{{ formatJson(selectedCommand.changedFilesJson) }}</pre>
            </div>
            <div v-if="selectedCommand.error" class="text-block text-block--error">
              <h4>错误信息</h4>
              <pre>{{ selectedCommand.error }}</pre>
            </div>

            <section class="session-detail">
              <h3>关联会话</h3>
              <TEmpty v-if="!selectedSession" description="命令未绑定会话或会话已清理" />
              <TDescriptions v-else bordered :column="1">
                <TDescriptionsItem label="实例">{{ selectedSession.instanceId }}</TDescriptionsItem>
                <TDescriptionsItem label="状态">{{ sessionStatusText(selectedSession.status) }}</TDescriptionsItem>
                <TDescriptionsItem label="主机">{{ selectedSession.hostName || '-' }}</TDescriptionsItem>
                <TDescriptionsItem label="容器">{{ selectedSession.containerId || '-' }}</TDescriptionsItem>
                <TDescriptionsItem label="Codex">{{ selectedSession.codexVersion || '-' }}</TDescriptionsItem>
                <TDescriptionsItem label="Node">{{ selectedSession.nodeVersion || '-' }}</TDescriptionsItem>
                <TDescriptionsItem label="Git">{{ selectedSession.gitVersion || '-' }}</TDescriptionsItem>
                <TDescriptionsItem label="工作区">{{ selectedSession.workspaceRoot || '-' }}</TDescriptionsItem>
                <TDescriptionsItem label="Codex Home">{{ selectedSession.codexHome || '-' }}</TDescriptionsItem>
                <TDescriptionsItem label="运行目录">{{ selectedSession.runsRoot || '-' }}</TDescriptionsItem>
                <TDescriptionsItem label="启动时间">{{ formatDateTime(selectedSession.startedAt) }}</TDescriptionsItem>
                <TDescriptionsItem label="最后心跳">{{ formatDateTime(selectedSession.lastHeartbeatAt) }}</TDescriptionsItem>
                <TDescriptionsItem label="错误摘要">{{ selectedSession.errorSummary || '-' }}</TDescriptionsItem>
              </TDescriptions>
            </section>
          </template>
        </div>
      </TDrawer>
    </template>
  </div>
</template>

<style scoped>
.command-audit-page {
  padding: 16px;
}

.header,
.panel,
.summary {
  margin-bottom: 16px;
  padding: 16px 20px;
  background: var(--td-bg-color-container);
  border: 1px solid var(--td-component-border);
  border-radius: 6px;
}

.audit-tabs {
  margin-bottom: 16px;
}

.audit-tabs :deep(.t-tab-panel) {
  padding-top: 12px;
}

.audit-tabs .panel {
  margin-bottom: 0;
}

.header {
  display: flex;
  gap: 16px;
  align-items: center;
  justify-content: space-between;
}

.header h2,
.panel h3,
.text-block h4 {
  margin: 0;
}

.header p {
  margin: 6px 0 0;
  color: var(--td-text-color-secondary);
}

.summary {
  display: grid;
  grid-template-columns: repeat(5, minmax(0, 1fr));
  gap: 12px;
}

.summary span {
  display: block;
  color: var(--td-text-color-secondary);
}

.summary strong {
  display: block;
  margin-top: 6px;
  font-size: 24px;
}

.filter-bar {
  margin-bottom: 12px;
}

.filter-control {
  width: 170px;
}

.keyword {
  width: 260px;
}

.detail-drawer,
.session-detail {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.session-detail {
  margin-top: 14px;
}

.text-block {
  margin-top: 14px;
}

.text-block h4 {
  margin-bottom: 8px;
  font-size: 14px;
}

.text-block pre {
  max-height: 360px;
  padding: 12px;
  overflow: auto;
  color: var(--td-text-color-primary);
  white-space: pre-wrap;
  word-break: break-word;
  background: var(--td-bg-color-secondarycontainer);
  border: 1px solid var(--td-component-border);
  border-radius: 6px;
}

.text-block--error pre {
  color: var(--td-error-color);
  background: var(--td-error-color-1);
}

@media (max-width: 1100px) {
  .summary {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 760px) {
  .header,
  .filter-bar :deep(.t-space) {
    align-items: stretch;
    flex-direction: column;
  }

  .filter-control,
  .keyword {
    width: 100%;
  }
}
</style>
