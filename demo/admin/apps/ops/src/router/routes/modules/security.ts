import type { RouteRecordRaw } from 'vue-router';

const routes: RouteRecordRaw[] = [
  {
    meta: {
      icon: 'lucide:shield-check',
      order: 95,
      title: '安全管理',
    },
    name: 'Security',
    path: '/security',
    redirect: '/system/agent-tokens',
    children: [
      {
        name: 'SystemAgentTokens',
        path: '/system/agent-tokens',
        component: () => import('#/views/system/agent-tokens/index.vue'),
        meta: {
          icon: 'lucide:key-square',
          keepAlive: false,
          title: '令牌管理',
        },
      },
    ],
  },
];

export default routes;
