import { describe, expect, it } from 'vitest';

import { coreRoutes } from './core';

describe('SSO logout route', () => {
  it('exposes a public history route for cross-application logout', () => {
    const logoutRoute = coreRoutes.find((route) => route.name === 'SsoLogout');

    expect(logoutRoute).toMatchObject({
      meta: { ignoreAccess: true },
      path: '/logout',
    });
  });
});
