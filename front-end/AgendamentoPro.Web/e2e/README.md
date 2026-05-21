# E2E (Playwright)

Smoke tests do AgendamentoPro Web. Vão crescer conforme o domínio.

## Rodar localmente

```bash
cd front-end/AgendamentoPro.Web
npm install                  # uma vez
npm run test:e2e:install     # uma vez — baixa Chromium do Playwright
npm run test:e2e             # roda em headless
npm run test:e2e:headed      # roda com browser visível
```

A config `playwright.config.ts` sobe o `npm start` automaticamente quando você
NÃO está em CI (`reuseExistingServer: true`, então se já tiver `npm start`
rodando ele só reusa).

## Em CI

Defina:

```env
CI=true
E2E_BASE_URL=https://staging.suaempresa.com.br
```

Com `CI=true`, o `webServer` é desabilitado — o pipeline já precisa subir
a app antes de rodar os tests.

## O que está coberto hoje

- App carrega sem console errors fatais
- Login admin renderiza os campos
- Rotas legais (privacidade/termos) carregam
- Slug de tenant inválido não trava

## O que ainda NÃO está coberto (próximos)

Tests de jornada (login OTP cliente → criar agendamento → aplicar cupom)
precisam de tenant seedado. Padrão recomendado quando for adicionar:

1. Criar tenant via `POST /api/v1/tenants` autenticado como SuperAdmin
   (faça login programático no `globalSetup`)
2. Pegar slug retornado
3. Rodar fluxo público em `/t/<slug>/...`
4. Em modo Development, `/api/v1/t/{slug}/otp/solicitar` retorna `codigoDev` —
   use isso pra completar o OTP sem WhatsApp real
5. Cleanup: delete o tenant ao final, OU use `dotnet ef database drop` em CI
