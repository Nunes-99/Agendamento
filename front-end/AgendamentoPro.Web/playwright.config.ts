import { defineConfig, devices } from '@playwright/test';

/**
 * Playwright config para smoke tests do AgendamentoPro.
 *
 * Como rodar localmente:
 *   1. Backend rodando: `cd back-end && dotnet run --project AgendamentoPro.API`
 *   2. `npm run test:e2e:install`  (uma única vez — baixa o Chromium)
 *   3. `npm run test:e2e`          (sobe o ng serve sozinho via webServer)
 *
 * CI: defina E2E_BASE_URL pra apontar pro deployment de staging
 * e DESABILITE o `webServer` (set CI=true).
 */
export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: process.env.CI ? [['github'], ['html', { open: 'never' }]] : 'list',

  use: {
    baseURL: process.env.E2E_BASE_URL ?? 'http://localhost:4200',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
    locale: 'pt-BR',
  },

  projects: [
    { name: 'chromium', use: { ...devices['Desktop Chrome'] } },
  ],

  // Em CI a infra já sobe o servidor; localmente Playwright cuida disso.
  webServer: process.env.CI ? undefined : {
    command: 'npm start',
    url: 'http://localhost:4200',
    reuseExistingServer: true,
    timeout: 120_000,
  },
});
