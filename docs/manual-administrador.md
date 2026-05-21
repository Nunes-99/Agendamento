# Manual do Administrador

> Referência completa de cada funcionalidade do painel administrativo
> do AgendamentoPro. Para um **guia inicial passo-a-passo**, comece
> pelo [Tutorial de Primeiros Passos](tutorial-primeiros-passos.md).
>
> Este documento é organizado **por módulo**, não por jornada. Use o
> índice para pular direto pro que você precisa.

---

## Índice

1. [Login e Sessão](#1-login-e-sessão)
2. [2FA (Verificação em dois fatores)](#2-2fa-verificação-em-dois-fatores)
3. [Dashboard](#3-dashboard)
4. [Agenda](#4-agenda)
5. [Agendamentos](#5-agendamentos)
6. [Serviços](#6-serviços)
7. [Recursos](#7-recursos)
8. [Combos](#8-combos)
9. [Pacotes pré-pagos](#9-pacotes-pré-pagos)
10. [Cupons](#10-cupons)
11. [Fidelidade](#11-fidelidade)
12. [Recorrências](#12-recorrências)
13. [Clientes](#13-clientes)
14. [Importação CSV de clientes](#14-importação-csv-de-clientes)
15. [Bloqueios de agenda](#15-bloqueios-de-agenda)
16. [Lista de espera](#16-lista-de-espera)
17. [Avaliações](#17-avaliações)
18. [Fotos antes/depois](#18-fotos-antesdepois)
19. [Caixa do dia](#19-caixa-do-dia)
20. [KPIs](#20-kpis)
21. [Relatórios](#21-relatórios)
22. [Auditoria](#22-auditoria)
23. [Configurações](#23-configurações)
24. [Notificações Web Push](#24-notificações-web-push)
25. [LGPD](#25-lgpd)
26. [Perfis e permissões](#26-perfis-e-permissões)
27. [Glossário](#27-glossário)

---

## 1. Login e Sessão

### Acesso

URL: `seu-dominio.com.br/admin/login`

### Login normal

1. Digite **e-mail** e **senha**
2. Clique em **Entrar**
3. Se 2FA estiver ativo, digite o código de 6 dígitos do app
4. Você cai no **Dashboard** automaticamente

### Sessão

- **Duração:** 8 horas (configurável). Após esse tempo, o sistema
  pede login novamente.
- **Renovação automática:** se você estiver usando o sistema, a sessão
  é renovada via "refresh token" — você não percebe.
- **Várias abas:** funcionam todas com a mesma sessão.

### Sair

Menu superior direito → **Sair**. O frontend descarta os tokens
locais. (O refresh token no servidor expira sozinho em 7 dias.)

### Esqueci a senha

1. Clique em **Esqueci a senha** na tela de login
2. Digite seu e-mail
3. Receberá um e-mail com link (validade: **1 hora**)
4. Clique no link → defina nova senha (mínimo 8 caracteres)
5. **Atenção:** trocar a senha **revoga todos os refresh tokens
   existentes**. Você precisará fazer login em todos os dispositivos
   novamente — segurança caso outro acesso esteja vazado.

### Lockout (bloqueio por tentativas)

- **5 erros seguidos** de senha → conta bloqueada por **15 minutos**
- Após o período, tente novamente
- O contador zera ao logar com sucesso ou após reset de senha

---

## 2. 2FA (Verificação em dois fatores)

### O que é

Camada extra de segurança: além da senha, você precisa de um **código
de 6 dígitos** que muda a cada 30 segundos no seu celular.

### Ativar 2FA

1. Acesse `/admin/seguranca/2fa`
2. Clique em **Iniciar setup**
3. O sistema gera um **secret aleatório** e mostra um **QR Code**
4. Abra um dos apps suportados:
   - Google Authenticator
   - Authy
   - Microsoft Authenticator
   - 1Password
5. Escaneie o QR Code com o app
6. Digite o **código de 6 dígitos** que o app mostra
7. Clique em **Confirmar** → 2FA ativado

### Desativar 2FA

1. Acesse `/admin/seguranca/2fa`
2. Clique em **Desativar**
3. Digite o **código atual** do app
4. Clique em **Confirmar** → 2FA desativado

### Trocar dispositivo / reconfigurar

Não é possível "trocar de QR Code" sem antes desativar. Para mudar de
celular:

1. **Desative** 2FA com o código atual no celular antigo
2. **Reative** 2FA → escaneie o novo QR Code no celular novo

### Perdeu o celular

Sem acesso ao app autenticador, você **não consegue** desativar 2FA
sozinho. Procedimento de emergência:

1. Contate o administrador do sistema (SuperAdmin)
2. SuperAdmin acessa o banco e desativa o 2FA manualmente
3. Você pode logar e reconfigurar

> ⚠️ Por segurança, **não há link "perdi acesso ao 2FA"**. Isso
> impede que um atacante use esse caminho.

### Replay de código

O sistema **rejeita o mesmo código sendo usado 2 vezes**, mesmo dentro
da janela de 30s. Se você tentou logar mas o código não passou, espere
o app gerar o **próximo** código (15-30s) antes de tentar de novo.

---

## 3. Dashboard

URL: `/admin/dashboard`

### O que aparece

```
┌──────────────────────────────────────────────────────────┐
│  📊 Receita do mês       R$ 12.500    ▲ +18% vs mês ant. │
│  📅 Agendamentos do mês  142          ▲ +5%              │
│  ❌ Taxa cancelamento    4%           ▼ -2pp             │
│  👻 Taxa no-show         3%           ▼ -1pp             │
│  🎟️ Ticket médio         R$ 88        ▲ +7%              │
│                                                          │
│  📈 [Gráfico de receita por dia — últimos 30 dias]       │
│                                                          │
│  ⏰ Agenda de hoje (12 atendimentos)                      │
│  ├ 09:00 - João - Lavagem Completa                       │
│  ├ 10:30 - Maria - Polimento                             │
│  └ ...                                                   │
└──────────────────────────────────────────────────────────┘
```

### Atualização

Os dados são calculados em tempo real toda vez que você abre a tela.
Não há cache.

### Filtro de mês

Por padrão, mostra o **mês corrente**. Você pode mudar o seletor para
ver meses anteriores.

---

## 4. Agenda

URL: `/admin/agenda`

### Visualizações

- **Dia** (padrão) — calendário do dia atual com slots
- **Semana** — visão semanal compacta
- **Lista** — tabela ordenada por horário

### Filtros

- **Data:** seletor de calendário
- **Recurso:** mostra só atendimentos do recurso X (deixe vazio para todos)
- **Status:** filtre por Confirmado, Em andamento, Concluído, Cancelado

### Cores dos slots

| Cor       | Status                                            |
| --------- | ------------------------------------------------- |
| 🔵 Azul    | Confirmado (pago e agendado)                      |
| 🟡 Amarelo | Pendente de pagamento                             |
| 🟢 Verde   | Em andamento (cliente está sendo atendido)        |
| ⚪ Cinza   | Concluído                                         |
| 🔴 Vermelho| Cancelado                                         |
| 🟠 Laranja | No-show (cliente não apareceu)                    |

### Ações em cada agendamento

Clique em um slot para abrir o menu:

- **Ver detalhes** — abre painel lateral com tudo
- **Iniciar atendimento** — muda status para "Em andamento"
- **Concluir** — fecha o atendimento, credita pontos, dispara avaliação
- **Marcar no-show** — cliente faltou
- **Reagendar** — abre seletor de nova data/hora
- **Cancelar** — informe o motivo, sistema notifica fila de espera

### Criar agendamento manual

Clique em um slot vazio ou no botão **+ Novo agendamento**:

```
Cliente:      [buscar ou cadastrar novo]
Serviço:      [selecionar do catálogo]
Recurso:      [auto / escolher manualmente]
Data:         [seletor]
Hora:         [seletor — só horários livres]
Forma:        Dinheiro / PIX / Cartão
Observação:   [livre]
```

Se forma = Dinheiro, o agendamento já fica **Confirmado**. Se PIX ou
cartão, gera cobrança e aguarda webhook.

---

## 5. Agendamentos

### Listagem com filtros

URL: `/admin/agendamentos`

Tabela paginada com filtros:

- Página / tamanho
- Data exata
- Status (Pendente, Confirmado, Em andamento, Concluído, Cancelado, NoShow)

### Estados (ciclo de vida)

```
PendentePagamento ─────► Confirmado ─────► EmAndamento ─────► Concluido
        │                     │                  │
        └─► Expirado          ├─► Cancelado      └─► NoShow
                              │
                              └─► (admin pode mover)
```

### Detalhe de um agendamento

Abrindo um agendamento, você vê:

- Cliente (nome, telefone, e-mail)
- Serviço (nome, preço, duração)
- Recurso (qual box/sala/profissional)
- Data e hora
- Status atual + status do pagamento
- Valor total + valor de entrada
- Cupom aplicado (se houver)
- Forma de pagamento
- QR Code PIX / link de pagamento (se aplicável)
- Token público (link "meu agendamento" do cliente)
- Histórico de mudanças de status
- Fotos antes/depois (se houver)
- Botões de ação

### Reagendar

1. Clique em **Reagendar**
2. Escolha nova data e hora (sistema mostra só horários livres do
   mesmo recurso)
3. Confirma
4. Sistema verifica:
   - Antecedência mínima
   - Limite de cancelamento (não permite reagendar muito em cima)
   - Conflito com outros agendamentos
5. Salva — cliente recebe WhatsApp avisando

### Cancelar

1. Clique em **Cancelar**
2. Preencha **motivo** (obrigatório)
3. Confirma
4. Se for parte de um **combo**, **todos os agendamentos do combo são
   cancelados** automaticamente
5. Sistema notifica próximo da lista de espera
6. Admins do tenant recebem push em tempo real

### Confirmar pagamento manualmente

Só pra admin (policy AdminTenant). Útil quando:

- Cliente pagou em espécie no balcão
- Webhook do gateway não chegou

Abra o agendamento → **Confirmar pagamento**. Status do pagamento vira
**Aprovado** e o agendamento vira **Confirmado**.

---

## 6. Serviços

URL: `/admin/servicos`

### Listar

- **Somente ativos** (padrão): mostra `SerAtivo=true`
- **Todos**: inclui inativos (não exibidos no catálogo público)

### Cadastrar / Editar

Campos:

| Campo            | Tipo    | Obrigatório | Descrição                                |
| ---------------- | ------- | ----------- | ---------------------------------------- |
| Nome             | texto   | Sim         | Aparece na vitrine                       |
| Descrição        | texto   | Não         | Detalhes (até 1000 caracteres)           |
| Preço            | decimal | Sim         | R$ — pode ter centavos                   |
| Duração          | inteiro | Sim         | Em minutos                               |
| Categoria        | texto   | Não         | Agrupa na vitrine ("Lavagem", "Estética")|
| Foto             | upload  | Não         | Aparece na vitrine                       |
| Ordem            | inteiro | Não         | Menor = aparece primeiro                 |
| Ativo            | bool    | Sim         | Inativo esconde do público mas mantém histórico |

### Desativar (Inativar)

Botão **Desativar** marca `SerAtivo=false`. O serviço **desaparece da
vitrine pública** mas:

- ✅ Continua nos relatórios históricos
- ✅ Agendamentos passados/futuros não são afetados
- ❌ Cliente não consegue agendar novo

### Excluir

Botão **Excluir** marca soft-delete (`Excluido=true`). O serviço some
de **todas** as listas, inclusive backoffice. Recuperação só via banco.

---

## 7. Recursos

URL: `/admin/recursos`

### O que é um recurso

A "unidade que atende". Exemplos:

- Lava-rápido → cada **box** é um recurso
- Barbearia → cada **cadeira** (com nome do barbeiro)
- Clínica → cada **sala** ou **profissional**
- Pet shop → cada **box de banho** + cada **mesa de tosa**

**Regra:** 1 recurso atende 1 cliente por vez. Se você tem 3 boxes, 3
clientes podem ser atendidos simultaneamente.

### Cadastrar

| Campo       | Exemplo                                   |
| ----------- | ----------------------------------------- |
| Nome        | Box 1 (ou "João - Barbeiro")              |
| Descrição   | Box coberto, externo                      |
| Tipo        | Box (livre — qualquer string)             |
| Ordem       | 0, 1, 2... (define ordem na agenda)       |
| Ativo       | ✅                                         |

### Inativar

`RecAtivo=false`. Recurso some da agenda pública mas mantém
agendamentos históricos.

---

## 8. Combos

URL: `/admin/combos`

### O que é um combo

**N serviços com preço promocional** menor que a soma individual.
O cliente paga uma vez, faz os N serviços em sequência no mesmo
recurso.

Exemplo:

```
Combo "Detalhamento Premium"
  ├ Lavagem completa      (60 min)
  ├ Polimento espelhado   (120 min)
  └ Vitrificação          (240 min)

Soma individual: R$ 580
Preço promocional: R$ 450
```

### Cadastrar

```
Nome:                Detalhamento Premium
Descrição:           Lavagem + polimento + vitrificação
Imagem:              [upload]
Preço promocional:   450,00
Serviços:            [Lavagem Completa, Polimento, Vitrificação]
Ordem:               1
Ativo:               ✅
```

### Como o cliente agenda

1. Cliente vê o combo no `/t/{slug}/combos`
2. Escolhe data e horário do **início** do combo
3. Sistema reserva **N agendamentos contíguos** no mesmo recurso
4. Cobrança agregada única
5. Confirmação do pagamento confirma **todos os N**
6. Cancelamento de **um** = cancelamento de **todos** (já que cliente
   pagou 1x pelo conjunto)

### Restrições

- **Não dá pra reagendar individualmente** um item do combo. Cliente
  deve cancelar todo o combo e criar novo.
- O combo inteiro precisa caber dentro do **mesmo dia** (mesmo
  horário de funcionamento, sem atravessar pausa de almoço).
- Preço promocional deve ser **maior que 0**.

---

## 9. Pacotes pré-pagos

URL: `/admin/pacotes`

### O que é um pacote

Cliente **paga antecipado por N atendimentos do mesmo serviço**,
geralmente com desconto. Usa quando quiser, dentro da validade.

Exemplo:

```
Pacote "5 Lavagens Completas"
  ├ Serviço base:  Lavagem Completa (avulso: R$ 80)
  ├ Quantidade:    5
  ├ Preço:         R$ 350     (R$ 70 cada — economia de R$ 50)
  └ Validade:      90 dias após a compra
```

### Cadastrar

```
Nome:           5 Lavagens Completas
Serviço base:   Lavagem Completa
Quantidade:     5
Preço:          350,00
Validade:       90       (dias após a compra)
Ativo:          ✅
```

### Como o cliente compra

1. Cliente vê os pacotes em `/t/{slug}/pacotes`
2. Escolhe um pacote → preenche dados pessoais
3. Sistema cria **SaldoPacote** no status **Pendente**
4. Gera cobrança PIX
5. Cliente paga → webhook MP confirma → SaldoPacote vira **Ativo**

### Como o cliente usa

Em cada novo agendamento desse mesmo serviço:

1. Sistema verifica se o cliente tem **SaldoPacote ativo** com
   quantidade > 0 e não expirado
2. Se SIM: **debita 1** do saldo, confirma o agendamento **sem
   cobrar nada**
3. Se NÃO: cobrança normal (PIX/cartão/dinheiro)

> 💡 Pacote tem preferência sobre cupom. Se cliente tem saldo, o
> cupom (se informado) **não é "queimado"** — fica preservado pra
> próximo agendamento sem saldo.

### Cliente vê saldo

Na "Minha Conta" do cliente:

```
📦 Meus pacotes
  ├ 5 Lavagens — Lavagem Completa
  │   Saldo: 3 de 5
  │   Expira em: 21/08/2026
  └ ...
```

---

## 10. Cupons

URL: `/admin/cupons`

### Tipos

- **Percentual** — aplica % de desconto sobre o valor total
- **Valor fixo** — abate R$ X do valor total

### Cadastrar

```
Código:           BEMVINDO10
Tipo:             Percentual
Valor:            10
Válido de:        21/05/2026
Válido até:       30/06/2026
Usos máximos:     100
Ativo:            ✅
```

### Regras

- **Código é único por tenant** (não pode ter 2 cupons com mesmo código)
- **Códigos são case-insensitive** ao usar (`bemvindo10` = `BEMVINDO10`)
- **Sistema converte para uppercase** ao salvar
- **Desconto não pode passar do valor total** (cupom de R$ 100 em
  serviço de R$ 80 → valor final R$ 0)

### Validar / Aplicar

- Cliente digita código no checkout do agendamento
- Sistema verifica:
  - Cupom existe e pertence ao tenant
  - Está ativo
  - Dentro da janela de validade
  - Tem usos disponíveis
- Aplica o desconto antes de calcular a entrada
- Registra 1 uso

### Ativar / Desativar

Botão liga/desliga sem deletar. Útil pra "pausar" uma campanha.

### Auditoria

Você não vê o histórico de uso direto na tela de cupons. Para ver
quem usou, vá em **Auditoria** (`/admin/auditoria`) e filtre por
tabela = "Cupom".

---

## 11. Fidelidade

URL: `/admin/fidelidade`

### Como funciona

- **Cada agendamento concluído credita 10 pontos** no cliente
- 100 pontos = 1 cupom de R$ 10 (valor fixo, uso único, validade 60 dias)
- Cliente vê o saldo na **Minha Conta**

### Consultar saldo de um cliente

1. Acesse a tela de Fidelidade
2. Digite o ID ou nome do cliente
3. Sistema mostra:
   ```
   Cliente: Maria Silva
   Saldo: 80 pontos
   ```

### Trocar pontos por cupom (em nome do cliente)

Use quando o cliente pedir "quero trocar meus pontos":

1. Encontre o cliente
2. Digite quantos pontos trocar (múltiplos de 10)
3. Clique em **Trocar**
4. Sistema:
   - Debita os pontos
   - Cria um cupom novo: código `FID-{clienteId}-{aleatório}`,
     tipo Valor Fixo, valor = pontos / 10, validade 60 dias, uso único
5. Mostra o código gerado — você passa pro cliente

### Onde os pontos vêm

Automático: cada vez que você marca um agendamento como **Concluído**,
10 pontos são creditados ao cliente daquele agendamento. Não há ação
manual.

### Não há débito de pontos por "uso direto"

Os pontos **só viram cupom**. Eles não dão desconto sozinhos. Você
sempre passa pela troca → cupom → aplicação no checkout.

---

## 12. Recorrências

URL: `/admin/recorrencias`

### O que é uma recorrência

Uma **série de agendamentos repetidos** criados de uma vez. Útil pra:

- Cliente fixo que vem toda semana
- Aulas regulares
- Atendimentos terapêuticos com frequência definida

### Criar uma série

```
Cliente:         [buscar]
Serviço:         [selecionar]
Recurso:         [selecionar]
Dia da semana:   Quarta
Hora início:     14:00
Frequência:      Semanal / Quinzenal / Mensal
Quantidade:      8       (de 1 a 52)
Data início:     [seletor]
```

Clique em **Criar série**.

### O que acontece

Sistema:

1. Calcula as N datas (ajusta pra próximo dia-da-semana certo)
2. Para cada data, verifica conflitos no recurso
3. **Datas livres:** cria agendamento Confirmado (admin-criado já confirma)
4. **Datas com conflito:** ignora e reporta no resultado

Resposta:

```json
{
  "recorrenciaId": 42,
  "criados": 6,
  "ids": [101, 102, 103, 104, 106, 108],
  "erros": [
    "03/06: horário indisponível (conflito com outro agendamento).",
    "17/06: horário indisponível (conflito com outro agendamento)."
  ]
}
```

### Cancelar série inteira

> ⚠️ Atualmente **não há endpoint pra cancelar todos da série em
> bloco**. Você precisa cancelar agendamento por agendamento. Feature
> planejada para próxima versão.

### Listar séries

A tela mostra todas as séries ativas do tenant.

---

## 13. Clientes

URL: `/admin/clientes`

### Listar com busca

Tabela paginada (20 por página). Campo de busca filtra por nome,
e-mail ou telefone.

### Cadastrar manualmente

Clique em **+ Novo cliente**:

| Campo      | Obrigatório | Exemplo                  |
| ---------- | ----------- | ------------------------ |
| Nome       | Sim         | Maria Silva              |
| Telefone   | Sim*        | (11) 99999-9999          |
| E-mail     | Sim*        | maria@email.com          |
| WhatsApp   | Não         | (11) 99999-9999          |
| CPF        | Não         | 123.456.789-00           |
| Observação | Não         | "Alergia a sabão neutro" |

*Pelo menos um dos dois (telefone ou e-mail) é obrigatório.

### Atualizar

Mesma tela. Edite e salve.

### Cliente criado automaticamente

Quando o cliente final agenda pelo site e preenche dados:

- Sistema **procura cliente existente** pelo telefone e depois e-mail
- Se encontra: reusa
- Se não encontra: **cria automaticamente**

Por isso, sua base de clientes cresce sozinha quando o site é usado.

### Anonimizar (LGPD)

Botão **Anonimizar**:

1. Limpa nome, e-mail, telefone, WhatsApp, CPF, observação
2. Nome vira `"Cliente removido #{id}"`
3. **Remove TODAS as fotos** dos agendamentos do cliente (do disco e
   do banco)
4. Mantém o registro do cliente e histórico de agendamentos (com nome
   anonimizado)
5. Loga a operação no audit

Cliente não consegue mais fazer login OTP (telefone foi apagado).

> ⚠️ **Operação irreversível.** Use só quando o cliente solicitar
> formalmente sob LGPD.

### Exportar dados (LGPD)

Botão **Exportar JSON**:

1. Gera arquivo JSON com:
   - Dados do cliente (nome, contatos, CPF)
   - Todos os agendamentos (data, serviço, valor, status)
   - Todas as fotos (URLs)
2. Faz download como `cliente-{id}-dados.json`
3. Útil pra responder pedido de portabilidade LGPD

---

## 14. Importação CSV de clientes

URL: `/admin/clientes/importar` (ou via menu Clientes → Importar)

### Formato do CSV

Cabeçalho obrigatório:

```csv
nome,telefone,email,cpf
Maria Silva,(11) 99999-9999,maria@email.com,
João Souza,(21) 88888-8888,joao@email.com,12345678900
```

### Regras

- **Limite:** 2 MB por arquivo (~ 20-30 mil linhas)
- **Encoding:** UTF-8 (com ou sem BOM)
- **Delimitador:** vírgula
- **Aspas:** opcional, sistema lida com escapes

### Deduplicação automática

Sistema **ignora linhas duplicadas** comparando:

- **Telefone** (normalizado pra só dígitos)
- **E-mail** (lowercase, trim)

Se o telefone já existe na base do tenant OU em uma linha anterior do
mesmo CSV, a linha é pulada e contada como **duplicada**.

### Resultado

Após o upload:

```json
{
  "inseridos": 487,
  "ignorados": 3,
  "duplicados": 15,
  "erros": [
    "Linha 42: Nome obrigatório.",
    "Linha 89: ..."
  ]
}
```

### Dicas

- Faça **um teste com 5 linhas** primeiro pra confirmar o formato
- Se vier de outro sistema, exporte como CSV e abra no Excel/Sheets
  pra ajustar cabeçalhos antes
- Erros não interrompem a importação — sistema processa tudo e
  reporta no final

---

## 15. Bloqueios de agenda

URL: `/admin/bloqueios`

### O que é

Marcação de **intervalos de tempo onde a agenda não aceita reservas**.
Usado pra:

- Feriados
- Manutenção de equipamento
- Recesso
- Compromisso pessoal

### Cadastrar

```
Recurso:         [vazio = bloqueia todos]   OU   [Box 1]
Data início:     25/12/2026 00:00
Data fim:        25/12/2026 23:59
Motivo:          Natal — fechado
```

Clique em **Salvar**.

### Como funciona

- **Sem recurso definido:** bloqueia **toda a agenda** do tenant
  naquele intervalo
- **Com recurso definido:** bloqueia **só aquele recurso**

Slots dentro do bloqueio:

- ❌ Não aparecem na agenda pública (cliente não consegue agendar)
- ⚠️ Agendamentos **já existentes** não são afetados (você precisa
  cancelar manualmente se quiser)

### Listar

Tabela mostra bloqueios futuros (próximos 12 meses por padrão) com
data, recurso, motivo.

### Remover

> ⚠️ Atualmente **não há endpoint DELETE** na UI. Pra remover um
> bloqueio criado por engano, é necessário ação direta no banco ou
> aguardar a feature ser entregue. Workaround: criar bloqueio com
> data muito antiga não afeta nada.

---

## 16. Lista de espera

URL: `/admin/lista-espera`

### Como o cliente entra na fila

Quando cliente não acha horário disponível para uma data específica,
ele pode acessar `/t/{slug}/lista-espera-publica`:

```
Serviço desejado:    [seletor]
Data desejada:       [calendário]
Seu nome:            [campo]
Seu telefone:        [campo]
Seu e-mail:          [opcional]
Observação:          [opcional]
```

### Como você vê

```
Posição  Cliente         Telefone           Serviço         Data        Notificado
1        Ana Costa       (11) 99999-1111    Lavagem Compl.  03/06/2026  ❌
2        Pedro Lima      (11) 99999-2222    Lavagem Compl.  03/06/2026  ❌
3        Mariana Reis    (11) 99999-3333    Polimento       05/06/2026  ✅
```

### Notificação automática

Quando você (ou cliente) **cancela** um agendamento, o sistema:

1. Procura o **primeiro não-notificado** da fila pra mesma data/serviço
2. Envia WhatsApp:
   > "Olá Ana! Vagou um horário. Agende em [link]"
3. Marca como **Notificado** automaticamente

### Notificação manual

Se quiser, você pode notificar manualmente:

1. Clique em **Notificar** ao lado do cliente
2. Sistema abre o WhatsApp Web com mensagem pré-preenchida
3. Você clica em enviar
4. Clique em **Marcar como notificado** no painel

### Dedup automático

Cliente não consegue entrar na fila **2 vezes** pro mesmo
serviço+data (deduplicação por telefone).

---

## 17. Avaliações

URL: `/admin/avaliacoes`

### Como funcionam

- Cliente avalia 1-5 estrelas + comentário (opcional)
- Acesso via link público enviado por WhatsApp/email após o
  atendimento ser concluído
- Sem login necessário (token único)
- Cada avaliação só pode ser respondida **uma vez**

### Como você vê

Lista paginada (20 por página) com:

- Nome do cliente (mascarado se anônimo)
- Nota (estrelas)
- Comentário
- Agendamento relacionado
- Data
- Status (Pendente / Respondida)
- Status público (Visível / Oculta)

### Filtros

- **Somente respondidas:** esconde tokens ainda não preenchidos

### Ocultar uma avaliação

Comentário maldoso ou injustificado? Clique em **Ocultar**:

- Avaliação some da home pública do tenant
- Mas continua no relatório interno
- Você pode reverter (Mostrar)

> 💡 **Ética:** evite ocultar avaliações negativas legítimas. Responda
> à reclamação no privado (WhatsApp) e mostre profissionalismo.

### Média pública

Sua home pública mostra:

- Média ponderada das avaliações **públicas** (Visível)
- Total de avaliações
- 5 últimas com nome do cliente, nota, comentário

---

## 18. Fotos antes/depois

URL: `/admin/agendamentos/{id}/fotos`

### Para que servem

- Lava-rápido: antes (sujo) / depois (limpo) — mostra resultado
- Estética: registro do procedimento
- Pet shop: tutor vê o resultado da tosa

### Upload

1. Abra o agendamento (vá pela Agenda → clique → "Ver fotos")
2. Clique em **+ Adicionar foto**
3. Selecione o tipo:
   - **Antes**
   - **Depois**
   - **Geral**
4. Selecione o arquivo (até 10 MB, formatos: jpg, png, webp, gif)
5. Upload

### O que acontece nos bastidores

- Arquivo salvo no storage configurado (Local ou S3)
- Job em segundo plano **redimensiona** imagens > 1920×1920 (mantendo
  proporção)
- Job atualiza o tamanho real do arquivo no banco

### Visualização do cliente

O cliente vê fotos do próprio agendamento na **Minha Conta** → aba
"Histórico" → "Ver fotos".

### Remover foto

Botão **Remover** ao lado da foto:

- Apaga do storage (local ou S3)
- Apaga do banco

---

## 19. Caixa do dia

URL: `/admin/caixa`

### Quando usar

No fim do expediente. Mostra resumo do dia para fechamento.

### Filtros

- **Data:** hoje (padrão) ou qualquer outra data

### Relatório

```
Data: 21/05/2026
─────────────────────────────────────
Total de agendamentos:      12
  Concluídos:                9
  Cancelados:                2
  No-show:                   1
  Pendentes:                 0

Receita prevista:      R$    980,00
Receita concluída:     R$    720,00
Receita recebida:      R$    720,00
  (PIX + cartão + dinheiro confirmados)
```

### Exportar

> 💡 Atualmente não há botão de exportar PDF/Excel direto na tela.
> Você pode imprimir a página via `Ctrl+P` do navegador.

---

## 20. KPIs

URL: `/admin/kpis`

### O que aparece

```
Período: Mês atual vs. mês anterior
─────────────────────────────────────────

                       Atual   Anterior   Var %
Agendamentos          142      135        +5%
Concluídos             96       89        +8%
Cancelados              6        9       -33%
No-show                 4        7       -43%
Taxa cancelamento    4,2%      6,7%      -2,5pp
Taxa no-show         2,8%      5,2%      -2,4pp
Receita        R$ 8.450    R$ 7.120     +18%
Ticket médio   R$ 88       R$ 80         +10%
```

### Filtro de mês

Seletor de mês de referência (compara sempre com o anterior).

### Como usar

- Veja **tendência positiva** (cancelamento e no-show caindo)
- Detecte **regressões** (ticket médio caiu = preços muito agressivos
  ou perda de mix premium)
- Use pra decidir **promoções** (mês com receita baixa = lançar cupom)

---

## 21. Relatórios

URL: `/admin/relatorios`

### 8 relatórios disponíveis

1. **Receita por dia** (período customizado) — soma receita
   concluída por dia
2. **Top serviços mais vendidos** (período) — ranking por quantidade
   e receita
3. **Taxa de ocupação** (período) — slots ocupados vs. disponíveis
   por recurso
4. **Cancelamentos** (período) — agrupados por dia, motivo mais
   comum
5. **LTV por cliente — Top 20** — clientes que mais gastam (nome,
   telefone, qtde, receita total, ticket médio, primeiro/último
   agendamento)
6. **No-show por dia da semana** — taxa em cada dia (descobre se
   "toda terça tem 30% de furo")
7. **No-show por hora do dia** — taxa em cada horário
8. **Sazonalidade mensal** (últimos 12 meses) — receita e quantidade
   por mês (quando contratar reforço)

### Filtros

- **Início / Fim:** período customizável (padrão: mês corrente)
- **Top N** (apenas LTV): default 20, customizável

### Visualização

Cada relatório tem barra horizontal pra comparação visual (no-show,
sazonalidade) ou tabela ordenada (top serviços, LTV).

### Exportar

> 💡 Atualmente exportação direta de relatório (CSV/PDF) não está
> disponível na UI. Você pode imprimir via navegador (`Ctrl+P`) ou
> usar a API diretamente.

---

## 22. Auditoria

URL: `/admin/auditoria`

### O que registra

**Tudo que muda no banco**:

- Quem fez (usuário + email)
- Quando (UTC)
- IP de origem
- Correlation ID (rastreia a operação inteira)
- Tabela alterada
- Chave (ID do registro)
- Ação (Insert / Update / Delete)
- JSON do estado **antes** e **depois**

### Filtros

- **Tabela:** ex. "Cliente", "Agendamento", "Cupom"
- **Ação:** Insert / Update / Delete
- **Período:** de / até
- **Usuário:** filtra por email

### Drill-down

Clique em um registro pra ver:

```
ID:             54321
Quando:         2026-05-21 14:33:12 UTC
Usuário:        maria@lava-rapido-acme.com.br
IP:             200.123.45.67
Tabela:         Cliente
Chave:          1042
Ação:           Update

Antes:
{
  "CliId": 1042,
  "CliNome": "Maria Silva",
  "CliEmail": "maria@email.com",
  "CliTelefone": "11999998888",
  "CliCpf": "***"
}

Depois:
{
  "CliId": 1042,
  "CliNome": "Maria Santos",
  ...
}
```

> 🔒 Senhas, tokens, secrets e CPF são **mascarados como `***`**
> (LGPD).

### Retenção

Logs com mais de **12 meses** são **deletados automaticamente** (job
diário às 04:00 UTC). Antes disso, ficam consultáveis.

### Uso típico

- "Quem cancelou o agendamento da Maria?"
- "Por que esse cliente sumiu da base?" (foi anonimizado, por quem?)
- "Quem mudou o preço da Lavagem Completa?"

---

## 23. Configurações

URL: `/admin/configuracoes`

4 abas: Empresa, Personalização, Regras de negócio, Notificações.

### 23.1 Empresa

Dados cadastrais (Nome, CNPJ, e-mail, telefone, endereço, etc.).

### 23.2 Personalização

Visual (logo, banner, favicon, cores, fonte).

> ⚠️ URLs aceitam **só HTTP/HTTPS** ou caminhos relativos (`/uploads/...`).
> `javascript:`, `data:`, `file:` são rejeitados (proteção XSS).

> ⚠️ Cores **só nos formatos `#FFF`, `#FFFA`, `#FFFFFF`, `#FFFFFFFF`**
> (3, 4, 6 ou 8 caracteres hex).

### 23.3 Regras de negócio

```
Percentual de entrada:       0-100 %
Buffer entre atendimentos:   0-240 min
Antecedência mínima:         0-720 h (até 30 dias)
Antecedência máxima:         1-365 dias
Limite de cancelamento:      0-720 h
```

### 23.4 Notificações (Web Push)

Toggle para ativar/desativar notificações push no dispositivo (mesmo
com app fechado). Ver seção [Notificações Web Push](#24-notificações-web-push).

---

## 24. Notificações Web Push

### O que é

Notificações **no dispositivo do administrador** mesmo com o app/aba
fechado(a). Compatível com Chrome, Edge, Firefox e Safari (16+).

### Pré-requisito do servidor

O administrador do sistema (devops) precisa configurar **chaves VAPID**
no `.env`:

```
VAPID_PUBLIC_KEY=...
VAPID_PRIVATE_KEY=...
VAPID_SUBJECT=mailto:admin@seudominio.com.br
```

Sem isso, o toggle aparece com aviso "Servidor sem VAPID configurado".

### Como ativar (admin do tenant)

1. Vá em **Configurações → Notificações**
2. Ative o toggle **"Receber notificações"**
3. Browser pede permissão → clique em **Permitir**
4. Pronto

### O que dispara push

- 🆕 Novo agendamento criado
- ✅ Pagamento aprovado
- ❌ Agendamento cancelado

Em cada um, o título aparece no celular/desktop, mesmo com o navegador
fechado.

### Desativar

Mesmo toggle → desligar.

---

## 25. LGPD

### Direitos do cliente

Como administrador, você pode atender 3 direitos LGPD via interface:

#### 25.1 Direito de portabilidade (Art. 18, V)

- Clientes → encontre o cliente → **Exportar dados**
- Sistema gera JSON com:
  - Dados cadastrais
  - Histórico de agendamentos
  - Fotos vinculadas
- Entregue ao cliente em qualquer formato que ele pedir

#### 25.2 Direito de retificação (Art. 18, III)

- Clientes → encontre → **Editar** → corrija
- Ou: cliente edita sozinho na "Minha Conta"

#### 25.3 Direito ao esquecimento (Art. 18, VI)

- Clientes → encontre → **Anonimizar**
- Sistema remove:
  - Nome (vira "Cliente removido #ID")
  - E-mail, telefone, WhatsApp, CPF
  - **TODAS as fotos** dos agendamentos
- Mantém:
  - Histórico de agendamentos (necessário pra integridade contábil)
  - Avaliações públicas (com nome anonimizado)
- Loga a operação no audit (com IP, hora, quem fez)

### Anonimização em massa

Endpoint opcional pra rodar mensalmente:

- Clientes inativos há > 24 meses são anonimizados automaticamente
- Configurável (parâmetro `inativoHaMeses`)
- Não disponível na UI por padrão — chamar via API

### Audit log dos dados

Já registrado automaticamente. Senhas/tokens/CPF **mascarados** no log.

### Retenção de logs

Audit log purga > 12 meses (job diário). Cumpre princípio de
minimização.

---

## 26. Perfis e permissões

O sistema tem 3 perfis (policies):

| Perfil       | Pode fazer                                                   |
| ------------ | ------------------------------------------------------------ |
| **SuperAdmin** | Tudo. Cria/edita tenants. Inicializa DB em modo PerTenant.  |
| **AdminTenant**| Tudo no próprio tenant: configurações, cupons, recursos, serviços, anonimizar/exportar LGPD, alterar visibilidade de avaliação, confirmar pagamento manual. |
| **Atendente**  | Operação do dia-a-dia: ver agenda, criar/cancelar/reagendar agendamento, marcar concluído/no-show, trocar pontos por cupom, marcar lista de espera como notificada. NÃO pode mudar configurações, criar cupons, anonimizar clientes. |

### Cliente final (B2C)

Tem JWT separado com role `Cliente`. Acessa só:

- `/t/{slug}/minha-conta` e sub-rotas
- Não acessa nada do `/admin/*`

### 2FA

Aplica-se a SuperAdmin, AdminTenant e Atendente — qualquer usuário do
painel administrativo.

---

## 27. Glossário

| Termo                       | Significa                                              |
| --------------------------- | ------------------------------------------------------ |
| **Tenant**                  | Estabelecimento (negócio) que usa o sistema           |
| **Slug**                    | Identificador URL-friendly do tenant (ex. `acme-lava`) |
| **Recurso**                 | Box/sala/cadeira/profissional — unidade que atende    |
| **Serviço**                 | Atendimento individual (ex. "Lavagem Completa")       |
| **Combo**                   | Conjunto de N serviços com preço promocional          |
| **Pacote**                  | N atendimentos do mesmo serviço pré-pagos             |
| **Cupom**                   | Código de desconto (% ou R$ fixo)                     |
| **Saldo (de pacote)**       | Quantidade restante de atendimentos do cliente        |
| **Recorrência**             | Série de agendamentos repetidos                       |
| **Slot**                    | Janela de tempo disponível para agendamento           |
| **Buffer**                  | Intervalo entre 2 atendimentos no mesmo recurso       |
| **Antecedência mínima**     | Tempo mínimo entre "agora" e o agendamento            |
| **Antecedência máxima**     | Quanto à frente cliente pode agendar                  |
| **Limite de cancelamento**  | Tempo mínimo pra cancelar sem contatar o estabelecimento |
| **No-show**                 | Cliente que não compareceu sem cancelar               |
| **Webhook**                 | Notificação automática do gateway de pagamento        |
| **OTP**                     | One-Time Password (código de 6 dígitos enviado por WhatsApp) |
| **2FA**                     | Two-Factor Authentication (login com senha + código)  |
| **JWT**                     | Token de autenticação                                 |
| **LGPD**                    | Lei Geral de Proteção de Dados (Brasil)               |
| **HMAC**                    | Mecanismo de assinatura criptográfica usado em webhooks |
| **PWA**                     | Progressive Web App (site instalável como app)        |
| **PerTenant DB**            | Modo onde cada tenant tem seu próprio banco SQLite    |

---

## Suporte e referências

- Tutorial inicial: [tutorial-primeiros-passos.md](tutorial-primeiros-passos.md)
- Configurar Mercado Pago: [setup-mercado-pago.md](setup-mercado-pago.md)
- Configurar WhatsApp Business: [setup-whatsapp-business.md](setup-whatsapp-business.md)
- Backlog do produto: [../BACKLOG.md](../BACKLOG.md)
- Documentação técnica: [../README.md](../README.md)

---

*Manual atualizado em maio de 2026. Versão do sistema: 238 testes
verdes, 39 auditorias de segurança aplicadas.*
