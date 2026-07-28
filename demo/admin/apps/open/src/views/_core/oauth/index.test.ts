import { describe, expect, it } from 'vitest';

import OauthPage from './index.vue';

describe('OAuth page', () => {
  it('compiles through the System Vite transform pipeline', () => {
    expect(OauthPage).toBeTruthy();
  });
});
