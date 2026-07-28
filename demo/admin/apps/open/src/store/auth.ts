import { resetAllStores, useAccessStore, useUserStore } from '@vben/stores';

import { defineStore } from 'pinia';

import { clearAuthorityAccountCache, getUserInfoApi } from '#/api';
import { getSsoLogoutUrl } from '#/utils/sso-logout';

const TICKET_STORAGE_KEY = 'vea:auth:ticket';

export const useAuthStore = defineStore('auth', () => {
  const accessStore = useAccessStore();
  const userStore = useUserStore();

  function logout() {
    const ticket = localStorage.getItem(TICKET_STORAGE_KEY) || '';
    const ssoLogoutUrl = getSsoLogoutUrl(ticket);

    localStorage.removeItem(TICKET_STORAGE_KEY);
    clearAuthorityAccountCache();
    sessionStorage.clear();
    resetAllStores();
    accessStore.setLoginExpired(false);

    window.history.replaceState(null, '', window.location.origin);
    window.location.replace(ssoLogoutUrl || '/403');
  }

  async function fetchUserInfo() {
    const userInfo = await getUserInfoApi();
    userStore.setUserInfo(userInfo);
    return userInfo;
  }

  function $reset() {}

  return {
    $reset,
    fetchUserInfo,
    logout,
  };
});
