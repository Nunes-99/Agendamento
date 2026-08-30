/**
 * Limites de texto das telas — os mesmos das colunas do banco
 * (ver AgendamentoProDbContext) e da validação de domínio (CampoTexto).
 * Repetidos aqui só para o `maxlength` do input avisar antes de o usuário
 * digitar 300 caracteres e perder o texto no envio.
 */
export const LIMITES = {
  nome: 200,
  email: 255,
  telefone: 20,   // cabe "(11) 98888-7777"
  cpf: 14,        // "000.000.000-00"
  cnpj: 18,
  observacao: 1000,
  observacaoCurta: 500,
  titulo: 120,
  endereco: 300,
  motivo: 500
} as const;

// Mesma regra do back-end (CampoTexto.EmailValido): recusa o que gateway e
// servidor de e-mail recusariam, sem tentar implementar a RFC 5322.
const EMAIL = /^[^@\s]+@[^@\s.]+(\.[^@\s.]+)+$/;

export function emailValido(valor: string): boolean {
  return EMAIL.test((valor || '').trim());
}

/**
 * Mensagem de erro vinda da API, na ordem em que ela realmente aparece:
 * `message` (compat), `detail` (ProblemDetails) e por fim o texto cru — que é
 * o que chega quando a resposta não é JSON. Sem isso a tela caía no genérico
 * ("Falha ao agendar") mesmo quando o servidor tinha explicado o motivo.
 */
export function mensagemErroApi(err: any, padrao: string): string {
  const corpo = err?.error;
  if (typeof corpo === 'string' && corpo.trim() && !corpo.trim().startsWith('<')) {
    return corpo.trim();
  }
  const msg = corpo?.message || corpo?.detail || corpo?.title;
  if (typeof msg === 'string' && msg.trim()) return msg.trim();

  // Erros de validação do ASP.NET vêm em `errors: { Campo: ["msg"] }`.
  const errors = corpo?.errors;
  if (errors && typeof errors === 'object') {
    const primeira = Object.values(errors).flat()[0];
    if (typeof primeira === 'string') return primeira;
  }
  if (err?.status === 0) return 'Sem conexão com o servidor. Verifique a internet.';
  if (err?.status === 429) return 'Muitas tentativas seguidas. Aguarde um minuto e tente de novo.';
  return padrao;
}
