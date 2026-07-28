import { beforeEach, describe, expect, it, vi } from 'vitest';

const requestClient = { delete: vi.fn(), get: vi.fn(), post: vi.fn() };
vi.mock('#/api/request', () => ({ requestClient }));

describe('AppRouteApi', () => {
  beforeEach(() => {
    requestClient.delete.mockReset();
    requestClient.post.mockReset();
  });

  it('keeps the AppRoute list endpoint and normalizes its page response', async () => {
    requestClient.post.mockResolvedValue({
      Count: 2,
      List: [{ appId: 'app-1', id: 'route-1', route: '/v1/a' }],
    });
    const { AppRouteApi } = await import('./routes');

    await expect(
      AppRouteApi.query({ current: 2, keyword: '/v1', pageSize: 50 }),
    ).resolves.toEqual({
      list: [{ appId: 'app-1', id: 'route-1', route: '/v1/a' }],
      total: 2,
    });
    expect(requestClient.post).toHaveBeenCalledWith(
      '/v1/security/system/approute/query/route',
      { Info: '/v1', Limit: 50, Page: 2 },
    );
  });

  it('uses the backend delete contract', async () => {
    requestClient.delete.mockResolvedValue(true);
    const { AppRouteApi } = await import('./routes');
    await AppRouteApi.remove('route-1');
    expect(requestClient.delete).toHaveBeenCalledWith(
      '/v1/security/system/approute/remove/route-1',
    );
  });
});
