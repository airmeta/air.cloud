import { beforeEach, describe, expect, it, vi } from 'vitest';

const requestClient = {
  delete: vi.fn(),
  get: vi.fn(),
  post: vi.fn(),
};

vi.mock('#/api/request', () => ({ requestClient }));

describe('listApplicationsApi', () => {
  beforeEach(() => {
    requestClient.get.mockReset();
    requestClient.post.mockReset();
  });

  it('uses the System service POST body contract and preserves the server total', async () => {
    requestClient.post.mockResolvedValue({
      Count: 42,
      List: [{ appId: 'app-1', appName: 'System' }],
    });

    const { listApplicationsApi } = await import('./management');
    await expect(
      listApplicationsApi({ keyword: 'System', pageNum: 1, pageSize: 9999 }),
    ).resolves.toEqual({
      list: [{ appId: 'app-1', appName: 'System' }],
      total: 42,
    });

    expect(requestClient.post).toHaveBeenCalledWith(
      '/v1/security/system/app/query',
      {
        AppId: '4841de0858714c5e8bee36e2fad70614',
        Info: 'System',
        Limit: 9999,
        Page: 1,
      },
    );
    expect(requestClient.get).not.toHaveBeenCalled();
  });

  it('uses current and pageSize as the backend Page and Limit fields', async () => {
    requestClient.post.mockResolvedValue({ list: [], total: 0 });

    const { listApplicationsApi } = await import('./management');
    await listApplicationsApi({ current: 3, pageSize: 50 });

    expect(requestClient.post).toHaveBeenCalledWith(
      '/v1/security/system/app/query',
      {
        AppId: '4841de0858714c5e8bee36e2fad70614',
        Info: undefined,
        Limit: 50,
        Page: 3,
      },
    );
  });
});

describe('saveRegionApi', () => {
  beforeEach(() => {
    requestClient.post.mockReset();
  });

  it('uses the region save contract for a new root region', async () => {
    requestClient.post.mockResolvedValue(true);

    const { saveRegionApi } = await import('./management');
    await expect(
      saveRegionApi({ code: '340000', name: 'Anhui', type: 0 }),
    ).resolves.toMatchObject({ code: '340000', name: 'Anhui', type: 0 });

    expect(requestClient.post).toHaveBeenCalledWith(
      '/v1/security/system/region/save',
      {
        AppId: '4841de0858714c5e8bee36e2fad70614',
        Code: '340000',
        Description: undefined,
        Id: undefined,
        LngAndSat: '',
        Name: 'Anhui',
        ParentId: null,
        ParentRegionId: '',
        Type: 0,
      },
    );
  });
});

describe('listRegionsApi', () => {
  beforeEach(() => {
    requestClient.post.mockReset();
  });

  it('uses the backend region query body like the reference project', async () => {
    requestClient.post.mockResolvedValue([]);

    const { listRegionsApi } = await import('./management');
    await expect(listRegionsApi({ keyword: '合肥' })).resolves.toEqual([]);

    expect(requestClient.post).toHaveBeenCalledWith(
      '/v1/security/system/region/query',
      {
        AppId: '4841de0858714c5e8bee36e2fad70614',
        Info: '合肥',
        Limit: 9999,
        Page: 1,
      },
    );
  });
});

describe('listSystemMenusApi', () => {
  beforeEach(() => {
    requestClient.post.mockReset();
  });

  it('preserves backend menu fields for the tree view', async () => {
    requestClient.post.mockResolvedValue({
      Count: 1,
      List: [
        {
          appId: 'app-1',
          authority: '',
          checked: false,
          component: '/system/users/index',
          hide: 0,
          icon: 'lucide:users',
          id: 'menu-1',
          parentId: '',
          path: '/system/users',
          sortNumber: 20,
          title: 'Users',
          type: 1,
        },
      ],
    });

    const { listSystemMenusApi } = await import('./management');
    await expect(listSystemMenusApi()).resolves.toEqual({
      list: [
        {
          appId: 'app-1',
          authority: '',
          checked: false,
          component: '/system/users/index',
          hide: 0,
          icon: 'lucide:users',
          id: 'menu-1',
          parentId: '',
          path: '/system/users',
          sortNumber: 20,
          title: 'Users',
          type: 1,
        },
      ],
      total: 1,
    });
  });
});

describe('legacy permission API removal', () => {
  it('does not export the retired system permissions endpoint', async () => {
    const management = await import('./management');

    expect(management).not.toHaveProperty('listSystemPermissionsApi');
    expect(management).not.toHaveProperty('saveSystemPermissionApi');
    expect(management).not.toHaveProperty('deleteSystemPermissionApi');
  });
});
