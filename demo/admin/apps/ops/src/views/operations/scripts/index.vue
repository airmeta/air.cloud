<script lang="ts" setup>
import type { SprintMvpApi } from '#/api/sprint/mvp';
import type { SystemApi } from '#/api';
import type { FormInstanceFunctions, FormRules, PrimaryTableCol } from 'tdesign-vue-next';

import { computed, onMounted, reactive, ref, watch } from 'vue';

import {
  listDictionaryItemsApi,
  listDictionaryTypesApi,
  listRuntimeEnvironmentContainersApi,
  listRuntimeEnvironmentsApi,
  saveRuntimeEnvironmentContainerApi,
} from '#/api';
import {
  listFeatureModulesApi,
  listProjectEndpointsApi,
  listProjectsApi,
} from '#/api/sprint/mvp';
import AdminListPage from '#/components/admin-list-page/admin-list-page.vue';
import { confirmAndClose } from '#/views/_shared/dialog-confirm';
import {
  optionalNumberRule,
  requiredRule,
  validateForm,
} from '#/views/_shared/form-rules';
import RowAction from '#/views/system/_shared/row-action.vue';
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

defineOptions({ name: 'OperationScripts' });

type ScriptState = '' | 'configured' | 'missing';

type EnvironmentCriteria = {
  endpointId?: string;
  moduleId?: string;
  projectId?: string;
  runtimeEnvironmentId?: string;
};

type ScriptRow = SystemApi.RuntimeEnvironmentContainer & {
  environment: SystemApi.RuntimeEnvironment;
};

const fallbackContainerTypes = [
  { label: 'Docker', value: 0 },
  { label: 'K3S', value: 1 },
  { label: 'K8S', value: 2 },
  { label: 'Tomcat', value: 3 },
  { label: 'Nginx', value: 4 },
  { label: 'Other', value: 9 },
];

const loading = ref(false);
const saving = ref(false);
const visible = ref(false);
const formRef = ref<FormInstanceFunctions>();
const projects = ref<SprintMvpApi.Project[]>([]);
const endpoints = ref<SprintMvpApi.ProjectEndpoint[]>([]);
const modules = ref<SprintMvpApi.FeatureModule[]>([]);
const environmentPool = ref<SystemApi.RuntimeEnvironment[]>([]);
const containerTypeItems = ref<SystemApi.DictionaryItem[]>([]);
const scriptRows = ref<ScriptRow[]>([]);

const filters = reactive({
  endpointId: '',
  keyword: '',
  moduleId: '',
  projectId: '',
  runtimeEnvironmentId: '',
  scriptState: '' as ScriptState,
  status: undefined as number | undefined,
});
const query = reactive({ ...filters });
const pagination = reactive({
  current: 1,
  pageSize: 30,
  pageSizeOptions: [30, 50, 100, 200],
});
const form = reactive<Partial<SystemApi.RuntimeEnvironmentContainer>>({
  containerPort: 80,
  containerType: 0,
  deployScript: '',
  description: '',
  hostPort: 80,
  id: undefined,
  name: '',
  prompt: '',
  protocol: 'tcp',
  runtimeEnvironmentId: '',
  serverIp: '',
  sort: 10,
  status: 1,
});

const rules: FormRules<typeof form> = {
  containerPort: optionalNumberRule('请输入有效的容器端口'),
  deployScript: requiredRule('请输入部署脚本'),
  hostPort: optionalNumberRule('请输入有效的主机端口'),
  name: requiredRule('请输入脚本服务名称'),
  runtimeEnvironmentId: requiredRule('请选择运行环境', 'change'),
  serverIp: requiredRule('请选择服务机器 IP', 'change'),
  sort: optionalNumberRule('排序必须是数字'),
};

const statusOptions = [
  { label: '启用', value: 1 },
  { label: '停用', value: 0 },
];
const scriptStateOptions = [
  { label: '已配置脚本', value: 'configured' },
  { label: '未配置脚本', value: 'missing' },
];
const protocolOptions = [
  { label: 'TCP', value: 'tcp' },
  { label: 'UDP', value: 'udp' },
];
const columns: PrimaryTableCol[] = [
  { colKey: 'environment', title: '运行环境', cell: 'environment', width: 220 },
  { colKey: 'name', title: '脚本服务', width: 180 },
  { colKey: 'containerType', title: '服务类型', cell: 'containerType', width: 110 },
  { colKey: 'serverIp', title: '机器 IP', width: 140 },
  { colKey: 'ports', title: '端口映射', cell: 'ports', width: 130 },
  { colKey: 'deployScript', title: '部署脚本', cell: 'deployScript', ellipsis: true },
  { colKey: 'status', title: '状态', cell: 'status', width: 90 },
  { colKey: 'actions', title: '操作', cell: 'actions', width: 150 },
];

