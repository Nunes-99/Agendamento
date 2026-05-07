export type TipoFoto = 1 | 2 | 3; // 1=Antes, 2=Depois, 3=Geral

export const TipoFotoLabel: Record<TipoFoto, string> = {
  1: 'Antes',
  2: 'Depois',
  3: 'Geral'
};

export interface FotoAgendamento {
  id: number;
  agendamentoId: number;
  tipo: TipoFoto;
  url: string;
  contentType?: string;
  tamanhoBytes: number;
  criadoEm: string;
}
