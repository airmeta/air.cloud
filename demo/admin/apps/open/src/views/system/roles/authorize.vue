<script lang="ts" setup>
import { IconifyIcon } from '@vben/icons';
import { computed, nextTick, onBeforeUnmount, onMounted, reactive, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';

import {
  listSystemRoleMenusApi,
  listSystemRolesApi,
  saveSystemRoleMenusApi,
  type SystemApi,
} from '#/api';
import {
  Button as TButton,
  Checkbox as TCheckbox,
  Descriptions as TDescriptions,
  DescriptionsItem as TDescriptionsItem,
  Empty as TEmpty,
  Loading as TLoading,
  MessagePlugin,
  Space as TSpace,
} from 'tdesign-vue-next';

defineOptions({ name: 'SystemRoleAuthorize' });

type MenuAuthNode = SystemApi.Menu & {
  checked?: boolean;
  children: MenuAuthNode[];
  level: number;
};

const authTreeRef = ref<HTMLElement>();
const authTreeMaxHeight = ref(260);
const form = reactive<{ menuIds: string[] }>({ menuIds: [] });
const loading = ref(false);
const menus = ref<(SystemApi.Menu & { checked?: boolean })[]>([]);
const role = ref<SystemApi.Role>();
const route = useRoute();
const router = useRouter();
const saving = ref(false);

const roleId = computed(() => String(route.params.id || ''));
const menuAuthTree = computed(() => buildMenuAuthTree(menus.value));
const menuAuthRows = computed(() => flattenMenuAuthTree(menuAuthTree.value));

function buildMenuAuthTree(sourceMenus: (SystemApi.Menu & { checked?: boolean })[]) {
  const nodeMap = new Map<string, MenuAuthNode>();
  sourceMenus.forEach((menu) => {
    nodeMap.set(menu.id, { ...menu, children: [], level: 0 });
  });

  const roots: MenuAuthNode[] = [];
  nodeMap.forEach((node) => {
    if (node.parentId && nodeMap.has(node.parentId)) {
      nodeMap.get(node.parentId)?.children.push(node);
    } else {
      roots.push(node);
    }
  });

  function sortAndMarkLevel(nodes: MenuAuthNode[], level: number) {
    nodes.sort(
      (left, right) =>
        (left.sortNumber ?? 0) - (right.sortNumber ?? 0) ||
        String(left.title || '').localeCompare(String(right.title || '')),
    );
    nodes.forEach((node) => {
      node.level = level;
      sortAndMarkLevel(node.children, level + 1);
    });
  }

  sortAndMarkLevel(roots, 0);
  return roots;
}

function flattenMenuAuthTree(nodes: MenuAuthNode[]) {
  const rows: MenuAuthNode[] = [];
  function walk(items: MenuAuthNode[]) {
    items.forEach((item) => {
      rows.push(item);
      walk(item.children);
    });
  }
  walk(nodes);
  return rows;
}

function assignCheckedMenus(sourceMenus: (SystemApi.Menu & { checked?: boolean })[]) {
  form.menuIds = sourceMenus.filter((menu) => menu.checked).map((menu) => menu.id);
}

function isMenuChecked(menuId: string) {
  return form.menuIds.includes(menuId);
}

function toggleMenu(menuId: string, value: boolean | { checked?: boolean }) {
  const checked = typeof value === 'boolean' ? value : value.checked === true;
  const nextIds = new Set(form.menuIds);
  if (checked) {
    nextIds.add(menuId);
  } else {
    nextIds.delete(menuId);
  }
  form.menuIds = [...nextIds];
}

async function loadDetail() {
  loading.value = true;
  try {
    const [roleResult, roleMenus] = await Promise.all([
      listSystemRolesApi({ current: 1, pageSize: 9999 }),
      listSystemRoleMenusApi(roleId.value),
    ]);
    role.value = roleResult.list.find((item) => item.id === roleId.value);
    menus.value = roleMenus;
    assignCheckedMenus(roleMenus);
  } finally {
    loading.value = false;
    updateAuthTreeHeight();
  }
}

async function saveAuthorize() {
  if (saving.value || !role.value) return;
  saving.value = true;
  try {
    await saveSystemRoleMenusApi(role.value.id, form.menuIds);
    MessagePlugin.success('角色菜单授权已保存');
    await loadDetail();
  } finally {
    saving.value = false;
  }
}

function updateAuthTreeHeight() {
  void nextTick(() => {
    const tree = authTreeRef.value;
    if (!tree) return;
    authTreeMaxHeight.value = Math.max(180, window.innerHeight - tree.getBoundingClientRect().top - 24);
  });
}

onMounted(() => {
  void loadDetail();
  updateAuthTreeHeight();
  window.addEventListener('resize', updateAuthTreeHeight);
});

onBeforeUnmount(() => {
  window.removeEventListener('resize', updateAuthTreeHeight);
});
</script>

<template>
  <div class="role-authorize-page">
    <section class="page-header">
      <div>
        <h2>{{ role?.roleName || '角色授权' }}</h2>
        <p>通过后端 role-menu 接口读取当前角色菜单授权，并提交到 batch-change-menu。</p>
      </div>
      <TSpace>
        <TButton variant="outline" @click="router.push('/system/roles')">
          <template #icon><IconifyIcon icon="lucide:arrow-left" /></template>
          返回
        </TButton>
        <TButton @click="loadDetail">
          <template #icon><IconifyIcon icon="lucide:refresh-cw" /></template>
          刷新
        </TButton>
      </TSpace>
    </section>

    <TLoading :loading="loading" show-overlay>
      <TEmpty v-if="!loading && !role" description="角色不存在或已被删除" />
      <template v-else-if="role">
        <section class="panel">
          <h3>角色信息</h3>
          <TDescriptions bordered :column="2">
            <TDescriptionsItem label="角色名称">{{ role.roleName }}</TDescriptionsItem>
            <TDescriptionsItem label="说明">{{ role.description || '未填写' }}</TDescriptionsItem>
          </TDescriptions>
        </section>

        <section class="panel">
          <div class="panel-header">
            <h3>菜单授权</h3>
            <TButton theme="primary" :loading="saving" @click="saveAuthorize">
              <template #icon><IconifyIcon icon="lucide:save" /></template>
              保存授权
            </TButton>
          </div>
          <div ref="authTreeRef" class="auth-tree" :style="{ maxHeight: `${authTreeMaxHeight}px` }">
            <div v-for="menu in menuAuthRows" :key="menu.id" class="auth-menu-row">
              <div class="auth-menu-main" :style="{ paddingLeft: `${menu.level * 22}px` }">
                <TCheckbox :checked="isMenuChecked(menu.id)" @change="toggleMenu(menu.id, $event)" />
                <div class="auth-menu-text">
                  <strong>{{ menu.title }}</strong>
                  <span>{{ menu.path }}</span>
                </div>
              </div>
            </div>
            <TEmpty v-if="menuAuthRows.length === 0" description="暂无菜单数据" />
          </div>
        </section>
      </template>
    </TLoading>
  </div>
</template>

<style scoped>
.role-authorize-page { padding: 16px; }
.page-header,
.panel { margin-bottom: 16px; padding: 16px 20px; background: var(--td-bg-color-container); border: 1px solid var(--td-component-border); border-radius: 6px; }
.page-header,
.panel-header { display: flex; gap: 16px; align-items: center; flex-wrap: wrap; justify-content: space-between; }
.page-header h2,
.panel h3 { margin: 0; }
.page-header p { margin: 6px 0 0; color: var(--td-text-color-secondary); }
.panel h3 { margin-bottom: 12px; }
.panel-header h3 { margin-bottom: 0; }
.auth-tree { display: grid; gap: 8px; min-height: 180px; overflow-y: auto; padding-right: 6px; }
.auth-menu-row { min-height: 52px; padding: 8px 12px; background: var(--td-bg-color-container); border: 1px solid var(--td-component-border); border-radius: 6px; }
.auth-menu-main { display: flex; gap: 8px; align-items: center; }
.auth-menu-text { display: grid; min-width: 0; gap: 2px; }
.auth-menu-text strong,
.auth-menu-text span { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.auth-menu-text span { color: var(--td-text-color-secondary); }
</style>
