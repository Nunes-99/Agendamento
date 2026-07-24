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
3. Path `/api/v1/t/{slug}/...` (endpoints públicos)

## API Versioning

Todas as rotas estão sob `/api/v1/`. Health checks ficam fora da versão (`/api/health/live`, `/api/health/ready`) por convenção. Webhooks externos: `/api/v1/webhooks/pagamento/{gateway}`.

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
  - `/t/:slug/entrar` e `/t/:slug/minha-conta` → login OTP por WhatsApp + área do cliente final (histórico, pacotes, fidelidade, perfil)
  - `/t/:slug/pacotes` → compra de pacote pré-pago (PIX)
  - `/t/:slug/lista-espera-publica` → entrar na fila quando data está cheia
  - `/admin/login` → login do administrador
  - `/admin/{dashboard,agenda,servicos,recursos,clientes,combos,relatorios,configuracoes,avaliacoes}`
  - `/admin/{recorrencias,pacotes,fidelidade,bloqueios,lista-espera,kpis,caixa,cupons,lgpd}`
  - `/admin/seguranca/2fa` → autenticação em dois fatores (TOTP)
  - `/admin/importar-clientes` → importação de clientes por CSV
  - `/admin/empresas` → gestão de tenants (super-admin)
  - `/admin/agendamentos/:id/fotos` → upload e galeria de fotos antes/depois
  - `/esqueci-senha` e `/redefinir-senha?token=...` → fluxo público de reset

### Realtime

`SignalR` em `/hubs/notificacoes` notifica o admin do tenant em tempo real para os eventos:
- `novo-agendamento`
- `pagamento-aprovado`
- `agendamento-cancelado`

O `RealtimeService` no Angular reconecta automaticamente; o `AdminShell` exibe sino com badge das últimas 20 notificações.

Em paralelo, **Web Push (VAPID)** envia notificação ao dispositivo do admin **mesmo com app fechado**, complementando o SignalR (que só funciona com aba aberta). Setup:

1. Em dev, gere o par VAPID uma única vez: `curl -X POST http://localhost:5050/api/v1/admin/web-push/generate-keys`
2. Cole os valores em `VAPID_PUBLIC_KEY` e `VAPID_PRIVATE_KEY` no `.env` e reinicie
3. Admin → Configurações → aba "Notificações" → ativar toggle

Em produção, gere o par via ferramenta externa equivalente (ex: lib `web-push` do npm — `web-push generate-vapid-keys`). **Nunca rotacione** — os browsers cacheiam a chave pública nas subscriptions.

### PWA

App é instalável (Android/iOS) com service worker (`@angular/service-worker`). Cache offline de assets e endpoints públicos por tenant (1h freshness). Habilitado só no build de produção.

## Como executar

### Setup inicial

1. Copie `.env.example` para `.env` e preencha os valores (chaves do Mercado Pago, WhatsApp, JWT secret).
2. Para SQLite local não há mais nada a configurar.

> **SQL Server não é suportado hoje.** O provider existe no código, mas **todas as
> migrations foram geradas contra o SQLite** — são 333 colunas declaradas como
> `TEXT`/`INTEGER`, tipos que no SQL Server ou são obsoletos (`TEXT` não aceita
> índice único) ou têm semântica diferente. Aplicá-las lá produz um schema
> inválido, e o problema só apareceria com dado de cliente dentro.
>
> Por isso o startup **falha imediatamente** com `Database:Provider=SqlServer`.
> Para habilitar de verdade, o caminho é o padrão do EF: um conjunto de migrations
> **por provider** (assemblies separados) e a suíte rodando contra uma instância
> real. Até lá, use SQLite.

## Documentação adicional

- 📘 [`docs/setup-whatsapp-business.md`](docs/setup-whatsapp-business.md) — passo a passo para criar conta Meta Business, registrar número, gerar token permanente e submeter templates `lembrete_24h`/`lembrete_2h`.
- 📘 [`docs/setup-mercado-pago.md`](docs/setup-mercado-pago.md) — passo a passo para criar aplicação MP, configurar webhook, gerar access token e webhook secret.
- 🛠️ [`scripts/backup-sqlite.sh`](scripts/backup-sqlite.sh) e [`scripts/restore-sqlite.sh`](scripts/restore-sqlite.sh) — backup online do SQLite + uploads, com retenção configurável e cron sugerido.

### Backend (dev)

```bash
cd back-end
dotnet restore
dotnet run --project AgendamentoPro.API
```

A API sobe em `http://localhost:5050` com Swagger habilitado em `/swagger`. Endpoints versionados em `/api/v1/`.

### Autenticação cliente final (OTP via WhatsApp)

