# Tutorial: Primeiros Passos

> Guia para configurar o seu estabelecimento no AgendamentoPro **do
> zero até o primeiro agendamento real**. Leitura linear, da primeira
> à última seção. Tempo estimado: **40-60 minutos**.

---

## Índice

1. [Antes de começar](#1-antes-de-começar)
2. [Primeiro acesso](#2-primeiro-acesso)
3. [Configurar o seu negócio](#3-configurar-o-seu-negócio)
4. [Cadastrar recursos (boxes/salas/profissionais)](#4-cadastrar-recursos-boxessalasprofissionais)
5. [Cadastrar serviços](#5-cadastrar-serviços)
6. [Configurar horário de funcionamento](#6-configurar-horário-de-funcionamento)
7. [Definir regras de negócio](#7-definir-regras-de-negócio)
8. [Personalizar a aparência](#8-personalizar-a-aparência)
9. [Testar como cliente](#9-testar-como-cliente)
10. [Receber o primeiro agendamento real](#10-receber-o-primeiro-agendamento-real)
11. [O que fazer no dia-a-dia](#11-o-que-fazer-no-dia-a-dia)
12. [Próximos passos](#12-próximos-passos)

---

## 1. Antes de começar

### O que você precisa em mãos

- ✅ **Credenciais de acesso** ao painel administrativo (email e senha
  ou link de "primeiro acesso" enviado pelo administrador do sistema)
- ✅ **Logo do seu negócio** em PNG ou JPG (recomendado: 200×200px)
- ✅ **Banner** (opcional — recomendado: 1200×400px)
- ✅ **Lista dos serviços** que você oferece, com preço e duração
- ✅ **Quantidade de boxes / salas / profissionais** que atendem em paralelo
- ✅ **Horário de funcionamento** (incluindo pausa de almoço, se houver)
- ✅ **Conta no Mercado Pago** ativa (se quiser receber pagamento PIX/cartão)
- ✅ **Conta WhatsApp Business** aprovada (se quiser enviar lembretes automáticos)

> 💡 **Dica:** você pode começar SEM Mercado Pago e SEM WhatsApp. O
> sistema funciona com agendamento "dinheiro na hora" e sem envio
> automático — perfeito para validar o fluxo antes de ativar
> integrações.

---

## 2. Primeiro acesso

### 2.1 Abra a tela de login

Acesse: `seu-dominio.com.br/admin/login`

Você verá:

```
┌─────────────────────────────────┐
│         [Logo AgendamentoPro]   │
│                                 │
│   Entrar                        │
│                                 │
│   E-mail   [_______________]    │
│   Senha    [_______________]    │
│                                 │
│           [Esqueci a senha]     │
│                                 │
│   [        Entrar        ]      │
└─────────────────────────────────┘
```

### 2.2 Faça login

1. Digite o **e-mail** fornecido pelo administrador
2. Digite a **senha**
3. Clique em **Entrar**

> ⚠️ Se errar a senha **5 vezes seguidas**, a conta bloqueia por 15
> minutos. Aguarde ou use "Esqueci a senha".

### 2.3 (Recomendado) Ativar 2FA

A primeira coisa que você deve fazer **antes de qualquer outra
configuração** é proteger a sua conta com 2FA (verificação em dois
fatores).

1. Vá em **Configurações → Segurança → 2FA** (ou
   `/admin/seguranca/2fa`)
2. Clique em **Iniciar setup**
3. Abra no seu celular o app **Google Authenticator** (ou Authy, ou
   1Password)
4. Escaneie o **QR Code** que aparece na tela
5. Digite no campo o **código de 6 dígitos** que o app gerou
6. Clique em **Confirmar**

Pronto. Da próxima vez que fizer login, vai pedir o código do
authenticator.

> ⚠️ **Guarde o app autenticador.** Se você perder o celular sem ter
> backup, vai precisar contatar o administrador do sistema para resetar
> o 2FA.

---

## 3. Configurar o seu negócio

Vá em **Configurações** (`/admin/configuracoes`).

A tela tem 4 abas:

```
┌──────────────────────────────────────────────────┐
│  [Empresa] [Personalização] [Regras] [Notif.]    │
└──────────────────────────────────────────────────┘
```

### 3.1 Aba "Empresa"

Preencha:

| Campo       | Exemplo                                          |
| ----------- | ------------------------------------------------ |
| Nome        | Lava-Rápido Acme                                 |
| Segmento    | Lava-rápido                                      |
| CNPJ        | 12.345.678/0001-90 (opcional)                    |
| E-mail      | contato@lava-rapido-acme.com.br                  |
| Telefone    | (11) 3000-0000                                   |
| WhatsApp    | (11) 99999-9999                                  |
| Endereço    | Rua das Flores, 123 - Vila Madalena              |
| Cidade      | São Paulo                                        |
| Estado      | SP                                               |
| CEP         | 05432-000                                        |
| Descrição   | Lavagem completa de carros, motos e SUVs...      |

Clique em **Salvar**.

---

## 4. Cadastrar recursos (boxes/salas/profissionais)

**Recurso** = a "unidade que atende". Um box de lava-rápido, uma sala
de massagem, uma cadeira de barbearia, um profissional específico. Se
você tem 3 boxes, são 3 recursos — pode atender 3 clientes
simultaneamente.

### 4.1 Acesse a tela

Menu lateral → **Recursos** (`/admin/recursos`).

### 4.2 Cadastre o primeiro recurso

Clique em **+ Novo recurso**:

```
Nome:        Box 1
Descrição:   Box coberto, externo
Tipo:        Box        (livre — você define: Box / Sala / Cadeira / Profissional)
Ordem:       0
Ativo:       ✅
```

Clique em **Salvar**.

### 4.3 Cadastre os demais

Repita para cada box/sala/profissional. Recomendado: ordem crescente
(Box 1 = 0, Box 2 = 1, etc.) para a agenda mostrar na sequência certa.

> 💡 **Importante:** o sistema oferece o **primeiro recurso livre** ao
> cliente. Se você tem 3 boxes e 2 estão ocupados às 14h, o cliente
> consegue agendar 14h e cai automaticamente no box livre.

---

## 5. Cadastrar serviços

### 5.1 Acesse

Menu → **Serviços** (`/admin/servicos`).

### 5.2 Cadastre o primeiro serviço

Clique em **+ Novo serviço**:

```
Nome:        Lavagem Completa Externa + Interna
Descrição:   Inclui aspiração, vidros, dashboard e pneus brilhantes
Preço:       80,00
Duração:     60 minutos
Categoria:   Lavagem        (opcional, agrupa no catálogo)
Ordem:       1              (ordem na vitrine)
Foto:        [upload]       (opcional, recomendado)
Ativo:       ✅
```

Clique em **Salvar**.

### 5.3 Cadastre 3-5 serviços principais

Exemplos completos para um lava-rápido:

| Nome                  | Preço    | Duração | Categoria   |
| --------------------- | -------- | ------- | ----------- |
| Lavagem Simples       | R$ 30    | 30 min  | Lavagem     |
| Lavagem Completa      | R$ 80    | 60 min  | Lavagem     |
| Polimento Espelhado   | R$ 150   | 120 min | Detalhamento|
| Vitrificação          | R$ 350   | 240 min | Detalhamento|
| Higienização Interna  | R$ 120   | 90 min  | Detalhamento|

> 💡 **Dica:** comece com os serviços mais pedidos. Você pode adicionar
> mais depois sem migrar nada.

---

## 6. Configurar horário de funcionamento

> ⚠️ Esta configuração é **obrigatória**. Sem ela, a agenda pública
> não mostra nenhum horário livre.

### 6.1 Acesse

Menu → **Configurações → Regras de negócio** (ou módulo
"Horários", dependendo da versão).

### 6.2 Configure cada dia da semana

Para cada dia:

```
Segunda    [✅ Aberto]    Abertura: 08:00   Fechamento: 18:00
                          Pausa início: 12:00  Pausa fim: 13:30
```

Exemplo completo:

| Dia      | Status   | Abertura | Pausa       | Fechamento |
| -------- | -------- | -------- | ----------- | ---------- |
| Segunda  | Aberto   | 08:00    | 12:00-13:30 | 18:00      |
| Terça    | Aberto   | 08:00    | 12:00-13:30 | 18:00      |
| Quarta   | Aberto   | 08:00    | 12:00-13:30 | 18:00      |
| Quinta   | Aberto   | 08:00    | 12:00-13:30 | 18:00      |
| Sexta    | Aberto   | 08:00    | 12:00-13:30 | 18:00      |
| Sábado   | Aberto   | 08:00    | (sem pausa) | 14:00      |
| Domingo  | Fechado  | —        | —           | —          |

Clique em **Salvar**.

> 💡 **Slots durante a pausa não aparecem na agenda pública.** Você
> está protegido contra cliente tentar agendar 12:30.

---

## 7. Definir regras de negócio

Configurações → aba **Regras de negócio**:

```
Percentual de entrada:       20      (% do valor cobrado como sinal)
Buffer entre atendimentos:   0       (minutos entre 2 atendimentos no mesmo recurso)
Antecedência mínima:         2       (horas — cliente não agenda nas próximas 2h)
Antecedência máxima:         60      (dias — não agenda além de 60 dias)
Limite de cancelamento:      12      (horas — cliente só cancela com 12h+ de antecedência)
```

### O que cada um significa

- **Percentual de entrada:** cliente paga apenas % no PIX/cartão pra
  confirmar a reserva. O restante paga na hora. `100` = paga 100%
  antecipado. `0` = não cobra antecipadamente (cliente reserva sem
  pagar — mais risco de no-show).

- **Buffer:** intervalo automático entre atendimentos no mesmo
  recurso. Exemplo: lavagem dura 60min, buffer 15min → próximo cliente
  só consegue 75min depois do início do anterior. Útil para limpar
  cabine ou recurso entre clientes.

- **Antecedência mínima:** evita que cliente apareça 5min antes "ah,
  acabei de agendar". Recomendado: `2` horas.

- **Antecedência máxima:** evita reservas para "daqui 1 ano".
  Recomendado: `60` ou `90` dias.

- **Limite de cancelamento:** abaixo desse prazo, o cliente NÃO pode
  cancelar pelo app — precisa ligar pra você. Protege contra furo de
  última hora.

Clique em **Salvar**.

---

## 8. Personalizar a aparência

Configurações → aba **Personalização**.

### 8.1 Subir o logo

```
Logo URL:    https://meucdn.com.br/logo-acme.png
```

> 💡 Se você ainda não tem um CDN, pode usar serviços gratuitos como
> Imgur, Cloudinary ou o próprio S3 da sua hospedagem.

### 8.2 (Opcional) Subir banner

```
Banner URL:  https://meucdn.com.br/banner-acme.jpg
Favicon URL: https://meucdn.com.br/favicon-acme.ico
```

### 8.3 Definir cores

```
Cor primária:    #1976d2     (azul — botões principais)
Cor secundária:  #f57c00     (laranja — destaques)
Cor de acento:   #4caf50     (verde — confirmações)
```

> 💡 Use o seletor de cor do navegador (clique no quadrado colorido).
> Não precisa decorar código hex.

### 8.4 Escolher fonte

```
Fonte:  Poppins
```

Pode usar qualquer fonte do **Google Fonts** (Poppins, Roboto,
Montserrat, Open Sans, etc.) ou deixar em branco para a fonte padrão.

Clique em **Salvar e aplicar**.

> 💡 **O resultado aparece em tempo real** na home pública. Abra
> `seu-dominio.com.br/t/seu-slug` em outra aba para ver.

---

## 9. Testar como cliente

Antes de divulgar para o público, **agende você mesmo** para confirmar
que tudo funciona.

### 9.1 Acesse a home pública

`seu-dominio.com.br/t/seu-slug`

Você verá:

```
┌────────────────────────────────────────────────┐
│  [Banner do negócio]                           │
│                                                │
│  [Logo]  Lava-Rápido Acme         ⭐ 4.8 (32)   │
│                                                │
│  [Vitrine de serviços]                         │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐        │
│  │ Lavagem  │ │ Lavagem  │ │ Polim.   │        │
│  │ Simples  │ │ Completa │ │ Espelhado│        │
│  │ R$ 30    │ │ R$ 80    │ │ R$ 150   │        │
│  │ 30 min   │ │ 60 min   │ │ 120 min  │        │
│  └──────────┘ └──────────┘ └──────────┘        │
└────────────────────────────────────────────────┘
```

### 9.2 Faça uma reserva de teste

1. Clique em "Lavagem Simples"
2. Escolha um horário **futuro** (respeitando antecedência mínima)
3. Preencha:
   - Nome: `Teste Admin`
   - Telefone: `(11) 99999-9999` (use o seu real para receber WhatsApp)
   - E-mail: o seu
4. Escolha **Dinheiro** (não vai cobrar de verdade)
5. Confirme

Se tudo deu certo, você verá a tela de confirmação. **Boa notícia: o
fluxo público funciona.** 🎉

### 9.3 Confira no painel admin

Vá em **Agenda** (`/admin/agenda`). O agendamento de teste deve
aparecer.

Clique nele → veja os detalhes → cancele (motivo: "Teste").

---

## 10. Receber o primeiro agendamento real

### 10.1 Configurar pagamento (recomendado)

Se você quer cobrança antecipada via PIX/cartão, precisa configurar
o Mercado Pago. Siga `docs/setup-mercado-pago.md`. Resumo:

1. Crie conta no Mercado Pago Developers
2. Pegue o **Access Token** de produção
3. Defina o **Webhook Secret**
4. Configure no painel ou via variável de ambiente
5. Aponte o webhook no painel MP para
   `https://seu-dominio.com.br/api/v1/webhooks/pagamento/MercadoPago`

### 10.2 Configurar WhatsApp (recomendado)

Para enviar lembretes 24h/2h, confirmações e OTPs:

1. Siga `docs/setup-whatsapp-business.md`
2. Solicite aprovação dos templates: `lembrete_24h`, `lembrete_2h`,
   `otp_codigo_verificacao`, `link_avaliacao`
3. Pegue o **Access Token** e o **Phone Number ID**
4. Configure no servidor

### 10.3 Divulgar o link

Compartilhe `seu-dominio.com.br/t/seu-slug`:

- 📱 No status do WhatsApp Business
- 📷 Na bio do Instagram
- 🖨️ No cartão de visita / panfleto
- 🌐 No Google Meu Negócio

Quando alguém clicar, vai ver a sua vitrine personalizada e pode
agendar 24/7.

---

## 11. O que fazer no dia-a-dia

### Manhã (5 minutos)

1. Abra **Dashboard** (`/admin/dashboard`) — veja KPIs do dia
2. Abra **Agenda** (`/admin/agenda`) — confira atendimentos do dia
3. Confirme que não houve cancelamento de última hora pelo cliente

### Durante o expediente

- **Cliente chega:** clique no agendamento → **Iniciar atendimento**
- **Cliente sai:** clique no agendamento → **Concluir**
  - Sistema credita pontos de fidelidade automaticamente (10 por
    atendimento)
  - Sistema envia link de avaliação por WhatsApp
- **Cliente liga pra cancelar:** abra o agendamento → **Cancelar** →
  preencha motivo
  - Sistema notifica próximo na lista de espera automaticamente
- **Cliente faltou (no-show):** abra o agendamento → **Marcar no-show**

### Fim do dia (5 minutos)

1. Vá em **Fechar caixa** (`/admin/caixa`)
2. Confira:
   - Total de atendimentos
   - Concluídos vs. no-show
   - Receita prevista vs. recebida
3. Imprima/exporte se precisar

### Semanal (15 minutos)

- **Relatórios** (`/admin/relatorios`):
  - Top serviços
  - Taxa de no-show por dia da semana (descobre se tem dia ruim)
  - Sazonalidade mensal
- **Lista de espera** (`/admin/lista-espera`): converte fila em
  agendamentos
- **Avaliações** (`/admin/avaliacoes`): responde clientes insatisfeitos

### Mensal (1 hora)

- **KPIs** (`/admin/kpis`): mês atual × anterior
- **LTV** (Top 20 clientes): identifica os que sustentam o negócio
- **Cupons:** crie campanhas de retenção ("VOLTA10" pra clientes
  inativos)
- **Pacotes:** ajuste preços se necessário

---

## 12. Próximos passos

Depois que você dominou o básico:

1. **Cadastrar combos** — pacotes promocionais que aumentam ticket
   médio (ver Manual do Administrador, seção "Combos")
2. **Cadastrar pacotes pré-pagos** — receba antecipado, garanta
   recorrência (ver "Pacotes")
3. **Programa de fidelidade** — está ativo automaticamente; aprenda a
   trocar pontos por cupom (ver "Fidelidade")
4. **Recorrência** — cliente fixo que vem toda semana? Agende as
   próximas 12 vezes de uma vez (ver "Recorrência")
5. **Lista de espera** — não deixe slot vago (ver "Lista de espera")
6. **Importação CSV** — migrou de outro sistema? Importe a base de
   clientes (ver "Clientes → Importar")
7. **Audit log** — em caso de dúvida "quem alterou isso?" (ver
   "Auditoria")

Para detalhes de cada funcionalidade, consulte o
[Manual do Administrador](manual-administrador.md).

---

## Solução de problemas comuns

### "Esqueci a senha"

- Clique em **Esqueci a senha** na tela de login
- Digite seu e-mail
- Você receberá um link no e-mail (válido por 1 hora)
- Clique no link e defina uma nova senha

### "Não consigo entrar — diz que estou bloqueado"

- Errou a senha 5 vezes seguidas → bloqueio de 15 minutos
- Aguarde 15 min ou use **Esqueci a senha** (reset libera o bloqueio)

### "Cliente não recebeu WhatsApp"

- Verifique se o WhatsApp está configurado (ver
  `setup-whatsapp-business.md`)
- Confira se o número do cliente está correto (com DDD)
- Templates da Meta podem cair em revisão — peça pro time técnico
  conferir os logs

### "Agenda não está aceitando reservas"

- Verifique se o horário de funcionamento está configurado para o
  dia da semana
- Verifique se há recurso ativo cadastrado
- Confira se algum bloqueio cobre a data/hora desejada

### "Pagamento aparece como Pendente eternamente"

- Cliente pagou mas o webhook do Mercado Pago não chegou
- Verifique se o webhook está configurado no painel MP
- Verifique nos logs se há erro de assinatura HMAC
- Você pode forçar manualmente: abra o agendamento → **Confirmar
  pagamento** (precisa policy AdminTenant)

### "Quero remover dados de um cliente (LGPD)"

- Vá em **Clientes** → encontre o cliente → **Anonimizar**
- Sistema remove nome, e-mail, telefone, CPF e todas as fotos
- Histórico de agendamentos é preservado (cliente fica como "Cliente
  removido #ID")

---

*Tutorial criado em maio de 2026. Para referência completa de cada
funcionalidade, consulte o
[Manual do Administrador](manual-administrador.md).*
