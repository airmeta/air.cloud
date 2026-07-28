<script lang="ts" setup>
import type { SprintMvpApi, SprintUserApi } from '#/api/sprint/mvp';
import type { FormInstanceFunctions, FormRules } from 'tdesign-vue-next';

import { IconifyIcon } from '@vben/icons';
import { computed, onMounted, reactive, ref, watch } from 'vue';

import {
  Button as TButton,
  Card as TCard,
  Drawer as TDrawer,
  Form as TForm,
  FormItem as TFormItem,
  Input as TInput,
  Link as TLink,
  MessagePlugin,
  Pagination as TPagination,
  Select as TSelect,
  Space as TSpace,
  Table as TTable,
  Tag as TTag,
  Textarea as TTextarea,
} from 'tdesign-vue-next';

import {
  createFeatureModuleApi,
  deleteFeatureModuleApi,
  createProjectEndpointApi,
  listFeatureModulesApi,
  listProjectEndpointsApi,
  listProjectsApi,
  listRequirementsApi,
  listSkillsApi,
  listUserOptionsApi,
  updateFeatureModuleApi,
  updateProjectEndpointApi,
} from '#/api/sprint/mvp';
import {
  optionalNumberRule,
  requiredRule,
  validateForm,
} from '#/views/_shared/form-rules';
import { confirmAndClose } from '#/views/_shared/dialog-confirm';
import { withSerialColumn } from '#/views/_shared/table-columns';

import ProjectContextListShell from '#/components/project-context-list-shell/project-context-list-shell.vue';
import SkillSelectOption from '../_shared/skill-select-option.vue';
import '../_shared/table-layout.css';

type EndpointPager = {
  current: number;
  pageSize: number;
};

const endpointSaving = ref(false);
const loading = ref(false);
const moduleSaving = ref(false);
const endpointVisible = ref(false);
const endpointMode = ref<'create' | 'edit'>('create');
const endpointFormRef = ref<FormInstanceFunctions>();
const moduleVisible = ref(false);
const moduleMode = ref<'create' | 'edit'>('create');
const moduleFormRef = ref<FormInstanceFunctions>();
const selectedProjectId = ref('');
const projects = ref<SprintMvpApi.Project[]>([]);
const endpoints = ref<SprintMvpApi.ProjectEndpoint[]>([]);
const modules = ref<SprintMvpApi.FeatureModule[]>([]);
const requirements = ref<SprintMvpApi.Requirement[]>([]);
const skills = ref<SprintMvpApi.Skill[]>([]);
const users = ref<SprintUserApi.UserOption[]>([]);
const deletingModuleIds = ref(new Set<string>());
const endpointPagers = reactive<Record<string, EndpointPager>>({});

const endpointForm = reactive({
  code: '',
  developerIds: [] as string[],
  id: '',
  name: '',
  ownerId: '',
  projectId: '',
  skillIds: [] as string[],
  sort: '',
  status: 'active',
  testerIds: [] as string[],
  type: 'web',
});
const moduleForm = reactive({
  code: '',
  description: '',
  developerIds: [] as string[],
  endpointId: '',
  id: '',
  name: '',
  ownerId: '',
  projectId: '',
  sort: '',
  status: 'active',
  testerIds: [] as string[],
});
const endpointRules = computed<FormRules<typeof endpointForm>>(() => ({
  name: requiredRule('请输入端名称'),
  sort: optionalNumberRule('排序必须是数字'),
  type: requiredRule('请选择端类型', 'change'),
}));
const moduleRules = computed<FormRules<typeof moduleForm>>(() => ({
  code: moduleMode.value === 'create' ? requiredRule('请输入模块编码') : [],
  endpointId: requiredRule('请选择所属端', 'change'),
  name: requiredRule('请输入模块名称'),
  sort: optionalNumberRule('排序必须是数字'),
}));

const endpointTypeOptions = [
  { label: 'iOS', value: 'ios' },
  { label: '安卓', value: 'android' },
  { label: '桌面端', value: 'desktop' },
  { label: 'Web网站', value: 'web' },
  { label: '管理后台', value: 'admin' },
  { label: '其他端', value: 'other' },
];
const statusOptions = [
  { label: '启用', value: 'active' },
  { label: '停用', value: 'disabled' },
];
const moduleColumns = [
  { colKey: 'name', title: '模块名称', width: 180 },
  { colKey: 'code', title: '编码', width: 120 },
  { colKey: 'ownerId', title: '负责人', width: 140 },
  { colKey: 'developerIds', title: '研发人员' },
  { colKey: 'testerIds', title: '测试人员' },
  { colKey: 'status', title: '状态', width: 90 },
  { colKey: 'actions', title: '操作', width: 120 },
];

