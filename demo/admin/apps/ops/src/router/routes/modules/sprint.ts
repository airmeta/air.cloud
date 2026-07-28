import type { RouteRecordRaw } from 'vue-router';

const routes: RouteRecordRaw[] = [
  {
    name: 'ProjectGroup',
    path: '/sprint/project',
    redirect: '/sprint/projects',
    meta: {
      icon: 'lucide:folder-kanban',
      order: 10,
      title: '项目管理',
    },
    children: [
      {
        name: 'SprintProjects',
        path: '/sprint/projects',
        component: () => import('#/views/sprint/projects/index.vue'),
        meta: {
          affixTab: true,
          icon: 'lucide:folder-kanban',
          title: '项目配置',
        },
      },
      {
        name: 'SprintMultiEndpoints',
        path: '/sprint/multi-endpoints',
        component: () => import('#/views/sprint/multi-endpoints/index.vue'),
        meta: {
          icon: 'lucide:layout-dashboard',
          title: '多端管理',
        },
      },
      {
        name: 'SprintProjectDetail',
        path: '/sprint/projects/detail/:id',
        component: () => import('#/views/sprint/projects/detail.vue'),
        meta: {
          activePath: '/sprint/projects',
          hideInMenu: true,
          title: '项目详情',
        },
      },
    ],
  },
  {
    name: 'GitManagementGroup',
    path: '/sprint/git',
    redirect: '/sprint/git/accounts',
    meta: {
      icon: 'lucide:git-branch',
      order: 15,
      title: 'Git管理',
    },
    children: [
      {
        name: 'SprintGitAccounts',
        path: '/sprint/git/accounts',
        component: () => import('#/views/sprint/git/accounts/index.vue'),
        meta: {
          icon: 'lucide:key-round',
          title: 'Git账户管理',
        },
      },
      {
        name: 'SprintGitRepositories',
        path: '/sprint/git/repositories',
        component: () => import('#/views/sprint/git/repositories/index.vue'),
        meta: {
          icon: 'lucide:git-fork',
          title: 'Git仓库管理',
        },
      },
    ],
  },
  {
    name: 'ProductGroup',
    path: '/sprint/product',
    redirect: '/sprint/requirements',
    meta: {
      icon: 'lucide:list-checks',
      order: 20,
      title: '产品管理',
    },
    children: [
      {
        name: 'SprintRequirements',
        path: '/sprint/requirements',
        component: () => import('#/views/sprint/requirements/index.vue'),
        meta: {
          icon: 'lucide:list-checks',
          title: '需求管理',
        },
      },
      {
        name: 'SprintRequirementDetail',
        path: '/sprint/requirements/detail/:id',
        component: () => import('#/views/sprint/requirements/detail.vue'),
        meta: {
          activePath: '/sprint/requirements',
          hideInMenu: true,
          title: '需求详情',
        },
      },
      {
        name: 'SprintRequirementReviews',
        path: '/sprint/reviews',
        component: () => import('#/views/sprint/reviews/index.vue'),
        meta: {
          icon: 'lucide:clipboard-check',
          title: '需求评审',
        },
      },
      {
        name: 'SprintAiChat',
        path: '/sprint/ai-chat',
        component: () => import('#/views/sprint/ai-chat/index.vue'),
        meta: {
          icon: 'lucide:message-circle',
          title: 'AI对话',
        },
      },
    ],
  },
  {
    name: 'WorkerGroup',
    path: '/sprint/worker',
    redirect: '/sprint/my-tasks',
    meta: {
      icon: 'lucide:workflow',
      order: 30,
      title: '研发执行',
    },
    children: [
      {
        name: 'SprintMyTasks',
        path: '/sprint/my-tasks',
        component: () => import('#/views/sprint/my-tasks/index.vue'),
        meta: {
          icon: 'lucide:user-check',
          title: '我的任务',
        },
      },
      {
        name: 'SprintTasks',
        path: '/sprint/tasks',
        component: () => import('#/views/sprint/tasks/index.vue'),
        meta: {
          icon: 'lucide:layout-list',
          title: '任务大厅',
        },
      },
      {
        name: 'SprintTaskDetail',
        path: '/sprint/tasks/detail/:id',
        component: () => import('#/views/sprint/tasks/detail.vue'),
        meta: {
          activePath: '/sprint/tasks',
          hideInMenu: true,
          title: '任务详情',
        },
      },
    ],
  },
  {
    name: 'TestGroup',
    path: '/sprint/test',
    redirect: '/sprint/tests',
    meta: {
      icon: 'lucide:test-tube-2',
      order: 40,
      title: '测试验证',
    },
    children: [
      {
        name: 'SprintTests',
        path: '/sprint/tests',
        component: () => import('#/views/sprint/tests/index.vue'),
        meta: {
          icon: 'lucide:test-tube-2',
          title: '测试计划',
        },
      },
      {
        name: 'SprintDefects',
        path: '/sprint/defects',
        component: () => import('#/views/sprint/defects/index.vue'),
        meta: {
          icon: 'lucide:bug',
          title: '缺陷跟踪',
        },
      },
      {
        name: 'SprintDefectDetail',
        path: '/sprint/defects/detail/:id',
        component: () => import('#/views/sprint/defects/detail.vue'),
        meta: {
          activePath: '/sprint/defects',
          hideInMenu: true,
          title: '缺陷详情',
        },
      },
    ],
  },
];

export default routes;
