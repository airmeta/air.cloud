<script lang="ts" setup>
import type { AutomationApi, SystemApi } from '#/api';
import type { SprintUserApi } from '#/api/sprint/mvp';
import type { FormInstanceFunctions, FormRules, PrimaryTableCol } from 'tdesign-vue-next';

import { IconifyIcon } from '@vben/icons';
import { computed, onMounted, reactive, ref } from 'vue';
import { useRouter } from 'vue-router';

import {
  createDigitalWorkerDeployTemplateApi,
  createDigitalWorkerApi,
  createWorkerCommandApi,
  deleteDigitalWorkerApi,
  generateDigitalWorkerInstallApi,
  listAgentTokensApi,
  listAiPlatformsApi,
  listDigitalWorkerDeployTemplatesApi,
  listDictionaryItemsApi,
  listDictionaryTypesApi,
  listDigitalWorkersApi,
  setDigitalWorkerStatusApi,
  updateDigitalWorkerDeployTemplateApi,
  updateDigitalWorkerApi,
} from '#/api';
import { listUserOptionsApi } from '#/api/sprint/mvp';
import AdminListPage from '#/components/admin-list-page/admin-list-page.vue';
import { formatDateTime } from '#/views/_shared/date-format';
import { requiredRule, validateForm } from '#/views/_shared/form-rules';
import { confirmAndClose } from '#/views/_shared/dialog-confirm';
import RowAction from '#/views/system/_shared/row-action.vue';
import {
  Button as TButton,
  Checkbox as TCheckbox,
  Drawer as TDrawer,
  Form as TForm,
  FormItem as TFormItem,
  Input as TInput,
  MessagePlugin,
  Select as TSelect,
  Space as TSpace,
  Switch as TSwitch,
  TabPanel as TTabPanel,
  Tabs as TTabs,
  Table as TTable,
  Tag as TTag,
  Textarea as TTextarea,
  Tooltip as TTooltip,
} from 'tdesign-vue-next';

defineOptions({ name: 'AutomationDigitalWorkers' });

const router = useRouter();
const loading = ref(false);
const saving = ref(false);
const visible = ref(false);
const installVisible = ref(false);
const installLoading = ref(false);
const templateVisible = ref(false);
const templateSaving = ref(false);
const templateFormRef = ref<FormInstanceFunctions>();
const formRef = ref<FormInstanceFunctions>();
const activeWorkerFormTab = ref('employee');
const installPlainSecret = ref(false);
const installTemplateId = ref('');
const installRender = ref<AutomationApi.DigitalWorkerInstallRender>();
const installWorker = ref<AutomationApi.DigitalWorker>();
const workers = ref<AutomationApi.DigitalWorker[]>([]);
const deployTemplates = ref<AutomationApi.DigitalWorkerDeployTemplate[]>([]);
const users = ref<SprintUserApi.UserOption[]>([]);
const tokens = ref<SystemApi.AgentToken[]>([]);
const aiPlatforms = ref<SystemApi.AiPlatform[]>([]);
const employeeTypeItems = ref<SystemApi.DictionaryItem[]>([]);

const filters = reactive({
  keyword: '',
  status: '',
  workerType: '',
});
const query = reactive({ ...filters });
const pagination = reactive({
  current: 1,
  pageSize: 30,
  pageSizeOptions: [30, 50, 100, 200],
});
const form = reactive<AutomationApi.SaveDigitalWorkerRequest & { aiPlatformCode?: string; id?: string; code?: string }>({
  agentTokenId: '',
  agentUserId: '',
  aiPlatformCode: 'openai',
  code: '',
  description: '',
  employeeType: 'development',
  heartbeatTimeoutSeconds: 90,
  idleMaxIntervalSeconds: 180,
  codexHome: '/codex-home',
  codexModel: 'gpt-5.4',
  codexProvider: 'openai',
  maxConcurrentRuns: 1,
  maxRunMinutes: 60,
  name: '',
  openAiBaseUrl: '',
  pollIntervalSeconds: 15,
  runSmokeOnStartup: false,
  runsRoot: '/runs',
  sandboxMode: 'workspace-write',
  smokePrompt: 'hello',
  status: 'inactive',
  workspaceRoot: '/workspaces',
  workerType: 'codex',
});
const templateForm = reactive<AutomationApi.SaveDigitalWorkerDeployTemplateRequest & { id?: string }>({
  backendTechCapabilities: 'dotnet',
  code: '',
  composeTemplate: '',
  description: '',
  dockerfileExtension: '',
  name: '',
  runtimeProfile: 'dotnet-default',
  sort: 100,
  status: 1,
  version: 1,
});

const rules: FormRules<typeof form> = {
  aiPlatformCode: requiredRule('请选择AI平台', 'change'),
  agentTokenId: requiredRule('请选择 Agent Token', 'change'),
  employeeType: requiredRule('请选择员工类型', 'change'),
  name: requiredRule('请输入员工名称'),
};
const templateRules: FormRules<typeof templateForm> = {
  backendTechCapabilities: requiredRule('请输入后端能力'),
  code: requiredRule('请输入模板编码'),
  composeTemplate: requiredRule('请输入 compose 模板'),
  name: requiredRule('请输入模板名称'),
  runtimeProfile: requiredRule('请输入运行画像'),
};

