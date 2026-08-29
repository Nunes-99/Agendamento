export interface Personalizacao {
  logoUrl?: string;
  bannerUrl?: string;
  faviconUrl?: string;
  corPrimaria: string;
  corSecundaria: string;
  corAcento: string;
  fonte: string;
}

/** Anúncio/promoção que o lojista publica na vitrine (home pública). */
export interface AnuncioVitrine {
  titulo: string;
  texto?: string;
  /** Destaque usa a cor de acento do tenant. */
  destaque: boolean;
  ativo: boolean;
}

export interface RegrasNegocio {
  percentualEntrada: number;
  bufferMinutos: number;
  antecedenciaMinHoras: number;
  antecedenciaMaxDias: number;
  limiteCancelamentoHoras: number;
}

export interface Tenant {
  id: number;
  nome: string;
  slug: string;
  segmento: string;
  cnpj?: string;
  email: string;
  telefone: string;
  whatsApp?: string;
  endereco?: string;
  cidade?: string;
  estado?: string;
  cep?: string;
  descricao?: string;
  ativo: boolean;
  personalizacao: Personalizacao;
  regras: RegrasNegocio;
}
