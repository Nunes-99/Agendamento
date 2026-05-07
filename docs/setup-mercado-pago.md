# Setup Mercado Pago — AgendamentoPro

Guia para integrar o Mercado Pago como gateway de pagamento (PIX e Cartão) no AgendamentoPro.

> **Modelo escolhido**: centralizado, igual ao WhatsApp. Você (operador do SaaS) cria UMA conta MP e
> todos os pagamentos de todos os tenants caem nela. Para cada tenant ter conta própria seria
> necessário usar **Mercado Pago Marketplace / Connect** (refactor maior, fora do escopo atual).

---

## Pré-requisitos

- [ ] **Conta Mercado Pago** ativada (PJ é obrigatório para volumes acima de R$ 5k/mês — abre em mercadopago.com.br/registration).
- [ ] **Conta verificada**: documento de identificação + comprovante de endereço enviados.
- [ ] **APP_PUBLIC_URL HTTPS** funcionando (Mercado Pago não chama webhook em HTTP nem em IP local).

Tempo estimado: ~1 hora (desconsiderando aprovação da conta MP, que pode levar até 24h).

---

## Passo 1 — Criar a aplicação no painel de developers

1. Acesse https://www.mercadopago.com.br/developers/panel/app
2. Faça login com a conta MP da empresa
3. Clique em **Criar aplicação**
4. Preencha:
   - **Nome da aplicação**: AgendamentoPro
   - **Descrição**: SaaS de agendamento online
   - **Modelo de integração**:
     - ✅ **Pagamentos online e presencial**
   - **Produtos**:
     - ✅ Checkout Pro
     - ✅ Pagamento via PIX (Pix Online)
     - ✅ Webhooks
   - **Categoria do site**: Tecnologia / Software
   - **URL do site**: a URL pública do AgendamentoPro (Ex: https://app.agendamentopro.com.br)
5. Clique em **Criar aplicação**

---

## Passo 2 — Pegar o Access Token

1. Dentro da aplicação criada → **Credenciais** (menu lateral)
2. Você verá duas seções:
   - **Credenciais de teste**: para sandbox (tokens começam com `TEST-`)
   - **Credenciais de produção**: para uso real (tokens começam com `APP_USR-`)
3. Em **Produção**, copie o **Access Token** (formato `APP_USR-xxxxxxxxxxxxxxxxxxxxxxxxxxxx`)

> Mantenha em local seguro (KeePass, 1Password). É o `MERCADOPAGO_ACCESS_TOKEN`.

### Para desenvolvimento

Use as credenciais de **TEST** durante dev. Acesse https://www.mercadopago.com.br/developers/panel/app → **Test users** para criar comprador e vendedor de teste com saldo fictício.

---

## Passo 3 — Configurar Webhook

> Webhooks são as notificações que o MP envia ao AgendamentoPro quando o status do pagamento muda.

1. Painel da aplicação → **Webhooks** (menu lateral) → **Configurar notificações**
2. **URL de produção**:
   ```
   https://SUA-URL-PUBLICA/api/webhooks/pagamento/MercadoPago
   ```
   (substitua `SUA-URL-PUBLICA` pela URL real — tem que ser HTTPS)
3. **URL de teste** (opcional, mas recomendado):
   ```
   https://SUA-URL-DEV/api/webhooks/pagamento/MercadoPago
   ```
   - Para dev local, use https://ngrok.com ou Cloudflare Tunnel para expor localhost
4. **Eventos que vamos receber** — marcar:
   - ✅ **Pagamentos** (`payment`)

   Ignorar os demais (orders, plans, etc) — o AgendamentoPro só usa pagamentos.
5. Clicar em **Salvar configuração**

---

## Passo 4 — Pegar o Webhook Secret

> Sem este secret, qualquer um pode chamar o webhook e fingir um pagamento aprovado. **NÃO PULE.**

1. Após salvar a configuração de webhook, na mesma tela aparece um campo **Chave secreta**
2. Clique em **Mostrar/Gerar chave**
3. Copie o valor (começa com algo aleatório de ~64 chars)

> É o `MERCADOPAGO_WEBHOOK_SECRET`.

O AgendamentoPro valida com HMAC-SHA256 + janela de 5 min de timestamp pra evitar replay. Se a assinatura não bater ou o timestamp estiver fora da janela, o webhook é descartado silenciosamente — confira em `MercadoPagoGateway.cs:VerificarAssinaturaMP`.

---

## Passo 5 — Configurar no AgendamentoPro

No `.env` da API:

```env
MERCADOPAGO_ACCESS_TOKEN=APP_USR-xxxxxxxxxxxx   # do passo 2
MERCADOPAGO_WEBHOOK_SECRET=xxxxxxxxxxxxxxxxxxxx # do passo 4
APP_PUBLIC_URL=https://app.agendamentopro.com.br # HTTPS obrigatório!
```

Reinicie a API. No log, ao processar um pagamento, você não deve ver mais "Mercado Pago não configurado".

---

## Passo 6 — Testar o fluxo end-to-end

### 6.1 Criar agendamento via fluxo público

1. Vá em `/t/SEU-SLUG/servicos`
2. Clique em **Agendar** num serviço
3. Escolha horário, preencha cliente, escolha **PIX** como pagamento
4. Você é redirecionado pro `/t/SEU-SLUG/pagamento/{id}` com o QR Code

### 6.2 Pagar (em sandbox)

- Use o app do MP de **test user** (criado em https://www.mercadopago.com.br/developers/panel/app → Test users) para escanear o QR
- Ou vá em https://www.mercadopago.com.br/developers/panel/app → **Pagamentos PIX** → **Simulador**

### 6.3 Verificar webhook

Em ~30s após o pagamento, o MP chama seu webhook. Confira no log da API:

```
[INFO] cid=xxxx tenant=1/agendamentopro user=- Webhook MercadoPago: payment 12345 status approved
```

Se aparecer `Webhook MercadoPago: assinatura inválida ou expirada` → o secret está errado ou seu servidor está com o relógio fora.

### 6.4 Status do agendamento atualizado

Volte ao admin / agenda — o agendamento deve ter mudado de **Pendente Pagamento** para **Confirmado**.

---

## Troubleshooting

### "Mercado Pago não configurado"

- `MERCADOPAGO_ACCESS_TOKEN` não está definido no `.env`. Reinicie após adicionar.

### Webhook chega 200 mas status do agendamento não muda

- Confira se `MERCADOPAGO_WEBHOOK_SECRET` está correto. Se estiver errado, o webhook é descartado silenciosamente como medida de segurança (a assinatura HMAC não valida).
- Confira o log: deve aparecer `Webhook MercadoPago: assinatura inválida ou expirada`.

### "Recipient phone number is invalid" ao tentar PIX

- Erro raro do MP. Tente recriar a cobrança.

### Pagamento aprovado mas webhook nunca chega

- MP só chama webhook em URLs HTTPS de domínio público. Localhost não funciona — use ngrok pra expor.
- Confira se a URL está cadastrada certinha em **Webhooks** (com `/api/webhooks/pagamento/MercadoPago`, sem barra final).

### "Payment 12345 não localizado no banco"

- O `external_reference` (`{tenantId}:{agendamentoId}`) não bate com nenhum agendamento. Pode ter ocorrido se você apagou agendamentos manualmente no banco. Ignorar.

### Replay protection rejeita webhook legítimo

- Servidor com relógio dessincronizado. Sincronize via NTP (`timedatectl set-ntp true` no Linux).

---

## Custos

| Item | Taxa |
|---|---|
| **PIX** | 0,99% por transação |
| **Cartão de crédito (à vista)** | 4,99% |
| **Cartão de crédito (parcelado)** | 4,99% + juros do parcelamento |
| **Boleto** | R$ 3,49 fixo |
| **Antecipação de recebíveis** | 2,99% a.m. (opcional) |

> Valores podem variar — confirme em https://www.mercadopago.com.br/ajuda/custo-receber-pagamentos_220

Sem mensalidade, sem fidelidade. Cobra apenas por transação efetivamente recebida.

---

## Reconciliação financeira

O AgendamentoPro registra cada pagamento na tabela `Pagamento` com:
- `PagGatewayId` — ID do payment no MP (use pra cruzar com extrato)
- `PagPayloadGateway` — JSON completo da última atualização (para auditoria)

Para reconciliar mensalmente:
1. Exporte extrato do MP em https://www.mercadopago.com.br/activities (formato CSV)
2. Compare `PagGatewayId` com a coluna **ID da transação** do CSV
3. Diferenças possíveis: estornos manuais, reversões automáticas anti-fraude

---

## Migração para Marketplace (futuro)

Se um dia for necessário que cada tenant receba diretamente em sua conta MP (e você fique com uma comissão), o caminho é o **Marketplace Mercado Pago**:

- Cada tenant conecta sua conta MP via OAuth
- AgendamentoPro vira "marketplace owner" e divide a comissão automaticamente em cada pagamento
- Refactor: `Tenant` precisa de `TenMpAccessToken`, `TenMpUserId`, e o `MercadoPagoGateway` recebe `ITenantContext` para escolher a conta certa
- Esforço estimado: 2-3 dias

Documentação: https://www.mercadopago.com.br/developers/pt/docs/checkout-api/integration-marketplace

---

## Links úteis

- Painel de developers: https://www.mercadopago.com.br/developers/panel/app
- Documentação Cloud API: https://www.mercadopago.com.br/developers/pt/docs
- Status MP: https://status.mercadopago.com.br
- Suporte para devs: https://www.mercadopago.com.br/ajuda