`POST /api/v1/t/{slug}/otp/solicitar { telefone }` → envia código de 6 dígitos via template WhatsApp (em dev, sem WhatsApp configurado, retorna `codigoDev` no response).
`POST /api/v1/t/{slug}/otp/validar { telefone, codigo }` → retorna JWT cliente (validade 7 dias, role=Cliente, claim `clienteId`).

Limites: 1 envio por minuto, 5 envios por hora por telefone, 3 tentativas por código, validade 10 minutos.

### Observabilidade

- Erros não tratados retornam **ProblemDetails** (RFC 7807) com `traceId`.
- Header `X-Correlation-Id` é gerado se não fornecido e ecoado no response — útil para correlacionar logs/respostas.
- `/api/health/live` (liveness) e `/api/health/ready` (readiness: DB + integrações).

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
| `APP_FRONTEND_URL` | URL pública do frontend (ex: `https://app.suaempresa.com.br`) — usada nos links de e-mail | — |
| `SMTP_HOST` / `SMTP_PORT` / `SMTP_USERNAME` / `SMTP_PASSWORD` / `SMTP_FROM_EMAIL` / `SMTP_FROM_NAME` / `SMTP_USE_SSL` | Configuração SMTP para envio de e-mails de reset de senha (opcional) | Gmail App Password, SendGrid, Mailgun, AWS SES, etc. |
| `SUPERADMIN_EMAIL` / `SUPERADMIN_PASSWORD` | Credenciais do super-admin | Defina o que quiser |
| `UPLOADS_PATH` | Pasta para fotos (default `/data/uploads` no compose, ignorado se `STORAGE_PROVIDER=s3`) | Volume persistente |
| `STORAGE_PROVIDER` | `local` (default) ou `s3` | — |
| `S3_BUCKET` / `S3_REGION` / `S3_ENDPOINT` / `S3_ACCESS_KEY` / `S3_SECRET_KEY` / `S3_PUBLIC_BASE_URL` / `S3_FORCE_PATH_STYLE` | Configuração do bucket S3 (ou MinIO/B2/R2). Veja `Storage de fotos` abaixo. | AWS console / MinIO / Backblaze |

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
| **Logs** | Serilog enriquecido com CorrelationId / TenantId / UserId / Environment / MachineName por request |
| **Audit** | EF SaveChangesInterceptor grava `LogAuditoria` em INSERT/UPDATE/DELETE com user/IP/old+new (mascarando senhas/tokens) |
| **Soft Delete** | Filter global automático em entidades `ISoftDeletable` — `.IgnoreQueryFilters()` recupera quando preciso |
| **Cross-tenant guard** | `OnTokenValidated` rejeita token cujo tenantId não bate com o tenant resolvido pelo path/header |
| **Forwarded headers** | API atrás de proxy reverso enxerga scheme/IP/host reais |
| **Background jobs** | Hangfire (in-memory storage) com retry automático + dashboard `/hangfire` (admin) |
| **Cache** | `ITenantCache` (decorator do IMemoryCache) com prefixo `tenant:{id}:` automático |

## Multi-tenancy: modo de banco

Por default cada deploy usa **um banco compartilhado** com isolamento via foreign key
`R_TenId` em todas as entidades + índices compostos. Funciona bem até centenas de
tenants e é o recomendado.

Para deploys que precisam de isolamento físico (LGPD-friendly, backup por tenant):

```env
DATABASE_MULTITENANCY=PerTenant
TENANTS_PATH=/data/tenants
```

Em modo **PerTenant** cada tenant tem seu próprio arquivo SQLite. Para inicializar
o banco físico de um tenant:

```http
POST /api/tenants/{id}/inicializar-database
Authorization: Bearer <token-superadmin>
```

A operação é idempotente. A migração de dados existentes (Shared → PerTenant)
precisa de tooling que copie linhas — não está incluído ainda.

## Storage de fotos

Default é **disco local** (`LocalFotoStorage`): arquivos em `UPLOADS_PATH`, servidos via `/uploads/...`. Para múltiplas réplicas ou backup gerenciado, use **S3 ou compatível** (MinIO, Backblaze B2, Cloudflare R2):

```env
STORAGE_PROVIDER=s3
S3_BUCKET=meu-bucket
S3_REGION=us-east-1
# Opcionais (MinIO/B2/R2):
S3_ENDPOINT=https://s3.us-west-002.backblazeb2.com
S3_FORCE_PATH_STYLE=true
S3_PUBLIC_BASE_URL=https://cdn.exemplo.com  # se usar CloudFront/Fastly
# Credenciais (default usa chain provider AWS — IAM role/instance profile):
S3_ACCESS_KEY=...
S3_SECRET_KEY=...
```

> **Resize**: o `FotoResizeJob` precisa de um caminho local para o ImageSharp. Em modo S3 o resize é pulado e o upload original fica como está. Para resize pós-upload em S3, configure `S3 Event → Lambda` (AWS) ou um worker externo que baixa-redimensiona-sobe. Frontend pode usar query params do CloudFront/Imgix para resize on-the-fly.

