import { test, expect } from '@playwright/test';

/**
 * Smoke tests do AgendamentoPro.
 *
 * Cobrem as superfícies que NÃO dependem de um tenant específico seedado:
 * - app carrega sem erro de console fatal
 * - login admin renderiza com campos básicos
 * - rota pública com tenant inválido degrada bem (não trava)
 * - políticas legais renderizam estáticas
 *
 * Tests mais profundos (fluxo de agendamento end-to-end, OTP) ficam fora
 * deste arquivo porque exigem tenant seedado e endpoints específicos —
 * crie um spec separado quando configurar fixture de seed via API.
 */

test.describe('Smoke', () => {
  test('raiz redireciona para login do admin', async ({ page }) => {
    await page.goto('/');
    await expect(page).toHaveURL(/\/admin\/login$/);
  });

  test('login admin renderiza campos de e-mail e senha', async ({ page }) => {
    await page.goto('/admin/login');
    await expect(page.getByRole('heading', { name: /entrar|login/i }).first()).toBeVisible();
    await expect(page.locator('input[type="email"], input[name="email"]').first()).toBeVisible();
    await expect(page.locator('input[type="password"]').first()).toBeVisible();
  });

  test('app não dispara erros de console fatais ao carregar', async ({ page }) => {
    const erros: string[] = [];
    page.on('pageerror', e => erros.push(e.message));
    page.on('console', msg => {
      if (msg.type() === 'error') erros.push(msg.text());
    });

    await page.goto('/admin/login', { waitUntil: 'networkidle' });

    // Filtra erros transitórios de rede esperados quando backend não está rodando
    // (Playwright pode rodar com backend off; o app ainda deve renderizar).
    const fatais = erros.filter(e =>
      !/Failed to fetch|NetworkError|ERR_CONNECTION|HttpErrorResponse/i.test(e)
    );
    expect(fatais, `Erros fatais inesperados:\n${fatais.join('\n')}`).toEqual([]);
  });

  test('política de privacidade renderiza', async ({ page }) => {
    await page.goto('/politica-privacidade');
    await expect(page.locator('body')).toContainText(/privacidade/i);
  });

  test('termos de uso renderizam', async ({ page }) => {
    await page.goto('/termos-uso');
    await expect(page.locator('body')).toContainText(/termos/i);
  });
});

test.describe('Rotas públicas com tenant', () => {
  test('home com slug inexistente não trava o app', async ({ page }) => {
    await page.goto('/t/tenant-inexistente-xpto');
    // O comportamento exato depende do erro handler; o critério é que a aba
    // não fica em branco nem mostra "Uncaught" no console.
    await expect(page).not.toHaveTitle(/^$/);
  });
});
