import { describe, expect, it } from 'vitest';

import { collectRegionIds, countRegions, filterRegionTree } from './region-tree';

const regions = [
  {
    code: '340000',
    id: 'anhui',
    name: 'Anhui',
    children: [
      { code: '340100', id: 'hefei', name: 'Hefei' },
      { code: '340200', id: 'wuhu', name: 'Wuhu' },
    ],
  },
  { code: '320000', id: 'jiangsu', name: 'Jiangsu' },
];

describe('region tree', () => {
  it('collects every node ID and counts every visible node', () => {
    expect(collectRegionIds(regions)).toEqual([
      'anhui',
      'hefei',
      'wuhu',
      'jiangsu',
    ]);
    expect(countRegions(regions)).toBe(4);
  });

  it('keeps the parent path when a child matches the search keyword', () => {
    expect(filterRegionTree(regions, 'hefei')).toEqual([
      {
        code: '340000',
        id: 'anhui',
        name: 'Anhui',
        children: [{ code: '340100', id: 'hefei', name: 'Hefei' }],
      },
    ]);
  });
});