const statusOptions = [
  { label: '未激活', value: 'inactive' },
  { label: '启动中', value: 'starting' },
  { label: '待命', value: 'idle' },
  { label: '工作中', value: 'working' },
  { label: '维护中', value: 'maintenance' },
  { label: '停用', value: 'disabled' },
];
const driverTypeOptions = [{ label: 'Codex', value: 'codex' }];
const maxConcurrentOptions = Array.from({ length: 10 }, (_, index) => {
  const value = index + 1;
  return { label: `${value}`, value };
});
const heartbeatTimeoutOptions = [30, 60, 90, 120].map((value) => ({ label: `${value} 秒`, value }));
const sandboxModeOptions = [
  { label: 'workspace-write', value: 'workspace-write' },
  { label: 'read-only', value: 'read-only' },
  { label: 'danger-full-access', value: 'danger-full-access' },
];
const fallbackEmployeeTypeItems: SystemApi.DictionaryItem[] = [
  { code: 'operations', dictionaryTypeId: '', id: 'operations', name: '运维', sort: 10, status: 1 },
  { code: 'development', dictionaryTypeId: '', id: 'development', name: '研发', sort: 20, status: 1 },
  { code: 'audit', dictionaryTypeId: '', id: 'audit', name: '审计', sort: 30, status: 1 },
  { code: 'test', dictionaryTypeId: '', id: 'test', name: '测试', sort: 40, status: 1 },
  { code: 'product', dictionaryTypeId: '', id: 'product', name: '产品', sort: 50, status: 1 },
];
const commandOptions = [
  { label: '烟测', value: 'smoke' },
  { label: '重载配置', value: 'reload_config' },
  { label: '当前任务后停止', value: 'stop_after_current' },
  { label: '取消当前运行', value: 'cancel_current_run' },
];
const templateStatusOptions = [
  { label: '启用', value: 1 },
  { label: '停用', value: 0 },
];
const columns: PrimaryTableCol[] = [
  { colKey: 'name', title: '名称', cell: 'name', width: 200 },
  { colKey: 'employeeType', title: '员工类型', cell: 'employeeType', width: 120 },
  { colKey: 'workerType', title: '驱动类型', cell: 'workerType', width: 120 },
  { colKey: 'agentUserId', title: '平台账号', cell: 'agentUserId', width: 150 },
  { colKey: 'runtimeProfile', title: '运行镜像', cell: 'runtimeProfile', width: 150 },
  { colKey: 'backendTechCapabilities', title: '后端能力', cell: 'backendTechCapabilities', width: 160 },
  { colKey: 'latestHeartbeatAt', title: '最近心跳', cell: 'latestHeartbeatAt', width: 170 },
  { colKey: 'runtimeSummary', title: '环境摘要', cell: 'runtimeSummary', minWidth: 220 },
  { colKey: 'runtime', title: '运行策略', cell: 'runtime', width: 150 },
  { colKey: 'status', title: '状态', cell: 'status', width: 100 },
  { colKey: 'updateTime', title: '更新时间', cell: 'updateTime', width: 170 },
  { colKey: 'actions', title: '操作', cell: 'actions', width: 260 },
];
const templateColumns: PrimaryTableCol[] = [
  { colKey: 'code', title: '模板编码', width: 150 },
  { colKey: 'name', title: '模板名称', minWidth: 160 },
  { colKey: 'runtimeProfile', title: '运行画像', width: 140 },
  { colKey: 'backendTechCapabilities', title: '后端能力', width: 160 },
  { colKey: 'version', title: '版本', width: 90 },
  { colKey: 'status', title: '状态', cell: 'templateStatus', width: 90 },
  { colKey: 'actions', title: '操作', cell: 'templateActions', width: 110 },
];

const tablePagination = computed(() => ({
  current: pagination.current,
  pageSize: pagination.pageSize,
  pageSizeOptions: pagination.pageSizeOptions,
  total: workers.value.length,
}));
const activeEmployeeTypeItems = computed(() =>
  (employeeTypeItems.value.length ? employeeTypeItems.value : fallbackEmployeeTypeItems).filter(
    (item) => item.status === 1,
  ),
);
const employeeTypeOptions = computed(() =>
  activeEmployeeTypeItems.value.map((item) => ({ label: item.name, value: item.code })),
);
const tokenOptions = computed(() =>
  tokens.value
    .filter((item) => item.status === 1 && !item.revokedAt)
    .map((item) => ({ label: `${item.name} - ${item.maskedToken}`, value: item.id })),
);
const activeAiPlatforms = computed(() => aiPlatforms.value.filter((item) => item.status === 1));
const aiPlatformOptions = computed(() =>
  activeAiPlatforms.value.map((item) => ({
    label: `${item.name} / ${item.model}`,
    value: item.code,
  })),
);
const deployTemplateOptions = computed(() =>
  deployTemplates.value
    .filter((item) => item.status === 1)
    .map((item) => ({
      label: `${item.name} / ${item.runtimeProfile} / ${item.backendTechCapabilities}`,
      value: item.id,
    })),
);
const userMap = computed(() => Object.fromEntries(users.value.map((item) => [item.id, item])));

