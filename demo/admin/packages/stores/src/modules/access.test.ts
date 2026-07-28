import { createPinia, setActivePinia } from 'pinia';
import { beforeEach, describe, expect, it } from 'vitest';

import { useAccessStore } from './access';

describe('useAccessStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
  });

  it('updates accessMenus state', () => {
    const store = useAccessStore();
    expect(store.accessMenus).toEqual([]);
    store.setAccessMenus([{ name: 'Dashboard', path: '/dashboard' }]);
    expect(store.accessMenus).toEqual([
      { name: 'Dashboard', path: '/dashboard' },
    ]);
  });

  it('syncs menu icons from system menu rows', () => {
    const store = useAccessStore();
    store.setAccessMenus([
      {
        children: [
          { icon: 'lucide:key-square', name: '令牌管理', path: '/system/agent-tokens' },
        ],
        icon: 'lucide:shield-lock',
        name: '安全管理',
        path: '/security',
      },
    ]);

    store.syncAccessMenusFromSystemMenus([
      { icon: 'lucide:shield-check', path: '/security', sortNumber: 95 },
      { icon: 'lucide:key-square', path: '/system/agent-tokens', sortNumber: 10 },
    ]);

    expect(store.accessMenus[0]?.icon).toBe('lucide:shield-check');
    expect(store.accessMenus[0]?.order).toBe(95);
    expect(store.accessMenus[0]?.children?.[0]?.icon).toBe('lucide:key-square');
  });

  it('updates accessToken state correctly', () => {
    const store = useAccessStore();
    expect(store.accessToken).toBeNull(); // 初始状态
    store.setAccessToken('abc123');
    expect(store.accessToken).toBe('abc123');
  });

  it('returns the correct accessToken', () => {
    const store = useAccessStore();
    store.setAccessToken('xyz789');
    expect(store.accessToken).toBe('xyz789');
  });

  // 测试设置空的访问菜单列表
  it('handles empty accessMenus correctly', () => {
    const store = useAccessStore();
    store.setAccessMenus([]);
    expect(store.accessMenus).toEqual([]);
  });

  // 测试设置空的访问路由列表
  it('handles empty accessRoutes correctly', () => {
    const store = useAccessStore();
    store.setAccessRoutes([]);
    expect(store.accessRoutes).toEqual([]);
  });
});
