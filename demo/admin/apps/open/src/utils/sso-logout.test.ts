import { describe, expect, it } from 'vitest';

import { getSsoLogoutUrl } from './sso-logout';

describe('getSsoLogoutUrl', () => {
  it('adds the current application ticket and AppId to the configured SSO logout URL', () => {
    const url = getSsoLogoutUrl(
      'ticket + /',
      JSON.stringify({
        appId: 'system-app',
        ssoLogoutUrl: 'http://localhost:6103/logout?source=system',
      }),
    );

    expect(url).toBe(
      'http://localhost:6103/logout?source=system&appId=system-app&tickit=ticket+%2B+%2F',
    );
  });

  it('returns null when the SSO logout URL is not configured', () => {
    expect(getSsoLogoutUrl('ticket-1', JSON.stringify({}))).toBeNull();
  });

  it('returns null when the business application AppId is missing', () => {
    expect(
      getSsoLogoutUrl(
        'ticket-1',
        JSON.stringify({ ssoLogoutUrl: 'http://localhost:6103/logout' }),
      ),
    ).toBeNull();
  });
});
