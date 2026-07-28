<script lang="ts" setup>
import type { SprintMvpApi, SprintUserApi } from '#/api/sprint/mvp';

import { IconifyIcon } from '@vben/icons';
import { computed, nextTick, onMounted, ref } from 'vue';
import { useRoute } from 'vue-router';

import {
  Button as TButton,
  Descriptions as TDescriptions,
  DescriptionsItem as TDescriptionsItem,
  Empty as TEmpty,
  Table as TTable,
  Tag as TTag,
} from 'tdesign-vue-next';

import {
  listBugsApi,
  listDevelopmentTasksApi,
  listProjectsApi,
  listRequirementsApi,
  listUserOptionsApi,
} from '#/api/sprint/mvp';
import {
  listGitAccountsApi,
  listGitRepositoriesApi,
  type SprintGitApi,
} from '#/api/sprint/git';
import { formatDateTime } from '#/views/_shared/date-format';
import { withSerialColumn } from '#/views/_shared/table-columns';
import { buildUserLookup, resolveUserName } from '../_shared/user-display';
import ProjectMaterialsPanel from './project-materials-panel.vue';

import '../_shared/table-layout.css';

const route = useRoute();
const loading = ref(false);
const project = ref<SprintMvpApi.Project>();
const requirements = ref<SprintMvpApi.Requirement[]>([]);
const tasks = ref<SprintMvpApi.DevelopmentTask[]>([]);
const bugs = ref<SprintMvpApi.Bug[]>([]);
const users = ref<SprintUserApi.UserOption[]>([]);
const gitAccounts = ref<SprintGitApi.GitAccount[]>([]);
const gitRepositories = ref<SprintGitApi.GitRepository[]>([]);
const materialsPanelRef = ref<HTMLElement>();

const requirementColumns = [
  { colKey: 'title', title: '需求' },
  { colKey: 'status', title: '状态', width: 130 },
  { colKey: 'createdBy', title: '产品经理', width: 140 },
];
const taskColumns = [
  { colKey: 'title', title: '任务' },
  { colKey: 'status', title: '状态', width: 130 },
  { colKey: 'assigneeId', title: '负责人', width: 140 },
];
const bugColumns = [
  { colKey: 'title', title: '缺陷' },
  { colKey: 'status', title: '状态', width: 130 },
  { colKey: 'createdBy', title: '提交人', width: 140 },
];

const summary = computed(() => ({
  bugCount: bugs.value.length,
  openBugCount: bugs.value.filter((item) => item.status !== 'closed').length,
  requirementCount: requirements.value.length,
  taskCount: tasks.value.length,
}));
const userLookup = computed(() => buildUserLookup(users.value));

function resolveGitAccountName(id?: string) {
  if (!id) return '未配置';
  const account = gitAccounts.value.find((item) => item.id === id);
  return account ? `${account.name} (${account.username})` : id;
}

function resolveGitRepositoryName(id?: string) {
  if (!id) return '未配置';
  const repository = gitRepositories.value.find((item) => item.id === id);
  return repository ? `${repository.name} (${repository.code})` : id;
}

function resolveUserNames(ids?: string[] | null) {
  if (!ids || ids.length === 0) return '未配置';
  return ids.map((id) => resolveUserName(id, userLookup.value)).join('、');
}

async function loadDetail() {
  loading.value = true;
  try {
    const projectId = String(route.params.id || '');
    const projects = await listProjectsApi();
    project.value = projects.find((item) => item.id === projectId);
    if (!project.value) return;

    [requirements.value, tasks.value, bugs.value, users.value, gitAccounts.value, gitRepositories.value] = await Promise.all([
      listRequirementsApi(projectId),
      listDevelopmentTasksApi({ projectId }),
      listBugsApi(projectId),
      listUserOptionsApi(),
      listGitAccountsApi(),
      listGitRepositoriesApi(),
    ]);
  } finally {
    loading.value = false;
  }
}

async function scrollToInitialSection() {
  if (route.query.section !== 'materials') return;
  await nextTick();
  materialsPanelRef.value?.scrollIntoView({ behavior: 'smooth', block: 'start' });
}

onMounted(async () => {
  await loadDetail();
  await scrollToInitialSection();
});
</script>

