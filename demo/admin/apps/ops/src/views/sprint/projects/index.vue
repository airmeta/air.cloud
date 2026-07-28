<script lang="ts" setup>
import type { SprintMvpApi, SprintUserApi } from '#/api/sprint/mvp';
import type { FormInstanceFunctions, FormRules } from 'tdesign-vue-next';

import { IconifyIcon } from '@vben/icons';
import { computed, onMounted, reactive, ref } from 'vue';
import { useRouter } from 'vue-router';

import {
  Button as TButton,
  Card as TCard,
  Drawer as TDrawer,
  Form as TForm,
  FormItem as TFormItem,
  Input as TInput,
  Link as TLink,
  MessagePlugin,
  Select as TSelect,
  Tag as TTag,
  Textarea as TTextarea,
} from 'tdesign-vue-next';

import {
  createProjectApi,
  listProjectsApi,
  listUserOptionsApi,
  updateProjectApi,
} from '#/api/sprint/mvp';
import {
  listGitAccountsApi,
  listGitRepositoriesApi,
  type SprintGitApi,
} from '#/api/sprint/git';
import {
  listDictionaryItemsApi,
  listDictionaryTypesApi,
  listAiPlatformsApi,
  listRuntimeEnvironmentsApi,
  type SystemApi,
} from '#/api/system/management';
import {
  requiredArrayRule,
  requiredRule,
  validateForm,
} from '#/views/_shared/form-rules';
import { formatDateTime } from '#/views/_shared/date-format';
import ProjectMaterialsPanel from './project-materials-panel.vue';

const router = useRouter();

const creating = ref(false);
const loading = ref(false);
const saving = ref(false);
const drawerVisible = ref(false);
const drawerMode = ref<'detail' | 'edit' | 'materials' | 'stats'>('detail');
const createVisible = ref(false);
const createFormRef = ref<FormInstanceFunctions>();
const editFormRef = ref<FormInstanceFunctions>();
const createCodeSeed = ref('');
const selectedProject = ref<SprintMvpApi.Project>();
const projects = ref<SprintMvpApi.Project[]>([]);
const users = ref<SprintUserApi.UserOption[]>([]);
const runtimeEnvironments = ref<SystemApi.RuntimeEnvironment[]>([]);
const gitAccounts = ref<SprintGitApi.GitAccount[]>([]);
const gitRepositories = ref<SprintGitApi.GitRepository[]>([]);
const aiPlatforms = ref<SystemApi.AiPlatform[]>([]);
const frontendTechOptions = ref<SystemApi.DictionaryItem[]>([]);
const backendTechOptions = ref<SystemApi.DictionaryItem[]>([]);

