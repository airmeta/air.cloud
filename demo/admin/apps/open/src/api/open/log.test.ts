import { beforeEach, describe, expect, it, vi } from 'vitest';

const requestClient = {
  get: vi.fn(),
};

vi.mock('#/api/request', () => ({ requestClient }));

describe('LogApi', () => {
  beforeEach(() => {
    requestClient.get.mockReset();
  });

  it('preserves the legacy log-page path and all query parameters', async () => {
    requestClient.get.mockResolvedValue({ list: [], total: 0 });
    const { LogApi } = await import('./log');
    const query = {
      createTime: ['2025-06-01 00:00:00', '2025-06-02 00:00:00'] as [
        string,
        string,
      ],
      keywords: '订单',
      pageNum: 2,
      pageSize: 20,
    };

    await LogApi.getPage(query);

    expect(requestClient.get).toHaveBeenCalledWith('/api/v1/logs/page', {
      params: query,
    });
  });

  it('uses the original visit statistics and trend contracts', async () => {
    requestClient.get.mockResolvedValue({});
    const { LogApi } = await import('./log');

    await LogApi.getVisitStats();
    await LogApi.getVisitTrend({
      endDate: '2025-06-07',
      startDate: '2025-06-01',
    });

    expect(requestClient.get).toHaveBeenNthCalledWith(
      1,
      '/api/v1/logs/visit-stats',
    );
    expect(requestClient.get).toHaveBeenNthCalledWith(
      2,
      '/api/v1/logs/visit-trend',
      { params: { endDate: '2025-06-07', startDate: '2025-06-01' } },
    );
  });
});
