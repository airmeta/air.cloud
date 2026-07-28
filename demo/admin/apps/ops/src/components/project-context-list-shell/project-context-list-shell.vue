<script lang="ts" setup>
import type { SprintMvpApi } from '#/api/sprint/mvp';

import { computed, onMounted, ref, watch } from 'vue';

import { IconifyIcon } from '@vben/icons';

import HeaderProjectSelect from '#/components/header-project-select/header-project-select.vue';
import { useProjectContextStore } from '#/store/project-context';

import { Button as TButton } from 'tdesign-vue-next';

defineOptions({ name: 'ProjectContextListShell' });

const props = withDefaults(
  defineProps<{
    description?: string;
    loading?: boolean;
    selectedProjectId?: string;
    title?: string;
  }>(),
  {
    description: '',
    loading: false,
    selectedProjectId: '',
    title: '',
  },
);

const emit = defineEmits<{
  'project-change': [project: SprintMvpApi.Project];
  refresh: [];
  'update:selectedProjectId': [projectId: string];
}>();

const projectContext = useProjectContextStore();
const pageProjectSelectRef = ref<InstanceType<typeof HeaderProjectSelect>>();

const selectedProjectId = computed(() => projectContext.selectedProjectId);
const selectedProject = computed(() => projectContext.selectedProject);
const selectedProjectCode = computed(() => selectedProject.value?.code || '-');
const selectedProjectName = computed(() => selectedProject.value?.name || '请选择项目');

function openProjectSelectDrawer() {
  pageProjectSelectRef.value?.openDrawer();
}

function syncExternalProjectId(projectId: string) {
  if (projectId && projectId !== projectContext.selectedProjectId) {
    projectContext.selectProject(projectId);
  }
}

watch(
  () => props.selectedProjectId,
  (projectId) => {
    syncExternalProjectId(projectId || '');
  },
  { immediate: true },
);

watch(
  () => projectContext.selectedProjectId,
  (projectId, previousProjectId) => {
    if (!projectId || projectId === props.selectedProjectId) return;
    emit('update:selectedProjectId', projectId);

    const project = projectContext.projects.find((item) => item.id === projectId);
    if (project && projectId !== previousProjectId) {
      emit('project-change', project);
    }
  },
  { immediate: true },
);

onMounted(() => projectContext.loadProjects());
</script>

<template>
  <div class="project-context-list-shell sprint-list-page">
    <section class="sprint-page-title project-context-list-shell__header">
      <div class="project-context-list-shell__title">
        <h2>
          <span class="project-context-list-shell__heading-text">
            <span>{{ selectedProjectName }}</span>
            <slot name="title">
              {{ title }}
            </slot>
            <span>({{ selectedProjectCode }})</span>
          </span>
          <TButton
            class="project-context-list-shell__switch"
            shape="circle"
            size="small"
            theme="default"
            title="切换项目"
            variant="outline"
            @click="openProjectSelectDrawer"
          >
            <IconifyIcon icon="ant-design:swap-outlined" />
          </TButton>
        </h2>
        <p>
          <slot name="description">
            {{ description }}
          </slot>
        </p>
      </div>
      <slot name="actions" :project="selectedProject" :project-id="selectedProjectId" />
    </section>

    <HeaderProjectSelect ref="pageProjectSelectRef" hide-trigger />

    <main class="sprint-project-workspace project-context-list-shell__workspace">
      <slot name="workspace-header" :project="selectedProject" :project-id="selectedProjectId" />

      <slot :project="selectedProject" :project-id="selectedProjectId" />
    </main>
  </div>
</template>

<style scoped>
.project-context-list-shell {
  height: 100%;
  min-height: 0;
}

.project-context-list-shell__header {
  display: flex;
  gap: 16px;
  align-items: center;
  justify-content: space-between;
}

.project-context-list-shell__title {
  min-width: 0;
}

.project-context-list-shell__title h2 {
  display: flex;
  max-width: 100%;
  align-items: center;
  gap: 8px;
}

.project-context-list-shell__heading-text {
  display: inline-flex;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.project-context-list-shell__title h2 :deep(*),
.project-context-list-shell__heading-text > span {
  min-width: 0;
}

.project-context-list-shell__switch {
  width: 20px;
  height: 20px;
  min-width: 20px;
  flex: 0 0 auto;
  padding: 0;
}

.project-context-list-shell__switch .iconify {
  width: 11px;
  height: 11px;
}

.project-context-list-shell__workspace {
  flex: 1;
}
</style>
