import { ref } from 'vue';
import { useRouter } from 'vue-router';

import { defineStore } from 'pinia';

import { notification } from '#/adapter/tdesign';
import {
  getUserInfoApi,
  initAppStatusApi,
  loginApi,
  logoutApi,
  queryAccountAppsApi,
} from '#/api';
import type { Recordable, SsoUserInfo } from '#/types';
import {
  clearSsoSession,
  setAccountInfo,
  setClientAppConfig,
  setClientConfig,
  setTicket,
} from '#/utils/sso/session';

/**
 * zh-cn: SSO 认证状态仓储，负责初始化、登录、用户信息加载、应用中心菜单和退出。
 * en-us: SSO authentication store responsible for initialization, login, user loading, app-center menu, and logout.
 */
export const useAuthStore = defineStore('auth', () => {
  const router = useRouter();

  const accessChecked = ref(false);
  const loginCaptchaBase64 = ref('');
  const loginCaptchaReady = ref(false);
  const loginLoading = ref(false);
  const userInfo = ref<null | SsoUserInfo>(null);

  function setLoginCaptchaBase64(value: string) {
    loginCaptchaBase64.value = value;
  }

  function setLoginCaptchaReady(value: boolean) {
    loginCaptchaReady.value = value;
  }

  /**
   * zh-cn: 初始化登录页所需的应用状态和客户端配置。
   * en-us: Initializes application status and client configuration required by the login page.
   *
   * @param appId
   * zh-cn: 可选目标应用 AppId。
   * en-us: Optional target application AppId.
   */
  async function initLoginPage(appId?: string) {
    const status = await initAppStatusApi(appId);
    setClientConfig(status.client);

    if (status.appId) {
      setClientAppConfig({
        appId: status.appId,
        appName: status.appName,
        loginPath: status.loginPath,
      });
    }

    return status;
  }

  /**
   * zh-cn: 执行账号登录，保存 Ticket，加载用户信息，并进入应用中心。
   * en-us: Performs account login, stores Ticket, loads user info, and enters the app center.
   *
   * @param params
   * zh-cn: 登录表单参数，包含账号、密码和验证码。
   * en-us: Login form parameters containing account, password, and captcha.
   * @param onSuccess
   * zh-cn: 登录成功后的可选回调。
   * en-us: Optional callback after successful login.
   */
  async function authLogin(
    params: Recordable<any>,
    onSuccess?: () => Promise<void> | void,
  ) {
    let currentUserInfo: null | SsoUserInfo = null;
    try {
      loginLoading.value = true;
      const result = await loginApi({
        code: params.code,
        password: params.password,
        username: params.username,
      });

      setTicket(result.ticket);
      setAccountInfo(result.payload ?? {});

      currentUserInfo = await fetchUserInfo();

      await loadAccountAppsAsMenus();
      if (onSuccess) {
        await onSuccess();
      } else {
        await router.push('/app-center');
      }

      notification.success({
        title: '登录成功',
        content: currentUserInfo.realName || currentUserInfo.username,
        duration: 3000,
      });
    } finally {
      loginLoading.value = false;
    }

    return {
      userInfo: currentUserInfo,
    };
  }

  /**
   * zh-cn: 从后端读取当前 Ticket 对应的用户信息并写入 Vben 用户仓储。
   * en-us: Reads user information for the current Ticket from the backend and writes it into the Vben user store.
   */
  async function fetchUserInfo() {
    userInfo.value = await getUserInfoApi();
    return userInfo.value;
  }

  /**
   * zh-cn: 加载当前账号应用列表，并标记 SSO 访问菜单已初始化。
   * en-us: Loads the current account's applications and marks the SSO access menus as initialized.
   */
  async function loadAccountAppsAsMenus() {
    const apps = await queryAccountAppsApi().catch(() => []);
    accessChecked.value = true;
    return apps;
  }

  /**
   * zh-cn: 退出当前 SSO 会话，远端退出失败也会清理本地 Ticket 状态。
   * en-us: Logs out of the current SSO session and clears local Ticket state even when remote logout fails.
   *
   * @param redirect
   * zh-cn: 是否在登录页携带当前路径作为回跳地址。
   * en-us: Whether to pass the current path as a redirect target to the login page.
   * @param appId
   * zh-cn: 可选的业务应用 AppId；由跨应用注销页传入，确保后端按签发 Ticket 的应用校验。
   * en-us: Optional business AppId supplied by the cross-application logout page so the backend validates the Ticket against its issuing application.
   */
  async function logout(redirect: boolean = true, appId?: string) {
    try {
      await logoutApi(appId);
    } catch {
      // ignore remote logout failure and always clear local ticket state
    }

    clearSsoSession();
    $reset();

    await router.replace({
      path: '/auth/login',
      query: redirect
        ? {
            redirect: encodeURIComponent(router.currentRoute.value.fullPath),
          }
        : {},
    });
  }

  /**
   * zh-cn: 重置认证仓储的临时加载状态。
   * en-us: Resets temporary loading state in the authentication store.
   */
  function $reset() {
    accessChecked.value = false;
    loginCaptchaBase64.value = '';
    loginCaptchaReady.value = false;
    loginLoading.value = false;
    userInfo.value = null;
  }

  return {
    $reset,
    accessChecked,
    authLogin,
    fetchUserInfo,
    initLoginPage,
    loginCaptchaBase64,
    loginCaptchaReady,
    loadAccountAppsAsMenus,
    loginLoading,
    logout,
    setLoginCaptchaBase64,
    setLoginCaptchaReady,
    userInfo,
  };
});
