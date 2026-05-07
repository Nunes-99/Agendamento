# AgendamentoPro

Sistema multi-tenant genérico de agendamento online — extraído da especificação de lava-rápido, mas
genérico o suficiente para qualquer empreendimento que faça agendamento (barbearia, clínica, salão,
estética, oficina, consultoria etc).

## Arquitetura

Inspirado no projeto **ConnectVeiculos**, segue **Clean Architecture**:

```
back-end/
  AgendamentoPro.sln
  AgendamentoPro.Core/           ← Entidades, Enums, Interfaces, Exceptions (sem dependências)
  AgendamentoPro.Application/    ← UseCases, InputModels, ViewModels, Validators (FluentValidation)
  AgendamentoPro.Infrastructure/ ← EF Core, Repositórios, IoC, Middlewares, Services, Migrations
  AgendamentoPro.API/            ← Controllers, Program.cs, JWT, Swagger, HealthChecks, RateLimiter
  AgendamentoPro.Tests/          ← xUnit + FluentAssertions + SQLite in-memory

front-end/
  AgendamentoPro.Web/            ← Angular 17 standalone, Material, mobile-first em REM
```

## Multi-tenant

Cada **Tenant** (empresa cliente do SaaS) tem:

- Slug único (`acme-lava-rapido`) — usado em rotas `/t/:slug/...`
- Personalização visual (logo, banner, cores primária/secundária/acento, fonte, favicon)
- Regras próprias (% de entrada, buffer entre atendimentos, antecedências mínima/máxima, prazo de cancelamento)
- Horários de funcionamento por dia da semana
- Recursos (boxes, salas, profissionais — termo genérico)
- Serviços, Combos, Clientes, Agendamentos, Pagamentos, Avaliações isolados

A resolução do tenant em cada request acontece em três níveis (em ordem):

1. Claim `tenantId` do JWT (uso administrativo)
2. Header `X-Tenant-Slug` (definido pelo frontend)
3. Path `/api/t/{slug}/...` (endpoints públicos)

## Frontend

- Angular 17 standalone, mobile-first
- Todos os tamanhos em **REM**
- Theming dinâmico via CSS Custom Properties — o `ThemeService` injeta as cores do tenant em tempo de carregamento, permitindo total personalização do site público sem alterar código.
- Rotas:
  - `/t/:slug` → home pública do estabelecimento (com banner, logo, cores, avaliações públicas)
  - `/t/:slug/servicos` → catálogo
  - `/t/:slug/combos` → combos promocionais
  - `/t/:slug/agendar/:servicoId` → fluxo passo-a-passo (horário → dados → pagamento)
  - `/t/:slug/pagamento/:id` → QR Code PIX / link / status em tempo real
  - `/t/:slug/confirmacao/:id` → tela de sucesso
  - `/avaliar/:token` → cliente final responde avaliação (sem login)
  - `/admin/login` → login do administrador
  - `/admin/{dashboard,agenda,servicos,recursos,clientes,combos,relatorios,configuracoes,avaliacoes}`

## Como executar

### Setup inicial

1. Copie `.env.example` para `.env` e preencha os valores (chaves do Mercado Pago, WhatsApp, JWT secret).
2. Para SQLite local não há mais nada a configurar; para SQL Server ajuste `Database__Provider` e `ConnectionStrings__Default` em `.env`.

### Backend (dev)

```bash
cd back-end
dotnet restore
dotnet run --project AgendamentoPro.API
```

A API sobe em `http://localhost:5050` com Swagger habilitado em `/swagger`.

**Banco**: SQLite por default (`agendamento.db`). Schema é criado/atualizado automaticamente via EF Migrations.

**Usuário SuperAdmin**: criado automaticamente no primeiro boot.
Por padrão o e-mail é `admin@agendamentopro.local` e a **senha é gerada aleatoriamente e mostrada UMA VEZ no log** (procure por "SUPER ADMIN criado com senha aleatória"). Anote em local seguro.

### Frontend (dev)

