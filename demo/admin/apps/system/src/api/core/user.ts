import type { UserInfo } from '@vben/types';

import { getAuthorityAccountApi } from './authority';

export async function getUserInfoApi() {
  const account = await getAuthorityAccountApi();

  return {
    avatar: '',
    desc: account.currentAppName || '',
    homePath: '/',
    realName: account.userName || account.account || '当前用户',
    roles: account.userGroups || [],
    token: '',
    userId: account.id || '',
    username: account.account || account.userName || '',
  } as UserInfo;
}