const selectedEndpoints = computed(() =>
  endpoints.value
    .filter((endpoint) => endpoint.projectId === selectedProjectId.value)
    .sort((left, right) => left.sort - right.sort || left.createTime.localeCompare(right.createTime)),
);
const selectedModules = computed(() =>
  modules.value.filter((module) => module.projectId === selectedProjectId.value),
);
const userOptions = computed(() =>
  users.value.map((user) => ({
    label: `${user.displayName} (${user.username})`,
    value: user.id,
  })),
);
const userMap = computed(() => Object.fromEntries(users.value.map((item) => [item.id, item])));
const skillOptions = computed(() =>
  skills.value.map((skill) => ({
    label: `${skill.code} - ${skill.name}`,
    skill,
    value: skill.id,
  })),
);
const skillMap = computed(() => Object.fromEntries(skills.value.map((item) => [item.id, item])));

function resolveUserName(userId?: string) {
  return userId ? userMap.value[userId]?.displayName || userId : '未指定';
}

function resolveUserNames(userIds?: string[]) {
  return userIds && userIds.length > 0 ? userIds.map((id) => resolveUserName(id)).join('、') : '未指定';
}

function endpointTypeLabel(type: string) {
  return endpointTypeOptions.find((item) => item.value === type)?.label || type;
}

function resolveSkillNames(skillIds?: string[]) {
  return skillIds && skillIds.length > 0
    ? skillIds.map((id) => skillMap.value[id]?.name || id).join(' / ')
    : '未选择';
}

function moduleRequirementCount(moduleId: string) {
  return requirements.value.filter((requirement) => requirement.moduleId === moduleId).length;
}

function canDeleteModule(module: SprintMvpApi.FeatureModule) {
  return moduleRequirementCount(module.id) === 0;
}

function isDeletingModule(moduleId: string) {
  return deletingModuleIds.value.has(moduleId);
}

function setDeletingModule(moduleId: string, deleting: boolean) {
  const nextIds = new Set(deletingModuleIds.value);
  if (deleting) {
    nextIds.add(moduleId);
  } else {
    nextIds.delete(moduleId);
  }
  deletingModuleIds.value = nextIds;
}

function endpointModules(endpointId: string) {
  return selectedModules.value
    .filter((module) => module.endpointId === endpointId)
    .sort((left, right) => left.sort - right.sort || left.createTime.localeCompare(right.createTime));
}

function endpointPageModules(endpointId: string) {
  const pager = ensurePager(endpointId);
  const source = endpointModules(endpointId);
  const start = (pager.current - 1) * pager.pageSize;
  return source.slice(start, start + pager.pageSize);
}

function ensurePager(endpointId: string) {
  endpointPagers[endpointId] ||= { current: 1, pageSize: 30 };
  return endpointPagers[endpointId];
}

function handleEndpointPageChange(endpointId: string, pageInfo: { current: number; pageSize: number }) {
  const pager = ensurePager(endpointId);
  pager.current = pageInfo.current;
  pager.pageSize = pageInfo.pageSize;
}

function resetEndpointForm(projectId = selectedProjectId.value) {
  Object.assign(endpointForm, {
    code: '',
    developerIds: [],
    id: '',
    name: '',
    ownerId: '',
    projectId,
    skillIds: [],
    sort: '',
    status: 'active',
    testerIds: [],
    type: 'admin',
  });
}

function openEndpointCreate() {
  if (!selectedProjectId.value) {
    MessagePlugin.warning('请先选择项目');
    return;
  }

  resetEndpointForm();
  endpointForm.name = '管理后台';
  endpointForm.code = generateEndpointCode(endpointForm.type, endpointForm.name);
  endpointMode.value = 'create';
  endpointVisible.value = true;
}

function openEndpointEdit(endpoint: SprintMvpApi.ProjectEndpoint) {
  Object.assign(endpointForm, {
    code: endpoint.code,
    developerIds: [...(endpoint.developerIds || [])],
    id: endpoint.id,
    name: endpoint.name,
    ownerId: endpoint.ownerId || '',
    projectId: endpoint.projectId,
    skillIds: [...(endpoint.skillIds || [])],
    sort: String(endpoint.sort ?? ''),
    status: endpoint.status,
    testerIds: [...(endpoint.testerIds || [])],
    type: endpoint.type,
  });
  endpointMode.value = 'edit';
  endpointVisible.value = true;
}