function resolveUserName(userId?: string) {
  const user = userId ? userMap.value[userId] : undefined;
  return user ? `${user.displayName || user.username}` : userId || '-';
}

function resetForm(row?: AutomationApi.DigitalWorker) {
  Object.assign(form, {
    agentTokenId: row?.agentTokenId || '',
    agentUserId: row?.agentUserId || '',
    aiPlatformCode: resolveAiPlatformCode(row),
    code: row?.code || '',
    description: row?.description || '',
    employeeType: row?.employeeType || 'development',
    heartbeatTimeoutSeconds: row?.heartbeatTimeoutSeconds || 90,
    idleMaxIntervalSeconds: row?.idleMaxIntervalSeconds || 180,
    codexHome: row?.codexHome || '/codex-home',
    codexModel: row?.codexModel || 'gpt-5.4',
    codexProvider: row?.codexProvider || 'openai',
    id: row?.id,
    maxConcurrentRuns: row?.maxConcurrentRuns || 1,
    maxRunMinutes: row?.maxRunMinutes || 60,
    name: row?.name || '',
    openAiBaseUrl: row?.openAiBaseUrl || '',
    pollIntervalSeconds: row?.pollIntervalSeconds || 15,
    runSmokeOnStartup: row?.runSmokeOnStartup || false,
    runsRoot: row?.runsRoot || '/runs',
    sandboxMode: row?.sandboxMode || 'workspace-write',
    smokePrompt: row?.smokePrompt || 'hello',
    status: row?.status || 'inactive',
    workspaceRoot: row?.workspaceRoot || '/workspaces',
    workerType: row?.workerType || 'codex',
  });
}

function resetTemplateForm(row?: AutomationApi.DigitalWorkerDeployTemplate) {
  Object.assign(templateForm, {
    backendTechCapabilities: row?.backendTechCapabilities || 'dotnet',
    code: row?.code || '',
    composeTemplate: row?.composeTemplate || '',
    description: row?.description || '',
    dockerfileExtension: row?.dockerfileExtension || '',
    id: row?.id,
    name: row?.name || '',
    runtimeProfile: row?.runtimeProfile || 'dotnet-default',
    sort: row?.sort ?? 100,
    status: row?.status ?? 1,
    version: row?.version ?? 1,
  });
}

function createTemplate() {
  resetTemplateForm();
}

function resolveAiPlatformCode(row?: AutomationApi.DigitalWorker) {
  if (!row) {
    return activeAiPlatforms.value[0]?.code || 'openai';
  }

  if (row.aiPlatformCode) {
    return row.aiPlatformCode;
  }

  return activeAiPlatforms.value.find((item) =>
    item.provider === row.codexProvider &&
    item.model === row.codexModel &&
    (item.openAiBaseUrl || '') === (row.openAiBaseUrl || ''),
  )?.code || activeAiPlatforms.value[0]?.code || 'openai';
}

function getSelectedAiPlatform() {
  return activeAiPlatforms.value.find((item) => item.code === form.aiPlatformCode);
}

function statusTheme(status?: string) {
  if (status === 'idle') return 'success';
  if (status === 'starting' || status === 'maintenance') return 'warning';
  if (status === 'working') return 'primary';
  if (status === 'disabled') return 'danger';
  return 'default';
}

function statusText(status?: string) {
  return statusOptions.find((item) => item.value === status)?.label || status || '-';
}

function resolveEnvironmentSummary(row?: AutomationApi.DigitalWorker) {
  if (!row) return ['-'];
  const lines: string[] = [];
  lines.push(`驱动：${row.workerType || '-'}`);
  lines.push(`AI：${row.codexProvider || '-'} / ${row.codexModel || '-'}`);
  lines.push(`沙箱：${row.sandboxMode || '-'}`);
  lines.push(`工作区：${row.workspaceRoot || '-'}`);
  lines.push(`Codex Home：${row.codexHome || '-'}`);
  return lines;
}

function resolveBackendCapabilities(row?: AutomationApi.DigitalWorker) {
  if (!row) return ['-'];
  const lines: string[] = [];
  lines.push(`项目：${row.projectIds?.length ? row.projectIds.join('、') : '未配置'}`);
  lines.push(`端：${row.endpointIds?.length ? row.endpointIds.join('、') : '未配置'}`);
  lines.push(`技能：${row.skillIds?.length ? row.skillIds.join('、') : '未配置'}`);
  return lines;
}

function sessionStatusText(status?: string) {
  if (!status) return '-';
  const map: Record<string, string> = {
    auth_required: '待登录',
    busy: '忙碌',
    error: '异常',
    expired: '已过期',
    idle: '空闲',
    offline: '离线',
    starting: '启动中',
  };
  return map[status] || status;
}

function templateStatusTheme(status?: number) {
  return status === 1 ? 'success' : 'default';
}