const form = reactive({
  architectId: '',
  aiPlatformCode: '',
  backendTechStack: [] as string[],
  code: '',
  description: '',
  developerIds: [] as string[],
  frontendTechStack: [] as string[],
  gitAccountId: '',
  gitRepositoryId: '',
  name: '',
  productManagerIds: [] as string[],
  projectManagerId: '',
  testerIds: [] as string[],
  testEnvironmentId: '',
});
const editForm = reactive({
  architectId: '',
  aiPlatformCode: '',
  backendTechStack: [] as string[],
  description: '',
  developerIds: [] as string[],
  frontendTechStack: [] as string[],
  gitAccountId: '',
  gitRepositoryId: '',
  name: '',
  productManagerIds: [] as string[],
  projectManagerId: '',
  testerIds: [] as string[],
  testEnvironmentId: '',
});
const projectRules: FormRules<typeof form> = {
  architectId: requiredRule('请选择架构师', 'change'),
  aiPlatformCode: requiredRule('请选择AI平台', 'change'),
  backendTechStack: requiredArrayRule('请选择后端技术栈'),
  developerIds: requiredArrayRule('请选择至少一名研发人员'),
  frontendTechStack: requiredArrayRule('请选择前端技术栈'),
  name: requiredRule('请输入项目名称'),
  productManagerIds: requiredArrayRule('请选择至少一名产品经理'),
  projectManagerId: requiredRule('请选择项目经理', 'change'),
  gitRepositoryId: requiredRule('请选择Git仓库', 'change'),
  testerIds: requiredArrayRule('请选择至少一名测试人员'),
  testEnvironmentId: requiredRule('请选择测试环境', 'change'),
};
const activeProjects = computed(
  () => projects.value.filter((item) => item.status === 'active').length,
);
const userOptions = computed(() =>
  users.value.map((user) => ({ label: `${user.displayName} (${user.username})`, value: user.id })),
);
const generatedProjectCode = computed(() => generateProjectCode(form.name));
const runtimeEnvironmentOptions = computed(() =>
  runtimeEnvironments.value
    .filter((environment) => environment.status === 1 && environment.environmentType === 'test')
    .map((environment) => ({
      label: `${environment.name} (${environment.code})`,
      value: environment.id,
    })),
);
const gitAccountOptions = computed(() =>
  gitAccounts.value
    .filter((account) => account.status === 'active')
    .map((account) => ({
      label: `${account.name} (${account.username})`,
      value: account.id,
    })),
);
const gitRepositoryOptions = computed(() =>
  gitRepositories.value
    .filter((repository) => repository.status === 'active')
    .map((repository) => ({
      label: `${repository.name} (${repository.code})`,
      value: repository.id,
    })),
);
const frontendTechSelectOptions = computed(() => dictionaryOptions(frontendTechOptions.value));
const backendTechSelectOptions = computed(() => dictionaryOptions(backendTechOptions.value));
const aiPlatformOptions = computed(() =>
  aiPlatforms.value
    .filter((platform) => platform.status === 1)
    .sort((left, right) => left.sort - right.sort || left.code.localeCompare(right.code))
    .map((platform) => ({
      label: `${platform.name} (${platform.code} / ${platform.model})`,
      value: platform.code,
    })),
);

function resetCreateForm() {
  Object.assign(form, {
    architectId: '',
    aiPlatformCode: '',
    backendTechStack: [],
    code: '',
    description: '',
    developerIds: [],
    frontendTechStack: [],
    gitAccountId: '',
    gitRepositoryId: '',
    name: '',
    productManagerIds: [],
    projectManagerId: '',
    testerIds: [],
    testEnvironmentId: '',
  });
}

function openCreate() {
  resetCreateForm();
  createCodeSeed.value = Date.now().toString(36).toUpperCase();
  createVisible.value = true;
}

function openProjectDetail(project: SprintMvpApi.Project) {
  router.push(`/sprint/projects/detail/${project.id}`);
}

function openProjectMaterials(project: SprintMvpApi.Project) {
  router.push({
    path: `/sprint/projects/detail/${project.id}`,
    query: { section: 'materials' },
  });
}

function openDrawer(project: SprintMvpApi.Project, mode: typeof drawerMode.value) {
  selectedProject.value = project;
  drawerMode.value = mode;
  if (mode === 'edit') {
    Object.assign(editForm, {
      architectId: project.architectId || '',
      aiPlatformCode: project.aiPlatformCode || '',
      backendTechStack: deserializeValues(project.backendTechStack),
      description: project.description || '',
      developerIds: [...(project.developerIds || [])],
      frontendTechStack: deserializeValues(project.frontendTechStack),
      gitAccountId: project.gitAccountId || '',
      gitRepositoryId: project.gitRepositoryId || '',
      name: project.name,
      productManagerIds: [...(project.productManagerIds || [])],
      projectManagerId: project.projectManagerId || '',
      testerIds: [...(project.testerIds || [])],
      testEnvironmentId: project.testEnvironmentId || '',
    });
  }
  drawerVisible.value = true;
}

async function loadProjects() {
  loading.value = true;
  try {
    [
      projects.value,
      users.value,
      runtimeEnvironments.value,
      gitAccounts.value,
      gitRepositories.value,
      aiPlatforms.value,
    ] = await Promise.all([
      listProjectsApi(),
      listUserOptionsApi(),
      listRuntimeEnvironmentsApi(),
      listGitAccountsApi(),
      listGitRepositoriesApi(),
      listAiPlatformsApi({ status: 1 }),
    ]);
  } finally {
    loading.value = false;
  }
}

