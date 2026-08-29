# AgendamentoPro — o que falta

O sistema está maduro e no GitHub. Diferente de um projeto no começo, aqui não
falta construir — falta **validar o caminho do dinheiro** e **tomar duas
decisões de negócio**. O resto é backlog.

A ordem abaixo vai do que trava receita ao que é melhoria.

---

## 1. Validar o pagamento de ponta a ponta  ·  ✅ VALIDADO em 2026-08-29

Feito com credencial **TEST-** do Mercado Pago (sandbox, conta
VITORROBERTONUNES — a mesma aplicação usada no ConnectBrinquedos).
Resultado: **o caminho do dinheiro funciona**, com dois defeitos corrigidos
no caminho (sem eles, NENHUM PIX era criado — ver `git log`):

1. **`payer.email` inválido**: o gateway mandava um placeholder
   `@agendamentopro.local`; o MP recusa TLD inventado com 400. Agora usa o
   e-mail do cliente (fallback com domínio real).
2. **`notification_url` com localhost**: o MP recusa URL não pública, e o
   PIX inteiro falhava por causa disso. Agora o campo só é enviado quando a
   `APP_PUBLIC_URL` é pública (em dev sem túnel, o webhook simplesmente não é
   chamado pelo MP — use ngrok para exercitá-lo de fora).

O que foi exercitado, nesta ordem:

- **PIX criado de verdade** (payment 1328005216, `pending`): QR copia-e-cola
  BR Code válido + ticket URL do sandbox; `external_reference` = `6:10`.
- **Webhook com PIX ainda pendente** → agendamento **continuou pendente**
  (o serviço reconsulta o MP antes de dar como pago — anti-forjaria OK).