```bash
cd front-end/AgendamentoPro.Web
npm install
npm start
```

Acessível em `http://localhost:4200`.

### Docker Compose (prod-like)

```bash
cp .env.example .env   # preencha JWT_SECRET_KEY, MERCADOPAGO_*, WHATSAPP_*, SUPERADMIN_*
docker compose up -d
```

API: `http://localhost:5050` · Web: `http://localhost:4200`

Volumes persistentes:
- `api-data` → `/data/agendamento.db` (SQLite) e uploads de fotos

Healthchecks built-in nos containers — orquestrador detecta containers com banco offline.

## Configuração obrigatória em produção

| Variável | O que é | Onde obter |
|---|---|---|
| `JWT_SECRET_KEY` | Chave para assinar tokens (mínimo 64 chars) | `openssl rand -base64 64` |
| `APP_PUBLIC_URL` | URL pública da API (HTTPS obrigatório em prod) | Ex: `https://api.suaempresa.com` |
| `ALLOWED_ORIGINS` | Origens CORS permitidas (vírgula) | Ex: `https://suaempresa.com.br` |
| `MERCADOPAGO_ACCESS_TOKEN` | Access token de produção | https://www.mercadopago.com.br/developers/panel/app |
| `MERCADOPAGO_WEBHOOK_SECRET` | Secret do webhook | Painel MP → Notificações → Webhooks |
| `WHATSAPP_ACCESS_TOKEN` | System User Token | https://developers.facebook.com → WhatsApp → API Setup |
| `WHATSAPP_PHONE_NUMBER_ID` | ID do número que envia | Mesma página acima |
| `SUPERADMIN_EMAIL` / `SUPERADMIN_PASSWORD` | Credenciais do super-admin | Defina o que quiser |
| `UPLOADS_PATH` | Pasta para fotos (default `/data/uploads` no compose) | Volume persistente |

Em **Development** o sistema usa um JWT secret padrão e gera senha aleatória se não houver `SUPERADMIN_PASSWORD`.
Em **Production** o startup **falha imediatamente** se:
- `JWT_SECRET_KEY` não estiver definido (mínimo 64 chars);
- `APP_PUBLIC_URL` não usar HTTPS.

## Hardening de segurança

| Camada | O que está implementado |
|---|---|
| **Autenticação** | JWT com refresh token; secret de 64+ chars obrigatório em prod |
| **Autorização** | Policies `SuperAdmin`, `AdminTenant`, `Atendente` |
| **CORS** | Whitelist explícita de origens, métodos e headers (sem `AllowAny*`) |
| **Rate limiting** | `auth` (5 req/min/IP), `webhook` (60), global (120) com janela fixa |
| **Webhook MP** | Validação HMAC + timestamp window (5 min replay protection) |
| **Webhook idempotência** | Tabela `WebhookEvento` com unique-index `(Gateway, EventoId)` |
| **Concorrência** | Unique-index `(R_RecId, AgeData, AgeHoraInicio)` no `Agendamento` |
| **Validação** | FluentValidation em todos os InputModels (action filter global) |
| **Headers HTTP** | nginx adiciona CSP, HSTS, X-Frame-Options, X-Content-Type-Options |
| **Logs** | Serilog enriquecido com CorrelationId / TenantId / UserId por request |

## Migrations

Schema evolui via EF Migrations (não usa `EnsureCreated()` em produção):

```bash
cd back-end
# Gerar nova migration
dotnet ef migrations add NomeDaMigration \
  --project AgendamentoPro.Infrastructure \
  --startup-project AgendamentoPro.API \
  --output-dir Database/Migrations

# Aplicar (acontece automaticamente no startup; manual se necessário):
dotnet ef database update \
  --project AgendamentoPro.Infrastructure \
  --startup-project AgendamentoPro.API
```

No startup, todas as migrations pendentes são aplicadas automaticamente. Se ainda não houver nenhuma migration no assembly, o sistema cai em `EnsureCreated()` como bootstrap.

