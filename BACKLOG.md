# Backlog AgendamentoPro

Itens não implementados, com classificação honesta de esforço/impacto.
Ordenados por prioridade (top = mais valor).

> Última varredura no código: 2026-05-20. Histórico anterior em `git log`.

---

## 🟡 Frontend pendente (Cypress / Playwright)

Backend tem 177 tests; frontend tem **zero**. Mínimo: smoke test de login OTP + criar
agendamento + checkout. Esforço: 1 dia de setup + ~30min por feature.

## 🟡 i18n (PT-BR, EN, ES)

`@ngx-translate/core` ainda não está no `package.json`. Refactor de todos os templates Angular.
Esforço: 2-3 dias.

## 🟡 NF-e / NFS-e

Integração com `NFe.io`, `eNotas` ou prefeitura municipal direto. Custo: licença + transação.
Bloqueante se for vender o SaaS pra terceiros. Esforço: 1 semana+.

## 🟡 Web Push (VAPID)

Hoje só `SignalR` in-app (admin precisa estar com a aba aberta). Para notificação real
no dispositivo, precisa par de chaves VAPID + Service Worker + tabela de subscriptions.
Esforço: 1-2 dias.

## 🟡 SMS fallback do WhatsApp

Quando o template não está aprovado/aceito, cair em SMS. Integração Twilio, Zenvia ou Infobip.
Esforço: 4-6h.

## 🟡 Gateway Stripe / Pagar.me

Hoje só Mercado Pago via `IGatewayPagamento`. Para internacional, Stripe; para BR
(cartão recorrente), Pagar.me. Esforço: 1 dia por gateway.

## 🟡 Relatórios LTV / no-show / sazonalidade

Hoje há dashboards básicos. Faltam: LTV por cliente, taxa de no-show por horário/dia
da semana, sazonalidade mensal/anual. Esforço: 4-6h cada relatório.

## 🟡 Resize de fotos em S3

`FotoResizeJob` precisa do arquivo em disco — em modo `STORAGE_PROVIDER=s3` o resize
é skip (documentado no README). Para resize após upload em S3, configurar S3 Event →
Lambda (AWS) ou pipeline externo que baixa-redimensiona-sobe. Esforço: depende do path.

---

## 🟢 Pequeno

- **Vulnerabilidade `ImageSharp` 3.1.7 (NU1902)** — aguardar patch upstream (hoje
  3.1.7 é o mais novo da linha 3.1.x).
- **`LembreteJob` + `DATABASE_MULTITENANCY=PerTenant`**: hoje o job já itera tenants
  ativos via DB shared (vide `LembreteJob.cs`). Vale tests integrados em modo PerTenant
  pra cobrir regressões.
- **Hangfire dashboard CSRF**: token JWT no header já protege. Em ambiente high-stakes,
  adicionar Bearer obrigatório no dashboard.

---

## Decisões a tomar antes de continuar

1. **Vender pra clientes externos?** Se sim, NF-e e LGPD avançado (2FA já feito, audit UI)
   viram blockers.
2. **Mobile-first ou app dedicado?** PWA cobre 80%; app nativo só faz sentido com push
   notification real (Web Push + APNs/FCM).
3. **Multi-region?** Hoje monolito; só sai daí com read-replicas + S3 com replicação cross-region.
4. **Modelo de cobrança?** Mensalidade fixa / % de transação / freemium — não definido.

---

## Já entregue (não rastrear aqui)

Ver `git log` para histórico. Resumo do que já está em master:

- PWA (manifest + service worker + cache offline público)
- API versionada em `/v1/` + ProblemDetails (RFC 7807) + Correlation-Id
- Login OTP por WhatsApp + área "Minha Conta" do cliente final
- SignalR + Notification Center (badge no admin)
- Dark mode (ThemeModeService + prefers-color-scheme)
- Cupom, Pacote Pré-pago, Pontos de Fidelidade, Recorrência (entities + use cases + telas admin)
- Lista de Espera reagindo a cancelamento (notifica via WhatsApp)
- LGPD (audit log, soft delete, mascaramento de senhas/tokens, retenção)
- 2FA admin com TOTP
- Lockout após N falhas + reset por e-mail
- Hangfire **persistente** (SQLite/SqlServer)
- Resize de fotos async via `FotoResizeJob` + atualização de `FotTamanhoBytes`
- `S3FotoStorage` (compatível com AWS S3, MinIO, B2, R2 — opt-in via `STORAGE_PROVIDER=s3`)
- 177 tests verdes (entities, use cases, repositórios, EF in-memory)

---

Última atualização: 2026-05-20.