## Background jobs (lembretes)

Lembretes 24h/2h rodam via **Hangfire** com storage persistente — jobs sobrevivem a restart do processo:

- **Provider SQLite** (default): arquivo separado `hangfire.db` em `AppContext.BaseDirectory` (configurável via `HANGFIRE_DB_PATH`).
- **Provider SqlServer**: usa a mesma connection string da aplicação; Hangfire cria o schema `[HangFire]` automaticamente no primeiro boot.
- **Escape hatch**: `HANGFIRE_STORAGE=Memory` reativa o modo in-memory (útil em testes/CI; jobs somem em restart).

Outros pontos:
- Retry automático (3 tentativas, backoff 60s/5min/15min)
- Dashboard em `/hangfire` (autenticado, SuperAdmin/Administrador)
- Para reativar o BackgroundService legado, set `USE_LEGACY_REMINDER=true`

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
- **Pagamento via gateway**: abstração `IGatewayPagamento`. Mercado Pago (PIX, cartão, checkout) sempre disponível; Stripe ativa quando `STRIPE_SECRET_KEY` está setado (cartão crédito/débito via Checkout Session). PIX no Stripe não é suportado — use MP. Webhook Stripe valida `Stripe-Signature`.
- **WhatsApp Cloud API**: integração via `INotificadorWhatsApp` + `BackgroundService` envia lembretes 24h e 2h antes do agendamento (templates `lembrete_24h` e `lembrete_2h` precisam ser pré-aprovados na Meta). Quando WhatsApp falha (template rejeitado, número sem WhatsApp), o `LembreteJob` faz fallback automático para **SMS via Twilio** se `TWILIO_*` estiver configurado.
- **Avaliação**: ao concluir agendamento, abre token público; cliente avalia 1-5 estrelas + comentário em `/avaliar/{token}`. Médias e últimas avaliações públicas no perfil do tenant.
- **Fotos antes/depois**: upload por agendamento (até 10 MB, jpg/png/webp/gif). Servidas estaticamente em `/uploads/...`.
- **Combos**: agrupa N serviços com preço promocional. Catálogo público + fluxo de agendamento que cria N agendamentos contíguos no mesmo recurso (vinculados via `AgeGrupoComboId`) com cobrança agregada única. Cancelar 1 cancela todo o grupo; reagendar individual é bloqueado (cancele e crie novo).
- **Reset de senha**: fluxo `/esqueci-senha` → token por e-mail (uso único, válido 1h) → `/redefinir-senha`. Quando SMTP configurado envia automaticamente; senão loga o link para o operador entregar manualmente. Refresh tokens existentes são revogados ao trocar a senha.

## Extensibilidade

- Novos gateways de pagamento: implemente `IGatewayPagamento` no Infrastructure e registre no IoC.
- Storage de fotos: `S3FotoStorage` já incluído (S3, MinIO, B2, R2 — ative com `STORAGE_PROVIDER=s3`). Para Azure Blob ou outros, implemente `IFotoStorage`.
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
- **Erros do navegador chegam ao log do servidor** via `POST /api/v1/erros-cliente`.
  Um `ErrorHandler` global e o interceptor de HTTP reportam o que antes morria no
  console de quem estivesse com o F12 aberto — foi assim que o SignalR ficou sem
  conectar sem ninguém perceber. Falha de servidor (5xx) e queda de rede também
  passaram a avisar o usuário na tela, em vez de deixá-la vazia.

## Testes de fumaça

`AgendamentoPro.Tests/Fumaca` sobe a aplicação DE VERDADE (`WebApplicationFactory`)
e percorre o caminho crítico: health, login do super-admin, cadastro de um tenant
novo e login do admin criado. Existe porque dois defeitos graves — a API não subia
por dependência não registrada, e nenhum tenant podia ser criado — atravessaram
285 testes verdes, já que todos eles montam seus objetos à mão com Moq.

`AgendamentoPro.Tests/IoC/ContainerTests` faz o que o host faz ao subir:
`BuildServiceProvider` com validação ligada. Qualquer dependência esquecida
quebra ali, em segundos.

## Roadmap

Próximos itens (não implementados ainda):
- SQL Server de verdade: migrations por provider (ver aviso em *Setup inicial*).
- Gateway Pagar.me como alternativa ao Mercado Pago para recorrência.
- Resize de fotos quando `STORAGE_PROVIDER=s3` (hoje o resize é pulado).
- i18n (PT-BR / EN / ES).
- App nativo — hoje coberto por PWA + Web Push.

> Já entregues, apesar de constarem como pendentes em versões anteriores deste
> arquivo: SMS fallback via Twilio, gateway Stripe, `S3FotoStorage` e os
> relatórios avançados (LTV, no-show, sazonalidade).
