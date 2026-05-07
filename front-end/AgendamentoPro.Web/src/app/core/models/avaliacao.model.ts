export interface Avaliacao {
  id: number;
  agendamentoId: number;
  token: string;
  clienteNome?: string;
  nota?: number;
  comentario?: string;
  criadoEm: string;
  respondidoEm?: string;
  publica: boolean;
}

export interface AvaliacaoPublica {
  clienteNome: string;
  nota: number;
  comentario?: string;
  respondidoEm: string;
}

export interface ResumoAvaliacoes {
  media: number;
  total: number;
  recentes: AvaliacaoPublica[];
}

export interface ResponderAvaliacaoInput {
  nota: number;
  comentario?: string;
}
