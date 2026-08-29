import { environment } from '../../../environments/environment';

/**
 * Converte URL relativa de upload ("/uploads/...") em absoluta apontando para a
 * raiz da API. Necessário porque em dev o frontend roda em :4200 e as imagens são
 * servidas pela API em :5050 — uma URL relativa resolveria contra o host errado.
 * URLs já absolutas (http/https, ex.: S3/CDN ou link externo do lojista) e data:
 * passam intocadas.
 */
export function urlUpload(url: string | undefined | null): string {
  if (!url) return '';
  if (/^https?:\/\//i.test(url) || url.startsWith('data:')) return url;
  const raiz = environment.apiUrl
    .replace(/\/api\/v\d+\/?$/i, '')
    .replace(/\/api\/?$/i, '');
  return raiz + (url.startsWith('/') ? url : '/' + url);
}