async function saveEndpoint() {
  if (endpointSaving.value) return;
  if (!(await validateForm(endpointFormRef.value))) return;
  if (!endpointForm.projectId || !endpointForm.name.trim() || !endpointForm.type) {
    MessagePlugin.warning('端名称和端类型必填');
    return;
  }

  const payload = {
    developerIds: [...endpointForm.developerIds],
    name: endpointForm.name.trim(),
    ownerId: endpointForm.ownerId || undefined,
    skillIds: [...endpointForm.skillIds],
    sort: endpointForm.sort ? Number(endpointForm.sort) : undefined,
    status: endpointForm.status || undefined,
    testerIds: [...endpointForm.testerIds],
    type: endpointForm.type,
  };
  endpointSaving.value = true;
  try {
    if (endpointMode.value === 'create') {
      await createProjectEndpointApi({
        ...payload,
        code: generateEndpointCode(endpointForm.type, endpointForm.name),
        projectId: endpointForm.projectId,
      });
    } else {
      await updateProjectEndpointApi(endpointForm.id, payload);
    }

    MessagePlugin.success('端配置已保存');
    endpointVisible.value = false;
    await loadData();
  } finally {
    endpointSaving.value = false;
  }
}

function generateEndpointCode(type: string, name: string) {
  const typePart = (type || 'endpoint').trim().toUpperCase().replaceAll(/[^0-9A-Z]+/g, '-');
  const namePart = name.trim().toUpperCase().replaceAll(/[^0-9A-Z]+/g, '-');
  const seed = Date.now().toString(36).toUpperCase();
  return [typePart || 'ENDPOINT', namePart, seed].filter(Boolean).join('-');
}

function resetModuleForm(projectId = selectedProjectId.value, endpointId = '') {
  Object.assign(moduleForm, {
    code: '',
    description: '',
    developerIds: [],
    endpointId,
    id: '',
    name: '',
    ownerId: '',
    projectId,
    sort: '',
    status: 'active',
    testerIds: [],
  });
}

function openModuleCreate(endpointId: string) {
  resetModuleForm(selectedProjectId.value, endpointId);
  moduleMode.value = 'create';
  moduleVisible.value = true;
}

function openModuleEdit(module: SprintMvpApi.FeatureModule) {
  Object.assign(moduleForm, {
    code: module.code,
    description: module.description || '',
    developerIds: [...(module.developerIds || [])],
    endpointId: module.endpointId,
    id: module.id,
    name: module.name,
    ownerId: module.ownerId || '',
    projectId: module.projectId,
    sort: String(module.sort ?? ''),
    status: module.status,
    testerIds: [...(module.testerIds || [])],
  });
  moduleMode.value = 'edit';
  moduleVisible.value = true;
}

async function saveModule() {
  if (moduleSaving.value) return;
  if (!(await validateForm(moduleFormRef.value))) return;
  if (!moduleForm.projectId || !moduleForm.endpointId || !moduleForm.name.trim()) {
    MessagePlugin.warning('模块名称和所属端必填');
    return;
  }

  const payload = {
    description: moduleForm.description.trim() || undefined,
    developerIds: [...moduleForm.developerIds],
    name: moduleForm.name.trim(),
    ownerId: moduleForm.ownerId || undefined,
    sort: moduleForm.sort ? Number(moduleForm.sort) : undefined,
    status: moduleForm.status || undefined,
    testerIds: [...moduleForm.testerIds],
  };
  moduleSaving.value = true;
  try {
    if (moduleMode.value === 'create') {
      if (!moduleForm.code.trim()) {
        MessagePlugin.warning('模块编码必填');
        return;
      }

      await createFeatureModuleApi({
        ...payload,
        code: moduleForm.code.trim().toUpperCase(),
        endpointId: moduleForm.endpointId,
        projectId: moduleForm.projectId,
      });
    } else {
      await updateFeatureModuleApi(moduleForm.id, payload);
    }

    MessagePlugin.success('模块配置已保存');
    moduleVisible.value = false;
    ensurePager(moduleForm.endpointId).current = 1;
    await loadData();
  } finally {
    moduleSaving.value = false;
  }
}

