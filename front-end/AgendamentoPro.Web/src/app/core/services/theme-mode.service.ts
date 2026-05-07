import { Injectable, signal, effect } from '@angular/core';

/**
 * Dark / Light mode com preferência persistida em localStorage.
 * Default: respeita prefers-color-scheme do SO.
 *
 * Uso: chamar `.alternar()` ou `.set('dark'|'light')`. O effect aplica
 * `data-theme` no <html> que ativa as variáveis CSS de _theme.scss.
 */
@Injectable({ providedIn: 'root' })
export class ThemeModeService {
  private readonly storageKey = 'agp.theme';
  readonly mode = signal<'light' | 'dark'>(this.detectarInicial());

  constructor() {
    effect(() => {
      const m = this.mode();
      document.documentElement.dataset['theme'] = m;
      localStorage.setItem(this.storageKey, m);
    });
  }

  alternar() {
    this.mode.set(this.mode() === 'dark' ? 'light' : 'dark');
  }

  set(m: 'light' | 'dark') {
    this.mode.set(m);
  }

  private detectarInicial(): 'light' | 'dark' {
    const salvo = localStorage.getItem(this.storageKey);
    if (salvo === 'dark' || salvo === 'light') return salvo;
    return window.matchMedia?.('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  }
}
