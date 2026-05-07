export interface Servico {
  id: number;
  tenantId: number;
  nome: string;
  descricao: string;
  preco: number;
  duracaoMinutos: number;
  imagemUrl?: string;
  categoria?: string;
  ordem: number;
  ativo: boolean;
}
