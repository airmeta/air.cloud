import { setDefaultSettings } from './base_settings';
import { bootstrap } from './bootstrap';
import { appSettings } from './settings';

async function initApplication() {
  setDefaultSettings(appSettings);
  await bootstrap();
  document.querySelector('#__app-loading__')?.remove();
}

void initApplication();
