# Backlog AgendamentoPro

Itens não implementados, com classificação honesta de esforço/impacto.
Ordenados por prioridade (top = mais valor).

> Última varredura no código: 2026-07-24. Histórico anterior em `git log`.

---

## 🔴 SQL Server: migrations por provider

O README prometia SQL Server; não funcionava. As migrations foram todas geradas
contra o SQLite (333 colunas `TEXT`/`INTEGER`) e produziriam schema inválido lá.
O startup passou a **barrar** o provider com mensagem explicativa.

Para habilitar de verdade: conjunto de migrations por provider em assemblies
separados + suíte rodando contra uma instância real. Esforço: ~2-3 dias, e não
dá para validar sem a instância.

---

## 🔴 Em construção — SaaS Billing (mensalidade por tenant)

**Decidido 2026-05-30:** sistema sai do modelo "grátis pra sempre" pra SaaS pago.

**Especificação:**
- **2 planos (catálogo global):**
  - Essencial — R$ 29,90/mês — 1 unidade
  - Multi-unidade — R$ 79,90/mês — N unidades ilimitadas
- **Gateway:** Mercado Pago Assinaturas (Preapproval API) — reaproveita config do gateway transacional.
- **Grace period:**
  - D+0 a D+7: status `Atrasada`, acesso total, banners de aviso.
  - D+8 a D+30: status `ReadOnly`, sem novos agendamentos / pagamentos / página pública.
  - D+30: soft delete (90d de retenção pra reativação).
- **Entities novas:** `Plano` (global), `Assinatura` (per-tenant), `FaturaAssinatura` (histórico).
- **Job:** `AssinaturaStatusJob` Hangfire diário pra transições de status.
- **Webhook:** `/api/v1/webhooks/assinatura/mercadopago` (separado do transacional).
- **Frontend:** página pública `/planos`, admin `/minha-assinatura` (faturas, mudar plano, cancelar, atualizar cartão).

Esforço: ~5-7 dias.

---

## 🟡 i18n (PT-BR, EN, ES)

`@ngx-translate/core` ainda não está no `package.json`. Refactor de todos os templates Angular.
Esforço: 2-3 dias. **Prioridade baixa** dado foco BR-only.

## 🟡 Gateway Pagar.me (fallback de recorrência)

Se Mercado Pago Assinaturas não atender (dunning fraco, UX ruim), migrar pra Pagar.me.
A entity `Assinatura` deve ser gateway-agnóstica desde o dia 1 pra facilitar essa troca.
Esforço: 1-2 dias.

## 🟡 Resize de fotos em S3

`FotoResizeJob` precisa do arquivo em disco — em modo `STORAGE_PROVIDER=s3` o resize
é skip (documentado no README). Para resize após upload em S3, configurar S3 Event →
Lambda (AWS) ou pipeline externo que baixa-redimensiona-sobe. Esforço: depende do path.

---

## 🟢 Pequeno

- ~~**Vulnerabilidade `ImageSharp` 3.1.7 (NU1902)**~~ — ✅ resolvido 2026-08-29:
  atualizado para 3.1.12 (patch upstream saiu).
- ~~**UX da vitrine pública**~~ — ✅ resolvido 2026-08-29 (2ª rodada): contraste do
  hero corrigido (h1 global vencia o branco do overlay), home pública lista os
  serviços com preço + CTA, data do resumo em `dd/MM/yyyy` (sem passar por Date —
  DatePipe mostraria o dia anterior no fuso BR), sinal usa `percentualEntrada` do
  tenant, senha do admin ≥8 no front.
- ~~**Fidelidade: consulta por "Cliente ID"**~~ e ~~**normalizar telefone na
  gravação**~~ — ✅ resolvidos 2026-08-29: a tela busca por nome/telefone/e-mail
  (mesmo padrão do diálogo de agendamento), `Cliente` grava telefone só com
  dígitos e a busca paginada acha cadastro antigo com máscara.
- **`LembreteJob` + `DATABASE_MULTITENANCY=PerTenant`**: hoje o job já itera tenants
  ativos via DB shared (vide `LembreteJob.cs`). Vale tests integrados em modo PerTenant
  pra cobrir regressões.
