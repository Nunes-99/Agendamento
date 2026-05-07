export interface ComboServico {
  servicoId: number;
  nome: string;
  preco: number;
  duracaoMinutos: number;
}

export interface Combo {
  id: number;
  nome: string;
  descricao?: string;
  imagemUrl?: string;
  precoOriginal: number;
  precoPromocional: number;
  economia: number;
  ordem: number;
  ativo: boolean;
  servicos: ComboServico[];
}

export interface ComboInput {
  nome: string;
  descricao?: string;
  imagemUrl?: string;
  precoPromocional: number;
  ordem: number;
  ativo: boolean;
  servicoIds: number[];
}
