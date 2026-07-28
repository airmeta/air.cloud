import type { RouteRecordRaw } from 'vue-router';

const routes: RouteRecordRaw[] = [
  {
    name: 'Workspace',
    path: '/dashboard/workspace',
    component: () => import('#/views/dashboard/workspace/index.vue'),
    meta: {
      affixTab: true,
      icon: 'lucide:panel-top',
      order: 0,
      title: 'Workspace',
    },
  },
];

export default routes;
