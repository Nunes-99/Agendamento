# Setup WhatsApp Business — AgendamentoPro

Guia passo a passo para configurar a integração WhatsApp Cloud API que envia
lembretes automáticos (24h e 2h antes do agendamento) e link de avaliação ao
concluir o atendimento.

> **Modelo escolhido**: centralizado. Você (operador do SaaS) cria UM Business
> Manager, registra UM número, e todas as mensagens de todos os tenants saem
> dele. Custo no tier free cobre operação pequena/média (1.000 conversas
> iniciadas pela empresa por mês, gratuitas).

---

## Pré-requisitos

- [ ] **Conta Facebook pessoal** com 2FA ativado (não pode ser perfil novo recém-criado — Meta bloqueia).
- [ ] **CNPJ** ativo (no caso, o da ACSN).
- [ ] **Cartão de crédito** internacional (Meta exige cadastrar mesmo sem cobrança imediata).
- [ ] **Número de telefone dedicado** que ainda não esteja vinculado a outro WhatsApp Business.
  - ⚠️ Após registrar, esse número não pode mais ser usado nem no app do WhatsApp comum nem no app do WhatsApp Business — fica "preso" à API.
  - Opções: chip novo, número VoIP (Twilio, Z-API, Take Blip), ramal de PABX virtual.
  - Recomendação: chip dedicado só pro AgendamentoPro.

Tempo estimado total: **~2 horas** (mais ~24h de espera para verificação opcional do negócio).

---

## Passo 1 — Criar Meta Business Manager

1. Acesse https://business.facebook.com
2. Clique em **Criar conta** (canto superior direito)
3. Preencha:
   - **Nome da empresa**: ACSN (ou o nome comercial)
   - **Seu nome**: Vitor Nunes
   - **E-mail comercial**: desenvolvimento@acsn.com.br
4. Confirme o e-mail (vai chegar um link)

> Quando logar, você verá o painel com o nome do Business Manager no topo.

---

## Passo 2 — Adicionar dados da empresa

1. Painel esquerdo → **Configurações do Business** → **Informações da empresa**
2. Clique em **Editar** e preencha:
   - **Nome legal**: razão social da ACSN
   - **CNPJ**
   - **Endereço completo**
   - **Telefone**
   - **Site**: connectveiculos.dev.br (ou outro)
3. **(Opcional, mas recomendado) Verificação do negócio**:
   - Painel esquerdo → **Centro de Segurança** → **Iniciar verificação**
   - Sobe um documento (CNPJ atualizado, conta de luz, etc.)
   - Aprovação leva 24-72h
   - Sem verificação: limite de 1 número, 250 conversas iniciadas por dia. Com verificação: até 20 números, sem limite diário.

---

## Passo 3 — Criar uma conta WhatsApp Business (WABA)

1. **Configurações do Business** → **Contas** → **Contas do WhatsApp** → **Adicionar**
2. Clique em **Criar uma nova conta do WhatsApp**
3. Nome: "AgendamentoPro" (ou o que preferir — não aparece para o cliente final)
4. Fuso horário: GMT-3 (Brasília)
5. **Adicionar pessoas**: adicione você mesmo como administrador

---

## Passo 4 — Adicionar número de telefone

1. Dentro da WABA recém-criada → **Configurações do WhatsApp** → **Números de telefone** → **Adicionar número de telefone**
2. **Nome para exibição** (importante — aparece pro cliente final):
   - Ex: "AgendamentoPro" ou "ACSN Agendamentos"
   - **Regras da Meta**: não pode ter "WhatsApp" no nome, não pode ser muito genérico, não pode prometer benefícios financeiros. Aprovação leva ~30min.
3. **Categoria**: Tecnologia / Software
4. **Site**: deixe a URL do AgendamentoPro
5. **Verificar número**:
   - Insira o número (formato internacional: +55 XX XXXXXXXXX)
   - Escolha **SMS** ou **Chamada**
   - Insira o código que chegar
