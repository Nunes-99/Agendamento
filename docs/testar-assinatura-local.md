# Testar SaaS Billing localmente (sem cobrar nada)

Guia pra rodar o fluxo de assinatura mensal em desenvolvimento usando o **sandbox do Mercado Pago**. Nenhuma transação real acontece — você usa cartões fake, usuário pagador fake, e o MP marca tudo como "modo teste".

> **Status do código:** backend + frontend prontos, build verde, 280 testes passando. Falta só testar end-to-end com cartão real (de teste).

---

## 1. Pré-requisitos

- API rodando em `http://localhost:5050` (`dotnet run` em `back-end/AgendamentoPro.API`)
- Frontend rodando em `http://localhost:4200` (`npm start` em `front-end/AgendamentoPro.Web`)
- Conta no Mercado Pago (não precisa de CNPJ pra criar credenciais de teste)

---

## 2. Pegar credenciais de teste do MP

1. Acesse https://www.mercadopago.com.br/developers/panel
2. **Suas integrações** → crie uma aplicação (ou abra uma existente)
3. Aba **Credenciais de teste** → copie o **Access token** (começa com `TEST-`)

```powershell
$env:MERCADOPAGO_ACCESS_TOKEN = "TEST-1234567890abcdef-..."
$env:APP_PUBLIC_URL = "http://localhost:5050"
# MERCADOPAGO_WEBHOOK_SECRET pode ficar vazio em dev — código já permite (com warning)
```

Reinicie a API depois de setar as vars.

---

## 3. Criar um usuário de teste pagador

Preapproval do MP **não aceita o e-mail da própria conta** como pagador. Crie um usuário de teste:

```powershell
$tk = $env:MERCADOPAGO_ACCESS_TOKEN
Invoke-RestMethod -Method Post `
  -Uri "https://api.mercadopago.com/users/test_user" `
  -Headers @{ Authorization = "Bearer $tk" } `
  -ContentType "application/json" `
  -Body '{"site_id":"MLB","description":"payer-test"}'
```

Resposta:
```json
{
  "id": 12345,
  "email": "test_user_98765432@testuser.com",
  "password": "qatest123",
  "site_status": "active"
}
```

**Anote o `email`** — é o que vai no campo "E-mail do pagador" da tela `/admin/minha-assinatura`. Salve a senha também, caso o MP peça login durante o checkout.

---

## 4. Cartões de teste (BR)

Use **um destes números** no checkout do MP:

| Marca | Número | CVV | Validade |
|---|---|---|---|
| Mastercard | `5031 4332 1540 6351` | 123 | qualquer futura |
| Visa | `4235 6477 2802 5682` | 123 | qualquer futura |
| Hipercard | `6062 8266 4736 5499` | 123 | qualquer futura |

**O nome no cartão controla o resultado** (importante!):

| Nome | Resultado |
|---|---|
| `APRO` | aprovado ✅ |
| `OTHE` | recusa genérica |
| `CONT` | pendente |
| `CALL` | "ligue pra autorização" |
| `FUND` | saldo insuficiente |
| `SECU` | CVV inválido |
| `EXPI` | data inválida |

Pra simular sucesso: nome `APRO`. Pra ver o banner amarelo de atraso: nome `OTHE`.

---

## 5. Webhook — escolha uma das 3 opções

MP só consegue chamar o webhook se a URL for **pública**. Localhost não dá. Opções:

### Opção A — ngrok (recomendado, ~5min)

1. Baixe em https://ngrok.com (free tier basta)
2. Rode em outro terminal: `ngrok http 5050`
3. Copie a URL pública (ex: `https://abc123.ngrok.io`)
4. Setar e reiniciar a API:
   ```powershell
   $env:APP_PUBLIC_URL = "https://abc123.ngrok.io"
   ```
5. No painel MP → **Webhooks** → adicionar URL `https://abc123.ngrok.io/api/v1/webhooks/assinatura/MercadoPago`, eventos:
   - `subscription_preapproval`
   - `subscription_authorized_payment`

### Opção B — Cloudflare Tunnel (sem login)

```powershell
# baixa cloudflared (https://github.com/cloudflare/cloudflared/releases)
cloudflared tunnel --url http://localhost:5050
```

### Opção C — Sem webhook (mais rápido pra primeiro teste)

Você consegue testar **toda a tela e o fluxo de criar assinatura + cadastrar cartão no MP** sem webhook. O que não vai funcionar:
- Transição automática de status quando a primeira cobrança chegar
- Banner amarelo aparecer sozinho quando uma cobrança recorrente falhar
- Cancelamento iniciado pelo cliente direto no painel MP refletir aqui

Mas há **endpoints de dev** (ver seção 7) que simulam isso manualmente.

---

## 6. Fluxo end-to-end (com ou sem ngrok)

1. **Logue** como admin de um tenant (`/admin/login`)
2. Vá em **`/admin/minha-assinatura`**
3. Selecione um plano + cole o email do test_user (passo 3) → **Continuar para o cartão**
4. Abre janela nova no MP → cadastre `5031 4332 1540 6351`, CVV `123`, validade `12/30`, nome `APRO`
5. Confirme. MP retorna pra `/admin/minha-assinatura`
6. **Com ngrok:** status muda pra `Ativa` em ~30s (webhook → invalida cache → próximo fetch atualiza)
7. **Sem ngrok:** status fica como criou. Use os endpoints dev pra simular.