const projectOptions = computed(() =>
  projects.value.map((item) => ({ label: `${item.name} (${item.code})`, value: item.id })),
);
const endpointOptions = computed(() =>
  endpoints.value
    .filter((item) => item.projectId === filters.projectId)
    .map((item) => ({ label: `${item.name} (${item.code})`, value: item.id })),
);
const moduleOptions = computed(() =>
  modules.value
    .filter((item) => item.projectId === filters.projectId)
    .filter((item) => item.endpointId === filters.endpointId)
    .map((item) => ({ label: `${item.name} (${item.code})`, value: item.id })),
);
const runtimeEnvironmentOptions = computed(() =>
  environmentPool.value
    .filter((item) => matchesEnvironment(item, filters))
    .map((item) => ({ label: environmentLabel(item), value: item.id })),
);
const formEnvironmentOptions = computed(() =>
  environmentPool.value.map((item) => ({ label: environmentLabel(item), value: item.id })),
);
const containerTypeOptions = computed(() => {
  const configured = containerTypeItems.value
    .filter((item) => item.status === 1)
    .sort((left, right) => left.sort - right.sort)
    .map((item) => ({ label: item.name, value: Number(item.code) }));
  return configured.length > 0 ? configured : fallbackContainerTypes;
});
const projectMap = computed(() => Object.fromEntries(projects.value.map((item) => [item.id, item])));
const endpointMap = computed(() => Object.fromEntries(endpoints.value.map((item) => [item.id, item])));
const moduleMap = computed(() => Object.fromEntries(modules.value.map((item) => [item.id, item])));
const selectedFormEnvironment = computed(() =>
  environmentPool.value.find((item) => item.id === form.runtimeEnvironmentId),
);
const formServerIpOptions = computed(() =>
  parseServerIps(selectedFormEnvironment.value?.serverIps).map((ip) => ({ label: ip, value: ip })),
);
const filteredRows = computed(() => {
  const keyword = query.keyword.trim().toLowerCase();
  return scriptRows.value.filter((row) => {
    if (query.status !== undefined && row.status !== query.status) {
      return false;
    }
    if (query.scriptState === 'configured' && !row.deployScript?.trim()) {
      return false;
    }
    if (query.scriptState === 'missing' && row.deployScript?.trim()) {
      return false;
    }
    if (!keyword) {
      return true;
    }

    return [
      row.name,
      row.description,
      row.prompt,
      row.deployScript,
      row.serverIp,
      row.environment.code,
      row.environment.name,
      resolveScope(row.environment),
    ]
      .filter(Boolean)
      .join('\n')
      .toLowerCase()
      .includes(keyword);
  });
});
const tablePagination = computed(() => ({
  current: pagination.current,
  pageSize: pagination.pageSize,
  pageSizeOptions: pagination.pageSizeOptions,
  total: filteredRows.value.length,
}));

watch(
  () => filters.projectId,
  () => {
    filters.endpointId = '';
    filters.moduleId = '';
    filters.runtimeEnvironmentId = '';
  },
);
watch(
  () => filters.endpointId,
  () => {
    filters.moduleId = '';
    filters.runtimeEnvironmentId = '';
  },
);
watch(
  () => filters.moduleId,
  () => {
    filters.runtimeEnvironmentId = '';
  },
);
watch(
  () => form.runtimeEnvironmentId,
  () => {
    const options = formServerIpOptions.value;
    if (form.serverIp && options.some((item) => item.value === form.serverIp)) {
      return;
    }

    form.serverIp = options[0]?.value || '';
  },
);

function parseServerIps(value?: string) {
  return (value || '')
    .split(/[,;\n\r]+/)
    .map((item) => item.trim())
    .filter(Boolean);
}

