export type Recordable<T = any> = Record<string, T>;

export interface SsoUserInfo {
  avatar?: string;
  desc?: string;
  homePath: string;
  realName?: string;
  roles: string[];
  ticket?: string;
  userId: string;
  username: string;
}
