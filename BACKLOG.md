# Backlog AgendamentoPro

Itens não implementados nas últimas rodadas, com classificação honesta de
esforço/impacto. Ordenados por prioridade de produto (top = mais valor).

---

## Frontend pesado (UI das features de backend já existentes)

Backend pronto, só falta UI Angular. Se for atacar, fazer em ordem:

### 🟡 Tela "Meu Agendamento" pública (cliente self-service)
- Rota: `/t/:slug/meu-agendamento/:token`
- Lê endpoint: `GET /api/agendamentos/acesso/{token}`
- Botões: Cancelar (POST `/cancelar`), Reagendar (POST `/reagendar`)
- Esforço: ~2h. Inclui mostrar contagem regressiva, botão "ver no Google Maps", etc.

### 🟡 Tela admin de Bloqueios
- Rota: `/admin/bloqueios`
- CRUD em cima de `GET/POST /api/admin/bloqueios`
- Calendário visual com dias bloqueados marcados em vermelho.
- Esforço: ~3h.

### 🟡 Tela admin de Lista de Espera
- Rota: `/admin/lista-espera`
- Lista clientes em espera por data; botão "notificar" (abre wa.me com mensagem pré-preenchida)
- Esforço: ~2h.

### 🟡 Tela admin de Cupons
- Rota: `/admin/cupons`
- CRUD + alternar ativo
- Cliente final: campo "Cupom" no checkout (`/agendar/:servicoId`) chamando `/api/t/{slug}/cupons/{codigo}/validar`
- Esforço: ~3h backend já tem, só UI.

### 🟡 Tela admin de Audit Log
- Rota: `/admin/auditoria`
- Lista paginada, filtros por tabela/ação/data, drill-down no payload JSON
- Esforço: ~2h.

### 🟡 Tela admin de KPIs avançado
- Rota: `/admin/dashboard-kpis`
- Gráficos comparativos (Chart.js): mês atual vs anterior, taxa de cancelamento, ticket médio
- Esforço: ~4h.

### 🟡 Importação CSV de clientes
- Rota: `/admin/clientes/importar`
- Upload de arquivo + preview + confirmação
- Esforço: ~2h.

### 🟡 Fechar Caixa do Dia
- Rota: `/admin/caixa`
- Resumo do dia + botão "Imprimir/exportar PDF"
- Esforço: ~2h.

### 🟡 Setup 2FA do admin
- Rota: `/admin/seguranca/2fa`
- 3 passos: iniciar (mostra QR via `qrcode` lib em JS), confirmar com código, sucesso
- Endpoints já existem em `/api/admin/2fa/{iniciar,confirmar,desativar}`
- Esforço: ~2h.

### 🟢 Avaliação automática via WhatsApp
- Backend já tenta, mas template `link_avaliacao` precisa ser criado/aprovado na Meta.
- Documentar em `docs/setup-whatsapp-business.md` (template já está mencionado).

---

## Refactors maiores (1+ dia cada)

### 🟡 PWA (Progressive Web App)
- Adicionar `manifest.json` + service worker + ícones em várias resoluções
- Workbox para offline cache de assets
- Esforço: 1-2 dias bem feito (com offline mode pra agendamento)

### 🟡 i18n (PT-BR, EN, ES)
- @ngx-translate/core ou Angular built-in i18n
- Refactor de TODOS os templates
- Esforço: 2-3 dias

### 🟡 Dark mode
- CSS variables ↔ data-theme="dark"
- Detecção via `prefers-color-scheme`
- Esforço: 4-6h

### 🟡 Notificação in-app realtime (admin)
- SignalR no backend + observable no Angular
- "Toast" quando novo agendamento aprovado / pagamento confirmado
- Esforço: 1-2 dias

### 🟡 NF-e/NFS-e
- Integração com NFe.io, eNotas, ou prefeitura municipal direto
- Custo: licença + transação
- Esforço: 1 semana+

### 🟡 Recorrência de agendamento
- "Toda 2ª-feira por 4 semanas" cria N agendamentos com link
- Cancelamento da série
- Esforço: 1-2 dias

### 🟡 Programa de fidelidade
- Pontos por agendamento concluído, troca por desconto
- Entidade Pontos + extrato + regra de conversão
- Esforço: 2-3 dias

### 🟡 Pacotes pré-pagos
- "Cliente compra 5 lavagens" → entity Saldo, débito a cada agendamento
- Integração com pagamento (cobra tudo upfront)
- Esforço: 2-3 dias

---

## Tech debt (importante mas pouca visibilidade)

### 🟡 Frontend tests (Cypress / Playwright)
- Hoje: zero. Backend tem 89.
- Mínimo: smoke test de login + criar agendamento.
- Esforço: 1 dia primeira vez, depois ~30min/feature.

### 🟡 Versionamento de API (`/v1/`)
- Hoje endpoints são `/api/...` — quebrar API quebra todos os clientes
- Refactor: prefixo `/v1/` em todos os controllers; manter `/api/...` como alias por 6 meses
- Esforço: 4h + 2h pra atualizar frontend

### 🟡 Storage S3-compatível
- Hoje: `LocalFotoStorage`. Em produção real precisa: backup, CDN, multi-instância.
- Implementar `S3FotoStorage` (AWS, MinIO, Backblaze B2)
- Esforço: 4-6h

### 🟡 Hangfire com storage persistente
- Hoje: `UseMemoryStorage()` — jobs perdidos em restart
- Trocar por `UseSqlServerStorage()` ou `UsePostgreSqlStorage()`
- Esforço: 2h + tabelas de Hangfire no DB

### 🟢 Resize de imagem assíncrono
- Hoje: resize bloqueia upload. Em prod com fila grande, atrapalha.
- Mover para Hangfire job
- Esforço: 1h

### 🟢 SonarCloud / CodeQL
- Análise estática automatizada no CI
- Esforço: 30min de configuração

### 🟢 Codecov
- Publicar cobertura por PR
- Esforço: 30min

---

## Correções/observações pequenas

### 🟢 Vulnerabilidade ImageSharp 3.1.5 (NU1902)
- Update pra 3.1.6+ quando sair patch (hoje 3.1.5 é o mais novo da 3.1.x).
- Mitigação: só usuários autenticados fazem upload; não expomos parsing de imagem aleatória.

### 🟢 Hangfire dashboard sem CSRF protection extra
- Token JWT no header já é proteção. Em ambiente high-stakes, adicionar Bearer obrigatório no Hangfire também.

### 🟢 Resize não regenera o `FotTamanhoBytes` no banco
- Após resize o tamanho real é menor que o registrado. Update pós-resize.
- Esforço: 30min

### 🟢 Hangfire recurring job + DATABASE_MULTITENANCY=PerTenant
- Hoje o job `LembreteJob` usa o DbContext do scope, que em PerTenant resolve por tenant — mas o job é GLOBAL (sem tenant). Vai cair no DB shared, que não tem agendamentos.
- Fix: iterar tenants e para cada um abrir scope com tenantId setado, OU criar job por tenant.
- Esforço: 2h

---

## Decisões a tomar antes de continuar

1. **Vai vender pra clientes externos?** Se sim, NF-e e LGPD avançado (2FA, audit UI) viram blockers.
2. **Mobile-first ou app dedicado?** PWA cobre 80%; app nativo só faz sentido com push notification real.
3. **Multi-region?** Hoje monolito só sai daí com replicação de DB (read-replicas) — depende de demanda.
4. **Quanto pagar pra usar?** Modelo de cobrança não definido (mensalidade fixa, % de transação, freemium).

---

Última atualização: ver `git log` deste arquivo.
