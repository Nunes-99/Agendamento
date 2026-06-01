export enum StatusAssinatura {
  Trial = 0,
  Ativa = 1,
  Atrasada = 2,
  ReadOnly = 3,
  Cancelada = 4,
  Expirada = 5
}

export enum StatusFaturaAssinatura {
  Pendente = 0,
  Paga = 1,
  Recusada = 2,
  Estornada = 3
}

export interface Plano {
  id: number;
  nome: string;
  descricao: string;
  preco: number;
  limiteUnidades: number;
  limiteProfissionais: number;
  limiteAgendamentosMes: number;
}

export interface FaturaAssinatura {
  id: number;
  valor: number;
  status: StatusFaturaAssinatura;
  statusTexto: string;
  referenciaInicio: string;
  referenciaFim: string;
  vencimentoEm: string;
  pagoEm?: string;
}

export interface Assinatura {
  id: number;
  planoId: number;
  planoNome: string;
  planoPreco: number;
  status: StatusAssinatura;
  statusTexto: string;
  gateway: string;
  dataInicio: string;
  trialAteEm?: string;
  proximoVencimento?: string;
  ultimoPagamentoEm?: string;
  atrasoDesde?: string;
  readOnlyDesde?: string;
  canceladaEm?: string;
  permiteEscrita: boolean;
  checkoutUrl?: string;
  faturas: FaturaAssinatura[];
}

export interface CriarAssinaturaInput {
  planoId: number;
  payerEmail: string;
}

export interface AlterarPlanoInput {
  novoPlanoId: number;
}
