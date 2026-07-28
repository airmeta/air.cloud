import type { RouteRecordRaw } from 'vue-router';

const routes: RouteRecordRaw[] = [
  {
    meta: {
      icon: 'lucide:settings',
      order: 90,
      title: '系统管理',
    },
    name: 'System',
    path: '/system',
    redirect: '/system/users',
    children: [
      {
        name: 'SystemUsers',
        path: '/system/users',
        component: () => import('#/views/system/users/index.vue'),
        meta: {
          icon: 'lucide:users',
          keepAlive: false,
          title: '用户管理',
        },
      },
      {
        name: 'SystemRoles',
        path: '/system/roles',
        component: () => import('#/views/system/roles/index.vue'),
        meta: {
          icon: 'lucide:shield-check',
          keepAlive: false,
          title: '角色管理',
        },
      },
      {
        name: 'SystemRoleAuthorize',
        path: '/system/roles/authorize/:id',
        component: () => import('#/views/system/roles/authorize.vue'),
        meta: {
          activePath: '/system/roles',
          hideInMenu: true,
          title: '角色授权',
        },
      },
      {
        name: 'SystemMenus',
        path: '/system/menus',
        component: () => import('#/views/system/menus/index.vue'),
        meta: {
          icon: 'lucide:menu',
          keepAlive: false,
          title: '菜单管理',
        },
      },
      {
        name: 'SystemDictionaries',
        path: '/system/dictionaries',
        component: () => import('#/views/system/dictionaries/index.vue'),
        meta: {
          icon: 'lucide:book-open-text',
          keepAlive: false,
          title: '字典管理',
        },
      },
      {
        name: 'SystemConfigurations',
        path: '/system/configurations',
        component: () => import('#/views/system/configurations/index.vue'),
        meta: {
          icon: 'lucide:sliders-horizontal',
          keepAlive: false,
          title: '系统配置',
        },
      },
      {
        name: 'SystemDepartments',
        path: '/system/departments',
        component: () => import('#/views/system/departments/index.vue'),
        meta: {
          icon: 'lucide:network',
          keepAlive: false,
          title: '部门管理',
        },
      },
      {
        name: 'SystemAssignments',
        path: '/system/assignments',
        component: () => import('#/views/system/assignments/index.vue'),
        meta: {
          icon: 'lucide:briefcase-business',
          keepAlive: false,
          title: '岗位管理',
        },
      },
    ],
  },
];

export default routes;