- **Hangfire dashboard CSRF**: token JWT no header já protege. Em ambiente high-stakes,
  adicionar Bearer obrigatório no dashboard.

---

## ❌ Descartado (decidido 2026-05-30)

- **NF-e / NFS-e** — sem emissão de nota no momento.
- **Multi-region** — monolito BR-only é suficiente.
- **App nativo (iOS/Android)** — mobile coberto por PWA + Web Push.
- **Versão gratuita / freemium** — todo tenant paga mensalidade.

---

## Decisões do SaaS Billing (resolvidas 2026-07-27)

1. **Trial** — ✅ **decidido e implementado**: cartão obrigatório no cadastro,
   primeiro mês grátis e cancelável, cobra a partir do segundo. Feito com o
   `free_trial` do preapproval do Mercado Pago + status `Trial` na assinatura.
   Falta só validar ponta a ponta com a credencial real do MP (ver
   `docs/O-QUE-FALTA.md`, item 1).
2. **Comissão sobre transações do cliente final?** — ✅ **decidido: não.** Só
   mensalidade. Era o comportamento atual; nada a implementar. Reavaliar um dia
   se fizer sentido, mas por ora o modelo é mensalidade limpa.

---

## Já entregue (não rastrear aqui)

Ver `git log` para histórico. Resumo do que já está em master:

- PWA (manifest + service worker + cache offline público)
- API versionada em `/v1/` + ProblemDetails (RFC 7807) + Correlation-Id
- Login OTP por WhatsApp + área "Minha Conta" do cliente final
- SignalR + Notification Center (badge no admin) + Web Push (VAPID)
- Dark mode (ThemeModeService + prefers-color-scheme)
- Cupom, Pacote Pré-pago, Pontos de Fidelidade, Recorrência (entities + use cases + telas admin)
- Lista de Espera reagindo a cancelamento (notifica via WhatsApp)
- LGPD (audit log, soft delete, mascaramento de senhas/tokens, retenção)
- 2FA admin com TOTP, Lockout após N falhas, reset por e-mail
- Hangfire **persistente** (SQLite/SqlServer)
- Resize de fotos async via `FotoResizeJob` + `S3FotoStorage` (AWS/MinIO/B2/R2)
- Gateways de pagamento **transacional** (cliente final): Mercado Pago (PIX/cartão/boleto) + Stripe (cartão internacional)
- SMS fallback Twilio (quando template WhatsApp falha)
- Playwright e2e (smoke tests)
- Relatórios avançados (LTV, no-show por hora/dia, sazonalidade)
- Auditorias de segurança aplicadas (cross-tenant, 2FA, rate limit, LGPD/PII)
- 294 tests backend verdes

### Correções de 2026-07-24 (passeio pelas 30 telas com a aplicação no ar)

- **A API não subia**: `RecaptchaValidator` implementado de ponta a ponta, sem o
  registro no contêiner. Em Development o host morria no boot; em Production
  subiria e quebraria no primeiro login.
- **Cancelar um horário o bloqueava para sempre**: o índice único de agendamento
  não excluía cancelados, sendo mais rígido que a regra que o código aplica.
  O mesmo defeito tornava **impossível criar tenant**.
- **SignalR nunca conectou**: CORS sem `X-Requested-With` no negotiate e, atrás
  disso, falta de `OnMessageReceived` para ler o token de `?access_token=` (o
  navegador não pode mandar `Authorization` em WebSocket).
- **`/planos` dava 500**: SQLite não ordena por `decimal`.
- **Tela pública de pacotes chamava rota de admin**, jogando o cliente final na
  tela de login do painel.
- **Dados fictícios na criação de tenant** viraram opt-in — antes todo cliente
  pagante nascia com ~95 agendamentos inventados na conta dele.
- **Teste de fumaça e teste de contêiner** criados: os dois defeitos graves acima
  eram invisíveis para teste de unidade.
- **Erro de navegador** passou a chegar ao log do servidor.

---

Última atualização: 2026-07-24.