function availableStatusActions(row: AutomationApi.DigitalWorker) {
  if (row.status === 'idle' || row.status === 'working') {
    return [{ icon: 'lucide:pause-circle', label: '维护', status: 'maintenance' as const, theme: 'warning' as const }];
  }

  if (row.status === 'maintenance') {
    return [
      { icon: 'lucide:check-circle-2', label: '恢复', status: 'idle' as const, theme: 'success' as const },
      { icon: 'lucide:power', label: '停用', status: 'disabled' as const, theme: 'danger' as const },
    ];
  }

  return [];
}

function resolveEmployeeTypeName(type?: string) {
  return activeEmployeeTypeItems.value.find((item) => item.code === type)?.name || type || '-';
}

function resolveDriverTypeName(type?: string) {
  return driverTypeOptions.find((item) => item.value === type)?.label || type || '-';
}

function commandText(commandType?: string) {
  return commandOptions.find((item) => item.value === commandType)?.label || commandType || '-';
}

function openCreate() {
  resetForm();
  activeWorkerFormTab.value = 'employee';
  visible.value = true;
}

function openEdit(row: AutomationApi.DigitalWorker) {
  resetForm(row);
  activeWorkerFormTab.value = 'employee';
  visible.value = true;
}

async function openDetail(row: AutomationApi.DigitalWorker) {
  await router.push(`/automation/digital-workers/${row.id}/command-audit`);
}

async function openInstall(row: AutomationApi.DigitalWorker) {
  installWorker.value = row;
  installTemplateId.value = deployTemplateOptions.value[0]?.value || '';
  installVisible.value = true;
  await generateInstall();
}

async function generateInstall() {
  if (!installWorker.value) return;
  installLoading.value = true;
  try {
    installRender.value = await generateDigitalWorkerInstallApi(installWorker.value.id, {
      plainSecretEnabled: installPlainSecret.value,
      templateId: installTemplateId.value || undefined,
    });
  } finally {
    installLoading.value = false;
  }
}

function openTemplates() {
  resetTemplateForm();
  templateVisible.value = true;
}

function editTemplate(row: AutomationApi.DigitalWorkerDeployTemplate) {
  resetTemplateForm(row);
}

async function saveTemplate() {
  if (templateSaving.value) return;
  if (!(await validateForm(templateFormRef.value))) return;

  templateSaving.value = true;
  try {
    const payload = {
      backendTechCapabilities: templateForm.backendTechCapabilities.trim(),
      code: templateForm.code.trim(),
      composeTemplate: templateForm.composeTemplate,
      description: templateForm.description?.trim() || undefined,
      dockerfileExtension: templateForm.dockerfileExtension?.trim() || undefined,
      name: templateForm.name.trim(),
      runtimeProfile: templateForm.runtimeProfile.trim(),
      sort: Number(templateForm.sort ?? 100),
      status: Number(templateForm.status ?? 1),
      version: Number(templateForm.version ?? 1),
    };
    if (templateForm.id) {
      await updateDigitalWorkerDeployTemplateApi(templateForm.id, payload);
    } else {
      await createDigitalWorkerDeployTemplateApi(payload);
    }
    MessagePlugin.success('部署模板已保存');
    await loadDeployTemplates();
    resetTemplateForm();
  } finally {
    templateSaving.value = false;
  }
}

async function copyText(content?: string) {
  if (!content) return;
  if (navigator.clipboard?.writeText) {
    await navigator.clipboard.writeText(content);
  } else {
    const textarea = document.createElement('textarea');
    textarea.value = content;
    textarea.style.position = 'fixed';
    textarea.style.opacity = '0';
    document.body.append(textarea);
    textarea.select();
    document.execCommand('copy');
    textarea.remove();
  }
  MessagePlugin.success('内容已复制');
}

function downloadText(filename: string, content?: string) {
  if (!content) return;
  const blob = new Blob([content], { type: 'text/plain;charset=utf-8' });
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = filename;
  document.body.append(anchor);
  anchor.click();
  anchor.remove();
  URL.revokeObjectURL(url);
}

function buildPayload() {
  const selectedToken = tokens.value.find((item) => item.id === form.agentTokenId);
  const selectedAiPlatform = getSelectedAiPlatform();
  return {
    agentTokenId: form.agentTokenId,
    agentUserId: selectedToken?.ownerUserId || form.agentUserId,
    aiPlatformCode: selectedAiPlatform?.code || form.aiPlatformCode || 'openai',
    code: form.id ? form.code?.trim() : undefined,
    description: form.description?.trim() || undefined,
    employeeType: form.employeeType,
    endpointIds: [],
    heartbeatTimeoutSeconds: Number(form.heartbeatTimeoutSeconds || 90),
    idleMaxIntervalSeconds: Number(form.idleMaxIntervalSeconds || 180),
    codexHome: form.codexHome?.trim() || '/codex-home',
    codexModel: selectedAiPlatform?.model || form.codexModel?.trim() || 'gpt-5.4',
    codexProvider: selectedAiPlatform?.provider || form.codexProvider?.trim() || 'openai',
    maxConcurrentRuns: Number(form.maxConcurrentRuns || 1),
    maxRunMinutes: Number(form.maxRunMinutes || 60),
    name: form.name.trim(),
    openAiBaseUrl: selectedAiPlatform?.openAiBaseUrl || form.openAiBaseUrl?.trim() || undefined,
    pollIntervalSeconds: Number(form.pollIntervalSeconds || 15),
    projectIds: [],
    runSmokeOnStartup: Boolean(form.runSmokeOnStartup),
    runsRoot: form.runsRoot?.trim() || '/runs',
    sandboxMode: form.sandboxMode || 'workspace-write',
    status: form.status,
    smokePrompt: form.smokePrompt?.trim() || undefined,
    workspaceRoot: form.workspaceRoot?.trim() || '/workspaces',
    workerType: form.workerType || 'codex',
  };
}

