export enum StatusAgendamento {
  PendentePagamento = 0,
  Confirmado = 1,
  EmAndamento = 2,
  Concluido = 3,
  Cancelado = 4,
  NoShow = 5
}

export enum StatusPagamento {
  Pendente = 0,
  Aprovado = 1,
  Recusado = 2,
  Estornado = 3,
  Expirado = 4
}

export enum FormaPagamento {
  CartaoCredito = 0,
  CartaoDebito = 1,
  Pix = 2,
  Dinheiro = 3,
  Boleto = 4
}

export interface SlotDisponivel {
  data: string;
  horaInicio: string;
  horaFim: string;
  recursoId: number;
  recursoNome: string;
}

export interface DiaDisponivel {
  data: string;
  vagas: number;
  primeiroHorario?: string;
}

export interface Agendamento {
  id: number;
  tenantId: number;
  clienteId: number;
  clienteNome: string;
  clienteTelefone?: string;
  servicoId: number;
  servicoNome: string;
  recursoId: number;
  recursoNome: string;
  data: string;
  horaInicio: string;
  horaFim: string;
  status: StatusAgendamento;
  statusDescricao: string;
  statusPagamento: StatusPagamento;
  valorTotal: number;
  valorEntrada: number;
  observacao?: string;
  motivoCancelamento?: string;
  criadoEm: string;
  avaliacaoToken?: string;
  grupoComboId?: string;
}

export interface CriarAgendamentoInput {
  servicoId: number;
  recursoId?: number;
  data: string;
  horaInicio: string;
  observacao?: string;
  cliente: {
    nome: string;
    email?: string;
    telefone: string;
    whatsApp?: string;
    cpf?: string;
  };
  formaPagamento: FormaPagamento;
}

export interface CobrancaPendente {
  id: number;
  forma: FormaPagamento;
  status: StatusPagamento;
  valor: number;
  qrCode?: string;
  linkPagamento?: string;
  expiracao?: string;
}

export interface CriarAgendamentoResult {
  agendamento: Agendamento;
  pagamento?: {
    id: number;
    forma: FormaPagamento;
    status: StatusPagamento;
    valor: number;
    qrCode?: string;
    linkPagamento?: string;
    expiracao?: string;
  };
}
