import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';

import { describe, expect, it } from 'vitest';

const captchaFieldSource = readFileSync(
  resolve(process.cwd(), 'apps/sso/src/views/auth/image-captcha-field.vue'),
  'utf8',
);

describe('ImageCaptchaField', () => {
  it('fills the fixed field because the backend now emits the matching 132 x 40 aspect ratio', () => {
    expect(captchaFieldSource).toContain('object-fit: cover;');
    expect(captchaFieldSource).not.toContain('object-fit: contain;');
  });
});