async function save() {
  if (saving.value) return;
  if (!(await validateForm(formRef.value))) return;

  saving.value = true;
  try {
    const payload = buildPayload();
    if (form.id) {
      await updateDigitalWorkerApi(form.id, payload);
    } else {
      await createDigitalWorkerApi(payload);
    }
    MessagePlugin.success('数字员工已保存');
    visible.value = false;
    await load();
  } finally {
    saving.value = false;
  }
}

function setStatus(row: AutomationApi.DigitalWorker, status: AutomationApi.WorkerStatus) {
  confirmAndClose({
    body: `确认将 ${row.name} 设置为 ${statusText(status)}？`,
    confirmBtn: '确认',
    header: '调整员工状态',
    onConfirm: async () => {
      await setDigitalWorkerStatusApi(row.id, status);
      MessagePlugin.success('状态已更新');
      await load();
    },
  });
}

function deleteWorker(row: AutomationApi.DigitalWorker) {
  confirmAndClose({
    body: `确认删除 ${row.name}？删除只会软删除员工主档，历史审计记录会保留。`,
    confirmBtn: '删除',
    header: '删除数字员工',
    onConfirm: async () => {
      await deleteDigitalWorkerApi(row.id);
      MessagePlugin.success('数字员工已删除');
      await load();
    },
  });
}

function sendCommand(row: AutomationApi.DigitalWorker, commandType: AutomationApi.WorkerCommandType) {
  confirmAndClose({
    body: `确认向 ${row.name} 下发 ${commandText(commandType)} 命令？命令会在下一次心跳时被受控端领取。`,
    confirmBtn: '下发',
    header: '下发 Worker 命令',
    onConfirm: async () => {
      await createWorkerCommandApi({ commandType, title: row.name, workerId: row.id });
      MessagePlugin.success('命令已下发');
      await openDetail(row);
    },
  });
}

async function applyFilters() {
  Object.assign(query, filters);
  pagination.current = 1;
  await load();
}

async function resetFilters() {
  Object.assign(filters, { keyword: '', status: '', workerType: '' });
  await applyFilters();
}

function handlePageChange(pageInfo: { current: number; pageSize: number }) {
  pagination.current = pageInfo.current;
  pagination.pageSize = pageInfo.pageSize;
}

async function loadReferences() {
  const [userRows, tokenRows, aiPlatformRows, dictionaryTypeRows] = await Promise.allSettled([
    listUserOptionsApi(),
    listAgentTokensApi(),
    listAiPlatformsApi({ status: 1 }),
    listDictionaryTypesApi(),
  ]);
  users.value = userRows.status === 'fulfilled' ? userRows.value : [];
  tokens.value = tokenRows.status === 'fulfilled' ? tokenRows.value : [];
  aiPlatforms.value = aiPlatformRows.status === 'fulfilled' ? aiPlatformRows.value : [];
  const dictionaryTypes = dictionaryTypeRows.status === 'fulfilled' ? dictionaryTypeRows.value : [];
  const employeeType = dictionaryTypes.find((item) => item.code === 'digital_worker_employee_type');
  employeeTypeItems.value = employeeType ? await listDictionaryItemsApi(employeeType.id) : fallbackEmployeeTypeItems;
}

async function loadDeployTemplates() {
  deployTemplates.value = await listDigitalWorkerDeployTemplatesApi();
}

async function load() {
  loading.value = true;
  try {
    workers.value = await listDigitalWorkersApi(query);
  } finally {
    loading.value = false;
  }
}

onMounted(async () => {
  await Promise.all([loadReferences(), loadDeployTemplates(), load()]);
});
</script>