function matchesEnvironment(environment: SystemApi.RuntimeEnvironment, criteria: EnvironmentCriteria) {
  if (criteria.runtimeEnvironmentId && environment.id !== criteria.runtimeEnvironmentId) {
    return false;
  }
  if (criteria.projectId && environment.projectId !== criteria.projectId) {
    return false;
  }
  if (criteria.endpointId && environment.endpointId !== criteria.endpointId) {
    return false;
  }
  return !(criteria.moduleId && environment.moduleId !== criteria.moduleId);
}

function environmentLabel(environment: SystemApi.RuntimeEnvironment) {
  return `${environment.name} (${environment.code})`;
}

function resolveScope(environment: SystemApi.RuntimeEnvironment) {
  const scope = [
    environment.projectId ? projectMap.value[environment.projectId]?.name || environment.projectId : '全局',
    environment.endpointId ? endpointMap.value[environment.endpointId]?.name || environment.endpointId : '',
    environment.moduleId ? moduleMap.value[environment.moduleId]?.name || environment.moduleId : '',
  ].filter(Boolean);
  return scope.join(' / ');
}

function resolveContainerType(value?: number) {
  return containerTypeOptions.value.find((item) => item.value === value)?.label || String(value ?? '-');
}

function firstScriptLine(value?: string) {
  const lines = (value || '')
    .split(/\r?\n/)
    .map((item) => item.trim())
    .filter(Boolean);
  return lines[0] || '';
}

function currentEnvironmentCandidates() {
  return environmentPool.value.filter((item) => matchesEnvironment(item, filters));
}

function resetForm(environment?: SystemApi.RuntimeEnvironment) {
  const firstIp = parseServerIps(environment?.serverIps)[0] || '';
  Object.assign(form, {
    containerPort: 80,
    containerType: 0,
    deployScript: '',
    description: '',
    hostPort: 80,
    id: undefined,
    name: '',
    prompt: '',
    protocol: 'tcp',
    runtimeEnvironmentId: environment?.id || '',
    serverIp: firstIp,
    sort: 10,
    status: 1,
  });
}

function openCreate() {
  const environment = currentEnvironmentCandidates()[0];
  if (!environment) {
    MessagePlugin.warning('请先在环境配置中创建运行环境');
    return;
  }

  resetForm(environment);
  visible.value = true;
}

function openEdit(row: ScriptRow) {
  Object.assign(form, {
    containerPort: row.containerPort,
    containerType: row.containerType,
    deployScript: row.deployScript || '',
    description: row.description || '',
    hostPort: row.hostPort,
    id: row.id,
    name: row.name,
    prompt: row.prompt || '',
    protocol: row.protocol,
    runtimeEnvironmentId: row.runtimeEnvironmentId,
    serverIp: row.serverIp || '',
    sort: row.sort,
    status: row.status,
  });
  visible.value = true;
}

function buildPayload(source: Partial<SystemApi.RuntimeEnvironmentContainer>) {
  return {
    containerPort: Number(source.containerPort || 0),
    containerType: Number(source.containerType ?? 0),
    deployScript: source.deployScript || undefined,
    description: source.description || undefined,
    hostPort: Number(source.hostPort || 0),
    id: source.id,
    name: source.name || '',
    prompt: source.prompt || undefined,
    protocol: source.protocol || 'tcp',
    runtimeEnvironmentId: source.runtimeEnvironmentId || '',
    serverIp: source.serverIp || undefined,
    sort: Number(source.sort || 0),
    status: Number(source.status ?? 1),
  };
}

async function loadReferences() {
  const [projectList, endpointList, moduleList, dictionaryTypes] = await Promise.all([
    listProjectsApi(),
    listProjectEndpointsApi(),
    listFeatureModulesApi(),
    listDictionaryTypesApi(),
  ]);
  projects.value = projectList;
  endpoints.value = endpointList;
  modules.value = moduleList;
  const containerType = dictionaryTypes.find((item) => item.code === 'runtime_container_type');
  containerTypeItems.value = containerType ? await listDictionaryItemsApi(containerType.id) : [];
}

