import { beforeEach, describe, expect, it, vi } from 'vitest';

const requestClient = { delete: vi.fn(), get: vi.fn(), post: vi.fn() };

vi.mock('#/api/request', () => ({ requestClient }));

describe('OpenManagementApi', () => {
  beforeEach(() => {
    requestClient.delete.mockReset();
    requestClient.get.mockReset();
    requestClient.post.mockReset();
  });

  it('keeps the authorization endpoint and legacy pagination fields', async () => {
    requestClient.post.mockResolvedValue({
      Count: 1,
      List: [{ id: 'auth-1' }],
    });
    const { OpenManagementApi } = await import('./management');

    await expect(
      OpenManagementApi.appAuthorization.query({
        appId: 'app-1',
        current: 2,
        externalInterfaceId: 'ext-1',
        keyword: 'key',
        pageSize: 20,
      }),
    ).resolves.toEqual({
      list: [{ id: 'auth-1' }],
      total: 1,
    });

    expect(requestClient.post).toHaveBeenCalledWith(
      '/v1/open/config/app_auth/query',
      {
        appId: 'app-1',
        externalInterfaceId: 'ext-1',
        keyword: 'key',
        limit: 20,
        page: 2,
      },
    );
  });

  it('keeps mapping and route remove URLs and HTTP methods', async () => {
    requestClient.delete.mockResolvedValue(true);
    const { OpenManagementApi } = await import('./management');

    await OpenManagementApi.internalInterfaces.remove('inner-1');
    await OpenManagementApi.externalInterfaces.remove('outer-1');
    await OpenManagementApi.routes.remove('route-1');

    expect(requestClient.delete).toHaveBeenNthCalledWith(
      1,
      '/v1/open/mapping/inter_interface/remove/inner-1',
    );
    expect(requestClient.delete).toHaveBeenNthCalledWith(
      2,
      '/v1/open/mapping/ext_interface/remove/outer-1',
    );
    expect(requestClient.delete).toHaveBeenNthCalledWith(
      3,
      '/v1/security/system/approute/remove/route-1',
    );
  });
});
