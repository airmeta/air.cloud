import { describe, expect, it } from 'vitest';

import { generateRoutesByBackend } from './generate-routes-backend';

describe('generateRoutesByBackend', () => {
  it('resolves migrated Vben application and region menu components', async () => {
    const applicationPage = () => null;
    const regionPage = () => null;

    const routes = await generateRoutesByBackend({
      fetchMenuListAsync: async () => [
        {
          children: [
            {
              component: '/system/apps',
              name: 'Applications',
              path: '/system/apps',
            },
            {
              component: '/system/regions',
              name: 'Regions',
              path: '/system/regions',
            },
          ],
          component: 'BasicLayout',
          name: 'System',
          path: '/system',
        },
      ],
      layoutMap: { BasicLayout: applicationPage },
      pageMap: {
        '/system/apps/index.vue': applicationPage,
        '/system/regions/index.vue': regionPage,
      },
    });

    expect(routes[0]?.children?.[0]?.component).toBe(applicationPage);
    expect(routes[0]?.children?.[1]?.component).toBe(regionPage);
  });
});