## Backup

### SQLite (default)

O arquivo `agendamento.db` mais o diretório `uploads/` ficam em `/data` (volume `api-data` no compose). Para backup:

```bash
# Stop API para garantir consistência (ou use sqlite3 .backup):
docker compose stop api
docker run --rm -v agendamento_api-data:/data -v $(pwd):/backup alpine \
  tar czf /backup/agendamento-$(date +%Y%m%d-%H%M).tar.gz -C /data .
docker compose start api
```

Para restore: pare o serviço, extraia o tar.gz no volume e suba novamente.

### SQL Server

Use `BACKUP DATABASE` do próprio SQL Server agendado via SQL Agent ou cron + `sqlcmd`. Os uploads ficam separados — agende rsync/snapshot do diretório `UPLOADS_PATH`.

## Regras de negócio implementadas

- **Concorrência**: índice único `(R_RecId, AgeData, AgeHoraInicio)` + validação prévia + transação. `ConcorrenciaException` traduzida automaticamente em 400.
- **Pagamento antecipado**: % de entrada configurável por tenant (default 20%), expiração 15 min.
- **Status do agendamento**: `PendentePagamento → Confirmado → EmAndamento → Concluido` (ou `Cancelado` / `NoShow`).
- **Reagendamento**: somente com antecedência ≥ limite do tenant (default 24h), mantém valor pago.
- **Cancelamento**: registra motivo e data.
- **Pagamento via gateway**: abstração `IGatewayPagamento` (Mercado Pago implementado; PIX, cartão, checkout).
- **WhatsApp Cloud API**: integração via `INotificadorWhatsApp` + `BackgroundService` envia lembretes 24h e 2h antes do agendamento (templates `lembrete_24h` e `lembrete_2h` precisam ser pré-aprovados na Meta).
- **Avaliação**: ao concluir agendamento, abre token público; cliente avalia 1-5 estrelas + comentário em `/avaliar/{token}`. Médias e últimas avaliações públicas no perfil do tenant.
- **Fotos antes/depois**: upload por agendamento (até 10 MB, jpg/png/webp/gif). Servidas estaticamente em `/uploads/...`.
- **Combos**: agrupa N serviços com preço promocional; visíveis no catálogo público.

## Extensibilidade

- Novos gateways de pagamento: implemente `IGatewayPagamento` no Infrastructure e registre no IoC.
- Storage de fotos remoto (S3, Azure Blob): implemente `IFotoStorage` substituindo `LocalFotoStorage`.
- Novo canal de notificação (e-mail, push): adicione um service no Core e implemente.
- Novos relatórios: adicione método em `IRelatoriosUseCase`.

## Testes

Suíte em `AgendamentoPro.Tests` (xUnit + FluentAssertions + Moq + SQLite in-memory).

```bash
cd back-end
dotnet test
```

Cobre:
- Transições de status do `Agendamento` (Confirmar → Iniciar → Concluir, idempotência, falhas).
- Comportamento idempotente do `Pagamento` (Aprovar/Recusar/Estornar/Expirar).
- Concorrência de agendamento no mesmo recurso/data/hora (unique-index).
- Idempotência de `WebhookEvento` (unique `(Gateway, EventoId)`).
- Validators FluentValidation (login, agendamento, serviço, slug de tenant).

## Observabilidade

- **Logs estruturados** (Serilog) com CorrelationId/TenantId/UserId — facilita correlacionar requests no Kibana/Grafana Loki.
- **Health checks**: `GET /api/health/live` (processo) e `GET /api/health/ready` (banco). Use no orquestrador.
- **Header `X-Correlation-Id`** ecoado em toda response — frontend pode logar pra debug.

## Roadmap

Próximos itens (não implementados ainda):
- Integração com SMS para fallback do WhatsApp.
- Gateway Stripe / Pagar.me.
- Storage S3 nativo (substituir LocalFotoStorage).
- Relatórios mais ricos (LTV cliente, taxa de no-show).
- App mobile com push notifications.