- **Pagamento aprovado de verdade** (cartão de teste `APRO`, payment
  1328005252, `approved`) → webhook → **agendamento virou Confirmado** e o
  pagamento Aprovado; visto na tela `/admin/agenda` ("Bruno Costa — Lavagem
  Simples — Sinal R$ 8,00 — ✓ Confirmado").
- **Webhook duplicado** → ignorado por idempotência (log explícito).
- **Webhook forjado** (payment inexistente) → MP responde 404 e nada é
  confirmado.

> Ainda **não exercitado com o MP chamando de fora**: em dev o webhook foi
> disparado localmente (localhost não recebe callback do MP). Para ver o
> callback real, suba um túnel (ngrok) e aponte `APP_PUBLIC_URL` para ele —
> o `notification_url` volta a ser enviado automaticamente. O
> `MERCADOPAGO_WEBHOOK_SECRET` também só é exigível nesse cenário.

### Referência histórica do que faltava aqui

O fluxo do cliente agendando está correto até o pagamento, mas o **pagamento em
si nunca foi exercitado de verdade** — QR do PIX, webhook de confirmação, o
agendamento saindo de *PendentePagamento* para *Confirmado*. Falta porque
depende de uma credencial do Mercado Pago, que é sua.

**Passo a passo:**

1. Siga [`docs/setup-mercado-pago.md`](setup-mercado-pago.md) para criar a
   aplicação e obter `MERCADOPAGO_ACCESS_TOKEN` e `MERCADOPAGO_WEBHOOK_SECRET`.
   Para testar sem dinheiro real, use as **credenciais de teste** (`TEST-...`) e
   um comprador de teste do Mercado Pago.
2. Rode local com essas variáveis no `.env` (veja
   [`docs/testar-assinatura-local.md`](testar-assinatura-local.md), que já
   descreve o ambiente).
3. Faça um agendamento pela página pública escolhendo **PIX**, e confira, nesta
   ordem:
   - a tela mostra o **QR Code** e o "copia e cola";
   - após pagar (no ambiente de teste), o **webhook** chega e o agendamento vira
     **Confirmado** — acompanhe pelo log e pela tela `/admin/agenda`;
   - o webhook rejeita um aviso forjado (ele reconsulta o Mercado Pago antes de
     dar como pago — isso já está testado no backend, mas vale ver acontecendo).

> Enquanto isto não for validado, **não coloque um cliente pagante para agendar
> com PIX** — é o único trecho do caminho de receita sem verificação real.
> Sem gateway configurado, o sistema já avisa o cliente ("pagamento online
> indisponível, entre em contato") em vez de dar erro — então o site não quebra,
> só não cobra online.

---

## 2. Modelo de assinatura  ·  ✅ decidido e implementado

As duas decisões de negócio foram tomadas e o código já as reflete:

1. **Trial**: cartão obrigatório no cadastro, **primeiro mês grátis e
   cancelável**, cobra a partir do segundo. Implementado com o `free_trial` do
   Mercado Pago. **A validação ponta a ponta cai no item 1 acima** — o cartão
   autorizado sem cobrança no mês 1 só se confirma contra a conta real do MP.
2. **Comissão sobre transações do cliente final**: **não** — só mensalidade.
   Era o comportamento atual; nada a fazer.

---

## 3. WhatsApp (lembretes)  ·  🟡 se quiser os avisos automáticos

Os lembretes 24h/2h antes do agendamento dependem de templates aprovados na
Meta. Passo a passo em
[`docs/setup-whatsapp-business.md`](setup-whatsapp-business.md): criar conta Meta
Business, registrar número, gerar token permanente e submeter os templates
`lembrete_24h` / `lembrete_2h`.

Sem isto o sistema funciona — só não manda os lembretes (e, se um template
falhar, já cai no fallback de SMS via Twilio, se `TWILIO_*` estiver configurado).

---

## 4. Backlog — quando houver tempo/necessidade  ·  🟢

Detalhes e esforço estimado no [`BACKLOG.md`](../BACKLOG.md):

- **SQL Server de verdade** — hoje o provider é **barrado no boot** com uma
  mensagem explicando o porquê (as migrations foram geradas para SQLite). Para
  habilitar: um conjunto de migrations por provider e a suíte rodando contra uma
  instância real. ~2-3 dias, e não dá para validar sem a instância. **Enquanto
  isso, use SQLite** (o padrão), que está testado.
- **Gateway Pagar.me** — alternativa ao Mercado Pago para recorrência, caso o
  dunning dele decepcione.
- **Resize de fotos em modo S3** — hoje pulado quando `STORAGE_PROVIDER=s3`.
- **i18n** (PT-BR / EN / ES) — prioridade baixa dado foco BR.
- **ImageSharp `NU1902`** — aguardar patch upstream.

---

## O que NÃO precisa mais fazer

### Corrigido em 2026-08-29 (teste de bancada com a aplicação no ar)

- **"Criar empresa" e "Salvar serviço" não faziam nada**: o `[mat-dialog-close]="obj"`
  não entregava o resultado ao `afterClosed` — o botão fechava o diálogo como se
  fosse "Cancelar", silenciosamente. Trocado pelo padrão do resto do app
  (`MatDialogRef.close(obj)` explícito).
- **Tenant Cancelada/Expirada ficava com acesso liberado para sempre**: o guard de
  assinatura usava `GetByTenantAsync`, que esconde Cancelada/Expirada (para permitir
  re-assinar) — o tenant parecia "sem assinatura" e passava livre. Novo
  `GetUltimaByTenantAsync` sem filtro de status alimenta o guard; agora
  `demo-cancelada`/`demo-expirada` retornam 503 na área pública como documentado.
- **Criar assinatura sem MP configurado dava 500 e travava o tenant**: a exceção do
  gateway estourava como erro de servidor e deixava uma assinatura órfã (Trial sem
  preapproval) que bloqueava novas tentativas com "já possui assinatura ativa".
  Agora vira 400 com mensagem amigável e o rascunho é desfeito (306 testes verdes,
  com teste novo cobrindo o caso).
- **ImageSharp atualizado 3.1.7 → 3.1.12** — o patch upstream do NU1902 saiu.
- **/planos e /admin/minha-assinatura agora anunciam o primeiro mês grátis** — o
  trial estava implementado mas invisível para o cliente.

### Corrigido em 2026-08-29 — 2ª rodada (varredura de todos os fluxos)

- **Cliente não via os próprios agendamentos na Minha Conta**: telefone com máscara
  diferente por fluxo ("(11) 99887-7665" no agendamento vs "11998877665" no OTP)
  duplicava o cadastro. Busca por telefone agora normaliza para dígitos (tolerando
  DDI 55) — também no fluxo de compra de pacote.
- **Caixa do dia contava cancelados**: dia só com cancelamentos mostrava a receita
  cheia em "prevista" e o pagamento pendente do cancelado como "pendente".
- **Auditoria gravava ID temporário do EF nos INSERTs** (`#-2147482644`): log de
  Insert passou a ser gravado após o save, com a chave real — sem isso não dava
  para correlacionar o log com a linha (LGPD/troubleshooting).
- **Vitrine pública renovada**: contraste do hero, catálogo na home, data dd/MM/yyyy,
  sinal com % do tenant. Varredura completa dos fluxos: combos, cupons, pacotes,
  bloqueios, recorrências, lista de espera, KPIs, relatórios, 2FA, importar CSV,
  OTP/Minha Conta e esqueci-senha — todos exercitados com a aplicação no ar.

### Corrigido em 2026-08-29 — 3ª rodada (validação completa de usabilidade/layout)

- **Dark mode quebrado em todo o app**: 44 fundos `#fff` fixos em 35 componentes +
  miolo do admin-shell claro fixo → tudo em CSS vars; modo escuro agora é coeso.
- **Personalização não atingia os botões Material** (preço na cor do tenant, botão
  índigo) → vars MDC apontam para `--cor-primaria`.
- **Comprar pacote sem gateway**: 500 genérico + SaldoPacote órfão → 503 com
  mensagem acionável, checagem antes de persistir; card de pacote ganhou
  affordance de seleção.
- **Ciclo de vida validado ponta a ponta com a aplicação no ar**: agendamento →
  confirmar pagamento (dinheiro) → iniciar → concluir → 10 pontos de fidelidade →
  token de avaliação → cliente avalia 5★ → nota e comentário aparecem na home
  pública. Minha Conta da cliente mostra o histórico (telefone normalizado
  funcionando no fluxo real). SignalR conectado (evento de novo agendamento só é
  emitido com pagamento OK — validar com credencial MP). Performance dev: home
  pública carrega em ~1,6s; APIs públicas respondem em 10–15ms.
- **Não coberto**: teste visual mobile (janela maximizada do Windows não
  redimensiona via automação) — vale abrir no celular; push de evento
  SignalR/Web Push e QR PIX real dependem do item 1.

### 2026-08-29 — 4ª rodada: vitrine do lojista + mobile validado

- **Nova feature — vitrine estilizável** (aba "Minha página"): o lojista publica
  até 8 anúncios/promoções (título, texto, visível, destaque com cor de acento)
  que aparecem na home pública; a fonte escolhida agora carrega de verdade
  (Google Fonts + inline no body — antes o campo não fazia nada) com select de
  fontes populares. Sem migration: JSON em ConfiguracaoTenant via VitrineController.
- **Mobile validado em 390px** (via iframes, já que a janela maximizada não
  redimensiona): home/catálogo/agendar em coluna única, admin com hambúrguer e
  KPIs empilhados, Minha Conta com tabs scrolláveis — tudo correto.

### Corrigido e testado na rodada anterior (ver `git log`):

- **A API não subia** (RecaptchaValidator fora do contêiner) — corrigido, com
  teste de fumaça que sobe a aplicação de verdade.
- **Cancelar um horário o bloqueava para sempre** / **não dava para criar
  tenant** — corrigido (índice único passou a ignorar cancelados).
- **SignalR nunca conectava** (CORS + token no WebSocket) — corrigido.
- **Marcar horário de graça pela rota pública** (forma "Dinheiro" anônima) —
  fechado.
- **Chamada ao gateway dentro da transação do banco** — movida para fora
  (evitava "database is locked" sob carga no SQLite).
- **"Minha Conta" dava 500** (ORDER BY por TimeSpan no SQLite) — corrigido.
- **Erro do navegador sumia no console** — passou a ir para o log do servidor.

---

## Referência rápida das variáveis (produção)

Lista completa no [`README.md`](../README.md). As que importam para os itens acima:

| Variável | Para quê | Item |
|---|---|---|
| `MERCADOPAGO_ACCESS_TOKEN` | cobrar (PIX/cartão) | 1 |
| `MERCADOPAGO_WEBHOOK_SECRET` | confirmar pagamento | 1 |
| `WHATSAPP_ACCESS_TOKEN` / `WHATSAPP_PHONE_NUMBER_ID` | lembretes | 3 |
| `TWILIO_*` | fallback SMS do WhatsApp | 3 |
| `JWT_SECRET_KEY` | obrigatória em produção (≥64 chars) | — |
| `Database:Provider` | deixe **Sqlite** (SqlServer é barrado hoje) | 4 |
