export function getCellRow<T = any>(first: any, second?: any): T | undefined {
  return second?.row ?? first?.row;
}