function deleteModule(module: SprintMvpApi.FeatureModule) {
  if (isDeletingModule(module.id)) return;
  const requirementCount = moduleRequirementCount(module.id);
  if (requirementCount > 0) {
    MessagePlugin.warning(`模块已有 ${requirementCount} 个需求引用，不能删除`);
    return;
  }

  setDeletingModule(module.id, true);
  confirmAndClose({
    body: `确认删除模块 ${module.name}？`,
    confirmBtn: '删除',
    header: '删除模块',
    onClose: () => setDeletingModule(module.id, false),
    onConfirm: async () => {
      try {
        await deleteFeatureModuleApi(module.id);
        MessagePlugin.success('模块已删除');
        await loadData();
      } finally {
        setDeletingModule(module.id, false);
      }
    },
  });
}

async function loadData() {
  loading.value = true;
  try {
    [
      projects.value,
      endpoints.value,
      modules.value,
      requirements.value,
      users.value,
      skills.value,
    ] = await Promise.all([
      listProjectsApi(),
      listProjectEndpointsApi(),
      listFeatureModulesApi(),
      listRequirementsApi(),
      listUserOptionsApi(),
      listSkillsApi(true),
    ]);
    if (!selectedProjectId.value && projects.value.length > 0) {
      selectedProjectId.value = projects.value[0]!.id;
    }
  } finally {
    loading.value = false;
  }
}

watch(selectedProjectId, () => {
  for (const endpoint of selectedEndpoints.value) {
    ensurePager(endpoint.id).current = 1;
  }
});

onMounted(loadData);
</script>

