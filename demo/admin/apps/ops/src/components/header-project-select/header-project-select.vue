<script lang="ts" setup>
import type { SprintMvpApi } from '#/api/sprint/mvp';

import { computed, onMounted, ref } from 'vue';

import { IconifyIcon } from '@vben/icons';

import {
  Button as TButton,
  Drawer as TDrawer,
  MessagePlugin,
  Tag as TTag,
  Tooltip as TTooltip,
} from 'tdesign-vue-next';

import { useProjectContextStore } from '#/store/project-context';

defineOptions({ name: 'HeaderProjectSelect' });

defineProps<{
  hideTrigger?: boolean;
}>();

const drawerVisible = ref(false);
const pendingProjectId = ref('');
const projectContext = useProjectContextStore();

const selectedProject = computed(() => projectContext.selectedProject);
const triggerTitle = computed(() =>
  selectedProject.value
    ? `${selectedProject.value.name} (${selectedProject.value.code})`
    : '选择项目',
);

async function loadProjects(force = false) {
  try {
    await projectContext.loadProjects(force);
  } catch {
    MessagePlugin.error('项目列表加载失败');
  }
}

async function openDrawer() {
  pendingProjectId.value = projectContext.selectedProjectId;
  drawerVisible.value = true;
  await loadProjects();
}

async function refreshProjects() {
  await loadProjects(true);
}

function hiddenTech(project: SprintMvpApi.Project) {
  return projectTech(project)
    .slice(3)
    .map((item) => `${item.scope}: ${item.name}`);
}

function projectStatusTheme(status: string) {
  return status === 'active' ? 'success' : 'default';
}

function cancelSelect() {
  pendingProjectId.value = projectContext.selectedProjectId;
  drawerVisible.value = false;
}

function confirmSelect() {
  if (pendingProjectId.value) {
    projectContext.selectProject(pendingProjectId.value);
  }
  drawerVisible.value = false;
}

function selectProject(project: SprintMvpApi.Project) {
  pendingProjectId.value = project.id;
}

function splitTechStack(value?: string) {
  return (value || '')
    .split(/[\/,，、；;\n\r]+/)
    .map((item) => item.trim())
    .filter(Boolean);
}

function projectTech(project: SprintMvpApi.Project) {
  return [
    ...splitTechStack(project.frontendTechStack).map((name) => ({
      name,
      scope: '前端',
    })),
    ...splitTechStack(project.backendTechStack).map((name) => ({
      name,
      scope: '后端',
    })),
  ];
}

function visibleTech(project: SprintMvpApi.Project) {
  return projectTech(project).slice(0, 3);
}

defineExpose({ openDrawer });

onMounted(() => loadProjects());
</script>

<template>
  <button
    v-if="!hideTrigger"
    class="header-project-trigger"
    :title="triggerTitle"
    type="button"
    @click="openDrawer"
  >
    <IconifyIcon class="header-project-trigger__icon" icon="lucide:folder-kanban" />
    <span class="header-project-trigger__text">
      {{ selectedProject?.name || '选择项目' }}
    </span>
    <IconifyIcon class="header-project-trigger__chevron" icon="lucide:chevron-down" />
  </button>

  <TDrawer
    v-model:visible="drawerVisible"
    :cancel-btn="{ content: '取消' }"
    :close-btn="true"
    :confirm-btn="{ content: '确定', theme: 'primary' }"
    destroy-on-close
    header="选择项目"
    placement="right"
    size="420px"
    @cancel="cancelSelect"
    @close="cancelSelect"
    @confirm="confirmSelect"
  >
    <div class="project-drawer">
      <div class="project-drawer__toolbar">
        <div>
          <h3>项目列表</h3>
          <p>{{ selectedProject?.code || '请选择要操作的项目' }}</p>
        </div>
        <TButton
          shape="circle"
          size="small"
          variant="outline"
          :loading="projectContext.loading"
          title="刷新"
          @click="refreshProjects"
        >
          <IconifyIcon icon="lucide:refresh-cw" />
        </TButton>
      </div>

      <div
        v-if="projectContext.projects.length === 0 && !projectContext.loading"
        class="project-drawer__empty"
      >
        暂无项目数据
      </div>
      <div v-else class="project-drawer__list">
        <button
          v-for="project in projectContext.projects"
          :key="project.id"
          class="project-drawer-card"
          :class="{ active: project.id === pendingProjectId }"
          type="button"
          @click="selectProject(project)"
        >
          <span class="project-card-head">
            <strong>{{ project.name }}</strong>
            <TTag :theme="projectStatusTheme(project.status)" size="small" variant="light">
              {{ project.status }}
            </TTag>
          </span>
          <span class="project-card-code">{{ project.code }}</span>
          <span class="project-card-desc">
            {{ project.description || '暂无项目说明' }}
          </span>
          <span class="project-card-stack">
            <span class="project-tech-tags">
              <TTag
                v-for="tech in visibleTech(project)"
                :key="`${project.id}-${tech.scope}-${tech.name}`"
                theme="primary"
                size="small"
                :title="`${tech.scope}: ${tech.name}`"
                variant="light"
              >
                <span class="project-tech-tag-text" :title="`${tech.scope}: ${tech.name}`">
                  {{ tech.name }}
                </span>
              </TTag>
              <TTag
                v-if="visibleTech(project).length === 0"
                theme="primary"
                size="small"
                title="未配置技术栈"
                variant="light"
              >
                未配置技术栈
              </TTag>
              <TTooltip v-if="hiddenTech(project).length > 0" placement="top" theme="light">
                <template #content>
                  <div class="project-tech-tooltip">
                    <div v-for="tech in hiddenTech(project)" :key="tech">{{ tech }}</div>
                  </div>
                </template>
                <TTag class="project-tech-warning" theme="warning" size="small" variant="light">
                  <IconifyIcon icon="lucide:circle-alert" />
                </TTag>
              </TTooltip>
            </span>
          </span>
        </button>
      </div>
    </div>
  </TDrawer>