<template>
  <div class="detail-page">
    <section class="header">
      <div>
        <h2>{{ project?.name || '项目详情' }}</h2>
        <p>{{ project?.code || '未找到项目' }}</p>
      </div>
      <TButton @click="loadDetail">
        <template #icon>
          <IconifyIcon icon="lucide:refresh-cw" />
        </template>
        刷新
      </TButton>
    </section>

    <TEmpty v-if="!loading && !project" description="项目不存在或已被删除" />

    <template v-else-if="project">
      <section class="panel">
        <TDescriptions bordered :column="2">
          <TDescriptionsItem label="项目编码">{{ project.code }}</TDescriptionsItem>
          <TDescriptionsItem label="项目名称">{{ project.name }}</TDescriptionsItem>
          <TDescriptionsItem label="状态">
            <TTag theme="success" variant="light">{{ project.status }}</TTag>
          </TDescriptionsItem>
          <TDescriptionsItem label="AI 平台">{{ project.aiPlatformCode || '未配置' }}</TDescriptionsItem>
          <TDescriptionsItem label="Git 仓库">{{ resolveGitRepositoryName(project.gitRepositoryId) }}</TDescriptionsItem>
          <TDescriptionsItem label="Git 账号">{{ resolveGitAccountName(project.gitAccountId) }}</TDescriptionsItem>
          <TDescriptionsItem label="测试环境">
            {{ project.testEnvironmentUrl || '未配置' }}
          </TDescriptionsItem>
          <TDescriptionsItem label="测试环境 ID">{{ project.testEnvironmentId || '未配置' }}</TDescriptionsItem>
          <TDescriptionsItem label="详细信息" :span="2">{{ project.description || '未填写' }}</TDescriptionsItem>
          <TDescriptionsItem label="前端技术栈">{{ project.frontendTechStack || '未配置' }}</TDescriptionsItem>
          <TDescriptionsItem label="后端技术栈">{{ project.backendTechStack || '未配置' }}</TDescriptionsItem>
          <TDescriptionsItem label="项目经理">{{ resolveUserName(project.projectManagerId, userLookup) }}</TDescriptionsItem>
          <TDescriptionsItem label="产品经理">{{ resolveUserNames(project.productManagerIds) }}</TDescriptionsItem>
          <TDescriptionsItem label="研发人员">{{ resolveUserNames(project.developerIds) }}</TDescriptionsItem>
          <TDescriptionsItem label="测试人员">{{ resolveUserNames(project.testerIds) }}</TDescriptionsItem>
          <TDescriptionsItem label="架构师">{{ resolveUserName(project.architectId, userLookup) }}</TDescriptionsItem>
          <TDescriptionsItem label="创建人">{{ resolveUserName(project.createdBy, userLookup) }}</TDescriptionsItem>
          <TDescriptionsItem label="创建时间">{{ formatDateTime(project.createTime) }}</TDescriptionsItem>
        </TDescriptions>
      </section>

      <section class="stats">
        <div>
          <span>需求</span>
          <strong>{{ summary.requirementCount }}</strong>
        </div>
        <div>
          <span>任务</span>
          <strong>{{ summary.taskCount }}</strong>
        </div>
        <div>
          <span>缺陷</span>
          <strong>{{ summary.bugCount }}</strong>
        </div>
        <div>
          <span>未关闭缺陷</span>
          <strong>{{ summary.openBugCount }}</strong>
        </div>
      </section>

      <section class="panel">
        <h3>需求</h3>
        <TTable row-key="id" class="sprint-compact-table" :columns="withSerialColumn(requirementColumns)" :data="requirements" hover stripe>
          <template #createdBy="{ row }">
            {{ resolveUserName(row.createdBy, userLookup) }}
          </template>
        </TTable>
      </section>

      <section class="panel">
        <h3>任务</h3>
        <TTable row-key="id" class="sprint-compact-table" :columns="withSerialColumn(taskColumns)" :data="tasks" hover stripe>
          <template #assigneeId="{ row }">
            {{ resolveUserName(row.assigneeId, userLookup, '未指派') }}
          </template>
        </TTable>
      </section>

      <section class="panel">
        <h3>缺陷</h3>
        <TTable row-key="id" class="sprint-compact-table" :columns="withSerialColumn(bugColumns)" :data="bugs" hover stripe>
          <template #createdBy="{ row }">
            {{ resolveUserName(row.createdBy, userLookup) }}
          </template>
        </TTable>
      </section>

      <section ref="materialsPanelRef" class="panel">
        <ProjectMaterialsPanel :project="project" :users="users" />
      </section>
    </template>
  </div>
</template>

<style scoped>
.detail-page {
  padding: 16px;
}

.header,
.panel,
.stats {
  margin-bottom: 16px;
  padding: 16px 20px;
  background: var(--td-bg-color-container);
  border: 1px solid var(--td-component-border);
  border-radius: 6px;
}

.header {
  display: flex;
  gap: 16px;
  align-items: center;
  flex-wrap: wrap;
  justify-content: space-between;
}

.header h2,
.panel h3 {
  margin: 0;
}

.header p {
  margin: 6px 0 0;
  color: var(--td-text-color-secondary);
}

.panel h3 {
  margin-bottom: 12px;
}

.repo-detail {
  display: inline-block;
  max-width: 100%;
  overflow: hidden;
  text-overflow: ellipsis;
  vertical-align: bottom;
  white-space: nowrap;
}

.stats {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 14px;
}

.stats span {
  display: block;
  color: var(--td-text-color-secondary);
}

.stats strong {
  display: block;
  margin-top: 6px;
  font-size: 24px;
}

@media (max-width: 960px) {
  .stats {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 560px) {
  .stats {
    grid-template-columns: 1fr;
  }
}
</style>
