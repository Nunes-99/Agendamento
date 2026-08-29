# Roteiro de demonstração

Como mostrar o AgendamentoPro para um cliente em **15 minutos**, com um
ambiente já cheio de dados plausíveis.

---

## 1. Preparar o ambiente (5 min, antes da reunião)

### 1.1 Subir o sistema

```bash
# API
cd back-end
dotnet run --project AgendamentoPro.API

# Frontend (outro terminal)
cd front-end/AgendamentoPro.Web
npm start
```

### 1.2 Criar a empresa de demonstração

Entre como **SuperAdmin** em `/admin/login` (deixe o campo *Empresa* vazio),
vá em **Empresas → Nova empresa** e preencha. O passo que importa:

> ✅ marque **"Preencher com dados fictícios (demonstração)"**

Isso popula a empresa inteira num clique:

| O que | Quanto |
| --- | --- |
| Serviços | 6 (de R$ 35 a R$ 380) |
| Boxes/recursos | 4 |
| Clientes | 30 com nomes e telefones brasileiros |
| Agendamentos | ~85, nos últimos 30 dias e próximos 14 |
| Avaliações | 12 (média ~4,2) |
| Combos | 2 promocionais |
| Cupons | BEMVINDO10 (10%) e VOLTA20 (R$ 20) |
| Pacotes pré-pagos | 2 |
| Pontos de fidelidade | 7 clientes, um deles com 120 pts (dá para trocar na hora) |
| Bloqueio de agenda | 1 feriado à frente |
| Lista de espera | 2 pessoas na fila |
| Vitrine | cores, fonte Poppins e 3 anúncios publicados |

Os agendamentos vêm com histórico realista: concluídos, cancelados,
no-shows, confirmados e pendentes de pagamento — o dashboard e os
relatórios abrem com números de verdade, não zerados.

### 1.3 Últimos 2 minutos de capricho

A galeria de fotos vem **vazia de propósito** (fotos são do espaço real).
Se a demo for para um segmento específico, suba 3 fotos em
**Configurações → Minha página → Galeria** — é o que faz a página parecer
de um negócio de verdade.

> 💡 Se for demonstrar para um lava-rápido, os dados já servem. Para
> barbearia/clínica, renomeie 2 ou 3 serviços antes (leva 1 minuto) —
> "Lavagem Completa" vira "Corte + Barba" e a conversa flui melhor.

---

## 2. O roteiro (15 min)

### Parte 1 — "É assim que o seu cliente vê" (4 min)

Abra `seu-dominio/t/{slug}` **no celular** (ou numa janela estreita).

1. **A página da loja**: capa, logo, a promoção da semana em destaque,
   os serviços com preço e duração, as fotos do espaço, a nota das
   avaliações. *"Isso aqui é seu, não é a cara de um app genérico."*
2. **Agendar**: escolha um serviço → os horários livres aparecem
   respeitando a duração do serviço e o intervalo entre atendimentos →
   escolha um → preencha nome e telefone → PIX.
3. **O QR aparece na hora.** *"O cliente paga o sinal antes de sair de
   casa — é isso que derruba o não-comparecimento."*

> Se o Mercado Pago não estiver configurado no ambiente, escolha
> **Dinheiro**: o agendamento é criado do mesmo jeito e você fala do PIX
> em vez de mostrar.

### Parte 2 — "E é assim que você administra" (6 min)

Entre no painel com o admin da empresa demo.

1. **Dashboard**: receita do mês, agendamentos, gráfico dos últimos 30
   dias, ranking de serviços. *"Você abre isso de manhã com o café."*
2. **Agenda**: o atendimento que acabou de ser criado está lá. Clique →
   **Iniciar** → **Concluir**. Mostre que ao concluir:
   - o cliente ganha 10 pontos de fidelidade
   - o link de avaliação é gerado
3. **Fidelidade**: busque um cliente pelo nome → o com 120 pontos →
   **troque por cupom** na frente dele. *"Fidelidade que funciona sozinha."*
4. **Lista de espera**: mostre a fila. *"Cancelou? O próximo é avisado."*
5. **Relatórios**: taxa de no-show por dia da semana e LTV.
   *"Aqui você descobre que terça de manhã é buraco — e cria promoção pra
   isso."*

### Parte 3 — "E dá pra deixar com a sua cara" (3 min)

**Configurações → Minha página**, com a página pública aberta ao lado.

1. Troque a **cor primária** para a cor da marca dele → salve → recarregue
   a página pública. *"Botões, preços, tudo acompanha."*
2. Publique um **anúncio** na hora: "Promoção de inauguração — 15% off".
   Recarregue: apareceu no topo.
3. Mostre o **upload de imagem com corte** (banner). *"Sem designer, sem
   hospedar foto em lugar nenhum."*

### Parte 4 — Fechamento (2 min)

- **Preço**: R$ 29,90/mês, **primeiro mês grátis**, sem comissão por
  transação (mostre `/planos`).
- **O que ele precisa**: conta Mercado Pago (para receber) e, se quiser
  lembretes automáticos, WhatsApp Business.
- **Tempo de configuração**: ~30 minutos para serviços, horários e visual.

---

## 3. Perguntas que sempre aparecem

| Pergunta | Resposta curta |
| --- | --- |
| "E se o cliente não pagar o sinal?" | O horário não fica preso: a reserva expira em 15 min e o slot volta a ficar livre. |
| "Posso cobrar só na hora?" | Pode. Configure o % de entrada como 0 ou use a forma "Dinheiro". |
| "E se eu tiver 2 unidades?" | Plano Multi-unidade, unidades ilimitadas. |
| "Vocês ficam com % do que eu recebo?" | Não. Só a mensalidade — o pagamento do cliente cai direto na sua conta do Mercado Pago. |
| "Funciona no celular?" | Sim, e o cliente pode instalar como app (PWA). Mostre no seu próprio celular. |
| "E se eu quiser sair?" | Cancela pelo painel, sem ligação. Dados ficam disponíveis por 90 dias. |
| "Meus clientes precisam criar conta?" | Não. Agendam com nome e telefone. Quem quiser acompanhar histórico entra com código no WhatsApp. |

---

## 4. Depois da demonstração

- O ambiente de demo pode ser **descartado**: exclua a empresa em
  **Empresas** (super-admin) e crie outra na próxima demo, sempre limpa.
- Para um piloto de verdade, crie a empresa **sem** dados fictícios e
  configure com os dados reais do cliente (ver
  [`tutorial-primeiros-passos.md`](tutorial-primeiros-passos.md)).

> ⚠️ Nunca marque "dados fictícios" numa empresa que vai virar produção —
> o cliente ficaria com 30 clientes e 85 agendamentos inventados na conta.