<template>
  <AdminListPage
    title="员工实例管理"
    description="维护通过 AgentSprint.Worker 注册和执行的数字员工，记录员工类型、驱动类型和运行策略，将环境摘要与后端能力展示在名称提示中。"
    table-title="数字员工列表"
    add-button-text="新增数字员工"
    :columns="columns"
    :data="workers"
    :loading="loading"
    :pagination="tablePagination"
    @add="openCreate"
    @page-change="handlePageChange"
    @refresh="load"
    @reset="resetFilters"
    @search="applyFilters"
  >
    <template #filters>
      <label class="filter-field">
        <span>员工信息</span>
        <TInput v-model="filters.keyword" clearable placeholder="编码 / 名称 / 描述" />
      </label>
      <label class="filter-field">
        <span>驱动类型</span>
        <TSelect v-model="filters.workerType" clearable placeholder="全部驱动" :options="driverTypeOptions" />
      </label>
      <label class="filter-field">
        <span>状态</span>
        <TSelect v-model="filters.status" clearable placeholder="全部状态" :options="statusOptions" />
      </label>
    </template>

    <template #name="{ row }">
      <TTooltip placement="top" theme="light">
        <template #content>
          <div class="worker-name-tip">
            <div class="worker-name-tip-section">
              <strong>环境摘要</strong>
              <p v-for="line in resolveEnvironmentSummary(row)" :key="line">{{ line }}</p>
            </div>
            <div class="worker-name-tip-section">
              <strong>后端能力</strong>
              <p v-for="line in resolveBackendCapabilities(row)" :key="line">{{ line }}</p>
            </div>
          </div>
        </template>
        <span class="worker-name-cell">{{ row.name }}</span>
      </TTooltip>
    </template>
    <template #toolbar>
      <TButton variant="outline" @click="openTemplates">
        <template #icon><IconifyIcon icon="lucide:blocks" /></template>
        部署模板
      </TButton>
    </template>

    <template #employeeType="{ row }">{{ resolveEmployeeTypeName(row.employeeType) }}</template>
    <template #workerType="{ row }">{{ resolveDriverTypeName(row.workerType) }}</template>
    <template #agentUserId="{ row }">{{ resolveUserName(row.agentUserId) }}</template>
    <template #runtimeProfile="{ row }">{{ row.runtimeProfile || '-' }}</template>
    <template #backendTechCapabilities="{ row }">{{ row.backendTechCapabilities || '-' }}</template>
    <template #latestHeartbeatAt="{ row }">
      <div class="heartbeat-cell">
        <span>{{ formatDateTime(row.latestHeartbeatAt) }}</span>
        <TTag v-if="row.latestSessionStatus" size="small" variant="light">
          {{ sessionStatusText(row.latestSessionStatus) }}
        </TTag>
      </div>
    </template>
    <template #runtimeSummary="{ row }">{{ row.runtimeSummary || '-' }}</template>
    <template #runtime="{ row }">
      {{ row.maxConcurrentRuns }} 并发 / {{ row.heartbeatTimeoutSeconds }} 秒心跳
    </template>
    <template #status="{ row }">
      <TTag :theme="statusTheme(row.status)" variant="light">{{ statusText(row.status) }}</TTag>
    </template>
    <template #updateTime="{ row }">
      {{ formatDateTime(row.updateTime || row.createTime) }}
    </template>
    <template #actions="{ row }">
      <TSpace>
        <RowAction icon="lucide:download" label="安装" @click="openInstall(row)" />
        <RowAction icon="lucide:history" label="审计" @click="openDetail(row)" />
        <RowAction label="编辑" @click="openEdit(row)" />
        <RowAction v-if="row.status === 'idle'" icon="lucide:rotate-cw" label="烟测" @click="sendCommand(row, 'smoke')" />
        <RowAction
          v-for="action in availableStatusActions(row)"
          :key="action.status"
          :icon="action.icon"
          :label="action.label"
          :theme="action.theme"
          @click="setStatus(row, action.status)"
        />
        <RowAction
          v-if="row.status === 'disabled'"
          icon="lucide:trash-2"
          label="删除"
          theme="danger"
          @click="deleteWorker(row)"
        />
      </TSpace>
    </template>
  </AdminListPage>

  <TDrawer
    v-model:visible="visible"
    size="780px"
    :header="form.id ? '编辑数字员工' : '新增数字员工'"
    :confirm-btn="{ content: '保存', loading: saving }"
    @confirm="save"
  >
    <TForm ref="formRef" class="worker-form" :data="form" :rules="rules" label-width="120px">
      <TTabs v-model="activeWorkerFormTab" :destroy-on-hide="false">
        <TTabPanel value="employee" label="员工信息">
          <div class="tab-form-body">
            <div class="form-grid form-grid--single">
              <TFormItem label="员工名称" name="name">
                <TInput v-model="form.name" placeholder="Codex worker" />
              </TFormItem>
            </div>
            <div class="form-grid form-grid--single">
              <TFormItem label="Agent Token" name="agentTokenId">
                <TSelect v-model="form.agentTokenId" filterable placeholder="请选择 Agent Token" :options="tokenOptions" />
              </TFormItem>
            </div>
            <div class="form-grid">
              <TFormItem label="员工类型" name="employeeType">
                <TSelect v-model="form.employeeType" :options="employeeTypeOptions" />
              </TFormItem>
              <TFormItem label="AI平台" name="aiPlatformCode">
                <TSelect
                  v-model="form.aiPlatformCode"
                  filterable
                  placeholder="请选择AI平台"
                  :options="aiPlatformOptions"
                />
              </TFormItem>
            </div>
            <div class="form-grid">
              <TFormItem label="状态">
                <TSelect v-model="form.status" :options="statusOptions" />
              </TFormItem>
            </div>
            <TFormItem label="说明">
              <TTextarea v-model="form.description" :autosize="{ minRows: 3, maxRows: 5 }" placeholder="记录部署位置、用途或接单边界" />
            </TFormItem>
          </div>
        </TTabPanel>

        <TTabPanel value="work" label="工作选项">
          <div class="tab-form-body">
            <div class="form-grid">
              <TFormItem label="启动烟测">
                <TSwitch v-model="form.runSmokeOnStartup" />
              </TFormItem>
              <TFormItem label="烟测提示词">
                <TInput v-model="form.smokePrompt" placeholder="你好" />
              </TFormItem>
            </div>
            <div class="form-grid">
              <TFormItem label="最大并发" name="maxConcurrentRuns">
                <TSelect v-model="form.maxConcurrentRuns" :options="maxConcurrentOptions" />
              </TFormItem>
              <TFormItem label="心跳超时" name="heartbeatTimeoutSeconds">
                <TSelect v-model="form.heartbeatTimeoutSeconds" :options="heartbeatTimeoutOptions" />
              </TFormItem>
            </div>
            <div class="form-grid">
              <TFormItem label="轮询间隔">
                <TInput v-model="form.pollIntervalSeconds" type="number" placeholder="15" />
              </TFormItem>
              <TFormItem label="最大空闲间隔">
                <TInput v-model="form.idleMaxIntervalSeconds" type="number" placeholder="180" />
              </TFormItem>
            </div>
            <div class="form-grid">
              <TFormItem label="最大运行分钟">
                <TInput v-model="form.maxRunMinutes" type="number" placeholder="60" />
              </TFormItem>
              <TFormItem label="沙箱模式">
                <TSelect v-model="form.sandboxMode" :options="sandboxModeOptions" />
              </TFormItem>
            </div>
          </div>
        </TTabPanel>

        <TTabPanel value="advanced" label="高级选项">
          <div class="tab-form-body">
            <div class="form-grid">
              <TFormItem label="驱动类型">
                <TSelect v-model="form.workerType" :options="driverTypeOptions" />
              </TFormItem>
              <TFormItem label="工作区根目录">
                <TInput v-model="form.workspaceRoot" placeholder="/workspaces" />
              </TFormItem>
            </div>
            <div class="form-grid">
              <TFormItem label="运行日志目录">
                <TInput v-model="form.runsRoot" placeholder="/runs" />
              </TFormItem>
              <TFormItem label="Codex Home">
                <TInput v-model="form.codexHome" placeholder="/codex-home" />
              </TFormItem>
            </div>
          </div>
        </TTabPanel>
      </TTabs>
    </TForm>
  </TDrawer>

  <TDrawer
    v-model:visible="installVisible"
    size="860px"
    :header="`安装配置 - ${installWorker?.name || ''}`"
    :footer="false"
  >
    <div class="install-panel">
      <div class="install-toolbar">
        <TCheckbox v-model="installPlainSecret" @change="generateInstall">明文输出</TCheckbox>
        <TSelect
          v-model="installTemplateId"
          class="install-template-select"
          filterable
          placeholder="选择部署模板"
          :options="deployTemplateOptions"
          @change="generateInstall"
        />
        <TButton :loading="installLoading" variant="outline" @click="generateInstall">重新生成</TButton>
      </div>
      <div v-if="installRender" class="install-meta">
        <span>生成记录：{{ installRender.id }}</span>
        <span>模板版本：{{ installRender.templateVersion }}</span>
      </div>
      <div class="install-section">
        <div class="install-section__header">
          <div class="install-section__title">docker-compose.yml</div>
          <TSpace>
            <TButton variant="text" @click="copyText(installRender?.renderedCompose)">
              <template #icon><IconifyIcon icon="lucide:copy" /></template>
              复制
            </TButton>
            <TButton variant="text" @click="downloadText('docker-compose.yml', installRender?.renderedCompose)">
              <template #icon><IconifyIcon icon="lucide:download" /></template>
              下载
            </TButton>
          </TSpace>
        </div>
        <TTextarea
          readonly
          :value="installRender?.renderedCompose || ''"
          :autosize="{ minRows: 14, maxRows: 24 }"
        />
      </div>
      <div v-if="installRender?.renderedEnv" class="install-section">
        <div class="install-section__header">
          <div class="install-section__title">.env</div>
          <TSpace>
            <TButton variant="text" @click="copyText(installRender.renderedEnv)">
              <template #icon><IconifyIcon icon="lucide:copy" /></template>
              复制
            </TButton>
            <TButton variant="text" @click="downloadText('.env', installRender.renderedEnv)">
              <template #icon><IconifyIcon icon="lucide:download" /></template>
              下载
            </TButton>
          </TSpace>
        </div>
        <TTextarea readonly :value="installRender.renderedEnv" :autosize="{ minRows: 3, maxRows: 8 }" />
      </div>
    </div>
  </TDrawer>

  <TDrawer
    v-model:visible="templateVisible"
    size="1040px"
    header="部署模板管理"
    :footer="false"
  >
    <div class="template-workbench">
      <div class="template-list">
        <div class="template-toolbar">
          <strong>模板列表</strong>
          <TSpace>
            <TButton variant="outline" @click="loadDeployTemplates">
              <template #icon><IconifyIcon icon="lucide:refresh-cw" /></template>
              刷新
            </TButton>
            <TButton theme="primary" @click="createTemplate">
              <template #icon><IconifyIcon icon="lucide:plus" /></template>
              新增
            </TButton>
          </TSpace>
        </div>
        <TTable row-key="id" :columns="templateColumns" :data="deployTemplates" hover>
          <template #templateStatus="{ row }">
            <TTag :theme="templateStatusTheme(row.status)" variant="light">
              {{ row.status === 1 ? '启用' : '停用' }}
            </TTag>
          </template>
          <template #templateActions="{ row }">
            <RowAction icon="lucide:edit" label="编辑" @click="editTemplate(row)" />
          </template>
        </TTable>
      </div>

      <TForm ref="templateFormRef" class="template-form" :data="templateForm" :rules="templateRules" label-width="110px">
        <div class="template-form__title">{{ templateForm.id ? '编辑模板' : '新增模板' }}</div>
        <div class="form-grid">
          <TFormItem label="模板编码" name="code">
            <TInput v-model="templateForm.code" placeholder="java17" />
          </TFormItem>
          <TFormItem label="模板名称" name="name">
            <TInput v-model="templateForm.name" placeholder="Java 17 worker" />
          </TFormItem>
        </div>
        <div class="form-grid">
          <TFormItem label="运行画像" name="runtimeProfile">
            <TInput v-model="templateForm.runtimeProfile" placeholder="java17" />
          </TFormItem>
          <TFormItem label="后端能力" name="backendTechCapabilities">
            <TInput v-model="templateForm.backendTechCapabilities" placeholder="dotnet,java" />
          </TFormItem>
        </div>
        <div class="form-grid">
          <TFormItem label="版本">
            <TInput v-model="templateForm.version" type="number" placeholder="1" />
          </TFormItem>
          <TFormItem label="状态">
            <TSelect v-model="templateForm.status" :options="templateStatusOptions" />
          </TFormItem>
        </div>
        <TFormItem label="说明">
          <TTextarea v-model="templateForm.description" :autosize="{ minRows: 2, maxRows: 4 }" />
        </TFormItem>
        <TFormItem label="Compose" name="composeTemplate">
          <TTextarea v-model="templateForm.composeTemplate" :autosize="{ minRows: 12, maxRows: 20 }" />
        </TFormItem>
        <TFormItem label="Dockerfile扩展">
          <TTextarea v-model="templateForm.dockerfileExtension" :autosize="{ minRows: 4, maxRows: 8 }" />
        </TFormItem>
        <div class="template-form__actions">
          <TButton theme="primary" :loading="templateSaving" @click="saveTemplate">
            <template #icon><IconifyIcon icon="lucide:save" /></template>
            保存模板
          </TButton>
        </div>
      </TForm>
    </div>
  </TDrawer>
