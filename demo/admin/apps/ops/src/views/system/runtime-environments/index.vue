<script lang="ts" setup>
import type { SprintMvpApi } from '#/api/sprint/mvp';
import type { SystemApi } from '#/api';
import type { FormInstanceFunctions, FormRules, PrimaryTableCol } from 'tdesign-vue-next';

import { IconifyIcon } from '@vben/icons';
import { computed, onMounted, reactive, ref, watch } from 'vue';

import {
  deleteRuntimeEnvironmentApi,
  deleteRuntimeEnvironmentContainerApi,
  listDictionaryItemsApi,
  listDictionaryTypesApi,
  listRuntimeEnvironmentContainersApi,
  listRuntimeEnvironmentsApi,
  saveRuntimeEnvironmentApi,
  saveRuntimeEnvironmentContainerApi,
} from '#/api';
import {
  listFeatureModulesApi,
  listProjectEndpointsApi,
  listProjectsApi,
} from '#/api/sprint/mvp';
import {
  optionalNumberRule,
  requiredRule,
  validateForm,
} from '#/views/_shared/form-rules';
import AdminListPage from '#/components/admin-list-page/admin-list-page.vue';
import { confirmAndClose } from '#/views/_shared/dialog-confirm';
import { withSerialColumn } from '#/views/_shared/table-columns';
import RowAction from '#/views/system/_shared/row-action.vue';
import { getCellRow } from '#/views/system/_shared/table-cell';
import {
  Button as TButton,
  Dialog as TDialog,
  Drawer as TDrawer,
  Form as TForm,
  FormItem as TFormItem,
  Input as TInput,
  MessagePlugin,
  Select as TSelect,
  Space as TSpace,
  Table as TTable,
  Tag as TTag,
  Textarea as TTextarea,
} from 'tdesign-vue-next';

defineOptions({ name: 'OperationEnvironments' });

const loading = ref(false);
const serviceSaving = ref(false);
const environmentSaving = ref(false);
const environmentVisible = ref(false);
const serviceVisible = ref(false);
const environmentFormRef = ref<FormInstanceFunctions>();
const serviceFormRef = ref<FormInstanceFunctions>();
const environments = ref<SystemApi.RuntimeEnvironment[]>([]);
const servicesByEnvironment = ref<Record<string, SystemApi.RuntimeEnvironmentContainer[]>>({});
const serviceLoadingIds = ref<string[]>([]);
const expandedRowKeys = ref<string[]>([]);
const projects = ref<SprintMvpApi.Project[]>([]);
const endpoints = ref<SprintMvpApi.ProjectEndpoint[]>([]);
const modules = ref<SprintMvpApi.FeatureModule[]>([]);
const containerTypeItems = ref<SystemApi.DictionaryItem[]>([]);

const filters = reactive({
  endpointId: '',
  moduleId: '',
  projectId: '',
});
const query = reactive({ ...filters });
const environmentPagination = reactive({
  current: 1,
  pageSize: 30,
  pageSizeOptions: [30, 50, 100, 200],
});
const environmentForm = reactive<Partial<SystemApi.RuntimeEnvironment>>({
  apiBaseUrl: '',
  code: '',
  composeFilePath: '',
  deployRoot: '',
  description: '',
  dockerDirectory: '',
  endpointId: '',
  environmentType: 'test',
  frontendProxyApiUrl: '',
  frontendUrl: '',
  id: undefined,
  localPackagePaths: '',
  mcpEndpoint: '',
  moduleId: '',
  name: '',
  projectId: '',
  remotePackagePath: '',
  serverIps: '',
  sort: 10,
  status: 1,
});
const serviceForm = reactive<Partial<SystemApi.RuntimeEnvironmentContainer>>({
  containerPort: 80,
  containerType: 0,
  deployScript: '',
  description: '',
  hostPort: 5999,
  id: undefined,
  name: '',
  prompt: '',
  protocol: 'tcp',
  runtimeEnvironmentId: '',
  serverIp: '',
  sort: 10,
  status: 1,
});