async function loadDictionaries() {
  const types = await listDictionaryTypesApi();
  const frontendType = types.find((item) => item.code === 'frontend_tech_stack');
  const backendType = types.find((item) => item.code === 'backend_tech_stack');
  const [frontendItems, backendItems] = await Promise.all([
    frontendType ? listDictionaryItemsApi(frontendType.id) : Promise.resolve([]),
    backendType ? listDictionaryItemsApi(backendType.id) : Promise.resolve([]),
  ]);
  frontendTechOptions.value = frontendItems;
  backendTechOptions.value = backendItems;
}

function resolveGitRepositoryName(id?: string) {
  if (!id) return '未配置';
  const repository = gitRepositories.value.find((item) => item.id === id);
  return repository ? `${repository.name} (${repository.code})` : id;
}

function dictionaryOptions(items: SystemApi.DictionaryItem[]) {
  return items
    .filter((item) => item.status === 1)
    .sort((left, right) => left.sort - right.sort || left.code.localeCompare(right.code))
    .map((item) => ({ label: item.name, value: item.code }));
}

function deserializeValues(value?: string) {
  return value?.split(',').map((item) => item.trim()).filter(Boolean) || [];
}

function serializeValues(values: string[]) {
  return values.join(',');
}


function resolveRuntimeEnvironmentUrl(id?: string) {
  const environment = runtimeEnvironments.value.find((item) => item.id === id);
  return environment?.frontendUrl || environment?.apiBaseUrl || environment?.mcpEndpoint || undefined;
}

function generateProjectCode(name: string) {
  const normalized = name
    .trim()
    .toUpperCase()
    .replaceAll(/[^0-9A-Z]+/g, '-')
    .replaceAll(/^-+|-+$/g, '');
  return `${normalized || 'PROJECT'}-${createCodeSeed.value || Date.now().toString(36).toUpperCase()}`;
}

function normalizeProjectPayload(source: typeof form | typeof editForm) {
  const repository = gitRepositories.value.find((item) => item.id === source.gitRepositoryId);
  if (!repository) {
    MessagePlugin.warning('请选择Git仓库');
    return;
  }

  if (
    !source.name.trim() ||
    source.frontendTechStack.length === 0 ||
    source.backendTechStack.length === 0 ||
    !source.projectManagerId ||
    !source.aiPlatformCode ||
    source.productManagerIds.length === 0 ||
    source.developerIds.length === 0 ||
    source.testerIds.length === 0 ||
    !source.architectId
  ) {
    MessagePlugin.warning('项目名称、AI平台、技术栈和项目团队均为必填');
    return;
  }

  return {
    architectId: source.architectId,
    aiPlatformCode: source.aiPlatformCode,
    backendTechStack: serializeValues(source.backendTechStack),
    description: source.description.trim() || undefined,
    developerIds: [...source.developerIds],
    frontendTechStack: serializeValues(source.frontendTechStack),
    gitAccountId: source.gitAccountId || repository.gitAccountId || undefined,
    gitRepositoryId: repository.id,
    name: source.name.trim(),
    productManagerIds: [...source.productManagerIds],
    projectManagerId: source.projectManagerId,
    testerIds: [...source.testerIds],
    testEnvironmentId: source.testEnvironmentId || undefined,
    testEnvironmentUrl: resolveRuntimeEnvironmentUrl(source.testEnvironmentId),
  };
}

async function createProject() {
  if (creating.value) return;
  if (!(await validateForm(createFormRef.value))) return;
  const payload = normalizeProjectPayload(form);
  if (!payload) return;

  creating.value = true;
  try {
    await createProjectApi({
      ...payload,
      code: generatedProjectCode.value,
    });
    MessagePlugin.success('项目已创建');
    resetCreateForm();
    createVisible.value = false;
    await loadProjects();
  } finally {
    creating.value = false;
  }
}

async function saveProject() {
  if (saving.value) return;
  if (!selectedProject.value) return;
  if (!(await validateForm(editFormRef.value))) return;
  const payload = normalizeProjectPayload(editForm);
  if (!payload) return;

  saving.value = true;
  try {
    selectedProject.value = await updateProjectApi(selectedProject.value.id, payload);
    MessagePlugin.success('项目已保存');
    drawerVisible.value = false;
    await loadProjects();
  } finally {
    saving.value = false;
  }
}

