import { beforeEach, describe, expect, it, vi } from 'vitest';

const requestClient = {
  get: vi.fn(),
};

vi.mock('#/api/request', () => ({ requestClient }));

describe('getAuthorityAccountApi', () => {
  beforeEach(() => {
    vi.resetModules();
    requestClient.get.mockReset();
    sessionStorage.clear();
  });

  it('reuses the successful account response after a module reload in the same browser session', async () => {
    requestClient.get.mockResolvedValue({
      account: 'admin',
      authoritys: [],
      currentAppId: 'system-app',
      id: 'user-1',
    });
    sessionStorage.setItem('app_config', JSON.stringify({ appId: 'system-app' }));

    const authority = await import('./authority');
    authority.clearAuthorityAccountCache();
    const account = await authority.getAuthorityAccountApi();

    expect(account.account).toBe('admin');
    expect(requestClient.get).toHaveBeenCalledTimes(1);

    vi.resetModules();
    const reloadedAuthority = await import('./authority');
    const cachedAccount = await reloadedAuthority.getAuthorityAccountApi();

    expect(cachedAccount).toEqual(account);
    expect(requestClient.get).toHaveBeenCalledTimes(1);
  });

  it('reloads the database authority snapshot when cached routes use the retired room components', async () => {
    sessionStorage.setItem('app_config', JSON.stringify({ appId: 'system-app' }));
    sessionStorage.setItem(
      'air-cloud-system:authority-account',
      JSON.stringify({
        account: {
          account: 'admin',
          authoritys: [
            {
              component: '@room/views/system/region',
              id: 'region',
              path: '/system/region',
              title: 'Region',
              type: 1,
            },
          ],
          id: 'user-1',
        },
        appId: 'system-app',
      }),
    );
    requestClient.get.mockResolvedValue({
      account: 'admin',
      authoritys: [
        {
          component: '/system/regions',
          id: 'regions',
          path: '/system/regions',
          title: 'Regions',
          type: 1,
        },
      ],
      id: 'user-1',
    });

    const authority = await import('./authority');

    await expect(authority.getAuthorityAccountApi()).resolves.toMatchObject({
      authoritys: [{ component: '/system/regions' }],
    });
    expect(requestClient.get).toHaveBeenCalledTimes(1);
  });

});
