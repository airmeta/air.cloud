import { beforeEach, describe, expect, it } from 'vitest';

import {
  clearSsoSession,
  getAccountInfo,
  getChosenApp,
  getClientAppConfig,
  getClientConfig,
  setAccountInfo,
  setChosenApp,
  setClientAppConfig,
  setClientConfig,
} from './session';

describe('SSO session storage', () => {
  beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();
  });

  it('stores SSO state in localStorage instead of sessionStorage', () => {
    setClientConfig({ ticket: 'ticket-1' });
    setClientAppConfig({ appId: 'app-1', appName: 'App 1' });
    setAccountInfo({ username: 'user-1' });
    setChosenApp('app-2');

    expect(getClientConfig()).toEqual({ ticket: 'ticket-1' });
    expect(getClientAppConfig()).toEqual({ appId: 'app-1', appName: 'App 1' });
    expect(getAccountInfo()).toEqual({ username: 'user-1' });
    expect(getChosenApp()).toBe('app-2');
    expect(localStorage.length).toBe(4);
    expect(sessionStorage.length).toBe(0);
  });

  it('clears all browser storage state on logout', () => {
    setClientConfig({ ticket: 'ticket-1' });
    setClientAppConfig({ appId: 'app-1', appName: 'App 1' });
    setAccountInfo({ username: 'user-1' });
    setChosenApp('app-2');
    localStorage.setItem('air_sso_future_cache', 'clear');
    localStorage.setItem('unrelated-key', 'preserve');
    sessionStorage.setItem('air_sso_runtime_cache', 'clear');
    sessionStorage.setItem('unrelated-session-key', 'preserve');

    clearSsoSession();

    expect(getClientConfig()).toBeNull();
    expect(getClientAppConfig()).toBeNull();
    expect(getAccountInfo()).toBeNull();
    expect(getChosenApp()).toBe('');
    expect(localStorage.getItem('air_sso_future_cache')).toBeNull();
    expect(sessionStorage.getItem('air_sso_runtime_cache')).toBeNull();
    expect(localStorage.getItem('unrelated-key')).toBeNull();
    expect(sessionStorage.getItem('unrelated-session-key')).toBeNull();
  });
});