<template>
  <ProjectContextListShell
    v-model:selected-project-id="selectedProjectId"
    class="multi-endpoint-page"
  >
    <template #title>
      多端管理
    </template>
    <template #description>
      按项目维护端和端下功能模块，配置负责人、研发人员和测试人员。
    </template>

    <template #actions>
        <TButton theme="primary" :disabled="!selectedProjectId" @click="openEndpointCreate">
            <template #icon>
              <IconifyIcon icon="lucide:plus" />
            </template>
            新增端
          </TButton>
      </template>

        <div v-if="!selectedProjectId" class="empty-state large">请选择左侧项目</div>
        <div v-else-if="selectedEndpoints.length === 0 && !loading" class="empty-state large">
          当前项目暂无端配置
        </div>

        <TCard
          v-for="endpoint in selectedEndpoints"
          :key="endpoint.id"
          class="endpoint-card"
          :bordered="true"
        >
          <template #title>
            <div class="endpoint-title">
              <div>
                <div class="endpoint-name-line">
                  <strong>{{ endpoint.name }}</strong>
                  <TLink theme="primary" title="编辑端" @click="openEndpointEdit(endpoint)">
                    <IconifyIcon icon="lucide:pencil" />
                  </TLink>
                </div>
                <span>{{ endpoint.code }} 路 {{ endpointTypeLabel(endpoint.type) }}</span>
              </div>
            </div>
          </template>
          <template #actions>
            <TLink theme="primary" @click="openModuleCreate(endpoint.id)">
              <IconifyIcon icon="lucide:plus" />
              新增模块
            </TLink>
          </template>

          <div class="endpoint-detail-grid">
            <div class="endpoint-basic-info">
              <dl>
                <div>
                  <dt>端编码</dt>
                  <dd>{{ endpoint.code }}</dd>
                </div>
                <div>
                  <dt>端类型</dt>
                  <dd>{{ endpointTypeLabel(endpoint.type) }}</dd>
                </div>
                <div>
                  <dt>负责人</dt>
                  <dd>{{ resolveUserName(endpoint.ownerId) }}</dd>
                </div>
                <div>
                  <dt>研发</dt>
                  <dd>{{ resolveUserNames(endpoint.developerIds) }}</dd>
                </div>
                <div>
                  <dt>测试</dt>
                  <dd>{{ resolveUserNames(endpoint.testerIds) }}</dd>
                </div>
                <div>
                  <dt>Skill</dt>
                  <dd>{{ resolveSkillNames(endpoint.skillIds) }}</dd>
                </div>
              </dl>
            </div>

            <div class="endpoint-module-list">
              <div class="module-table-head">
                <h4>模块管理</h4>
              </div>
              <TTable
                row-key="id"
                class="sprint-compact-table"
                :columns="withSerialColumn(moduleColumns, { offset: () => (ensurePager(endpoint.id).current - 1) * ensurePager(endpoint.id).pageSize })"
                :data="endpointPageModules(endpoint.id)"
                :loading="loading"
                size="small"
                hover
                stripe
              >
                <template #ownerId="{ row }">
                  {{ resolveUserName(row.ownerId) }}
                </template>
                <template #developerIds="{ row }">
                  {{ resolveUserNames(row.developerIds) }}
                </template>
                <template #testerIds="{ row }">
                  {{ resolveUserNames(row.testerIds) }}
                </template>
                <template #status="{ row }">
                  <TTag variant="light">{{ row.status }}</TTag>
                </template>
                <template #actions="{ row }">
                  <TSpace class="sprint-row-actions">
                    <TLink theme="primary" @click="openModuleEdit(row)">
                      <IconifyIcon icon="lucide:pencil" />
                      编辑
                    </TLink>
                    <TButton
                      v-if="canDeleteModule(row)"
                      size="small"
                      theme="danger"
                      variant="text"
                      :disabled="isDeletingModule(row.id)"
                      :loading="isDeletingModule(row.id)"
                      @click="deleteModule(row)"
                    >
                      <template #icon>
                        <IconifyIcon icon="lucide:trash-2" />
                      </template>
                      删除
                    </TButton>
                    <TTag v-else size="small" variant="light">
                      已关联 {{ moduleRequirementCount(row.id) }} 个需求
                    </TTag>
                  </TSpace>
                </template>
              </TTable>
              <div class="module-pagination">
                <TPagination
                  v-model="ensurePager(endpoint.id).current"
                  v-model:page-size="ensurePager(endpoint.id).pageSize"
                  :page-size-options="[30, 50, 100, 200]"
                  :total="endpointModules(endpoint.id).length"
                  size="small"
                  show-jumper
                  show-page-size
                  @change="handleEndpointPageChange(endpoint.id, $event)"
                />
              </div>
            </div>
          </div>
        </TCard>

    <TDrawer
      v-model:visible="endpointVisible"
      :size="'520px'"
      :header="endpointMode === 'create' ? '新增端' : '编辑端'"
      :confirm-btn="{ content: '保存', loading: endpointSaving }"
      @confirm="saveEndpoint"
    >
      <TForm ref="endpointFormRef" :data="endpointForm" :rules="endpointRules" label-width="90px">
        <TFormItem v-if="endpointMode === 'edit'" label="端编码" name="code">
          <TInput v-model="endpointForm.code" disabled />
        </TFormItem>
        <TFormItem label="端名称" name="name">
          <TInput v-model="endpointForm.name" placeholder="Web网站" />
        </TFormItem>
        <TFormItem label="端类型" name="type">
          <TSelect v-model="endpointForm.type" :options="endpointTypeOptions" />
        </TFormItem>
        <TFormItem label="负责人">
          <TSelect v-model="endpointForm.ownerId" clearable filterable :options="userOptions" />
        </TFormItem>
        <TFormItem label="研发人员">
          <TSelect v-model="endpointForm.developerIds" multiple filterable :options="userOptions" />
        </TFormItem>
        <TFormItem label="测试人员">
          <TSelect v-model="endpointForm.testerIds" multiple filterable :options="userOptions" />
        </TFormItem>
        <TFormItem label="Skill">
          <TSelect v-model="endpointForm.skillIds" multiple filterable :options="skillOptions">
            <template #option="{ option }">
              <SkillSelectOption :skill="option.skill" />
            </template>
          </TSelect>
        </TFormItem>
        <TFormItem label="状态">
          <TSelect v-model="endpointForm.status" :options="statusOptions" />
        </TFormItem>
        <TFormItem label="排序" name="sort">
          <TInput v-model="endpointForm.sort" placeholder="100" />
        </TFormItem>
      </TForm>
    </TDrawer>

    <TDrawer
      v-model:visible="moduleVisible"
      :size="'520px'"
      :header="moduleMode === 'create' ? '新增模块' : '编辑模块'"
      :confirm-btn="{ content: '保存', loading: moduleSaving }"
      @confirm="saveModule"
    >
      <TForm ref="moduleFormRef" :data="moduleForm" :rules="moduleRules" label-width="90px">
        <TFormItem label="所属端" name="endpointId">
          <TSelect
            v-model="moduleForm.endpointId"
            :disabled="moduleMode === 'edit'"
            :options="selectedEndpoints.map((item) => ({ label: item.name, value: item.id }))"
          />
        </TFormItem>
        <TFormItem label="模块编码" name="code">
          <TInput v-model="moduleForm.code" :disabled="moduleMode === 'edit'" placeholder="ORDER" />
        </TFormItem>
        <TFormItem label="模块名称" name="name">
          <TInput v-model="moduleForm.name" placeholder="订单管理" />
        </TFormItem>
        <TFormItem label="模块说明">
          <TTextarea v-model="moduleForm.description" :autosize="{ minRows: 3, maxRows: 5 }" />
        </TFormItem>
        <TFormItem label="负责人">
          <TSelect v-model="moduleForm.ownerId" clearable filterable :options="userOptions" />
        </TFormItem>
        <TFormItem label="研发人员">
          <TSelect v-model="moduleForm.developerIds" multiple filterable :options="userOptions" />
        </TFormItem>
        <TFormItem label="测试人员">
          <TSelect v-model="moduleForm.testerIds" multiple filterable :options="userOptions" />
        </TFormItem>
        <TFormItem label="状态">
          <TSelect v-model="moduleForm.status" :options="statusOptions" />
        </TFormItem>
        <TFormItem label="排序" name="sort">
          <TInput v-model="moduleForm.sort" placeholder="100" />
        </TFormItem>
      </TForm>
    </TDrawer>
  </ProjectContextListShell>
