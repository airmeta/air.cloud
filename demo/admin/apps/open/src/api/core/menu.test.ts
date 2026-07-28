import { describe, expect, it } from 'vitest';

import {
  convertAuthoritysToVbenRoutes,
  getFirstAccessibleMenuPath,
  hasAccessibleMenuPath,
} from './menu';

describe('convertAuthoritysToVbenRoutes', () => {
  it('converts database authority records into Vben routes and omits button nodes', () => {
    const routes = convertAuthoritysToVbenRoutes([
      {
        id: 'system',
        parentId: '0',
        path: '/system',
        title: 'System',
        type: 0,
      },
      {
        component: '/system/users',
        hide: 0,
        icon: 'lucide:users',
        id: 'users',
        parentId: 'system',
        path: '/system/users',
        sortNumber: 2,
        title: 'Users',
        type: 1,
      },
      {
        authority: 'System:User:Create',
        id: 'create-user',
        parentId: 'users',
        path: '',
        title: 'Create user',
        type: 2,
      },
    ]);

    expect(routes).toEqual([
      {
        children: [
          {
            children: [],
            component: '/system/users',
            meta: {
              hideInMenu: false,
              icon: 'lucide:users',
              order: 2,
              title: 'Users',
            },
            name: 'Authorityusers',
            path: '/system/users',
          },
        ],
        component: 'BasicLayout',
        meta: {
          hideInMenu: false,
          icon: undefined,
          order: undefined,
          title: 'System',
        },
        name: 'Authoritysystem',
        path: '/system',
      },
    ]);
  });

  it('keeps hidden route menus in routes so direct navigation can still resolve', () => {
    const routes = convertAuthoritysToVbenRoutes([
      {
        component: '/system/roles/authorize',
        hide: 1,
        id: 'role-authorize',
        path: '/system/roles/authorize/:id',
        title: 'Role authorize',
        type: 2,
      },
    ]);

    expect(routes[0]).toMatchObject({
      component: '/system/roles/authorize',
      meta: { hideInMenu: true },
    });
  });

  it('selects the first visible Vben menu using database sort order', () => {
    const routes = convertAuthoritysToVbenRoutes([
      {
        id: 'system',
        path: '/system',
        sortNumber: 1,
        title: 'System',
        type: 0,
      },
      {
        component: '/system/roles',
        id: 'roles',
        parentId: 'system',
        path: '/system/roles',
        sortNumber: 2,
        title: 'Roles',
        type: 1,
      },
      {
        component: '/system/users',
        id: 'users',
        parentId: 'system',
        path: '/system/users',
        sortNumber: 1,
        title: 'Users',
        type: 1,
      },
    ]);

    expect(getFirstAccessibleMenuPath(routes)).toBe('/system/users');
    expect(hasAccessibleMenuPath(routes, '/system/roles')).toBe(true);
    expect(
      hasAccessibleMenuPath(
        [
          ...routes,
          {
            component: '/system/roles/authorize',
            name: 'Authorize',
            path: '/system/roles/authorize/:id',
          },
        ],
        '/system/roles/authorize/495bde08f21846d98eae3fb63cbdfa38',
      ),
    ).toBe(true);
    expect(hasAccessibleMenuPath(routes, '/system/missing')).toBe(false);
  });
});