onMounted(async () => {
  await Promise.all([loadProjects(), loadDictionaries()]);
});
</script>

<template>
  <div class="project-page">
    <section class="toolbar">
      <div>
        <h2>项目管理</h2>
        <p>按项目聚合需求、任务、测试和缺陷。</p>
      </div>
      <div class="toolbar-stats">
        <div>
          <span>项目数</span>
          <strong>{{ projects.length }}</strong>
        </div>
        <div>
          <span>推进中</span>
          <strong>{{ activeProjects }}</strong>
        </div>
        <TButton theme="primary" :disabled="loading" @click="openCreate">
          <template #icon>
            <IconifyIcon icon="lucide:plus" />
          </template>
          新增项目
        </TButton>
      </div>
    </section>

    <section class="project-grid" :class="{ loading }">
      <div v-if="!loading && projects.length === 0" class="project-empty">
        <h3>暂无项目数据</h3>
        <p>先新建项目，再维护端、功能模块、需求和任务闭环。</p>
        <TButton theme="primary" :disabled="loading" @click="openCreate">
          <template #icon>
            <IconifyIcon icon="lucide:plus" />
          </template>
          新建项目
        </TButton>
      </div>
      <TCard
        v-for="project in projects"
        :key="project.id"
        class="project-card"
        :bordered="true"
      >
        <div class="card-main">
          <div>
            <span class="code">{{ project.code }}</span>
            <h3>{{ project.name }}</h3>
          </div>
          <TTag theme="success" variant="light">{{ project.status }}</TTag>
        </div>
        <p class="repo">{{ resolveGitRepositoryName(project.gitRepositoryId) }}</p>
        <div class="card-actions">
          <TLink theme="primary" @click="openDrawer(project, 'edit')">
            <IconifyIcon icon="lucide:pencil" />
            编辑
          </TLink>
          <TLink theme="primary" @click="openProjectDetail(project)">
            <IconifyIcon icon="lucide:eye" />
            详情
          </TLink>
          <TLink theme="primary" @click="openProjectMaterials(project)">
            <IconifyIcon icon="lucide:folder-open" />
            材料
          </TLink>
          <TLink theme="primary" @click="openDrawer(project, 'stats')">
            <IconifyIcon icon="lucide:bar-chart-3" />
            统计
          </TLink>
        </div>
      </TCard>
    </section>

    <TDrawer
      v-model:visible="drawerVisible"
      :footer="drawerMode === 'edit'"
      :size="'60%'"
      :header="selectedProject?.name || '项目详情'"
      :confirm-btn="{ content: '保存', loading: saving }"
      @confirm="saveProject"
    >
      <div v-if="selectedProject" class="drawer-content">
        <template v-if="drawerMode === 'stats'">
          <div class="stats-grid">
            <div class="stat-box">
              <span>项目状态</span>
              <strong>{{ selectedProject.status }}</strong>
            </div>
            <div class="stat-box">
              <span>创建人</span>
              <strong>{{ selectedProject.createdBy }}</strong>
            </div>
            <div class="stat-box">
              <span>创建时间</span>
              <strong>{{ formatDateTime(selectedProject.createTime) }}</strong>
            </div>
          </div>
        </template>
        <template v-else-if="drawerMode === 'materials'">
          <ProjectMaterialsPanel :project="selectedProject" :users="users" />
        </template>
        <template v-else-if="drawerMode === 'edit'">
          <TForm ref="editFormRef" :data="editForm" :rules="projectRules" label-width="100px">
            <TFormItem label="项目编码">
              <TInput :value="selectedProject.code" disabled />
            </TFormItem>
            <TFormItem label="项目名称" name="name">
              <TInput v-model="editForm.name" />
            </TFormItem>
            <TFormItem label="详细信息">
              <TTextarea v-model="editForm.description" :autosize="{ minRows: 3, maxRows: 6 }" />
            </TFormItem>
            <TFormItem label="Git仓库" name="gitRepositoryId">
              <TSelect
                v-model="editForm.gitRepositoryId"
                filterable
                :options="gitRepositoryOptions"
              />
            </TFormItem>
            <TFormItem label="Git账户">
              <TSelect
                v-model="editForm.gitAccountId"
                clearable
                filterable
                :options="gitAccountOptions"
              />
            </TFormItem>
            <TFormItem label="AI平台" name="aiPlatformCode">
              <TSelect
                v-model="editForm.aiPlatformCode"
                filterable
                :options="aiPlatformOptions"
              />
            </TFormItem>
            <TFormItem label="测试环境" name="testEnvironmentId">
              <TSelect
                v-model="editForm.testEnvironmentId"
                clearable
                filterable
                :options="runtimeEnvironmentOptions"
              />
            </TFormItem>
            <TFormItem label="前端技术栈" name="frontendTechStack">
              <TSelect
                v-model="editForm.frontendTechStack"
                multiple
                filterable
                :options="frontendTechSelectOptions"
              />
            </TFormItem>
            <TFormItem label="后端技术栈" name="backendTechStack">
              <TSelect
                v-model="editForm.backendTechStack"
                multiple
                filterable
                :options="backendTechSelectOptions"
              />
            </TFormItem>
            <TFormItem label="项目经理" name="projectManagerId">
              <TSelect v-model="editForm.projectManagerId" filterable :options="userOptions" />
            </TFormItem>
            <TFormItem label="产品经理" name="productManagerIds">
              <TSelect v-model="editForm.productManagerIds" multiple filterable :options="userOptions" />
            </TFormItem>
            <TFormItem label="研发人员" name="developerIds">
              <TSelect v-model="editForm.developerIds" multiple filterable :options="userOptions" />
            </TFormItem>
            <TFormItem label="测试人员" name="testerIds">
              <TSelect v-model="editForm.testerIds" multiple filterable :options="userOptions" />
            </TFormItem>
            <TFormItem label="架构师" name="architectId">
              <TSelect v-model="editForm.architectId" filterable :options="userOptions" />
            </TFormItem>
          </TForm>
        </template>
      </div>
    </TDrawer>

    <TDrawer
      v-model:visible="createVisible"
      :size="'60%'"
      header="新增项目"
      :confirm-btn="{ content: '保存', loading: creating }"
      @confirm="createProject"
    >
      <TForm ref="createFormRef" :data="form" :rules="projectRules" label-width="100px">
        <TFormItem label="项目名称" name="name">
          <TInput v-model="form.name" placeholder="AgentSprint" />
        </TFormItem>
        <TFormItem label="详细信息">
          <TTextarea
            v-model="form.description"
            :autosize="{ minRows: 3, maxRows: 6 }"
            placeholder="项目背景、交付范围和关键约束"
          />
        </TFormItem>
        <TFormItem label="Git仓库" name="gitRepositoryId">
          <TSelect
            v-model="form.gitRepositoryId"
            filterable
            :options="gitRepositoryOptions"
          />
        </TFormItem>
        <TFormItem label="Git账户">
          <TSelect
            v-model="form.gitAccountId"
            clearable
            filterable
            :options="gitAccountOptions"
          />
        </TFormItem>
        <TFormItem label="AI平台" name="aiPlatformCode">
          <TSelect
            v-model="form.aiPlatformCode"
            filterable
            :options="aiPlatformOptions"
          />
        </TFormItem>
        <TFormItem label="测试环境" name="testEnvironmentId">
          <TSelect
            v-model="form.testEnvironmentId"
            clearable
            filterable
            :options="runtimeEnvironmentOptions"
          />
        </TFormItem>
        <TFormItem label="前端技术栈" name="frontendTechStack">
          <TSelect
            v-model="form.frontendTechStack"
            multiple
            filterable
            :options="frontendTechSelectOptions"
          />
        </TFormItem>
        <TFormItem label="后端技术栈" name="backendTechStack">
          <TSelect
            v-model="form.backendTechStack"
            multiple
            filterable
            :options="backendTechSelectOptions"
          />
        </TFormItem>
        <TFormItem label="项目经理" name="projectManagerId">
          <TSelect v-model="form.projectManagerId" filterable :options="userOptions" />
        </TFormItem>
        <TFormItem label="产品经理" name="productManagerIds">
          <TSelect v-model="form.productManagerIds" multiple filterable :options="userOptions" />
        </TFormItem>
        <TFormItem label="研发人员" name="developerIds">
          <TSelect v-model="form.developerIds" multiple filterable :options="userOptions" />
        </TFormItem>
        <TFormItem label="测试人员" name="testerIds">
          <TSelect v-model="form.testerIds" multiple filterable :options="userOptions" />
        </TFormItem>
        <TFormItem label="架构师" name="architectId">
          <TSelect v-model="form.architectId" filterable :options="userOptions" />
        </TFormItem>
      </TForm>
    </TDrawer>
  </div>