6. Aguarde aprovação do display name (~30min, e-mail confirma)

---

## Passo 5 — Criar System User e gerar token permanente

> Tokens "temporários" expiram em 24h. Para uso em produção precisa de System User Token.

1. **Configurações do Business** → **Usuários** → **Usuários do sistema** → **Adicionar**
2. Nome: `agendamentopro-api`
3. Papel: **Administrador**
4. Clicar em **Adicionar ativos** → **Apps** → marque o app default da WABA
5. Em ativos atribuídos, clicar nos 3 pontos → permissões:
   - ✅ `whatsapp_business_messaging`
   - ✅ `whatsapp_business_management`
6. Voltar pro System User → **Gerar novo token**
   - App: o mesmo da WABA
   - Validade: **Nunca expira**
   - Permissões: as duas marcadas acima
7. **Copie o token agora** (formato `EAAB...`, ~200 caracteres). Se fechar sem copiar, precisa gerar outro.

> **Salve em local seguro** (KeePass, 1Password). É o `WHATSAPP_ACCESS_TOKEN`.

---

## Passo 6 — Pegar o Phone Number ID

1. **Configurações do WhatsApp** → **Números de telefone**
2. Ao lado do número, clique em **Visão geral da API** (ou no número diretamente)
3. Anote o campo **Identificação do número de telefone** (15 dígitos numéricos, tipo `123456789012345`)

> Esse é o `WHATSAPP_PHONE_NUMBER_ID`.

---

## Passo 7 — Configurar no AgendamentoPro

No `.env` da API:

```env
WHATSAPP_ACCESS_TOKEN=EAAB...   # token do passo 5
WHATSAPP_PHONE_NUMBER_ID=123456789012345  # do passo 6
WHATSAPP_API_VERSION=v19.0
```

Reinicie a API. No log de boot você NÃO deve ver mais o aviso "WhatsApp Cloud API não configurado".

Para testar manualmente, abra o Swagger e dispare um agendamento que conclua — ou aguarde 5 min e veja o log do `LembreteBackgroundService` rodar.

---

## Passo 8 — Criar e submeter os templates

Mensagens proativas (lembretes, confirmações) só podem usar **templates pré-aprovados**.

### 8.1 Acesse o Gerenciador de Modelos