</template>

<style scoped>
.header-project-trigger {
  display: flex;
  width: min(30vw, 260px);
  min-width: 180px;
  height: 32px;
  align-items: center;
  gap: 6px;
  padding: 0 10px;
  margin-right: 12px;
  color: hsl(var(--foreground));
  text-align: left;
  background: hsl(var(--accent));
  border: 0;
  border-radius: 16px;
  cursor: pointer;
}

.header-project-trigger__icon,
.header-project-trigger__chevron {
  flex: 0 0 auto;
  width: 16px;
  height: 16px;
  color: hsl(var(--muted-foreground));
}

.header-project-trigger__text {
  display: block;
  flex: 1 1 auto;
  min-width: 0;
  overflow: hidden;
  font-size: 12px;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.header-project-trigger__chevron {
  margin-left: auto;
}

.project-drawer {
  display: flex;
  height: 100%;
  min-height: 0;
  flex-direction: column;
  gap: 12px;
}

.project-drawer__toolbar {
  display: flex;
  gap: 12px;
  align-items: center;
  justify-content: space-between;
  padding-bottom: 12px;
  border-bottom: 1px solid var(--td-component-border);
}

.project-drawer__toolbar h3 {
  margin: 0;
  font-size: 16px;
  line-height: 22px;
}

.project-drawer__toolbar p {
  margin: 4px 0 0;
  color: var(--td-text-color-secondary);
}

.project-drawer__list {
  display: flex;
  min-height: 0;
  flex: 1;
  flex-direction: column;
  gap: 10px;
  overflow-x: hidden;
  overflow-y: auto;
  padding-right: 4px;
}

.project-drawer-card {
  display: flex;
  width: 100%;
  flex-direction: column;
  gap: 8px;
  padding: 12px;
  color: var(--td-text-color-primary);
  text-align: left;
  background: var(--td-bg-color-container);
  border: 1px solid var(--td-component-border);
  border-radius: 6px;
  cursor: pointer;
  transition:
    border-color 0.2s ease,
    box-shadow 0.2s ease,
    background-color 0.2s ease;
}

.project-drawer-card:hover,
.project-drawer-card.active {
  background: var(--td-success-color-light);
  border-color: var(--td-success-color);
  box-shadow: 0 2px 8px rgb(0 0 0 / 6%);
}

.project-card-head {
  display: flex;
  gap: 8px;
  align-items: center;
  justify-content: space-between;
}

.project-card-head strong {
  min-width: 0;
  overflow: hidden;
  font-size: 14px;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.project-card-code,
.project-card-desc,
.project-card-stack {
  color: var(--td-text-color-secondary);
  font-size: 12px;
  line-height: 1.5;
}

.project-card-desc {
  display: -webkit-box;
  min-height: 36px;
  overflow: hidden;
  -webkit-box-orient: vertical;
  -webkit-line-clamp: 2;
}

.project-tech-tags {
  display: flex;
  min-width: 0;
  flex-wrap: nowrap;
  gap: 5px;
  align-items: center;
}

.project-tech-tags .t-tag {
  display: inline-flex;
  width: auto;
  max-width: 70px;
  flex: 0 1 auto;
  height: 20px;
  min-width: 0;
  overflow: hidden;
  padding: 0 6px;
  font-size: 11px;
  line-height: 18px;
}

.project-tech-tags .t-tag > span,
.project-tech-tags .t-tag__text,
.project-tech-tag-text {
  display: block;
  width: 100%;
  max-width: 100%;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.project-tech-warning {
  width: 22px;
  max-width: 22px;
  flex: 0 0 22px;
  justify-content: center;
  padding: 0 4px;
}

.project-tech-warning .iconify {
  width: 13px;
  height: 13px;
}

.project-tech-tooltip {
  display: grid;
  gap: 4px;
  max-width: 260px;
}

.project-drawer__empty {
  padding: 14px;
  color: var(--td-text-color-secondary);
  background: var(--td-bg-color-container-hover);
  border-radius: 6px;
}

@media (max-width: 768px) {
  .header-project-trigger {
    width: 172px;
    min-width: 0;
    margin-right: 4px;
  }
}
</style>