</template>

<style scoped>
.project-page {
  display: flex;
  flex-direction: column;
  gap: 16px;
  padding: 16px;
}

.toolbar {
  display: flex;
  gap: 16px;
  align-items: center;
  flex-wrap: wrap;
  justify-content: space-between;
  padding: 16px 20px;
  background: var(--td-bg-color-container);
  border: 1px solid var(--td-component-border);
  border-radius: 6px;
}

.toolbar h2 {
  margin: 0;
  font-size: 20px;
}

.toolbar p {
  margin: 6px 0 0;
  color: var(--td-text-color-secondary);
}

.toolbar-stats {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 24px;
}

.toolbar-stats span {
  display: block;
  color: var(--td-text-color-secondary);
  font-size: 12px;
}

.toolbar-stats strong {
  display: block;
  margin-top: 4px;
  font-size: 22px;
}

.project-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 300px));
  gap: 16px;
}

.project-empty {
  grid-column: 1 / -1;
  display: grid;
  gap: 10px;
  justify-items: start;
  padding: 28px;
  background: var(--td-bg-color-container);
  border: 1px solid var(--td-component-border);
  border-radius: 6px;
}

.project-empty h3,
.project-empty p {
  margin: 0;
}

.project-empty p {
  color: var(--td-text-color-secondary);
}