</template>

<style scoped>
.worker-name-cell {
  cursor: help;
  border-bottom: 1px dashed var(--td-component-stroke);
}

.worker-name-tip {
  display: grid;
  gap: 8px;
  max-width: 320px;
  line-height: 1.6;
}

.worker-name-tip-section {
  display: grid;
  gap: 2px;
}

.worker-name-tip-section strong {
  color: var(--td-text-color-primary);
}

.worker-name-tip-section p {
  margin: 0;
  color: var(--td-text-color-secondary);
  word-break: break-all;
}

.filter-field {
  display: grid;
  grid-template-columns: auto minmax(180px, 260px);
  gap: 8px;
  align-items: center;
  color: var(--td-text-color-secondary);
}

.form-grid,
.detail-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
}

.form-grid--single {
  grid-template-columns: 1fr;
}

.worker-form {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.worker-form :deep(.t-form__item) {
  margin-bottom: 0;
}

.tab-form-body {
  display: flex;
  flex-direction: column;
  gap: 14px;
  padding-top: 14px;
}

.install-panel {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.install-toolbar,
.install-meta {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
  align-items: center;
}

.install-meta {
  color: var(--td-text-color-secondary);
}

.install-template-select {
  min-width: 280px;
}

.heartbeat-cell {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  align-items: center;
}

.install-section {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.install-section__header {
  display: flex;
  gap: 12px;
  align-items: center;
  justify-content: space-between;
}

.install-section__title {
  font-weight: 600;
  color: var(--td-text-color-primary);
}

.template-workbench {
  display: grid;
  grid-template-columns: minmax(360px, 0.9fr) minmax(0, 1.1fr);
  gap: 16px;
}

.template-list,
.template-form {
  min-width: 0;
}

.template-toolbar,
.template-form__actions {
  display: flex;
  gap: 12px;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 12px;
}

.template-form {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.template-form :deep(.t-form__item) {
  margin-bottom: 0;
}

.template-form__title {
  font-weight: 600;
  color: var(--td-text-color-primary);
}

@media (max-width: 760px) {
  .filter-field,
  .form-grid,
  .detail-grid,
  .template-workbench {
    grid-template-columns: 1fr;
    width: 100%;
  }
}
</style>
