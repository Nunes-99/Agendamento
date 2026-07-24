# AgendamentoPro — o que falta

O sistema está maduro e no GitHub. Diferente de um projeto no começo, aqui não
falta construir — falta **validar o caminho do dinheiro** e **tomar duas
decisões de negócio**. O resto é backlog.

A ordem abaixo vai do que trava receita ao que é melhoria.

---

## 1. Validar o pagamento de ponta a ponta  ·  🔴 o único item crítico

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

## 2. Decidir o modelo de assinatura  ·  🟡 decisão sua, não código

Duas perguntas em aberto no [`BACKLOG.md`](../BACKLOG.md), ambas de negócio:

1. **Trial**: cartão obrigatório no cadastro, ou X dias grátis sem cartão? Isso
   muda o fluxo de signup do SaaS — precisa estar decidido antes de implementá-lo.
2. **Comissão sobre as transações do cliente final?** Hoje só se cobra a
   mensalidade do tenant. Reavaliar quando o faturamento recorrente estabilizar.

Não há o que programar até você responder. Quando responder, o
`CriarAssinatura`/webhook de assinatura já existem como base.

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

Corrigido e testado nesta rodada (ver `git log`):

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
