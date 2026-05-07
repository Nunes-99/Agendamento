# 📄 ESPECIFICAÇÃO COMPLETA (NÍVEL PRODUÇÃO)
## Sistema de Agendamento para Lava Rápido (Integrado ao Connect Veículos)

---

# 🎯 VISÃO GERAL
Sistema web completo para agendamento de lavagem de veículos com:
- Frontend em Angular (mobile-first, REM)
- Backend em ASP.NET Core (C#)
- Integração com sistema base Connect Veículos
- Pagamento antecipado (20%)
- Regras avançadas de agenda, concorrência e cancelamento
- Painel administrativo completo

---

# 🧱 STACK TECNOLÓGICA

## Backend
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- JWT Auth

## Frontend
- Angular
- Angular Material
- CSS usando REM
- Mobile-first obrigatório

## Integrações
- Mercado Pago (crédito, débito, PIX)
- WhatsApp (link direto inicialmente)

---

# 🧩 INTEGRAÇÃO COM CONNECT VEÍCULOS
- Reaproveitar cadastro de clientes (se existir)
- Reaproveitar autenticação
- Criar módulo separado: AGENDAMENTO
- Não acoplar regras de negócio existentes

---

# 🗄️ MODELAGEM DE DADOS

## Tabela: Servicos
- Id
- Nome
- Descricao
- Preco
- DuracaoMinutos
- ImagemUrl
- Ativo

## Tabela: Agendamentos
- Id
- ClienteId
- ServicoId
- Data
- HoraInicio
- HoraFim
- Status
- PagamentoStatus
- ValorTotal
- ValorEntrada
- BoxId
- DataCriacao

## Índice obrigatório:
UNIQUE(Data, HoraInicio, BoxId)

---

# 🔒 REGRAS CRÍTICAS

## Concorrência
- Usar transação no banco
- Validar antes de inserir
- Índice único obrigatório

## Status
- PendentePagamento
- Confirmado
- Cancelado
- Concluido
- NoShow

---

# 💳 PAGAMENTO

## Formas:
- Crédito
- Débito
- PIX

## Regras:
- 20% antecipado
- Confirmação via webhook
- Expiração: 10–15 minutos

## Dinheiro:
- Apenas admin pode usar

---

# 📅 REGRAS DE AGENDA

## Bloqueio por duração
- Serviço ocupa intervalo completo

## Buffer
- Intervalo entre atendimentos

## Multi-box
- Permitir múltiplos atendimentos simultâneos

---

# 🔄 REAGENDAMENTO

- Apenas data/horário
- Mantém valor pago
- Respeita regra de 24h

---

# ⛔ CANCELAMENTO

- >24h: reagendar
- <24h: sem reembolso

---

# 📲 WHATSAPP

Mensagens:
- Confirmação
- Lembrete 24h
- Lembrete 2h
- Cancelamento

---

# 🧑‍💼 ADMIN

## Funcionalidades
- Login
- Dashboard
- Agenda
- Relatórios
- WhatsApp

---

# 📊 RELATÓRIOS

- Receita
- Serviços mais vendidos
- Taxa de ocupação
- Cancelamentos

---

# 🚀 MELHORIAS

## Monetização
- Upsell
- Combos
- Planos mensais

## Experiência
- Avaliação
- Fotos antes/depois

## Automação
- Lembretes
- Reativação de clientes

---

# 📱 UI/UX

- Mobile-first
- REM obrigatório
- Interface simples

---

# 🗂️ BACKLOG COMPLETO

## Fase 1
- Setup backend/frontend

## Fase 2
- CRUD serviços

## Fase 3
- Agenda

## Fase 4
- Agendamento

## Fase 5
- Pagamento

## Fase 6
- Admin

## Fase 7
- WhatsApp

## Fase 8
- Melhorias

## Fase 9
- Testes

---

# 🧪 TESTES OBRIGATÓRIOS

- Concorrência
- Pagamento
- Cancelamento
- Responsividade

---

# 🎯 OBJETIVO FINAL
Sistema profissional, escalável e vendável (SaaS-ready).