.project-card {
  width: 100%;
  max-width: 300px;
  height: 150px;
}

.project-card :deep(.t-loading__parent) {
  height: 100%;
}

.project-card :deep(.t-card__body) {
  box-sizing: border-box;
  display: flex;
  flex-direction: column;
  height: 100%;
  padding: 14px 16px 12px;
}

.card-main,
.card-actions {
  display: flex;
  gap: 12px;
}

.card-main {
  align-items: flex-start;
  justify-content: space-between;
  min-height: 46px;
}

.card-actions {
  align-items: center;
  justify-content: flex-start;
  padding-top: 10px;
  margin-top: auto;
  line-height: 1.6;
  border-top: 1px solid var(--td-component-border);
}

.card-actions :deep(.t-link) {
  min-height: 22px;
}

.code {
  display: block;
  max-width: 210px;
  overflow: hidden;
  color: var(--td-text-color-secondary);
  font-size: 12px;
  text-overflow: ellipsis;
  white-space: nowrap;
}

h3 {
  margin: 4px 0 0;
  overflow: hidden;
  font-size: 16px;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.repo {
  height: 18px;
  margin: 8px 0 10px;
  overflow: hidden;
  color: var(--td-text-color-secondary);
  font-size: 12px;
  line-height: 18px;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.drawer-content dl {
  display: grid;
  grid-template-columns: 100px minmax(0, 1fr);
  gap: 12px;
}

.drawer-content dt {
  color: var(--td-text-color-secondary);
}

.drawer-content dd {
  min-width: 0;
  margin: 0;
  word-break: break-all;
}

.drawer-content .repo-detail {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  word-break: normal;
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 16px;
}

.stat-box {
  padding: 14px;
  border: 1px solid var(--td-component-border);
  border-radius: 6px;
}

.stat-box span {
  display: block;
  margin-bottom: 8px;
  color: var(--td-text-color-secondary);
}

.stat-box strong {
  word-break: break-all;
}

.hint {
  color: var(--td-text-color-secondary);
}

@media (max-width: 720px) {
  .project-grid {
    grid-template-columns: 1fr;
  }

  .project-card {
    max-width: none;
  }

  .stats-grid {
    grid-template-columns: 1fr;
  }
}
</style>