async function loadRows() {
  loading.value = true;
  try {
    environmentPool.value = await listRuntimeEnvironmentsApi();
    const environments = environmentPool.value.filter((item) => matchesEnvironment(item, query));
    const groups = await Promise.all(
      environments.map(async (environment) => ({
        environment,
        services: await listRuntimeEnvironmentContainersApi(environment.id),
      })),
    );
    scriptRows.value = groups
      .flatMap(({ environment, services }) => services.map((service) => ({ ...service, environment })))
      .sort((left, right) =>
        left.environment.sort === right.environment.sort
          ? left.sort - right.sort
          : left.environment.sort - right.environment.sort,
      );
  } finally {
    loading.value = false;
  }
}

async function applyFilters() {
  Object.assign(query, filters);
  pagination.current = 1;
  await loadRows();
}

async function resetFilters() {
  Object.assign(filters, {
    endpointId: '',
    keyword: '',
    moduleId: '',
    projectId: '',
    runtimeEnvironmentId: '',
    scriptState: '',
    status: undefined,
  });
  await applyFilters();
}

function handlePageChange(pageInfo: { current: number; pageSize: number }) {
  pagination.current = pageInfo.current;
  pagination.pageSize = pageInfo.pageSize;
}

async function save() {
  if (saving.value) return;
  if (!(await validateForm(formRef.value))) return;

  saving.value = true;
  try {
    await saveRuntimeEnvironmentContainerApi(buildPayload(form));
    MessagePlugin.success('部署脚本已保存');
    visible.value = false;
    await loadRows();
  } finally {
    saving.value = false;
  }
}

function clearScript(row: ScriptRow) {
  if (!row.deployScript?.trim()) {
    MessagePlugin.info('当前服务尚未配置部署脚本');
    return;
  }

  confirmAndClose({
    body: `确认清空 ${row.name} 的部署脚本？服务本身会保留。`,
    confirmBtn: '清空',
    header: '清空部署脚本',
    onConfirm: async () => {
      await saveRuntimeEnvironmentContainerApi(buildPayload({ ...row, deployScript: '' }));
      MessagePlugin.success('部署脚本已清空');
      await loadRows();
    },
  });
}

onMounted(async () => {
  await loadReferences();
  await loadRows();
});
</script>