</template>

<style scoped>
.panel-title,
.workspace-head,
.module-table-head,
.endpoint-title {
  display: flex;
  gap: 12px;
  align-items: center;
  justify-content: space-between;
}

.panel-title {
  padding-bottom: 10px;
  margin-bottom: 10px;
  border-bottom: 1px solid var(--td-component-border);
}

.panel-title h3,
.workspace-head h3,
.module-table-head h4 {
  margin: 0;
  font-size: 16px;
}

.workspace-head {
  padding: 4px 4px 12px;
  border-bottom: 1px solid var(--td-component-border);
}

.workspace-head p {
  margin: 4px 0 0;
  color: var(--td-text-color-secondary);
}

.empty-state {
  padding: 14px;
  color: var(--td-text-color-secondary);
  background: var(--td-bg-color-container-hover);
  border-radius: 6px;
}

.empty-state.large {
  display: grid;
  min-height: 180px;
  place-items: center;
}

.endpoint-card :deep(.t-card__header) {
  min-height: 48px;
  padding: 12px 16px;
}

.endpoint-card :deep(.t-card__body) {
  padding: 12px 16px 14px;
}

.endpoint-title strong {
  display: block;
}

.endpoint-name-line {
  display: flex;
  gap: 6px;
  align-items: center;
}

.endpoint-name-line .t-link {
  display: inline-flex;
  width: 16px;
  height: 16px;
  align-items: center;
  justify-content: center;
  line-height: 1;
  font-size: 12px;
}

.endpoint-name-line .iconify {
  width: 12px;
  height: 12px;
}

.endpoint-title span {
  display: block;
  margin-top: 4px;
  color: var(--td-text-color-secondary);
  font-size: 12px;
}

.endpoint-detail-grid {
  display: grid;
  grid-template-columns: 300px minmax(0, 1fr);
  gap: 16px;
  align-items: stretch;
}

.endpoint-basic-info {
  min-width: 0;
  height: 100%;
  padding-right: 16px;
  border-right: 1px solid var(--td-component-border);
}

.endpoint-basic-info dl {
  display: grid;
  gap: 10px;
  margin: 0;
  font-size: 12px;
}

.endpoint-basic-info div {
  display: grid;
  grid-template-columns: 64px minmax(0, 1fr);
  gap: 8px;
  min-width: 0;
}

.endpoint-basic-info dt {
  color: var(--td-text-color-secondary);
}

.endpoint-basic-info dd {
  margin: 0;
  word-break: break-all;
}

.endpoint-module-list {
  display: flex;
  height: 100%;
  min-width: 0;
  flex-direction: column;
}

.endpoint-module-list :deep(.t-table) {
  flex: 1;
}

.module-table-head {
  padding: 0 0 10px;
}

.module-pagination {
  display: flex;
  justify-content: flex-end;
  padding-top: 10px;
  border-top: 1px solid var(--td-component-border);
}

@media (max-width: 640px) {
  .endpoint-detail-grid {
    grid-template-columns: 1fr;
  }

  .endpoint-basic-info {
    padding-right: 0;
    padding-bottom: 12px;
    border-right: 0;
    border-bottom: 1px solid var(--td-component-border);
  }
}
</style>
