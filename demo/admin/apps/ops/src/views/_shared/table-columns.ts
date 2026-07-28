type TableCellContext = {
  rowIndex?: number;
};

export function getTableRowIndex(first: any, second?: any) {
  const context = (first && typeof first === 'object' && 'rowIndex' in first ? first : second) as
    | TableCellContext
    | undefined;
  return typeof context?.rowIndex === 'number' ? context.rowIndex : 0;
}

export function createIndexColumn(startIndex = 0) {
  return {
    align: 'center' as const,
    cell: (...args: any[]) => String(startIndex + getTableRowIndex(args[0], args[1]) + 1),
    colKey: '__index',
    title: '序号',
    width: 72,
  };
}

export function withSerialColumn<T extends any[]>(
  columns: T,
  options?: {
    offset?: number | (() => number);
  },
) {
  const offset = options?.offset;
  const resolveOffset = () => (typeof offset === 'function' ? offset() : offset || 0);
  return [
    {
      align: 'center' as const,
      cell: (...args: any[]) => String(resolveOffset() + getTableRowIndex(args[0], args[1]) + 1),
      colKey: '__serial',
      title: '序号',
      width: 72,
    },
    ...columns,
  ];
}