const environmentRules: FormRules<typeof environmentForm> = {
  code: requiredRule('请输入环境编码'),
  environmentType: requiredRule('请选择环境类型', 'change'),
  name: requiredRule('请输入环境名称'),
  serverIps: requiredRule('请输入服务器 IP'),
  sort: optionalNumberRule('排序必须是数字'),
};
const serviceRules: FormRules<typeof serviceForm> = {
  containerPort: optionalNumberRule('服务端口必须是数字'),
  containerType: requiredRule('请选择服务类型', 'change'),
  hostPort: optionalNumberRule('宿主端口必须是数字'),
  name: requiredRule('请输入服务名称'),
  runtimeEnvironmentId: requiredRule('请选择运行环境', 'change'),
  serverIp: requiredRule('请选择服务机器 IP', 'change'),
  sort: optionalNumberRule('排序必须是数字'),
};

const statusOptions = [
  { label: '启用', value: 1 },
  { label: '停用', value: 0 },
];
const environmentTypeOptions = [
  { label: '测试环境', value: 'test' },
  { label: '预发环境', value: 'staging' },
  { label: '生产环境', value: 'production' },
];
const protocolOptions = [
  { label: 'TCP', value: 'tcp' },
  { label: 'UDP', value: 'udp' },
];
const environmentColumns: PrimaryTableCol[] = [
  { colKey: 'code', title: '环境编码', width: 120 },
  { colKey: 'name', title: '环境名称', width: 150 },
  { colKey: 'scope', title: '归属范围', cell: 'scope', width: 240 },
  { colKey: 'serverIps', title: '服务器 IP', cell: 'serverIps', width: 220 },
  { colKey: 'deployRoot', title: '部署根目录', width: 230 },
  { colKey: 'status', title: '状态', width: 80 },
  { colKey: 'actions', title: '操作', width: 170 },
];
const serviceColumns: PrimaryTableCol[] = [
  { colKey: 'name', title: '服务名称', width: 180 },
  { colKey: 'containerType', title: '服务类型', cell: 'containerType', width: 120 },
  { colKey: 'serverIp', title: '机器 IP', width: 150 },
  { colKey: 'ports', title: '端口映射', cell: 'ports', width: 150 },
  { colKey: 'protocol', title: '协议', width: 80 },
  { colKey: 'description', title: '说明', cell: (...args: any[]) => getCellRow(args[0], args[1])?.description || '-' },
  { colKey: 'status', title: '状态', width: 80, cell: (...args: any[]) => (getCellRow(args[0], args[1])?.status === 1 ? '启用' : '停用') },
  { colKey: 'actions', title: '操作', width: 120, cell: 'actions' },
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
const formEndpointOptions = computed(() =>
  endpoints.value
    .filter((item) => item.projectId === environmentForm.projectId)
    .map((item) => ({ label: `${item.name} (${item.code})`, value: item.id })),
);
const formModuleOptions = computed(() =>
  modules.value
    .filter((item) => item.projectId === environmentForm.projectId)
    .filter((item) => item.endpointId === environmentForm.endpointId)
    .map((item) => ({ label: `${item.name} (${item.code})`, value: item.id })),
);
const containerTypeOptions = computed(() =>
  containerTypeItems.value
    .filter((item) => item.status === 1)
    .sort((left, right) => left.sort - right.sort)
    .map((item) => ({ label: item.name, value: Number(item.code) })),
);
const projectMap = computed(() => Object.fromEntries(projects.value.map((item) => [item.id, item])));
const endpointMap = computed(() => Object.fromEntries(endpoints.value.map((item) => [item.id, item])));
const moduleMap = computed(() => Object.fromEntries(modules.value.map((item) => [item.id, item])));
const selectedServiceEnvironment = computed(() =>
  environments.value.find((item) => item.id === serviceForm.runtimeEnvironmentId),
);
const serviceIpOptions = computed(() =>
  parseServerIps(selectedServiceEnvironment.value?.serverIps).map((ip) => ({ label: ip, value: ip })),
);
const environmentTablePagination = computed(() => ({
  current: environmentPagination.current,
  pageSize: environmentPagination.pageSize,
  pageSizeOptions: environmentPagination.pageSizeOptions,
  total: environments.value.length,
}));

watch(
  () => environments.value.length,
  (total) => {
    const maxPage = Math.max(1, Math.ceil(total / environmentPagination.pageSize));
    if (environmentPagination.current > maxPage) {
      environmentPagination.current = maxPage;
    }
  },
);

watch(
  () => filters.projectId,
  () => {
    filters.endpointId = '';
    filters.moduleId = '';
  },
);
watch(
  () => filters.endpointId,
  () => {
    filters.moduleId = '';
  },
);
watch(
  () => environmentForm.projectId,
  () => {
    environmentForm.endpointId = '';
    environmentForm.moduleId = '';
  },
);
watch(
  () => environmentForm.endpointId,
  () => {
    environmentForm.moduleId = '';
  },
);
watch(
  () => serviceForm.runtimeEnvironmentId,
  () => {
    if (serviceForm.serverIp && !serviceIpOptions.value.some((item) => item.value === serviceForm.serverIp)) {
      serviceForm.serverIp = '';
    }
  },
);

function parseServerIps(value?: string) {
  return (value || '')
    .split(/[,;\n\r]+/)
    .map((item) => item.trim())
    .filter(Boolean);
}

function formatServerIps(value?: string) {
  const ips = parseServerIps(value);
  return ips.length > 0 ? ips.join(' / ') : '-';
}

function resolveScope(row: SystemApi.RuntimeEnvironment) {
  const scope = [
    row.projectId ? projectMap.value[row.projectId]?.name || row.projectId : '全局',
    row.endpointId ? endpointMap.value[row.endpointId]?.name || row.endpointId : '',
    row.moduleId ? moduleMap.value[row.moduleId]?.name || row.moduleId : '',
  ].filter(Boolean);
  return scope.join(' / ');
}

function resolveContainerType(value?: number) {
  return containerTypeOptions.value.find((item) => item.value === value)?.label || String(value ?? '-');
}

function openEnvironment(row?: SystemApi.RuntimeEnvironment) {
  Object.assign(environmentForm, {
    apiBaseUrl: row?.apiBaseUrl || '',
    code: row?.code || '',
    composeFilePath: row?.composeFilePath || '',
    deployRoot: row?.deployRoot || '',
    description: row?.description || '',
    dockerDirectory: row?.dockerDirectory || '',
    endpointId: row?.endpointId || '',
    environmentType: row?.environmentType || 'test',
    frontendProxyApiUrl: row?.frontendProxyApiUrl || '',
    frontendUrl: row?.frontendUrl || '',
    id: row?.id,
    localPackagePaths: row?.localPackagePaths || '',
    mcpEndpoint: row?.mcpEndpoint || '',
    moduleId: row?.moduleId || '',
    name: row?.name || '',
    projectId: row?.projectId || '',
    remotePackagePath: row?.remotePackagePath || '',
    serverIps: row?.serverIps || '',
    sort: row?.sort ?? 10,
    status: row?.status ?? 1,
  });
  environmentVisible.value = true;
}

function openService(environment: SystemApi.RuntimeEnvironment, row?: SystemApi.RuntimeEnvironmentContainer) {
  const firstIp = parseServerIps(environment.serverIps)[0] || '';
  Object.assign(serviceForm, {
    containerPort: row?.containerPort ?? 80,
    containerType: row?.containerType ?? 0,
    deployScript: row?.deployScript || '',
    description: row?.description || '',
    hostPort: row?.hostPort ?? 5999,
    id: row?.id,
    name: row?.name || '',
    prompt: row?.prompt || '',
    protocol: row?.protocol || 'tcp',
    runtimeEnvironmentId: environment.id,
    serverIp: row?.serverIp || firstIp,
    sort: row?.sort ?? 10,
    status: row?.status ?? 1,
  });
  serviceVisible.value = true;
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

async function loadEnvironments() {
  loading.value = true;
  try {
    environments.value = await listRuntimeEnvironmentsApi({
      endpointId: query.endpointId || undefined,
      moduleId: query.moduleId || undefined,
      projectId: query.projectId || undefined,
    });
    expandedRowKeys.value = expandedRowKeys.value.filter((id) =>
      environments.value.some((item) => item.id === id),
    );
  } finally {
    loading.value = false;
  }
}

async function loadServices(runtimeEnvironmentId: string) {
  if (!runtimeEnvironmentId) return;
  serviceLoadingIds.value = [...new Set([...serviceLoadingIds.value, runtimeEnvironmentId])];
  try {
    servicesByEnvironment.value = {
      ...servicesByEnvironment.value,
      [runtimeEnvironmentId]: await listRuntimeEnvironmentContainersApi(runtimeEnvironmentId),
    };
  } finally {
    serviceLoadingIds.value = serviceLoadingIds.value.filter((id) => id !== runtimeEnvironmentId);
  }
}

async function onExpandChange(keys: Array<number | string>) {
  expandedRowKeys.value = keys.map(String);
  await Promise.all(
    expandedRowKeys.value
      .filter((id) => !servicesByEnvironment.value[id])
      .map((id) => loadServices(id)),
  );
}

async function search() {
  Object.assign(query, filters);
  environmentPagination.current = 1;
  await loadEnvironments();
}

async function reset() {
  Object.assign(filters, { endpointId: '', moduleId: '', projectId: '' });
  Object.assign(query, filters);
  environmentPagination.current = 1;
  await loadEnvironments();
}

function handlePageChange(pageInfo: { current: number; pageSize: number }) {
  environmentPagination.current = pageInfo.current;
  environmentPagination.pageSize = pageInfo.pageSize;
}

async function saveEnvironment() {
  if (environmentSaving.value) return;
  if (!(await validateForm(environmentFormRef.value))) return;
  environmentSaving.value = true;
  try {
    const saved = await saveRuntimeEnvironmentApi({
      ...environmentForm,
      endpointId: environmentForm.endpointId || undefined,
      moduleId: environmentForm.moduleId || undefined,
      projectId: environmentForm.projectId || undefined,
      sort: Number(environmentForm.sort || 0),
    });
    MessagePlugin.success('运行环境已保存');
    environmentVisible.value = false;
    await loadEnvironments();
    if (expandedRowKeys.value.includes(saved.id)) {
      await loadServices(saved.id);
    }
  } finally {
    environmentSaving.value = false;
  }
}

async function saveService() {
  if (serviceSaving.value) return;
  if (!(await validateForm(serviceFormRef.value))) return;
  serviceSaving.value = true;
  try {
    await saveRuntimeEnvironmentContainerApi({
      ...serviceForm,
      containerPort: Number(serviceForm.containerPort),
      containerType: Number(serviceForm.containerType ?? 0),
      hostPort: Number(serviceForm.hostPort),
      sort: Number(serviceForm.sort || 0),
    });
    MessagePlugin.success('服务已保存');
    serviceVisible.value = false;
    await loadServices(serviceForm.runtimeEnvironmentId || '');
  } finally {
    serviceSaving.value = false;
  }
}

function removeEnvironment(row: SystemApi.RuntimeEnvironment) {
  confirmAndClose({
    body: `确认删除运行环境 ${row.code}？该环境下的服务也会被删除。`,
    confirmBtn: '删除',
    header: '删除运行环境',
    onConfirm: async () => {
      await deleteRuntimeEnvironmentApi(row.id);
      MessagePlugin.success('运行环境已删除');
      await loadEnvironments();
    },
  });
}

function removeService(row: SystemApi.RuntimeEnvironmentContainer) {
  confirmAndClose({
    body: `确认删除服务 ${row.name}？`,
    confirmBtn: '删除',
    header: '删除服务',
    onConfirm: async () => {
      await deleteRuntimeEnvironmentContainerApi(row.id);
      MessagePlugin.success('服务已删除');
      await loadServices(row.runtimeEnvironmentId);
    },
  });
}

onMounted(async () => {
  await loadReferences();
  await loadEnvironments();
});
</script>

<template>
  <div>
    <AdminListPage
      title="环境配置"
      description="按项目、端、模块维护环境服务器与部署配置，展开环境行管理服务。"
      table-title="环境列表"
      add-button-text="新增环境"
      :columns="environmentColumns"
      :data="environments"
      :expanded-row-keys="expandedRowKeys"
      :loading="loading"
      :pagination="environmentTablePagination"
      :refreshable="false"
      @add="openEnvironment()"
      @expand-change="onExpandChange"
      @page-change="handlePageChange"
      @reset="reset"
      @search="search"
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
      </template>
      <template #scope="{ row }">{{ resolveScope(row) }}</template>
      <template #serverIps="{ row }">{{ formatServerIps(row.serverIps) }}</template>
      <template #status="{ row }">
        <TTag :theme="row.status === 1 ? 'success' : 'default'" variant="light">
          {{ row.status === 1 ? '启用' : '停用' }}
        </TTag>
      </template>
      <template #actions="{ row }">
        <TSpace>
          <RowAction icon="lucide:server-cog" label="服务" @click="openService(row)" />
          <RowAction label="编辑" @click="openEnvironment(row)" />
          <RowAction label="删除" theme="danger" @click="removeEnvironment(row)" />
        </TSpace>
      </template>
      <template #expandedRow="{ row }">
        <div class="service-panel">
          <div class="service-panel__header">
            <div>
              <strong>服务管理</strong>
              <span>{{ row.name }} · {{ formatServerIps(row.serverIps) }}</span>
            </div>
            <TButton size="small" theme="primary" @click="openService(row)">
              <template #icon>
                <IconifyIcon icon="lucide:plus" />
              </template>
              新增服务
            </TButton>
          </div>
          <TTable
            row-key="id"
            class="agent-list-table"
            size="small"
            :columns="withSerialColumn(serviceColumns)"
            :data="servicesByEnvironment[row.id] || []"
            :loading="serviceLoadingIds.includes(row.id)"
            hover
            stripe
          >
            <template #containerType="{ row: service }">{{ resolveContainerType(service.containerType) }}</template>
            <template #ports="{ row: service }">{{ service.hostPort }} -> {{ service.containerPort }}</template>
            <template #actions="{ row: service }">
              <TSpace>
                <RowAction label="编辑" @click="openService(row, service)" />
                <RowAction label="删除" theme="danger" @click="removeService(service)" />
              </TSpace>
            </template>
          </TTable>
        </div>
      </template>
    </AdminListPage>

    <TDrawer v-model:visible="environmentVisible" size="760px" header="运行环境维护" :confirm-btn="{ content: '保存', loading: environmentSaving }" @confirm="saveEnvironment">
      <TForm ref="environmentFormRef" :data="environmentForm" :rules="environmentRules" label-width="116px">
        <TFormItem label="环境编码" name="code"><TInput v-model="environmentForm.code" placeholder="test" /></TFormItem>
        <TFormItem label="环境名称" name="name"><TInput v-model="environmentForm.name" placeholder="测试环境" /></TFormItem>
        <TFormItem label="环境类型" name="environmentType"><TSelect v-model="environmentForm.environmentType" :options="environmentTypeOptions" /></TFormItem>
        <TFormItem label="所属项目"><TSelect v-model="environmentForm.projectId" clearable filterable :options="projectOptions" /></TFormItem>
        <TFormItem label="所属端"><TSelect v-model="environmentForm.endpointId" clearable filterable :disabled="!environmentForm.projectId" :options="formEndpointOptions" /></TFormItem>
        <TFormItem label="所属模块"><TSelect v-model="environmentForm.moduleId" clearable filterable :disabled="!environmentForm.endpointId" :options="formModuleOptions" /></TFormItem>
        <TFormItem label="服务器 IP" name="serverIps"><TTextarea v-model="environmentForm.serverIps" placeholder="192.168.80.101&#10;192.168.80.102" :autosize="{ minRows: 2, maxRows: 5 }" /></TFormItem>
        <TFormItem label="前端地址"><TInput v-model="environmentForm.frontendUrl" placeholder="http://192.168.80.101:5999" /></TFormItem>
        <TFormItem label="API 地址"><TInput v-model="environmentForm.apiBaseUrl" placeholder="http://192.168.80.101:18080" /></TFormItem>
        <TFormItem label="前端代理 API"><TInput v-model="environmentForm.frontendProxyApiUrl" placeholder="http://192.168.80.101:5999/api" /></TFormItem>
        <TFormItem label="MCP 地址"><TInput v-model="environmentForm.mcpEndpoint" placeholder="http://192.168.80.101:5010/mcp" /></TFormItem>
        <TFormItem label="部署根目录"><TInput v-model="environmentForm.deployRoot" placeholder="/opt/agentsprint-deploy" /></TFormItem>
        <TFormItem label="Docker 目录"><TInput v-model="environmentForm.dockerDirectory" placeholder="/opt/agentsprint-deploy/docker" /></TFormItem>
        <TFormItem label="远端发布包"><TInput v-model="environmentForm.remotePackagePath" placeholder="/opt/agentsprint-deploy/agentsprint-docker-deploy.tgz" /></TFormItem>
        <TFormItem label="Compose 文件"><TInput v-model="environmentForm.composeFilePath" placeholder="/opt/agentsprint-deploy/docker/docker-compose.yml" /></TFormItem>
        <TFormItem label="本地发布包"><TTextarea v-model="environmentForm.localPackagePaths" :autosize="{ minRows: 3, maxRows: 6 }" /></TFormItem>
        <TFormItem label="说明"><TTextarea v-model="environmentForm.description" :autosize="{ minRows: 3, maxRows: 5 }" /></TFormItem>
        <TFormItem label="排序" name="sort"><TInput v-model="environmentForm.sort" type="number" /></TFormItem>
        <TFormItem label="状态"><TSelect v-model="environmentForm.status" :options="statusOptions" /></TFormItem>
      </TForm>
    </TDrawer>

    <TDialog v-model:visible="serviceVisible" header="服务维护" width="760px" :confirm-btn="{ content: '保存', loading: serviceSaving }" @confirm="saveService">
      <TForm ref="serviceFormRef" :data="serviceForm" :rules="serviceRules" label-width="112px">
        <TFormItem label="运行环境" name="runtimeEnvironmentId">
          <TSelect
            v-model="serviceForm.runtimeEnvironmentId"
            :options="environments.map((item) => ({ label: `${item.name} (${item.code})`, value: item.id }))"
          />
        </TFormItem>
        <TFormItem label="服务名称" name="name"><TInput v-model="serviceForm.name" placeholder="agentsprint-admin" /></TFormItem>
        <TFormItem label="服务类型" name="containerType"><TSelect v-model="serviceForm.containerType" :options="containerTypeOptions" /></TFormItem>
        <TFormItem label="机器 IP" name="serverIp"><TSelect v-model="serviceForm.serverIp" :disabled="!serviceForm.runtimeEnvironmentId" :options="serviceIpOptions" /></TFormItem>
        <TFormItem label="宿主端口" name="hostPort"><TInput v-model="serviceForm.hostPort" type="number" /></TFormItem>
        <TFormItem label="服务端口" name="containerPort"><TInput v-model="serviceForm.containerPort" type="number" /></TFormItem>
        <TFormItem label="协议"><TSelect v-model="serviceForm.protocol" :options="protocolOptions" /></TFormItem>
        <TFormItem label="说明"><TTextarea v-model="serviceForm.description" /></TFormItem>
        <TFormItem label="提示词"><TTextarea v-model="serviceForm.prompt" :autosize="{ minRows: 4, maxRows: 10 }" /></TFormItem>
        <TFormItem label="部署脚本"><TTextarea v-model="serviceForm.deployScript" class="script-textarea" :autosize="{ minRows: 6, maxRows: 14 }" /></TFormItem>
        <TFormItem label="排序" name="sort"><TInput v-model="serviceForm.sort" type="number" /></TFormItem>
        <TFormItem label="状态"><TSelect v-model="serviceForm.status" :options="statusOptions" /></TFormItem>
      </TForm>
    </TDialog>
  </div>
</template>

<style scoped>
.filter-field {
  display: grid;
  grid-template-columns: auto minmax(180px, 260px);
  gap: 8px;
  align-items: center;
  color: var(--td-text-color-secondary);
}

.service-panel {
  background: var(--td-bg-color-container);
  border: 1px solid var(--td-component-border);
  border-radius: 6px;
  padding: 12px;
}

.service-panel__header {
  align-items: center;
  display: flex;
  justify-content: space-between;
  margin-bottom: 12px;
}

.service-panel__header strong {
  display: block;
  font-size: 14px;
  line-height: 22px;
}

.service-panel__header span {
  color: var(--td-text-color-secondary);
  display: block;
  font-size: 12px;
  line-height: 20px;
}

.script-textarea :deep(textarea) {
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, 'Liberation Mono', monospace;
}

@media (max-width: 960px) {
  .filter-field {
    grid-template-columns: 1fr;
    width: 100%;
  }
}
</style>
