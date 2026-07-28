import type { SprintMvpApi } from '#/api/sprint/mvp';

import { computed, ref } from 'vue';

import { defineStore } from 'pinia';

import { listProjectsApi } from '#/api/sprint/mvp';

const SELECTED_PROJECT_STORAGE_KEY = 'agentsprint:selected-project-id';

export const useProjectContextStore = defineStore('project-context', () => {
  const loading = ref(false);
  const loaded = ref(false);
  const projects = ref<SprintMvpApi.Project[]>([]);
  const selectedProjectId = ref(
    localStorage.getItem(SELECTED_PROJECT_STORAGE_KEY) || '',
  );

  const selectedProject = computed(() =>
    projects.value.find((project) => project.id === selectedProjectId.value),
  );

  function selectProject(projectId: string) {
    selectedProjectId.value = projectId;
    if (projectId) {
      localStorage.setItem(SELECTED_PROJECT_STORAGE_KEY, projectId);
    } else {
      localStorage.removeItem(SELECTED_PROJECT_STORAGE_KEY);
    }
  }

  async function loadProjects(force = false) {
    if (loading.value) return;
    if (loaded.value && !force) return;

    loading.value = true;
    try {
      const rows = await listProjectsApi();
      projects.value = rows;
      loaded.value = true;

      const selectedExists = rows.some(
        (project) => project.id === selectedProjectId.value,
      );
      if (!selectedExists) {
        selectProject(rows[0]?.id || '');
      }
    } finally {
      loading.value = false;
    }
  }

  return {
    loadProjects,
    loaded,
    loading,
    projects,
    selectProject,
    selectedProject,
    selectedProjectId,
  };
});