---

## 7. Endpoints dev — só funcionam em ambiente não-Production

Quando você roda local sem `ASPNETCORE_ENVIRONMENT=Production` (default é `Development`), estes endpoints estão disponíveis. Em produção retornam 404.

### Como tenant admin

#### Simular pagamento aprovado
Cria uma `FaturaAssinatura` paga + chama `RegistrarPagamento` na assinatura ativa.
```http
POST /api/v1/admin/assinatura/dev/simular-pagamento
Authorization: Bearer <admin-token>
```
Use isso pra sair de Atrasada/ReadOnly e voltar pra Ativa.

#### Forçar status específico
Útil pra ver a UI em cada estado.
```http
POST /api/v1/admin/assinatura/dev/forcar-status?status=ReadOnly
Authorization: Bearer <admin-token>
```
Valores aceitos: `Ativa`, `Atrasada`, `ReadOnly`, `Cancelada`, `Expirada`.

O use case ajusta as datas internas (`AssAtrasoDesde`, `AssReadOnlyDesde`) consistentemente com o status alvo — o `AssinaturaStatusJob` rodando depois não vai sair do estado pretendido por causa de datas inconsistentes.

### Como SuperAdmin

#### Seed de tenants demo
Cria 5 tenants — um por status — pra inspecionar a UI multi-tenant.
```http
POST /api/v1/superadmin/dev/seed-assinaturas-demo
Authorization: Bearer <superadmin-token>
```
Tenants criados (slugs):
- `demo-ativa`
- `demo-atrasada`
- `demo-readonly`
- `demo-cancelada`
- `demo-expirada`

Idempotente: rodar de novo só ignora os que já existem.

---

## 8. Inspecionar a UI em cada status

Com a assinatura no status alvo:

| Status | O que ver |
|---|---|
| `Trial` | (atualmente não usado — sem trial configurado) |
| `Ativa` | sem banner; tudo funciona normalmente |
| `Atrasada` | banner **amarelo** no topo do admin; ainda permite escrita; `/admin/minha-assinatura` mostra "Pagamento pendente" |
| `ReadOnly` | banner **vermelho**; POST/PUT/DELETE em endpoints admin retornam **402**; área pública `/api/v1/t/{slug}/...` retorna **503** |
| `Cancelada` | banner vermelho; idem ReadOnly |
| `Expirada` | banner vermelho; o `AssinaturaStatusJob` também soft-deleta o Tenant (D+30) |

Pra testar bloqueio em ReadOnly, tente criar um recurso depois de forçar o status:
```http
POST /api/v1/admin/recursos
# → 402 Payment Required, ProblemDetails com link pra /admin/minha-assinatura
```

E pra testar bloqueio público:
```http
GET /api/v1/t/seu-tenant-slug/servicos
# → 503 Service Unavailable
```

---

## 9. Gotchas comuns

| Sintoma | Causa provável |
|---|---|
| `400 Bad Request` ao criar preapproval | `payer_email` é o e-mail da própria conta MP — use o test_user. |
| Cartão recusado mesmo com nome `APRO` | Token de produção (`APP_USR-`) misturado com cartão de teste. Use token `TEST-`. |
| Webhook nunca chega | `APP_PUBLIC_URL` apontando pra `localhost` — MP não consegue chamar. Use ngrok. |
| Status não muda após pagamento aprovado no MP | Webhook secret divergente (TEST vs PROD). Configure o secret correto no painel MP **Webhooks → assinatura desse webhook**. |
| Banner não some após simular pagamento | Cache 30s — o endpoint `simular-pagamento` já invalida; se persistir, F5. |
| Trocar token MP não surte efeito | A API cacheia o header `Authorization` no `HttpClient` no startup. Reinicie a API. |
| `/admin/planos-catalogo` retorna 403 | Você não está logado como `SuperAdmin`. |

---

## 10. Quando subir pra produção

1. Trocar `MERCADOPAGO_ACCESS_TOKEN` pra credencial **de produção** (começa com `APP_USR-`)
2. Gerar `MERCADOPAGO_WEBHOOK_SECRET` no painel MP → **Webhooks → Configurar**
3. Setar `ASPNETCORE_ENVIRONMENT=Production` → endpoints `dev/*` retornam 404 automaticamente
4. Apontar `APP_PUBLIC_URL` pro domínio real
5. Cadastrar URLs de webhook no painel MP:
   - `https://seu-dominio.com.br/api/v1/webhooks/pagamento/MercadoPago` (transacional)
   - `https://seu-dominio.com.br/api/v1/webhooks/assinatura/MercadoPago` (SaaS)
6. **Limpar tenants demo** se rodou seed em produção por engano:
   ```sql
   DELETE FROM Assinatura WHERE R_TenId IN (SELECT TenId FROM Tenant WHERE TenSlug LIKE 'demo-%');
   DELETE FROM Tenant WHERE TenSlug LIKE 'demo-%';
   ```
