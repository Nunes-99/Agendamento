import { Injectable } from '@angular/core';
import { Personalizacao } from '../models/tenant.model';
import { urlUpload } from '../utils/url.util';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  aplicarPadrao(): void {
    const root = document.documentElement;
    root.style.setProperty('--cor-primaria', '#1976d2');
    root.style.setProperty('--cor-secundaria', '#424242');
    root.style.setProperty('--cor-acento', '#ff4081');
    root.style.setProperty('--app-font', 'Roboto');
  }

  // Fontes já injetadas nesta sessão — evita duplicar <link> a cada navegação.
  private static fontesCarregadas = new Set<string>();

  /// Definir só o font-family não basta: sem baixar a fonte, o navegador cai no
  /// fallback e a personalização "não funciona". Injeta o stylesheet do Google
  /// Fonts para a fonte escolhida; se ela não existir lá, o 404 é silencioso e o
  /// fallback (Roboto/sans-serif) continua valendo.
  private carregarFonte(fonte: string): void {
    const nome = (fonte || '').trim();
    if (!nome || ThemeService.fontesCarregadas.has(nome)) return;
    const sistema = ['arial', 'helvetica', 'verdana', 'georgia', 'tahoma',
      'times new roman', 'sans-serif', 'serif', 'monospace', 'roboto'];
    if (sistema.includes(nome.toLowerCase())) return;
    const link = document.createElement('link');
    link.rel = 'stylesheet';
    link.href = 'https://fonts.googleapis.com/css2?family='
      + encodeURIComponent(nome).replace(/%20/g, '+')
      + ':wght@400;500;600;700&display=swap';
    document.head.appendChild(link);
    ThemeService.fontesCarregadas.add(nome);
  }

  aplicarPersonalizacao(p: Personalizacao | undefined | null): void {
    if (!p) return;
    const root = document.documentElement;
    if (p.corPrimaria) root.style.setProperty('--cor-primaria', p.corPrimaria);
    if (p.corSecundaria) root.style.setProperty('--cor-secundaria', p.corSecundaria);
    if (p.corAcento) root.style.setProperty('--cor-acento', p.corAcento);
    if (p.fonte) {
      this.carregarFonte(p.fonte);
      root.style.setProperty('--app-font', p.fonte);
      // Inline no body: o CSS de tipografia do Material vence a regra global de
      // font-family via cascata, então só setar a var não trocava a fonte.
      document.body.style.fontFamily = `'${p.fonte}', Roboto, sans-serif`;
    }
    if (p.faviconUrl) {
      const link: HTMLLinkElement = document.querySelector("link[rel*='icon']") || document.createElement('link');
      link.rel = 'icon';
      link.href = urlUpload(p.faviconUrl); // upload relativo é servido pela API
      document.head.appendChild(link);
    }
  }
}