<template>
  <AdminListPage
    title="脚本管理"
    description="集中维护运行环境服务的部署脚本，便于运维发布时按环境和服务快速定位。"
    table-title="部署脚本列表"
    add-button-text="新增脚本"
    :columns="columns"
    :data="filteredRows"
    :loading="loading"
    :pagination="tablePagination"
    @add="openCreate"
    @page-change="handlePageChange"
    @refresh="loadRows"
    @reset="resetFilters"
    @search="applyFilters"
  >
    <template #filters>
      <label class="filter-field">
        <span>项目</span>
        <TSelect v-model="filters.projectId" clearable filterable placeholder="全部项目" :options="projectOptions" />
      </label>
      <label class="filter-field">
        <span>端</span>
        <TSelect
          v-model="filters.endpointId"
          clearable
          filterable
          placeholder="全部端"
          :disabled="!filters.projectId"
          :options="endpointOptions"
        />
      </label>
      <label class="filter-field">
        <span>模块</span>
        <TSelect
          v-model="filters.moduleId"
          clearable
          filterable
          placeholder="全部模块"
          :disabled="!filters.endpointId"
          :options="moduleOptions"
        />
      </label>
      <label class="filter-field">
        <span>环境</span>
        <TSelect
          v-model="filters.runtimeEnvironmentId"
          clearable
          filterable
          placeholder="全部环境"
          :options="runtimeEnvironmentOptions"
        />
      </label>
      <label class="filter-field">
        <span>脚本信息</span>
        <TInput v-model="filters.keyword" clearable placeholder="服务 / 环境 / 脚本内容" />
      </label>
      <label class="filter-field">
        <span>脚本状态</span>
        <TSelect v-model="filters.scriptState" clearable placeholder="全部脚本" :options="scriptStateOptions" />
      </label>
      <label class="filter-field">
        <span>状态</span>
        <TSelect v-model="filters.status" clearable placeholder="全部状态" :options="statusOptions" />
      </label>
    </template>

    <template #environment="{ row }">
      <div class="environment-cell">
        <strong>{{ row.environment.name }}</strong>
        <span>{{ row.environment.code }} · {{ resolveScope(row.environment) }}</span>
      </div>
    </template>
    <template #containerType="{ row }">{{ resolveContainerType(row.containerType) }}</template>
    <template #ports="{ row }">{{ row.hostPort }} -> {{ row.containerPort }}</template>
    <template #deployScript="{ row }">
      <code v-if="row.deployScript?.trim()" class="script-snippet">{{ firstScriptLine(row.deployScript) }}</code>
      <TTag v-else theme="warning" variant="light">未配置</TTag>
    </template>
    <template #status="{ row }">
      <TTag :theme="row.status === 1 ? 'success' : 'default'" variant="light">
        {{ row.status === 1 ? '启用' : '停用' }}
      </TTag>
    </template>
    <template #actions="{ row }">
      <TSpace>
        <RowAction label="编辑" @click="openEdit(row)" />
        <RowAction icon="lucide:eraser" label="清空" theme="danger" @click="clearScript(row)" />
      </TSpace>
    </template>
  </AdminListPage>

  <TDrawer
    v-model:visible="visible"
    size="760px"
    header="部署脚本维护"
    :confirm-btn="{ content: '保存', loading: saving }"
    @confirm="save"
  >
    <TForm ref="formRef" :data="form" :rules="rules" label-width="110px">
      <TFormItem label="运行环境" name="runtimeEnvironmentId">
        <TSelect
          v-model="form.runtimeEnvironmentId"
          filterable
          placeholder="请选择运行环境"
          :options="formEnvironmentOptions"
        />
      </TFormItem>
      <TFormItem label="服务名称" name="name">
        <TInput v-model="form.name" placeholder="agentsprint-api" />
      </TFormItem>
      <div class="form-grid">
        <TFormItem label="服务类型" name="containerType">
          <TSelect v-model="form.containerType" :options="containerTypeOptions" />
        </TFormItem>
        <TFormItem label="服务机器" name="serverIp">
          <TSelect v-model="form.serverIp" placeholder="请选择 IP" :options="formServerIpOptions" />
        </TFormItem>
      </div>
      <div class="form-grid">
        <TFormItem label="主机端口" name="hostPort">
          <TInput v-model="form.hostPort" placeholder="5999" />
        </TFormItem>
        <TFormItem label="容器端口" name="containerPort">
          <TInput v-model="form.containerPort" placeholder="80" />
        </TFormItem>
      </div>
      <div class="form-grid">
        <TFormItem label="协议">
          <TSelect v-model="form.protocol" :options="protocolOptions" />
        </TFormItem>
        <TFormItem label="排序" name="sort">
          <TInput v-model="form.sort" />
        </TFormItem>
      </div>
      <TFormItem label="执行提示">
        <TTextarea v-model="form.prompt" :autosize="{ minRows: 2, maxRows: 4 }" placeholder="记录执行前置条件、回滚说明或健康检查入口" />
      </TFormItem>
      <TFormItem label="部署脚本" name="deployScript">
        <TTextarea
          v-model="form.deployScript"
          :autosize="{ minRows: 8, maxRows: 14 }"
          placeholder="docker compose up -d agentsprint-api"
        />
      </TFormItem>
      <TFormItem label="说明">
        <TTextarea v-model="form.description" :autosize="{ minRows: 2, maxRows: 4 }" placeholder="脚本用途或影响范围" />
      </TFormItem>
      <TFormItem label="状态">
        <TSelect v-model="form.status" :options="statusOptions" />
      </TFormItem>
    </TForm>
  </TDrawer>
</template>

<style scoped>
.filter-field {
  display: grid;
  grid-template-columns: auto minmax(180px, 260px);
  gap: 8px;
  align-items: center;
  color: var(--td-text-color-secondary);
}

.environment-cell {
  display: grid;
  gap: 2px;
  min-width: 0;
}

.environment-cell strong {
  overflow: hidden;
  color: var(--td-text-color-primary);
  text-overflow: ellipsis;
  white-space: nowrap;
}

.environment-cell span {
  overflow: hidden;
  color: var(--td-text-color-secondary);
  font-size: 12px;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.script-snippet {
  display: inline-block;
  max-width: 100%;
  overflow: hidden;
  padding: 2px 6px;
  border-radius: 4px;
  background: var(--td-bg-color-container-hover);
  color: var(--td-text-color-primary);
  font-family: var(--td-font-family-mono, Consolas, monospace);
  font-size: 12px;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.form-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
}

@media (max-width: 760px) {
  .filter-field,
  .form-grid {
    grid-template-columns: 1fr;
    width: 100%;
  }
}
</style>