1. Painel → **Catálogo de mensagens** → **Modelos de mensagem** (ou direto em https://business.facebook.com/wa/manage/message-templates/)
2. Clique em **Criar modelo**

### 8.2 Template `lembrete_24h`

| Campo | Valor |
|---|---|
| Nome | `lembrete_24h` |
| Categoria | **Utility** (não Marketing — utility é mais barato e aprova mais rápido) |
| Idiomas | Português (BR) |

**Corpo da mensagem** (cole exatamente):
```
Olá {{1}}! 👋

Lembrando do seu agendamento amanhã:

📋 Serviço: {{2}}
📅 Data: {{3}}
⏰ Horário: {{4}}
📍 Local: {{5}}

Se precisar reagendar, responda esta mensagem.
```

**Exemplos para os parâmetros** (a Meta exige):
- {{1}}: João Silva
- {{2}}: Lavagem completa
- {{3}}: 15/05/2026
- {{4}}: 14:30
- {{5}}: Auto Wash Premium

Salvar e enviar para revisão.

### 8.3 Template `lembrete_2h`

| Campo | Valor |
|---|---|
| Nome | `lembrete_2h` |
| Categoria | **Utility** |
| Idiomas | Português (BR) |

**Corpo**:
```
Oi {{1}}! Faltam 2 horas para seu atendimento ⏰

📋 {{2}}
⏰ Hoje às {{3}}

Estamos te esperando! Se houver imprevisto, responda aqui.
```

**Exemplos**:
- {{1}}: João Silva
- {{2}}: Lavagem completa
- {{3}}: 14:30

### 8.4 (Opcional) Template `link_avaliacao`

Para enviar o link de avaliação ao concluir agendamento. Não é usado pelo código atual, mas é útil:

| Campo | Valor |
|---|---|
| Nome | `link_avaliacao` |
| Categoria | **Utility** |

**Corpo**:
```
Oi {{1}}! Como foi seu atendimento de hoje?

Sua opinião nos ajuda a melhorar 💚
Avalie em: {{2}}
```

### 8.5 Aguardar aprovação

- Tempo típico: **5 min a 24h**
- Status fica em "Em análise" → "Aprovado" ou "Rejeitado"
- Se rejeitado: Meta indica o motivo (ex: linguagem promocional, formato inválido). Edite e reenvie.

---

## Passo 9 — Cadastrar método de pagamento

Mesmo no tier grátis, Meta exige cadastrar cartão.

1. **Configurações do Business** → **Pagamentos** → **Adicionar nova forma de pagamento**
2. Cartão de crédito da empresa
3. Meta cobra somente quando você passa do tier grátis. Configure **alerta de gasto** (ex: avisar a cada R$ 50).

---

## Passo 10 — Validar tudo

Checklist final:

- [ ] WHATSAPP_ACCESS_TOKEN no `.env` (token permanente)
- [ ] WHATSAPP_PHONE_NUMBER_ID no `.env`
- [ ] API reiniciada
- [ ] Display name aprovado (verifica em Configurações → Números → seu número → status "Aprovado")
- [ ] Template `lembrete_24h` aprovado
- [ ] Template `lembrete_2h` aprovado
- [ ] Cartão cadastrado em Pagamentos
- [ ] Webhook configurado **se** for receber respostas dos clientes — fora do escopo dos lembretes; opcional

Para um teste fim a fim:

1. Crie um agendamento no `/admin/agendamentos` com horário em ~2h05min
2. Aguarde 5 min (intervalo do `LembreteBackgroundService`)
3. Cliente recebe a mensagem `lembrete_2h` no WhatsApp
4. Logs da API: procure por "Falha ao enviar Lembrete2h" — se aparecer, há problema de template/permissão

---

## Custos estimados

| Volume | Custo mensal aprox. |
|---|---|
| Até 1.000 conversas iniciadas pela empresa/mês (utility) | **R$ 0** (tier free) |
| 1.001–5.000 conversas utility | ~R$ 100–250 |
| Conversas iniciadas pelo cliente (resposta dentro de 24h) | **R$ 0** (sempre grátis) |

> Cada **conversa** dura 24h: dentro dela você pode mandar quantas mensagens quiser sem pagar mais.

---

## Troubleshooting

### "Recipient phone number not in allowed list"

Aparece em modo de **desenvolvimento**. Solução:
- Configurações do WhatsApp → Início → seção **Números de teste**
- Adicione os números (com DDI) que vão receber em desenvolvimento
- Ou mova para produção (Live mode) — exige template aprovado e display name aprovado.

### "Template name not found"

- Confirme que o template está com status **Aprovado** (não "Em análise" nem "Rejeitado")
- Confirme que o `language` no código bate com o do template (`pt_BR` vs `pt`)
- O nome é case-sensitive no envio: `lembrete_24h` ≠ `Lembrete_24h`

### Mensagem não chega mas API retorna 200

- Cliente bloqueou seu número (não há como detectar)
- Cliente não tem WhatsApp instalado
- Número errado — confirme com DDI 55 + DDD + 9 dígitos no Brasil

### "Phone number is not yet approved"

Display name ainda não foi aprovado pela Meta. Aguarde até 30min após registro.
Se demorar mais, abra ticket em Suporte → Recursos.

---

## Links úteis

- Painel Business: https://business.facebook.com
- Gerenciador de Modelos: https://business.facebook.com/wa/manage/message-templates/
- Documentação Cloud API: https://developers.facebook.com/docs/whatsapp/cloud-api
- Pricing: https://developers.facebook.com/docs/whatsapp/pricing
- Status da plataforma: https://metastatus.com/whatsapp-business-api
