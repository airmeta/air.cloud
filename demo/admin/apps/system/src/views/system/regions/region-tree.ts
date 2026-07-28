interface RegionTreeNode {
  children?: RegionTreeNode[];
  code: string;
  id: string;
  name: string;
}

function collectRegionIds(regions: RegionTreeNode[]): string[] {
  return regions.flatMap((region) => [
    region.id,
    ...collectRegionIds(region.children || []),
  ]);
}

function countRegions(regions: RegionTreeNode[]): number {
  return regions.reduce(
    (count, region) => count + 1 + countRegions(region.children || []),
    0,
  );
}

function filterRegionTree<T extends RegionTreeNode>(
  regions: T[],
  keyword: string,
): T[] {
  const normalizedKeyword = keyword.trim().toLowerCase();
  if (!normalizedKeyword) {
    return regions;
  }

  return regions.flatMap((region) => {
    const children = filterRegionTree(region.children || [], normalizedKeyword);
    const matches = [region.code, region.name].some((value) =>
      value.toLowerCase().includes(normalizedKeyword),
    );

    if (matches) {
      return [region];
    }

    return children.length > 0
      ? [{ ...region, children } as T]
      : [];
  });
}

export { collectRegionIds, countRegions, filterRegionTree };
